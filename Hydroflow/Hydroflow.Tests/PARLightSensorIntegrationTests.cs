/*
 * File: PARLightSensorIntegrationTests.cs
 * Author: Jacob Fonseca
 * Project: HydroFlow SCADA
 * Task: HF-53
 *
 * Integration test for the real data path: sample file -> LoopingFileReader ->
 * PARLightSensor -> MessageBus -> subscriber. Uses the copy of data/par_light.txt
 * in the test output folder and only reads it.
 * The Device base class, lux unit and Normal status are provisional
 * integration decisions pending team confirmation.
 */

using HydroFlow.Core;
using HydroFlow.Devices;

namespace HydroFlow.Tests;

[TestClass]
public class PARLightSensorIntegrationTests
{
    /*
     * Purpose: Confirm seven published readings follow the sample file in order,
     * wrap back to the first value, and carry the expected packet details.
     */
    [TestMethod]
    public void PublishReading_SampleFileSevenTimes_DeliversValuesInLoopingOrder()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "data", "par_light.txt");
        Assert.IsTrue(File.Exists(path), $"Sample data file was not copied to the test output: {path}");

        var reader = new LoopingFileReader(path);
        var bus = new MessageBus();
        var received = new List<TelemetryPacket>();
        bus.Subscribe(p => received.Add(p));
        var sensor = new PARLightSensor("PAR-01", "PAR Light Sensor", reader, bus);

        for (int i = 0; i < 7; i++)
            sensor.PublishReading();

        var expectedValues = new[] { 450.0, 515.0, 400.0, 450.0, 515.0, 400.0, 450.0 };
        CollectionAssert.AreEqual(expectedValues, received.Select(p => p.Value).ToArray());

        foreach (var packet in received)
        {
            Assert.AreEqual("PAR-01", packet.DeviceId);
            Assert.AreEqual("Sensor", packet.Type);
            Assert.AreEqual("lux", packet.Unit);
            Assert.AreEqual(DeviceStatus.Normal, packet.Status);
            Assert.AreEqual(DateTimeKind.Utc, packet.Timestamp.Kind);
        }
    }
}
