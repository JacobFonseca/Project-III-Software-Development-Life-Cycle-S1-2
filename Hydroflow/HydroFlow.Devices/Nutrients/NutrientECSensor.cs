
using HydroFlow.Core;

namespace HydroFlow.Devices;

public class NutrientECSensor : Sensor
{
    public NutrientECSensor(
        string deviceId,
        string name,
        IDataSource source,
        IMessageBus bus)
        : base(deviceId, name, "mS/cm", source, bus)
    {
    }
}
