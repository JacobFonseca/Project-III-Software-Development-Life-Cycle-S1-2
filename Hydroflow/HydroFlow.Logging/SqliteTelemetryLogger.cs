/*
 * File: SqliteTelemetryLogger.cs
 * Author: Jacob Fonseca
 * Project: HydroFlow SCADA
 * Task: HF-57
 *
 * Saves every packet published on the MessageBus to a local SQLite database.
 */

using System.Globalization;
using HydroFlow.Core;
using Microsoft.Data.Sqlite;

namespace HydroFlow.Logging;

public sealed class SqliteTelemetryLogger : ITelemetryLogger, IDisposable
{
    // File name used by the app. It sits beside the .exe so every run uses the same database.
    public const string DefaultFileName = "hydroflow.db";

    // The bus can publish from more than one thread, and one connection is shared.
    private readonly object _lock = new();
    private readonly SqliteConnection _connection;
    private bool _attached;
    private bool _disposed;

    /*
     * Purpose: Open (or create) the database file and make sure the Telemetry table exists.
     * Existing rows are kept, so records survive an app restart.
     */
    public SqliteTelemetryLogger(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
            throw new ArgumentException("A database path is required.", nameof(databasePath));

        DatabasePath = Path.GetFullPath(databasePath);

        // Pooling is off so the file is released as soon as the logger is disposed.
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false
        }.ToString();

        _connection = new SqliteConnection(connectionString);
        _connection.Open();

        using var create = _connection.CreateCommand();
        create.CommandText =
            @"CREATE TABLE IF NOT EXISTS Telemetry (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                DeviceId  TEXT    NOT NULL,
                Type      TEXT    NOT NULL,
                Value     REAL    NOT NULL,
                Unit      TEXT    NOT NULL,
                Timestamp TEXT    NOT NULL,
                Status    TEXT    NOT NULL,
                Message   TEXT    NOT NULL DEFAULT ''
              );";
        create.ExecuteNonQuery();
    }

    // Full path of the database file this logger writes to.
    public string DatabasePath { get; }

    /*
     * Purpose: Build the default database path beside the running executable.
     */
    public static string GetDefaultPath() =>
        Path.Combine(AppContext.BaseDirectory, DefaultFileName);

    /*
     * Purpose: Subscribe this logger to the shared bus.
     * IMessageBus has no Unsubscribe, so a second call is ignored to avoid saving each packet twice.
     */
    public void Attach(IMessageBus bus)
    {
        ArgumentNullException.ThrowIfNull(bus);

        lock (_lock)
        {
            if (_attached)
                return;

            _attached = true;
        }

        bus.Subscribe(Log);
    }

    /*
     * Purpose: Save one packet of any type (Sensor, Actuator, Alarm or Command) as one row.
     * Timestamps are stored as UTC in round trip format so they read back exactly.
     */
    public void Log(TelemetryPacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);

        lock (_lock)
        {
            // Packets can still arrive after the app starts closing, and they are dropped then.
            if (_disposed)
                return;

            using var insert = _connection.CreateCommand();
            insert.CommandText =
                @"INSERT INTO Telemetry (DeviceId, Type, Value, Unit, Timestamp, Status, Message)
                  VALUES ($deviceId, $type, $value, $unit, $timestamp, $status, $message);";
            insert.Parameters.AddWithValue("$deviceId", packet.DeviceId);
            insert.Parameters.AddWithValue("$type", packet.Type);
            insert.Parameters.AddWithValue("$value", packet.Value);
            insert.Parameters.AddWithValue("$unit", packet.Unit ?? "");
            insert.Parameters.AddWithValue("$timestamp", ToUtc(packet.Timestamp).ToString("O", CultureInfo.InvariantCulture));
            insert.Parameters.AddWithValue("$status", packet.Status.ToString());
            insert.Parameters.AddWithValue("$message", packet.Message ?? "");
            insert.ExecuteNonQuery();
        }
    }

    /*
     * Purpose: Read every saved packet, oldest first.
     * Used by the tests now and available for a later history view.
     */
    public IReadOnlyList<TelemetryPacket> ReadAll()
    {
        var packets = new List<TelemetryPacket>();

        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            using var select = _connection.CreateCommand();
            select.CommandText =
                "SELECT DeviceId, Type, Value, Unit, Timestamp, Status, Message FROM Telemetry ORDER BY Id;";

            using var reader = select.ExecuteReader();
            while (reader.Read())
            {
                DateTime timestamp = DateTime.Parse(
                    reader.GetString(4), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

                packets.Add(new TelemetryPacket(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetDouble(2),
                    reader.GetString(3),
                    timestamp,
                    Enum.Parse<DeviceStatus>(reader.GetString(5)),
                    reader.GetString(6)));
            }
        }

        return packets;
    }

    /*
     * Purpose: Close the database connection. Later packets are ignored.
     */
    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
                return;

            _disposed = true;
            _connection.Dispose();
        }
    }

    // Local times are converted. A time with no kind is treated as UTC, since devices use DateTime.UtcNow.
    private static DateTime ToUtc(DateTime time) => time.Kind switch
    {
        DateTimeKind.Local => time.ToUniversalTime(),
        DateTimeKind.Unspecified => DateTime.SpecifyKind(time, DateTimeKind.Utc),
        _ => time
    };
}
