
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
        // Provides a predictable reading for testing.
        // Fake logging service used for testing.
        private class FakeLogger : ITelemetryLogger
        {
            public List<TelemetryPacket> LoggedPackets { get; } = new();

            public void Log(TelemetryPacket packet)
            {
                ArgumentNullException.ThrowIfNull(packet);
                LoggedPackets.Add(packet);
            }
        }
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
    [TestMethod]
        public void US4_NormalTemperature_NoAlert()
        {
            // Arrange
            MessageBus bus = new MessageBus();
            var controller = new HydroFlow.Controller.SCADAController(bus);

            FakeLogger logger = new FakeLogger();
            bus.Subscribe(logger.Log);

            // Act
            bus.Publish(new TelemetryPacket(
                "WT-01", "Sensor", 20.0, "°C",
                DateTime.UtcNow, DeviceStatus.Normal));

            // Assert
            var latest = controller.GetLatest("WT-01");

            Assert.IsNotNull(latest);
            Assert.AreEqual(20.0, latest.Value);
            Assert.AreEqual(DeviceStatus.Normal, latest.Status);

            Assert.AreEqual(0, logger.LoggedPackets
                .Count(p => p.Type == "Alarm"));
        }

        [TestMethod]
        public void US4_HighTemperature_AlertPublished()
        {
            // Arrange
            MessageBus bus = new MessageBus();
            var controller = new HydroFlow.Controller.SCADAController(bus);

            FakeLogger logger = new FakeLogger();
            bus.Subscribe(logger.Log);

            // Act
            bus.Publish(new TelemetryPacket(
                "WT-01", "Sensor", 20.0, "°C",
                DateTime.UtcNow, DeviceStatus.Normal));

            bus.Publish(new TelemetryPacket(
                "WT-01", "Sensor", 28.0, "°C",
                DateTime.UtcNow, DeviceStatus.Normal));

            // Assert
            var alarm = logger.LoggedPackets.Single(
                p => p.Type == "Alarm");

            Assert.AreEqual(DeviceStatus.Alarm, alarm.Status);
            Assert.AreEqual(28.0, alarm.Value);
            Assert.AreEqual("ALARM: Water Temperature HIGH", alarm.Message);

            Assert.AreEqual(DeviceStatus.Alarm,
                controller.GetLatest("WT-01")!.Status);
        }

        [TestMethod]
        public void US4_WarningTemperature_AlertPublished()
        {
            // Arrange
            MessageBus bus = new MessageBus();
            var controller = new HydroFlow.Controller.SCADAController(bus);

            FakeLogger logger = new FakeLogger();
            bus.Subscribe(logger.Log);

            // Act
            bus.Publish(new TelemetryPacket(
                "WT-01", "Sensor", 26.0, "°C",
                DateTime.UtcNow, DeviceStatus.Normal));

            // Assert
            var alarm = logger.LoggedPackets.Single(
                p => p.Type == "Alarm");

            Assert.AreEqual(DeviceStatus.Warning, alarm.Status);
            Assert.AreEqual("WARNING: Water Temperature HIGH", alarm.Message);
        }

        [TestMethod]
        public void US4_LowTemperature_AlertPublished()
        {
            // Arrange
            MessageBus bus = new MessageBus();
            var controller = new HydroFlow.Controller.SCADAController(bus);

            FakeLogger logger = new FakeLogger();
            bus.Subscribe(logger.Log);

            // Act
            bus.Publish(new TelemetryPacket(
                "WT-01", "Sensor", 14.0, "°C",
                DateTime.UtcNow, DeviceStatus.Normal));

            // Assert
            var alarm = logger.LoggedPackets.Single(
                p => p.Type == "Alarm");

            Assert.AreEqual(DeviceStatus.Alarm, alarm.Status);
            Assert.AreEqual("ALARM: Water Temperature LOW", alarm.Message);
        }

        [TestMethod]
        public void US4_TemperatureReturnsToNormal()
        {
            // Arrange
            MessageBus bus = new MessageBus();
            var controller = new HydroFlow.Controller.SCADAController(bus);

            FakeLogger logger = new FakeLogger();
            bus.Subscribe(logger.Log);

            // Act
            bus.Publish(new TelemetryPacket(
                "WT-01", "Sensor", 28.0, "°C",
                DateTime.UtcNow, DeviceStatus.Normal));

            bus.Publish(new TelemetryPacket(
                "WT-01", "Sensor", 20.0, "°C",
                DateTime.UtcNow, DeviceStatus.Normal));

            // Assert
            var alarms = logger.LoggedPackets
                .Where(p => p.Type == "Alarm").ToList();

            Assert.AreEqual(2, alarms.Count);
            Assert.AreEqual(DeviceStatus.Alarm, alarms[0].Status);
            Assert.AreEqual(DeviceStatus.Normal, alarms[1].Status);
            Assert.AreEqual("NORMAL: Water Temperature", alarms[1].Message);

            Assert.AreEqual(DeviceStatus.Normal,
                controller.GetLatest("WT-01")!.Status);
        }

        [TestMethod]
        public void US4_AlertContainsCorrectDetails()
        {
            // Arrange
            MessageBus bus = new MessageBus();
            var controller = new HydroFlow.Controller.SCADAController(bus);

            FakeLogger logger = new FakeLogger();
            bus.Subscribe(logger.Log);

            // Act
            bus.Publish(new TelemetryPacket(
                "WT-01", "Sensor", 28.0, "°C",
                DateTime.UtcNow, DeviceStatus.Normal));

            // Assert
            var alarm = logger.LoggedPackets.Single(
                p => p.Type == "Alarm");

            Assert.AreEqual("WT-01", alarm.DeviceId);
            Assert.AreEqual("Alarm", alarm.Type);
            Assert.AreEqual(28.0, alarm.Value);
            Assert.AreEqual("°C", alarm.Unit);
            Assert.AreEqual(DeviceStatus.Alarm, alarm.Status);
            Assert.AreEqual("ALARM: Water Temperature HIGH", alarm.Message);
        }

        [TestMethod]
        public void US4_AlertLoggedWithTimestamp()
        {
            // Arrange
            MessageBus bus = new MessageBus();
            var controller = new HydroFlow.Controller.SCADAController(bus);

            FakeLogger logger = new FakeLogger();
            bus.Subscribe(logger.Log);

            DateTime before = DateTime.UtcNow;

            // Act
            bus.Publish(new TelemetryPacket(
                "WT-01", "Sensor", 28.0, "°C",
                DateTime.UtcNow, DeviceStatus.Normal));

            DateTime after = DateTime.UtcNow;

            // Assert
            var alarm = logger.LoggedPackets.Single(
                p => p.Type == "Alarm");

            Assert.IsTrue(alarm.Timestamp >= before);
            Assert.IsTrue(alarm.Timestamp <= after);
        }

        [TestMethod]
        public void US4_RepeatedTemperature_NoDuplicateAlert()
        {
            // Arrange
            MessageBus bus = new MessageBus();
            var controller = new HydroFlow.Controller.SCADAController(bus);

            FakeLogger logger = new FakeLogger();
            bus.Subscribe(logger.Log);

            // Act
            bus.Publish(new TelemetryPacket(
                "WT-01", "Sensor", 28.0, "°C",
                DateTime.UtcNow, DeviceStatus.Normal));

            bus.Publish(new TelemetryPacket(
                "WT-01", "Sensor", 29.0, "°C",
                DateTime.UtcNow, DeviceStatus.Normal));

            bus.Publish(new TelemetryPacket(
                "WT-01", "Sensor", 30.0, "°C",
                DateTime.UtcNow, DeviceStatus.Normal));

            // Assert
            Assert.AreEqual(1, logger.LoggedPackets
                .Count(p => p.Type == "Alarm"));

            Assert.AreEqual(3, logger.LoggedPackets
                .Count(p => p.Type == "Sensor"));

            Assert.AreEqual(30.0,
                controller.GetLatest("WT-01")!.Value);
        }
    }

    }

