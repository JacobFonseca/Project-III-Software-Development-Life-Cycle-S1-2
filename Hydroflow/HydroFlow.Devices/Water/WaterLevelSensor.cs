using HydroFlow.Core;
using HydroFlow.Devices;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HydroFlow.Core
{

    public class WaterLevelSensor : Sensor
    {
        // Value is % of tank capacity (0-100).
        // Range checks are Sprint 2 alarm logic.

        public WaterLevelSensor(
            string deviceId,
            string name,
            IDataSource source,
            IMessageBus bus)
            : base(deviceId, name, "%", source, bus)
        {
        }
    }
}
