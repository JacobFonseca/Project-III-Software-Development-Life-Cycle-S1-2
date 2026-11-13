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
        private List<double> waterLevels = new();
        private int currentIndex = 0;

        public WaterLevelSensor(string sensorId, string filePath)
            : base(sensorId, "0", "L")
        {
            foreach (string line in File.ReadAllLines(filePath))
            {
                if (double.TryParse(line, out double level))
                {
                    waterLevels.Add(level);
                }
            }

            if (waterLevels.Count == 0)
            {
                throw new InvalidDataException(
                    "No valid water levels found.");
            }
        }

        public double GetWaterLevel(double waterLevel)
        {
            double maxCapacity = 500;

            return Math.Clamp(waterLevel, 0, maxCapacity);
        }

        public double GetPercentage(double waterLevel)
        {
            double maxCapacity = 500;

            double level = GetWaterLevel(waterLevel);

            return (level / maxCapacity) * 100;
        }

        public double ReadLevel()
        {
            double level = waterLevels[currentIndex];

            currentIndex++;

            if (currentIndex >= waterLevels.Count)
            {
                currentIndex = 0;
            }

            return GetWaterLevel(level);
        }

        public string GetUnit()
        {
            return "L";
        }
    }
}
