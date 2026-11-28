
/*
 * File: SQLiteDatabaseOperationTests.cs
 * Author: Kadeema Wakha
 * Project: HydroFlow SCADA
 * Task: HF-60
 *
 * Additional unit tests for SQLite database operations,
 * error handling and concurrent logging.
 */

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HydroFlow.Core;
using HydroFlow.Logging;
using Microsoft.Data.Sqlite;

namespace HydroFlow.Tests;

[TestClass]
public class SQLiteDatabaseOperationTests
{
    private string _databasePath = "";

    [TestInitialize]
    public void Setup()
    {
        _databasePath = Path.Combine(
            Path.GetTempPath(),
            "hydroflow_hf60_" +
            Guid.NewGuid().ToString("N") + ".db");
    }

    [TestCleanup]
    public void Cleanup()
    {
        foreach (string suffix in new[]
        {
            "", "-journal", "-wal", "-shm"
        })
        {
            if (File.Exists(_databasePath + suffix))
                File.Delete(_databasePath + suffix);
        }
    }

    private TelemetryPacket CreatePacket(double value = 6.2)
    {
        return new TelemetryPacket(
            "PH-01",
            "Sensor",
            value,
            "pH",
            DateTime.UtcNow,
            DeviceStatus.Normal);
    }

    private void ExecuteSql(string sql)
    {
        using var connection = new SqliteConnection(
            new SqliteConnectionStringBuilder
            {
                DataSource = _databasePath,
                Pooling = false
            }.ToString());

        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    // Verify that messages containing special characters
    // are stored without being changed.
    [TestMethod]
    public void Log_SpecialCharacters_PreservesMessage()
    {
        using var logger =
            new SqliteTelemetryLogger(_databasePath);

        var packet = CreatePacket() with
        {
            Message = "Sensor's reading: \"6.2\"; pH"
        };

        logger.Log(packet);

        var saved = logger.ReadAll();

        Assert.AreEqual(1, saved.Count);
        Assert.AreEqual(packet.Message, saved[0].Message);
        Assert.AreEqual(0, logger.FailedCount);
    }

    // Verify that readings from the same device
    // are stored separately instead of being overwritten.
    [TestMethod]
    public void Log_SameDevice_PreservesReadingHistory()
    {
        using var logger =
            new SqliteTelemetryLogger(_databasePath);

        logger.Log(CreatePacket(6.0));
        logger.Log(CreatePacket(6.5));
        logger.Log(CreatePacket(7.2));

        var saved = logger.ReadAll();

        Assert.AreEqual(3, saved.Count);
        Assert.AreEqual(6.0, saved[0].Value);
        Assert.AreEqual(6.5, saved[1].Value);
        Assert.AreEqual(7.2, saved[2].Value);

        Assert.IsTrue(saved.All(
            p => p.DeviceId == "PH-01"));
    }

    // Verify that simultaneous writes do not lose readings.
    [TestMethod]
    public void Log_ConcurrentWrites_SavesEveryPacket()
    {
        using var logger =
            new SqliteTelemetryLogger(_databasePath);

        Parallel.For(0, 50, i =>
        {
            logger.Log(CreatePacket(i));
        });

        var saved = logger.ReadAll();

        Assert.AreEqual(50, saved.Count);
        Assert.AreEqual(50,
            saved.Select(p => p.Value).Distinct().Count());

        Assert.AreEqual(0, logger.FailedCount);
    }

    // Verify that every unsuccessful insert
    // increases the failure counter.
    [TestMethod]
    public void Log_MultipleFailures_IncreasesFailureCount()
    {
        using var logger =
            new SqliteTelemetryLogger(_databasePath);

        int reportedFailures = 0;

        logger.LogFailed += _ => reportedFailures++;

        ExecuteSql("DROP TABLE Telemetry;");

        logger.Log(CreatePacket());
        logger.Log(CreatePacket());
        logger.Log(CreatePacket());

        Assert.AreEqual(3, logger.FailedCount);
        Assert.AreEqual(3, reportedFailures);
        Assert.IsNotNull(logger.LastError);
    }

    // Verify that logging resumes after a temporary
    // database problem has been resolved.
    [TestMethod]
    public void Log_AfterDatabaseRecovery_SavesPacket()
    {
        using var logger =
            new SqliteTelemetryLogger(_databasePath);

        ExecuteSql(
            "ALTER TABLE Telemetry RENAME TO TelemetryBackup;");

        logger.Log(CreatePacket(6.0));

        Assert.AreEqual(1, logger.FailedCount);

        ExecuteSql(
            "ALTER TABLE TelemetryBackup RENAME TO Telemetry;");

        logger.Log(CreatePacket(6.5));

        var saved = logger.ReadAll();

        Assert.AreEqual(1, saved.Count);
        Assert.AreEqual(6.5, saved[0].Value);

        // Previous failures remain recorded.
        Assert.AreEqual(1, logger.FailedCount);
    }

    // Verify that disposing the logger twice is safe.
    [TestMethod]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        using var logger =
            new SqliteTelemetryLogger(_databasePath);

        logger.Log(CreatePacket());

        logger.Dispose();
        logger.Dispose();

        using var reopened =
            new SqliteTelemetryLogger(_databasePath);

        Assert.AreEqual(1, reopened.ReadAll().Count);
    }

    // Verify that reading from a disposed logger
    // throws the correct exception.
    [TestMethod]
    public void ReadAll_AfterDispose_ThrowsException()
    {
        using var logger =
            new SqliteTelemetryLogger(_databasePath);

        logger.Dispose();

        Assert.ThrowsException<ObjectDisposedException>(
            () => logger.ReadAll());
    }

    // Verify that unspecified timestamps are treated as UTC.
    [TestMethod]
    public void Log_UnspecifiedTimestamp_TreatsAsUtc()
    {
        using var logger =
            new SqliteTelemetryLogger(_databasePath);

        var timestamp = new DateTime(
            2026, 11, 25, 12, 30, 0,
            DateTimeKind.Unspecified);

        var packet = CreatePacket() with
        {
            Timestamp = timestamp
        };

        logger.Log(packet);

        var saved = logger.ReadAll()[0];

        Assert.AreEqual(DateTimeKind.Utc,
            saved.Timestamp.Kind);

        Assert.AreEqual(
            DateTime.SpecifyKind(timestamp, DateTimeKind.Utc),
            saved.Timestamp);
    }

    // Verify that a null message bus is rejected.
    [TestMethod]
    public void Attach_NullBus_ThrowsException()
    {
        using var logger =
            new SqliteTelemetryLogger(_databasePath);

        Assert.ThrowsException<ArgumentNullException>(
            () => logger.Attach(null!));
    }

    // Verify that an empty database returns no recent records.
    [TestMethod]
    public void ReadRecent_EmptyDatabase_ReturnsEmptyList()
    {
        using var logger =
            new SqliteTelemetryLogger(_databasePath);

        var recent = logger.ReadRecent(10);

        Assert.AreEqual(0, recent.Count);
    }
}
