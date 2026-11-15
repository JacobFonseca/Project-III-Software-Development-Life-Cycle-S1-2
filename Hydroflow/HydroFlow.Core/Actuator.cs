
using System;

namespace HydroFlow.Core;

public abstract class Actuator : Device
{
    private readonly IDataSource _feedback;
    private readonly IMessageBus _bus;
    private TelemetryPacket _latestPacket;

    public string Unit { get; }
    public bool IsOn { get; private set; }
    public double Setting { get; private set; }

    protected Actuator(
        string deviceId,
        string name,
        string unit,
        IDataSource feedback,
        IMessageBus bus)
        : base(deviceId, name)
    {
        if (string.IsNullOrWhiteSpace(unit))
            throw new ArgumentException(
                "Unit is required.", nameof(unit));

        _feedback = feedback ??
            throw new ArgumentNullException(nameof(feedback));

        _bus = bus ??
            throw new ArgumentNullException(nameof(bus));

        Unit = unit;
        IsOn = false;
        Setting = 0;

        _latestPacket = CreatePacket(0);
    }

    public void Execute(bool on, double setting)
    {
        IsOn = on;
        Setting = setting;

        _latestPacket = CreatePacket(
            IsOn ? Setting : 0);

        _bus.Publish(_latestPacket);
    }

    public TelemetryPacket Tick()
    {
        double value = IsOn ? _feedback.ReadNext() : 0;

        _latestPacket = CreatePacket(value);
        _bus.Publish(_latestPacket);

        return _latestPacket;
    }

    public override TelemetryPacket GetPacket()
    {
        return _latestPacket;
    }

    private TelemetryPacket CreatePacket(double value)
    {
        return new TelemetryPacket(
            DeviceId,
            "Actuator",
            value,
            Unit,
            DateTime.UtcNow,
            DeviceStatus.Normal
        );
    }
}
