/*
 * File: LEDLightControllerTests.cs
 * Author: Jacob Fonseca
 * Project: HydroFlow SCADA
 * Task: HF-54
 *
 * Unit tests for LEDLightController using a fake data source and a real MessageBus.
 * The Device base class, lux unit, Normal status, Green/White colours and the
 * SetColour method are provisional decisions pending team confirmation.
 */

using HydroFlow.Core;
using HydroFlow.Devices;

namespace HydroFlow.Tests;

[TestClass]
public class LEDLightControllerTests
{
    /*
     * Purpose: Fake data source that returns set values in order and counts how often it is read.
     * If ExceptionToThrow is set, ReadNext counts the call and then throws it.
     */
    private class FakeDataSource : IDataSource
    {
        private readonly double[] _values;

        public int ReadCount { get; private set; }
        public Exception? ExceptionToThrow { get; set; }

        public FakeDataSource(params double[] values)
        {
            _values = values;
        }

        public double ReadNext()
        {
            ReadCount++;
            if (ExceptionToThrow is not null)
                throw ExceptionToThrow;
            return _values[(ReadCount - 1) % _values.Length];
        }
    }

    /*
     * Purpose: Confirm the controller rejects a null data source.
     */
    [TestMethod]
    public void Constructor_NullDataSource_ThrowsArgumentNullException()
    {
        var bus = new MessageBus();

        Assert.IsTrue(Throws<ArgumentNullException>(() =>
            new LEDLightController("LED-01", "LED Grow Light", null!, bus)));
    }

    /*
     * Purpose: Confirm the controller rejects a null message bus.
     */
    [TestMethod]
    public void Constructor_NullMessageBus_ThrowsArgumentNullException()
    {
        var source = new FakeDataSource(450);

        Assert.IsTrue(Throws<ArgumentNullException>(() =>
            new LEDLightController("LED-01", "LED Grow Light", source, null!)));
    }

    /*
     * Purpose: Confirm the Device base constructor rejects a null, empty or whitespace ID.
     */
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Constructor_BlankDeviceId_ThrowsArgumentException(string? deviceId)
    {
        var source = new FakeDataSource(450);
        var bus = new MessageBus();

        Assert.IsTrue(Throws<ArgumentException>(() =>
            new LEDLightController(deviceId!, "LED Grow Light", source, bus)));
    }

    /*
     * Purpose: Confirm the Device base constructor rejects a null, empty or whitespace name.
     */
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Constructor_BlankName_ThrowsArgumentException(string? name)
    {
        var source = new FakeDataSource(450);
        var bus = new MessageBus();

        Assert.IsTrue(Throws<ArgumentException>(() =>
            new LEDLightController("LED-01", name!, source, bus)));
    }

    /*
     * Purpose: Confirm a new controller starts Green without reading or publishing.
     */
    [TestMethod]
    public void Constructor_ValidArguments_StartsGreenWithoutReading()
    {
        var source = new FakeDataSource(450);
        var bus = new MessageBus();
        int published = 0;
        bus.Subscribe(_ => published++);

        var controller = new LEDLightController("LED-01", "LED Grow Light", source, bus);

        Assert.AreEqual(LedColour.Green, controller.Colour);
        Assert.AreEqual(0, source.ReadCount);
        Assert.AreEqual(0, published);
    }

    /*
     * Purpose: Confirm one GetPacket call reads exactly one value.
     */
    [TestMethod]
    public void GetPacket_ValidReading_ReadsDataSourceOnce()
    {
        var source = new FakeDataSource(450);
        var controller = new LEDLightController("LED-01", "LED Grow Light", source, new MessageBus());

        controller.GetPacket();

        Assert.AreEqual(1, source.ReadCount);
    }

    /*
     * Purpose: Confirm the packet holds the controller ID, Actuator type, value, unit and status.
     */
    [TestMethod]
    public void GetPacket_ValidReading_ReturnsPacketWithActuatorDetails()
    {
        var source = new FakeDataSource(515.5);
        var controller = new LEDLightController("LED-01", "LED Grow Light", source, new MessageBus());

        TelemetryPacket packet = controller.GetPacket();

        Assert.AreEqual("LED-01", packet.DeviceId);
        Assert.AreEqual("Actuator", packet.Type);
        Assert.AreEqual(515.5, packet.Value);
        Assert.AreEqual("lux", packet.Unit);
        Assert.AreEqual(DeviceStatus.Normal, packet.Status);
    }

    /*
     * Purpose: Confirm the timestamp is UTC and taken during the GetPacket call.
     */
    [TestMethod]
    public void GetPacket_ValidReading_TimestampIsCurrentUtc()
    {
        var controller = new LEDLightController("LED-01", "LED Grow Light", new FakeDataSource(450), new MessageBus());

        DateTime before = DateTime.UtcNow;
        TelemetryPacket packet = controller.GetPacket();
        DateTime after = DateTime.UtcNow;

        Assert.AreEqual(DateTimeKind.Utc, packet.Timestamp.Kind);
        Assert.IsTrue(packet.Timestamp >= before && packet.Timestamp <= after);
    }

    /*
     * Purpose: Confirm each GetPacket call returns the next value from the data source.
     */
    [TestMethod]
    public void GetPacket_CalledRepeatedly_ReadsSuccessiveValues()
    {
        var source = new FakeDataSource(450, 480, 515);
        var controller = new LEDLightController("LED-01", "LED Grow Light", source, new MessageBus());

        Assert.AreEqual(450.0, controller.GetPacket().Value);
        Assert.AreEqual(480.0, controller.GetPacket().Value);
        Assert.AreEqual(515.0, controller.GetPacket().Value);
        Assert.AreEqual(3, source.ReadCount);
    }

