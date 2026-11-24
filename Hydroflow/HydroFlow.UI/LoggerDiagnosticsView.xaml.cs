/*
 * File: LoggerDiagnosticsView.xaml.cs
 * Author: Jacob Fonseca
 * Project: HydroFlow SCADA
 * Task: HF-63
 *
 * Reads a small, bounded set of saved rows from the logger and shows them.
 * It refreshes every few seconds instead of on every telemetry tick.
 */

using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using HydroFlow.Logging;

namespace HydroFlow.UI
{
    public partial class LoggerDiagnosticsView : UserControl
    {
        // Newest rows shown. Kept small so each refresh is one short query.
        public const int RecentLimit = 50;

        private readonly DispatcherTimer _refreshTimer = new() { Interval = TimeSpan.FromSeconds(5) };
        private SqliteTelemetryLogger? _logger;

        /*
         * Purpose: Build the view. The timer only runs while the view is on screen.
         */
        public LoggerDiagnosticsView()
        {
            InitializeComponent();
            _refreshTimer.Tick += (_, _) => Refresh();
            Loaded += (_, _) => { Refresh(); _refreshTimer.Start(); };
            Unloaded += (_, _) => _refreshTimer.Stop();
        }

        /*
         * Purpose: Give the view the app's logger. Null means logging is off.
         */
        public void ShowLogger(SqliteTelemetryLogger? logger)
        {
            _logger = logger;
            Refresh();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e) => Refresh();

        /*
         * Purpose: Read the row count, failed saves and the newest alarms and commands.
         */
        private void Refresh()
        {
            if (_logger is null)
            {
                SummaryText.Text = "Logging is off. hydroflow.db could not be opened when the app started.";
                EntriesGrid.ItemsSource = null;
                return;
            }

            try
            {
                long rows = _logger.CountRows();
                SummaryText.Text = $"{rows} rows saved in {_logger.DatabasePath}. " +
                                   $"Showing the newest {RecentLimit} alarms and commands.";

                // Alarm and Command rows carry the messages. Readings are left out to keep the list readable.
                EntriesGrid.ItemsSource = _logger.ReadRecent(RecentLimit, "Alarm", "Command")
                    .Select(p => new
                    {
                        Time = p.Timestamp.ToLocalTime(),
                        p.DeviceId,
                        p.Type,
                        p.Status,
                        p.Message
                    })
                    .ToList();
            }
            catch (Exception ex) when (ex is Microsoft.Data.Sqlite.SqliteException or ObjectDisposedException)
            {
                SummaryText.Text = "Could not read hydroflow.db: " + ex.Message;
            }

            int failed = _logger.FailedCount;
            ErrorText.Visibility = failed > 0 ? Visibility.Visible : Visibility.Collapsed;
            ErrorText.Text = failed > 0
                ? $"{failed} packets could not be saved. Last error: {_logger.LastError?.Message}"
                : "";
        }
    }
}
