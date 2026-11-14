
using HydroFlow.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HydroFlow.Tests
{


    [TestClass]
    public class WaterSolenoidValveTests
    {
        [TestMethod]
        public void Execute_OpensValve()
        {
            // Arrange
            string path = Path.GetTempFileName();

            try
            {
                File.WriteAllText(path, "1");

                var source = new LoopingFileReader(path);
                var bus = new MessageBus();

                var valve = new WaterSolenoidValve(
                    "VALVE01", "Water Valve", source, bus);

                // Act
                valve.Execute(true, 1);

                // Assert
                Assert.IsTrue(valve.IsOn);
                Assert.AreEqual(1.0, valve.Setting);
                Assert.AreEqual(1.0, valve.GetPacket().Value);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [TestMethod]
        public void Execute_ClosesValve()
        {
            // Arrange
            string path = Path.GetTempFileName();

            try
            {
                File.WriteAllText(path, "1");

                var source = new LoopingFileReader(path);
                var bus = new MessageBus();

                var valve = new WaterSolenoidValve(
                    "VALVE01", "Water Valve", source, bus);

                // Act
                valve.Execute(false, 0);

                // Assert
                Assert.IsFalse(valve.IsOn);
                Assert.AreEqual(0.0, valve.GetPacket().Value);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [TestMethod]
        public void Tick_LoopsValveReadings()
        {
            // Arrange
            string path = Path.GetTempFileName();

            try
            {
                File.WriteAllLines(path, new[]
                {
                "0", "1"
            });

                var source = new LoopingFileReader(path);
                var bus = new MessageBus();

                var valve = new WaterSolenoidValve(
                    "VALVE01", "Water Valve", source, bus);

                valve.Execute(true, 1);

                // Act
                double first = valve.Tick().Value;
                double second = valve.Tick().Value;
                double third = valve.Tick().Value;

                // Assert
                Assert.AreEqual(0.0, first);
                Assert.AreEqual(1.0, second);
                Assert.AreEqual(0.0, third);
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
                File.WriteAllText(path, "1");

                var source = new LoopingFileReader(path);
                var bus = new MessageBus();

                TelemetryPacket? received = null;
                bus.Subscribe(packet => received = packet);

                var valve = new WaterSolenoidValve(
                    "VALVE01", "Water Valve", source, bus);

                valve.Execute(true, 1);

                // Act
                TelemetryPacket result = valve.Tick();

                // Assert
                Assert.IsNotNull(received);
                Assert.AreEqual("VALVE01", received.DeviceId);
                Assert.AreEqual("Actuator", received.Type);
                Assert.AreEqual(1.0, received.Value);
                Assert.AreEqual("state", received.Unit);
                Assert.AreEqual(result, received);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
