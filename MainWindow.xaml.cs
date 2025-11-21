using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Threading;

namespace ZeroMix
{
    public partial class MainWindow : Window
    {
        private NotifyIcon? _notifyIcon;
        private PerformanceCounter? _cpuCounter;
        private PerformanceCounter? _ramCounter;
        private PerformanceCounter? _diskCounter;
        private DispatcherTimer? _performanceTimer;
        private DriveInfo? _systemDrive;

        

        public MainWindow()
        {
            InitializeComponent();
            InitializeTrayIcon();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Set initial view after the window has loaded
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
            _diskCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
            _systemDrive = new DriveInfo("C");

            _performanceTimer = new DispatcherTimer();
            _performanceTimer.Interval = TimeSpan.FromSeconds(2);
            _performanceTimer.Tick += PerformanceTimer_Tick;
        }

        private void PerformanceTimer_Tick(object? sender, EventArgs e)
        {
            UpdateDashboard();
        }

        private void UpdateDashboard()
        {
            try
            {
                // CPU Usage
                float cpuUsage = _cpuCounter!.NextValue();
                CpuPercentText.Text = $"{cpuUsage:F1} %";
                CpuProgressBar.Value = cpuUsage;

                // RAM Usage
                float availableRam = _ramCounter!.NextValue();
                ManagementClass managementClass = new ManagementClass("Win32_ComputerSystem");
                ManagementObjectCollection managementObjectCollection = managementClass.GetInstances();
                long totalRAM = 0;
                foreach (ManagementObject managementObject in managementObjectCollection)
                {
                    totalRAM = Convert.ToInt64(managementObject["TotalPhysicalMemory"]) / (1024 * 1024);
                }
                
                float usedRam = totalRAM - (int)availableRam;
                float ramPercent = (usedRam / totalRAM) * 100;
                
                RamPercentText.Text = $"{ramPercent:F1} %";
                RamProgressBar.Value = ramPercent;

                // Disk Usage
                if (_systemDrive != null && _systemDrive.IsReady)
                {
                    long totalBytes = _systemDrive.TotalSize;
                    long freeBytes = _systemDrive.AvailableFreeSpace;
                    long usedBytes = totalBytes - freeBytes;
                    double diskPercent = ((double)usedBytes / totalBytes) * 100;

                    DiskPercentText.Text = $"{diskPercent:F1} %";
                    DiskProgressBar.Value = diskPercent;
                }

                // Update tray icon tooltip
                _notifyIcon!.Text = $"CPU: {cpuUsage:F1}% | RAM: {ramPercent:F1}% | Disk: {(float)(DiskProgressBar.Value):F1}%";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating dashboard: {ex.Message}");
            }
        }

        private void UpdateSystemInfo()
        {
            try
            {
                // OS Version
                ManagementClass osClass = new ManagementClass("Win32_OperatingSystem");
                ManagementObjectCollection osCollection = osClass.GetInstances();
                foreach (ManagementObject os in osCollection)
                {
                    string? osVersion = os["Caption"]?.ToString();
                    OsVersionText.Text = osVersion ?? "Unknown OS";
                }

                // Processor
                ManagementClass procClass = new ManagementClass("Win32_Processor");
                ManagementObjectCollection procCollection = procClass.GetInstances();
                foreach (ManagementObject proc in procCollection)
                {
                    ProcessorText.Text = proc["Name"]?.ToString() ?? "Unknown Processor";
                }

                // RAM
                ManagementClass ramClass = new ManagementClass("Win32_ComputerSystem");
                ManagementObjectCollection ramCollection = ramClass.GetInstances();
                foreach (ManagementObject ram in ramCollection)
                {
                    long totalRam = Convert.ToInt64(ram["TotalPhysicalMemory"]) / (1024 * 1024 * 1024);
                    TotalRamText.Text = $"RAM: {totalRam} GB";
                }

                // Network
                ManagementClass netClass = new ManagementClass("Win32_NetworkAdapterConfiguration");
                ManagementObjectCollection netCollection = netClass.GetInstances();
                int activeNetworks = 0;
                foreach (ManagementObject net in netCollection)
                {
                    if ((bool?)net["IPEnabled"] == true)
                        activeNetworks++;
                }
                NetworkText.Text = $"Network: {activeNetworks} Active";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating system info: {ex.Message}");
            }
        }

