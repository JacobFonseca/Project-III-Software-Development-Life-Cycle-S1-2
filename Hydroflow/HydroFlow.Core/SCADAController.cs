
using System.Collections.Generic;
using HydroFlow.Core;
using HydroFlow.Controller;

namespace HydroFlow.Controller
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
