/*
 * File: DashboardIntegrationTests.cs
 * Author: Jacob Fonseca
 * Project: HydroFlow SCADA
 * Task: HF-69
 *
 * Runs the real Simulation, SCADAController and CommandInvoker on one bus and checks the dashboard,
 * wired the same way MainWindow does it: one handler for readings and one for alarms.
 */

using HydroFlow.Controller;
using HydroFlow.Controller.Dashboard;
using HydroFlow.Core;
using HydroFlow.Devices;

namespace HydroFlow.Tests;

[TestClass]
public class DashboardIntegrationTests
{
    // Every device reads the current Value. Tests change it between ticks.
    private class AdjustableSource : IDataSource
    {
        public double Value { get; set; }
        public double ReadNext() => Value;
    }

    private AdjustableSource _source = new();
    private Simulation _simulation = null!;
    private DashboardReadings _dashboard = null!;

    [TestInitialize]
    public void Setup()
    {
        _source = new AdjustableSource { Value = 1 };
        _simulation = new Simulation(_ => _source);
        _ = new SCADAController(_simulation.Bus);

        _dashboard = new DashboardReadings();
        _dashboard.AddDevices(_simulation.Sensors.Cast<Device>().Concat(_simulation.Actuators));

        // Same as MainWindow.ShowReadingsFrom: the reading handler skips Alarm and Command packets,
        // and alarms reach the tiles through their own handler.
        _simulation.Bus.Subscribe(p => _dashboard.ApplyReading(p));
        _simulation.Bus.Subscribe(p => _dashboard.ApplyAlarm(p));
    }

    /*
     * Purpose: Check that one tick fills in all 12 devices without adding rows.
     */
    [TestMethod]
    public void Tick_Once_UpdatesAllTwelveRows()
    {
        _simulation.Tick();

        Assert.AreEqual(12, _dashboard.Rows.Count);
        Assert.IsTrue(_dashboard.Rows.All(r => r.LastUpdated != null));
        Assert.AreEqual(7, _dashboard.Rows.Count(r => r.Value == 1));
        Assert.AreEqual(5, _dashboard.Rows.Count(r => r.Value == 0), "Actuators are off, so they read 0.");
    }

    /*
     * Purpose: Check that repeated ticks keep one row per device and move last-updated forward.
     */
    [TestMethod]
    public void Tick_Repeated_KeepsOneRowPerDeviceAndAdvancesTime()
    {
        _simulation.Tick();
        DateTime first = _dashboard.Find("PAR-01")!.LastUpdated!.Value;
        Thread.Sleep(20);

        _simulation.Tick();
        _simulation.Tick();

        Assert.AreEqual(12, _dashboard.Rows.Count);
        Assert.AreEqual(12, _dashboard.Rows.Select(r => r.DeviceId).Distinct().Count());
        Assert.IsTrue(_dashboard.Find("PAR-01")!.LastUpdated > first);
    }

    /*
     * Purpose: Check that out-of-range readings turn the right tiles to Alarm and keep the readings.
     * At 10, WL-01, WT-01, PH-01 and EC-01 are in Alarm under HF-100's thresholds.
     */
    [TestMethod]
    public void Tick_ValuesOutOfRange_TilesShowAlarmAndKeepReading()
    {
        _source.Value = 10;

        _simulation.Tick();

        Assert.AreEqual(4, _dashboard.ActiveAlertCount);
        Assert.AreEqual("4 alerts", _dashboard.HealthText);
        foreach (string id in new[] { "WL-01", "WT-01", "PH-01", "EC-01" })
        {
            DashboardRow row = _dashboard.Find(id)!;
            Assert.AreEqual(DeviceStatus.Alarm, row.AlertStatus, id);
            Assert.AreEqual(10, row.Value, id);
        }
    }

    /*
     * Purpose: Check that alerts clear when readings go back in range, and that holding a bad value
     * for several ticks does not raise the count.
     */
    [TestMethod]
    public void Tick_BackInRange_ClearsAlerts()
    {
        _source.Value = 10;
        _simulation.Tick();
        _simulation.Tick();
        _simulation.Tick();
        Assert.AreEqual(4, _dashboard.ActiveAlertCount);

        // At 6 only pH is back in range. Water level, water temperature and EC are still out.
        _source.Value = 6;
        _simulation.Tick();

        Assert.AreEqual(DeviceStatus.Normal, _dashboard.Find("PH-01")!.AlertStatus);
        Assert.AreEqual(3, _dashboard.ActiveAlertCount);
        Assert.AreEqual("3 alerts", _dashboard.HealthText);
    }

    /*
     * Purpose: Check that a manual command updates the actuator's reading,
     * and that its Command packet does not overwrite that reading.
     */
    [TestMethod]
    public void CommandRun_UpdatesActuatorAndCommandPacketDoesNotOverwrite()
    {
        _simulation.Tick();
        var fan = _simulation.Actuators.Single(a => a.DeviceId == "FAN-01");
        var invoker = new CommandInvoker(_simulation.Bus);

        invoker.Run(new ActuatorCommand(fan, true, 1000));

        DashboardRow row = _dashboard.Find("FAN-01")!;
        Assert.AreEqual(1000, row.Value);
        Assert.AreEqual("rpm", row.Unit);
        Assert.AreEqual(DeviceStatus.Normal, row.AlertStatus);
        Assert.AreEqual(12, _dashboard.Rows.Count);
    }

    /*
     * Purpose: Check that rows stay grouped by module after live updates.
     */
    [TestMethod]
    public void Tick_AfterUpdates_RowsStayInModuleOrder()
    {
        _simulation.Tick();

        var order = _dashboard.Rows.Select(r => DeviceModules.OrderOf(r.Module)).ToList();
        CollectionAssert.AreEqual(order.OrderBy(o => o).ToList(), order);
    }
}
