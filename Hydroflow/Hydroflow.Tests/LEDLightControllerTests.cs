/*
 * File: LEDLightControllerTests.cs
 * Author: Jacob Fonseca
 * Project: HydroFlow SCADA
 * Task: HF-54
 *
 * Unit tests for LEDLightController using a fake data source and a real MessageBus.
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
     * Purpose: Confirm a new controller starts off, Green, at 0 percent, without reading or publishing.
     */
    [TestMethod]
    public void Constructor_ValidArguments_StartsOffAndGreenWithoutReadingOrPublishing()
    {
        var source = new FakeDataSource(75);
        var bus = new MessageBus();
        int published = 0;
        bus.Subscribe(_ => published++);

        var controller = new LEDLightController("LED-01", "LED Grow Light", source, bus);

        Assert.IsFalse(controller.IsOn);
        Assert.AreEqual(0.0, controller.Setting);
        Assert.AreEqual(LedColor.Green, controller.Color);
        Assert.AreEqual("%", controller.GetPacket().Unit);
        Assert.AreEqual(0, source.ReadCount);
        Assert.AreEqual(0, published);
    }

    /*
     * Purpose: Confirm Execute turns the LED on and publishes the intensity in percent without reading.
     */
    [TestMethod]
    public void Execute_OnWithIntensity_SetsStateAndPublishesPercent()
    {
        var source = new FakeDataSource(75);
        var bus = new MessageBus();
        var received = new List<TelemetryPacket>();
        bus.Subscribe(p => received.Add(p));
        var controller = new LEDLightController("LED-01", "LED Grow Light", source, bus);

        controller.Execute(true, 60);

        Assert.IsTrue(controller.IsOn);
        Assert.AreEqual(60.0, controller.Setting);
        Assert.AreEqual(1, received.Count);
        Assert.AreEqual("LED-01", received[0].DeviceId);
        Assert.AreEqual("Actuator", received[0].Type);
        Assert.AreEqual(60.0, received[0].Value);
        Assert.AreEqual("%", received[0].Unit);
        Assert.AreEqual(DeviceStatus.Normal, received[0].Status);
        Assert.AreEqual(0, source.ReadCount);
    }

    /*
     * Purpose: Confirm Execute with off publishes 0 percent.
     */
    [TestMethod]
    public void Execute_Off_PublishesZeroPercent()
    {
        var bus = new MessageBus();
        var received = new List<TelemetryPacket>();
        bus.Subscribe(p => received.Add(p));
        var controller = new LEDLightController("LED-01", "LED Grow Light", new FakeDataSource(75), bus);

        controller.Execute(true, 60);
        controller.Execute(false, 0);

        Assert.IsFalse(controller.IsOn);
        Assert.AreEqual(0.0, received[^1].Value);
    }

    /*
     * Purpose: Confirm the 0 and 100 percent limits are accepted.
     */
    [TestMethod]
    [DataRow(0.0)]
    [DataRow(100.0)]
    public void Execute_BoundaryIntensity_IsAccepted(double intensity)
    {
        var controller = new LEDLightController("LED-01", "LED Grow Light", new FakeDataSource(75), new MessageBus());

        controller.Execute(true, intensity);

        Assert.AreEqual(intensity, controller.Setting);
        Assert.AreEqual(intensity, controller.GetPacket().Value);
    }

    /*
     * Purpose: Confirm an intensity outside 0 to 100 percent is rejected, the previous state is kept,
     * and nothing is published.
     */
    [TestMethod]
    [DataRow(-1.0)]
    [DataRow(100.5)]
    [DataRow(150.0)]
    [DataRow(double.NaN)]
    public void Execute_IntensityOutOfRange_ThrowsAndKeepsState(double intensity)
    {
        var bus = new MessageBus();
        int published = 0;
        bus.Subscribe(_ => published++);
        var controller = new LEDLightController("LED-01", "LED Grow Light", new FakeDataSource(75), bus);
        controller.Execute(true, 50);

        Assert.IsTrue(Throws<ArgumentOutOfRangeException>(() => controller.Execute(true, intensity)));

        Assert.IsTrue(controller.IsOn);
        Assert.AreEqual(50.0, controller.Setting);
        Assert.AreEqual(1, published);
    }

    /*
     * Purpose: Confirm Tick publishes 0 percent without reading while the LED is off.
     */
    [TestMethod]
    public void Tick_WhenOff_PublishesZeroWithoutReading()
    {
        var source = new FakeDataSource(75);
        var bus = new MessageBus();
        var received = new List<TelemetryPacket>();
        bus.Subscribe(p => received.Add(p));
        var controller = new LEDLightController("LED-01", "LED Grow Light", source, bus);

        TelemetryPacket packet = controller.Tick();

        Assert.AreEqual(0.0, packet.Value);
        Assert.AreEqual("%", packet.Unit);
        Assert.AreEqual(0, source.ReadCount);
        Assert.AreEqual(1, received.Count);
    }

    /*
     * Purpose: Confirm Tick reads one feedback value while on and publishes it in percent.
     */
    [TestMethod]
    public void Tick_WhenOn_PublishesFeedbackPercent()
    {
        var source = new FakeDataSource(82.5);
        var controller = new LEDLightController("LED-01", "LED Grow Light", source, new MessageBus());
        controller.Execute(true, 80);

        TelemetryPacket packet = controller.Tick();

        Assert.AreEqual(1, source.ReadCount);
        Assert.AreEqual("LED-01", packet.DeviceId);
        Assert.AreEqual("Actuator", packet.Type);
        Assert.AreEqual(82.5, packet.Value);
        Assert.AreEqual("%", packet.Unit);
        Assert.AreEqual(DeviceStatus.Normal, packet.Status);
    }

    /*
     * Purpose: Confirm the timestamp is UTC and taken during the Tick call.
     */
    [TestMethod]
    public void Tick_WhenOn_TimestampIsCurrentUtc()
    {
        var controller = new LEDLightController("LED-01", "LED Grow Light", new FakeDataSource(75), new MessageBus());
        controller.Execute(true, 80);

        DateTime before = DateTime.UtcNow;
        TelemetryPacket packet = controller.Tick();
        DateTime after = DateTime.UtcNow;

        Assert.AreEqual(DateTimeKind.Utc, packet.Timestamp.Kind);
        Assert.IsTrue(packet.Timestamp >= before && packet.Timestamp <= after);
    }

    /*
     * Purpose: Confirm each Tick while on returns the next value from the data source.
     */
    [TestMethod]
    public void Tick_CalledRepeatedly_ReadsSuccessiveValues()
    {
        var source = new FakeDataSource(75, 80, 85);
        var controller = new LEDLightController("LED-01", "LED Grow Light", source, new MessageBus());
        controller.Execute(true, 80);

        Assert.AreEqual(75.0, controller.Tick().Value);
        Assert.AreEqual(80.0, controller.Tick().Value);
        Assert.AreEqual(85.0, controller.Tick().Value);
        Assert.AreEqual(3, source.ReadCount);
    }

    /*
     * Purpose: Confirm polling after commands keeps the on state, intensity setting and colour.
     */
    [TestMethod]
    public void Tick_AfterCommands_KeepsCommandedState()
    {
        var controller = new LEDLightController("LED-01", "LED Grow Light", new FakeDataSource(75, 80, 85), new MessageBus());
        controller.SetColor(LedColor.White);
        controller.Execute(true, 80);

        controller.Tick();
        controller.Tick();
        controller.Tick();

        Assert.IsTrue(controller.IsOn);
        Assert.AreEqual(80.0, controller.Setting);
        Assert.AreEqual(LedColor.White, controller.Color);
    }

    /*
     * Purpose: Confirm GetPacket returns the packet from the last Tick
     * without reading the data source or publishing again.
     */
    [TestMethod]
    public void GetPacket_AfterTick_ReturnsSamePacketWithoutReadingOrPublishing()
    {
        var source = new FakeDataSource(75);
        var bus = new MessageBus();
        int published = 0;
        bus.Subscribe(_ => published++);
        var controller = new LEDLightController("LED-01", "LED Grow Light", source, bus);
        controller.Execute(true, 80);

        TelemetryPacket ticked = controller.Tick();
        TelemetryPacket cached = controller.GetPacket();

        Assert.AreSame(ticked, cached);
        Assert.AreEqual(1, source.ReadCount);
        Assert.AreEqual(2, published);
    }

    /*
     * Purpose: Confirm Tick sends the packet it returns to every subscriber.
     */
    [TestMethod]
    public void Tick_MultipleSubscribers_DeliversReturnedPacketToEach()
    {
        var bus = new MessageBus();
        var firstReceived = new List<TelemetryPacket>();
        var secondReceived = new List<TelemetryPacket>();
        bus.Subscribe(p => firstReceived.Add(p));
        bus.Subscribe(p => secondReceived.Add(p));
        var controller = new LEDLightController("LED-01", "LED Grow Light", new FakeDataSource(75), bus);

        TelemetryPacket returned = controller.Tick();

        Assert.AreEqual(1, firstReceived.Count);
        Assert.AreEqual(1, secondReceived.Count);
        Assert.AreSame(returned, firstReceived[0]);
        Assert.AreSame(returned, secondReceived[0]);
    }

    /*
     * Purpose: Confirm an exception from the data source reaches the caller unchanged
     * and Tick publishes nothing.
     */
    [TestMethod]
    public void Tick_DataSourceThrows_PropagatesExceptionAndPublishesNothing()
    {
        var expected = new InvalidOperationException("Data source failed.");
        var source = new FakeDataSource { ExceptionToThrow = expected };
        var bus = new MessageBus();
        int published = 0;
        bus.Subscribe(_ => published++);
        var controller = new LEDLightController("LED-01", "LED Grow Light", source, bus);
        controller.Execute(true, 80);

        try
        {
            controller.Tick();
            Assert.Fail("Expected exception");
        }
        catch (InvalidOperationException ex)
        {
            Assert.AreSame(expected, ex);
        }

        Assert.AreEqual(1, source.ReadCount);
        Assert.AreEqual(1, published);
    }

    /*
     * Purpose: Confirm SetColor accepts each supported colour.
     */
    [TestMethod]
    [DataRow(LedColor.Green)]
    [DataRow(LedColor.White)]
    public void SetColor_SupportedColor_ChangesColor(LedColor color)
    {
        var controller = new LEDLightController("LED-01", "LED Grow Light", new FakeDataSource(75), new MessageBus());

        controller.SetColor(color);

        Assert.AreEqual(color, controller.Color);
    }

    /*
     * Purpose: Confirm the SRS story change from Green to White and back works in sequence.
     */
    [TestMethod]
    public void SetColor_GreenToWhiteAndBack_TracksLatestCommand()
    {
        var controller = new LEDLightController("LED-01", "LED Grow Light", new FakeDataSource(75), new MessageBus());

        controller.SetColor(LedColor.White);
        Assert.AreEqual(LedColor.White, controller.Color);

        controller.SetColor(LedColor.Green);
        Assert.AreEqual(LedColor.Green, controller.Color);
    }

    /*
     * Purpose: Confirm an undefined colour is rejected and the current colour is kept.
     */
    [TestMethod]
    public void SetColor_UndefinedColor_ThrowsAndKeepsCurrentColor()
    {
        var controller = new LEDLightController("LED-01", "LED Grow Light", new FakeDataSource(75), new MessageBus());
        controller.SetColor(LedColor.White);

        Assert.IsTrue(Throws<ArgumentOutOfRangeException>(() => controller.SetColor((LedColor)99)));
        Assert.AreEqual(LedColor.White, controller.Color);
    }

    /*
     * Purpose: Confirm a colour command does not read the data source or publish a packet.
     */
    [TestMethod]
    public void SetColor_WithSubscriber_DoesNotReadOrPublish()
    {
        var source = new FakeDataSource(75);
        var bus = new MessageBus();
        int published = 0;
        bus.Subscribe(_ => published++);
        var controller = new LEDLightController("LED-01", "LED Grow Light", source, bus);

        controller.SetColor(LedColor.White);

        Assert.AreEqual(0, source.ReadCount);
        Assert.AreEqual(0, published);
    }

    // Same helper as in CoreTests.cs so tests work on any MSTest version.
    private static bool Throws<T>(Action action) where T : Exception
    {
        try { action(); return false; }
        catch (T) { return true; }
    }
}
