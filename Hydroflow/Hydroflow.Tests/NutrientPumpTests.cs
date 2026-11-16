
using HydroFlow.Core;
using HydroFlow.Devices;
using HydroFlow.Devices.Nutrients;
using System;
using System.Collections.Generic;

namespace HydroFlow.Tests;

[TestClass]
public class NutrientPumpTests
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
    public void pHDosingPump_Execute_PublishesCorrectData()
    {
        var bus = new MessageBus();
        var got = Collect(bus);

        var pump = new pHDosingPump(
            "PHP-01",
            "pH Dosing Pump",
            new FakeSource(50),
            bus
        );

        pump.Execute(true, 60);

        Assert.IsTrue(pump.IsOn);
        Assert.AreEqual(60, pump.Setting);
        Assert.AreEqual("PHP-01", got[0].DeviceId);
        Assert.AreEqual("Actuator", got[0].Type);
        Assert.AreEqual("mL/min", got[0].Unit);
        Assert.AreEqual(60, got[0].Value);
    }

    [TestMethod]
    public void pHDosingPump_Tick_PublishesFeedback()
    {
        var bus = new MessageBus();
        var got = Collect(bus);

        var pump = new pHDosingPump(
            "PHP-01",
            "pH Dosing Pump",
            new FakeSource(50),
            bus
        );

        pump.Execute(true, 60);
        pump.Tick();

        Assert.AreEqual(50, got[^1].Value);
        Assert.AreEqual("PHP-01", got[^1].DeviceId);
        Assert.AreEqual("mL/min", got[^1].Unit);
    }

    [TestMethod]
    public void NutrientDosingPump_Execute_PublishesCorrectData()
    {
        var bus = new MessageBus();
        var got = Collect(bus);

        var pump = new NutrientDosingPump(
            "NUT-01",
            "Nutrient Dosing Pump",
            new FakeSource(80),
            bus
        );

        pump.Execute(true, 100);

        Assert.IsTrue(pump.IsOn);
        Assert.AreEqual(100, pump.Setting);
        Assert.AreEqual("NUT-01", got[0].DeviceId);
        Assert.AreEqual("Actuator", got[0].Type);
        Assert.AreEqual("mL/min", got[0].Unit);
        Assert.AreEqual(100, got[0].Value);
    }

    [TestMethod]
    public void NutrientDosingPump_Tick_PublishesFeedback()
    {
        var bus = new MessageBus();
        var got = Collect(bus);

        var pump = new NutrientDosingPump(
            "NUT-01",
            "Nutrient Dosing Pump",
            new FakeSource(80),
            bus
        );

        pump.Execute(true, 100);
        pump.Tick();

        Assert.AreEqual(80, got[^1].Value);
        Assert.AreEqual("NUT-01", got[^1].DeviceId);
        Assert.AreEqual("mL/min", got[^1].Unit);
    }

    [TestMethod]
    public void pHDosingPump_Tick_WhenOff_PublishesZero()
    {
        var bus = new MessageBus();
        var got = Collect(bus);

        var pump = new pHDosingPump(
            "PHP-01",
            "pH Dosing Pump",
            new FakeSource(50),
            bus
        );

        pump.Tick();

        Assert.AreEqual(0, got[0].Value);
    }

    [TestMethod]
    public void NutrientDosingPump_Tick_WhenOff_PublishesZero()
    {
        var bus = new MessageBus();
        var got = Collect(bus);

        var pump = new NutrientDosingPump(
            "NUT-01",
            "Nutrient Dosing Pump",
            new FakeSource(80),
            bus
        );

        pump.Tick();

        Assert.AreEqual(0, got[0].Value);
    }
}
