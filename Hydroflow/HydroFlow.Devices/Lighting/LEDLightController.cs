/*
 * File: LEDLightController.cs
 * Author: Jacob Fonseca
 * Project: HydroFlow SCADA
 * Task: HF-54
 *
 * Simulated LED grow light. Reads its light output level from a data source,
 * publishes telemetry through the MessageBus, and accepts a colour command.
 *
 * Temporary assumptions pending team confirmation:
 * Inherits from Device (Actuator is not available yet), publishes the light
 * level in lux with Normal status, starts Green, and supports Green and White.
 * SetColour is a provisional control method. When the shared command contract
 * (DeviceCommand / Actuator) arrives, it should call SetColour or replace it.
 */

using HydroFlow.Core;

namespace HydroFlow.Devices;

public class LEDLightController : Device
{
    // Type and unit included in each reading.
    private const string ActuatorType = "Actuator";
    private const string Unit = "lux";

    // Data source for light levels and message bus for publishing.
    private readonly IDataSource _dataSource;
    private readonly IMessageBus _messageBus;

    // Current colour. Only SetColour changes it, so polling never overwrites a command.
    public LedColour Colour { get; private set; } = LedColour.Green;

    /*
     * Purpose: Set up the controller with its ID, name, data source, and message bus.
     */
    public LEDLightController(string deviceId, string name, IDataSource dataSource, IMessageBus messageBus)
        : base(deviceId, name)
    {
        // Check both dependencies before storing them.
        ArgumentNullException.ThrowIfNull(dataSource);
        ArgumentNullException.ThrowIfNull(messageBus);

        _dataSource = dataSource;
        _messageBus = messageBus;
    }

    /*
     * Purpose: Read the next light level and package it with the controller details.
     * Reports Normal until the controller evaluates the reading.
     */
    public override TelemetryPacket GetPacket()
    {
        double value = _dataSource.ReadNext();
        return new TelemetryPacket(DeviceId, ActuatorType, value, Unit, DateTime.UtcNow, DeviceStatus.Normal);
    }

    /*
     * Purpose: Publish one reading when called by the external polling code.
     */
    public void PublishReading()
    {
        TelemetryPacket packet = GetPacket();
        _messageBus.Publish(packet);
    }

    /*
     * Purpose: Change the LED colour (provisional control method).
     * Does not read the data source or publish, so commands and polling stay independent.
     */
    public void SetColour(LedColour colour)
    {
        // Reject values outside the enum, such as (LedColour)99.
        if (!Enum.IsDefined(colour))
            throw new ArgumentOutOfRangeException(nameof(colour), colour, "Unknown LED colour.");

        Colour = colour;
    }
}
