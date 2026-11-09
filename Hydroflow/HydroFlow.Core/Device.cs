using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HydroFlow.Core;

public abstract class Device
{
    public string DeviceId { get; }
    public string Name { get; }

    protected Device(string deviceId, string name)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Device id is required.", nameof(deviceId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Device name is required.", nameof(name));
        DeviceId = deviceId;
        Name = name;
    }

    public abstract TelemetryPacket GetPacket();
}