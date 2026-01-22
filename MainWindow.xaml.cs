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
using System.Windows.Media;
using System.Windows.Input;
using ZeroMix.Recorder;
using System.Windows.Documents;
using System.Windows.Navigation;

// using COmponene ZeroMixcreatePlugns
using CheckBox = System.Windows.Controls.CheckBox;
using Grid = System.Windows.Controls.Grid;
using GridLength = System.Windows.GridLength;
using GridUnitType = System.Windows.GridUnitType;
using FontFamily = System.Windows.Media.FontFamily;
using Brushes = System.Windows.Media.Brushes;
using Cursor = System.Windows.Input.Cursor;
using Cursors = System.Windows.Input.Cursors;
using Button = System.Windows.Controls.Button;
using Orientation = System.Windows.Controls.Orientation;
using Brush = System.Windows.Media.Brush;

namespace ZeroMix
{
    using ZeroMix.zeromix.CreatePlugins;
    
    public partial class MainWindow : Window
    {
        private const string CURRENT_VERSION = "2.4.0";
        
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
        private RecordingManager? _recordingManager;
        private GlobalHotkeyManager? _hotkeyManager;
        private bool _isRecordingActive = false;
        private DispatcherTimer? _recordDurationTimer;
        private Key _currentRecordHotkey = Key.F9;
        private bool _isPickingHotkey = false;
        
        // Cache untuk System Health agar tidak query WMI setiap tick
        private long _cachedTotalRAM = 0;
        private string? _cachedOsVersion;
        private string? _cachedProcessor;

        private string[]? _startupArgs;

        public void ChangeLanguage(string cultureCode)
        {
            var dict = new ResourceDictionary();
            switch (cultureCode)
            {
                case "id-ID":
                    dict.Source = new Uri("Resources/Locales/id-ID.xaml", UriKind.Relative);
                    break;
                case "ja-JP":
                    dict.Source = new Uri("Resources/Locales/ja-JP.xaml", UriKind.Relative);
                    break;
                case "zh-CN":
                    dict.Source = new Uri("Resources/Locales/zh-CN.xaml", UriKind.Relative);
                    break;
                default:
                    dict.Source = new Uri("Resources/Locales/en-US.xaml", UriKind.Relative);
                    break;
            }

            // Find old dictionary (the one containing 'Wiz_Welcome') and remove it
            var oldDict = System.Windows.Application.Current.Resources.MergedDictionaries.FirstOrDefault(
                d => d.Source != null && d.Source.OriginalString.Contains("Resources/Locales/"));
            
            if (oldDict != null)
            {
                System.Windows.Application.Current.Resources.MergedDictionaries.Remove(oldDict);
            }
            
            System.Windows.Application.Current.Resources.MergedDictionaries.Add(dict);
        }

        public MainWindow(string[]? args = null)
        {
            _startupArgs = args;
            
            // Auto-Detect Installer Language Selection
            try 
            {
                string langFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "language.ini");
                if (File.Exists(langFile))
                {
                    string code = File.ReadAllText(langFile).Trim();
                    ChangeLanguage(code);
                }
            } 
            catch { /* Ignore if fails, default to EN */ }

            // Register Lua Bridge
            MoonSharp.Interpreter.UserData.RegisterType<Plugins.ZeroMixLuaApi>();
            
            InitializeComponent();
            InitializeTrayIcon();
            InitializeTaskbarWatcher();
            // InitializeRecorder(); // Removed to prevent startup crash, handled in background task below
            this.MouseLeftButtonDown += MainWindow_MouseLeftButtonDown;

            // Register Global Hotkey (F9) immediately
            this.Loaded += (s, e) => {
                try {
                    _hotkeyManager = new GlobalHotkeyManager();
                    _hotkeyManager.Register(this);
                    _hotkeyManager.HotkeyPressed += () => {
                        Dispatcher.Invoke(() => ZeroRecordBtn_Click(this, new RoutedEventArgs()));
                    };
                } catch { }
            };

            // Pre-load recording engine to prevent lag with robust FFmpeg path detection
            Task.Run(() => {
                try {
                    string ffmpegPath = ResolveFFmpegPath();
                    _recordingManager = new RecordingManager(ffmpegPath);
                } catch { }
            });
        }

