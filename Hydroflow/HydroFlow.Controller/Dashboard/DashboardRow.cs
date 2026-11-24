/*
 * File: DashboardRow.cs
 * Author: Jacob Fonseca
 * Project: HydroFlow SCADA
 * Task: HF-59
 *
 * One device on the dashboard: its module, latest reading and when that reading arrived.
 */

using System.ComponentModel;
using HydroFlow.Core;

namespace HydroFlow.Controller.Dashboard;

public class DashboardRow : INotifyPropertyChanged
{
    private double? _value;
    private string _unit;
    private DeviceStatus _status = DeviceStatus.Normal;
    private DateTime? _lastUpdated;

    /*
     * Purpose: Create a row for a device that has not reported yet.
     */
    public DashboardRow(string deviceId, string name, string module, string unit)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Device id is required.", nameof(deviceId));

        DeviceId = deviceId;
        Name = name ?? "";
        Module = module ?? DeviceModules.Other;
        _unit = unit ?? "";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string DeviceId { get; }
    public string Name { get; }
    public string Module { get; }

    // Null until the first reading arrives.
    public double? Value
    {
        get => _value;
        private set => Set(ref _value, value, nameof(Value));
    }

    public string Unit
    {
        get => _unit;
        private set => Set(ref _unit, value, nameof(Unit));
    }

    // Status carried by the latest reading packet.
    public DeviceStatus Status
    {
        get => _status;
        private set => Set(ref _status, value, nameof(Status));
    }

    // Packet time of the latest reading, in UTC. Null until the first reading.
    public DateTime? LastUpdated
    {
        get => _lastUpdated;
        private set
        {
            if (Set(ref _lastUpdated, value, nameof(LastUpdated)))
                OnPropertyChanged(nameof(LastUpdatedLocal));
        }
    }

    // LastUpdated in the computer's local time, for display.
    public DateTime? LastUpdatedLocal => _lastUpdated?.ToLocalTime();

    /*
     * Purpose: Copy a reading packet into the row. Only DashboardReadings calls this.
     */
    internal void ApplyReading(TelemetryPacket packet)
    {
        Value = packet.Value;
        Unit = packet.Unit;
        Status = packet.Status;
        LastUpdated = packet.Timestamp.Kind == DateTimeKind.Local
            ? packet.Timestamp.ToUniversalTime()
            : DateTime.SpecifyKind(packet.Timestamp, DateTimeKind.Utc);
    }

    // Returns true when the value changed, so callers only raise events for real changes.
    private bool Set<T>(ref T field, T value, string propertyName)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
