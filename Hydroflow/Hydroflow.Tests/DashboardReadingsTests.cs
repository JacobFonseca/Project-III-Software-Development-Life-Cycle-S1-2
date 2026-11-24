/*
 * File: DashboardReadingsTests.cs
 * Author: Jacob Fonseca
 * Project: HydroFlow SCADA
 * Task: HF-59
 *
 * Tests dashboard rows: module grouping, one row per device, and last-updated time.
 */

using HydroFlow.Controller.Dashboard;
using HydroFlow.Core;
using HydroFlow.Devices;

namespace HydroFlow.Tests;

[TestClass]
public class DashboardReadingsTests
{
    private class FakeSource : IDataSource
    {
        public double ReadNext() => 1;
    }

    // Dashboard filled with the simulation's 12 devices, like MainWindow.Start does.
    private static DashboardReadings CreateWithSimulationDevices()
    {
        var simulation = new Simulation(_ => new FakeSource());
        var dashboard = new DashboardReadings();
        dashboard.AddDevices(simulation.Sensors.Cast<Device>().Concat(simulation.Actuators));
        return dashboard;
    }

    /*
     * Purpose: Check that all 12 devices are listed before any reading arrives.
     */
    [TestMethod]
    public void AddDevices_Simulation_ListsAllTwelveWaiting()
    {
        var dashboard = CreateWithSimulationDevices();

        Assert.AreEqual(12, dashboard.Rows.Count);
        Assert.IsTrue(dashboard.Rows.All(r => r.Value == null && r.LastUpdated == null));
    }

    /*
     * Purpose: Check that rows are sorted Climate, Water, Nutrients, Lighting, with the right counts.
     */
    [TestMethod]
    public void AddDevices_Simulation_RowsSortedByModule()
    {
        var dashboard = CreateWithSimulationDevices();

        var modules = dashboard.Rows.Select(r => r.Module).ToList();
        var expected = new[] { "Climate", "Climate", "Climate", "Water", "Water", "Water",
                               "Nutrients", "Nutrients", "Nutrients", "Nutrients", "Lighting", "Lighting" };
        CollectionAssert.AreEqual(expected, modules);
    }

    /*
     * Purpose: Check that adding the same devices twice does not list them twice.
     */
    [TestMethod]
    public void AddDevices_CalledTwice_NoDuplicateRows()
    {
        var simulation = new Simulation(_ => new FakeSource());
        var dashboard = new DashboardReadings();
        var devices = simulation.Sensors.Cast<Device>().Concat(simulation.Actuators).ToList();

        dashboard.AddDevices(devices);
        dashboard.AddDevices(devices);

        Assert.AreEqual(12, dashboard.Rows.Count);
    }

    /*
     * Purpose: Check that a reading fills in value, unit, status and last-updated time.
     */
    [TestMethod]
    public void ApplyReading_Sensor_UpdatesRow()
    {
        var dashboard = CreateWithSimulationDevices();
        var time = new DateTime(2026, 11, 24, 5, 0, 0, DateTimeKind.Utc);

        bool applied = dashboard.ApplyReading(new TelemetryPacket("PH-01", "Sensor", 6.2, "pH", time, DeviceStatus.Normal));

        DashboardRow row = dashboard.Find("PH-01")!;
        Assert.IsTrue(applied);
        Assert.AreEqual(6.2, row.Value);
        Assert.AreEqual("pH", row.Unit);
        Assert.AreEqual(time, row.LastUpdated);
        Assert.AreEqual(time.ToLocalTime(), row.LastUpdatedLocal);
    }

    /*
     * Purpose: Check that repeated readings update the same row instead of adding rows.
     */
    [TestMethod]
    public void ApplyReading_Repeated_UpdatesExistingRow()
    {
        var dashboard = CreateWithSimulationDevices();
        var first = DateTime.UtcNow;
        var second = first.AddSeconds(1);

        dashboard.ApplyReading(new TelemetryPacket("FAN-01", "Actuator", 0, "rpm", first, DeviceStatus.Normal));
        dashboard.ApplyReading(new TelemetryPacket("FAN-01", "Actuator", 1250, "rpm", second, DeviceStatus.Normal));

        Assert.AreEqual(12, dashboard.Rows.Count);
        Assert.AreEqual(1, dashboard.Rows.Count(r => r.DeviceId == "FAN-01"));
        Assert.AreEqual(1250, dashboard.Find("FAN-01")!.Value);
        Assert.AreEqual(second, dashboard.Find("FAN-01")!.LastUpdated);
    }

    /*
     * Purpose: Check that Alarm and Command packets do not overwrite a reading or add a row.
     */
    [TestMethod]
    [DataRow("Alarm")]
    [DataRow("Command")]
    public void ApplyReading_AlarmOrCommand_IsIgnored(string type)
    {
        var dashboard = CreateWithSimulationDevices();
        var readingTime = DateTime.UtcNow;
        dashboard.ApplyReading(new TelemetryPacket("LED-01", "Actuator", 80, "%", readingTime, DeviceStatus.Normal));

        bool applied = dashboard.ApplyReading(
            new TelemetryPacket("LED-01", type, 0, "", readingTime.AddSeconds(1), DeviceStatus.Alarm, "message"));

        DashboardRow row = dashboard.Find("LED-01")!;
        Assert.IsFalse(applied);
        Assert.AreEqual(12, dashboard.Rows.Count);
        Assert.AreEqual(80, row.Value);
        Assert.AreEqual("%", row.Unit);
        Assert.AreEqual(DeviceStatus.Normal, row.Status);
        Assert.AreEqual(readingTime, row.LastUpdated);
    }

    /*
     * Purpose: Check that a reading from an unregistered device is still shown, under Other.
     */
    [TestMethod]
    public void ApplyReading_UnknownDevice_AddsRowUnderOther()
    {
        var dashboard = CreateWithSimulationDevices();

        dashboard.ApplyReading(new TelemetryPacket("CO2-01", "Sensor", 800, "ppm", DateTime.UtcNow, DeviceStatus.Normal));

        Assert.AreEqual(13, dashboard.Rows.Count);
        Assert.AreEqual(DeviceModules.Other, dashboard.Rows.Last().Module);
    }

    /*
     * Purpose: Check that a changed reading raises PropertyChanged so the grid refreshes.
     */
    [TestMethod]
    public void ApplyReading_ValueChanges_RaisesPropertyChanged()
    {
        var dashboard = CreateWithSimulationDevices();
        DashboardRow row = dashboard.Find("WL-01")!;
        var changed = new List<string?>();
        row.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        dashboard.ApplyReading(new TelemetryPacket("WL-01", "Sensor", 45, "%", DateTime.UtcNow, DeviceStatus.Normal));

        CollectionAssert.Contains(changed, nameof(DashboardRow.Value));
        CollectionAssert.Contains(changed, nameof(DashboardRow.LastUpdated));
        CollectionAssert.Contains(changed, nameof(DashboardRow.LastUpdatedLocal));
    }

    /*
     * Purpose: Check that a null packet is rejected.
     */
    [TestMethod]
    public void ApplyReading_NullPacket_ThrowsArgumentNullException()
    {
        var dashboard = new DashboardReadings();

        Assert.ThrowsException<ArgumentNullException>(() => dashboard.ApplyReading(null!));
    }
}
