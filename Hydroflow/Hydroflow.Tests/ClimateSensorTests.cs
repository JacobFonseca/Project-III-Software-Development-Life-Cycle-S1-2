using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HydroFlow.Core;
using HydroFlow.Devices;
namespace HydroFlow.Tests;

[TestClass]
public class ClimateSensorTests
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
    public void AirTemp_Tick_PublishesPacket()
    {
        var bus = new MessageBus(); var got = Collect(bus);
        new AirTempSensor("AIR-01", "Air Temperature", new FakeSource(23.5), bus).Tick();
        Assert.AreEqual(1, got.Count);
        Assert.AreEqual("AIR-01", got[0].DeviceId);
        Assert.AreEqual(23.5, got[0].Value);
        Assert.AreEqual("°C", got[0].Unit);
        Assert.AreEqual("Sensor", got[0].Type);
    }

    [TestMethod]
    public void Humidity_Tick_PublishesPacket()
    {
        var bus = new MessageBus(); var got = Collect(bus);
        new HumiditySensor("HUM-01", "Humidity", new FakeSource(71), bus).Tick();
        Assert.AreEqual("HUM-01", got[0].DeviceId);
        Assert.AreEqual(71, got[0].Value);
        Assert.AreEqual("%", got[0].Unit);
    }
}