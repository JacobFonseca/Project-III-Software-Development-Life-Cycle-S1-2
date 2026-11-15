
using System;
using System.Collections.Generic;
using HydroFlow.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hydroflow.Core;

[TestClass]
public class ActuatorTests
{
    private class FakeDataSource : IDataSource
    {
        public int ReadCount { get; private set; }

        public double ReadNext()
        {
            ReadCount++;
            return 42.5;
        }
    }

    private class FakeMessageBus : IMessageBus
    {
        public List<TelemetryPacket> Published { get; } = new();

        public void Subscribe(Action<TelemetryPacket> handler)
        {
        }

        public void Publish(TelemetryPacket packet)
        {
            Published.Add(packet);
        }
    }

    private class TestActuator : Actuator
    {
        public TestActuator(
            string id,
            string name,
            string unit,
            IDataSource feedback,
            IMessageBus bus)
            : base(id, name, unit, feedback, bus)
        {
        }
    }

    [TestMethod]
    public void Actuator_StartsOff()
    {
        var actuator = CreateActuator();

        Assert.IsFalse(actuator.IsOn);
        Assert.AreEqual(0.0, actuator.Setting);
    }

    [TestMethod]
    public void Execute_TurnsActuatorOn()
    {
        var actuator = CreateActuator();

        actuator.Execute(true, 75);

        Assert.IsTrue(actuator.IsOn);
        Assert.AreEqual(75.0, actuator.Setting);
    }

    [TestMethod]
    public void Execute_TurnsActuatorOff()
    {
        var actuator = CreateActuator();

        actuator.Execute(true, 75);
        actuator.Execute(false, 0);

        Assert.IsFalse(actuator.IsOn);
        Assert.AreEqual(0.0, actuator.Setting);
    }

    [TestMethod]
    public void Execute_PublishesTelemetry()
    {
        var bus = new FakeMessageBus();
        var actuator = CreateActuator(bus: bus);

        actuator.Execute(true, 50);

        Assert.AreEqual(1, bus.Published.Count);
        Assert.AreEqual("Actuator", bus.Published[0].Type);
        Assert.AreEqual(50.0, bus.Published[0].Value);
        Assert.AreEqual("A1", bus.Published[0].DeviceId);
    }

    [TestMethod]
    public void Tick_ReadsFeedbackWhenOn()
    {
        var feedback = new FakeDataSource();
        var actuator = CreateActuator(feedback: feedback);

        actuator.Execute(true, 75);
        var packet = actuator.Tick();

        Assert.AreEqual(42.5, packet.Value);
        Assert.AreEqual(1, feedback.ReadCount);
    }

    [TestMethod]
    public void Tick_PublishesZeroWhenOff()
    {
        var feedback = new FakeDataSource();
        var bus = new FakeMessageBus();
        var actuator = CreateActuator(feedback, bus);

        var packet = actuator.Tick();

        Assert.AreEqual(0.0, packet.Value);
        Assert.AreEqual(0, feedback.ReadCount);
        Assert.AreEqual(1, bus.Published.Count);
    }

    [TestMethod]
    public void GetPacket_ReturnsLatestPacket()
    {
        var actuator = CreateActuator();

        actuator.Execute(true, 60);
        var packet = actuator.GetPacket();

        Assert.AreEqual(60.0, packet.Value);
        Assert.AreEqual("Actuator", packet.Type);
        Assert.AreEqual("%", packet.Unit);
        Assert.AreEqual(DeviceStatus.Normal, packet.Status);
    }

    [TestMethod]
    public void Constructor_RejectsNullFeedback()
    {
        var bus = new FakeMessageBus();

        Assert.ThrowsException<ArgumentNullException>(() =>
    new TestActuator("A1", "Pump", "%", null!, bus));
    }

    private static TestActuator CreateActuator(
        FakeDataSource? feedback = null,
        FakeMessageBus? bus = null)
    {
        return new TestActuator(
            "A1",
            "Pump",
            "%",
            feedback ?? new FakeDataSource(),
            bus ?? new FakeMessageBus());
    }
}
