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
using System.Windows.Threading;
using HydroFlow.Core;
using HydroFlow.Devices;

namespace HydroFlow.UI
{
    public partial class MainWindow : Window
    {
        // One row per device ID. A new packet replaces that device's row.
        private readonly ObservableCollection<TelemetryPacket> _readings = new();

        // IMessageBus has no Unsubscribe, so packets that arrive after the window closes are ignored.
        private bool _isClosed;

        // The window owns the only tick loop, because Simulation has no timer of its own.
        private DispatcherTimer? _timer;

        /*
         * Purpose: Set up the window and connect the readings table to its rows.
         */
        public MainWindow()
        {
            InitializeComponent();
            ReadingsGrid.ItemsSource = _readings;
            Closed += OnWindowClosed;
        }

        /*
         * Purpose: Show the simulation's packets and tick every device once per second.
         * The window subscribes before the first tick so no reading is missed.
         */
        public void Start(Simulation simulation)
        {
            ArgumentNullException.ThrowIfNull(simulation);

            // Only one tick loop may run.
            if (_timer is not null)
                throw new InvalidOperationException("The simulation is already running.");

            ShowReadingsFrom(simulation.Bus);

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (_, _) => simulation.Tick();
            _timer.Start();
        }

        /*
         * Purpose: Start showing packets published on the given message bus.
         * Start calls this before the first tick.
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
            // Alarm and Command packets reuse the device ID, so only readings may change a row.
            if (packet.Type != "Sensor" && packet.Type != "Actuator") return;

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

        /*
         * Purpose: Stop the tick loop and ignore any later packets once the window closes.
         */
        private void OnWindowClosed(object? sender, EventArgs e)
        {
            _isClosed = true;
            _timer?.Stop();
        }
    }
}
