using HydroFlow.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hydroflow.Core
{
    public class SCADAController
    {
        private Dictionary<string, TelemetryPacket> packets = new();

        public SCADAController(IMessageBus bus)
        {
            bus.Subscribe(SavePacket);
        }

        private void SavePacket(TelemetryPacket packet)
        {
            packets[packet.DeviceId] = packet;
        }

        public TelemetryPacket? GetLatest(string deviceId)
        {
            if (packets.ContainsKey(deviceId))
            {
                return packets[deviceId];
            }

            return null;
        }
    }
}
