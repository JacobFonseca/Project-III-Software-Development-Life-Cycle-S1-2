using HydroFlow.Core;

namespace HydroFlow.Devices.Nutrients;

public class pHSensor : Sensor
{
    public pHSensor(
        string deviceId,
        string name,
        IDataSource source,
        IMessageBus bus)
        : base(deviceId, name, "pH", source, bus)
    {
    }
}
