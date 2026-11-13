using HydroFlow.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hydroflow.Core
{
    public class WaterTempSensor : Sensor
    {
        private List<double> temperatures = new();
        private int currentIndex = 0;
        private readonly IMessageBus bus;

        public WaterTempSensor(
            string sensorId,
            string filePath,
            IMessageBus messageBus)
            : base(sensorId, "0", "°C")
        {
            ArgumentNullException.ThrowIfNull(messageBus);

            bus = messageBus;

            foreach (string line in File.ReadAllLines(filePath))
            {
                if (double.TryParse(
                    line,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double temperature))
                {
                    temperatures.Add(temperature);
                }
            }

            if (temperatures.Count == 0)
            {
                throw new InvalidDataException(
                    "No valid temperature data found.");
            }
        }

        public double ReadTemperature()
        {
            double temperature = temperatures[currentIndex];

            currentIndex++;

            if (currentIndex >= temperatures.Count)
            {
                currentIndex = 0;
            }

            return temperature;
        }

        public void PublishTemperature()
        {
            double temperature = ReadTemperature();

            TelemetryPacket packet = new TelemetryPacket(
                SensorId,
                "Sensor",
                temperature,
                SensorUnit,
                DateTime.Now,
                DeviceStatus.Normal
            );

            bus.Publish(packet);
        }
    }
}
