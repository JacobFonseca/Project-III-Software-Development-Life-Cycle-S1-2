using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using HydroFlow.Core;
using HydroFlow.Devices;
namespace HydroFlow.Tests;


[TestClass]
public class CommandInvokerTests
{
    private class TestActuator : Actuator
    {
        public TestActuator(string path, IMessageBus bus)
            : base("TST-01", "Test", "%", new LoopingFileReader(path), bus) { }

        protected override void ValidateSetting(double setting)
        {
            if (setting < 0 || setting > 100)
                throw new ArgumentOutOfRangeException(nameof(setting));
        }
    }

    private static (TestActuator actuator, CommandInvoker invoker, List<TelemetryPacket> packets) Build()
    {
        string path = Path.GetTempFileName();
        File.WriteAllLines(path, new[] { "5" });
        var bus = new MessageBus();
        var packets = new List<TelemetryPacket>();
        bus.Subscribe(p => packets.Add(p));
        return (new TestActuator(path, bus), new CommandInvoker(bus), packets);
    }

    [TestMethod]
    public void Run_ExecutesCommandOnActuator()
    {
        var (actuator, invoker, _) = Build();
        invoker.Run(new ActuatorCommand(actuator, true, 40));
        Assert.IsTrue(actuator.IsOn);
        Assert.AreEqual(40, actuator.Setting);
    }

    [TestMethod]
    public void Run_AddsCommandToHistory()
    {
        var (actuator, invoker, _) = Build();
        invoker.Run(new ActuatorCommand(actuator, true, 40));
        Assert.AreEqual(1, invoker.History.Count);
    }

    [TestMethod]
    public void Run_PublishesCommandPacketWithDescription()
    {
        var (actuator, invoker, packets) = Build();
        invoker.Run(new ActuatorCommand(actuator, true, 40));
        var cmd = packets.Find(p => p.Type == "Command");
        Assert.IsNotNull(cmd);
        Assert.AreEqual("TST-01", cmd.DeviceId);
        StringAssert.Contains(cmd.Message, "ON");
    }

    [TestMethod]
    public void Run_WhenActuatorRejectsSetting_PublishesNothingAndKeepsNoHistory()
    {
        var (actuator, invoker, packets) = Build();
        bool threw = false;
        try { invoker.Run(new ActuatorCommand(actuator, true, 101)); }
        catch (ArgumentOutOfRangeException) { threw = true; }

        Assert.IsTrue(threw);
        Assert.AreEqual(0, invoker.History.Count);
        Assert.IsFalse(packets.Exists(p => p.Type == "Command"));
    }
}