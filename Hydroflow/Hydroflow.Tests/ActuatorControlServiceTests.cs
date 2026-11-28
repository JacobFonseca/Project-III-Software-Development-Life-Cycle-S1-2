using System;
using System.Collections.Generic;
using System.Linq;
using HydroFlow.Core;
using HydroFlow.Controller;
using HydroFlow.Devices;

namespace HydroFlow.Tests;

[TestClass]
public class ActuatorControlServiceTests
{
    private Simulation _simulation = null!;
    private ActuatorControlService _service = null!;
    private List<TelemetryPacket> _packets = null!;

    private class FakeSource : IDataSource
    {
        public double ReadNext() => 0;
    }

    [TestInitialize]
    public void Setup()
    {
        _simulation = new Simulation(
            _ => new FakeSource());

        _packets = new List<TelemetryPacket>();

        _simulation.Bus.Subscribe(
            packet => _packets.Add(packet));

        _service = new ActuatorControlService(
            _simulation.Actuators,
            _simulation.Bus);
    }

    private Actuator GetActuator(string deviceId)
    {
        return _simulation.Actuators.Single(
            a => a.DeviceId == deviceId);
    }

    // Test ON and the default setting for all five devices.
    [TestMethod]
    [DataRow("FAN-01", 1000.0)]
    [DataRow("VLV-01", 1.0)]
    [DataRow("PHP-01", 50.0)]
    [DataRow("NUT-01", 50.0)]
    [DataRow("LED-01", 80.0)]
    public void Set_On_UsesDefaultSetting(
        string deviceId, double expectedSetting)
    {
        _service.Set(deviceId, true);

        var actuator = GetActuator(deviceId);

        Assert.IsTrue(actuator.IsOn);
        Assert.AreEqual(expectedSetting, actuator.Setting);
    }

    // Test OFF for all five devices.
    [TestMethod]
    [DataRow("FAN-01")]
    [DataRow("VLV-01")]
    [DataRow("PHP-01")]
    [DataRow("NUT-01")]
    [DataRow("LED-01")]
    public void Set_Off_TurnsDeviceOff(string deviceId)
    {
        _service.Set(deviceId, true);
        _service.Set(deviceId, false);

        var actuator = GetActuator(deviceId);

        Assert.IsFalse(actuator.IsOn);
        Assert.AreEqual(0.0, actuator.Setting);
    }

    // Each ON or OFF action publishes one Command packet.
    [TestMethod]
    [DataRow("FAN-01")]
    [DataRow("VLV-01")]
    [DataRow("PHP-01")]
    [DataRow("NUT-01")]
    [DataRow("LED-01")]
    public void Set_Toggle_PublishesOneCommandPacket(
        string deviceId)
    {
        _service.Set(deviceId, true);

        var commands = _packets
            .Where(p => p.Type == "Command")
            .ToList();

        Assert.AreEqual(1, commands.Count);
        Assert.AreEqual(deviceId, commands[0].DeviceId);

        _packets.Clear();

        _service.Set(deviceId, false);

        commands = _packets
            .Where(p => p.Type == "Command")
            .ToList();

        Assert.AreEqual(1, commands.Count);
        Assert.AreEqual(deviceId, commands[0].DeviceId);
    }

    // Invalid settings must not change the device.
    [TestMethod]
    [DataRow("LED-01")]
    [DataRow("VLV-01")]
    public void Set_InvalidSetting_LeavesDeviceUnchanged(
        string deviceId)
    {
        _service.Set(deviceId, true);

        var actuator = GetActuator(deviceId);

        bool previousState = actuator.IsOn;
        double previousSetting = actuator.Setting;

        _packets.Clear();

        Assert.ThrowsException<ArgumentOutOfRangeException>(
            () => _service.Set(deviceId, false, 101));

        Assert.AreEqual(previousState, actuator.IsOn);
        Assert.AreEqual(previousSetting, actuator.Setting);

        Assert.AreEqual(0,
            _packets.Count(p => p.Type == "Command"));
    }
}