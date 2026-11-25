/*
 * File: SqliteTelemetryLoggerTests.cs
 * Author: Jacob Fonseca
 * Project: HydroFlow SCADA
 * Task: HF-57
 *
 * Tests that the SQLite logger saves every packet type, keeps rows after a restart,
 * and subscribes to the bus only once.
 * HF-63: also tests the bounded reads used by the diagnostics view.
 */

using HydroFlow.Controller;
using HydroFlow.Core;
using HydroFlow.Logging;
using Microsoft.Data.Sqlite;

namespace HydroFlow.Tests;

[TestClass]
public class SqliteTelemetryLoggerTests
{
    // Each test gets its own file because tests run in parallel.
    private string _databasePath = "";

    [TestInitialize]
    public void Setup()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), "hydroflow_test_" + Guid.NewGuid().ToString("N") + ".db");
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

    // Counts rows with a separate connection, so the check does not depend on the logger's own reader.
    private int CountRows(string? whereType = null)
    {
        using var connection = new SqliteConnection(
            new SqliteConnectionStringBuilder { DataSource = _databasePath, Pooling = false }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = whereType == null
            ? "SELECT COUNT(*) FROM Telemetry;"
            : "SELECT COUNT(*) FROM Telemetry WHERE Type = $type;";
        if (whereType != null)
            command.Parameters.AddWithValue("$type", whereType);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    /*
     * Purpose: Check that creating the logger makes the database file and an empty table.
     */
    [TestMethod]
    public void Constructor_NewPath_CreatesDatabaseWithEmptyTable()
    {
        using (var logger = new SqliteTelemetryLogger(_databasePath))
        {
            Assert.IsTrue(File.Exists(_databasePath));
            Assert.AreEqual(0, logger.ReadAll().Count);
        }

        Assert.AreEqual(0, CountRows());
    }

    /*
     * Purpose: Check that a blank path is rejected.
     */
    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void Constructor_BlankPath_ThrowsArgumentException(string path)
    {
        Assert.ThrowsException<ArgumentException>(() => new SqliteTelemetryLogger(path));
    }

    /*
     * Purpose: Check that the default path is hydroflow.db beside the executable.
     */
    [TestMethod]
    public void GetDefaultPath_IsHydroflowDbBesideExecutable()
    {
        string expected = Path.Combine(AppContext.BaseDirectory, "hydroflow.db");

        Assert.AreEqual(expected, SqliteTelemetryLogger.GetDefaultPath());
    }

    /*
     * Purpose: Check that all four packet types are saved with every field, including the message.
     */
    [TestMethod]
    public void Log_EveryPacketType_SavesAllFields()
    {
        var time = new DateTime(2026, 11, 24, 5, 30, 15, 123, DateTimeKind.Utc);
        var packets = new[]
        {
            new TelemetryPacket("PH-01", "Sensor", 6.1, "pH", time, DeviceStatus.Normal),
            new TelemetryPacket("FAN-01", "Actuator", 1250, "rpm", time, DeviceStatus.Normal),
            new TelemetryPacket("WL-01", "Alarm", 25, "%", time, DeviceStatus.Alarm, "ALARM: Water Level LOW"),
            new TelemetryPacket("LED-01", "Command", 0, "", time, DeviceStatus.Normal, "LED-01 turned ON (setting 80 %)")
        };

        using var logger = new SqliteTelemetryLogger(_databasePath);
        foreach (var packet in packets)
            logger.Log(packet);

        var saved = logger.ReadAll();

        CollectionAssert.AreEqual(packets, saved.ToArray());
        Assert.AreEqual(1, CountRows("Sensor"));
        Assert.AreEqual(1, CountRows("Actuator"));
        Assert.AreEqual(1, CountRows("Alarm"));
        Assert.AreEqual(1, CountRows("Command"));
    }

    /*
     * Purpose: Check that timestamps are stored as UTC, including a local time passed in.
     */
    [TestMethod]
    public void Log_LocalTimestamp_IsStoredAsUtc()
    {
        var local = new DateTime(2026, 11, 24, 0, 19, 0, DateTimeKind.Local);

        using var logger = new SqliteTelemetryLogger(_databasePath);
        logger.Log(new TelemetryPacket("AIR-01", "Sensor", 22.5, "°C", local, DeviceStatus.Normal));

        DateTime saved = logger.ReadAll()[0].Timestamp;

        Assert.AreEqual(DateTimeKind.Utc, saved.Kind);
        Assert.AreEqual(local.ToUniversalTime(), saved);
    }

    /*
     * Purpose: Check that a null packet is rejected, as the logger interface requires.
     */
    [TestMethod]
    public void Log_NullPacket_ThrowsArgumentNullException()
    {
        using var logger = new SqliteTelemetryLogger(_databasePath);

        Assert.ThrowsException<ArgumentNullException>(() => logger.Log(null!));
    }

    /*
     * Purpose: Check that rows from an earlier run are still there after the app restarts,
     * and that new rows are added after them.
     */
    [TestMethod]
    public void Restart_SamePath_KeepsEarlierRowsAndAppends()
    {
        var first = new TelemetryPacket("WT-01", "Sensor", 22, "°C", DateTime.UtcNow, DeviceStatus.Normal);
        var second = new TelemetryPacket("VLV-01", "Actuator", 1, "state", DateTime.UtcNow, DeviceStatus.Normal);

        using (var firstRun = new SqliteTelemetryLogger(_databasePath))
            firstRun.Log(first);

        using (var secondRun = new SqliteTelemetryLogger(_databasePath))
        {
            secondRun.Log(second);

            var saved = secondRun.ReadAll();
            Assert.AreEqual(2, saved.Count);
            Assert.AreEqual(first, saved[0]);
            Assert.AreEqual(second, saved[1]);
        }

        Assert.AreEqual(2, CountRows());
    }

    /*
     * Purpose: Check that packets published on the bus are saved after Attach.
     */
    [TestMethod]
    public void Attach_PublishedPacket_IsSaved()
    {
        var bus = new MessageBus();
        using var logger = new SqliteTelemetryLogger(_databasePath);
        logger.Attach(bus);

        bus.Publish(new TelemetryPacket("EC-01", "Sensor", 1.6, "mS/cm", DateTime.UtcNow, DeviceStatus.Normal));

        Assert.AreEqual(1, CountRows());
    }

    /*
     * Purpose: Check that calling Attach twice still saves each packet only once.
     */
    [TestMethod]
    public void Attach_CalledTwice_SavesEachPacketOnce()
    {
        var bus = new MessageBus();
        using var logger = new SqliteTelemetryLogger(_databasePath);
        logger.Attach(bus);
        logger.Attach(bus);

        bus.Publish(new TelemetryPacket("HUM-01", "Sensor", 66, "%", DateTime.UtcNow, DeviceStatus.Normal));

        Assert.AreEqual(1, CountRows());
    }

    /*
     * Purpose: Check the app's setup. With the controller on the same bus, a reading out of range
     * saves the reading and then the alarm, and nothing is saved twice.
     */
    [TestMethod]
    public void Attach_WithSCADAController_SavesReadingThenAlarmOnce()
    {
        var bus = new MessageBus();
        using var logger = new SqliteTelemetryLogger(_databasePath);
        logger.Attach(bus);
        _ = new SCADAController(bus);

        bus.Publish(new TelemetryPacket("WL-01", "Sensor", 20, "%", DateTime.UtcNow, DeviceStatus.Normal));

        var saved = logger.ReadAll();
        Assert.AreEqual(2, saved.Count);
        Assert.AreEqual("Sensor", saved[0].Type);
        Assert.AreEqual("Alarm", saved[1].Type);
        Assert.AreEqual("WL-01", saved[1].DeviceId);
        Assert.AreEqual(DeviceStatus.Alarm, saved[1].Status);
        Assert.IsFalse(string.IsNullOrEmpty(saved[1].Message));
    }

    /*
     * Purpose: Check that packets that arrive after Dispose are ignored instead of throwing.
     */
    [TestMethod]
    public void Log_AfterDispose_IsIgnored()
    {
        var logger = new SqliteTelemetryLogger(_databasePath);
        logger.Dispose();

        logger.Log(new TelemetryPacket("PAR-01", "Sensor", 515, "lux", DateTime.UtcNow, DeviceStatus.Normal));

        Assert.AreEqual(0, CountRows());
    }

    /*
     * Purpose: Check that LoggedAt is written as UTC and is separate from the packet's own time.
     */
    [TestMethod]
    public void Log_SavesLoggedAtAsUtcSeparateFromPacketTime()
    {
        var packetTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime before = DateTime.UtcNow;

        using (var logger = new SqliteTelemetryLogger(_databasePath))
            logger.Log(new TelemetryPacket("PH-01", "Sensor", 6.0, "pH", packetTime, DeviceStatus.Normal));

        using var connection = new SqliteConnection(
            new SqliteConnectionStringBuilder { DataSource = _databasePath, Pooling = false }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Timestamp, LoggedAt FROM Telemetry;";
        using var reader = command.ExecuteReader();
        Assert.IsTrue(reader.Read());

        Assert.AreEqual("2026-01-01T00:00:00.0000000Z", reader.GetString(0));
        StringAssert.EndsWith(reader.GetString(1), "Z");
        DateTime loggedAt = DateTime.Parse(reader.GetString(1), null, System.Globalization.DateTimeStyles.RoundtripKind);
        Assert.IsTrue(loggedAt >= before.AddSeconds(-1));
    }

    /*
     * Purpose: Check that a database error is counted and reported instead of thrown,
     * so the simulation tick keeps running.
     */
    [TestMethod]
    public void Log_DatabaseError_ReportsFailureWithoutThrowing()
    {
        using var logger = new SqliteTelemetryLogger(_databasePath);
        Exception? reported = null;
        int reportCount = 0;
        logger.LogFailed += ex => { reported = ex; reportCount++; };

        // Remove the table from outside the logger so the next insert fails.
        using (var connection = new SqliteConnection(
            new SqliteConnectionStringBuilder { DataSource = _databasePath, Pooling = false }.ToString()))
        {
            connection.Open();
            using var drop = connection.CreateCommand();
            drop.CommandText = "DROP TABLE Telemetry;";
            drop.ExecuteNonQuery();
        }

        logger.Log(new TelemetryPacket("NUT-01", "Actuator", 50, "mL/min", DateTime.UtcNow, DeviceStatus.Normal));

        Assert.AreEqual(1, logger.FailedCount);
        Assert.AreEqual(1, reportCount);
        Assert.IsInstanceOfType(reported, typeof(SqliteException));
        Assert.AreSame(reported, logger.LastError);
    }

    /*
     * Purpose: Check that a database from before LoggedAt existed keeps its rows and gets the new column.
     */
    [TestMethod]
    public void Constructor_OldDatabaseWithoutLoggedAt_KeepsRowsAndAddsColumn()
    {
        using (var connection = new SqliteConnection(
            new SqliteConnectionStringBuilder { DataSource = _databasePath, Pooling = false }.ToString()))
        {
            connection.Open();
            using var create = connection.CreateCommand();
            create.CommandText =
                @"CREATE TABLE Telemetry (Id INTEGER PRIMARY KEY AUTOINCREMENT, DeviceId TEXT NOT NULL, Type TEXT NOT NULL,
                    Value REAL NOT NULL, Unit TEXT NOT NULL, Timestamp TEXT NOT NULL, Status TEXT NOT NULL,
                    Message TEXT NOT NULL DEFAULT '');
                  INSERT INTO Telemetry (DeviceId, Type, Value, Unit, Timestamp, Status, Message)
                  VALUES ('WL-01', 'Sensor', 50, '%', '2026-11-24T05:29:28.0000000Z', 'Normal', '');";
            create.ExecuteNonQuery();
        }

        using var logger = new SqliteTelemetryLogger(_databasePath);
        logger.Log(new TelemetryPacket("WL-01", "Sensor", 45, "%", DateTime.UtcNow, DeviceStatus.Normal));

        var saved = logger.ReadAll();
        Assert.AreEqual(2, saved.Count);
        Assert.AreEqual(50, saved[0].Value);
        Assert.AreEqual(0, logger.FailedCount);
    }

    // Saves count packets, cycling through the four packet types.
    private static void SaveMixedPackets(SqliteTelemetryLogger logger, int count)
    {
        string[] types = { "Sensor", "Actuator", "Alarm", "Command" };
        for (int i = 0; i < count; i++)
            logger.Log(new TelemetryPacket("DEV-" + i, types[i % 4], i, "u", DateTime.UtcNow, DeviceStatus.Normal, "msg " + i));
    }

    /*
     * Purpose: Check that ReadRecent returns the newest rows first and no more than the limit.
     */
    [TestMethod]
    public void ReadRecent_MoreRowsThanLimit_ReturnsNewestFirst()
    {
        using var logger = new SqliteTelemetryLogger(_databasePath);
        SaveMixedPackets(logger, 20);

        var recent = logger.ReadRecent(5);

        Assert.AreEqual(5, recent.Count);
        Assert.AreEqual("DEV-19", recent[0].DeviceId);
        Assert.AreEqual("DEV-15", recent[4].DeviceId);
    }

    /*
     * Purpose: Check that ReadRecent can return only the packet types asked for.
     */
    [TestMethod]
    public void ReadRecent_WithTypes_ReturnsOnlyThoseTypes()
    {
        using var logger = new SqliteTelemetryLogger(_databasePath);
        SaveMixedPackets(logger, 20);

        var recent = logger.ReadRecent(50, "Alarm", "Command");

        Assert.AreEqual(10, recent.Count);
        Assert.IsTrue(recent.All(p => p.Type == "Alarm" || p.Type == "Command"));
        Assert.AreEqual("msg 19", recent[0].Message);
    }

    /*
     * Purpose: Check that a limit above MaxRecentRows is capped.
     */
    [TestMethod]
    public void ReadRecent_LimitAboveMax_IsCapped()
    {
        using var logger = new SqliteTelemetryLogger(_databasePath);
        SaveMixedPackets(logger, SqliteTelemetryLogger.MaxRecentRows + 10);

        var recent = logger.ReadRecent(int.MaxValue);

        Assert.AreEqual(SqliteTelemetryLogger.MaxRecentRows, recent.Count);
    }

    /*
     * Purpose: Check that a limit below 1 is rejected.
     */
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void ReadRecent_LimitBelowOne_ThrowsArgumentOutOfRangeException(int limit)
    {
        using var logger = new SqliteTelemetryLogger(_databasePath);

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => logger.ReadRecent(limit));
    }

    /*
     * Purpose: Check that a type name is treated as a value, not as part of the SQL.
     */
    [TestMethod]
    public void ReadRecent_TypeWithQuote_ReturnsNothingAndKeepsTable()
    {
        using var logger = new SqliteTelemetryLogger(_databasePath);
        SaveMixedPackets(logger, 4);

        var recent = logger.ReadRecent(10, "Alarm'; DROP TABLE Telemetry; --");

        Assert.AreEqual(0, recent.Count);
        Assert.AreEqual(4, logger.CountRows());
    }

    /*
     * Purpose: Check that CountRows matches the number of saved packets.
     */
    [TestMethod]
    public void CountRows_AfterSaving_MatchesSaved()
    {
        using var logger = new SqliteTelemetryLogger(_databasePath);
        SaveMixedPackets(logger, 7);

        Assert.AreEqual(7, logger.CountRows());
    }
}
