using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HydroFlow.Core;
using HydroFlow.Devices;
namespace HydroFlow.Tests;

[TestClass]
public class ExhaustFanControllerTests
{
    private class FakeSource : IDataSource
    {
        private readonly double[] _v; private int _i;
        public FakeSource(params double[] v) { _v = v; }
        public double ReadNext() => _v[_i++ % _v.Length];
    }

    private static List<TelemetryPacket> Collect(IMessageBus bus)
    {
        var list = new List<TelemetryPacket>();
        bus.Subscribe(p => list.Add(p));
        return list;
    }

    [TestMethod]
    public void Execute_SetsStateAndPublishes()
    {
        var bus = new MessageBus(); var got = Collect(bus);
        var fan = new ExhaustFanController("FAN-01", "Exhaust Fan", new FakeSource(900), bus);
        fan.Execute(true, 1200);
        Assert.IsTrue(fan.IsOn);
        Assert.AreEqual(1200, fan.Setting);
        Assert.AreEqual("FAN-01", got[0].DeviceId);
        Assert.AreEqual("rpm", got[0].Unit);
        Assert.AreEqual(1200, got[0].Value);
    }

    [TestMethod]
    public void Tick_WhenOff_PublishesZero()
    {
        var bus = new MessageBus(); var got = Collect(bus);
        new ExhaustFanController("FAN-01", "Exhaust Fan", new FakeSource(900), bus).Tick();
        Assert.AreEqual(0, got[0].Value);
    }

    [TestMethod]
    public void Tick_WhenOn_PublishesFeedback()
    {
        var bus = new MessageBus(); var got = Collect(bus);
        var fan = new ExhaustFanController("FAN-01", "Exhaust Fan", new FakeSource(900), bus);
        fan.Execute(true, 1200);
        fan.Tick();
        Assert.AreEqual(900, got[^1].Value);
    }
}