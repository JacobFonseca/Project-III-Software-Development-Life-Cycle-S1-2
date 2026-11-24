using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HydroFlow.Controller;
using HydroFlow.Core;

namespace HydroFlow.Tests;

[TestClass]
public class ClimateAutomationTests
{
    private class TestFan : Actuator
    {
        public TestFan(string path, IMessageBus bus)
            : base("FAN-01", "Fan", "rpm", new LoopingFileReader(path), bus) { }
    }

    private static (MessageBus bus, TestFan fan, List<TelemetryPacket> packets) Build()
    {
        string path = Path.GetTempFileName();
        File.WriteAllLines(path, new[] { "1000" });
        var bus = new MessageBus();
        var packets = new List<TelemetryPacket>();
        bus.Subscribe(p => packets.Add(p));
        var fan = new TestFan(path, bus);
        _ = new ClimateAutomation(bus, new CommandInvoker(bus), fan, 1200);
        return (bus, fan, packets);
    }

    private static TelemetryPacket Humidity(DeviceStatus s, string type = "Alarm") =>
        new TelemetryPacket("HUM-01", type, 71, "%", DateTime.UtcNow, s, "msg");

    [TestMethod]
    public void Warning_TurnsFanOn()
    {
        var (bus, fan, _) = Build();
        bus.Publish(Humidity(DeviceStatus.Warning));
        Assert.IsTrue(fan.IsOn);
        Assert.AreEqual(1200, fan.Setting);
    }

    [TestMethod]
    public void BackToNormal_TurnsFanOff()
    {
        var (bus, fan, _) = Build();
        bus.Publish(Humidity(DeviceStatus.Warning));
        bus.Publish(Humidity(DeviceStatus.Normal));
        Assert.IsFalse(fan.IsOn);
    }

    [TestMethod]
    public void AlarmAfterWarning_DoesNotSendSecondOnCommand()
    {
        var (bus, _, packets) = Build();
        bus.Publish(Humidity(DeviceStatus.Warning));
        bus.Publish(Humidity(DeviceStatus.Alarm));
        Assert.AreEqual(1, packets.FindAll(p => p.Type == "Command").Count);
    }

    [TestMethod]
    public void OtherDevice_IsIgnored()
    {
        var (bus, fan, _) = Build();
        bus.Publish(new TelemetryPacket("PH-01", "Alarm", 7.1, "pH",
            DateTime.UtcNow, DeviceStatus.Alarm, "msg"));
        Assert.IsFalse(fan.IsOn);
    }

    [TestMethod]
    public void SensorPacket_IsIgnored()
    {
        var (bus, fan, _) = Build();
        bus.Publish(Humidity(DeviceStatus.Warning, "Sensor"));
        Assert.IsFalse(fan.IsOn);
    }
}