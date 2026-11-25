/*
 * File: DashboardReadings.cs
 * Author: Jacob Fonseca
 * Project: HydroFlow SCADA
 * Task: HF-59
 *
 * Keeps one dashboard row per device, grouped by module, and updates it from reading packets.
 * HF-97: also tracks each device's alert status from Alarm packets and counts active alerts.
 * Not thread safe: MainWindow calls it on the UI thread.
 */

using System.Collections.ObjectModel;
using System.ComponentModel;
using HydroFlow.Core;

namespace HydroFlow.Controller.Dashboard;

public class DashboardReadings : INotifyPropertyChanged
{
    private readonly ObservableCollection<DashboardRow> _rows = new();
    private readonly Dictionary<string, DashboardRow> _byId = new();
    private int _activeAlertCount;

    /*
     * Purpose: Start with no devices. Call AddDevices with the simulation's devices.
     */
    public DashboardReadings()
    {
        Rows = new ReadOnlyObservableCollection<DashboardRow>(_rows);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    // Rows sorted by module (Climate, Water, Nutrients, Lighting, Other), then in the order devices were added.
    public ReadOnlyObservableCollection<DashboardRow> Rows { get; }

    /*
     * Purpose: Add a waiting row for each registered device, so all devices show before their first reading.
     * A device ID that already has a row is skipped, so nothing is listed twice.
     */
    public void AddDevices(IEnumerable<Device> devices)
    {
        ArgumentNullException.ThrowIfNull(devices);

        foreach (Device device in devices)
        {
            if (_byId.ContainsKey(device.DeviceId))
                continue;

            string unit = device switch
            {
                Sensor sensor => sensor.Unit,
                Actuator actuator => actuator.Unit,
                _ => ""
            };

            Insert(new DashboardRow(device.DeviceId, device.Name, DeviceModules.For(device), unit));
        }
    }

    /*
     * Purpose: Update the row for the device that sent a reading.
     * Returns false for Alarm and Command packets, which reuse the device ID and must not change a reading.
     * A reading from a device with no row gets a new row under Other.
     */
    public bool ApplyReading(TelemetryPacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);

        if (packet.Type != "Sensor" && packet.Type != "Actuator")
            return false;

        if (!_byId.TryGetValue(packet.DeviceId, out DashboardRow? row))
        {
            row = new DashboardRow(packet.DeviceId, packet.DeviceId, DeviceModules.Other, packet.Unit);
            Insert(row);
        }

        row.ApplyReading(packet);
        return true;
    }

    /*
     * Purpose: Update a device's alert status from an Alarm packet.
     * Returns false for any other packet type. The device's latest reading is kept.
     * An alarm for a device with no row gets a new row under Other, so the alert is not lost.
     */
    public bool ApplyAlarm(TelemetryPacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);

        if (packet.Type != "Alarm")
            return false;

        if (!_byId.TryGetValue(packet.DeviceId, out DashboardRow? row))
        {
            row = new DashboardRow(packet.DeviceId, packet.DeviceId, DeviceModules.Other, packet.Unit);
            Insert(row);
        }

        row.ApplyAlarm(packet);

        // Counted from each device's current status, so repeated packets can't push the count up
        // and a device going back to Normal clears its alert.
        ActiveAlertCount = _rows.Count(r => r.AlertStatus != DeviceStatus.Normal);
        return true;
    }

    // Devices whose current alert status is Warning or Alarm.
    public int ActiveAlertCount
    {
        get => _activeAlertCount;
        private set
        {
            if (_activeAlertCount == value)
                return;

            _activeAlertCount = value;
            OnPropertyChanged(nameof(ActiveAlertCount));
            OnPropertyChanged(nameof(HasActiveAlerts));
            OnPropertyChanged(nameof(HealthText));
        }
    }

    public bool HasActiveAlerts => _activeAlertCount > 0;

    // Header text: "All normal", "1 alert" or "N alerts".
    public string HealthText => _activeAlertCount switch
    {
        0 => "All normal",
        1 => "1 alert",
        _ => $"{_activeAlertCount} alerts"
    };

    /*
     * Purpose: Find a device's row, or null if it has none.
     */
    public DashboardRow? Find(string deviceId) =>
        _byId.TryGetValue(deviceId, out DashboardRow? row) ? row : null;

    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    // Keeps rows sorted by module so the grouped view shows modules in DisplayOrder.
    private void Insert(DashboardRow row)
    {
        int order = DeviceModules.OrderOf(row.Module);
        int index = _rows.Count;
        while (index > 0 && DeviceModules.OrderOf(_rows[index - 1].Module) > order)
            index--;

        _rows.Insert(index, row);
        _byId[row.DeviceId] = row;
    }
}
