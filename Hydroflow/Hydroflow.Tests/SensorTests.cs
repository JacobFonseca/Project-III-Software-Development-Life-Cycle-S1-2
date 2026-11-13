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
    public class SensorTests
    {
        // Fake IDataSource
        private class FakeDataSource : IDataSource
        {
            public double ReadNext()
            {
                return 25.5;
            }
        }

        // Fake Sensor because Sensor is abstract
        private class FakeSensor : Sensor
        {
            public FakeSensor(IDataSource source, IMessageBus bus)
                : base("S1", "Test Sensor", "C", source, bus)
            {
            }
        }

        [TestMethod]
        public void Tick_ReadsValueFromDataSource()
        {
            // Arrange
            FakeDataSource source = new FakeDataSource();
            MessageBus bus = new MessageBus();
            FakeSensor sensor = new FakeSensor(source, bus);

            // Act
            TelemetryPacket packet = sensor.Tick();

            // Assert
            Assert.AreEqual(25.5, packet.Value);
        }

        [TestMethod]
        public void Tick_CreatesCorrectTelemetryPacket()
        {
            // Arrange
            FakeDataSource source = new FakeDataSource();
            MessageBus bus = new MessageBus();
            FakeSensor sensor = new FakeSensor(source, bus);

            // Act
            TelemetryPacket packet = sensor.Tick();

            // Assert
            Assert.AreEqual("S1", packet.DeviceId);
            Assert.AreEqual("Sensor", packet.Type);
            Assert.AreEqual(25.5, packet.Value);
            Assert.AreEqual("C", packet.Unit);
            Assert.AreEqual(DeviceStatus.Normal, packet.Status);
        }

        [TestMethod]
        public void Tick_PublishesPacketToMessageBus()
        {
            // Arrange
            FakeDataSource source = new FakeDataSource();
            MessageBus bus = new MessageBus();
            FakeSensor sensor = new FakeSensor(source, bus);

            TelemetryPacket? receivedPacket = null;

            bus.Subscribe(packet =>
            {
                receivedPacket = packet;
            });

            // Act
            sensor.Tick();

            // Assert
            Assert.IsNotNull(receivedPacket);
            Assert.AreEqual(25.5, receivedPacket.Value);
            Assert.AreEqual("S1", receivedPacket.DeviceId);
        }

        [TestMethod]
        public void GetPacket_ReturnsLatestTickValue()
        {
            // Arrange
            FakeDataSource source = new FakeDataSource();
            MessageBus bus = new MessageBus();
            FakeSensor sensor = new FakeSensor(source, bus);

            // Act
            sensor.Tick();
            TelemetryPacket packet = sensor.GetPacket();

            // Assert
            Assert.AreEqual(25.5, packet.Value);
        }
    }

}