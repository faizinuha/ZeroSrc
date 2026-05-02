using System;
using System.Windows;
using System.Windows.Threading;

namespace zeromix.CatGatekeeper
{
    public partial class CatGatekeeperUI : System.Windows.Controls.UserControl
    {
        private CatGatekeeperService _service;
        private DispatcherTimer? _updateTimer;
        private CatOverlayWindow? _overlayWindow;
        private bool _isExpanded = false;

        public CatGatekeeperUI() : this(new CatGatekeeperService()) { }

        public CatGatekeeperUI(CatGatekeeperService service)
        {
            InitializeComponent();
            _service = service;
            _service.OnBreakTimeReached += OnBreakTimeReached;
            _service.OnBreakEnded += OnBreakEnded;

            this.Loaded   += OnLoaded;
            this.Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            LoadSettings();
            _service.Start();

            _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _updateTimer.Tick += UpdateStatus;
            _updateTimer.Start();

            CatPluginToggle.IsChecked = true;

            // TestDurationSlider handler
            TestDurationSlider.ValueChanged += (s, ev) =>
            {
                int min = (int)ev.NewValue;
                TestDurationText.Text = $"{min} menit";
            };
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _updateTimer?.Stop();
            _service.Stop();
        }

        // ── Card click ─────────────────────────────────────────────────────
        private void CatCard_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isExpanded = !_isExpanded;
            CatSettingsBorder.Visibility = _isExpanded ? Visibility.Visible : Visibility.Collapsed;
            StatusText.Visibility = _isExpanded ? Visibility.Collapsed : Visibility.Visible;
        }

        // ── Test button ────────────────────────────────────────────────────
        private void TestBtn_Click(object sender, RoutedEventArgs e)
        {
            // Ambil durasi dari slider (1 atau 2 menit)
            int testMinutes = (int)(TestDurationSlider?.Value ?? 1);

            // Tutup overlay lama jika ada
            _overlayWindow?.Close();
            _overlayWindow = null;

            // Buka overlay test
            _overlayWindow = new CatOverlayWindow(testMinutes);
            _overlayWindow.Closed += (s, ev) => _overlayWindow = null;
            _overlayWindow.Show();
        }

        // ── Checkbox toggle ────────────────────────────────────────────────
        private void CatPluginToggle_Click(object sender, RoutedEventArgs e)
        {
            bool isEnabled = CatPluginToggle.IsChecked == true;
            if (isEnabled)
            {
                _service.Start();
                if (StatusDetailText != null)
                {
                    StatusDetailText.Text = "Tracking...";
                    StatusDetailText.Foreground = System.Windows.Media.Brushes.LimeGreen;
                }
            }
            else
            {
                _service.Stop();
                if (StatusDetailText != null)
                {
                    StatusDetailText.Text = "Disabled";
                    StatusDetailText.Foreground = System.Windows.Media.Brushes.Gray;
                }
            }
        }

        // ── Sliders ────────────────────────────────────────────────────────
        private void UsageLimitSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (UsageLimitText == null) return;
            int minutes = (int)e.NewValue;
            UsageLimitText.Text = $"{minutes} menit";
            _service.UsageLimitMinutes = minutes;
            SaveSettings();
        }

        private void BreakTimeSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
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
            int m = seconds / 60;
            int s = seconds % 60;
            ActiveTimeText.Text = $"{m}m {s}s";

            if (_service.IsBreakActive())
            {
                StatusDetailText.Text = "Break! 🐱";
                StatusDetailText.Foreground = System.Windows.Media.Brushes.Orange;
                if (StatusText != null) StatusText.Text = "Break Time! 🐱";
            }
            else if (CatPluginToggle.IsChecked == true)
            {
                StatusDetailText.Text = "Tracking...";
                StatusDetailText.Foreground = System.Windows.Media.Brushes.LimeGreen;
                if (StatusText != null) StatusText.Text = $"Aktif: {m}m {s}s";
            }
        }

        // ── Break events ───────────────────────────────────────────────────
        private void OnBreakTimeReached()
        {
            Dispatcher.Invoke(() =>
            {
                _overlayWindow?.Close();
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

        // ── Settings ───────────────────────────────────────────────────────
        private void LoadSettings()
        {
            try
            {
                string path = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ZeroMix", "cat_gatekeeper_config.json");

                if (System.IO.File.Exists(path))
                {
                    var json = System.IO.File.ReadAllText(path);
                    var cfg  = System.Text.Json.JsonSerializer.Deserialize<Config>(json);
                    if (cfg != null)
                    {
                        UsageLimitSlider.Value = cfg.UsageLimitMinutes;
                        BreakTimeSlider.Value  = cfg.BreakTimeMinutes;
                    }
                }
            }
            catch { }
        }

        private void Credit_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void SaveSettings()
        {
            try
            {
                string dir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ZeroMix");
                System.IO.Directory.CreateDirectory(dir);

                var cfg  = new Config { UsageLimitMinutes = _service.UsageLimitMinutes, BreakTimeMinutes = _service.BreakTimeMinutes };
                var json = System.Text.Json.JsonSerializer.Serialize(cfg, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "cat_gatekeeper_config.json"), json);
            }
            catch { }
        }

        private class Config
        {
            public int UsageLimitMinutes { get; set; } = 60;
            public int BreakTimeMinutes  { get; set; } = 5;
        }
    }
}
