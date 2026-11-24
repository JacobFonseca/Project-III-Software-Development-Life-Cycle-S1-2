using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HydroFlow.Core;

namespace HydroFlow.Tests;


[TestClass]
public class AlarmTrackerTests
{
    private static TelemetryPacket Alarm(string id, DeviceStatus status, string msg) =>
        new TelemetryPacket(id, "Alarm", 7.1, "pH", DateTime.UtcNow, status, msg);

    [TestMethod]
    public void WarningPacket_AddsEntry()
    {
        var bus = new MessageBus();
        var tracker = new AlarmTracker(bus);

        bus.Publish(Alarm("PH-01", DeviceStatus.Warning, "WARNING: pH HIGH"));

        Assert.AreEqual(1, tracker.Active.Count);
        Assert.AreEqual("PH-01", tracker.Active[0].DeviceId);
        Assert.AreEqual("WARNING: pH HIGH", tracker.Active[0].Message);
        Assert.IsFalse(tracker.Active[0].IsCleared);
    }

    [TestMethod]
    public void SensorPacket_IsIgnored()
    {
        var bus = new MessageBus();
        var tracker = new AlarmTracker(bus);

        bus.Publish(new TelemetryPacket(
            "PH-01", "Sensor", 7.1, "pH", DateTime.UtcNow, DeviceStatus.Alarm));

        Assert.AreEqual(0, tracker.Active.Count);
    }

    [TestMethod]
    public void NormalAfterAlarm_KeepsEntryMarkedCleared()
    {
        var bus = new MessageBus();
        var tracker = new AlarmTracker(bus);

        bus.Publish(Alarm("PH-01", DeviceStatus.Alarm, "ALARM: pH HIGH"));
        bus.Publish(Alarm("PH-01", DeviceStatus.Normal, "NORMAL: pH"));

        Assert.AreEqual(1, tracker.Active.Count);
        Assert.IsTrue(tracker.Active[0].IsCleared);
    }

    [TestMethod]
    public void SecondAlarmForSameDevice_UpdatesWithoutDuplicating()
    {
        var bus = new MessageBus();
        var tracker = new AlarmTracker(bus);

        bus.Publish(Alarm("PH-01", DeviceStatus.Warning, "WARNING: pH HIGH"));
        bus.Publish(Alarm("PH-01", DeviceStatus.Alarm, "ALARM: pH HIGH"));

        Assert.AreEqual(1, tracker.Active.Count);
        Assert.AreEqual(DeviceStatus.Alarm, tracker.Active[0].Status);
    }

    [TestMethod]
    public void Acknowledge_RemovesEntryAndPublishesCommand()
    {
        var bus = new MessageBus();
        var packets = new List<TelemetryPacket>();
        bus.Subscribe(p => packets.Add(p));
        var tracker = new AlarmTracker(bus);

        bus.Publish(Alarm("PH-01", DeviceStatus.Alarm, "ALARM: pH HIGH"));
        tracker.Acknowledge("PH-01");

        Assert.AreEqual(0, tracker.Active.Count);
        var cmd = packets.Find(p => p.Type == "Command");
        Assert.IsNotNull(cmd);
        StringAssert.Contains(cmd.Message, "PH-01");
    }

    [TestMethod]
    public void Changed_IsRaisedWhenAlarmArrives()
    {
        var bus = new MessageBus();
        var tracker = new AlarmTracker(bus);
        int count = 0;
        tracker.Changed += () => count++;

        bus.Publish(Alarm("PH-01", DeviceStatus.Alarm, "ALARM: pH HIGH"));

        Assert.AreEqual(1, count);
    }
}