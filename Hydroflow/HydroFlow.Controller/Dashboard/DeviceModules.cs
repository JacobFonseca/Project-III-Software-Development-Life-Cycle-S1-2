/*
 * File: DeviceModules.cs
 * Author: Jacob Fonseca
 * Project: HydroFlow SCADA
 * Task: HF-59
 *
 * Says which dashboard module (Climate, Water, Nutrients, Lighting) each device class belongs to.
 */

using HydroFlow.Core;
using HydroFlow.Devices;
using HydroFlow.Devices.Nutrients;
using HydroFlow.Devices.Water;

namespace HydroFlow.Controller.Dashboard;

public static class DeviceModules
{
    public const string Climate = "Climate";
    public const string Water = "Water";
    public const string Nutrients = "Nutrients";
    public const string Lighting = "Lighting";

    // Used for a device class that is not in the list below, so its readings still show.
    public const string Other = "Other";

    // Order the groups appear on the dashboard.
    public static IReadOnlyList<string> DisplayOrder { get; } = new[] { Climate, Water, Nutrients, Lighting, Other };

    /*
     * Purpose: Find the module for a device from its class.
     * The device namespaces don't match their folders, so the class is checked directly.
     * The groups match the module comments in Simulation.
     */
    public static string For(Device device)
    {
        ArgumentNullException.ThrowIfNull(device);

        return device switch
        {
            AirTempSensor or HumiditySensor or ExhaustFanController => Climate,
            WaterLevelSensor or WaterTempSensor or WaterSolenoidValve => Water,
            pHSensor or pHDosingPump or NutrientECSensor or NutrientDosingPump => Nutrients,
            PARLightSensor or LEDLightController => Lighting,
            _ => Other
        };
    }

    /*
     * Purpose: Position of a module in DisplayOrder, used to sort rows.
     */
    public static int OrderOf(string module)
    {
        int index = DisplayOrder.ToList().IndexOf(module);
        return index < 0 ? DisplayOrder.Count : index;
    }
}
