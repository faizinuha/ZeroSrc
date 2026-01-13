using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace ZeroMix
{
    public partial class ClockWidget : Window
    {
        private DispatcherTimer? _clockTimer;

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOMOVE = 0x0002;
        const uint SWP_NOACTIVATE = 0x0010;

        const int GWL_EXSTYLE = -20;
        const int WS_EX_TOOLWINDOW = 0x00000080;

        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public ClockWidget()
        {
            InitializeComponent();
            SetupContextMenu();
        }

        private void SetupContextMenu()
        {
            ContextMenu cm = new ContextMenu();
            MenuItem closeItem = new MenuItem { Header = "Close Clock" };
            closeItem.Click += (s, e) => Close();
            cm.Items.Add(closeItem);
            this.ContextMenu = cm;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Center on Screen
            this.Left = (SystemParameters.PrimaryScreenWidth - this.Width) / 2;
            this.Top = (SystemParameters.PrimaryScreenHeight - this.Height) / 2;

            // Start clock update timer
            _clockTimer = new DispatcherTimer();
            _clockTimer.Interval = TimeSpan.FromSeconds(1);
            _clockTimer.Tick += ClockTimer_Tick;
            _clockTimer.Start();

            // Initial update
            UpdateClockDisplay();

            // Hide from Alt+Tab and make it stay at the bottom of Z-order
            var helper = new System.Windows.Interop.WindowInteropHelper(this);
            
            // Hide from Alt+Tab (WS_EX_TOOLWINDOW)
            int exStyle = GetWindowLong(helper.Handle, GWL_EXSTYLE);
            SetWindowLong(helper.Handle, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW);

            // Desktop style Z-order
            SetWindowPos(helper.Handle, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
        }

        private void ClockTimer_Tick(object? sender, EventArgs e)
        {
            UpdateClockDisplay();
        }

        private void UpdateClockDisplay()
        {
            DateTime now = DateTime.Now;

            // Update time (HH:mm:ss)
            TimeDisplay.Text = now.ToString("HH:mm:ss");

            // Update date
            DateDisplay.Text = now.ToString("dddd, MMMM dd, yyyy").ToUpper();

            // Update day of year
            int dayOfYear = now.DayOfYear;
            DayOfWeekDisplay.Text = $"DAY {dayOfYear}";

            // Update UTC offset
            TimeZoneInfo localTimeZone = TimeZoneInfo.Local;
            TimeSpan offset = localTimeZone.GetUtcOffset(now);
            string sign = offset < TimeSpan.Zero ? "-" : "+";
            UtcDisplay.Text = $"UTC {sign}{Math.Abs(offset.Hours):D2}:{Math.Abs(offset.Minutes):D2}";
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                Close();
                return;
            }
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
