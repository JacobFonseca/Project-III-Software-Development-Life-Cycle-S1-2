/*
 * File: DeviceFactoryTests.cs
 * Author: Jacob Fonseca
 * Project: HydroFlow SCADA
 * Task: HF-34
 *
 * Tests device creation, registration, and invalid inputs.
 */

using HydroFlow.Core;

namespace HydroFlow.Tests;

[TestClass]
public class DeviceFactoryTests
{
    // Simple device used to test the factory.
    private class FakeDevice : Device
    {
        public FakeDevice(string id, string name) : base(id, name) { }
        public override TelemetryPacket GetPacket() =>
            new(DeviceId, "Sensor", 1.0, "x", DateTime.Now, DeviceStatus.Normal);
    }

    /*
     * Purpose: Check that the creator receives the supplied ID and name
     * and that the factory returns the device it creates.
     */
    [TestMethod]
    public void Create_RegisteredType_ReturnsCreatedDeviceWithSuppliedIdAndName()
    {
        var factory = new DeviceFactory();
        string? receivedId = null;
        string? receivedName = null;
        Device? created = null;
        factory.Register("AirTemp", (id, name) =>
        {
            receivedId = id;
            receivedName = name;
            created = new FakeDevice(id, name);
            return created;
        });

        var device = factory.Create("AirTemp", "AT-01", "Air Temperature Sensor");

        Assert.AreSame(created, device);
        Assert.AreEqual("AT-01", receivedId);
        Assert.AreEqual("Air Temperature Sensor", receivedName);
        Assert.AreEqual("AT-01", device.DeviceId);
        Assert.AreEqual("Air Temperature Sensor", device.Name);
    }

    /*
     * Purpose: Check that each Create call runs the registered creator.
     */
    [TestMethod]
    public void Create_CalledTwice_InvokesCreatorEachTime()
    {
        var factory = new DeviceFactory();
        int calls = 0;
        factory.Register("AirTemp", (id, name) =>
        {
            calls++;
            return new FakeDevice(id, name);
        });

        var first = factory.Create("AirTemp", "AT-01", "Air Temp 1");
        var second = factory.Create("AirTemp", "AT-02", "Air Temp 2");

        Assert.AreEqual(2, calls);
        Assert.AreNotSame(first, second);
    }

    /*
     * Purpose: Check that device type lookup ignores letter case.
     */
    [TestMethod]
    public void Create_DifferentCase_FindsRegisteredType()
    {
        var factory = new DeviceFactory();
        factory.Register("AirTemp", (id, name) => new FakeDevice(id, name));

        var device = factory.Create("AIRTEMP", "AT-01", "Air Temp");

        Assert.AreEqual("AT-01", device.DeviceId);
    }

    /*
     * Purpose: Reject duplicate types and keep the original creator.
     */
    [TestMethod]
    [DataRow("AirTemp")]
    [DataRow("AIRTEMP")]
    public void Register_DuplicateType_ThrowsArgumentExceptionAndKeepsOriginal(string duplicateType)
    {
        var factory = new DeviceFactory();
        int originalCalls = 0;
        int duplicateCalls = 0;
        factory.Register("AirTemp", (id, name) => { originalCalls++; return new FakeDevice(id, name); });

        Assert.IsTrue(Throws<ArgumentException>(() =>
            factory.Register(duplicateType, (id, name) => { duplicateCalls++; return new FakeDevice(id, name); })));

        factory.Create("AirTemp", "AT-01", "Air Temp");
        Assert.AreEqual(1, originalCalls);
        Assert.AreEqual(0, duplicateCalls);
    }

    /*
     * Purpose: Reject a device type that has not been registered.
     */
    [TestMethod]
    public void Create_UnregisteredType_ThrowsArgumentException()
    {
        var factory = new DeviceFactory();
        factory.Register("AirTemp", (id, name) => new FakeDevice(id, name));

        Assert.IsTrue(Throws<ArgumentException>(() => factory.Create("WaterTemp", "WT-01", "Water Temp")));
    }

    /*
     * Purpose: Reject null, empty, and whitespace types during registration.
     */
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Register_BlankType_ThrowsArgumentException(string? deviceType)
    {
        var factory = new DeviceFactory();

        Assert.IsTrue(Throws<ArgumentException>(() =>
            factory.Register(deviceType!, (id, name) => new FakeDevice(id, name))));
    }

    /*
     * Purpose: Reject null, empty, and whitespace types during creation.
     */
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Create_BlankType_ThrowsArgumentException(string? deviceType)
    {
        var factory = new DeviceFactory();
        factory.Register("AirTemp", (id, name) => new FakeDevice(id, name));

        Assert.IsTrue(Throws<ArgumentException>(() => factory.Create(deviceType!, "AT-01", "Air Temp")));
    }

    /*
     * Purpose: Reject a registration without a creator function.
     */
    [TestMethod]
    public void Register_NullCreator_ThrowsArgumentNullException()
    {
        var factory = new DeviceFactory();

        Assert.IsTrue(Throws<ArgumentNullException>(() => factory.Register("AirTemp", null!)));
    }

    /*
     * Purpose: Reject blank device IDs before the creator runs.
     */
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Create_BlankDeviceId_ThrowsBeforeInvokingCreator(string? deviceId)
    {
        var factory = new DeviceFactory();
        int calls = 0;
        factory.Register("AirTemp", (id, name) => { calls++; return new FakeDevice(id, name); });

        Assert.IsTrue(Throws<ArgumentException>(() => factory.Create("AirTemp", deviceId!, "Air Temp")));
        Assert.AreEqual(0, calls);
    }

    /*
     * Purpose: Reject blank device names before the creator runs.
     */
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Create_BlankName_ThrowsBeforeInvokingCreator(string? name)
    {
        var factory = new DeviceFactory();
        int calls = 0;
        factory.Register("AirTemp", (id, n) => { calls++; return new FakeDevice(id, n); });

        Assert.IsTrue(Throws<ArgumentException>(() => factory.Create("AirTemp", "AT-01", name!)));
        Assert.AreEqual(0, calls);
    }

    /*
     * Purpose: Reject a null device returned by the creator.
     */
    [TestMethod]
    public void Create_CreatorReturnsNull_ThrowsInvalidOperationException()
    {
        var factory = new DeviceFactory();
        factory.Register("AirTemp", (id, name) => null!);

        Assert.IsTrue(Throws<InvalidOperationException>(() => factory.Create("AirTemp", "AT-01", "Air Temp")));
    }

    /*
     * Purpose: Check that an exception from the creator reaches the caller unchanged.
     */
    [TestMethod]
    public void Create_CreatorThrows_PropagatesSameException()
    {
        var factory = new DeviceFactory();
        var expected = new NotSupportedException("Creator failed.");
        factory.Register("AirTemp", (id, name) => throw expected);

        try
        {
            factory.Create("AirTemp", "AT-01", "Air Temp");
            Assert.Fail("Expected exception");
        }
        catch (NotSupportedException ex)
        {
            Assert.AreSame(expected, ex);
        }
    }

    /*
     * Purpose: Return whether the action throws the expected exception type.
     */
    private static bool Throws<T>(Action action) where T : Exception
    {
        try { action(); return false; }
        catch (T) { return true; }
    }
}