        private void RefreshProcessList()
        {
            try
            {
                var processes = Process.GetProcesses()
                    .Where(p => p.TotalProcessorTime.TotalSeconds > 0)
                    .OrderByDescending(p => p.TotalProcessorTime)
                    .Take(10)
                    .Select(p => new
                    {
                        Name = p.ProcessName,
                        Memory = p.WorkingSet64 / (1024 * 1024),
                        CPU = p.TotalProcessorTime.TotalSeconds
                    })
                    .ToList();

                // Only update if ProcessList control exists (for backwards compatibility)
                var processList = FindName("ProcessList") as ListBox;
                if (processList != null)
                {
                    processList.Items.Clear();
                    foreach (var proc in processes)
                    {
                        processList.Items.Add($"{proc.Name} - {proc.Memory} MB");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error refreshing process list: {ex.Message}");
            }
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

        private void DeactivateAllTabs()
        {
            if (HomeContent != null) HomeContent.Visibility = Visibility.Collapsed;
            if (AboutContent != null) AboutContent.Visibility = Visibility.Collapsed;
            if (PrivacyContent != null) PrivacyContent.Visibility = Visibility.Collapsed;

            if (_performanceTimer != null)
            {
                _performanceTimer.Stop();
            }

            if (HomeButton != null) HomeButton.Background = System.Windows.Media.Brushes.Transparent;
            if (AboutButton != null) AboutButton.Background = System.Windows.Media.Brushes.Transparent;
            if (PrivacyButton != null) PrivacyButton.Background = System.Windows.Media.Brushes.Transparent;
            if (WallpaperButton != null) WallpaperButton.Background = System.Windows.Media.Brushes.Transparent;
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            DeactivateAllTabs();
            HomeContent.Visibility = Visibility.Visible;
            HomeButton.Background = (System.Windows.Media.SolidColorBrush)FindResource("NavSelectedBrush");
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            DeactivateAllTabs();
            AboutContent.Visibility = Visibility.Visible;
            AboutButton.Background = (System.Windows.Media.SolidColorBrush)FindResource("NavSelectedBrush");
        }

        private void PrivacyButton_Click(object sender, RoutedEventArgs e)
        {
            DeactivateAllTabs();
            PrivacyContent.Visibility = Visibility.Visible;
            PrivacyButton.Background = (System.Windows.Media.SolidColorBrush)FindResource("NavSelectedBrush");
        }

        private void WallpaperButton_Click(object sender, RoutedEventArgs e)
        {
            var wallpaperWindow = new Wallpapers();
            wallpaperWindow.ShowDialog();
        }

        // remove Tidak di pakek

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

        private void EnableMonitoringBtn_Click(object sender, RoutedEventArgs e)
        {
            MonitoringPanel.Visibility = Visibility.Visible;

            if (_performanceTimer == null)
            {
                InitializePerformanceCounters();
                UpdateSystemInfo();
                RefreshProcessList();
            }
            _performanceTimer.Start();
        }

        private void OpenClockBtn_Click(object sender, RoutedEventArgs e)
        {
            var clockWidget = new ClockWidget();
            clockWidget.Show();
        }

        private void DisableMonitoringBtn_Click(object sender, RoutedEventArgs e)
        {
            MonitoringPanel.Visibility = Visibility.Collapsed;
            if (_performanceTimer != null)
            {
                _performanceTimer.Stop();
            }
        }

        private async void ClearCacheButton_Click(object sender, RoutedEventArgs e)
        {
            ClearCacheButton.IsEnabled = false;
            CacheStatusText.Text = "Pembersihan Segeara Mohon tunggu..";
            CacheProgressBar.Visibility = Visibility.Visible;

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

            CacheStatusText.Text = $"Cleaning complete. Skipped {skippedFiles} files that were in use.";
            ClearCacheButton.IsEnabled = true;
            CacheProgressBar.Visibility = Visibility.Collapsed;
        }

        private void RefreshProcessesBtn_Click(object sender, RoutedEventArgs e)
        {
            RefreshProcessList();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }
    }
}