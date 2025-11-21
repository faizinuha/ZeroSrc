using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

namespace ZeroMix
{
    public partial class TransparentTaskbar : Window
    {
        private PerformanceCounter? _cpuCounter;
        private PerformanceCounter? _ramCounter;
        private DispatcherTimer? _updateTimer;

        public TransparentTaskbar()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Position at top of screen
            this.Left = 0;
            this.Top = 0;
            this.Width = SystemParameters.PrimaryScreenWidth;

            // Initialize performance counters
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
                _ramCounter = new PerformanceCounter("Memory", "% Committed Bytes In Use", "", true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing counters: {ex.Message}");
            }

            // Start update timer
            _updateTimer = new DispatcherTimer();
            _updateTimer.Interval = TimeSpan.FromSeconds(1);
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();

            // Initial update
            UpdateTaskbarInfo();
        }

        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            UpdateTaskbarInfo();
        }

        private void UpdateTaskbarInfo()
        {
            // Update time
            TimeDisplay.Text = DateTime.Now.ToString("HH:mm");

            // Update CPU
            if (_cpuCounter != null)
            {
                float cpuUsage = _cpuCounter.NextValue();
                CpuDisplay.Text = $"{(int)cpuUsage}%";
                
                // Color based on usage
                if (cpuUsage > 80)
                    CpuDisplay.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 107, 107));
                else if (cpuUsage > 50)
                    CpuDisplay.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 193, 7));
                else
                    CpuDisplay.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 217, 255));
            }

            // Update RAM
            if (_ramCounter != null)
            {
                float ramUsage = _ramCounter.NextValue();
                RamDisplay.Text = $"{(int)ramUsage}%";
                
                // Color based on usage
                if (ramUsage > 80)
                    RamDisplay.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 107, 107));
                else if (ramUsage > 50)
                    RamDisplay.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 193, 7));
                else
                    RamDisplay.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 217, 255));
            }
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Allow dragging
            DragMove();
        }

        // Navigation Buttons
        private void HomeTaskbarBtn_Click(object sender, RoutedEventArgs e)
        {
            // Open home window or navigate
            MessageBox.Show("Home clicked");
        }

        private void DashboardTaskbarBtn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Dashboard clicked");
        }

        private void LauncherTaskbarBtn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Launcher clicked");
        }

        private void SettingsTaskbarBtn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Settings clicked");
        }

        private void PinButton_Click(object sender, RoutedEventArgs e)
        {
            Topmost = !Topmost;
            PinButton.Foreground = Topmost 
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80))
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(224, 224, 224));
        }

        private void TaskbarSettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Taskbar Settings");
        }

        private void CloseTaskbarBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_updateTimer != null)
            {
                _updateTimer.Stop();
            }
            if (_cpuCounter != null) _cpuCounter.Dispose();
            if (_ramCounter != null) _ramCounter.Dispose();
            base.OnClosed(e);
        }
    }
}