        private string ResolveFFmpegPath()
        {
            // Try priority locations:
            var possiblePaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FFMPEG", "ffmpeg.exe"),
                Path.Combine(Directory.GetCurrentDirectory(), "FFMPEG", "ffmpeg.exe"),
                // If we are in bin/Debug/..., go up 3 levels to find project root
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "FFMPEG", "ffmpeg.exe")
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    Console.WriteLine($"[ZeroMix] Found FFmpeg at: {path}");
                    return path;
                }
            }

            // Fallback to absolute path user mentioned if all else fails
            string userPath = @"c:\ZeroMix\ZeroMix\FFMPEG\ffmpeg.exe";
            if (File.Exists(userPath)) return userPath;

            return "ffmpeg.exe"; // Try system PATH
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

            // Initialize Language Selector
            InitializeLanguageSelector();

            // Initialize Lua Engine
            _pluginEngine = new Plugins.PluginEngine(this);
            _pluginEngine.Start();

            // Handle Startup Args (Toggle Plugins via Shortcut)
            if (_startupArgs != null && _startupArgs.Length >= 2 && _startupArgs[0] == "--plugin")
            {
                string targetPlugin = _startupArgs[1];
                var plugin = _pluginEngine.GetPlugins().FirstOrDefault(p => p.Name == targetPlugin);
                if (plugin != null) _pluginEngine.TogglePlugin(plugin);
            }
        }

        private void InitializeLanguageSelector()
        {
            var comboBox = this.FindName("LanguageComboBox") as System.Windows.Controls.ComboBox;
            if (comboBox != null)
            {
                // Read current language from language.ini
                string languageFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "language.ini");
                string currentLang = "en-US";
                
                if (File.Exists(languageFile))
                {
                    try
                    {
                        currentLang = File.ReadAllText(languageFile).Trim();
                    }
                    catch { }
                }

                // Set combobox to current language
                foreach (ComboBoxItem item in comboBox.Items)
                {
                    if (item.Tag?.ToString() == currentLang)
                    {
                        comboBox.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string selectedLanguage = selectedItem.Tag?.ToString() ?? "en-US";
                
                // Save to language.ini
                string languageFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "language.ini");
                try
                {
                    File.WriteAllText(languageFile, selectedLanguage);
                }
                catch
                {
                    System.Windows.MessageBox.Show("Failed to save language preference.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Instantly apply language
                ChangeLanguage(selectedLanguage);

                // Update Status or specific UI elements if they don't use DynamicResource
                StatusLabel.Text = "Language updated to " + (selectedItem.Content?.ToString() ?? "Default");
            }
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
            contextMenu.Items.Add("ZeroMix Studio (Editor)", null, (s, args) => OpenVideoEditor());
            contextMenu.Items.Add(new ToolStripSeparator());
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

                // RAM Usage - gunakan cached totalRAM, tidak perlu query WMI setiap tick
                float availableRam = _ramCounter!.NextValue();
                
                // Cache totalRAM saat pertama kali
                if (_cachedTotalRAM == 0)
                {
                    Task.Run(() => {
                        try {
                            ManagementClass managementClass = new ManagementClass("Win32_ComputerSystem");
                            foreach (ManagementObject obj in managementClass.GetInstances())
                            {
                                _cachedTotalRAM = Convert.ToInt64(obj["TotalPhysicalMemory"]) / (1024 * 1024);
                                break;
                            }
                        } catch { _cachedTotalRAM = 8192; } // Default 8GB jika gagal
                    });
                    _cachedTotalRAM = 8192; // Temporary default
                }

                float usedRam = _cachedTotalRAM - (int)availableRam;
                float ramPercent = (usedRam / _cachedTotalRAM) * 100;

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

        private async void UpdateSystemInfo()
        {
            try
            {
                // Jalankan semua WMI queries di background thread untuk menghindari blocking UI
                await Task.Run(() => 
                {
                    try {
                        // OS Version (cache)
                        if (string.IsNullOrEmpty(_cachedOsVersion))
                        {
                            ManagementClass osClass = new ManagementClass("Win32_OperatingSystem");
                            foreach (ManagementObject os in osClass.GetInstances())
                            {
                                _cachedOsVersion = os["Caption"]?.ToString() ?? "Unknown OS";
                                break;
                            }
                        }

                        // Processor (cache)
                        if (string.IsNullOrEmpty(_cachedProcessor))
                        {
                            ManagementClass procClass = new ManagementClass("Win32_Processor");
                            foreach (ManagementObject proc in procClass.GetInstances())
                            {
                                _cachedProcessor = proc["Name"]?.ToString() ?? "Unknown Processor";
                                break;
                            }
                        }

                        // RAM (cache)
                        if (_cachedTotalRAM == 0)
                        {
                            ManagementClass ramClass = new ManagementClass("Win32_ComputerSystem");
                            foreach (ManagementObject ram in ramClass.GetInstances())
                            {
                                _cachedTotalRAM = Convert.ToInt64(ram["TotalPhysicalMemory"]) / (1024 * 1024);
                                break;
                            }
                        }
                    } catch { }
                });

                // Update UI di main thread
                OsVersionText.Text = _cachedOsVersion ?? "Unknown OS";
                ProcessorText.Text = _cachedProcessor ?? "Unknown Processor";
                TotalRamText.Text = $"RAM: {_cachedTotalRAM / 1024} GB";

                // Network - ini lebih cepat, bisa langsung di UI thread
                int activeNetworks = 0;
                try {
                    await Task.Run(() => {
                        ManagementClass netClass = new ManagementClass("Win32_NetworkAdapterConfiguration");
                        foreach (ManagementObject net in netClass.GetInstances())
                        {
                            if ((bool?)net["IPEnabled"] == true)
                                activeNetworks++;
                        }
                    });
                } catch { }
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
            if (PluginsContent != null) PluginsContent.Visibility = Visibility.Collapsed;
            if (RecorderContent != null) RecorderContent.Visibility = Visibility.Collapsed;

            if (_performanceTimer != null) _performanceTimer.Stop();

            if (HomeButton != null) HomeButton.Background = System.Windows.Media.Brushes.Transparent;
            if (AboutButton != null) AboutButton.Background = System.Windows.Media.Brushes.Transparent;
            if (PrivacyButton != null) PrivacyButton.Background = System.Windows.Media.Brushes.Transparent;
            if (WallpaperButton != null) WallpaperButton.Background = System.Windows.Media.Brushes.Transparent;
            if (PluginsButton != null) PluginsButton.Background = System.Windows.Media.Brushes.Transparent;
            if (RecorderButton != null) RecorderButton.Background = System.Windows.Media.Brushes.Transparent;
        }


        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (_isPickingHotkey)
            {
                _currentRecordHotkey = e.Key;
                HotkeyDisplayText.Text = e.Key.ToString();
                _isPickingHotkey = false;
                HotkeyBorder.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(22, 27, 38));
                e.Handled = true;
                return;
            }

            if (e.Key == _currentRecordHotkey)
            {
                if (RecorderContent.Visibility == Visibility.Visible)
                {
                    ZeroRecordBtn_Click(this, new RoutedEventArgs());
                }
            }
        }

        private void HotkeyBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isPickingHotkey = true;
            HotkeyDisplayText.Text = "PRESS KEY...";
            HotkeyBorder.Background = (SolidColorBrush)FindResource("NeonBlueBrush");
        }

        private async void LoadAudioDevices()
        {
            if (MicComboBox == null || SpeakerComboBox == null) return;

            MicComboBox.Items.Clear();
            SpeakerComboBox.Items.Clear();
            
            MicComboBox.Items.Add(new ComboBoxItem { Content = "Default System Microphone" });
            MicComboBox.Items.Add(new ComboBoxItem { Content = "No Audio" });
            SpeakerComboBox.Items.Add(new ComboBoxItem { Content = "Default System Speaker" });
            SpeakerComboBox.Items.Add(new ComboBoxItem { Content = "No Audio" });
            
            MicComboBox.SelectedIndex = 0;
            SpeakerComboBox.SelectedIndex = 0;

            try
            {
                string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FFMPEG", "ffmpeg.exe");
                var deviceNames = await Task.Run(() =>
                {
                    var names = new System.Collections.Generic.List<string>();
                    var psi = new ProcessStartInfo
                    {
                        FileName = ffmpegPath,
                        Arguments = "-list_devices true -f dshow -i dummy",
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var p = Process.Start(psi);
                    if (p == null) return names;
                    
                    string output = p.StandardError.ReadToEnd();
                    bool captureNext = false;
                    foreach (var line in output.Split('\n'))
                    {
                        if (line.Contains("DirectShow audio devices")) captureNext = true;
                        else if (line.Contains("DirectShow video devices")) captureNext = false;
                        
                        if (captureNext && line.Contains("\""))
                        {
                            var match = System.Text.RegularExpressions.Regex.Match(line, "\"(.*?)\"");
                            if (match.Success)
                            {
                                string name = match.Groups[1].Value;
                                if (!names.Contains(name)) names.Add(name);
                            }
                        }
                    }
                    return names;
                });

                foreach (var name in deviceNames)
                {
                    MicComboBox.Items.Add(new ComboBoxItem { Content = name });
                    // Usually we don't pick speakers from dshow as wasapi loopback is better, 
                    // but for completeness we can list them or let the user choose.
                }
            }
            catch { }
        }

        private void RecorderButton_Click(object sender, RoutedEventArgs e)
        {
            DeactivateAllTabs();
            RecorderContent.Visibility = Visibility.Visible;
            RecorderButton.Background = (System.Windows.Media.SolidColorBrush)FindResource("NavSelectedBrush");
            
            // Load audio devices
            LoadAudioDevices();
        }


        private void PluginsButton_Click(object sender, RoutedEventArgs e)
        {
            DeactivateAllTabs();
            PluginsContent.Visibility = Visibility.Visible;
            PluginsButton.Background = (System.Windows.Media.SolidColorBrush)FindResource("NavSelectedBrush");
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

        private void NavWallpapers_Click(object sender, RoutedEventArgs e)
        {
            DeactivateAllTabs();
            WallpapersContent.Visibility = Visibility.Visible;
            WallpaperButton.Background = (System.Windows.Media.SolidColorBrush)FindResource("NavSelectedBrush");
        }

        // WallpaperButton_Click removed as it is no longer used (Wallpapers view is now integrated)

        // --- Dashboard Logic --- //

        private async void EnableMonitoringBtn_Click(object sender, RoutedEventArgs e)
        {
            MonitoringPanel.Visibility = Visibility.Visible;
            StatusLabel.Text = "Initializing System Probes...";

            if (_performanceTimer == null)
            {
                await Task.Run(() => 
                {
                    InitializePerformanceCounters();
                    Dispatcher.Invoke(() => 
                    {
                        UpdateSystemInfo();
                        RefreshProcessList();
                    });
                });
            }
            
            _performanceTimer?.Start();
            StatusLabel.Text = "System Monitor Active";
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

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                e.Handled = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GPUCompositor] FATAL: Compose failed: {ex.Message}");
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
                System.Windows.MessageBox.Show($"Error: {ex.Message}", "Ghost Taskbar Error");
            }
        }

        private void InitializeRecorder()
        {
            string ffmpegPath = ResolveFFmpegPath();
            _recordingManager = new RecordingManager(ffmpegPath);
        }

        private async void ZeroRecordBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_recordingManager == null) return;

            // Prevent spam clicks
            if (HomeRecordBtn != null) HomeRecordBtn.IsEnabled = false;

            if (!_isRecordingActive)
            {
                StatusLabel.Text = "Booting Recorder Engine...";
                if (HomeRecordBtnText != null) HomeRecordBtnText.Text = "STARTING...";

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                
                // Get FPS from UI
                int fps = 30;
                if (FpsComboBox != null)
                {
                    switch (FpsComboBox.SelectedIndex)
                    {
                        case 1: fps = 45; break;
                        case 2: fps = 50; break;
                        case 3: fps = 60; break;
                    }
                }

                // Get Audio Devices
                string mic = (MicComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "No Audio";
                string speaker = (SpeakerComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "No Audio";

                // Check if FFmpeg exists
                if (!File.Exists(_recordingManager.FFmpegPath))
                {
                    System.Windows.MessageBox.Show($"FFmpeg not found at:\n{_recordingManager.FFmpegPath}\n\nPlease check the FFMPEG folder.", "ZeroRecord Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    if (HomeRecordBtn != null) HomeRecordBtn.IsEnabled = true;
                    if (HomeRecordBtnText != null) HomeRecordBtnText.Text = "START RECORD";
                    return;
                }

                // We no longer block on _recordingManager.IsInitialized because it has a GDI fallback now.
                // Just start the recording.

                bool started = await Task.Run(() => 
                {
                    try 
                    {
                        _recordingManager.StartRecording($"ZeroRecord_{timestamp}.mp4", fps, mic, speaker);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() => System.Windows.MessageBox.Show("Failed to start recording: " + ex.Message));
                        return false;
                    }
                });

                if (started)
                {
                    _isRecordingActive = true;
                    UpdateRecordUI(true);
                    
                    // Start duration UI timer
                    RecordDurationText.Visibility = Visibility.Visible;
                    if (_recordDurationTimer == null)
                    {
                        _recordDurationTimer = new DispatcherTimer();
                        _recordDurationTimer.Interval = TimeSpan.FromSeconds(1);
                        _recordDurationTimer.Tick += (s, args) => {
                            string dur = _recordingManager.GetDuration();
                            RecordDurationText.Text = dur;
                            if (_notifyIcon != null) _notifyIcon.Text = $"🔴 RECORDING - {dur}";
                        };
                    }
                    _recordDurationTimer.Start();
                    
                    if (_notifyIcon != null)
                    {
                        _notifyIcon.BalloonTipTitle = "ZeroRecord Started";
                        _notifyIcon.BalloonTipText = "Recording your desktop screen...";
                        _notifyIcon.ShowBalloonTip(2000);
                    }
                    StatusLabel.Text = "Recording Active";
                }
                else
                {
                     UpdateRecordUI(false);
                }
            }
            else
            {
                StatusLabel.Text = "Finalizing Video...";
                if (HomeRecordBtnText != null) HomeRecordBtnText.Text = "SAVING...";

                await Task.Run(() => 
                {
                     _recordingManager.StopRecording();
                });

                _isRecordingActive = false;
                _recordDurationTimer?.Stop();
                
                UpdateRecordUI(false);
                if (RecordDurationText != null)
                {
                    RecordDurationText.Visibility = Visibility.Collapsed;
                    RecordDurationText.Text = "00:00";
                }
                
                if (_notifyIcon != null)
                {
                    _notifyIcon.Text = "ZeroMix Dashboard";
                    _notifyIcon.BalloonTipTitle = "ZeroRecord Stopped";
                    _notifyIcon.BalloonTipText = "Video saved to Videos\\ZeroRecord folder.";
                    _notifyIcon.ShowBalloonTip(2000);
                }
                StatusLabel.Text = "Recording Saved to Videos\\ZeroRecord";
            }

            if (HomeRecordBtn != null) HomeRecordBtn.IsEnabled = true;
        }

        private void OpenRecordingsBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "ZeroRecord");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                Process.Start("explorer.exe", path);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Could not open recordings folder: " + ex.Message);
            }
        }

        private void UpdateRecordUI(bool isActive)
        {
            if (isActive)
            {
                if (HomeRecordBtnText != null) HomeRecordBtnText.Text = "STOP RECORD";
                if (HomeRecordBtn != null) HomeRecordBtn.Opacity = 1.0;
            }
            else
            {
                if (HomeRecordBtnText != null) HomeRecordBtnText.Text = "ZeroRecord";
                if (HomeRecordBtn != null) HomeRecordBtn.Opacity = 0.7;
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
            bool isTemplate = inputWin.IsTemplate;
            
            // folder prefix: user.pub for public/template, user.priv for private
            string folderName = $"user.{(isPublic || isTemplate ? "pub" : "priv")}.{pluginName}";
            string pluginDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins", folderName);

            try
            {
                // Create Directory
                Directory.CreateDirectory(pluginDir);

                // Create Lua Template
                string luaTemplate = "";
                
                if (isTemplate)
                {
                    luaTemplate = @$"-- ZeroMix Plugin: {pluginName} (Template)
-- Deskripsi: To-Do List dengan sistem simpan data.

function OnLoad()
    CreateUI('{pluginName}', 300, 400)
    AddLabel('Apakah ada Rencaa?')
    AddInput('task_input', '')
    AddButton('Tambah Tugas..', 'AddTask')
    
    -- Muat data lama dari file JSON
    local savedTasks = LoadConfig('tasks_data')
    if savedTasks ~= '' and savedTasks ~= nil then
        AddLabel('--- TUGAS TERSIMPAN ---')
        AddLabel(savedTasks)
    end
end

function AddTask()
    local task = GetInput('task_input')
    if task ~= '' and task ~= nil then
        AddLabel('• ' .. task)
        
        -- Simpan data (Append ke data lama atau simpan baru)
        local current = LoadConfig('tasks_data')
        local updated = current .. '\n• ' .. task
        SaveConfig('tasks_data', updated)
        Notify('Sukses', 'Tugas disimpan ke JSON!\n' .. updated)
    else
        Notify('Peringatan', 'Isi tugasnya dulu...')
    end
end";
                }
                else
                {
                    luaTemplate = @$"-- ZeroMix Plugin: {pluginName}
-- Created: {DateTime.Now}
-- Type: {(isPublic ? "Public" : "Private")}

function OnLoad()
    -- Masukkan logika kustom kamu di sini
    Log('Plugin {pluginName} aktif!')
end

function OnUpdate()
    -- Masukkan logika kustom kamu di sini
end";
                }
                
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
        public void RefreshUserPluginsUI()
        {
            if (UserPluginsContainer == null) return;

            UserPluginsContainer.Children.Clear();
            var engine = _pluginEngine;
            if (engine == null) return;

            foreach (var plugin in engine.GetPlugins())
            {
                // Create a card for each Lua plugin
                Border card = new Border
                {
                    Style = (Style)FindResource("CompactCardBorder")
                };

                Grid grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

                // Icon (Smaller)
                Border iconBorder = new Border
                {
                    Width = 45,
                    Height = 45,
                    CornerRadius = new CornerRadius(8),
                    Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 32, 44)),
                    Margin = new Thickness(0, 0, 15, 0)
                };
                iconBorder.Child = new TextBlock
                {
                    Text = "🧩",
                    FontSize = 22,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(iconBorder, 0);
                grid.Children.Add(iconBorder);

                // Info (Smaller)
                StackPanel infoStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                infoStack.Children.Add(new TextBlock
                {
                    Text = plugin.Name,
                    Foreground = System.Windows.Media.Brushes.White,
                    FontSize = 15,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 2)
                });
                infoStack.Children.Add(new TextBlock
                {
                    Text = "Plugin Lua • " + (plugin.IsEnabled ? "Aktif" : "Nonaktif"),
                    Foreground = (System.Windows.Media.Brush)FindResource("SubTextBrush"),
                    FontSize = 11
                });
                Grid.SetColumn(infoStack, 1);
                grid.Children.Add(infoStack);

                // Control Panel (Right Side)
                StackPanel controlStack = new StackPanel 
                { 
                    Orientation = Orientation.Horizontal, 
                    VerticalAlignment = VerticalAlignment.Center 
                };

                // Folder Button (Open Directory)
                Button folderBtn = new Button
                {
                    Content = "", // Folder Icon
                    FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    Background = Brushes.Transparent,
                    Foreground = (Brush)FindResource("SubTextBrush"),
                    BorderThickness = new Thickness(0),
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 10, 0),
                    Cursor = Cursors.Hand,
                    ToolTip = "Buka Folder Plugin"
                };
                folderBtn.Click += (s, e) => {
                    string? folder = Path.GetDirectoryName(plugin.Path);
                    if (folder != null) Process.Start("explorer.exe", folder);
                };
                controlStack.Children.Add(folderBtn);

                // Shortcut Button
                Button shortcutBtn = new Button
                {
                    Content = "", // Link/Shortcut Icon
                    FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    Background = Brushes.Transparent,
                    Foreground = (Brush)FindResource("SubTextBrush"),
                    BorderThickness = new Thickness(0),
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 10, 0),
                    Cursor = Cursors.Hand,
                    ToolTip = "Buat Shortcut di Desktop"
                };
                shortcutBtn.Click += (s, e) => {
                    CreateDesktopShortcut(plugin.Name);
                    System.Windows.MessageBox.Show($"Shortcut untuk '{plugin.Name}' berhasil dibuat di Desktop!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
                };
                controlStack.Children.Add(shortcutBtn);

                // Delete Button
                Button deleteBtn = new Button
                {
                    Content = "", // Trash Icon
                    FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    Background = Brushes.Transparent,
                    Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 107, 107)),
                    BorderThickness = new Thickness(0),
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 10, 0),
                    Cursor = Cursors.Hand,
                    ToolTip = "Hapus Permanen"
                };
                deleteBtn.Click += (s, e) => {
                    var result = System.Windows.MessageBox.Show(
                        $"Kamu yakin ingin menghapus plugin '{plugin.Name}'? Folder akan dihapus selamanya.",
                        "Hapus Plugin",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        engine.RemovePlugin(plugin);
                    }
                };
                controlStack.Children.Add(deleteBtn);

                // Control (Toggle)
                CheckBox toggle = new CheckBox
                {
                    IsChecked = plugin.IsEnabled,
                    VerticalAlignment = VerticalAlignment.Center,
                    LayoutTransform = new ScaleTransform(1.5, 1.5)
                };
                toggle.Click += (s, e) => {
                    engine.TogglePlugin(plugin);
                };
                controlStack.Children.Add(toggle);

                Grid.SetColumn(controlStack, 2);
                grid.Children.Add(controlStack);

                card.Child = grid;
                UserPluginsContainer.Children.Add(card);
            }
        }

        private void CreateDesktopShortcut(string pluginName)
        {
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string shortcutPath = Path.Combine(desktop, $"{pluginName}.lnk");
                string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                
                // PowerShell script to create WScript.Shell shortcut
                string command = $"$s=(New-Object -COM WScript.Shell).CreateShortcut('{shortcutPath}');$s.TargetPath='{exePath}';$s.Arguments='--plugin \"{pluginName}\"';$s.Save()";
                
                var proc = Process.Start(new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"-NoProfile -Command \"{command.Replace("'", "''")}\"",
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
                proc?.WaitForExit();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ERROR] Shortcut creation failed: " + ex.Message);
            }
        }

        private async void CheckUpdateBtn_Click(object sender, RoutedEventArgs e)
        {
            CheckUpdateBtn.IsEnabled = false;
            CheckUpdateBtn.Content = "Checking...";
            
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // GitHub API requires User-Agent
                    client.DefaultRequestHeaders.Add("User-Agent", "ZeroMix-Updater");
                    
                    // Ganti URL ini dengan URL repo Kakak jika sudah ada
                    string url = "https://api.github.com/repos/faizinuha/ZeroMix/releases/latest";
                    var response = await client.GetStringAsync(url);
                    
                    using (JsonDocument doc = JsonDocument.Parse(response))
                    {
                        string latestVersion = doc.RootElement.GetProperty("tag_name").GetString()?.Replace("v", "") ?? "0.0.0";
                        string downloadUrl = doc.RootElement.GetProperty("assets")[0].GetProperty("browser_download_url").GetString() ?? "";
                        
                        if (IsNewerVersion(latestVersion, CURRENT_VERSION))
                        {
                            UpdateBadge.Visibility = Visibility.Visible;
                            var result = System.Windows.MessageBox.Show(
                                $"Versi baru tersedia: v{latestVersion}\n\nApakah Kakak ingin download sekarang?", 
                                "ZeroMix Update", 
                                MessageBoxButton.YesNo, 
                                MessageBoxImage.Information);

                            if (result == MessageBoxResult.Yes)
                            {
                                Process.Start(new ProcessStartInfo(downloadUrl) { UseShellExecute = true });
                            }
                        }
                        else
                        {
                            System.Windows.MessageBox.Show("ZeroMix sudah versi terbaru! 😎", "No Update", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Gagal cek update. Pastikan internet Kakak nyala ya! 🌐", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine("[UPDATE] Error: " + ex.Message);
            }
            finally
            {
                CheckUpdateBtn.IsEnabled = true;
                CheckUpdateBtn.Content = "Check for Updates";
            }
        }

        private bool IsNewerVersion(string latest, string current)
        {
            try {
                Version vLatest = new Version(latest);
                Version vCurrent = new Version(current);
                return vLatest > vCurrent;
            } catch { return latest != current; }
        }

        private void OpenVideoEditor()
        {
            try
            {
                StudioWindow studio = new StudioWindow();
                studio.Show();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Gagal membuka ZeroMix Studio: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
