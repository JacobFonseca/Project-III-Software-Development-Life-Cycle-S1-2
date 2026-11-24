/*
 * File: SqliteTelemetryLoggerIntegrationTests.cs
 * Author: Jacob Fonseca
 * Project: HydroFlow SCADA
 * Task: HF-101
 *
 * Checks that commands and alarms are saved through the logger's one bus subscription,
 * using the real Simulation, SCADAController and CommandInvoker together like the app does.
 */

using HydroFlow.Controller;
using HydroFlow.Core;
using HydroFlow.Devices;
using HydroFlow.Logging;

namespace HydroFlow.Tests;

[TestClass]
public class SqliteTelemetryLoggerIntegrationTests
{
    private string _databasePath = "";

    // Gives every device the same value. 10 puts WL-01, WT-01, PH-01 and EC-01 in Alarm.
    private class FixedSource : IDataSource
    {
        private readonly double _value;
        public FixedSource(double value) => _value = value;
        public double ReadNext() => _value;
    }

    [TestInitialize]
    public void Setup()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), "hydroflow_it_" + Guid.NewGuid().ToString("N") + ".db");
    }

    [TestCleanup]
    public void Cleanup()
    {
        foreach (string suffix in new[] { "", "-journal", "-wal", "-shm" })
        {
            if (File.Exists(_databasePath + suffix))
                File.Delete(_databasePath + suffix);
        }
    }

    // Same order as App.OnStartup: logger first, then the controller, on the simulation's bus.
    private (Simulation simulation, SqliteTelemetryLogger logger) StartLikeApp(double value)
    {
        var simulation = new Simulation(_ => new FixedSource(value));
        var logger = new SqliteTelemetryLogger(_databasePath);
        logger.Attach(simulation.Bus);
        _ = new SCADAController(simulation.Bus);
        return (simulation, logger);
    }

    /*
     * Purpose: Check that a manual command saves the actuator change and one Command row with its message.
     */
    [TestMethod]
    public void CommandRun_OnAppBus_SavesActuatorThenOneCommand()
    {
        var (simulation, logger) = StartLikeApp(1);
        using (logger)
        {
            var led = simulation.Actuators.Single(a => a.DeviceId == "LED-01");
            var invoker = new CommandInvoker(simulation.Bus);

            invoker.Run(new ActuatorCommand(led, true, 80));

            var saved = logger.ReadAll();
            Assert.AreEqual(2, saved.Count);
            Assert.AreEqual("Actuator", saved[0].Type);
            Assert.AreEqual(80, saved[0].Value);
            Assert.AreEqual("%", saved[0].Unit);
            Assert.AreEqual("Command", saved[1].Type);
            Assert.AreEqual("LED-01", saved[1].DeviceId);
            Assert.AreEqual("LED-01 turned ON (setting 80 %)", saved[1].Message);
        }
    }

    /*
     * Purpose: Check that a command the actuator rejects saves nothing.
     */
    [TestMethod]
    public void CommandRun_Rejected_SavesNothing()
    {
        var (simulation, logger) = StartLikeApp(1);
        using (logger)
        {
            var led = simulation.Actuators.Single(a => a.DeviceId == "LED-01");
            var invoker = new CommandInvoker(simulation.Bus);

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => invoker.Run(new ActuatorCommand(led, true, 150)));

            Assert.AreEqual(0, logger.CountRows());
        }
    }

    /*
     * Purpose: Check that one tick saves all 12 readings and an alarm right after each reading that caused one.
     */
    [TestMethod]
    public void Tick_ValuesOutOfRange_SavesReadingsAndAlarmsOnce()
    {
        var (simulation, logger) = StartLikeApp(10);
        using (logger)
        {
            simulation.Tick();

            var saved = logger.ReadAll();
            Assert.AreEqual(12, saved.Count(p => p.Type == "Sensor" || p.Type == "Actuator"));

            var alarmIds = saved.Where(p => p.Type == "Alarm").Select(p => p.DeviceId).OrderBy(id => id).ToList();
            CollectionAssert.AreEqual(new[] { "EC-01", "PH-01", "WL-01", "WT-01" }, alarmIds);

            for (int i = 0; i < saved.Count; i++)
            {
                if (saved[i].Type != "Alarm")
                    continue;

                Assert.AreEqual("Sensor", saved[i - 1].Type);
                Assert.AreEqual(saved[i].DeviceId, saved[i - 1].DeviceId);
                Assert.IsFalse(string.IsNullOrEmpty(saved[i].Message));
            }
        }
    }

    /*
     * Purpose: Check that a second tick with the same values adds readings but no repeat alarms.
     */
    [TestMethod]
    public void Tick_SameStatusTwice_DoesNotSaveAlarmsAgain()
    {
        var (simulation, logger) = StartLikeApp(10);
        using (logger)
        {
            simulation.Tick();
            simulation.Tick();

            var saved = logger.ReadAll();
            Assert.AreEqual(24, saved.Count(p => p.Type == "Sensor" || p.Type == "Actuator"));
            Assert.AreEqual(4, saved.Count(p => p.Type == "Alarm"));
        }
    }

    /*
     * Purpose: Check that commands and alarms are still there, unchanged and in UTC, after the database is reopened.
     */
    [TestMethod]
    public void CommandsAndAlarms_AfterReopen_AreKeptInUtc()
    {
        List<TelemetryPacket> before;
        var (simulation, logger) = StartLikeApp(10);
        using (logger)
        {
            simulation.Tick();
            var fan = simulation.Actuators.Single(a => a.DeviceId == "FAN-01");
            var invoker = new CommandInvoker(simulation.Bus);
            invoker.Run(new ActuatorCommand(fan, true, 1000));
            invoker.Run(new ActuatorCommand(fan, false, 0));
            before = logger.ReadAll().ToList();
        }

        using var reopened = new SqliteTelemetryLogger(_databasePath);
        var after = reopened.ReadAll();

        CollectionAssert.AreEqual(before, after.ToList());
        Assert.AreEqual(2, after.Count(p => p.Type == "Command"));
        Assert.AreEqual(4, after.Count(p => p.Type == "Alarm"));
        Assert.IsTrue(after.All(p => p.Timestamp.Kind == DateTimeKind.Utc));
        Assert.AreEqual("FAN-01 turned OFF (setting 0 rpm)", after.Last().Message);
    }
}