    /*
     * Purpose: Confirm GetPacket only builds a packet and does not publish it.
     */
    [TestMethod]
    public void GetPacket_WithSubscriber_DoesNotPublish()
    {
        var bus = new MessageBus();
        int published = 0;
        bus.Subscribe(_ => published++);
        var controller = new LEDLightController("LED-01", "LED Grow Light", new FakeDataSource(450), bus);

        controller.GetPacket();

        Assert.AreEqual(0, published);
    }

    /*
     * Purpose: Confirm PublishReading reads once and sends the same single packet to every subscriber.
     */
    [TestMethod]
    public void PublishReading_MultipleSubscribers_DeliversOnePacketToEach()
    {
        var source = new FakeDataSource(450);
        var bus = new MessageBus();
        var firstReceived = new List<TelemetryPacket>();
        var secondReceived = new List<TelemetryPacket>();
        bus.Subscribe(p => firstReceived.Add(p));
        bus.Subscribe(p => secondReceived.Add(p));
        var controller = new LEDLightController("LED-01", "LED Grow Light", source, bus);

        controller.PublishReading();

        Assert.AreEqual(1, source.ReadCount);
        Assert.AreEqual(1, firstReceived.Count);
        Assert.AreEqual(1, secondReceived.Count);
        Assert.AreSame(firstReceived[0], secondReceived[0]);
        Assert.AreEqual("LED-01", firstReceived[0].DeviceId);
        Assert.AreEqual(450.0, firstReceived[0].Value);
    }

    /*
     * Purpose: Confirm an exception from the data source reaches the caller unchanged
     * and nothing is published.
     */
    [TestMethod]
    public void PublishReading_DataSourceThrows_PropagatesExceptionAndPublishesNothing()
    {
        var expected = new InvalidOperationException("Data source failed.");
        var source = new FakeDataSource { ExceptionToThrow = expected };
        var bus = new MessageBus();
        int published = 0;
        bus.Subscribe(_ => published++);
        var controller = new LEDLightController("LED-01", "LED Grow Light", source, bus);

        try
        {
            controller.PublishReading();
            Assert.Fail("Expected exception");
        }
        catch (InvalidOperationException ex)
        {
            Assert.AreSame(expected, ex);
        }

        Assert.AreEqual(1, source.ReadCount);
        Assert.AreEqual(0, published);
    }

    /*
     * Purpose: Confirm SetColour accepts each supported colour.
     */
    [TestMethod]
    [DataRow(LedColour.Green)]
    [DataRow(LedColour.White)]
    public void SetColour_SupportedColour_ChangesColour(LedColour colour)
    {
        var controller = new LEDLightController("LED-01", "LED Grow Light", new FakeDataSource(450), new MessageBus());

        controller.SetColour(colour);

        Assert.AreEqual(colour, controller.Colour);
    }

    /*
     * Purpose: Confirm the SRS story change from Green to White and back works in sequence.
     */
    [TestMethod]
    public void SetColour_GreenToWhiteAndBack_TracksLatestCommand()
    {
        var controller = new LEDLightController("LED-01", "LED Grow Light", new FakeDataSource(450), new MessageBus());

        controller.SetColour(LedColour.White);
        Assert.AreEqual(LedColour.White, controller.Colour);

        controller.SetColour(LedColour.Green);
        Assert.AreEqual(LedColour.Green, controller.Colour);
    }

    /*
     * Purpose: Confirm an undefined colour is rejected and the current colour is kept.
     */
    [TestMethod]
    public void SetColour_UndefinedColour_ThrowsAndKeepsCurrentColour()
    {
        var controller = new LEDLightController("LED-01", "LED Grow Light", new FakeDataSource(450), new MessageBus());
        controller.SetColour(LedColour.White);

        Assert.IsTrue(Throws<ArgumentOutOfRangeException>(() => controller.SetColour((LedColour)99)));
        Assert.AreEqual(LedColour.White, controller.Colour);
    }

    /*
     * Purpose: Confirm a colour command does not consume a reading or publish a packet.
     */
    [TestMethod]
    public void SetColour_WithSubscriber_DoesNotReadOrPublish()
    {
        var source = new FakeDataSource(450);
        var bus = new MessageBus();
        int published = 0;
        bus.Subscribe(_ => published++);
        var controller = new LEDLightController("LED-01", "LED Grow Light", source, bus);

        controller.SetColour(LedColour.White);

        Assert.AreEqual(0, source.ReadCount);
        Assert.AreEqual(0, published);
    }

    /*
     * Purpose: Confirm polling after a command keeps the commanded colour and
     * continues reading the next simulated values.
     */
    [TestMethod]
    public void PublishReading_AfterSetColour_KeepsCommandedColour()
    {
        var source = new FakeDataSource(450, 480, 515);
        var bus = new MessageBus();
        var received = new List<TelemetryPacket>();
        bus.Subscribe(p => received.Add(p));
        var controller = new LEDLightController("LED-01", "LED Grow Light", source, bus);

        controller.PublishReading();
        controller.SetColour(LedColour.White);
        controller.PublishReading();
        controller.PublishReading();

        Assert.AreEqual(LedColour.White, controller.Colour);
        CollectionAssert.AreEqual(new[] { 450.0, 480.0, 515.0 }, received.Select(p => p.Value).ToArray());
    }

    // Same helper as in CoreTests.cs so tests work on any MSTest version.
    private static bool Throws<T>(Action action) where T : Exception
    {
        try { action(); return false; }
        catch (T) { return true; }
    }
}
