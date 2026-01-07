using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Threading;
using System.Net.Http;
using System.Text.Json;
using System.Threading;

namespace ZeroMix
{
    using ZeroMix.zeromix.CreatePlugins;
    
    public partial class MainWindow : Window
    {
        // Windows API for Taskbar transparency
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        private enum WindowCompositionAttribute
        {
            WCA_ACCENT_POLICY = 19
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        private enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
            ACCENT_NORMAL = 0
        }

        private bool _isTaskbarTransparent = false;
        private NotifyIcon? _notifyIcon;
        private PerformanceCounter? _cpuCounter;
        private PerformanceCounter? _ramCounter;
        private PerformanceCounter? _diskCounter;
        private DispatcherTimer? _performanceTimer;
        private DriveInfo? _systemDrive;
        private string? _initialWallpaperPath;
        private ClockWidget? _clockWidget;
        private DispatcherTimer? _taskbarWatcher;
        private Plugins.PluginEngine? _pluginEngine;

        public MainWindow()
        {
            // Register Lua Bridge
            MoonSharp.Interpreter.UserData.RegisterType<Plugins.ZeroMixLuaApi>();
            
            InitializeComponent();
            InitializeTrayIcon();
            InitializeTaskbarWatcher();
            this.MouseLeftButtonDown += MainWindow_MouseLeftButtonDown;
        }

        private void InitializeTaskbarWatcher()
        {
            _taskbarWatcher = new DispatcherTimer();
            _taskbarWatcher.Interval = TimeSpan.FromMilliseconds(500);
            _taskbarWatcher.Tick += (s, e) =>
            {
                if (_isTaskbarTransparent)
                {
                    EnableTransparentTaskbar();
                }
            };
        }

        private void MainWindow_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                this.DragMove();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Set initial view after the window has loaded
            HomeButton_Click(this, new RoutedEventArgs());
            _initialWallpaperPath = GetSystemWallpaperPath();

            // Initialize Lua Engine
            _pluginEngine = new Plugins.PluginEngine(this);
            _pluginEngine.Start();
        }

