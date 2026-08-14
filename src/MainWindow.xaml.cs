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
using System.Windows.Threading;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ZeroMix.Recorder;
using ZeroMix.Widgets;
using System.Windows.Documents;
using System.Windows.Navigation;
using ZeroMix.ZeroShell;
using ZeroMix.Hotkeys;
using System.Windows.Media.Animation;
using ZeroMix.SleepMode;
using Wpf.Ui.Appearance;
using Wpf.Ui.Tray.Controls;

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
using Color = System.Drawing.Color;

namespace ZeroMix
{
    using ZeroMix.zeromix.CreatePlugins;
    using ZeroMix.Features.ZeroConnect;

    public partial class MainWindow : Wpf.Ui.Controls.FluentWindow, ZeroMix.Plugins.IZeroMixHost
    {
        private const string CURRENT_VERSION = "7.6.0-Demo";

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
        private Wpf.Ui.Tray.Controls.NotifyIcon? _wpfTray;
        private System.Windows.Forms.NotifyIcon? _notifyIcon;
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
        private DispatcherTimer? _gameDetectTimer;
        private IntPtr _detectedGameHandle = IntPtr.Zero;
        private RecordingBorderWindow? _recordingBorder;
        private System.Windows.Rect _selectedCaptureRect = System.Windows.Rect.Empty;
        private GlobalHotkeyManager? _hotkeyManager;
        private string _selectedRecordingMode = "FullScreen";
        private bool _isRecordingActive = false;
        private DispatcherTimer? _recordDurationTimer;
        private Key _currentRecordHotkey = Key.F9;
        private bool _isPickingHotkey = false;

        // Cache untuk System Health agar tidak query WMI setiap tick
        private long _cachedTotalRAM = 0;
        private string? _cachedOsVersion;
        private string? _cachedProcessor;

        private string[]? _startupArgs;
        private Virtual_Assisten.VirtualAssistantWindow? _assistantWindow;
        private System.Collections.ObjectModel.ObservableCollection<RecordingHistoryItem> _recordingHistory = new();
        private Window? _zeroShellWindow;
        private bool _isSidebarCollapsed = false;
        private SleepManager? _sleepManager;

        // ── IPluginHost + IZeroMixHost implementation ───────────────────────
        public string HostName => "ZeroMix";
        public string HostVersion => CURRENT_VERSION;

        public void Dispatch(Action action) => Dispatcher.Invoke(action);

        public void Log(string message) => System.Diagnostics.Debug.WriteLine($"[Plugin] {message}");

        public void SetStatus(string text) => StatusLabel.Text = text;

        public void ShowNotification(string title, string message) =>
            System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

        public double GetCpuUsage()
        {
            double val = 0;
            Dispatcher.Invoke(() =>
            {
                if (double.TryParse(CpuPercentText.Text.Replace(" %", ""), out double r)) val = r;
            });
            return val;
        }

        public double GetRamUsage()
        {
            double val = 0;
            Dispatcher.Invoke(() =>
            {
                if (double.TryParse(RamPercentText.Text.Replace(" %", ""), out double r)) val = r;
            });
            return val;
        }

        public double GetDiskUsage()
        {
            double val = 0;
            Dispatcher.Invoke(() =>
            {
                if (double.TryParse(DiskPercentText.Text.Replace(" %", ""), out double r)) val = r;
            });
            return val;
        }

        public T? GetService<T>() where T : class
        {
            // ZeroMix expose semua service-nya sendiri
            if (typeof(T).IsAssignableFrom(typeof(MainWindow))) return this as T;
            return null;
        }
        // ────────────────────────────────────────────────────────────────────

        private void HamburgerBtn_Click(object sender, RoutedEventArgs e)
        {
            _isSidebarCollapsed = !_isSidebarCollapsed;
            double targetWidth = _isSidebarCollapsed ? 60 : 220;

            // Sidebar Width Animation
            var anim = new DoubleAnimation(targetWidth, TimeSpan.FromSeconds(0.3));
            anim.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            SidebarPane.BeginAnimation(FrameworkElement.WidthProperty, anim);

            // Hide/Show Text Labels & Center Icons
            var visibility = _isSidebarCollapsed ? Visibility.Collapsed : Visibility.Visible;
            var iconMargin = _isSidebarCollapsed ? new Thickness(0) : new Thickness(0, 0, 15, 0);

            if (NavTextHome != null) NavTextHome.Visibility = visibility;
            if (NavTextWall != null) NavTextWall.Visibility = visibility;
            if (NavTextPlugins != null) NavTextPlugins.Visibility = visibility;
            if (NavTextRec != null) NavTextRec.Visibility = visibility;
            if (NavTextAsst != null) NavTextAsst.Visibility = visibility;
            if (NavTextAbout != null) NavTextAbout.Visibility = visibility;

            // Adjust Icon Margins
            if (IconHome != null) IconHome.Margin = iconMargin;
            if (IconWall != null) IconWall.Margin = iconMargin;
            if (IconPlugins != null) IconPlugins.Margin = iconMargin;
            if (IconRec != null) IconRec.Margin = iconMargin;
            if (IconAsst != null) IconAsst.Margin = iconMargin;
            if (IconAbout != null) IconAbout.Margin = iconMargin;

            // Handle logo display or other elements if needed
            if (StatusLabel != null) StatusLabel.Visibility = visibility;
            if (LanguageComboBox != null) LanguageComboBox.Visibility = visibility;
        }

        public void ChangeLanguage(string cultureCode)
        {
            // Validasi code
            var validCodes = new[] { "en-US", "id-ID", "ja-JP", "zh-CN", "ko-KR" };
            if (!validCodes.Contains(cultureCode))
                cultureCode = "en-US";

            // Pakai path absolut agar bekerja baik saat dev maupun installed
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string localeFile = Path.Combine(baseDir, "Assets", "Resources", "Locales", $"{cultureCode}.xaml");

            // Fallback ke en-US jika file tidak ada
            if (!File.Exists(localeFile))
                localeFile = Path.Combine(baseDir, "Assets", "Resources", "Locales", "en-US.xaml");

            try
            {
                var dict = new ResourceDictionary
                {
                    Source = new Uri(localeFile, UriKind.Absolute)
                };

                // Hapus locale lama, tambah yang baru
                var oldDict = System.Windows.Application.Current.Resources.MergedDictionaries
                    .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Locales"));

                if (oldDict != null)
                    System.Windows.Application.Current.Resources.MergedDictionaries.Remove(oldDict);

                System.Windows.Application.Current.Resources.MergedDictionaries.Add(dict);

                // Update hardcoded UI text yang tidak pakai DynamicResource
                ApplyLanguageToStaticElements(dict);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Language] Failed to load {cultureCode}: {ex.Message}");
            }
        }

        private void ApplyLanguageToStaticElements(ResourceDictionary dict)
        {
            try
            {
                string Get(string key) => dict.Contains(key) ? dict[key]?.ToString() ?? "" : "";

                // Sidebar nav labels
                if (NavTextHome != null) NavTextHome.Text = Get("Nav_Home");
                if (NavTextWall != null) NavTextWall.Text = Get("Nav_Wallpapers");
                if (NavTextPlugins != null) NavTextPlugins.Text = Get("Nav_Plugins");
                if (NavTextRec != null) NavTextRec.Text = Get("Nav_Record");
                if (NavTextAsst != null) NavTextAsst.Text = Get("Nav_AI") is { Length: > 0 } s ? s : "AI Companions";
                if (NavTextAbout != null) NavTextAbout.Text = Get("Nav_About");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Language] ApplyLanguageToStaticElements error: {ex.Message}");
            }
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
            // Subscribe to settings changes to toggle Welcome quick actions
            try
            {
                SettingsService.Instance.OnChanged += (_, __) => Dispatcher.Invoke(UpdateWelcomeVisibility);
                // Apply immediately
                Dispatcher.BeginInvoke(new Action(() => UpdateWelcomeVisibility()), DispatcherPriority.Loaded);
            }
            catch { }

            // set content rendered handler to init tray when render ready
            this.ContentRendered += OnContentRenderedInitTray;
            // ★ Bagian 7 — Fix white flash:
            // SEBELUMNYA: this.Opacity = 0 + fade-in di Window_Loaded.
            // Ini justru MENYEBABKAN flash: Opacity < 1 memaksa WPF memakai
            // WS_EX_LAYERED (layered window), dan DWM TIDAK menerapkan Mica
            // (DWMWA_SYSTEMBACKDROP_TYPE) pada layered window. Akibatnya Mica
            // baru ter-attach SETELAH animasi fade-in selesai (opacity kembali 1)
            // → momen itulah flash putih muncul.
            // FIX: hapus hack Opacity, dan ganti Background window jadi gelap
            // (#FF141922 di XAML) sebagai fallback frame sebelum Mica siap.
            ApplicationThemeManager.Apply(this);
            StartGameDetection();
            // Tray icon diinisialisasi via XAML (ui:FluentWindow.Tray)
            // InitializeTrayIcon dipanggil di Window_Loaded setelah HWND tersedia
            // Initialize SleepMode
            _sleepManager = new SleepManager();

