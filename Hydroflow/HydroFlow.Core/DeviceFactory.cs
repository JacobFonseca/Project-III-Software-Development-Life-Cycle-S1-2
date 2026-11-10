/*
 * File: DeviceFactory.cs
 * Author: Jacob Fonseca
 * Project: HydroFlow SCADA
 * Task: HF-34
 *
 * Creates devices using registered creator functions.
 * Proposed API pending team confirmation.
 */

namespace HydroFlow.Core;

public class DeviceFactory
{
    // Store the creator for each device type. Type names ignore letter case.
    private readonly Dictionary<string, Func<string, string, Device>> _creators =
        new(StringComparer.OrdinalIgnoreCase);

    /*
     * Method: Register
     * Purpose: Register a function that creates a specific device type.
     */
    public void Register(string deviceType, Func<string, string, Device> creator)
    {
        // Check the type name and creator before storing them.
        if (string.IsNullOrWhiteSpace(deviceType))
            throw new ArgumentException("Device type is required.", nameof(deviceType));

        ArgumentNullException.ThrowIfNull(creator);

        // Reject duplicates so an existing registration stays intact.
        if (!_creators.TryAdd(deviceType, creator))
            throw new ArgumentException(
                $"Device type '{deviceType}' is already registered.",
                nameof(deviceType));
    }

    /*
     * Method: Create
     * Purpose: Call the registered creator and return its device.
     */
    public Device Create(string deviceType, string deviceId, string name)
    {
        // Check all arguments before calling the creator.
        if (string.IsNullOrWhiteSpace(deviceType))
            throw new ArgumentException("Device type is required.", nameof(deviceType));

        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Device id is required.", nameof(deviceId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Device name is required.", nameof(name));

        // Find the creator registered for this device type.
        if (!_creators.TryGetValue(deviceType, out var creator))
            throw new ArgumentException(
                $"Device type '{deviceType}' is not registered.",
                nameof(deviceType));

        // Pass the supplied ID and name to the creator.
        var device = creator(deviceId, name);

        // A creator must return a valid device.
        if (device is null)
            throw new InvalidOperationException(
                $"The creator for device type '{deviceType}' returned null.");

        return device;
    }
}