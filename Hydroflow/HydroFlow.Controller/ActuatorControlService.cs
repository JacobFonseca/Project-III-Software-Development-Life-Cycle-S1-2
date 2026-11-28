using System;
using System.Collections.Generic;
using System.Linq;
using HydroFlow.Core;

namespace HydroFlow.Controller;

public class ActuatorControlService
{
    private readonly Dictionary<string, Actuator> _actuators;
    private readonly CommandInvoker _invoker;

    public ActuatorControlService(
        IEnumerable<Actuator> actuators,
        IMessageBus bus)
    {
        _actuators = actuators.ToDictionary(a => a.DeviceId);
        _invoker = new CommandInvoker(bus);
    }

    public string Set(string deviceId, bool on)
    {
        return Set(deviceId, on,
            on ? GetDefaultSetting(deviceId) : 0);
    }

    public string Set(string deviceId, bool on, double setting)
    {
        if (!_actuators.TryGetValue(deviceId, out var actuator))
        {
            throw new ArgumentException("Unknown actuator.");
        }

        var command = new ActuatorCommand(actuator, on, setting);

        _invoker.Run(command);

        return command.Description;
    }

    private static double GetDefaultSetting(string deviceId)
    {
        return deviceId switch
        {
            "FAN-01" => 1000,
            "VLV-01" => 1,
            "PHP-01" => 50,
            "NUT-01" => 50,
            "LED-01" => 80,
            _ => throw new ArgumentException("Unknown actuator.")
        };
    }
}