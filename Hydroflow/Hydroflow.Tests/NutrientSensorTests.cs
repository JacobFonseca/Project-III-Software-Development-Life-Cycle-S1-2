
using HydroFlow.Core;
using HydroFlow.Devices;
using HydroFlow.Devices.Nutrients;
using System;
using System.Collections.Generic;

namespace HydroFlow.Tests;

[TestClass]
public class NutrientSensorTests
{
    private class FakeSource : IDataSource
    {
        private readonly double[] _values;
        private int _index;

        public FakeSource(params double[] values)
        {
            _values = values;
        }

        public double ReadNext()
        {
            return _values[_index++ % _values.Length];
        }
    }

    private static List<TelemetryPacket> Collect(IMessageBus bus)
    {
        var list = new List<TelemetryPacket>();
        bus.Subscribe(p => list.Add(p));
        return list;
    }

    [TestMethod]
    public void pHSensor_Tick_PublishesCorrectData()
    {
        var bus = new MessageBus();
        var got = Collect(bus);

        var sensor = new pHSensor(
            "PH-01",
            "pH Sensor",
            new FakeSource(6.5),
            bus
        );

        sensor.Tick();

        Assert.AreEqual(1, got.Count);
        Assert.AreEqual("PH-01", got[0].DeviceId);
        Assert.AreEqual("Sensor", got[0].Type);
        Assert.AreEqual("pH", got[0].Unit);
        Assert.AreEqual(6.5, got[0].Value);
    }

    [TestMethod]
    public void NutrientECSensor_Tick_PublishesCorrectData()
    {
        var bus = new MessageBus();
        var got = Collect(bus);

        var sensor = new NutrientECSensor(
            "EC-01",
            "Nutrient EC Sensor",
            new FakeSource(1.8),
            bus
        );

        sensor.Tick();

        Assert.AreEqual(1, got.Count);
        Assert.AreEqual("EC-01", got[0].DeviceId);
        Assert.AreEqual("Sensor", got[0].Type);
        Assert.AreEqual("mS/cm", got[0].Unit);
        Assert.AreEqual(1.8, got[0].Value);
    }
}
