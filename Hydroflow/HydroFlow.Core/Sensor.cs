using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hydroflow.Core
{
    abstract class Sensor

    {

        public string SensorId { get; }
        public string SensorValue { get; }

        public string SensorUnit { get; }


      public Sensor(string sensorId, string sensorValue, string sensorUnit)
        {
            if (string.IsNullOrWhiteSpace(sensorId))
                throw new ArgumentException("Sensor id is required.", nameof(sensorId));
            if (string.IsNullOrWhiteSpace(sensorValue))
                throw new ArgumentException("Sensor value is required.", nameof(sensorValue));
            if (string.IsNullOrWhiteSpace(sensorUnit))
                throw new ArgumentException("Sensor unit is required.", nameof(sensorUnit));
            SensorId = sensorId;
            SensorValue = sensorValue;
            SensorUnit = sensorUnit;
        }










    }
}
