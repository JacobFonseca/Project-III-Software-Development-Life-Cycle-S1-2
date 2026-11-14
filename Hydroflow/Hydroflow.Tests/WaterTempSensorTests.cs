
using HydroFlow.Core;
using HydroFlow.Devices.Water;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HydroFlow.Tests
{

    [TestClass]
    public class WaterTempSensorTests
    {
        [TestMethod]
        public void Tick_ReturnsTemperature()
        {
            // Arrange
            string path = Path.GetTempFileName();

            try
            {
                File.WriteAllText(path, "22.5");

                var source = new LoopingFileReader(path);
                var bus = new MessageBus();

                var sensor = new WaterTempSensor(
                    "TEMP01", "Water Temperature", source, bus);

                // Act
                TelemetryPacket packet = sensor.Tick();

                // Assert
                Assert.AreEqual(22.5, packet.Value);
                Assert.AreEqual("°C", packet.Unit);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [TestMethod]
        public void Tick_LoopsTemperatureReadings()
        {
            // Arrange
            string path = Path.GetTempFileName();

            try
            {
                File.WriteAllLines(path, new[]
                {
                "22.5", "23.0"
            });

                var source = new LoopingFileReader(path);
                var bus = new MessageBus();

                var sensor = new WaterTempSensor(
                    "TEMP01", "Water Temperature", source, bus);

                // Act
                double first = sensor.Tick().Value;
                double second = sensor.Tick().Value;
                double third = sensor.Tick().Value;

                // Assert
                Assert.AreEqual(22.5, first);
                Assert.AreEqual(23.0, second);
                Assert.AreEqual(22.5, third);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [TestMethod]
        public void Tick_PublishesToMessageBus()
        {
            // Arrange
            string path = Path.GetTempFileName();

            try
            {
                File.WriteAllText(path, "25.5");

                var source = new LoopingFileReader(path);
                var bus = new MessageBus();

                TelemetryPacket? received = null;

                bus.Subscribe(packet => received = packet);

                var sensor = new WaterTempSensor(
                    "TEMP01", "Water Temperature", source, bus);

                // Act
                sensor.Tick();

                // Assert
                Assert.IsNotNull(received);
                Assert.AreEqual("TEMP01", received.DeviceId);
                Assert.AreEqual(25.5, received.Value);
                Assert.AreEqual("°C", received.Unit);
                Assert.AreEqual("Sensor", received.Type);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}

