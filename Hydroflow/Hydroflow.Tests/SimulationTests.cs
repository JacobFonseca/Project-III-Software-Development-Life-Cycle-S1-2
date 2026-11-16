using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HydroFlow.Core;
using HydroFlow.Devices;
namespace HydroFlow.Tests;

[TestClass]
public class SimulationTests
{
    private class FakeSource : IDataSource
    {
        public double ReadNext() => 1;
    }

    [TestMethod]
    public void Tick_PublishesOnePacketPerRegisteredDevice()
    {
        var sim = new Simulation(_ => new FakeSource());
        var ids = new HashSet<string>();
        sim.Bus.Subscribe(p => ids.Add(p.DeviceId));
        sim.Tick();
        Assert.AreEqual(sim.Sensors.Count + sim.Actuators.Count, ids.Count);
    }
}