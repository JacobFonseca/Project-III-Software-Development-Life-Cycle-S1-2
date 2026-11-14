using HydroFlow.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class WaterSolenoidValve : Actuator
{
    // 0 = Closed, 1 = Open
    public WaterSolenoidValve(
        string deviceId,
        string name,
        IDataSource source,
        IMessageBus bus)
        : base(deviceId, name, "state", source, bus)
    {
    }
}