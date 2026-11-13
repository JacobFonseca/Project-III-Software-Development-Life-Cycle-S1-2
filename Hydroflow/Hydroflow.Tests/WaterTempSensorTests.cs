using Hydroflow.Core;
using HydroFlow.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hydroflow.Tests
{
    [TestClass]
    public class WaterTempSensorTests
    {
        private string CreateTestFile()
        {
            string path = Path.GetTempFileName();

            File.WriteAllLines(path, new[]
            {
                "20.5",
                "21.0",
                "22.3"
            });

            return path;
        }

        [TestMethod]
        public void WaterTempSensor_ReadsFirstTemperature()
        {
            string path = CreateTestFile();

            try
            {
                MessageBus bus = new MessageBus();

                WaterTempSensor sensor =
                    new WaterTempSensor("WT01", path, bus);

                double result = sensor.ReadTemperature();

                Assert.AreEqual(20.5, result);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [TestMethod]
        public void WaterTempSensor_LoopsData()
        {
            string path = CreateTestFile();

            try
            {
                MessageBus bus = new MessageBus();

                WaterTempSensor sensor =
                    new WaterTempSensor("WT01", path, bus);

                sensor.ReadTemperature();
                sensor.ReadTemperature();
                sensor.ReadTemperature();

                double result = sensor.ReadTemperature();

                Assert.AreEqual(20.5, result);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [TestMethod]
        public void WaterTempSensor_UsesCelsius()
        {
            string path = CreateTestFile();

            try
            {
                MessageBus bus = new MessageBus();

                WaterTempSensor sensor =
                    new WaterTempSensor("WT01", path, bus);

                Assert.AreEqual("°C", sensor.SensorUnit);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [TestMethod]
        public void WaterTempSensor_PublishesThroughMessageBus()
        {
            string path = CreateTestFile();

            try
            {
                MessageBus bus = new MessageBus();

                TelemetryPacket? receivedPacket = null;

                bus.Subscribe(packet =>
                {
                    receivedPacket = packet;
                });

                WaterTempSensor sensor =
                    new WaterTempSensor("WT01", path, bus);

                sensor.PublishTemperature();

                Assert.IsNotNull(receivedPacket);
                Assert.AreEqual("WT01", receivedPacket.DeviceId);
                Assert.AreEqual("Sensor", receivedPacket.Type);
                Assert.AreEqual(20.5, receivedPacket.Value);
                Assert.AreEqual("°C", receivedPacket.Unit);
                Assert.AreEqual(DeviceStatus.Normal, receivedPacket.Status);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}

