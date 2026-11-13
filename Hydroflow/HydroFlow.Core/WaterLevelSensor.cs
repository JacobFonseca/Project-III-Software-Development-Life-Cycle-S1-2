using HydroFlow.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hydroflow.Core
{
    public class WaterLevelSensor : Sensor
    {
        public WaterLevelSensor(
            string deviceId,
            string name,
            IDataSource source,
            IMessageBus bus)
            : base(deviceId, name, "L", source, bus)
        {
        }
    }
}