            // Berikan handle HotkeyCore ke SleepManager (untuk pendaftaran shortcut)
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (App.HotkeyCoreInstance != null)
                {
                    var helper = new System.Windows.Interop.WindowInteropHelper(App.HotkeyCoreInstance);
                    _sleepManager.SetHotkeyHandle(helper.Handle);
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
            InitializeTaskbarWatcher();
            // InitializeRecorder(); // Removed to prevent startup crash, handled in background task below

            // Register Global Hotkey (F9) immediately
            this.Loaded += (s, e) =>
            {
                try
                {
                    _hotkeyManager = new GlobalHotkeyManager();
                    _hotkeyManager.Register(this);
                    _hotkeyManager.HotkeyPressed += () =>
                    {
                        Dispatcher.Invoke(() => ZeroRecordBtn_Click(this, new RoutedEventArgs()));
                    };
                }
                catch { }
            };

            // RecordingManager di-init lazy saat user buka halaman Capture
            // Tidak pre-load di startup agar hemat RAM ~20-30 MB
        }

        private string ResolveFFmpegPath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string cwd = Directory.GetCurrentDirectory();

            var possiblePaths = new[]
            {
                Path.Combine(baseDir, "Tools", "FFMPEG", "ffmpeg.exe"),
                Path.Combine(baseDir, "FFMPEG", "ffmpeg.exe"),
                Path.Combine(cwd, "Tools", "FFMPEG", "ffmpeg.exe"),
                Path.Combine(cwd, "FFMPEG", "ffmpeg.exe"),
                // project root when running from bin/Debug/net9.0-windows/win-x64/
                Path.Combine(baseDir, "..", "..", "..", "..", "Tools", "FFMPEG", "ffmpeg.exe"),
                @"c:\ZeroMix\ZeroMix\Tools\FFMPEG\ffmpeg.exe",
            };

            foreach (var path in possiblePaths)
            {
                string full = Path.GetFullPath(path);
                if (File.Exists(full))
                {
                    Console.WriteLine($"[ZeroMix] Found FFmpeg at: {full}");
                    return full;
                }
            }

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
            // Fade-in opacity DIHAPUS (Bagian 7) — animasi opacity < 1 membuat
            // window layered, yang menghalangi DWM attach Mica (lihat constructor).

            // Set initial view after the window has loaded
            HomeButton_Click(this, new RoutedEventArgs());
            _initialWallpaperPath = GetSystemWallpaperPath();

            // Initialize Language Selector
            InitializeLanguageSelector();

            // If a XAML-declared WPF-UI.Tray.NotifyIcon resource exists, prefer it (avoids reflection/fallback)
            try
            {
                if (this.Resources.Contains("AppTrayResource"))
                {
                    var appTray = this.Resources["AppTrayResource"] as Wpf.Ui.Tray.Controls.NotifyIcon;
                    if (appTray != null)
                    {
                        _wpfTray = appTray;
                        Console.WriteLine("[Tray] Using resource-declared AppTray instance");
                    }
                }
            }
            catch { }

            // Setup tray icon langsung di Loaded
            InitializeTrayIcon();
            // Ensure native tray is present as a robust fallback if user can't see WPF/WinForms icon
            EnsureNativeTrayIfNeeded();
            // Install an extra message hook to show the app context menu when native tray is right-clicked.
            try
            {
                if (_hwndSource != null)
                {
                    _hwndSource.AddHook((IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
                    {
                        try
                        {
                            if (msg == WM_TRAY_CALLBACK)
                            {
                                int ev = lParam.ToInt32();
                                if (ev == 0x0205) // WM_RBUTTONUP
                                {
                                    Dispatcher.Invoke(ShowTrayContextMenu);
                                    handled = true;
                                }
                            }
                        }
                        catch { }
                        return IntPtr.Zero;
                    });
                }
            }
            catch { }

            // Tunda semua operasi berat agar window selesai render dulu
            Dispatcher.BeginInvoke(async () =>
            {
                // Welcome screen — disabled (v6.9.9)
                // ZeroMix.Plugins.Welcome.WelcomePlugin.TryShowWelcome();

                // Load VA thumbnails — delay lebih lama agar view sudah di-render
                await Task.Delay(800);
                LoadVAThumbnails();

                // Load CatGatekeeper plugin secara lazy dan aman
                LoadCatGatekeeperPlugin();

                // Initialize Lua Engine di background
                await Task.Run(() =>
                {
                    try
                    {
                        var engine = new Plugins.PluginEngine(this);
                        engine.Start();
                        Dispatcher.Invoke(() => _pluginEngine = engine);
                    }
                    catch { }
                });

                // Silent update check — paling terakhir, tidak urgent
                await Task.Delay(2000);
                _ = CheckUpdateSilentAsync();

                // Handle Startup Args
                if (_startupArgs != null && _startupArgs.Length >= 2 && _startupArgs[0] == "--plugin")
                {
                    await Task.Delay(500);
                    string targetPlugin = _startupArgs[1];
                    var plugin = _pluginEngine?.GetPlugins().FirstOrDefault(p => p.Name == targetPlugin);
                    if (plugin != null) _pluginEngine?.TogglePlugin(plugin);
                }

            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        private void LoadVAThumbnails()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var thumbs = new[]
            {
                (Image: FrierenThumb, File: Path.Combine("Virtual_Assisten", "VA_Thumbnails", "Frieren.png")),
                (Image: FernThumb,    File: Path.Combine("Virtual_Assisten", "VA_Thumbnails", "fern.jpg")),
                (Image: HuohuoThumb, File: Path.Combine("Virtual_Assisten", "VA_Thumbnails", "Huohuo.jpg")),
                (Image: JianThumb, File: Path.Combine("Virtual_Assisten", "VA_Thumbnails", "JaneDo.png")),
            };

            foreach (var (img, file) in thumbs)
            {
                try
                {
                    string fullPath = Path.Combine(baseDir, file);
                    if (!File.Exists(fullPath))
                    {
                        Debug.WriteLine($"[VA] Thumbnail not found: {fullPath}");
                        continue;
                    }

                    var bmp = new System.Windows.Media.Imaging.BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(fullPath, UriKind.Absolute);
                    bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bmp.DecodePixelWidth = 300;
                    bmp.EndInit();
                    bmp.Freeze();

                    img.Source = bmp;
                    Debug.WriteLine($"[VA] Thumbnail loaded: {file}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[VA] Failed to load thumbnail {file}: {ex.Message}");
                }
            }
        }

        private void UpdateWelcomeVisibility()
        {
            try
            {
                var m = SettingsService.Instance.Model;
                if (EnableMonitoringBtn != null) EnableMonitoringBtn.Visibility = m.ShowFeature1 ? Visibility.Visible : Visibility.Collapsed;
                if (OpenClockBtn != null) OpenClockBtn.Visibility = m.ShowFeature2 ? Visibility.Visible : Visibility.Collapsed;
                if (TaskbarToggleBtn != null) TaskbarToggleBtn.Visibility = m.ShowFeature3 ? Visibility.Visible : Visibility.Collapsed;
                if (HomeRecordBtn != null) HomeRecordBtn.Visibility = m.ShowFeature4 ? Visibility.Visible : Visibility.Collapsed;
                if (SleepSettingsBtn != null) SleepSettingsBtn.Visibility = m.ShowFeature5 ? Visibility.Visible : Visibility.Collapsed;
                if (EditorBtn != null) EditorBtn.Visibility = m.ShowFeature6 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Settings] UpdateWelcomeVisibility error: {ex.Message}");
            }
        }

        private void LoadCatGatekeeperPlugin()
        {
            try
            {
                if (CatGatekeeperContainer == null) return;
                var ui = new global::zeromix.CatGatekeeper.CatGatekeeperUI();
                CatGatekeeperContainer.Content = ui;
                Console.WriteLine("[Plugin] CatGatekeeper loaded successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Plugin] CatGatekeeper failed to load: {ex.Message}");
                // Gagal load plugin tidak boleh crash aplikasi
            }
        }

        private void InitializeLanguageSelector()
        {
            var comboBox = this.FindName("LanguageComboBox") as System.Windows.Controls.ComboBox;
            if (comboBox != null)
            {
                // Read current language — check AppData first, then BaseDirectory fallback
                string appDataFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ZeroMix", "language.ini");
                string baseDirFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "language.ini");
                string currentLang = "en-US";

                try
                {
                    if (File.Exists(appDataFile))
                        currentLang = File.ReadAllText(appDataFile).Trim();
                    else if (File.Exists(baseDirFile))
                        currentLang = File.ReadAllText(baseDirFile).Trim();
                }
                catch { }

                // Suppress SelectionChanged during init
                comboBox.SelectionChanged -= LanguageComboBox_SelectionChanged;
                foreach (ComboBoxItem item in comboBox.Items)
                {
                    if (item.Tag?.ToString() == currentLang)
                    {
                        comboBox.SelectedItem = item;
                        break;
                    }
                }
                comboBox.SelectionChanged += LanguageComboBox_SelectionChanged;

                // Apply the saved language
                ChangeLanguage(currentLang);
            }
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string selectedLanguage = selectedItem.Tag?.ToString() ?? "en-US";

                // Save to AppData
                string appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ZeroMix");
                Directory.CreateDirectory(appDataDir);
                string languageFile = Path.Combine(appDataDir, "language.ini");

                try { File.WriteAllText(languageFile, selectedLanguage); }
                catch (Exception ex) { Debug.WriteLine($"[Language] Save failed: {ex.Message}"); }

                // Apply language instantly — no reload needed
                ChangeLanguage(selectedLanguage);
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

        private void OnContentRenderedInitTray(object? sender, EventArgs e)
        {
            this.ContentRendered -= OnContentRenderedInitTray;
            if (TryInitWpfTray()) { EnsureNativeTrayIfNeeded(); return; }
            // WPF tray unavailable; skipping WinForms fallback (disabled).
        }

        private bool TryInitWpfTray()
        {
            try
            {
                var wpfTray = new global::Wpf.Ui.Tray.Controls.NotifyIcon();

                // Try common tooltip properties
                try { wpfTray.GetType().GetProperty("ToolTipText")?.SetValue(wpfTray, "ZeroMix"); } catch { try { wpfTray.GetType().GetProperty("ToolTip")?.SetValue(wpfTray, "ZeroMix"); } catch { } }

                // Try set icon if possible
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Icons", "zeromix.ico");
                if (File.Exists(iconPath))
                {
                    var pIcon = wpfTray.GetType().GetProperty("Icon") ?? wpfTray.GetType().GetProperty("IconSource") ?? wpfTray.GetType().GetProperty("Image");
                    if (pIcon != null && pIcon.CanWrite)
                    {
                        var propType = pIcon.PropertyType;
                        try
                        {
                            if (propType == typeof(System.Drawing.Icon))
                            {
                                pIcon.SetValue(wpfTray, new System.Drawing.Icon(iconPath));
                            }
                            else if (typeof(System.Windows.Media.ImageSource).IsAssignableFrom(propType))
                            {
                                var bmp = new BitmapImage();
                                bmp.BeginInit();
                                bmp.UriSource = new Uri(iconPath, UriKind.Absolute);
                                bmp.CacheOption = BitmapCacheOption.OnLoad;
                                bmp.EndInit();
                                pIcon.SetValue(wpfTray, bmp);
                            }
                        }
                        catch (Exception exIcon)
                        {
                            Debug.WriteLine($"[Tray] Failed to set WPF tray icon: {exIcon.Message}");
                        }
                    }
                }

                // Attach double-click / click handler if available (try several event names)
                foreach (var evName in new[] { "DoubleClick", "Click", "MouseClick", "TrayMouseDoubleClick", "TrayClick" })
                {
                    var ev = wpfTray.GetType().GetEvent(evName);
                    if (ev != null)
                    {
                        try
                        {
                            EventHandler handler = (s, a) => Dispatcher.Invoke(ShowMainWindow);
                            ev.AddEventHandler(wpfTray, handler);
                            break;
                        }
                        catch { }
                    }
                }

                // Attach right-click/context handler if available (don't break; try to add alongside double-click)
                foreach (var evName in new[] { "RightClick", "MouseClick", "TrayMouseClick", "ContextMenuOpening", "TrayRightClick" })
                {
                    var ev = wpfTray.GetType().GetEvent(evName);
                    if (ev != null)
                    {
                        try
                        {
                            EventHandler handler = (s, a) => Dispatcher.Invoke(ShowTrayContextMenu);
                            ev.AddEventHandler(wpfTray, handler);
                        }
                        catch { }
                    }
                }

                // Keep instance alive in resources and store to field
                try { this.Resources["WpfTrayInstance"] = wpfTray; } catch { }
                _wpfTray = wpfTray;
                Console.WriteLine("[Tray] Initialized via WPF-UI.Tray (direct)");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Tray] WPF-UI.Tray init failed: {ex.Message}");
                Console.WriteLine("[Tray] WPF-UI.Tray init failed; skipping WinForms fallback to avoid runtime TypeLoadException.");
                return false;
            }
        }

