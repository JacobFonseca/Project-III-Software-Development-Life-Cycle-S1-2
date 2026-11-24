namespace HydroFlow.Core;

public enum DeviceStatus { Normal, Warning, Alarm }

public record TelemetryPacket(
    string DeviceId,
    string Type,          // "Sensor", "Actuator", "Alarm" or "Command"
    double Value,
    string Unit,
    DateTime Timestamp,
    DeviceStatus Status,
    string Message = "");