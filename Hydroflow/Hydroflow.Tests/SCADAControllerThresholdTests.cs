using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HydroFlow.Controller;
using HydroFlow.Core;

namespace Hydroflow.Tests;


[TestClass]
public class SCADAControllerThresholdTests
{
private static (MessageBus bus, List<TelemetryPacket> alarms, SCADAController controller) Build()
{
    var bus = new MessageBus();
    var alarms = new List<TelemetryPacket>();
    bus.Subscribe(p =>
    {
        if (p.Type == "Alarm") alarms.Add(p);
    });
    return (bus, alarms, new SCADAController(bus));
}

private static TelemetryPacket Sensor(string id, string unit, double value, string type = "Sensor") =>
    new TelemetryPacket(id, type, value, unit, DateTime.UtcNow, DeviceStatus.Normal);

// ---- Normal / Warning / Alarm for each of the six devices ----

[TestMethod]
[DataRow("PH-01", "pH", 6.0, DeviceStatus.Normal)]
[DataRow("PH-01", "pH", 6.7, DeviceStatus.Warning)]
[DataRow("PH-01", "pH", 5.6, DeviceStatus.Warning)]
[DataRow("PH-01", "pH", 7.1, DeviceStatus.Alarm)]
[DataRow("PH-01", "pH", 5.0, DeviceStatus.Alarm)]
[DataRow("WT-01", "°C", 20.0, DeviceStatus.Normal)]
[DataRow("WT-01", "°C", 26.0, DeviceStatus.Warning)]
[DataRow("WT-01", "°C", 28.0, DeviceStatus.Alarm)]
[DataRow("WT-01", "°C", 14.0, DeviceStatus.Alarm)]
[DataRow("WL-01", "%", 60.0, DeviceStatus.Normal)]
[DataRow("WL-01", "%", 35.0, DeviceStatus.Warning)]
[DataRow("WL-01", "%", 25.0, DeviceStatus.Alarm)]
[DataRow("HUM-01", "%", 65.0, DeviceStatus.Normal)]
[DataRow("HUM-01", "%", 71.0, DeviceStatus.Warning)]
[DataRow("HUM-01", "%", 78.0, DeviceStatus.Alarm)]
[DataRow("AIR-01", "°C", 23.0, DeviceStatus.Normal)]
[DataRow("AIR-01", "°C", 27.0, DeviceStatus.Warning)]
[DataRow("AIR-01", "°C", 29.0, DeviceStatus.Alarm)]
[DataRow("EC-01", "mS/cm", 1.8, DeviceStatus.Normal)]
[DataRow("EC-01", "mS/cm", 1.2, DeviceStatus.Warning)]
[DataRow("EC-01", "mS/cm", 2.4, DeviceStatus.Warning)]
[DataRow("EC-01", "mS/cm", 0.8, DeviceStatus.Alarm)]
[DataRow("EC-01", "mS/cm", 2.8, DeviceStatus.Alarm)]
public void Reading_IsClassifiedCorrectly(string id, string unit, double value, DeviceStatus expected)
{
    var (bus, alarms, _) = Build();

    bus.Publish(Sensor(id, unit, value));

    if (expected == DeviceStatus.Normal)
    {
        Assert.AreEqual(0, alarms.Count);   // Normal start + Normal reading = no alarm packet
    }
    else
    {
        Assert.AreEqual(1, alarms.Count);
        Assert.AreEqual(expected, alarms[0].Status);
        Assert.AreEqual(id, alarms[0].DeviceId);
    }
}

// ---- Publish only when the status changes ----

[TestMethod]
public void SameStatusTwice_PublishesOnlyOneAlarm()
{
    var (bus, alarms, _) = Build();

    bus.Publish(Sensor("PH-01", "pH", 7.1));
    bus.Publish(Sensor("PH-01", "pH", 7.2));   // still Alarm

    Assert.AreEqual(1, alarms.Count);
}

[TestMethod]
public void ReturnToNormal_PublishesNormalAlarmPacket()
{
    var (bus, alarms, _) = Build();

    bus.Publish(Sensor("PH-01", "pH", 7.1));
    bus.Publish(Sensor("PH-01", "pH", 6.0));

    Assert.AreEqual(2, alarms.Count);
    Assert.AreEqual(DeviceStatus.Normal, alarms[1].Status);
    Assert.AreEqual("NORMAL: pH", alarms[1].Message);
}

[TestMethod]
public void AlarmPacket_CarriesSensorValueAndUnit()
{
    var (bus, alarms, _) = Build();

    bus.Publish(Sensor("PH-01", "pH", 7.1));

    Assert.AreEqual(7.1, alarms[0].Value, 0.0001);
    Assert.AreEqual("pH", alarms[0].Unit);
    Assert.AreEqual("Alarm", alarms[0].Type);
}

// ---- Message text ----

[TestMethod]
[DataRow("PH-01", "pH", 7.1, "ALARM: pH HIGH")]
[DataRow("PH-01", "pH", 5.0, "ALARM: pH LOW")]
[DataRow("HUM-01", "%", 71.0, "WARNING: Humidity HIGH")]
[DataRow("WL-01", "%", 25.0, "ALARM: Water Level LOW")]
[DataRow("WT-01", "°C", 28.0, "ALARM: Water Temperature HIGH")]
[DataRow("EC-01", "mS/cm", 1.2, "WARNING: Nutrient EC LOW")]
public void Message_HasExpectedText(string id, string unit, double value, string expected)
{
    var (bus, alarms, _) = Build();

    bus.Publish(Sensor(id, unit, value));

    Assert.AreEqual(1, alarms.Count);
    Assert.AreEqual(expected, alarms[0].Message);
}

    // ---- Things that must be ignored ----
[TestMethod]

    [DataRow("Actuator")]
    [DataRow("Command")]
    public void NonSensorPackets_AreIgnored(string type)
    {
        var (bus, alarms, _) = Build();

        bus.Publish(Sensor("PH-01", "pH", 7.1, type));

        Assert.AreEqual(0, alarms.Count);
    }

[TestMethod]
    public void AlarmPackets_AreNotReEvaluated()
    {
        var (bus, alarms, _) = Build();

        // Inject one Alarm packet ourselves; the controller must not react to it.
        bus.Publish(new TelemetryPacket(
            "PH-01", "Alarm", 7.1, "pH", DateTime.UtcNow, DeviceStatus.Alarm, "ALARM: pH HIGH"));

        Assert.AreEqual(1, alarms.Count);   // only the one we injected, no extra and no loop
    }


[TestMethod]
public void DeviceWithoutThresholds_NeverRaisesAlarm()
{
    var (bus, alarms, _) = Build();

    bus.Publish(Sensor("PAR-01", "lux", 9999));

    Assert.AreEqual(0, alarms.Count);
}

// ---- GetLatest still works ----

[TestMethod]
public void GetLatest_ReturnsLastSensorReading()
{
    var (bus, _, controller) = Build();

    bus.Publish(Sensor("PH-01", "pH", 6.2));
    bus.Publish(Sensor("PH-01", "pH", 6.4));

    Assert.AreEqual(6.4, controller.GetLatest("PH-01")!.Value, 0.0001);
}

[TestMethod]
public void GetLatest_UnknownDevice_ReturnsNull()
{
    var (_, _, controller) = Build();

    Assert.IsNull(controller.GetLatest("NOPE-01"));
}
}