using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using zeromix.SnapLayout.Models;

namespace zeromix.SnapLayout
{
    /// <summary>
    /// Settings panel UI untuk Snap Layout plugin.
    /// Mengelola toggles, preset selector, hotkey display, snap history.
    /// </summary>
    public partial class SnapLayoutUI : System.Windows.Controls.UserControl
    {
        private readonly SnapLayoutService _service;
        private bool _isExpanded;
        private DispatcherTimer? _clockTimer;
        private int _todaySnapCount;
        private DateOnly _currentDate = DateOnly.FromDateTime(DateTime.Now);

        // Hotkey edit state
        private bool _awaitingHotkey;

        public SnapLayoutUI() : this(new SnapLayoutService()) { }

        public SnapLayoutUI(SnapLayoutService service)
        {
            InitializeComponent();
            _service = service;

            _service.OnSnapped += OnSnapped;
            _service.OnSnapFailed += OnSnapFailed;
            _service.OnHotkeyConflict += OnHotkeyConflict;

            this.Loaded += OnLoaded;
            this.Unloaded += OnUnloaded;
        }

        public SnapLayoutService Service => _service;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            LoadSettings();

            // Clock timer untuk reset harian
            _clockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _clockTimer.Tick += (s, ev) =>
            {
                var today = DateOnly.FromDateTime(DateTime.Now);
                if (today != _currentDate)
                {
                    _currentDate = today;
                    _todaySnapCount = 0;
                    UpdateSnapCount();
                }
            };
            _clockTimer.Start();

            // Restore state
            ApplySettingsToUI();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _clockTimer?.Stop();
            _service.Stop();
        }

        // ── Card click (expand/collapse) ──────────────────────────────────

        private void SnapCard_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isExpanded = !_isExpanded;

