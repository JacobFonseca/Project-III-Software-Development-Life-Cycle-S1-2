/*
 * File: PARLightSensorTests.cs
 * Author: Jacob Fonseca
 * Project: HydroFlow SCADA
 * Task: HF-53
 *
 * Unit tests for PARLightSensor using a fake data source and a real MessageBus.
 * The Device base class, lux unit and Normal status are provisional
 * integration decisions pending team confirmation.
 */

using HydroFlow.Core;
using HydroFlow.Devices;

namespace HydroFlow.Tests;

[TestClass]
public class PARLightSensorTests
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
     * Purpose: Confirm the sensor rejects a null data source.
     */
    [TestMethod]
    public void Constructor_NullDataSource_ThrowsArgumentNullException()
    {
        var bus = new MessageBus();

        Assert.IsTrue(Throws<ArgumentNullException>(() =>
            new PARLightSensor("PAR-01", "PAR Light Sensor", null!, bus)));
    }

    /*
     * Purpose: Confirm the sensor rejects a null message bus.
     */
    [TestMethod]
    public void Constructor_NullMessageBus_ThrowsArgumentNullException()
    {
        var source = new FakeDataSource(100);

        Assert.IsTrue(Throws<ArgumentNullException>(() =>
            new PARLightSensor("PAR-01", "PAR Light Sensor", source, null!)));
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
        var source = new FakeDataSource(100);
        var bus = new MessageBus();

        Assert.IsTrue(Throws<ArgumentException>(() =>
            new PARLightSensor(deviceId!, "PAR Light Sensor", source, bus)));
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
        var source = new FakeDataSource(100);
        var bus = new MessageBus();

        Assert.IsTrue(Throws<ArgumentException>(() =>
            new PARLightSensor("PAR-01", name!, source, bus)));
    }

    /*
     * Purpose: Confirm one GetPacket call reads exactly one value.
     */
    [TestMethod]
    public void GetPacket_ValidReading_ReadsDataSourceOnce()
    {
        var source = new FakeDataSource(350.5);
        var sensor = new PARLightSensor("PAR-01", "PAR Light Sensor", source, new MessageBus());

        sensor.GetPacket();

        Assert.AreEqual(1, source.ReadCount);
    }

    /*
     * Purpose: Confirm the packet holds the sensor ID, type, value, unit and status.
     */
    [TestMethod]
    public void GetPacket_ValidReading_ReturnsPacketWithSensorDetails()
    {
        var source = new FakeDataSource(350.5);
        var sensor = new PARLightSensor("PAR-01", "PAR Light Sensor", source, new MessageBus());

        TelemetryPacket packet = sensor.GetPacket();

        Assert.AreEqual("PAR-01", packet.DeviceId);
        Assert.AreEqual("Sensor", packet.Type);
        Assert.AreEqual(350.5, packet.Value);
        Assert.AreEqual("lux", packet.Unit);
        Assert.AreEqual(DeviceStatus.Normal, packet.Status);
    }

    /*
     * Purpose: Confirm the timestamp is UTC and taken during the GetPacket call.
     */
    [TestMethod]
    public void GetPacket_ValidReading_TimestampIsCurrentUtc()
    {
        var sensor = new PARLightSensor("PAR-01", "PAR Light Sensor", new FakeDataSource(350.5), new MessageBus());

        DateTime before = DateTime.UtcNow;
        TelemetryPacket packet = sensor.GetPacket();
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
        var source = new FakeDataSource(100, 200, 300);
        var sensor = new PARLightSensor("PAR-01", "PAR Light Sensor", source, new MessageBus());

        Assert.AreEqual(100.0, sensor.GetPacket().Value);
        Assert.AreEqual(200.0, sensor.GetPacket().Value);
        Assert.AreEqual(300.0, sensor.GetPacket().Value);
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
        var sensor = new PARLightSensor("PAR-01", "PAR Light Sensor", new FakeDataSource(350.5), bus);

        sensor.GetPacket();

        Assert.AreEqual(0, published);
    }

    /*
     * Purpose: Confirm PublishReading reads once and sends the same single packet to every subscriber.
     */
    [TestMethod]
    public void PublishReading_MultipleSubscribers_DeliversOnePacketToEach()
    {
        var source = new FakeDataSource(350.5);
        var bus = new MessageBus();
        var firstReceived = new List<TelemetryPacket>();
        var secondReceived = new List<TelemetryPacket>();
        bus.Subscribe(p => firstReceived.Add(p));
        bus.Subscribe(p => secondReceived.Add(p));
        var sensor = new PARLightSensor("PAR-01", "PAR Light Sensor", source, bus);

        sensor.PublishReading();

        Assert.AreEqual(1, source.ReadCount);
        Assert.AreEqual(1, firstReceived.Count);
        Assert.AreEqual(1, secondReceived.Count);
        Assert.AreSame(firstReceived[0], secondReceived[0]);
        Assert.AreEqual("PAR-01", firstReceived[0].DeviceId);
        Assert.AreEqual(350.5, firstReceived[0].Value);
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
        var sensor = new PARLightSensor("PAR-01", "PAR Light Sensor", source, bus);

        try
        {
            sensor.PublishReading();
            Assert.Fail("Expected exception");
        }
        catch (InvalidOperationException ex)
        {
            Assert.AreSame(expected, ex);
        }

        Assert.AreEqual(1, source.ReadCount);
        Assert.AreEqual(0, published);
    }

    // Same helper as in CoreTests.cs so tests work on any MSTest version.
    private static bool Throws<T>(Action action) where T : Exception
    {
        try { action(); return false; }
        catch (T) { return true; }
    }
}