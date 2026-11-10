using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HydroFlow.Core;

public interface IMessageBus
{
    void Subscribe(Action<TelemetryPacket> handler);
    void Publish(TelemetryPacket packet);
}