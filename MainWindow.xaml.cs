using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;

namespace ZeroMix
{
    public partial class MainWindow : Window
    {
        private NotifyIcon _notifyIcon;
        private PerformanceCounter _cpuCounter;
        private PerformanceCounter _ramCounter;
        private DispatcherTimer _performanceTimer;

        public MainWindow()
        {
            InitializeComponent();
            InitializeTrayIcon();
            InitializePerformanceCounters();

            // Set initial view
            HomeButton_Click(this, new RoutedEventArgs());
        }

        private void InitializeTrayIcon()
        {
            _notifyIcon = new NotifyIcon();

            // Load the icon from embedded resources, which works in both Debug and Release
            var iconUri = new Uri("zeromix.ico", UriKind.RelativeOrAbsolute);
            var iconStream = System.Windows.Application.GetResourceStream(iconUri)?.Stream;
            if (iconStream != null)
            {
                _notifyIcon.Icon = new System.Drawing.Icon(iconStream);
            }

            _notifyIcon.Visible = true;
            _notifyIcon.DoubleClick += (s, args) => ShowWindow();

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Show Dashboard", null, (s, args) => ShowWindow());
            contextMenu.Items.Add("Exit", null, (s, args) => ExitApplication());
            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void InitializePerformanceCounters()
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _ramCounter = new PerformanceCounter("Memory", "Available MBytes");

            _performanceTimer = new DispatcherTimer();
            _performanceTimer.Interval = TimeSpan.FromSeconds(2);
            _performanceTimer.Tick += PerformanceTimer_Tick;
        }

        private void PerformanceTimer_Tick(object sender, EventArgs e)
        {
            // CPU Usage
            float cpuUsage = _cpuCounter.NextValue();
            CpuUsageText.Text = $"{cpuUsage:F1} %";

            // RAM Usage
            float availableRam = _ramCounter.NextValue();
            RamUsageText.Text = $"{availableRam:F0} MB Available";

            // Update tray icon tooltip
            _notifyIcon.Text = $"CPU: {cpuUsage:F1}% | RAM: {availableRam:F0}MB Avail.";
        }

        private void ShowWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void ExitApplication()
        {
            _notifyIcon.Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // Instead of closing, hide the window
            e.Cancel = true;
            this.Hide();
            base.OnClosing(e);
        }

        // --- Navigation --- //

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            HomeContent.Visibility = Visibility.Visible;
            DashboardContent.Visibility = Visibility.Collapsed;
            _performanceTimer.Stop();

            HomeButton.Background = (System.Windows.Media.SolidColorBrush)FindResource("NavSelectedBrush");
            DashboardButton.Background = System.Windows.Media.Brushes.Transparent;
        }

        private void DashboardButton_Click(object sender, RoutedEventArgs e)
    {
      HomeContent.Visibility = Visibility.Collapsed;
            DashboardContent.Visibility = Visibility.Visible;
            _performanceTimer.Start();

            DashboardButton.Background = (System.Windows.Media.SolidColorBrush)FindResource("NavSelectedBrush");
            HomeButton.Background = System.Windows.Media.Brushes.Transparent;
        }

        private void CustomButton_Click(object sender, RoutedEventArgs e)
        {
            var customShortcutWindow = new CustomShortcutWindow();
            customShortcutWindow.ShowDialog();
        }
    // Remove Tidak di pakek
        // private void OpenOverlay_Click(object sender, RoutedEventArgs e)
        // {
        //     var overlay = new SearchOverlay();
        //     overlay.Show();
        // }

        // --- Dashboard Logic --- //

        private async void CleanTempButton_Click(object sender, RoutedEventArgs e)
        {
            CleanTempButton.IsEnabled = false;
            CleanStatusText.Text = "Pembersihan Segeara Mohon tunggu..";

            int skippedFiles = 0;

            await Task.Run(() =>
            {
                string[] tempPaths = { Path.GetTempPath(), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp") };

                foreach (var path in tempPaths)
                {
                    var directory = new DirectoryInfo(path);
                    if (!directory.Exists) continue;

                    foreach (var file in directory.GetFiles())
                    {
                        try
                        {
                            file.Delete();
                        }
                        catch (Exception)
                        {
                            skippedFiles++;
                        }
                    }

                    foreach (var dir in directory.GetDirectories())
                    {
                        try
                        {
                            dir.Delete(true);
                        }
                        catch (Exception)
                        {
                            skippedFiles++;
                        }
                    }
                }
            });

            CleanStatusText.Text = $"Cleaning complete. Skipped {skippedFiles} files that were in use.";
            CleanTempButton.IsEnabled = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }
    }
}