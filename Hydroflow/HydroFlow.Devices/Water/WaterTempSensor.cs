using HydroFlow.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HydroFlow.Devices.Water
{
    public class WaterTempSensor : Sensor
    {
        public WaterTempSensor(
            string deviceId,
            string name,
            IDataSource source,
            IMessageBus bus)
            : base(deviceId, name, "°C", source, bus)
        {
        }
    }
}
