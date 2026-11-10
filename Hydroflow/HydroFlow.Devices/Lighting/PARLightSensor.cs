/*
 * File: PARLightSensor.cs
 * Author: Jacob Fonseca
 * Project: HydroFlow SCADA
 * Task: HF-53
 *
 * Reads simulated light levels from a data source and publishes
 * telemetry packets through the MessageBus.
 *
 * Temporary assumptions pending team confirmation:
 * Inherits from Device, uses lux, and reports Normal status.
 */

using HydroFlow.Core;

namespace HydroFlow.Devices;

public class PARLightSensor : Device
{
    // Type and unit included in each reading.
    private const string SensorType = "Sensor";
    private const string Unit = "lux";

    // Data source for readings and message bus for publishing.
    private readonly IDataSource _dataSource;
    private readonly IMessageBus _messageBus;

    /*
     * Purpose: Set up the sensor with its ID, name, data source, and message bus.
     */
    public PARLightSensor(string deviceId, string name, IDataSource dataSource, IMessageBus messageBus)
        : base(deviceId, name)
    {
        // Check both dependencies before storing them.
        ArgumentNullException.ThrowIfNull(dataSource);
        ArgumentNullException.ThrowIfNull(messageBus);

        _dataSource = dataSource;
        _messageBus = messageBus;
    }

    /*
     * Purpose: Read the next value and package it with the sensor details.
     * Reports Normal until the controller evaluates the reading.
     */
    public override TelemetryPacket GetPacket()
    {
        double value = _dataSource.ReadNext();
        return new TelemetryPacket(DeviceId, SensorType, value, Unit, DateTime.UtcNow, DeviceStatus.Normal);
    }

    /*
     * Purpose: Publish one reading when called by the external polling code.
     */
    public void PublishReading()
    {
        TelemetryPacket packet = GetPacket();
        _messageBus.Publish(packet);
    }
}