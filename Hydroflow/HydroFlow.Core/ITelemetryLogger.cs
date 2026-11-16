/*
 * File: ITelemetryLogger.cs
 * Author: Jacob Fonseca
 * Project: HydroFlow SCADA
 * Task: HF-39
 *
 * Defines the logger contract for storing the telemetry packets published on the MessageBus.
 */

namespace HydroFlow.Core;

public interface ITelemetryLogger
{
    /*
     * Purpose: Store one telemetry packet exactly as it was published.
     * Matches Action<TelemetryPacket>, so a logger can be passed straight to IMessageBus.Subscribe.
     * Implementations reject a null packet with ArgumentNullException.
     */
    void Log(TelemetryPacket packet);
}
