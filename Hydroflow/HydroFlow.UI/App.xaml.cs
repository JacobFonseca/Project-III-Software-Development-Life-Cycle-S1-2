/*
 * File: App.xaml.cs
 * Author: Jacob Fonseca
 * Project: HydroFlow SCADA
 * Task: HF-38
 *
 * Starts the device simulation, the SQLite logger and the monitoring window.
 */

using System.IO;
using System.Windows;
using HydroFlow.Core;
using HydroFlow.Devices;
using HydroFlow.Controller;
using HydroFlow.Logging;

namespace HydroFlow.UI
{
    public partial class App : Application
    {
        // Kept for the lifetime of the app so thresholds are evaluated on every tick.
        private SCADAController? _controller;

        // HF-57: one logger for the whole app, closed in OnExit.
        private SqliteTelemetryLogger? _logger;

        /*
         * Purpose: Create the simulation from the copied data files and show the monitoring window.
         */
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // The UI project copies data\*.txt beside the .exe, so files are resolved from there.
            string dataFolder = Path.Combine(AppContext.BaseDirectory, "data");
            var simulation = new Simulation(file => new LoopingFileReader(Path.Combine(dataFolder, file)));

            // HF-57: subscribed before the controller, so a reading is saved before the alarm it causes.
            StartLogger(simulation.Bus);

            // HF-100: evaluates sensor readings and publishes Alarm packets on the same bus.
            _controller = new SCADAController(simulation.Bus);

            // HF-63: the window's diagnostics view reads from the same logger.
            var window = new MainWindow { Logger = _logger };
            window.Start(simulation);
            window.Show();
        }

        /*
         * Purpose: Open hydroflow.db and subscribe the logger once.
         * If the database can't be opened, the dashboard still runs and the user is told logging is off.
         */
        private void StartLogger(IMessageBus bus)
        {
            try
            {
                _logger = new SqliteTelemetryLogger(SqliteTelemetryLogger.GetDefaultPath());
            }
            catch (Exception ex) when (ex is Microsoft.Data.Sqlite.SqliteException or IOException or UnauthorizedAccessException)
            {
                MessageBox.Show("Logging is off. hydroflow.db could not be opened:\n" + ex.Message,
                    "HydroFlow logger", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Only the first failure gets a message box, so a broken database can't flood the screen.
            // Later failures are still counted in FailedCount and written to Trace.
            bool warned = false;
            _logger.LogFailed += ex =>
            {
                if (warned)
                    return;
                warned = true;
                Dispatcher.InvokeAsync(() => MessageBox.Show("A packet could not be saved to hydroflow.db:\n" + ex.Message,
                    "HydroFlow logger", MessageBoxButton.OK, MessageBoxImage.Warning));
            };

            _logger.Attach(bus);
        }

        /*
         * Purpose: Close the database so the last rows are written and the file is released.
         */
        protected override void OnExit(ExitEventArgs e)
        {
            _logger?.Dispose();
            base.OnExit(e);
        }
    }
}
