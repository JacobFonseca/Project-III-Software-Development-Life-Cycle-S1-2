using HydroFlow.Core;
using HydroFlow.Devices.Water;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace HydroFlow.Devices;

public class Simulation
{
    private readonly List<Sensor> _sensors = new();
    private readonly List<Actuator> _actuators = new();

    public IMessageBus Bus { get; } = new MessageBus();
    public IReadOnlyList<Sensor> Sensors => _sensors;
    public IReadOnlyList<Actuator> Actuators => _actuators;

    // sourceFor turns a data file name into a data source.
    public Simulation(Func<string, IDataSource> sourceFor)
    {
        ArgumentNullException.ThrowIfNull(sourceFor);

        // Climate (Afsana)
        _sensors.Add(new AirTempSensor("AIR-01", "Air Temperature", sourceFor("air_temp.txt"), Bus));
        _sensors.Add(new HumiditySensor("HUM-01", "Humidity", sourceFor("humidity.txt"), Bus));
        _actuators.Add(new ExhaustFanController("FAN-01", "Exhaust Fan", sourceFor("fan_rpm.txt"), Bus));

        // Water (Evangeline)
        _sensors.Add(new WaterLevelSensor("WL-01", "Water Level", sourceFor("water_level.txt"), Bus));
        _sensors.Add(new WaterTempSensor("WT-01", "Water Temperature", sourceFor("water_temp.txt"), Bus));
        _actuators.Add(new WaterSolenoidValve("VLV-01", "Water Valve", sourceFor("water_solenoid_valve.txt"), Bus));

        // Uncomment each line only after that teammate's PR is merged and the build passes:
        // Nutrient (Kadeema)
        // _sensors.Add(new pHSensor("PH-01", "pH Sensor", sourceFor("ph.txt"), Bus));
        // _actuators.Add(new pHDosingPump("PHP-01", "pH Dosing Pump", sourceFor("ph_pump.txt"), Bus));
        // _sensors.Add(new NutrientECSensor("EC-01", "Nutrient EC", sourceFor("ec.txt"), Bus));
        // _actuators.Add(new NutrientDosingPump("NUT-01", "Nutrient Pump", sourceFor("nutrient_pump.txt"), Bus));

        // Lighting (Jacob)
        // _sensors.Add(new PARLightSensor("PAR-01", "PAR Light", sourceFor("par_light.txt"), Bus));
        // _actuators.Add(new LEDLightController("LED-01", "LED Lights", sourceFor("led_intensity.txt"), Bus));
    }

    public void Tick()
    {
        foreach (var s in _sensors) s.Tick();
        foreach (var a in _actuators) a.Tick();
    }
}