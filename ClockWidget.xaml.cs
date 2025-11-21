using System;
using System.Windows;
using System.Windows.Threading;

namespace ZeroMix
{
    public partial class ClockWidget : Window
    {
        private DispatcherTimer? _clockTimer;

        public ClockWidget()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Start clock update timer
            _clockTimer = new DispatcherTimer();
            _clockTimer.Interval = TimeSpan.FromSeconds(1);
            _clockTimer.Tick += ClockTimer_Tick;
            _clockTimer.Start();

            // Initial update
            UpdateClockDisplay();

            // Position window ke bottom-right
            this.Left = SystemParameters.PrimaryScreenWidth - this.Width - 20;
            this.Top = SystemParameters.PrimaryScreenHeight - this.Height - 20;
        }

        private void ClockTimer_Tick(object? sender, EventArgs e)
        {
            UpdateClockDisplay();
        }

        private void UpdateClockDisplay()
        {
            DateTime now = DateTime.Now;
            DateTime utcNow = DateTime.UtcNow;

            // Update time (HH:mm:ss)
            TimeDisplay.Text = now.ToString("HH:mm:ss");

            // Update date
            DateDisplay.Text = now.ToString("dddd, MMMM dd, yyyy");

            // Update day of year
            int dayOfYear = now.DayOfYear;
            int daysInYear = (DateTime.IsLeapYear(now.Year) ? 366 : 365);
            DayOfWeekDisplay.Text = $"Day {dayOfYear}/{daysInYear}";

            // Update UTC offset
            TimeZoneInfo localTimeZone = TimeZoneInfo.Local;
            TimeSpan offset = localTimeZone.GetUtcOffset(now);
            string sign = offset < TimeSpan.Zero ? "-" : "+";
            string utcText = $"UTC{sign}{Math.Abs(offset.Hours):D2}:{Math.Abs(offset.Minutes):D2}";
            UtcDisplay.Text = utcText;
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Allow dragging window
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void AlwaysOnTopButton_Click(object sender, RoutedEventArgs e)
        {
            Topmost = !Topmost;
            AlwaysOnTopButton.Background = Topmost 
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80))
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 212));
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_clockTimer != null)
            {
                _clockTimer.Stop();
            }
        }
    }
}
