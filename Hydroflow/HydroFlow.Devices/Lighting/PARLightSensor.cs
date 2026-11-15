/*
 * File: PARLightSensor.cs
 * Author: Jacob Fonseca
 * Project: HydroFlow SCADA
 * Task: HF-53
 *
 * PAR light sensor that reports simulated light levels in lux through the shared Sensor base class.
 */

using HydroFlow.Core;

namespace HydroFlow.Devices;

public class PARLightSensor : Sensor
{
    /*
     * Purpose: Set up the sensor with its ID, name, data source, and message bus.
     * Sensor handles validation, reading, caching, and publishing, using lux as the unit.
     */
    public PARLightSensor(string deviceId, string name, IDataSource dataSource, IMessageBus messageBus)
        : base(deviceId, name, "lux", dataSource, messageBus)
    {
    }
}