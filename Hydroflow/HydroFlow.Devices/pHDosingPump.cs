
using HydroFlow.Core;

namespace HydroFlow.Devices;

public class pHDosingPump : Actuator
{
    public pHDosingPump(
        string deviceId,
        string name,
        IDataSource source,
        IMessageBus bus)
        : base(deviceId, name, "mL/min", source, bus)
    {
    }
}
