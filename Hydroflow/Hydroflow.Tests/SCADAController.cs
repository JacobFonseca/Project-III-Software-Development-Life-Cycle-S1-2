using HydroFlow.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hydroflow.Tests
{
    internal class SCADAController
    {
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
    }
}
