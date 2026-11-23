using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HydroFlow.Core;

public class ActuatorCommand : ICommand
{
    private readonly Actuator _actuator;
    private readonly bool _on;
    private readonly double _setting;

    public ActuatorCommand(Actuator actuator, bool on, double setting)
    {
        _actuator = actuator ?? throw new ArgumentNullException(nameof(actuator));
        _on = on;
        _setting = setting;
    }

    public string DeviceId => _actuator.DeviceId;

    public string Description =>
        $"{_actuator.DeviceId} turned {(_on ? "ON" : "OFF")} (setting {_setting} {_actuator.Unit})";

    public void Execute() => _actuator.Execute(_on, _setting);
}
