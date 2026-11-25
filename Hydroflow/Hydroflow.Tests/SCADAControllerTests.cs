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
        private class FakeSource : IDataSource
        {
            public double ReadNext()
            {
                return 1;
            }
        }

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

        // US-6 test for automatic water refill

        [TestMethod]
        public void US6_NormalWaterLevel_ValveRemainsClosed()
        {
            // Arrange
            MessageBus bus = new MessageBus();
            CommandInvoker invoker = new CommandInvoker(bus);

            WaterSolenoidValve valve = new WaterSolenoidValve(
                "VLV-01", "Water Valve", new FakeSource(), bus);

            var controller = new HydroFlow.Controller.SCADAController(
                bus, invoker, valve);

            // Act
            bus.Publish(new TelemetryPacket(
                "WL-01", "Sensor", 82.0, "%",
                DateTime.UtcNow, DeviceStatus.Normal));

            // Assert
            Assert.IsFalse(valve.IsOn);
            Assert.AreEqual(0, invoker.History.Count);
            Assert.AreEqual(DeviceStatus.Normal,
                controller.GetLatest("WL-01")!.Status);
        }

        [TestMethod]
        public void US6_LowWaterLevel_AlarmPublished()
        {
            // Arrange
            MessageBus bus = new MessageBus();
            CommandInvoker invoker = new CommandInvoker(bus);

            WaterSolenoidValve valve = new WaterSolenoidValve(
                "VLV-01", "Water Valve", new FakeSource(), bus);

            var controller = new HydroFlow.Controller.SCADAController(
                bus, invoker, valve);

            FakeLogger logger = new FakeLogger();
            bus.Subscribe(logger.Log);

            // Act
            bus.Publish(new TelemetryPacket(
                "WL-01", "Sensor", 25.0, "%",
                DateTime.UtcNow, DeviceStatus.Normal));

            // Assert
            var alarm = logger.LoggedPackets.Single(
                p => p.Type == "Alarm");

            Assert.AreEqual("WL-01", alarm.DeviceId);
            Assert.AreEqual(25.0, alarm.Value);
            Assert.AreEqual(DeviceStatus.Alarm, alarm.Status);
            Assert.AreEqual("ALARM: Water Level LOW", alarm.Message);
        }

        [TestMethod]
        public void US6_LowWaterLevel_ValveOpens()
        {
            // Arrange
            MessageBus bus = new MessageBus();
            CommandInvoker invoker = new CommandInvoker(bus);

            WaterSolenoidValve valve = new WaterSolenoidValve(
                "VLV-01", "Water Valve", new FakeSource(), bus);

            var controller = new HydroFlow.Controller.SCADAController(
                bus, invoker, valve);

            // Act
            bus.Publish(new TelemetryPacket(
                "WL-01", "Sensor", 82.0, "%",
                DateTime.UtcNow, DeviceStatus.Normal));

            bus.Publish(new TelemetryPacket(
                "WL-01", "Sensor", 25.0, "%",
                DateTime.UtcNow, DeviceStatus.Normal));

            // Assert
            Assert.IsTrue(valve.IsOn);
            Assert.AreEqual(1.0, valve.Setting);
            Assert.AreEqual(1, invoker.History.Count);
        }

        [TestMethod]
        public void US6_RepeatedLowReadings_NoDuplicateCommands()
        {
            // Arrange
            MessageBus bus = new MessageBus();
            CommandInvoker invoker = new CommandInvoker(bus);

            WaterSolenoidValve valve = new WaterSolenoidValve(
                "VLV-01", "Water Valve", new FakeSource(), bus);

            var controller = new HydroFlow.Controller.SCADAController(
                bus, invoker, valve);

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
            Assert.IsTrue(valve.IsOn);
            Assert.AreEqual(1, invoker.History.Count);
        }

        [TestMethod]
        public void US6_WarningDuringRefill_ValveRemainsOpen()
        {
            // Arrange
            MessageBus bus = new MessageBus();
            CommandInvoker invoker = new CommandInvoker(bus);

            WaterSolenoidValve valve = new WaterSolenoidValve(
                "VLV-01", "Water Valve", new FakeSource(), bus);

            var controller = new HydroFlow.Controller.SCADAController(
                bus, invoker, valve);

            // Act
            bus.Publish(new TelemetryPacket(
                "WL-01", "Sensor", 25.0, "%",
                DateTime.UtcNow, DeviceStatus.Normal));

            bus.Publish(new TelemetryPacket(
                "WL-01", "Sensor", 35.0, "%",
                DateTime.UtcNow, DeviceStatus.Normal));

            // Assert
            Assert.IsTrue(valve.IsOn);
            Assert.AreEqual(1, invoker.History.Count);

            Assert.AreEqual(DeviceStatus.Warning,
                controller.GetLatest("WL-01")!.Status);
        }

        [TestMethod]
        public void US6_WaterLevelRecovers_ValveCloses()
        {
            // Arrange
            MessageBus bus = new MessageBus();
            CommandInvoker invoker = new CommandInvoker(bus);

            WaterSolenoidValve valve = new WaterSolenoidValve(
                "VLV-01", "Water Valve", new FakeSource(), bus);

            var controller = new HydroFlow.Controller.SCADAController(
                bus, invoker, valve);

            // Act
            bus.Publish(new TelemetryPacket(
                "WL-01", "Sensor", 25.0, "%",
                DateTime.UtcNow, DeviceStatus.Normal));

            Assert.IsTrue(valve.IsOn);

            bus.Publish(new TelemetryPacket(
                "WL-01", "Sensor", 82.0, "%",
                DateTime.UtcNow, DeviceStatus.Normal));

            // Assert
            Assert.IsFalse(valve.IsOn);
            Assert.AreEqual(0.0, valve.Setting);
            Assert.AreEqual(2, invoker.History.Count);

            Assert.AreEqual(DeviceStatus.Normal,
                controller.GetLatest("WL-01")!.Status);
        }


        [TestMethod]
        public void US6_RepeatedNormalReadings_NoDuplicateCloseCommands()
        {
            // Arrange
            MessageBus bus = new MessageBus();
            CommandInvoker invoker = new CommandInvoker(bus);

            WaterSolenoidValve valve = new WaterSolenoidValve(
                "VLV-01", "Water Valve", new FakeSource(), bus);

            var controller = new HydroFlow.Controller.SCADAController(
                bus, invoker, valve);

            // Act
            bus.Publish(new TelemetryPacket(
                "WL-01", "Sensor", 25.0, "%",
                DateTime.UtcNow, DeviceStatus.Normal));

            bus.Publish(new TelemetryPacket(
                "WL-01", "Sensor", 82.0, "%",
                DateTime.UtcNow, DeviceStatus.Normal));

            bus.Publish(new TelemetryPacket(
                "WL-01", "Sensor", 85.0, "%",
                DateTime.UtcNow, DeviceStatus.Normal));

            // Assert
            Assert.IsFalse(valve.IsOn);
            Assert.AreEqual(2, invoker.History.Count);
        }

        [TestMethod]
        public void US6_RefillCommands_AreLogged()
        {
            // Arrange
            MessageBus bus = new MessageBus();
            CommandInvoker invoker = new CommandInvoker(bus);

            WaterSolenoidValve valve = new WaterSolenoidValve(
                "VLV-01", "Water Valve", new FakeSource(), bus);

            var controller = new HydroFlow.Controller.SCADAController(
                bus, invoker, valve);

            FakeLogger logger = new FakeLogger();
            bus.Subscribe(logger.Log);

            DateTime before = DateTime.UtcNow;

            // Act
            bus.Publish(new TelemetryPacket(
                "WL-01", "Sensor", 25.0, "%",
                DateTime.UtcNow, DeviceStatus.Normal));

            bus.Publish(new TelemetryPacket(
                "WL-01", "Sensor", 82.0, "%",
                DateTime.UtcNow, DeviceStatus.Normal));

            DateTime after = DateTime.UtcNow;

            // Assert
            var commands = logger.LoggedPackets
                .Where(p => p.Type == "Command"
                    && p.DeviceId == "VLV-01")
                .ToList();

            var actuatorPackets = logger.LoggedPackets
                .Where(p => p.Type == "Actuator"
                    && p.DeviceId == "VLV-01")
                .ToList();

            Assert.AreEqual(2, commands.Count);
            Assert.AreEqual(2, actuatorPackets.Count);

            Assert.IsTrue(commands[0].Message.Contains("ON"));
            Assert.IsTrue(commands[1].Message.Contains("OFF"));

            Assert.AreEqual(1.0, actuatorPackets[0].Value);
            Assert.AreEqual(0.0, actuatorPackets[1].Value);

            foreach (var command in commands)
            {
                Assert.IsTrue(command.Timestamp >= before);
                Assert.IsTrue(command.Timestamp <= after);
            }
        }

        [TestMethod]
        public void US6_UnrelatedSensor_DoesNotControlValve()
        {
            // Arrange
            MessageBus bus = new MessageBus();
            CommandInvoker invoker = new CommandInvoker(bus);

            WaterSolenoidValve valve = new WaterSolenoidValve(
                "VLV-01", "Water Valve", new FakeSource(), bus);

            var controller = new HydroFlow.Controller.SCADAController(
                bus, invoker, valve);

            // Act
            bus.Publish(new TelemetryPacket(
                "WT-01", "Sensor", 28.0, "°C",
                DateTime.UtcNow, DeviceStatus.Normal));

            // Assert
            Assert.IsFalse(valve.IsOn);
            Assert.AreEqual(0, invoker.History.Count);
        }
    }
}
