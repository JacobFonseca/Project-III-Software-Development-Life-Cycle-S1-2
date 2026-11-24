using HydroFlow.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace Hydroflow.Tests
{
    [TestClass]
    public class SCADAControllerTests
    {

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
        public void UnknownDevice_ReturnsNull()
        {
            MessageBus bus = new MessageBus();

            HydroFlow.Controller.SCADAController controller =
                new HydroFlow.Controller.SCADAController(bus);

            var result = controller.GetLatest("Unknown");

            Assert.IsNull(result);
        }

        [TestMethod]
        public void PublishedPacket_IsSaved()
        {
            MessageBus bus = new MessageBus();

            HydroFlow.Controller.SCADAController controller =
                new HydroFlow.Controller.SCADAController(bus);

            var packet = new TelemetryPacket(
                "Sensor1",
                "Sensor",
                25.0,
                "C",
                DateTime.Now,
                DeviceStatus.Normal
            );

            bus.Publish(packet);

            Assert.AreEqual(packet, controller.GetLatest("Sensor1"));
        }

        [TestMethod]
        public void NewPacket_ReplacesOldPacket()
        {
            MessageBus bus = new MessageBus();

            HydroFlow.Controller.SCADAController controller =
                new HydroFlow.Controller.SCADAController(bus);

            var oldPacket = new TelemetryPacket(
                "Sensor1", "Sensor", 20.0, "C",
                DateTime.Now, DeviceStatus.Normal
            );

            var newPacket = new TelemetryPacket(
                "Sensor1", "Sensor", 30.0, "C",
                DateTime.Now, DeviceStatus.Normal
            );

            bus.Publish(oldPacket);
            bus.Publish(newPacket);

            Assert.AreEqual(newPacket, controller.GetLatest("Sensor1"));
        }

        [TestMethod]
        public void Warning_IsLogged()
        {
            // Arrange
            MessageBus bus = new MessageBus();

            var controller =
                new HydroFlow.Controller.SCADAController(bus);

            FakeLogger logger = new FakeLogger();

            bus.Subscribe(logger.Log);

            var packet = new TelemetryPacket(
                "WL-01",
                "Sensor",
                35.0,
                "%",
                DateTime.UtcNow,
                DeviceStatus.Normal
            );

            // Act
            bus.Publish(packet);

            // Assert
            var alarm = logger.LoggedPackets.Single(
                p => p.Type == "Alarm");

            Assert.AreEqual(DeviceStatus.Warning, alarm.Status);
            Assert.AreEqual("WARNING: Water Level LOW", alarm.Message);
        }

        [TestMethod]
        public void Alarm_IsLogged()
        {
            // Arrange
            MessageBus bus = new MessageBus();
            var controller = new HydroFlow.Controller.SCADAController(bus);

            FakeLogger logger = new FakeLogger();
            bus.Subscribe(logger.Log);

            var packet = new TelemetryPacket(
                "WL-01", "Sensor", 25.0, "%",
                DateTime.UtcNow, DeviceStatus.Normal
            );

            // Act
            bus.Publish(packet);

            // Assert
            var alarm = logger.LoggedPackets.Single(
                p => p.Type == "Alarm");

            Assert.AreEqual(DeviceStatus.Alarm, alarm.Status);
            Assert.AreEqual("ALARM: Water Level LOW", alarm.Message);
        }

        [TestMethod]
        public void RecoveryToNormal_IsLogged()
        {
            // Arrange
            MessageBus bus = new MessageBus();
            var controller = new HydroFlow.Controller.SCADAController(bus);

            FakeLogger logger = new FakeLogger();
            bus.Subscribe(logger.Log);

            // Act
            bus.Publish(new TelemetryPacket(
                "WL-01", "Sensor", 25.0, "%",
                DateTime.UtcNow, DeviceStatus.Normal));

            bus.Publish(new TelemetryPacket(
                "WL-01", "Sensor", 50.0, "%",
                DateTime.UtcNow, DeviceStatus.Normal));

            // Assert
            var alarms = logger.LoggedPackets
                .Where(p => p.Type == "Alarm").ToList();

            Assert.AreEqual(2, alarms.Count);
            Assert.AreEqual(DeviceStatus.Alarm, alarms[0].Status);
            Assert.AreEqual(DeviceStatus.Normal, alarms[1].Status);
            Assert.AreEqual("NORMAL: Water Level", alarms[1].Message);
        }

        [TestMethod]
        public void UnchangedStatus_NoDuplicateAlarmLog()
        {
            // Arrange
            MessageBus bus = new MessageBus();
            var controller = new HydroFlow.Controller.SCADAController(bus);

            FakeLogger logger = new FakeLogger();
            bus.Subscribe(logger.Log);

            // Act
            bus.Publish(new TelemetryPacket(
                "WL-01", "Sensor", 25.0, "%",
                DateTime.UtcNow, DeviceStatus.Normal));

            bus.Publish(new TelemetryPacket(
                "WL-01", "Sensor", 20.0, "%",
                DateTime.UtcNow, DeviceStatus.Normal));

            bus.Publish(new TelemetryPacket(
                "WL-01", "Sensor", 15.0, "%",
                DateTime.UtcNow, DeviceStatus.Normal));

            // Assert
            var alarms = logger.LoggedPackets
                .Where(p => p.Type == "Alarm").ToList();

            Assert.AreEqual(1, alarms.Count);
        }

        [TestMethod]
        public void AlarmMessage_IsLoggedCorrectly()
        {
            // Arrange
            MessageBus bus = new MessageBus();
            var controller = new HydroFlow.Controller.SCADAController(bus);

            FakeLogger logger = new FakeLogger();
            bus.Subscribe(logger.Log);

            // Act
            bus.Publish(new TelemetryPacket(
                "PH-01", "Sensor", 7.5, "pH",
                DateTime.UtcNow, DeviceStatus.Normal));

            // Assert
            var alarm = logger.LoggedPackets.Single(
                p => p.Type == "Alarm");

            Assert.AreEqual("ALARM: pH HIGH", alarm.Message);
        }

        [TestMethod]
        public void AlarmPacket_ContainsCorrectDetails()
        {
            // Arrange
            MessageBus bus = new MessageBus();
            var controller = new HydroFlow.Controller.SCADAController(bus);

            FakeLogger logger = new FakeLogger();
            bus.Subscribe(logger.Log);

            // Act
            bus.Publish(new TelemetryPacket(
                "WT-01", "Sensor", 30.0, "°C",
                DateTime.UtcNow, DeviceStatus.Normal));

            // Assert
            var alarm = logger.LoggedPackets.Single(
                p => p.Type == "Alarm");

            Assert.AreEqual("WT-01", alarm.DeviceId);
            Assert.AreEqual("Alarm", alarm.Type);
            Assert.AreEqual(30.0, alarm.Value);
            Assert.AreEqual("°C", alarm.Unit);
            Assert.AreEqual(DeviceStatus.Alarm, alarm.Status);
            Assert.AreEqual("ALARM: Water Temperature HIGH", alarm.Message);
        }


        [TestMethod]
        public void MultipleDevices_AreLoggedIndependently()
        {
            // Arrange
            MessageBus bus = new MessageBus();
            var controller = new HydroFlow.Controller.SCADAController(bus);

            FakeLogger logger = new FakeLogger();
            bus.Subscribe(logger.Log);

            // Act
            bus.Publish(new TelemetryPacket(
                "WL-01", "Sensor", 25.0, "%",
                DateTime.UtcNow, DeviceStatus.Normal));

            bus.Publish(new TelemetryPacket(
                "PH-01", "Sensor", 7.5, "pH",
                DateTime.UtcNow, DeviceStatus.Normal));

            // Assert
            var alarms = logger.LoggedPackets
                .Where(p => p.Type == "Alarm").ToList();

            Assert.AreEqual(2, alarms.Count);
            Assert.AreEqual("WL-01", alarms[0].DeviceId);
            Assert.AreEqual("PH-01", alarms[1].DeviceId);
        }
    }
}
