/*
 * File: DeviceModulesTests.cs
 * Author: Jacob Fonseca
 * Project: HydroFlow SCADA
 * Task: HF-59
 *
 * Tests that every simulated device is put in the right dashboard module.
 */

using HydroFlow.Controller.Dashboard;
using HydroFlow.Core;
using HydroFlow.Devices;

namespace HydroFlow.Tests;

[TestClass]
public class DeviceModulesTests
{
    // Returns the same value every time, so no data files are needed.
    private class FakeSource : IDataSource
    {
        public double ReadNext() => 1;
    }

    /*
     * Purpose: Check the module of each of the 12 devices the simulation registers.
     */
    [TestMethod]
    [DataRow("AIR-01", "Climate")]
    [DataRow("HUM-01", "Climate")]
    [DataRow("FAN-01", "Climate")]
    [DataRow("WL-01", "Water")]
    [DataRow("WT-01", "Water")]
    [DataRow("VLV-01", "Water")]
    [DataRow("PH-01", "Nutrients")]
    [DataRow("PHP-01", "Nutrients")]
    [DataRow("EC-01", "Nutrients")]
    [DataRow("NUT-01", "Nutrients")]
    [DataRow("PAR-01", "Lighting")]
    [DataRow("LED-01", "Lighting")]
    public void For_SimulationDevice_ReturnsItsModule(string deviceId, string expected)
    {
        var simulation = new Simulation(_ => new FakeSource());
        Device device = simulation.Sensors.Cast<Device>().Concat(simulation.Actuators)
            .Single(d => d.DeviceId == deviceId);

        Assert.AreEqual(expected, DeviceModules.For(device));
    }

    /*
     * Purpose: Check that no simulated device falls into Other.
     */
    [TestMethod]
    public void For_AllSimulationDevices_NoneAreOther()
    {
        var simulation = new Simulation(_ => new FakeSource());

        foreach (Device device in simulation.Sensors.Cast<Device>().Concat(simulation.Actuators))
            Assert.AreNotEqual(DeviceModules.Other, DeviceModules.For(device), device.DeviceId);
    }

    /*
     * Purpose: Check that a null device is rejected.
     */
    [TestMethod]
    public void For_NullDevice_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(() => DeviceModules.For(null!));
    }

    /*
     * Purpose: Check that an unknown module name sorts after all the known ones.
     */
    [TestMethod]
    public void OrderOf_UnknownModule_SortsLast()
    {
        Assert.AreEqual(DeviceModules.DisplayOrder.Count, DeviceModules.OrderOf("Something else"));
        Assert.IsTrue(DeviceModules.OrderOf(DeviceModules.Climate) < DeviceModules.OrderOf(DeviceModules.Lighting));
    }
}
