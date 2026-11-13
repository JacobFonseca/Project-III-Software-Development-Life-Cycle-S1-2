using Hydroflow.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hydroflow.Tests
{
    internal class WaterLevelSensorTest
    {
        private string CreateTestFile()
        {
            string path = Path.GetTempFileName();

            File.WriteAllLines(path, new[]
            {
                "100",
                "150",
                "200"
            });

            return path;
        }

        [TestMethod]
        public void WaterLevelSensor_ReadsFirstValue()
        {
            string path = CreateTestFile();

            try
            {
                WaterLevelSensor sensor =
                    new WaterLevelSensor("WL01", path);

                double result = sensor.ReadLevel();

                Assert.AreEqual(100.0, result);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [TestMethod]
        public void WaterLevelSensor_LoopsData()
        {
            string path = CreateTestFile();

            try
            {
                WaterLevelSensor sensor =
                    new WaterLevelSensor("WL01", path);

                sensor.ReadLevel();
                sensor.ReadLevel();
                sensor.ReadLevel();

                double result = sensor.ReadLevel();

                Assert.AreEqual(100.0, result);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [TestMethod]
        public void WaterLevelSensor_UsesLitres()
        {
            string path = CreateTestFile();

            try
            {
                WaterLevelSensor sensor =
                    new WaterLevelSensor("WL01", path);

                Assert.AreEqual("L", sensor.GetUnit());
            }
            finally
            {
                File.Delete(path);
            }
        }

        [TestMethod]
        public void WaterLevelPercentage_DoesNotExceed100()
        {
            WaterLevelSensor sensor = new WaterLevelSensor("WL01", filePath);

            double result = sensor.GetPercentage(600);

            Assert.AreEqual(100.0, result);
        }
    }
}
