using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HydroFlow.Core;

public abstract class Sensor : Device
{
    private readonly IDataSource _source;
    private readonly IMessageBus _bus;
    private TelemetryPacket _latestPacket;
    public string Unit { get; }

    protected Sensor(string deviceId, string name, string unit, IDataSource source, IMessageBus bus)
        : base(deviceId, name)
    {
        if (string.IsNullOrWhiteSpace(unit))
            throw new ArgumentException("Unit is required.", nameof(unit));
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        Unit = unit;
        _latestPacket = CreatePacket(0);
    }

    public TelemetryPacket Tick()
    {
        _latestPacket = CreatePacket(_source.ReadNext());
        _bus.Publish(_latestPacket);
        return _latestPacket;
    }

    public override TelemetryPacket GetPacket() => _latestPacket;

    private TelemetryPacket CreatePacket(double value) =>
        new(DeviceId, "Sensor", value, Unit, DateTime.UtcNow, DeviceStatus.Normal);
}
