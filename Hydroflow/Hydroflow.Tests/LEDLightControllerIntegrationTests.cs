/*
 * File: LEDLightControllerIntegrationTests.cs
 * Author: Jacob Fonseca
 * Project: HydroFlow SCADA
 * Task: HF-54
 *
 * Integration test for the real data path: sample file -> LoopingFileReader ->
 * LEDLightController -> MessageBus -> subscriber, with a colour command in the
 * middle of polling. Uses the copy of data/led_light.txt in the test output folder
 * and only reads it. SetColour stands in for the shared command contract, which
 * is not available yet.
 */

using HydroFlow.Core;
using HydroFlow.Devices;

namespace HydroFlow.Tests;

[TestClass]
public class LEDLightControllerIntegrationTests
{
    /*
     * Purpose: Confirm seven published readings follow the sample file in order and wrap,
     * and that a colour command during polling is kept and does not skip a reading.
     */
    [TestMethod]
    public void PublishReading_SampleFileWithColourCommand_LoopsValuesAndKeepsColour()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "data", "led_light.txt");
        Assert.IsTrue(File.Exists(path), $"Sample data file was not copied to the test output: {path}");

        var reader = new LoopingFileReader(path);
        var bus = new MessageBus();
        var received = new List<TelemetryPacket>();
        bus.Subscribe(p => received.Add(p));
        var controller = new LEDLightController("LED-01", "LED Grow Light", reader, bus);

        controller.PublishReading();
        controller.PublishReading();

        // SRS lighting story: change the LED from green to white while polling continues.
        controller.SetColour(LedColour.White);

        for (int i = 0; i < 5; i++)
            controller.PublishReading();

        var expectedValues = new[] { 450.0, 480.0, 515.0, 450.0, 480.0, 515.0, 450.0 };
        CollectionAssert.AreEqual(expectedValues, received.Select(p => p.Value).ToArray());
        Assert.AreEqual(LedColour.White, controller.Colour);

        foreach (var packet in received)
        {
            Assert.AreEqual("LED-01", packet.DeviceId);
            Assert.AreEqual("Actuator", packet.Type);
            Assert.AreEqual("lux", packet.Unit);
            Assert.AreEqual(DeviceStatus.Normal, packet.Status);
            Assert.AreEqual(DateTimeKind.Utc, packet.Timestamp.Kind);
        }
    }
}