        private string? GetSystemWallpaperPath()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop"))
                {
                    return key?.GetValue("Wallpaper") as string;
                }
            }
            catch { return null; }
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
                var processList = FindName("ProcessList") as System.Windows.Controls.ListBox;
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
            App.OptimizeMemory(); // Trim memory when hidden
            base.OnClosing(e);
        }

        // --- Navigation --- //

        private void DeactivateAllTabs()
        {
            if (HomeContent != null) HomeContent.Visibility = Visibility.Collapsed;
            if (AboutContent != null) AboutContent.Visibility = Visibility.Collapsed;
            if (PrivacyContent != null) PrivacyContent.Visibility = Visibility.Collapsed;
            if (WallpapersContent != null) WallpapersContent.Visibility = Visibility.Collapsed;

            if (_performanceTimer != null)
            {
                _performanceTimer.Stop();
            }

            if (HomeButton != null) HomeButton.Background = System.Windows.Media.Brushes.Transparent;
            if (AboutButton != null) AboutButton.Background = System.Windows.Media.Brushes.Transparent;
            if (PrivacyButton != null) PrivacyButton.Background = System.Windows.Media.Brushes.Transparent;
            if (WallpaperButton != null) WallpaperButton.Background = System.Windows.Media.Brushes.Transparent;
            if (PluginsButton != null) PluginsButton.Background = System.Windows.Media.Brushes.Transparent;
            if (PluginsContent != null) PluginsContent.Visibility = Visibility.Collapsed;
        }

        private void PlayTransition(UIElement content)
        {
            if (content is FrameworkElement fe)
            {
                var sb = (System.Windows.Media.Animation.Storyboard)FindResource("FadeIn");
                sb.Begin(fe);
            }
        }

        private void PluginsButton_Click(object sender, RoutedEventArgs e)
        {
            DeactivateAllTabs();
            PluginsContent.Visibility = Visibility.Visible;
            PluginsButton.Background = (System.Windows.Media.SolidColorBrush)FindResource("NavSelectedBrush");
            PlayTransition(PluginsContent);
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            DeactivateAllTabs();
            HomeContent.Visibility = Visibility.Visible;
            HomeButton.Background = (System.Windows.Media.SolidColorBrush)FindResource("NavSelectedBrush");
            PlayTransition(HomeContent);
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            DeactivateAllTabs();
            AboutContent.Visibility = Visibility.Visible;
            AboutButton.Background = (System.Windows.Media.SolidColorBrush)FindResource("NavSelectedBrush");
            PlayTransition(AboutContent);
        }

        private void PrivacyButton_Click(object sender, RoutedEventArgs e)
        {
            DeactivateAllTabs();
            PrivacyContent.Visibility = Visibility.Visible;
            PrivacyButton.Background = (System.Windows.Media.SolidColorBrush)FindResource("NavSelectedBrush");
            PlayTransition(PrivacyContent);
        }

        private void NavWallpapers_Click(object sender, RoutedEventArgs e)
        {
            DeactivateAllTabs();
            WallpapersContent.Visibility = Visibility.Visible;
            WallpaperButton.Background = (System.Windows.Media.SolidColorBrush)FindResource("NavSelectedBrush");
            PlayTransition(WallpapersContent);
        }

        private void WallpaperButton_Click(object sender, RoutedEventArgs e)
        {
            var wallpaperWindow = new Wallpapers();
            wallpaperWindow.ShowDialog();
        }

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
            if (_clockWidget == null || !IsWindowOpen<ClockWidget>())
            {
                _clockWidget = new ClockWidget();
                _clockWidget.Show();
            }
            else
            {
                if (_clockWidget.IsVisible)
                    _clockWidget.Hide();
                else
                    _clockWidget.Show();
            }
        }

        private bool IsWindowOpen<T>(string name = "") where T : Window
        {
            return System.Windows.Application.Current.Windows.OfType<T>().Any(w => string.IsNullOrEmpty(name) || w.Name == name);
        }

        private void TaskbarToggleBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isTaskbarTransparent)
                {
                    _isTaskbarTransparent = true;
                    _taskbarWatcher?.Start();
                    EnableTransparentTaskbar(); 
                    // StatusLabel.Text = "Ghost Taskbar: Clear Mode Active";
                    TaskbarToggleBtn.Opacity = 1.0;
                }
                else
                {
                    _isTaskbarTransparent = false;
                    _taskbarWatcher?.Stop();
                    DisableTransparentTaskbar();
                    // StatusLabel.Text = "Ghost Taskbar: Returned to Default";
                    TaskbarToggleBtn.Opacity = 0.7;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error toggling taskbar: {ex.Message}", "Error");
            }
        }

        private void EnableTransparentTaskbar()
        {
            // Shell_TrayWnd is the main taskbar
            IntPtr taskbarHandle = FindWindow("Shell_TrayWnd", null);
            
            // Mode Clear: ACCENT_ENABLE_TRANSPARENTGRADIENT (2)
            // Color: 0x00000000 (Full Transparent)
            // Flags: 2 (Draw borders/refresh policy)
            ApplyTaskbarAccent(taskbarHandle, AccentState.ACCENT_ENABLE_TRANSPARENTGRADIENT, 2, 0x00000000);

            // Shell_SecondaryTrayWnd for extra monitors
            IntPtr secondaryTaskbarHandle = FindWindow("Shell_SecondaryTrayWnd", null);
            if (secondaryTaskbarHandle != IntPtr.Zero)
            {
                ApplyTaskbarAccent(secondaryTaskbarHandle, AccentState.ACCENT_ENABLE_TRANSPARENTGRADIENT, 2, 0x00000000);
            }
        }

        private void DisableTransparentTaskbar()
        {
            // Shell_TrayWnd is the main taskbar
            IntPtr taskbarHandle = FindWindow("Shell_TrayWnd", null);
            
            // On Windows 10/11, state 0 (Disabled) often results in a solid black bar.
            // Using state 1 (Gradient) with color 0 often tells Windows to go back 
            // to its own internal theme-based rendering (Default/Blur/Acrylic).
            ApplyTaskbarAccent(taskbarHandle, AccentState.ACCENT_ENABLE_GRADIENT, 0, 0x00000000);

            // Re-apply for secondary taskbar
            IntPtr secondaryTaskbarHandle = FindWindow("Shell_SecondaryTrayWnd", null);
            if (secondaryTaskbarHandle != IntPtr.Zero)
            {
                ApplyTaskbarAccent(secondaryTaskbarHandle, AccentState.ACCENT_ENABLE_GRADIENT, 0, 0x00000000);
            }
        }

        private void ApplyTaskbarAccent(IntPtr handle, AccentState state, int flags, int color)
        {
            if (handle == IntPtr.Zero) return;

            var accent = new AccentPolicy();
            accent.AccentState = state;
            accent.AccentFlags = flags;
            accent.GradientColor = color;

            var data = new WindowCompositionAttributeData();
            data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
            data.SizeOfData = Marshal.SizeOf(accent);
            data.Data = Marshal.AllocHGlobal(data.SizeOfData);
            Marshal.StructureToPtr(accent, data.Data, false);

            SetWindowCompositionAttribute(handle, ref data);
            Marshal.FreeHGlobal(data.Data);
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
            CacheStatusText.Text = "Menganalisis file sampah...";
            
            // Show the monitoring panel if hidden to see status
            MonitoringPanel.Visibility = Visibility.Visible;

            await Task.Run(async () =>
            {
                string[] tempPaths = { 
                    Path.GetTempPath(), 
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp")
                };

                int deletedCount = 0;
                int skippedCount = 0;
                long totalSize = 0;

                foreach (var path in tempPaths)
                {
                    var directory = new DirectoryInfo(path);
                    if (!directory.Exists) continue;

                    // Update UI status
                    this.Dispatcher.Invoke(() => CacheStatusText.Text = $"Cleaning: {path}");

                    foreach (var file in directory.GetFiles())
                    {
                        try
                        {
                            totalSize += file.Length;
                            file.Delete();
                            deletedCount++;
                        }
                        catch { skippedCount++; }
                    }

                    foreach (var dir in directory.GetDirectories())
                    {
                        try
                        {
                            dir.Delete(true);
                            deletedCount++;
                        }
                        catch { skippedCount++; }
                    }
                    
                    await Task.Delay(10); // Prevent total UI freeze
                }

                this.Dispatcher.Invoke(() => {
                    CacheStatusText.Text = $"Purge Complete! Cleared {deletedCount} items ({totalSize / (1024 * 1024)} MB). Skipped {skippedCount} files in use.";
                    ClearCacheButton.IsEnabled = true;
                    // Trigger a process refresh
                    RefreshProcessList();
                });
            });
        }

        private void RefreshProcessesBtn_Click(object sender, RoutedEventArgs e)
        {
            RefreshProcessList();
        }
        
        private TransparentTaskbar? _taskbar;

        // Jika menggunakan tombol di Quick Features
        private void OpenTaskbarBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_taskbar == null || !_taskbar.IsVisible)
            {
                _taskbar = new TransparentTaskbar();
                _taskbar.Show();
            }
            else
            {
                _taskbar.Activate();
            }
        }

        // Atau jika menggunakan tombol di Sidebar
        private void TaskbarButton_Click(object sender, RoutedEventArgs e)
        {
            if (_taskbar == null || !_taskbar.IsVisible)
            {
                _taskbar = new TransparentTaskbar();
                _taskbar.Show();
            }
            else
            {
                _taskbar.Activate();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            App.OptimizeMemory();
        }

        // --- Plugin Creation System ---
        private async void CreatePluginBtn_Click(object sender, RoutedEventArgs e)
        {
            // Use the new minimalist window
            var inputWin = new CreatePluginWindow();
            inputWin.Owner = this;
            inputWin.ShowDialog();

            if (!inputWin.IsConfirmed) return;

            string pluginName = inputWin.PluginName;
            bool isPublic = inputWin.IsPublic;
            
            string folderName = $"user.{(isPublic ? "pub" : "priv")}.{pluginName}";
            string pluginDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins", folderName);

            try
            {
                // Create Directory
                Directory.CreateDirectory(pluginDir);

                // Create Lua Template
                string luaTemplate = @$"-- ZeroMix Plugin: {pluginName}
-- Created: {DateTime.Now}
-- Type: {(isPublic ? "Public" : "Private")}

function OnLoad()
    ZeroMix.Log('Plugin {pluginName} aktif!')
    ZeroMix.SetStatusText('Plugin {pluginName} Berjalan...')
end

function OnUpdate()
    -- Contoh: Cek CPU setiap detik
    local cpu = ZeroMix.GetCpuUsage()
    if cpu > 80 then
        ZeroMix.Log('Peringatan: CPU Tinggi! ' .. cpu .. '%')
    end
end";
                
                await File.WriteAllTextAsync(Path.Combine(pluginDir, "script.lua"), luaTemplate);

                if (isPublic)
                {
                    string metadata = $"{{\"name\": \"{pluginName}\", \"author\": \"User\", \"version\": \"1.0.0\"}}";
                    await File.WriteAllTextAsync(Path.Combine(pluginDir, "manifest.json"), metadata);
                }

                System.Windows.MessageBox.Show(
                    $"Berhasil membuat Plugin '{pluginName}'.\n\nFolder: {folderName}\nSilakan cek folder Plugins untuk mulai mengedit.", 
                    "Sukses", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Information);

                // Instant Load!
                _pluginEngine?.LoadPluginFromDirectory(pluginDir);

                Process.Start("explorer.exe", pluginDir);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Gagal membuat plugin: " + ex.Message);
            }
        }
    }
}
