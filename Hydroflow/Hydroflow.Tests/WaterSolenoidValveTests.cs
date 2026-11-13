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
    public class WaterSolenoidValveTests
    {
        private string CreateTestFile()
        {
            string path = Path.GetTempFileName();

            File.WriteAllLines(path, new[]
            {
                "OPEN",
                "CLOSED"
            });

            return path;
        }

        [TestMethod]
        public void Valve_ReadsOpenState()
        {
            string path = CreateTestFile();

            try
            {
                MessageBus bus = new MessageBus();

                WaterSolenoidValve valve =
                    new WaterSolenoidValve("V01", path, bus);

                double result = valve.ReadState();

                Assert.AreEqual(1.0, result);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [TestMethod]
        public void Valve_LoopsData()
        {
            string path = CreateTestFile();

            try
            {
                MessageBus bus = new MessageBus();

                WaterSolenoidValve valve =
                    new WaterSolenoidValve("V01", path, bus);

                valve.ReadState();
                valve.ReadState();

                double result = valve.ReadState();

                Assert.AreEqual(1.0, result);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [TestMethod]
        public void Valve_PublishesThroughMessageBus()
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

                WaterSolenoidValve valve =
                    new WaterSolenoidValve("V01", path, bus);

                valve.PublishState();

                Assert.IsNotNull(receivedPacket);
                Assert.AreEqual("V01", receivedPacket.DeviceId);
                Assert.AreEqual("Actuator", receivedPacket.Type);
                Assert.AreEqual(1.0, receivedPacket.Value);
                Assert.AreEqual("State", receivedPacket.Unit);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
