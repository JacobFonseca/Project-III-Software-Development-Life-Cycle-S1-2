namespace HydroFlow.Core;

public enum DeviceStatus { Normal, Warning, Alarm }

public record TelemetryPacket(
    string DeviceId,
    string Type,          // "Sensor" or "Actuator"
    double Value,
    string Unit,
    DateTime Timestamp,
    DeviceStatus Status);