using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HydroFlow.Core;

namespace HydroFlow.UI
{
    public record AlarmRow(string DeviceId, string Text, Brush Background, Brush Foreground);

    public partial class AlarmBanner : UserControl
    {
        private static readonly Brush Red = Frozen(0xC6, 0x28, 0x28);
        private static readonly Brush Yellow = Frozen(0xF9, 0xA8, 0x25);
        private static readonly Brush Green = Frozen(0x2E, 0x7D, 0x32);
        private static readonly Brush White = Frozen(0xFF, 0xFF, 0xFF);
        private static readonly Brush Black = Frozen(0x21, 0x21, 0x21);

        private AlarmTracker? _tracker;

        public AlarmBanner()
        {
            InitializeComponent();
        }

        public void Initialize(AlarmTracker tracker)
        {
            _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
            _tracker.Changed += () => Dispatcher.Invoke(Refresh);
            Refresh();
        }

        private void Refresh()
        {
            if (_tracker == null) return;
            AlarmList.ItemsSource = _tracker.Active.Select(ToRow).ToList();
        }

        private static AlarmRow ToRow(AlarmEntry a)
        {
            string time = a.Timestamp.ToLocalTime().ToString("HH:mm:ss");

            if (a.IsCleared)
                return new AlarmRow(a.DeviceId,
                    $"{time}  {a.Message}  (back to normal)", Green, White);

            return a.Status == DeviceStatus.Alarm
                ? new AlarmRow(a.DeviceId, $"{time}  {a.Message}", Red, White)
                : new AlarmRow(a.DeviceId, $"{time}  {a.Message}", Yellow, Black);
        }

        private void Acknowledge_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: string id })
                _tracker?.Acknowledge(id);
        }

        private static Brush Frozen(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }
    }
}