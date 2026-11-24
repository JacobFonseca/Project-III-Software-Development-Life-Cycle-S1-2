using System;
using System.Collections.Generic;
using System.Linq;

namespace HydroFlow.Core;

public record AlarmEntry(
    string DeviceId,
    string Message,
    DeviceStatus Status,
    DateTime Timestamp,
    bool IsCleared);

public class AlarmTracker
{
    private readonly IMessageBus _bus;
    private readonly List<AlarmEntry> _active = new();
    private readonly object _lock = new();

    public event Action? Changed;

    public AlarmTracker(IMessageBus bus)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _bus.Subscribe(OnPacket);
    }

    public IReadOnlyList<AlarmEntry> Active
    {
        get { lock (_lock) { return _active.ToList(); } }
    }

    private void OnPacket(TelemetryPacket p)
    {
        if (p.Type != "Alarm") return;

        lock (_lock)
        {
            int i = _active.FindIndex(a => a.DeviceId == p.DeviceId);

            if (p.Status == DeviceStatus.Normal)
            {
                if (i < 0) return;   // nothing to clear
                // Keep it on screen until the operator acknowledges it.
                _active[i] = _active[i] with { IsCleared = true };
            }
            else
            {
                var entry = new AlarmEntry(
                    p.DeviceId, p.Message, p.Status, p.Timestamp, false);
                if (i < 0) _active.Add(entry); else _active[i] = entry;
            }
        }

        Changed?.Invoke();
    }

    public void Acknowledge(string deviceId)
    {
        bool removed;
        lock (_lock)
        {
            removed = _active.RemoveAll(a => a.DeviceId == deviceId) > 0;
        }
        if (!removed) return;

        // Logs the operator action (FR-05).
        _bus.Publish(new TelemetryPacket(
            deviceId, "Command", 0, "", DateTime.UtcNow, DeviceStatus.Normal,
            $"Operator acknowledged alarm on {deviceId}"));

        Changed?.Invoke();
    }
}