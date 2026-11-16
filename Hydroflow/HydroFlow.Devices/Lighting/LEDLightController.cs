/*
 * File: LEDLightController.cs
 * Author: Jacob Fonseca
 * Project: HydroFlow SCADA
 * Task: HF-54
 *
 * LED grow light controller that reports intensity in percent through the shared
 * Actuator base class and keeps its Green or White colour locally.
 */

using HydroFlow.Core;

namespace HydroFlow.Devices;

public class LEDLightController : Actuator
{
    // Current colour. Only SetColor changes it, so Execute and Tick never overwrite it.
    // TelemetryPacket has no colour field, so the dashboard reads this property directly.
    public LedColor Color { get; private set; } = LedColor.Green;

    /*
     * Purpose: Set up the controller with its ID, name, data source, and message bus.
     * Actuator handles validation, on/off state, feedback reading, caching, and publishing, using % as the unit.
     */
    public LEDLightController(string deviceId, string name, IDataSource dataSource, IMessageBus messageBus)
        : base(deviceId, name, "%", dataSource, messageBus)
    {
    }

    /*
     * Purpose: Reject an LED intensity outside 0 to 100 percent.
     * Actuator.Execute calls this first, so the check also applies through an Actuator reference.
     */
    protected override void ValidateSetting(double setting)
    {
        // Written this way so NaN is rejected too. Infinities fall outside the range.
        if (!(setting >= 0 && setting <= 100))
            throw new ArgumentOutOfRangeException(nameof(setting), setting, "LED intensity must be from 0 to 100 percent.");
    }

    /*
     * Purpose: Change the LED colour.
     * Does not read the data source or publish, so commands and polling stay independent.
     */
    public void SetColor(LedColor color)
    {
        // Reject values outside the enum, such as (LedColor)99.
        if (!Enum.IsDefined(color))
            throw new ArgumentOutOfRangeException(nameof(color), color, "Unknown LED colour.");

        Color = color;
    }
}
