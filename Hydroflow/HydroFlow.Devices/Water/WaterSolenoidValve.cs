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

    protected override void ValidateSetting(double setting)
    {
        if (setting < 0 || setting > 100)
            throw new ArgumentOutOfRangeException(nameof(setting), "Valve opening must be 0-100%.");
    }


}