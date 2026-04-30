using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace zeromix.CatGatekeeper
{
    public partial class CatGatekeeperUI : System.Windows.Controls.UserControl
    {
        private CatGatekeeperService _service;
        private DispatcherTimer? _updateTimer;
        private CatOverlayWindow? _overlayWindow;
        private bool _isExpanded = false;

        // Parameterless constructor untuk XAML / lazy load dari code-behind
        public CatGatekeeperUI() : this(new CatGatekeeperService()) { }

        public CatGatekeeperUI(CatGatekeeperService service)
        {
            InitializeComponent();
            _service = service;
            _service.OnBreakTimeReached += OnBreakTimeReached;
            _service.OnBreakEnded += OnBreakEnded;

            // Semua init setelah UI loaded — cegah crash saat XAML parse
            this.Loaded += OnLoaded;
            this.Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Load saved settings dulu sebelum start
            LoadSettings();

            // Start service
            _service.Start();

            // Update status setiap detik
            _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _updateTimer.Tick += UpdateStatus;
            _updateTimer.Start();

            // Restore toggle state
            CatPluginToggle.IsChecked = true;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _updateTimer?.Stop();
            _service.Stop();
        }

        // ── Card click: toggle expand ──────────────────────────────────────
        private void CatCard_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isExpanded = !_isExpanded;
            CatSettingsBorder.Visibility = _isExpanded ? Visibility.Visible : Visibility.Collapsed;
            StatusText.Visibility = _isExpanded ? Visibility.Collapsed : Visibility.Visible;
        }

        // ── Checkbox toggle ────────────────────────────────────────────────
        private void CatPluginToggle_Click(object sender, RoutedEventArgs e)
        {
            bool isEnabled = CatPluginToggle.IsChecked == true;
            if (isEnabled)
            {
                _service.Start();
                StatusDetailText.Text = "Tracking...";
                StatusDetailText.Foreground = System.Windows.Media.Brushes.LimeGreen;
            }
            else
            {
                _service.Stop();
                StatusDetailText.Text = "Disabled";
                StatusDetailText.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        // ── Sliders ────────────────────────────────────────────────────────
        private void UsageLimitSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (UsageLimitText == null) return;
            int minutes = (int)e.NewValue;
            UsageLimitText.Text = $"{minutes} menit";
            _service.UsageLimitMinutes = minutes;
            SaveSettings();
        }

        private void BreakTimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (BreakTimeText == null) return;
            int minutes = (int)e.NewValue;
            BreakTimeText.Text = $"{minutes} menit";
            _service.BreakTimeMinutes = minutes;
            SaveSettings();
        }

        // ── Status update ──────────────────────────────────────────────────
        private void UpdateStatus(object? sender, EventArgs e)
        {
            if (ActiveTimeText == null || StatusDetailText == null) return;

            int seconds = _service.GetCurrentSeconds();
            int minutes = seconds / 60;
            int secs = seconds % 60;
            ActiveTimeText.Text = $"{minutes} menit {secs} detik";

            if (_service.IsBreakActive())
            {
                StatusDetailText.Text = "Break! 🐱";
                StatusDetailText.Foreground = System.Windows.Media.Brushes.Orange;
                StatusText.Text = "Break Time! 🐱";
            }
            else if (CatPluginToggle.IsChecked == true)
            {
                StatusDetailText.Text = "Tracking...";
                StatusDetailText.Foreground = System.Windows.Media.Brushes.LimeGreen;
                StatusText.Text = $"Aktif: {minutes}m {secs}s";
            }
        }

        // ── Break events ───────────────────────────────────────────────────
        private void OnBreakTimeReached()
        {
            Dispatcher.Invoke(() =>
            {
                _overlayWindow = new CatOverlayWindow(_service.BreakTimeMinutes);
                _overlayWindow.Closed += (s, e) => _service.EndBreak();
                _overlayWindow.Show();
            });
        }

        private void OnBreakEnded()
        {
            Dispatcher.Invoke(() =>
            {
                _overlayWindow?.Close();
                _overlayWindow = null;
            });
        }

        // ── Settings persistence ───────────────────────────────────────────
        private void LoadSettings()
        {
            try
            {
                string configPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ZeroMix", "cat_gatekeeper_config.json");

                if (System.IO.File.Exists(configPath))
                {
                    string json = System.IO.File.ReadAllText(configPath);
                    var config = System.Text.Json.JsonSerializer.Deserialize<Config>(json);
                    if (config != null)
                    {
                        UsageLimitSlider.Value = config.UsageLimitMinutes;
                        BreakTimeSlider.Value  = config.BreakTimeMinutes;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CatGatekeeper] Load settings error: {ex.Message}");
            }
        }

        private void SaveSettings()
        {
            try
            {
                string configDir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ZeroMix");
                System.IO.Directory.CreateDirectory(configDir);

                var config = new Config
                {
                    UsageLimitMinutes = _service.UsageLimitMinutes,
                    BreakTimeMinutes  = _service.BreakTimeMinutes
                };

                string json = System.Text.Json.JsonSerializer.Serialize(config,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(
                    System.IO.Path.Combine(configDir, "cat_gatekeeper_config.json"), json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CatGatekeeper] Save settings error: {ex.Message}");
            }
        }

        private class Config
        {
            public int UsageLimitMinutes { get; set; } = 60;
            public int BreakTimeMinutes  { get; set; } = 5;
        }
    }
}