        private void InitializeTrayIcon()
        {
            try
            {
                // Prefer WPF-UI.Tray NotifyIcon to avoid System.Windows.Forms TypeLoadException on some runtimes
                try
                {
                    var trayType = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a =>
                        {
                            try { return a.GetTypes(); } catch { return Array.Empty<Type>(); }
                        })
                        .FirstOrDefault(t => t.FullName == "Wpf.Ui.Tray.Controls.NotifyIcon");

                    if (trayType != null)
                    {
                        var trayObj = Activator.CreateInstance(trayType);


                        // Set tooltip if available
                        foreach (var pn in new[] { "ToolTipText", "ToolTip", "Tooltip", "Text" })
                        {
                            var p = trayType.GetProperty(pn);
                            if (p != null && p.CanWrite)
                            {
                                p.SetValue(trayObj, "ZeroMix");
                                break;
                            }
                        }

                        // Try set icon if property exists
                        string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Icons", "zeromix.ico");
                        if (File.Exists(iconPath))
                        {
                            var pIcon = trayType.GetProperty("Icon") ?? trayType.GetProperty("IconSource") ?? trayType.GetProperty("Image");
                            if (pIcon != null && pIcon.CanWrite)
                            {
                                var propType = pIcon.PropertyType;
                                try
                                {
                                    if (propType == typeof(System.Drawing.Icon))
                                    {
                                        pIcon.SetValue(trayObj, new System.Drawing.Icon(iconPath));
                                    }
                                    else if (typeof(System.Windows.Media.ImageSource).IsAssignableFrom(propType))
                                    {
                                        var bmp = new BitmapImage();
                                        bmp.BeginInit();
                                        bmp.UriSource = new Uri(iconPath, UriKind.Absolute);
                                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                                        bmp.EndInit();
                                        pIcon.SetValue(trayObj, bmp);
                                    }
                                }
                                catch (Exception exIcon)
                                {
                                    Debug.WriteLine($"[Tray] Failed to set WPF tray icon: {exIcon.Message}");
                                }
                            }
                        }

                        // Attach double-click / click handler if available
                        foreach (var evName in new[] { "DoubleClick", "Click", "MouseClick", "TrayMouseDoubleClick", "TrayClick" })
                        {
                            var ev = trayType.GetEvent(evName);
                            if (ev != null)
                            {
                                try
                                {
                                    // Try common EventHandler signature first

                                    EventHandler handler = (s, a) => Dispatcher.Invoke(ShowMainWindow);
                                    ev.AddEventHandler(trayObj, handler);
                                    break;
                                }
                                catch { /* ignore and try next event name */ }
                            }
                        }

                        // Store instance in resources to keep it alive / part of logical tree
                        try { this.Resources["WpfTrayInstance"] = trayObj; } catch { /* ignore if resource key exists */ }
                        Console.WriteLine("[Tray] Initialized via WPF-UI.Tray");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Tray] WPF-UI.Tray init failed: {ex.Message}");
                }

                // Fallback to WinForms NotifyIcon if WPF-UI.Tray unavailable or fail
                _notifyIcon = new System.Windows.Forms.NotifyIcon();
                // Default to system app icon first to avoid runtime dependency issues
                _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
                try
                {
                    string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Icons", "zeromix.ico");
                    if (File.Exists(iconPath))
                    {
                        try
                        {
                            var ico = new System.Drawing.Icon(iconPath);
                            _notifyIcon.Icon = ico;
                        }
                        catch (Exception icoEx)
                        {
                            Debug.WriteLine($"Tray icon load failed, using fallback: {icoEx.Message}");
                        }
                    }
                }
                catch { }

                _notifyIcon.Text = "ZeroMix";
                _notifyIcon.Visible = true;

                _notifyIcon.MouseClick += (s, e) =>
                {
                    if (e.Button == System.Windows.Forms.MouseButtons.Left)
                        Dispatcher.Invoke(ShowMainWindow);
                };
                _notifyIcon.DoubleClick += (s, e) => Dispatcher.Invoke(ShowMainWindow);

                var cms = new System.Windows.Forms.ContextMenuStrip();
                cms.Items.Add("Show Dashboard", null, (s, e) => Dispatcher.Invoke(ShowMainWindow));
                cms.Items.Add("ZeroMix Studio", null, (s, e) => Dispatcher.Invoke(OpenVideoEditor));

                cms.Items.Add(new System.Windows.Forms.ToolStripSeparator());
                cms.Items.Add("ZeroShell", null, (s, e) => Dispatcher.Invoke(ToggleZeroShell));
                cms.Items.Add(new System.Windows.Forms.ToolStripSeparator());
                cms.Items.Add("Exit", null, (s, e) => Dispatcher.Invoke(ExitApplication));
                _notifyIcon.ContextMenuStrip = cms;
                Console.WriteLine("[Tray] Initialized (WinForms fallback)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Tray init failed: {ex.Message}");
                Console.WriteLine($"[Tray] Init failed: {ex.Message}\n{ex}");
                _notifyIcon = null;
            }
        }

        private void GenericTrayEventHandler(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(ShowMainWindow);
        }

        private void ShowMainWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        // ── Tray Icon Event Handlers ────────────────────────────────────────
        // (Tidak digunakan karena tray dibuat programatik, bukan via XAML)
        // ────────────────────────────────────────────────────────────────────

        // If WPF/WinForms trays fail to appear for the user, a native Shell_NotifyIcon fallback is available below.
        // The methods use P/Invoke to avoid referencing System.Windows.Forms.NotifyIcon constructor which caused TypeLoadException on some runtimes.

        private void EnsureNativeTrayIfNeeded()
        {
            try
            {
                // If a WPF tray or WinForms tray is already active, still attempt native fallback only if user likely cannot see icon.
                // In practice we will add native tray when other methods didn't visibly register an icon for the session.
                if (_nativeTrayAdded) return;
                InitializeNativeTrayIcon();
            }
            catch { }
        }

        // (Native tray helpers appended below)





        private void ShowTrayContextMenu()
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    var menu = new System.Windows.Controls.ContextMenu();

                    var miShow = new System.Windows.Controls.MenuItem { Header = "Show Dashboard" };
                    miShow.Click += (_, __) => ShowMainWindow();
                    menu.Items.Add(miShow);

                    var miStudio = new System.Windows.Controls.MenuItem { Header = "ZeroMix Studio" };
                    miStudio.Click += (_, __) => OpenVideoEditor();
                    menu.Items.Add(miStudio);


                    menu.Items.Add(new System.Windows.Controls.Separator());

                    var miShell = new System.Windows.Controls.MenuItem { Header = "ZeroShell" };
                    miShell.Click += (_, __) => ToggleZeroShell();
                    menu.Items.Add(miShell);

                    menu.Items.Add(new System.Windows.Controls.Separator());

                    var miExit = new System.Windows.Controls.MenuItem { Header = "Exit" };
                    miExit.Click += (_, __) => ExitApplication();
                    menu.Items.Add(miExit);

                    menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
                    menu.IsOpen = true;
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Tray] ShowTrayContextMenu failed: {ex.Message}");
            }
        }

        private void ToggleZeroShell()
        {
            if (_zeroShellWindow == null)
            {
                _zeroShellWindow = new ZeroShellWindow();
                _zeroShellWindow.Closed += (s, ev) =>
                {
                    _zeroShellWindow = null;
                    UpdateTrayMenuState();
                };
                _zeroShellWindow.Show();
            }
            else
            {
                _zeroShellWindow.Close();
                _zeroShellWindow = null;
            }
            UpdateTrayMenuState();
        }

        private void UpdateTrayMenuState() { /* WinForms tray — no state update needed */ }

        private void ShowExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var studio = new ZeroMix.Studio.StudioWindow();
                studio.Show();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Open Studio failed: {ex.Message}");
            }
        }
        private void CreateTrayContextMenu() { /* replaced by WinForms ContextMenuStrip in InitializeTrayIcon */ }

        private void InitializePerformanceCounters()
        {
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
                _diskCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
                _systemDrive = new DriveInfo("C");

                _performanceTimer = new DispatcherTimer();
                _performanceTimer.Interval = TimeSpan.FromSeconds(1); // Set ke 1 detik agar lebih responsif
                _performanceTimer.Tick += PerformanceTimer_Tick;

                // Panggil sekali untuk pemanasan data
                _cpuCounter.NextValue();
            }
            catch
            {
                // Fallback jika PerformanceCounter tidak tersedia
                _performanceTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                _performanceTimer.Tick += PerformanceTimer_Tick;
            }
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
                    Task.Run(() =>
                    {
                        try
                        {
                            ManagementClass managementClass = new ManagementClass("Win32_ComputerSystem");
                            foreach (ManagementObject obj in managementClass.GetInstances())
                            {
                                _cachedTotalRAM = Convert.ToInt64(obj["TotalPhysicalMemory"]) / (1024 * 1024);
                                break;
                            }
                        }
                        catch { _cachedTotalRAM = 8192; } // Default 8GB jika gagal
                    });
                }

                if (_cachedTotalRAM <= 0)
                {
                    // Fallback jika task belum selesai
                    _cachedTotalRAM = 8192;
                }

                float usedRam = _cachedTotalRAM - (int)availableRam;
                float ramPercent = Math.Clamp((usedRam / _cachedTotalRAM) * 100, 0, 100);

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
                string trayTip = $"CPU: {cpuUsage:F1}% | RAM: {ramPercent:F1}% | Disk: {(float)(DiskProgressBar.Value):F1}%";
                if (_wpfTray != null)
                {
                    try
                    {
                        var prop = _wpfTray.GetType().GetProperty("ToolTipText") ?? _wpfTray.GetType().GetProperty("ToolTip") ?? _wpfTray.GetType().GetProperty("Tooltip") ?? _wpfTray.GetType().GetProperty("Text");
                        if (prop != null && prop.CanWrite)
                        {
                            var val = trayTip.Length > 255 ? trayTip.Substring(0, 255) : trayTip;
                            prop.SetValue(_wpfTray, val);
                        }
                    }
                    catch { }
                }
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
                    try
                    {
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
                    }
                    catch { }
                });

                // Update UI di main thread
                OsVersionText.Text = _cachedOsVersion ?? "Unknown OS";
                ProcessorText.Text = _cachedProcessor ?? "Unknown Processor";
                TotalRamText.Text = $"RAM: {_cachedTotalRAM / 1024} GB";

                // Network - ini lebih cepat, bisa langsung di UI thread
                int activeNetworks = 0;
                try
                {
                    await Task.Run(() =>
                    {
                        ManagementClass netClass = new ManagementClass("Win32_NetworkAdapterConfiguration");
                        foreach (ManagementObject net in netClass.GetInstances())
                        {
                            if ((bool?)net["IPEnabled"] == true)
                                activeNetworks++;
                        }
                    });
                }
                catch { }
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
            if (_wpfTray != null)
            {
                try
                {
                    var dispose = _wpfTray.GetType().GetMethod("Dispose");
                    if (dispose != null) dispose.Invoke(_wpfTray, null);
                    else
                    {
                        var isOpen = _wpfTray.GetType().GetProperty("IsOpen");
                        if (isOpen != null && isOpen.CanWrite) isOpen.SetValue(_wpfTray, false);
                    }
                }
                catch { }
                try { this.Resources.Remove("WpfTrayInstance"); } catch { }
                _wpfTray = null;
            }
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

        // --- Sleep Mode Settings --- //

        private void SleepSettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_sleepManager == null) return;
            // Navigasi ke panel SleepContent — isi dari settings saat ini
            SLP_LoadSettingsToUI(_sleepManager.Settings);
            DeactivateAllTabs();
            SleepContent.Visibility = Visibility.Visible;
        }

        private void SettingsSidebarButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var win = new ZeroMix.Features.ZeroConnect.UI.SettingsWindow { Owner = this };
                var res = win.ShowDialog();
                // SettingsService.Instance.Update will be called by the dialog when Save is pressed
            }
            catch { }
        }

        // ── Sleep Panel Handlers ─────────────────────────────────────────────
        private AodStyle _slpSelectedStyle = AodStyle.MinimalClock;
        private string? _slpCustomBgPath = null;
        private string? _slpMusicPath = null;

        private void SLP_LoadSettingsToUI(SleepSettingsModel s)
        {
            SLP_ManualModeChk.IsChecked = s.HasMode(TriggerMode.Manual);
            SLP_IdleModeChk.IsChecked = s.HasMode(TriggerMode.Idle);
            SLP_ShortcutModeChk.IsChecked = s.HasMode(TriggerMode.Shortcut);
            SLP_IdleSecondsTxt.Text = s.IdleThresholdSeconds.ToString();
            SLP_ShortcutTxt.Text = s.ShortcutKey;
            SLP_ExitMouseMoveChk.IsChecked = s.ExitOnMouseMove;
            SLP_ExitMouseDownChk.IsChecked = s.ExitOnMouseDown;
            SLP_ExitKeyDownChk.IsChecked = s.ExitOnKeyDown;
            SLP_BrightnessSld.Value = s.Brightness;
            _slpSelectedStyle = s.Style;
            SLP_SelectStyleCard(_slpSelectedStyle);
            _slpCustomBgPath = s.CustomBackgroundPath;
            SLP_CustomBgPathText.Text = string.IsNullOrEmpty(s.CustomBackgroundPath)
                ? "Tidak ada file dipilih" : System.IO.Path.GetFileName(s.CustomBackgroundPath);
            _slpMusicPath = s.MusicPath;
            SLP_MusicPathText.Text = string.IsNullOrEmpty(s.MusicPath)
                ? "Tidak ada musik dipilih" : System.IO.Path.GetFileName(s.MusicPath);
            SLP_MusicVolumeSld.Value = s.MusicVolume;
        }

        private SleepSettingsModel SLP_GetSettingsFromUI()
        {
            var s = new SleepSettingsModel();
            s.Mode = TriggerMode.None;
            if (SLP_ManualModeChk.IsChecked == true) s.Mode |= TriggerMode.Manual;
            if (SLP_IdleModeChk.IsChecked == true) s.Mode |= TriggerMode.Idle;
            if (SLP_ShortcutModeChk.IsChecked == true) s.Mode |= TriggerMode.Shortcut;
            if (int.TryParse(SLP_IdleSecondsTxt.Text, out int sec)) s.IdleThresholdSeconds = sec;
            s.ShortcutKey = SLP_ShortcutTxt.Text;
            s.ExitOnMouseMove = SLP_ExitMouseMoveChk.IsChecked ?? true;
            s.ExitOnMouseDown = SLP_ExitMouseDownChk.IsChecked ?? true;
            s.ExitOnKeyDown = SLP_ExitKeyDownChk.IsChecked ?? true;
            s.Style = _slpSelectedStyle;
            s.Brightness = SLP_BrightnessSld.Value;
            s.AutoDisableOnLowBattery = true;
            s.CustomBackgroundPath = _slpCustomBgPath;
            s.MusicPath = _slpMusicPath;
            s.MusicVolume = SLP_MusicVolumeSld.Value;
            return s;
        }

        private void SLP_SelectStyleCard(AodStyle style)
        {
            var neon = (System.Windows.Media.Brush)FindResource("NeonBlueBrush");
            var border = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 32, 48));
            var cards = new[] {
                (SLP_StyleCardMinimal, AodStyle.MinimalClock),
                (SLP_StyleCardGlow,    AodStyle.DigitalGlow),
                (SLP_StyleCardAnalog,  AodStyle.Analog),
                (SLP_StyleCardDate,    AodStyle.DateFocus),
                (SLP_StyleCardBlank,   AodStyle.Blank),
            };
            foreach (var (card, s) in cards)
            {
                if (card == null) continue;
                card.BorderBrush = s == style ? neon : border;
                card.BorderThickness = s == style ? new Thickness(2) : new Thickness(1);
            }
        }

        private void SLP_StyleCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border b && b.Tag is string tag &&
                Enum.TryParse<AodStyle>(tag, out var style))
            {
                _slpSelectedStyle = style;
                SLP_SelectStyleCard(style);
            }
        }

        private void SLP_BrightnessSld_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SLP_BrightnessLabel != null)
                SLP_BrightnessLabel.Text = $" — {(int)(e.NewValue * 100)}%";
        }

        private void SLP_MusicVolumeSld_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SLP_MusicVolumeLabel != null)
                SLP_MusicVolumeLabel.Text = $"{(int)(e.NewValue * 100)}%";
        }

        private void SLP_BrowseCustomBg_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Pilih Background AOD",
                Filter = "Media Files|*.jpg;*.jpeg;*.png;*.bmp;*.mp4;*.webm;*.mkv|All Files|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                _slpCustomBgPath = dlg.FileName;
                SLP_CustomBgPathText.Text = System.IO.Path.GetFileName(dlg.FileName);
            }
        }

        private void SLP_ClearCustomBg_Click(object sender, RoutedEventArgs e)
        {
            _slpCustomBgPath = null;
            SLP_CustomBgPathText.Text = "Tidak ada file dipilih";
        }

        private void SLP_BrowseMusic_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Pilih Musik untuk Sleep Mode",
                Filter = "Audio Files|*.mp3;*.wav;*.flac;*.ogg;*.m4a;*.aac|All Files|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                _slpMusicPath = dlg.FileName;
                SLP_MusicPathText.Text = System.IO.Path.GetFileName(dlg.FileName);
            }
        }

        private void SLP_ClearMusic_Click(object sender, RoutedEventArgs e)
        {
            _slpMusicPath = null;
            SLP_MusicPathText.Text = "Tidak ada musik dipilih";
        }

        private void SLP_StartBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_sleepManager == null) return;
            var settings = SLP_GetSettingsFromUI();
            _sleepManager.ApplySettings(settings);
            // Kembali ke Home lalu jalankan overlay
            HomeButton_Click(this, new RoutedEventArgs());
            if (settings.HasMode(TriggerMode.Manual))
                _sleepManager.ShowOverlay();
        }

        private void SleepBackBtn_Click(object sender, RoutedEventArgs e)
        {
            // Simpan settings lalu kembali ke Home
            if (_sleepManager != null)
                _sleepManager.ApplySettings(SLP_GetSettingsFromUI());
            HomeButton_Click(this, new RoutedEventArgs());
        }

        // --- Navigation --- //

        private void DeactivateAllTabs()
        {
            if (HomeContent != null) HomeContent.Visibility = Visibility.Collapsed;
            if (AboutContent != null) AboutContent.Visibility = Visibility.Collapsed;
            if (AssistantContent != null) AssistantContent.Visibility = Visibility.Collapsed;
            if (WallpapersContent != null) WallpapersContent.Visibility = Visibility.Collapsed;
            if (PluginsContent != null) PluginsContent.Visibility = Visibility.Collapsed;
            if (RecorderContent != null) RecorderContent.Visibility = Visibility.Collapsed;
            if (SleepContent != null) SleepContent.Visibility = Visibility.Collapsed;
            if (ZeroContent != null) ZeroContent.Visibility = Visibility.Collapsed; // Add ZeroConnect
            // Di dalam HamburgerBtn_Click, setelah baris NavTextAbout
            if (NavTextZeroConnect != null) NavTextZeroConnect.Visibility = Visibility;
            if (IconZeroConnect != null) IconZeroConnect.Margin = iconMargin;

            if (_performanceTimer != null) _performanceTimer.Stop();

            if (HomeButton != null) HomeButton.Background = System.Windows.Media.Brushes.Transparent;
            if (AboutButton != null) AboutButton.Background = System.Windows.Media.Brushes.Transparent;
            if (AssistantButton != null) AssistantButton.Background = System.Windows.Media.Brushes.Transparent;
            if (WallpaperButton != null) WallpaperButton.Background = System.Windows.Media.Brushes.Transparent;
            if (PluginsButton != null) PluginsButton.Background = System.Windows.Media.Brushes.Transparent;
            if (RecorderButton != null) RecorderButton.Background = System.Windows.Media.Brushes.Transparent;
            if (ZeroConnectButton != null) ZeroConnectButton.Background = System.Windows.Media.Brushes.Transparent;
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
            var recorderContent = FindName("RecorderContent") as UIElement;
            if (recorderContent != null) recorderContent.Visibility = Visibility.Visible;
            var __navBrush = TryFindResource("NavSelectedBrush") as System.Windows.Media.Brush;
            RecorderButton.Background = __navBrush ?? System.Windows.Media.Brushes.Transparent;

            // Lazy-init RecordingManager hanya saat halaman Capture dibuka
            if (_recordingManager == null)
            {
                Task.Run(() =>
                {
                    try
                    {
                        string ffmpegPath = ResolveFFmpegPath();
                        _recordingManager = new RecordingManager(ffmpegPath);
                    }
                    catch { }
                });
            }

            // Load audio devices & history
            LoadAudioDevices();
            LoadRecordingHistory();
        }

        private void RecordingMode_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border)
            {
                _selectedRecordingMode = border.Tag?.ToString() ?? "FullScreen";

                // Update UI Visuals
                var modes = new[] { ModeFullScreen, ModeApp, ModeArea, ModeWindow };
                var canvases = new[] { CanvasFullScreen, CanvasApp, CanvasArea, CanvasWindow };

                for (int i = 0; i < modes.Length; i++)
                {
                    var m = modes[i];
                    var c = canvases[i];

                    if (m == null) continue;
                    if (m == border)
                    {
                        m.BorderBrush = (System.Windows.Media.SolidColorBrush)FindResource("NeonBlueBrush");
                        m.BorderThickness = new Thickness(2);
                        m.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(20, 25, 34));
                        if (c != null) c.Opacity = 0.3;
                    }
                    else
                    {
                        m.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 32, 48));
                        m.BorderThickness = new Thickness(1);
                        m.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(17, 22, 32));
                        if (c != null) c.Opacity = 0.1;
                    }
                }

                // Handle specific mode setups
                if (_selectedRecordingMode == "Area")
                {
                    var selector = new AreaSelectorWindow();
                    if (selector.ShowDialog() == true && !selector.IsCancelled)
                    {
                        _selectedCaptureRect = selector.SelectedRect;
                        StatusLabel.Text = $"Area Selected: {(int)_selectedCaptureRect.Width}x{(int)_selectedCaptureRect.Height}";
                    }
                }
                else if (_selectedRecordingMode == "Window")
                {
                    var picker = new ZeroMix.Recorder.WindowPickerWindow();
                    picker.Owner = this;
                    picker.ShowDialog();

                    if (picker.IsConfirmed && picker.SelectedHandle != IntPtr.Zero)
                    {
                        _detectedGameHandle = picker.SelectedHandle;
                        StatusLabel.Text = $"Window dipilih untuk direkam";
                    }
                    else
                    {
                        // Reset ke FullScreen kalau batal
                        _selectedRecordingMode = "FullScreen";
                        StatusLabel.Text = "Mode direset ke FullScreen";
                    }
                }
            }
        }

        private void PlayRecording_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is RecordingHistoryItem item)
            {
                try
                {
                    Process.Start(new ProcessStartInfo(item.FilePath) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Gagal memutar video: " + ex.Message);
                }
            }
        }

        private void DeleteRecording_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is RecordingHistoryItem item)
            {
                var result = System.Windows.MessageBox.Show($"Hapus rekaman {item.FileName}?", "Konfirmasi", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        if (File.Exists(item.FilePath)) File.Delete(item.FilePath);
                        _recordingHistory.Remove(item);
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show("Gagal menghapus file: " + ex.Message);
                    }
                }
            }
        }

        private void LoadRecordingHistory()
        {
            try
            {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "ZeroRecord");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                var files = new DirectoryInfo(path).GetFiles("*.mp4")
                    .OrderByDescending(f => f.CreationTime)
                    .Take(10); // Show last 10

                _recordingHistory.Clear();
                foreach (var file in files)
                {
                    _recordingHistory.Add(new RecordingHistoryItem
                    {
                        FileName = file.Name,
                        FilePath = file.FullName,
                        CreationDate = file.CreationTime.ToString("dd MMM yyyy, HH:mm"),
                        FileSize = (file.Length / (1024.0 * 1024.0)).ToString("0.0") + " MB"
                    });
                }

                // Assuming RecordingHistoryList is a ListBox or similar control in your XAML
                // You might need to cast FindName result if it's not directly accessible
                var recordingHistoryList = FindName("RecordingHistoryList") as System.Windows.Controls.ItemsControl;
                if (recordingHistoryList != null)
                {
                    recordingHistoryList.ItemsSource = _recordingHistory;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ZeroRecord] Failed to load history: " + ex.Message);
            }
        }


        private void PluginsButton_Click(object sender, RoutedEventArgs e)
        {
            DeactivateAllTabs();
            PluginsContent.Visibility = Visibility.Visible;
            var __navBrush = TryFindResource("NavSelectedBrush") as System.Windows.Media.Brush;
            PluginsButton.Background = __navBrush ?? System.Windows.Media.Brushes.Transparent;
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            DeactivateAllTabs();
            HomeContent.Visibility = Visibility.Visible;
            var __navBrush = TryFindResource("NavSelectedBrush") as System.Windows.Media.Brush;
            HomeButton.Background = __navBrush ?? System.Windows.Media.Brushes.Transparent;
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            DeactivateAllTabs();
            AboutContent.Visibility = Visibility.Visible;
            var __navBrush = TryFindResource("NavSelectedBrush") as System.Windows.Media.Brush;
            AboutButton.Background = __navBrush ?? System.Windows.Media.Brushes.Transparent;
        }

        private void NavWallpapers_Click(object sender, RoutedEventArgs e)
        {
            DeactivateAllTabs();
            WallpapersContent.Visibility = Visibility.Visible;
            var __navBrush = TryFindResource("NavSelectedBrush") as System.Windows.Media.Brush;
            WallpaperButton.Background = __navBrush ?? System.Windows.Media.Brushes.Transparent;
        }

        private void AssistantButton_Click(object sender, RoutedEventArgs e)
        {
            DeactivateAllTabs();
            AssistantContent.Visibility = Visibility.Visible;
            var __navBrush = TryFindResource("NavSelectedBrush") as System.Windows.Media.Brush;
            AssistantButton.Background = __navBrush ?? System.Windows.Media.Brushes.Transparent;
        }


        
        private void ZeroConnectButton_Click(object sender, RoutedEventArgs e)
        {
            DeactivateAllTabs();
            ZeroContent.Visibility = Visibility.Visible;

            var navBrush = TryFindResource("NavSelectedBrush") as System.Windows.Media.Brush;
            ZeroConnectButton.Background = navBrush
                ?? new SolidColorBrush(System.Windows.Media.Color.FromRgb(20, 25, 34));
        }
        private void FiveSixEditorButton_Click(object sender, RoutedEventArgs e)
        {
            var editorWin = new ZeroMix.Studio.StudioWindow();
            editorWin.Owner = this;
            editorWin.ShowDialog();
        }

        private void DonationLink_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://trakteer.id/MyCici") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening donation link: {ex.Message}");
            }
        }

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
            try
            {
                string ffmpegPath = ResolveFFmpegPath();
                _recordingManager = new RecordingManager(ffmpegPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ZeroMix] Recorder init failed: {ex.Message}");
            }
        }

        private void StartGameDetection()
        {
            _gameDetectTimer = new DispatcherTimer();
            _gameDetectTimer.Interval = TimeSpan.FromSeconds(2);
            _gameDetectTimer.Tick += (s, e) =>
            {
                if (GameDetector.IsGameRunning(out string name, out IntPtr handle))
                {
                    _detectedGameHandle = handle;
                    GameDetectText.Text = $"GAME DETECTED: {name.ToUpper()}";
                    GameDetectBadge.Visibility = Visibility.Visible;
                }
                else
                {
                    _detectedGameHandle = IntPtr.Zero;
                    GameDetectBadge.Visibility = Visibility.Collapsed;
                }
            };
            _gameDetectTimer.Start();
        }

        private async void ZeroRecordBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_recordingManager == null) return;
            if (HomeRecordBtn != null) HomeRecordBtn.IsEnabled = false;

            if (!_isRecordingActive)
            {
                // ── Ambil settings dari UI ──────────────────────────────────
                int fps = 30;
                if (FpsComboBox != null)
                {
                    string content = (FpsComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "30 FPS";
                    int.TryParse(content.Split(' ')[0], out fps);
                }

                string mic = (MicComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "No Audio";
                string speaker = (SpeakerComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "No Audio";

                // Format output
                string format = "mp4";
                if (FindName("FormatComboBox") is System.Windows.Controls.ComboBox fmtBox)
                    format = (fmtBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "mp4";

                // Bitrate dari slider (default 8000 kbps)
                int bitrate = 8000;
                if (FindName("BitrateSlider") is Slider bSlider)
                    bitrate = (int)bSlider.Value;

                if (!File.Exists(_recordingManager.FFmpegPath))
                {
                    System.Windows.MessageBox.Show($"FFmpeg not found at:\n{_recordingManager.FFmpegPath}", "ZeroRecord Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    if (HomeRecordBtn != null) HomeRecordBtn.IsEnabled = true;
                    return;
                }

                IntPtr? hCapture = null;
                System.Windows.Rect? rCapture = null;
                if (_selectedRecordingMode == "Application") hCapture = _detectedGameHandle;
                else if (_selectedRecordingMode == "Area") rCapture = _selectedCaptureRect;
                else if (_selectedRecordingMode == "Window") hCapture = _detectedGameHandle != IntPtr.Zero ? _detectedGameHandle : IntPtr.Zero;

                // ── Countdown 3..2..1 ───────────────────────────────────────
                if (HomeRecordBtnText != null) HomeRecordBtnText.Text = "3...";
                StatusLabel.Text = "Bersiap...";
                await Task.Delay(1000);
                if (HomeRecordBtnText != null) HomeRecordBtnText.Text = "2...";
                await Task.Delay(1000);
                if (HomeRecordBtnText != null) HomeRecordBtnText.Text = "1...";
                await Task.Delay(1000);
                if (HomeRecordBtnText != null) HomeRecordBtnText.Text = "STARTING...";

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                bool started = await Task.Run(() =>
                {
                    try
                    {
                        _recordingManager.StartRecording($"ZeroRecord_{timestamp}.{format}", fps, mic, speaker, hCapture, rCapture, format, bitrate);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ZeroRecord] Start Error: {ex.Message}");
                        Dispatcher.Invoke(() =>
                            System.Windows.MessageBox.Show($"Gagal memulai perekaman:\n{ex.Message}", "Recording Error", MessageBoxButton.OK, MessageBoxImage.Error));
                        return false;
                    }
                });

                if (started)
                {
                    // Show Recording Border for marked area
                    if (_selectedRecordingMode == "Area" && rCapture != null)
                    {
                        _recordingBorder = new RecordingBorderWindow(rCapture.Value);
                        _recordingBorder.Show();
                    }
                    else if (_selectedRecordingMode == "Window" && hCapture.HasValue && hCapture.Value != IntPtr.Zero)
                    {
                        if (ScreenStudioRecorder.GetWindowRect(hCapture.Value, out var rect))
                        {
                            var r = new System.Windows.Rect(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
                            _recordingBorder = new RecordingBorderWindow(r);
                            _recordingBorder.Show();
                        }
                    }

                    // Apply Cinematic Zoom setting
                    if (_recordingManager.Recorder != null)
                    {
                        _recordingManager.Recorder.IsZoomEnabled = CinematicZoomToggle?.IsChecked ?? true;
                    }

                    _isRecordingActive = true;
                    UpdateRecordUI(true);

                    RecordDurationText.Visibility = Visibility.Visible;
                    if (_recordDurationTimer == null)
                    {
                        _recordDurationTimer = new DispatcherTimer();
                        _recordDurationTimer.Interval = TimeSpan.FromSeconds(1);
                        _recordDurationTimer.Tick += (s, args) =>
                        {
                            RecordDurationText.Text = _recordingManager.GetDuration();
                        };
                    }
                    _recordDurationTimer.Start();
                    StatusLabel.Text = "Recording Active";
                }
                else
                {
                    UpdateRecordUI(false);
                }

                if (HomeRecordBtn != null) HomeRecordBtn.IsEnabled = true;
            }
            else
            {
                StatusLabel.Text = "Finalizing Video...";
                if (HomeRecordBtnText != null) HomeRecordBtnText.Text = "SAVING...";

                await Task.Run(() =>
                {
                    try { _recordingManager.StopRecording(); } catch { }
                });

                _isRecordingActive = false;
                _recordDurationTimer?.Stop();

                _recordingBorder?.Close();
                _recordingBorder = null;

                UpdateRecordUI(false);
                if (RecordDurationText != null) RecordDurationText.Visibility = Visibility.Collapsed;

                // Refresh History
                Dispatcher.Invoke(LoadRecordingHistory);
                StatusLabel.Text = "Recording Saved!";
                if (HomeRecordBtn != null) HomeRecordBtn.IsEnabled = true;
            }
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

        private void BitrateSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (BitrateLabel == null) return;
            double mbps = e.NewValue / 1000.0;
            string label = mbps switch
            {
                <= 3 => $" — {mbps:F0} Mbps (Low)",
                <= 8 => $" — {mbps:F0} Mbps (Medium)",
                <= 20 => $" — {mbps:F0} Mbps (High)",
                _ => $" — {mbps:F0} Mbps (Ultra)"
            };
            BitrateLabel.Text = label;
        }

        private void PauseResumeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_recordingManager == null || !_isRecordingActive) return;

            if (_recordingManager.IsPaused)
            {
                _recordingManager.Resume();
                _recordDurationTimer?.Start();
                if (FindName("PauseResumeBtn") is Button btn) btn.Content = "⏸ Pause";
                StatusLabel.Text = "Recording Active";
            }
            else
            {
                _recordingManager.Pause();
                _recordDurationTimer?.Stop();
                if (FindName("PauseResumeBtn") is Button btn) btn.Content = "▶ Resume";
                StatusLabel.Text = "Recording Paused";
            }
        }

        private void UpdateRecordUI(bool isActive)
        {
            if (isActive)
            {
                if (HomeRecordBtnText != null) HomeRecordBtnText.Text = "STOP RECORD";
                if (HomeRecordBtn != null) HomeRecordBtn.Opacity = 1.0;
                if (FindName("PauseResumeBtn") is Button pb) { pb.Visibility = Visibility.Visible; pb.Content = "⏸ Pause"; }
            }
            else
            {
                if (HomeRecordBtnText != null) HomeRecordBtnText.Text = "ZeroRecord";
                if (HomeRecordBtn != null) HomeRecordBtn.Opacity = 0.7;
                if (FindName("PauseResumeBtn") is Button pb) pb.Visibility = Visibility.Collapsed;
            }
        }

        private void EnableTransparentTaskbar()
        {
            // Shell_TrayWnd is the main taskbar
            IntPtr taskbarHandle = FindWindow("Shell_TrayWnd", null);

            // Mode Clear: ACCENT_ENABLE_TRANSPARENTGRADIENT (2)
            // Color: 0x01140A0D (Almost transparent dark to hide 'square' glitches)
            ApplyTaskbarAccent(taskbarHandle, AccentState.ACCENT_ENABLE_TRANSPARENTGRADIENT, 2, 0x01140A0D);

            // Shell_SecondaryTrayWnd for extra monitors
            IntPtr secondaryTaskbarHandle = FindWindow("Shell_SecondaryTrayWnd", null);
            if (secondaryTaskbarHandle != IntPtr.Zero)
            {
                ApplyTaskbarAccent(secondaryTaskbarHandle, AccentState.ACCENT_ENABLE_TRANSPARENTGRADIENT, 2, 0x01140A0D);
            }
        }

        private void DisableTransparentTaskbar()
        {
            // Shell_TrayWnd is the main taskbar
            IntPtr taskbarHandle = FindWindow("Shell_TrayWnd", null);

            // Returning to ACCENT_DISABLED (0) lets Windows take back control of rendering
            // based on the user's system theme (Light/Dark/Blur).
            ApplyTaskbarAccent(taskbarHandle, AccentState.ACCENT_DISABLED, 0, 0x00000000);

            // Re-apply for secondary taskbar
            IntPtr secondaryTaskbarHandle = FindWindow("Shell_SecondaryTrayWnd", null);
            if (secondaryTaskbarHandle != IntPtr.Zero)
            {
                ApplyTaskbarAccent(secondaryTaskbarHandle, AccentState.ACCENT_DISABLED, 0, 0x00000000);
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
            CacheStatusText.Text = "🛡️ Menganalisis sistem & file sampah...";

            MonitoringPanel.Visibility = Visibility.Visible;

            await Task.Run(() =>
            {
                // Daftar folder sampah yang lebih lengkap
                string[] tempPaths = {
                    Path.GetTempPath(),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft\\Windows\\Explorer"), // Thumbnail cache
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Package Cache")
                };

                int deletedCount = 0;
                int skippedCount = 0;
                long totalSize = 0;

                foreach (var path in tempPaths)
                {
                    if (!Directory.Exists(path)) continue;
                    var directory = new DirectoryInfo(path);

                    this.Dispatcher.Invoke(() => StatusLabel.Text = $"⚡ Cleaning: {path}");

                    // Hapus File (Tanpa Delay buatan agar cepat)
                    try
                    {
                        foreach (var file in directory.GetFiles("*", SearchOption.TopDirectoryOnly))
                        {
                            try
                            {
                                totalSize += file.Length;
                                file.Delete();
                                deletedCount++;
                            }
                            catch { skippedCount++; }
                        }
                    }
                    catch { }

                    // Hapus Sub-folder
                    try
                    {
                        foreach (var dir in directory.GetDirectories())
                        {
                            try
                            {
                                dir.Delete(true);
                                deletedCount++;
                            }
                            catch { skippedCount++; }
                        }
                    }
                    catch { }
                }

                this.Dispatcher.Invoke(() =>
                {
                    double sizeInMb = totalSize / (1024.0 * 1024.0);
                    CacheStatusText.Text = $"✨ Selesai! {deletedCount} item dibuang ({sizeInMb:F2} MB). {skippedCount} file in-use.";
                    StatusLabel.Text = "Optimization Complete";
                    ClearCacheButton.IsEnabled = true;
                    // Refresh stats
                    UpdateDashboard();
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

        private void MaximizeRestore_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (MaximizeButton == null) return;

            if (this.WindowState == WindowState.Maximized)
            {
                MaximizeButton.Content = "\uE923"; // Restore icon
            }
            else
            {
                MaximizeButton.Content = "\uE922"; // Maximize icon
            }
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
                folderBtn.Click += (s, e) =>
                {
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
                shortcutBtn.Click += (s, e) =>
                {
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
                deleteBtn.Click += (s, e) =>
                {
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
                toggle.Click += (s, e) =>
                {
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

        private async Task CheckUpdateSilentAsync()
        {
            try
            {
                await Task.Delay(3000); // Tunggu app fully loaded
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "ZeroMix-Updater");
                client.Timeout = TimeSpan.FromSeconds(10);

                var response = await client.GetStringAsync("https://api.github.com/repos/faizinuha/ZeroMix/releases/latest");
                using var doc = JsonDocument.Parse(response);
                string latestVersion = doc.RootElement.GetProperty("tag_name").GetString()?.Replace("v", "") ?? "0.0.0";

                if (IsNewerVersion(latestVersion, CURRENT_VERSION))
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (UpdateBadge != null) UpdateBadge.Visibility = Visibility.Visible;
                        if (SidebarUpdateDot != null) SidebarUpdateDot.Visibility = Visibility.Visible;
                        if (SidebarUpdateBadge != null) SidebarUpdateBadge.Visibility = Visibility.Visible;
                        StatusLabel.Text = $"Update v{latestVersion} tersedia!";
                    });
                }
            }
            catch { /* Silent fail, no internet or API limit */ }
        }

        private async void CheckUpdateBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;

                // 1. Coba ZeroMix-Updater.exe dulu (GUI updater)
                string updaterExe = Path.Combine(baseDir, "Tools", "Updater", "ZeroMix-Updater.exe");
                if (File.Exists(updaterExe))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = updaterExe,
                        UseShellExecute = true,
                        WorkingDirectory = Path.GetDirectoryName(updaterExe)
                    });
                    StatusLabel.Text = "Updater launched...";
                    return;
                }

                // 2. Fallback: buka terminal dan jalankan zeromix-update.bat → ps1
                string batPath = Path.Combine(baseDir, "zeromix-update.bat");
                string ps1Path = Path.Combine(baseDir, "zeromix-update.ps1");

                if (File.Exists(batPath))
                {
                    // Buka CMD biasa (terminal yang umum dikenal user)
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/k \"{batPath}\"",
                        UseShellExecute = true,
                        WorkingDirectory = baseDir
                    });
                    StatusLabel.Text = "Update terminal opened...";
                    return;
                }

                if (File.Exists(ps1Path))
                {
                    // Buka PowerShell dengan ps1
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoExit -ExecutionPolicy Bypass -File \"{ps1Path}\"",
                        UseShellExecute = true,
                        WorkingDirectory = baseDir
                    });
                    StatusLabel.Text = "Update terminal opened...";
                    return;
                }

                // 3. Fallback terakhir: cek GitHub API dan buka browser
                StatusLabel.Text = "Checking for updates...";
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "ZeroMix-App");
                client.Timeout = TimeSpan.FromSeconds(8);
                var response = await client.GetStringAsync("https://api.github.com/repos/faizinuha/ZeroMix/releases/latest");
                var json = JsonDocument.Parse(response);

                string latestVersion = json.RootElement.GetProperty("tag_name").GetString()?.TrimStart('v') ?? "";
                string downloadUrl = json.RootElement.GetProperty("html_url").GetString() ?? "";

                if (IsNewerVersion(latestVersion, CURRENT_VERSION))
                {
                    var result = System.Windows.MessageBox.Show(
                        $"Update tersedia: v{latestVersion}\n\nVersi saat ini: v{CURRENT_VERSION}\n\nBuka halaman download?",
                        "Update Available", MessageBoxButton.YesNo, MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                        Process.Start(new ProcessStartInfo(downloadUrl) { UseShellExecute = true });

                    StatusLabel.Text = $"Update available: v{latestVersion}";
                }
                else
                {
                    System.Windows.MessageBox.Show(
                        $"Sudah versi terbaru (v{CURRENT_VERSION})",
                        "No Updates", MessageBoxButton.OK, MessageBoxImage.Information);
                    StatusLabel.Text = "You're up to date!";
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Gagal cek update:\n{ex.Message}\n\nCek manual di:\nhttps://github.com/faizinuha/ZeroMix/releases",
                    "Update Check Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                StatusLabel.Text = "Update check failed";
            }
        }

        private void OpenUpdaterBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string updaterPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", "Updater", "ZeroMix-Updater.exe");
                if (File.Exists(updaterPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = updaterPath,
                        UseShellExecute = true,
                        WorkingDirectory = Path.GetDirectoryName(updaterPath)
                    });
                }
                else
                {
                    // Fallback: buka halaman releases di browser
                    Process.Start(new ProcessStartInfo("https://github.com/faizinuha/ZeroMix/releases/latest") { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Updater] Failed to open: {ex.Message}");
                Process.Start(new ProcessStartInfo("https://github.com/faizinuha/ZeroMix/releases/latest") { UseShellExecute = true });
            }
        }

        private bool IsNewerVersion(string latest, string current)
        {
            try
            {
                Version vLatest = new Version(latest);
                Version vCurrent = new Version(current);
                return vLatest > vCurrent;
            }
            catch { return latest != current; }
        }

        private string _lastCharacter = "Frieren";

        private void AssistantMasterToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (_assistantWindow == null || !IsWindowOpen<Virtual_Assisten.VirtualAssistantWindow>())
            {
                _assistantWindow = new Virtual_Assisten.VirtualAssistantWindow();
            }

            // Get Pre-Launch Settings from Dashboard
            // PreConfigure removed - Virtual Assistant now uses auto-configured defaults
            // No manual configuration needed

            if (!_assistantWindow.IsVisible)
            {
                _assistantWindow.Show();
                AssistantStatusText.Text = "ONLINE";
                AssistantStatusText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 255, 136));
                StatusLabel.Text = $"{_lastCharacter} is here to help!";
            }

            _assistantWindow.SetCharacter(_lastCharacter);
        }

        private void AssistantMasterToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_assistantWindow != null)
            {
                _assistantWindow.Close();
                _assistantWindow = null;
                AssistantStatusText.Text = "OFFLINE";
                AssistantStatusText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(113, 128, 150));
                StatusLabel.Text = "Assistant is resting...";
            }
        }

        private void OpenFrieren_Click(object sender, RoutedEventArgs e)
        {
            _lastCharacter = "Frieren";
            AssistantMasterToggle.IsChecked = true;
            AssistantMasterToggle_Checked(this, new RoutedEventArgs());
        }

        private void OpenFern_Click(object sender, RoutedEventArgs e)
        {
            _lastCharacter = "Fern";
            AssistantMasterToggle.IsChecked = true;
            AssistantMasterToggle_Checked(this, new RoutedEventArgs());
        }

        private void OpenHuohuo_Click(object sender, RoutedEventArgs e)
        {
            _lastCharacter = "Huohuo";
            AssistantMasterToggle.IsChecked = true;
            AssistantMasterToggle_Checked(this, new RoutedEventArgs());
        }

        private void OpenJian_Click(object sender, RoutedEventArgs e)
        {
            _lastCharacter = "Jian";
            AssistantMasterToggle.IsChecked = true;
            AssistantMasterToggle_Checked(this, new RoutedEventArgs());
        }

        private void OpenVideoEditor()
        {
            try
            {
                Studio.StudioWindow studio = new Studio.StudioWindow();
                studio.Show();
                studio.Activate();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Gagal membuka ZeroMix Studio: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Open56Editor()
        {
            try
            {
                var editor = new Studio.EditorWebWindow();
                editor.Show();
                editor.Activate();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Gagal membuka 56Editor: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Native Shell_NotifyIcon implementation (placed near end of MainWindow)
        private IntPtr _nativeTrayIconHandle = IntPtr.Zero;
        private uint _nativeTrayId = 0xBEEF;
        private const int WM_APP = 0x8000;
        private const int WM_TRAY_CALLBACK = WM_APP + 1;
        private System.Windows.Interop.HwndSource? _hwndSource;
        private bool _nativeTrayAdded = false;
        private Thickness iconMargin;
        private const uint NIM_ADD = 0x00000000;
        private const uint NIM_MODIFY = 0x00000001;
        private const uint NIM_DELETE = 0x00000002;
        private const uint NIF_MESSAGE = 0x00000001;
        private const uint NIF_ICON = 0x00000002;
        private const uint NIF_TIP = 0x00000004;
        private const uint IMAGE_ICON = 1;
        private const uint LR_LOADFROMFILE = 0x00000010;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NOTIFYICONDATA
        {
            public uint cbSize;
            public IntPtr hWnd;
            public uint uID;
            public uint uFlags;
            public uint uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType, int cx, int cy, uint fuLoad);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA pnid);

        private void InitializeNativeTrayIcon()
        {
            try
            {
                if (_nativeTrayAdded) return;
                var helper = new System.Windows.Interop.WindowInteropHelper(this);
                var hWnd = helper.Handle;
                if (hWnd == IntPtr.Zero) return; // Need HWND to receive callbacks

                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Icons", "zeromix.ico");
                IntPtr hIcon = IntPtr.Zero;
                if (File.Exists(iconPath))
                {
                    hIcon = LoadImage(IntPtr.Zero, iconPath, IMAGE_ICON, 0, 0, LR_LOADFROMFILE);
                }

                if (hIcon == IntPtr.Zero)
                {
                    var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                    if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                        hIcon = LoadImage(IntPtr.Zero, exePath, IMAGE_ICON, 0, 0, LR_LOADFROMFILE);
                }

                if (hIcon == IntPtr.Zero)
                {
                    Debug.WriteLine("[NativeTray] Failed to load icon handle");
                    return;
                }

                var nid = new NOTIFYICONDATA();
                nid.cbSize = (uint)Marshal.SizeOf(typeof(NOTIFYICONDATA));
                nid.hWnd = hWnd;
                nid.uID = _nativeTrayId;
                nid.uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP;
                nid.uCallbackMessage = (uint)WM_TRAY_CALLBACK;
                nid.hIcon = hIcon;
                nid.szTip = "ZeroMix";

                bool ok = Shell_NotifyIcon(NIM_ADD, ref nid);
                if (!ok)
                {
                    Debug.WriteLine($"[NativeTray] Shell_NotifyIcon NIM_ADD failed: {Marshal.GetLastWin32Error()}");
                    DestroyIcon(hIcon);
                    return;
                }

                _nativeTrayIconHandle = hIcon;
                _nativeTrayAdded = true;

                _hwndSource = System.Windows.Interop.HwndSource.FromHwnd(hWnd);
                if (_hwndSource != null) _hwndSource.AddHook(NativeWndProc);

                this.Closed += (_, __) => CleanupNativeTrayIcon();

                Console.WriteLine("[NativeTray] Added native tray icon via Shell_NotifyIcon");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NativeTray] Init failed: {ex.Message}");
            }
        }

        private IntPtr NativeWndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            try
            {
                if (msg == WM_TRAY_CALLBACK)
                {
                    int ev = lParam.ToInt32();
                    if (ev == 0x0203 || ev == 0x0202)
                    {
                        Dispatcher.Invoke(ShowMainWindow);
                        handled = true;
                    }
                    else if (ev == 0x0205)
                    {
                        Dispatcher.Invoke(ShowMainWindow);
                        handled = true;
                    }
                }
            }
            catch { }
            return IntPtr.Zero;
        }

        private void CleanupNativeTrayIcon()
        {
            try
            {
                if (!_nativeTrayAdded) return;
                var helper = new System.Windows.Interop.WindowInteropHelper(this);
                var hWnd = helper.Handle;
                var nid = new NOTIFYICONDATA();
                nid.cbSize = (uint)Marshal.SizeOf(typeof(NOTIFYICONDATA));
                nid.hWnd = hWnd;
                nid.uID = _nativeTrayId;
                Shell_NotifyIcon(NIM_DELETE, ref nid);
                if (_nativeTrayIconHandle != IntPtr.Zero)
                {
                    DestroyIcon(_nativeTrayIconHandle);
                    _nativeTrayIconHandle = IntPtr.Zero;
                }
                if (_hwndSource != null)
                {
                    _hwndSource.RemoveHook(NativeWndProc);
                    _hwndSource = null;
                }
                _nativeTrayAdded = false;
                Console.WriteLine("[NativeTray] Removed native tray icon");
            }
            catch { }
        }
    }
    public class RecordingHistoryItem
    {
        public string FileName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string CreationDate { get; set; } = "";
        public string FileSize { get; set; } = "";
    }
}
