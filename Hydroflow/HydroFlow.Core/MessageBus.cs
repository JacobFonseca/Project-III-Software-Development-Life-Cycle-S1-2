using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HydroFlow.Core;

/// <summary>Observer pattern: devices publish packets, subscribers (GUI, controller, logger) receive them.</summary>
public class MessageBus : IMessageBus
{
    private readonly List<Action<TelemetryPacket>> _subscribers = new();
    private readonly object _lock = new();

    public void Subscribe(Action<TelemetryPacket> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        lock (_lock) { _subscribers.Add(handler); }
    }

    public void Publish(TelemetryPacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        Action<TelemetryPacket>[] snapshot;
        lock (_lock) { snapshot = _subscribers.ToArray(); }
        foreach (var subscriber in snapshot)
            subscriber(packet);
    }
}