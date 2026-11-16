using HydroFlow.Core;

namespace HydroFlow.Devices.Nutrients;

public class NutrientDosingPump : Actuator
{
    public NutrientDosingPump(
        string deviceId,
        string name,
        IDataSource source,
        IMessageBus bus)
        : base(deviceId, name, "mL/min", source, bus)
    {
    }
}
