using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HydroFlow.Core;

namespace HydroFlow.Tests;

[TestClass]
public class DeviceTests
{
    private class FakeDevice : Device
    {
        public FakeDevice(string id, string name) : base(id, name) { }
        public override TelemetryPacket GetPacket() =>
            new(DeviceId, "Sensor", 1.0, "x", DateTime.Now, DeviceStatus.Normal);
    }

    [TestMethod]
    public void Constructor_SetsIdAndName()
    {
        var d = new FakeDevice("PH-01", "pH Sensor");
        Assert.AreEqual("PH-01", d.DeviceId);
        Assert.AreEqual("pH Sensor", d.Name);
    }

    [TestMethod]
    public void Constructor_EmptyId_Throws()
    {
        Assert.IsTrue(Throws<ArgumentException>(() => new FakeDevice(" ", "name")));
    }

    [TestMethod]
    public void Constructor_EmptyName_Throws()
    {
        Assert.IsTrue(Throws<ArgumentException>(() => new FakeDevice("ID", "")));
    }

    // Small helper so tests work on any MSTest version.
    private static bool Throws<T>(Action action) where T : Exception
    {
        try { action(); return false; }
        catch (T) { return true; }
    }
}

[TestClass]
public class MessageBusTests
{
    private static TelemetryPacket Packet(string id, double v = 1) =>
        new(id, "Sensor", v, "u", DateTime.Now, DeviceStatus.Normal);

    [TestMethod]
    public void Publish_DeliversToSubscriber()
    {
        var bus = new MessageBus();
        TelemetryPacket? got = null;
        bus.Subscribe(p => got = p);
        var sent = Packet("PH-01", 6.2);
        bus.Publish(sent);
        Assert.AreEqual(sent, got);
    }

    [TestMethod]
    public void Publish_DeliversToAllSubscribers()
    {
        var bus = new MessageBus();
        int count = 0;
        bus.Subscribe(_ => count++);
        bus.Subscribe(_ => count++);
        bus.Subscribe(_ => count++);
        bus.Publish(Packet("A"));
        Assert.AreEqual(3, count);
    }

    [TestMethod]
    public void Publish_NoSubscribers_DoesNotThrow()
    {
        new MessageBus().Publish(Packet("A"));
    }

    [TestMethod]
    public void Publish_KeepsOrder()
    {
        var bus = new MessageBus();
        var ids = new List<string>();
        bus.Subscribe(p => ids.Add(p.DeviceId));
        bus.Publish(Packet("1"));
        bus.Publish(Packet("2"));
        bus.Publish(Packet("3"));
        CollectionAssert.AreEqual(new[] { "1", "2", "3" }, ids);
    }

    [TestMethod]
    public void Publish_NullPacket_Throws()
    {
        var bus = new MessageBus();
        try { bus.Publish(null!); Assert.Fail("Expected exception"); }
        catch (ArgumentNullException) { }
    }
}
