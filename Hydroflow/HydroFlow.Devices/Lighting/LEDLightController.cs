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
     * Purpose: Turn the LED on or off at an intensity from 0 to 100 percent.
     * Actuator does not check the range, so it is checked here before the base call.
     * Actuator.Execute is not virtual, so calls through an Actuator reference skip this check
     * until Actuator offers a validation hook.
     */
    public new void Execute(bool on, double intensity)
    {
        // Written this way so NaN is rejected too.
        if (!(intensity >= 0 && intensity <= 100))
            throw new ArgumentOutOfRangeException(nameof(intensity), intensity, "LED intensity must be from 0 to 100 percent.");

        base.Execute(on, intensity);
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