            if (_isExpanded)
            {
                SettingsPanel.Visibility = Visibility.Visible;
                var expand = Resources["ExpandAnim"] as Storyboard;
                expand?.Begin();
                StatusText.Visibility = Visibility.Collapsed;
            }
            else
            {
                var collapse = Resources["CollapseAnim"] as Storyboard;
                if (collapse != null)
                {
                    EventHandler? onCompleted = null;
                    onCompleted = (s, ev) =>
                    {
                        collapse.Completed -= onCompleted;
                        SettingsPanel.Visibility = Visibility.Collapsed;
                        StatusText.Visibility = Visibility.Visible;
                    };
                    collapse.Completed += onCompleted;
                    collapse.Begin();
                }
                else
                {
                    SettingsPanel.Visibility = Visibility.Collapsed;
                    StatusText.Visibility = Visibility.Visible;
                }
            }
        }

        // ── Toggle ────────────────────────────────────────────────────────

        private void SnapPluginToggle_Click(object sender, RoutedEventArgs e)
        {
            if (_service == null) return;
            bool enabled = SnapPluginToggle.IsChecked == true;

            if (enabled)
            {
                var win = Window.GetWindow(this);
                if (win != null)
                    _service.Start(win);

                StatusText.Text = "Active";
                StatusText.Foreground = System.Windows.Media.Brushes.LimeGreen;
                LogMsg("[OK] Snap Layout AKTIF.");
            }
            else
            {
                _service.Stop();
                StatusText.Text = "Inactive";
                StatusText.Foreground = System.Windows.Media.Brushes.Gray;
                LogMsg("[STOP] Snap Layout DIMATIKAN.");
            }

            SaveSettings();
        }

        // ── Layout Preset ─────────────────────────────────────────────────

        private void LayoutCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Guard: event bisa di-fire oleh XAML parser sebelum _service diinisialisasi
            if (_service == null) return;

            if (LayoutCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                var preset = SnapLayoutHelper.ParsePreset(tag);
                _service.ActivePreset = preset;
                _service.DragPreset = preset;
                SaveSettings();
                LogMsg($"[LAYOUT] Preset: {tag}");
            }
        }

        // ── Trigger Toggles ───────────────────────────────────────────────

        private void DragModeToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (_service == null) return;
            _service.DragModeEnabled = DragModeToggle.IsChecked == true;
            SaveSettings();
            LogMsg(_service.DragModeEnabled ? "[DRAG] Drag-to-zone AKTIF." : "[DRAG] Drag-to-zone NONAKTIF.");
        }

        private void KeybindToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (_service == null) return;
            _service.KeybindMode = KeybindToggle.IsChecked == true;
            SaveSettings();
            LogMsg(_service.KeybindMode ? "[KEYBIND] Keybind overlay AKTIF." : "[KEYBIND] Keybind overlay NONAKTIF.");
        }

        // ── Hotkey ────────────────────────────────────────────────────────

        private void ChangeHotkey_Click(object sender, RoutedEventArgs e)
        {
            _awaitingHotkey = true;
            LogMsg("[HOTKEY] Tekan kombinasi hotkey baru... (ESC untuk batal)");

            var win = Window.GetWindow(this);
            if (win != null)
            {
                System.Windows.Input.KeyEventHandler handler = null!;
                handler = (s, ev) =>
                {
                    if (ev.Key == System.Windows.Input.Key.Escape)
                    {
                        _awaitingHotkey = false;
                        win.PreviewKeyDown -= handler;
                        LogMsg("[HOTKEY] Dibatalkan.");
                        ev.Handled = true;
                        return;
                    }

                    if (_awaitingHotkey)
                    {
                        uint mod = 0;
                        if ((System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) != 0)
                            mod |= SnapLayoutService.MOD_CONTROL;
                        if ((System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Windows) != 0)
                            mod |= SnapLayoutService.MOD_WIN;
                        if ((System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Alt) != 0)
                            mod |= SnapLayoutService.MOD_ALT;

                        uint vk = (uint)System.Windows.Input.KeyInterop.VirtualKeyFromKey(ev.Key);

                        if (mod == 0 || vk == 0)
                        {
                            LogMsg("[HOTKEY] Kombinasi tidak valid. Gunakan kombinasi dengan modifier (Ctrl/Win/Alt).");
                            ev.Handled = true;
                            return;
                        }

                        // Apply
                        _service.ChangeHotkey(mod, vk);
                        UpdateHotkeyDisplay(mod, vk);
                        SaveSettings();
                        LogMsg($"[HOTKEY] Hotkey diubah.");
                        _awaitingHotkey = false;
                        win.PreviewKeyDown -= handler;
                        ev.Handled = true;
                    }
                };
                win.PreviewKeyDown += handler;
            }
        }

        private void UpdateHotkeyDisplay(uint mod, uint vk)
        {
            string modStr = "";
            if ((mod & SnapLayoutService.MOD_CONTROL) != 0) modStr += "Ctrl+";
            if ((mod & SnapLayoutService.MOD_WIN) != 0) modStr += "Win+";
            if ((mod & SnapLayoutService.MOD_ALT) != 0) modStr += "Alt+";

            string keyStr = System.Windows.Input.KeyInterop.KeyFromVirtualKey((int)vk).ToString();
            HotkeyDisplay.Text = modStr + keyStr;
        }

        private void UpdateHotkeyDisplayFromService()
        {
            UpdateHotkeyDisplay(_service.HotkeyModifiers, _service.HotkeyVK);
        }

        // ── Snap Events ───────────────────────────────────────────────────

        private void OnSnapped(IntPtr hwnd, SnapZone zone)
        {
            Dispatcher.Invoke(() =>
            {
                _todaySnapCount++;
                UpdateSnapCount();

                string windowName = GetWindowTitle(hwnd);
                LogMsg($"{windowName} → {zone.Label} ({zone.ScreenRect.Width:F0}x{zone.ScreenRect.Height:F0})");
            });
        }

        private void OnSnapFailed(string reason)
        {
            Dispatcher.Invoke(() =>
            {
                LogMsg($"[FAIL] {reason}");
            });
        }

        private void OnHotkeyConflict(string message)
        {
            Dispatcher.Invoke(() =>
            {
                LogMsg($"[WARN] {message}");
                UpdateHotkeyDisplayFromService();
            });
        }

        private string GetWindowTitle(IntPtr hwnd)
        {
            try
            {
                var sb = new System.Text.StringBuilder(256);
                ZeroMix.Native.Win32.Window.GetWindowText(hwnd, sb, sb.Capacity);
                string title = sb.ToString();
                if (!string.IsNullOrEmpty(title))
                {
                    ZeroMix.Native.Win32.Window.GetWindowThreadProcessId(hwnd, out uint pid);
                    var proc = System.Diagnostics.Process.GetProcessById((int)pid);
                    return $"{proc.ProcessName}.exe";
                }
            }
            catch { }
            return "Unknown";
        }

        // ── Snap Count ────────────────────────────────────────────────────

        private void UpdateSnapCount()
        {
            SnapCountDisplay.Text = $"Today: {_todaySnapCount} snap{( _todaySnapCount != 1 ? "s" : "")}";

            if (_todaySnapCount > 0)
                StatusText.Text = $"{_todaySnapCount} window{( _todaySnapCount != 1 ? "s" : "")} snapped today";
            else
                StatusText.Text = "Inactive";
        }

        // ── Log ───────────────────────────────────────────────────────────

        private void LogMsg(string msg)
        {
            string time = DateTime.Now.ToString("HH:mm:ss");
            LiveLog.Text = $"[{time}] {msg}\n" + LiveLog.Text;

            if (LiveLog.Text.Length > 2000)
                LiveLog.Text = LiveLog.Text.Substring(0, 2000);
        }

        // ── Settings Persistence ──────────────────────────────────────────

        private SnapLayoutConfig LoadConfig()
        {
            try
            {
                string path = GetConfigPath();
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<SnapLayoutConfig>(json) ?? new SnapLayoutConfig();
                }
            }
            catch { }
            return new SnapLayoutConfig();
        }

        private void SaveConfig(SnapLayoutConfig cfg)
        {
            try
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ZeroMix");
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, "snap_layout_config.json");
                var json = JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch { }
        }

        private string GetConfigPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ZeroMix", "snap_layout_config.json");
        }

        private void LoadSettings()
        {
            var cfg = LoadConfig();
            if (cfg != null)
            {
                _todaySnapCount = cfg.SnapCountToday;
                _currentDate = DateOnly.FromDateTime(DateTime.Now);

                // Set preset combo
                foreach (ComboBoxItem item in LayoutCombo.Items)
                {
                    if (item.Tag?.ToString() == cfg.ActivePreset)
                    {
                        LayoutCombo.SelectedItem = item;
                        break;
                    }
                }

                DragModeToggle.IsChecked = cfg.DragModeEnabled;
                KeybindToggle.IsChecked = cfg.KeybindMode;

                if (cfg.HotkeyModifiers != 0 || cfg.HotkeyVK != 0)
                {
                    _service.ChangeHotkey(cfg.HotkeyModifiers, cfg.HotkeyVK);
                }
            }
        }

        private void SaveSettings()
        {
            var cfg = new SnapLayoutConfig
            {
                IsEnabled = SnapPluginToggle.IsChecked == true,
                ActivePreset = (LayoutCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "TwoColumns",
                DragModeEnabled = DragModeToggle.IsChecked == true,
                KeybindMode = KeybindToggle.IsChecked == true,
                HotkeyModifiers = _service.HotkeyModifiers,
                HotkeyVK = _service.HotkeyVK,
                SnapCountToday = _todaySnapCount
            };
            SaveConfig(cfg);
        }

        private void ApplySettingsToUI()
        {
            var cfg = LoadConfig();
            SnapPluginToggle.IsChecked = cfg.IsEnabled;
            UpdateSnapCount();
            UpdateHotkeyDisplayFromService();

            if (cfg.IsEnabled)
            {
                var win = Window.GetWindow(this);
                if (win != null)
                    _service.Start(win);

                StatusText.Text = $"{_todaySnapCount} window{( _todaySnapCount != 1 ? "s" : "")} snapped today";
                StatusText.Foreground = System.Windows.Media.Brushes.LimeGreen;
            }
        }

        // ── Config Model ──────────────────────────────────────────────────

        private class SnapLayoutConfig
        {
            public bool IsEnabled { get; set; } = false;
            public string ActivePreset { get; set; } = "TwoColumns";
            public bool DragModeEnabled { get; set; } = true;
            public bool KeybindMode { get; set; } = true;
            public uint HotkeyModifiers { get; set; } = 10; // Ctrl+Win
            public uint HotkeyVK { get; set; } = 0x5A; // Z
            public int SnapCountToday { get; set; } = 0;
        }
    }
}
