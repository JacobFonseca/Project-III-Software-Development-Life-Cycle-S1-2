/*
 * File: DashboardReadingsTests.cs
 * Author: Jacob Fonseca
 * Project: HydroFlow SCADA
 * Task: HF-59
 *
 * Tests dashboard rows: module grouping, one row per device, and last-updated time.
 * HF-97: also tests alert status from Alarm packets and the active alert count.
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

    // Alarm packet in the same shape SCADAController publishes.
    private static TelemetryPacket Alarm(string deviceId, DeviceStatus status, double value = 0, string unit = "") =>
        new(deviceId, "Alarm", value, unit, DateTime.UtcNow, status, status.ToString().ToUpper() + ": " + deviceId);

    /*
     * Purpose: Check that every device starts Normal with the header showing "All normal".
     */
    [TestMethod]
    public void NewDashboard_NoAlarms_AllNormal()
    {
        var dashboard = CreateWithSimulationDevices();

        Assert.AreEqual(0, dashboard.ActiveAlertCount);
        Assert.AreEqual("All normal", dashboard.HealthText);
        Assert.IsFalse(dashboard.HasActiveAlerts);
        Assert.IsTrue(dashboard.Rows.All(r => r.AlertStatus == DeviceStatus.Normal));
    }

    /*
     * Purpose: Check the tile status moving Normal to Warning to Alarm and back to Normal.
     */
    [TestMethod]
    public void ApplyAlarm_StatusChanges_TileFollowsAndReturnsToNormal()
    {
        var dashboard = CreateWithSimulationDevices();
        DashboardRow row = dashboard.Find("PH-01")!;

        dashboard.ApplyAlarm(Alarm("PH-01", DeviceStatus.Warning));
        Assert.AreEqual(DeviceStatus.Warning, row.AlertStatus);

        dashboard.ApplyAlarm(Alarm("PH-01", DeviceStatus.Alarm));
        Assert.AreEqual(DeviceStatus.Alarm, row.AlertStatus);

        dashboard.ApplyAlarm(Alarm("PH-01", DeviceStatus.Normal));
        Assert.AreEqual(DeviceStatus.Normal, row.AlertStatus);
        Assert.AreEqual(0, dashboard.ActiveAlertCount);
    }

    /*
     * Purpose: Check that an Alarm packet keeps the latest reading on the tile.
     */
    [TestMethod]
    public void ApplyAlarm_KeepsLatestReading()
    {
        var dashboard = CreateWithSimulationDevices();
        var readingTime = DateTime.UtcNow;
        dashboard.ApplyReading(new TelemetryPacket("WL-01", "Sensor", 25, "%", readingTime, DeviceStatus.Normal));

        dashboard.ApplyAlarm(Alarm("WL-01", DeviceStatus.Alarm, 99, "x"));

        DashboardRow row = dashboard.Find("WL-01")!;
        Assert.AreEqual(25, row.Value);
        Assert.AreEqual("%", row.Unit);
        Assert.AreEqual(readingTime, row.LastUpdated);
        Assert.AreEqual(DeviceStatus.Alarm, row.AlertStatus);
    }

    /*
     * Purpose: Check that a later reading does not clear the alert colour.
     */
    [TestMethod]
    public void ApplyReading_AfterAlarm_KeepsAlertStatus()
    {
        var dashboard = CreateWithSimulationDevices();
        dashboard.ApplyAlarm(Alarm("EC-01", DeviceStatus.Alarm));

        dashboard.ApplyReading(new TelemetryPacket("EC-01", "Sensor", 3.5, "mS/cm", DateTime.UtcNow, DeviceStatus.Normal));

        Assert.AreEqual(DeviceStatus.Alarm, dashboard.Find("EC-01")!.AlertStatus);
        Assert.AreEqual(1, dashboard.ActiveAlertCount);
    }

    /*
     * Purpose: Check that the count is devices with an active alert, and that Warning counts too.
     */
    [TestMethod]
    public void ApplyAlarm_WarningAndAlarmOnTwoDevices_CountsTwo()
    {
        var dashboard = CreateWithSimulationDevices();

        dashboard.ApplyAlarm(Alarm("HUM-01", DeviceStatus.Warning));
        dashboard.ApplyAlarm(Alarm("WT-01", DeviceStatus.Alarm));

        Assert.AreEqual(2, dashboard.ActiveAlertCount);
        Assert.AreEqual("2 alerts", dashboard.HealthText);
        Assert.IsTrue(dashboard.HasActiveAlerts);
    }

    /*
     * Purpose: Check that repeated Alarm packets for one device count once.
     */
    [TestMethod]
    public void ApplyAlarm_RepeatedForSameDevice_CountsOnce()
    {
        var dashboard = CreateWithSimulationDevices();

        dashboard.ApplyAlarm(Alarm("AIR-01", DeviceStatus.Alarm));
        dashboard.ApplyAlarm(Alarm("AIR-01", DeviceStatus.Alarm));
        dashboard.ApplyAlarm(Alarm("AIR-01", DeviceStatus.Warning));

        Assert.AreEqual(1, dashboard.ActiveAlertCount);
        Assert.AreEqual("1 alert", dashboard.HealthText);
    }

    /*
     * Purpose: Check that one device returning to Normal clears only its own alert.
     */
    [TestMethod]
    public void ApplyAlarm_OneDeviceBackToNormal_OtherAlertStays()
    {
        var dashboard = CreateWithSimulationDevices();
        dashboard.ApplyAlarm(Alarm("PH-01", DeviceStatus.Alarm));
        dashboard.ApplyAlarm(Alarm("WL-01", DeviceStatus.Alarm));

        dashboard.ApplyAlarm(Alarm("PH-01", DeviceStatus.Normal));

        Assert.AreEqual(1, dashboard.ActiveAlertCount);
        Assert.AreEqual(DeviceStatus.Alarm, dashboard.Find("WL-01")!.AlertStatus);
    }

    /*
     * Purpose: Check that only Alarm packets change the alert status.
     */
    [TestMethod]
    [DataRow("Sensor")]
    [DataRow("Actuator")]
    [DataRow("Command")]
    public void ApplyAlarm_OtherPacketType_IsIgnored(string type)
    {
        var dashboard = CreateWithSimulationDevices();

        bool applied = dashboard.ApplyAlarm(
            new TelemetryPacket("FAN-01", type, 0, "rpm", DateTime.UtcNow, DeviceStatus.Alarm));

        Assert.IsFalse(applied);
        Assert.AreEqual(DeviceStatus.Normal, dashboard.Find("FAN-01")!.AlertStatus);
        Assert.AreEqual(0, dashboard.ActiveAlertCount);
    }

    /*
     * Purpose: Check that the header text raises PropertyChanged when the count changes.
     */
    [TestMethod]
    public void ApplyAlarm_CountChanges_RaisesHealthTextChanged()
    {
        var dashboard = CreateWithSimulationDevices();
        var changed = new List<string?>();
        dashboard.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        dashboard.ApplyAlarm(Alarm("PAR-01", DeviceStatus.Warning));

        CollectionAssert.Contains(changed, nameof(DashboardReadings.ActiveAlertCount));
        CollectionAssert.Contains(changed, nameof(DashboardReadings.HealthText));
    }
}
