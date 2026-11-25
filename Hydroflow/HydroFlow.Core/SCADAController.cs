/*
 * File: SCADAController.cs
 * Author: Evangeline Singh
 * Project: HydroFlow SCADA
 * Task: HF-100
 *
 * SCADA controller that manages telemetry packets and evaluates device statuses.
 */
using System;
using System.Collections.Generic;
using HydroFlow.Core;

namespace HydroFlow.Controller
{
    public class SCADAController
    {
        private readonly Dictionary<string, TelemetryPacket> packets = new();
        private readonly Dictionary<string, DeviceStatus> previousStatuses = new();
        private readonly IMessageBus bus;

        // Used for automatic water refill.
        private readonly CommandInvoker? invoker;
        private readonly Actuator? waterValve;

        public SCADAController(
    IMessageBus bus,
    CommandInvoker? invoker = null,
    Actuator? waterValve = null)
        {
            this.bus = bus ?? throw new ArgumentNullException(nameof(bus));

            this.invoker = invoker;
            this.waterValve = waterValve;

            bus.Subscribe(SavePacket);
        }
        private void SavePacket(TelemetryPacket packet)
        {
            // Only evaluate sensor readings.
            if (packet.Type != "Sensor")
                return;

            // Calculate the status using the sensor thresholds.
            DeviceStatus newStatus = CheckStatus(packet);

            // Save the latest sensor reading with its evaluated status.
            packets[packet.DeviceId] = packet with
            {
                Status = newStatus
            };

            // A new device starts with an assumed Normal status.
            DeviceStatus oldStatus = previousStatuses.TryGetValue(
                packet.DeviceId, out DeviceStatus previous)
                ? previous
                : DeviceStatus.Normal;

            previousStatuses[packet.DeviceId] = newStatus;

            // Publish only when the status changes.
            if (oldStatus != newStatus)
            {
                string message = CreateAlarmMessage(packet, newStatus);

                TelemetryPacket alarmPacket = new TelemetryPacket(
                    packet.DeviceId,
                    "Alarm",
                    packet.Value,
                    packet.Unit,
                    DateTime.UtcNow,
                    newStatus,
                    message
                );

                bus.Publish(alarmPacket);
            }
                // Automatically control the water valve.
                if (packet.DeviceId == "WL-01")
                {
                    ControlWaterRefill(newStatus);
                }
        }

        private void ControlWaterRefill(DeviceStatus status)
        {
            // Automatic refill requires a valve and command invoker.
            if (invoker == null || waterValve == null)
                return;

            // Open the valve when the water level is too low.
            if (status == DeviceStatus.Alarm && !waterValve.IsOn)
            {
                invoker.Run(new ActuatorCommand(
                    waterValve, true, 1));
            }

            // Close the valve when the water level returns to normal.
            else if (status == DeviceStatus.Normal && waterValve.IsOn)
            {
                invoker.Run(new ActuatorCommand(
                    waterValve, false, 0));
            }
        }

        private DeviceStatus CheckStatus(TelemetryPacket packet)
        {
            double value = packet.Value;

            switch (packet.DeviceId)
            {
                // pH
                case "PH-01":
                    if (value < 5.5 || value > 7.0)
                        return DeviceStatus.Alarm;

                    if (value < 5.8 || value > 6.5)
                        return DeviceStatus.Warning;

                    return DeviceStatus.Normal;

                // Water Temperature
                case "WT-01":
                    if (value < 15 || value > 27)
                        return DeviceStatus.Alarm;

                    if (value > 25)
                        return DeviceStatus.Warning;

                    return DeviceStatus.Normal;

                // Water Level
                case "WL-01":
                    if (value < 30)
                        return DeviceStatus.Alarm;

                    if (value < 40)
                        return DeviceStatus.Warning;

                    return DeviceStatus.Normal;

                // Humidity
                case "HUM-01":
                    if (value > 75)
                        return DeviceStatus.Alarm;

                    if (value > 70)
                        return DeviceStatus.Warning;

                    return DeviceStatus.Normal;

                // Air Temperature
                case "AIR-01":
                    if (value > 28)
                        return DeviceStatus.Alarm;

                    if (value > 26)
                        return DeviceStatus.Warning;

                    return DeviceStatus.Normal;

                // Nutrient EC
                case "EC-01":
                    if (value < 1.0 || value > 2.6)
                        return DeviceStatus.Alarm;

                    if (value < 1.5 || value > 2.2)
                        return DeviceStatus.Warning;

                    return DeviceStatus.Normal;

                // No approved PAR Light thresholds yet.
                default:
                    return DeviceStatus.Normal;
            }
        }

        private string CreateAlarmMessage(
            TelemetryPacket packet,
            DeviceStatus status)
        {
            string sensorName = packet.DeviceId switch
            {
                "PH-01" => "pH",
                "WT-01" => "Water Temperature",
                "WL-01" => "Water Level",
                "HUM-01" => "Humidity",
                "AIR-01" => "Air Temperature",
                "EC-01" => "Nutrient EC",
                _ => packet.DeviceId
            };

            if (status == DeviceStatus.Normal)
                return "NORMAL: " + sensorName;

            string direction = packet.DeviceId switch
            {
                "PH-01" => packet.Value < 5.8 ? "LOW" : "HIGH",
                "WT-01" => packet.Value < 18 ? "LOW" : "HIGH",
                "WL-01" => "LOW",
                "HUM-01" => "HIGH",
                "AIR-01" => packet.Value < 20 ? "LOW" : "HIGH",
                "EC-01" => packet.Value < 1.5 ? "LOW" : "HIGH",
                _ => ""
            };

            return status.ToString().ToUpper() + ": "
                + sensorName + " " + direction;
        }

        public TelemetryPacket? GetLatest(string deviceId)
        {
            return packets.TryGetValue(deviceId, out TelemetryPacket? p ) ? p : null;
        }
    }
}

