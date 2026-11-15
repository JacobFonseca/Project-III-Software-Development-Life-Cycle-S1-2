/*
 * File: LEDLightControllerIntegrationTests.cs
 * Author: Jacob Fonseca
 * Project: HydroFlow SCADA
 * Task: HF-54
 *
 * Integration test for the real path from led_intensity.txt through LoopingFileReader and LEDLightController to the MessageBus.
 */

using HydroFlow.Core;
using HydroFlow.Devices;

namespace HydroFlow.Tests;

[TestClass]
public class LEDLightControllerIntegrationTests
{
    /*
     * Purpose: Confirm an Execute command publishes the setting, seven Tick calls then publish
     * percent feedback that follows the sample file in order and wraps, and a colour change
     * during polling is kept.
     */
    [TestMethod]
    public void Tick_SampleFileAfterExecute_LoopsPercentValuesAndKeepsCommands()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "data", "led_intensity.txt");
        Assert.IsTrue(File.Exists(path), $"Sample data file was not copied to the test output: {path}");

        var reader = new LoopingFileReader(path);
        var bus = new MessageBus();
        var received = new List<TelemetryPacket>();
        bus.Subscribe(p => received.Add(p));
        var controller = new LEDLightController("LED-01", "LED Grow Light", reader, bus);

        controller.Execute(true, 80);
        controller.Tick();
        controller.Tick();

        // SRS lighting story: change the LED from green to white while polling continues.
        controller.SetColor(LedColor.White);

        for (int i = 0; i < 5; i++)
            controller.Tick();

        // First packet is the Execute setting, then seven feedback readings from the file.
        var expectedValues = new[] { 80.0, 75.0, 80.0, 85.0, 75.0, 80.0, 85.0, 75.0 };
        CollectionAssert.AreEqual(expectedValues, received.Select(p => p.Value).ToArray());
        Assert.IsTrue(controller.IsOn);
        Assert.AreEqual(80.0, controller.Setting);
        Assert.AreEqual(LedColor.White, controller.Color);

        foreach (var packet in received)
        {
            Assert.AreEqual("LED-01", packet.DeviceId);
            Assert.AreEqual("Actuator", packet.Type);
            Assert.AreEqual("%", packet.Unit);
            Assert.AreEqual(DeviceStatus.Normal, packet.Status);
            Assert.AreEqual(DateTimeKind.Utc, packet.Timestamp.Kind);
        }
    }
}
