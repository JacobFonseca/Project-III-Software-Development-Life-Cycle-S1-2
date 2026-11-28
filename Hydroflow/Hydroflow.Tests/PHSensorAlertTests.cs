
using System;
using System.Collections.Generic;
using HydroFlow.Core;
using HydroFlow.Controller;

namespace Hydroflow.Tests;

[TestClass]
public class PHSensorAlertTests
{
    private MessageBus _bus = null!;
    private List<TelemetryPacket> _alarms = null!;

    [TestInitialize]
    public void Setup()
    {
        _bus = new MessageBus();
        _alarms = new List<TelemetryPacket>();

        _ = new SCADAController(_bus);

        _bus.Subscribe(packet =>
        {
            if (packet.Type == "Alarm")
                _alarms.Add(packet);
        });
    }

    private void PublishPH(double value)
    {
        _bus.Publish(new TelemetryPacket(
            "PH-01",
            "Sensor",
            value,
            "pH",
            DateTime.UtcNow,
            DeviceStatus.Normal));
    }

    // Normal pH should not generate an alert.
    [TestMethod]
    public void NormalPH_NoAlert()
    {
        PublishPH(6.0);

        Assert.AreEqual(0, _alarms.Count);
    }

    // High pH should generate a warning.
    [TestMethod]
    public void HighPH_GeneratesWarning()
    {
        PublishPH(6.8);

        Assert.AreEqual(1, _alarms.Count);
        Assert.AreEqual(DeviceStatus.Warning,
            _alarms[0].Status);
    }

    // Very high pH should generate an alarm.
    [TestMethod]
    public void VeryHighPH_GeneratesAlarm()
    {
        PublishPH(7.5);

        Assert.AreEqual(1, _alarms.Count);
        Assert.AreEqual(DeviceStatus.Alarm,
            _alarms[0].Status);
    }

    // Repeated high readings should not duplicate alerts.
    [TestMethod]
    public void RepeatedHighPH_NoDuplicateAlerts()
    {
        PublishPH(7.5);
        PublishPH(7.6);
        PublishPH(7.7);

        Assert.AreEqual(1, _alarms.Count);
    }

    // Returning to normal should publish a recovery packet.
    [TestMethod]
    public void PHReturnsToNormal_ClearsAlarm()
    {
        PublishPH(7.5);
        PublishPH(6.0);

        Assert.AreEqual(2, _alarms.Count);
        Assert.AreEqual(DeviceStatus.Normal,
            _alarms[1].Status);

        Assert.AreEqual("NORMAL: pH",
            _alarms[1].Message);
    }

    // Verify the alarm contains the correct information.
    [TestMethod]
    public void HighPH_AlarmContainsCorrectInformation()
    {
        PublishPH(7.5);

        Assert.AreEqual(1, _alarms.Count);

        var alarm = _alarms[0];

        Assert.AreEqual("PH-01", alarm.DeviceId);
        Assert.AreEqual("Alarm", alarm.Type);
        Assert.AreEqual(7.5, alarm.Value);
        Assert.AreEqual("pH", alarm.Unit);
        Assert.AreEqual(DeviceStatus.Alarm, alarm.Status);
        Assert.AreEqual("ALARM: pH HIGH",
            alarm.Message);
    }
}
