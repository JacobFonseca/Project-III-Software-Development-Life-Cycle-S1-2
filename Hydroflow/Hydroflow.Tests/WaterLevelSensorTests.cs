using HydroFlow.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HydroFlow.Tests
{
    [TestClass]
    public class WaterLevelSensorTests
    {
        // Fake data source for testing
        private class FakeDataSource : IDataSource
        {
            private readonly double _value;

            public FakeDataSource(double value)
            {
                _value = value;
            }

            public double ReadNext()
            {
                return _value;
            }
        }

        [DataTestMethod]
        [DataRow(0.0)]
        [DataRow(12.0)]
        [DataRow(15.0)]
        [DataRow(20.0)]
        [DataRow(50.0)]
        [DataRow(100.0)]
        public void Tick_ReturnsCorrectWaterLevel(double level)
        {
            // Arrange
            FakeDataSource source = new FakeDataSource(level);
            MessageBus bus = new MessageBus();

            WaterLevelSensor sensor = new WaterLevelSensor(
                "WL1", "Water Level", source, bus);

            // Act
            TelemetryPacket packet = sensor.Tick();

            // Assert
            Assert.AreEqual(level, packet.Value);
            Assert.AreEqual("%", packet.Unit);
        }

        [TestMethod]
        public void Tick_CreatesCorrectTelemetryPacket()
        {
            // Arrange
            FakeDataSource source = new FakeDataSource(50);
            MessageBus bus = new MessageBus();

            WaterLevelSensor sensor = new WaterLevelSensor(
                "WL1", "Water Level", source, bus);

            // Act
            TelemetryPacket packet = sensor.Tick();

            // Assert
            Assert.AreEqual("WL1", packet.DeviceId);
            Assert.AreEqual("Sensor", packet.Type);
            Assert.AreEqual(50.0, packet.Value);
            Assert.AreEqual("%", packet.Unit);
            Assert.AreEqual(DeviceStatus.Normal, packet.Status);
        }

        [TestMethod]
        public void Tick_PublishesToMessageBus()
        {
            // Arrange
            FakeDataSource source = new FakeDataSource(15);
            MessageBus bus = new MessageBus();

            WaterLevelSensor sensor = new WaterLevelSensor(
                "WL1", "Water Level", source, bus);

            TelemetryPacket? received = null;

            bus.Subscribe(packet =>
            {
                received = packet;
            });

            // Act
            sensor.Tick();

            // Assert
            Assert.IsNotNull(received);
            Assert.AreEqual(15.0, received.Value);
            Assert.AreEqual("%", received.Unit);
        }

        [TestMethod]
        public void Tick_LowWaterLevel_DoesNotThrow()
        {
            // Arrange
            FakeDataSource source = new FakeDataSource(12);
            MessageBus bus = new MessageBus();

            WaterLevelSensor sensor = new WaterLevelSensor(
                "WL1", "Water Level", source, bus);

            // Act
            TelemetryPacket packet = sensor.Tick();

            // Assert
            Assert.AreEqual(12.0, packet.Value);
            Assert.AreEqual(DeviceStatus.Normal, packet.Status);
        }

        [TestMethod]
        public void GetPacket_ReturnsLatestWaterLevel()
        {
            // Arrange
            FakeDataSource source = new FakeDataSource(75);
            MessageBus bus = new MessageBus();

            WaterLevelSensor sensor = new WaterLevelSensor(
                "WL1", "Water Level", source, bus);

            // Act
            sensor.Tick();
            TelemetryPacket packet = sensor.GetPacket();

            // Assert
            Assert.AreEqual(75.0, packet.Value);
            Assert.AreEqual("%", packet.Unit);
        }
    }
}
