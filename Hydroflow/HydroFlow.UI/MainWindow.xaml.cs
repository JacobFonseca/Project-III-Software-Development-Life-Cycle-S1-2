/*
 * File: MainWindow.xaml.cs
 * Author: Jacob Fonseca
 * Project: HydroFlow SCADA
 * Task: HF-38
 *
 * Displays the latest reading from each HydroFlow device.
 */

using System.Collections.ObjectModel;
using System.Windows;
using HydroFlow.Core;

namespace HydroFlow.UI
{
    public partial class MainWindow : Window
    {
        // One row per device ID. A new packet replaces that device's row.
        private readonly ObservableCollection<TelemetryPacket> _readings = new();

        // IMessageBus has no Unsubscribe, so packets that arrive after the window closes are ignored.
        private bool _isClosed;

        /*
         * Purpose: Set up the window and connect the readings table to its rows.
         */
        public MainWindow()
        {
            InitializeComponent();
            ReadingsGrid.ItemsSource = _readings;
            Closed += (_, _) => _isClosed = true;
        }

        /*
         * Purpose: Start showing packets published on the given message bus.
         * Application setup calls this once the device simulation is available.
         */
        public void ShowReadingsFrom(IMessageBus bus)
        {
            ArgumentNullException.ThrowIfNull(bus);
            bus.Subscribe(OnPacketReceived);
        }

        /*
         * Purpose: Refresh the displayed reading for the device that sent the packet.
         */
        private void OnPacketReceived(TelemetryPacket packet)
        {
            // MessageBus calls subscribers on the publisher's thread, so move to the UI thread first.
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.InvokeAsync(() => OnPacketReceived(packet));
                return;
            }

            if (_isClosed)
                return;

            for (int i = 0; i < _readings.Count; i++)
            {
                if (_readings[i].DeviceId == packet.DeviceId)
                {
                    _readings[i] = packet;
                    return;
                }
            }

            _readings.Add(packet);
            EmptyMessage.Visibility = Visibility.Collapsed;
        }
    }
}
