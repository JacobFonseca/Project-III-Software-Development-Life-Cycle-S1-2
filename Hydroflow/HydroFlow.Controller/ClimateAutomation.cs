using System;
using HydroFlow.Core;

namespace HydroFlow.Controller
{
    public class ClimateAutomation
    {
        public const string HumidityId = "HUM-01";

        private readonly CommandInvoker _invoker;
        private readonly Actuator _fan;
        private readonly double _onSetting;

        public ClimateAutomation(IMessageBus bus, CommandInvoker invoker, Actuator fan, double onSetting = 1200)
        {
            if (bus == null) throw new ArgumentNullException(nameof(bus));
            _invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
            _fan = fan ?? throw new ArgumentNullException(nameof(fan));
            _onSetting = onSetting;
            bus.Subscribe(OnPacket);
        }

        private void OnPacket(TelemetryPacket p)
        {
            if (p.Type != "Alarm" || p.DeviceId != HumidityId) return;

            if (p.Status != DeviceStatus.Normal)
            {
                if (!_fan.IsOn)
                    _invoker.Run(new ActuatorCommand(_fan, true, _onSetting));
            }
            else if (_fan.IsOn)
            {
                _invoker.Run(new ActuatorCommand(_fan, false, 0));
            }
        }
    }
}