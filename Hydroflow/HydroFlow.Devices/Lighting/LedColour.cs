/*
 * File: LedColour.cs
 * Author: Jacob Fonseca
 * Project: HydroFlow SCADA
 * Task: HF-54
 *
 * Colours the LED light controller can be set to.
 * Provisional: only the two colours in the SRS lighting story (green to white).
 * Add colours here if the team or SRS defines more.
 */

namespace HydroFlow.Devices;

public enum LedColour
{
    Green,
    White
}
