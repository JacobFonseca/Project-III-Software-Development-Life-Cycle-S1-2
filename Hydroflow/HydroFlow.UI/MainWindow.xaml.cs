/*
 * File: MainWindow.xaml.cs
 * Author: Jacob Fonseca
 * Project: HydroFlow SCADA
 * Task: HF-38
 *
 * Displays the latest reading from each HydroFlow device, grouped by module.
 */

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using HydroFlow.Core;
using HydroFlow.Controller.Dashboard;
using HydroFlow.Devices;
using HydroFlow.Logging;

namespace HydroFlow.UI
{
    public partial class MainWindow : Window
    {
        // One row per device ID, grouped by module. A new reading updates that device's row.
        private readonly DashboardReadings _dashboard = new();

        // IMessageBus has no Unsubscribe, so packets that arrive after the window closes are ignored.
        private bool _isClosed;

        // The window owns the only tick loop, because Simulation has no timer of its own.
        private DispatcherTimer? _timer;

        // HF-63: only one diagnostics window is open at a time.
        private Window? _diagnosticsWindow;

        // HF-63: set by App. Null when hydroflow.db could not be opened.
        public SqliteTelemetryLogger? Logger { get; set; }

        /*
         * Purpose: Set up the window and connect the readings table to its rows.
         */
        public MainWindow()
        {
            InitializeComponent();

            // HF-59: the grid shows rows under a heading for each module.
            var view = CollectionViewSource.GetDefaultView(_dashboard.Rows);
            view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(DashboardRow.Module)));
            ReadingsGrid.ItemsSource = view;

            // HF-97: one tile per device in the tiles slot, and the health text in the header.
            DeviceTilesSlot.Content = new ItemsControl
            {
                ItemsSource = _dashboard.Rows,
                ItemTemplate = (DataTemplate)FindResource("DeviceTileTemplate"),
                ItemsPanel = (ItemsPanelTemplate)FindResource("DeviceTilePanel")
            };
            HealthText.DataContext = _dashboard;
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

            // All registered devices are listed right away, then fill in as readings arrive.
            _dashboard.AddDevices(simulation.Sensors.Cast<Device>().Concat(simulation.Actuators));

            ShowReadingsFrom(simulation.Bus);

            // HF-67: show active alarms from the Alarm packets on the same bus.
            var tracker = new AlarmTracker(simulation.Bus);
            var banner = new AlarmBanner();
            banner.Initialize(tracker);
            AlarmBannerSlot.Content = banner;

           
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

            // HF-97: alarms have their own handler, because the reading handler skips them.
            bus.Subscribe(OnAlarmReceived);
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

            if (_dashboard.ApplyReading(packet))
                EmptyMessage.Visibility = Visibility.Collapsed;
        }

        /*
         * Purpose: Update the device's tile colour and the alert count from an Alarm packet.
         * The device's latest reading is kept.
         */
        private void OnAlarmReceived(TelemetryPacket packet)
        {
            if (packet.Type != "Alarm") return;

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.InvokeAsync(() => OnAlarmReceived(packet));
                return;
            }

            if (_isClosed)
                return;

            _dashboard.ApplyAlarm(packet);
        }

        /*
         * Purpose: Open the logger diagnostics view, or bring it to the front if it is already open.
         */
        private void DiagnosticsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_diagnosticsWindow is not null)
            {
                _diagnosticsWindow.Activate();
                return;
            }

            var view = new LoggerDiagnosticsView();
            view.ShowLogger(Logger);

            // Owned by this window, so it closes with the dashboard.
            _diagnosticsWindow = new Window
            {
                Title = "HydroFlow logger diagnostics",
                Content = view,
                Owner = this,
                Width = 760,
                Height = 480
            };
            _diagnosticsWindow.Closed += (_, _) => _diagnosticsWindow = null;
            _diagnosticsWindow.Show();
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
