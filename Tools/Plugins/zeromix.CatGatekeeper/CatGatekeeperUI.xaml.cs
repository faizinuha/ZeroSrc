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
        private DispatcherTimer _updateTimer;
        private CatOverlayWindow? _overlayWindow;

        // Parameterless constructor untuk XAML instantiation
        public CatGatekeeperUI() : this(new CatGatekeeperService()) { }

        public CatGatekeeperUI(CatGatekeeperService service)
        {
            InitializeComponent();
            
            _service = service;
            _service.OnBreakTimeReached += OnBreakTimeReached;
            _service.OnBreakEnded += OnBreakEnded;
            
            // Update UI every second
            _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _updateTimer.Tick += UpdateStatus;
            _updateTimer.Start();
            
            // Load saved settings
            LoadSettings();
        }

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

        private void UpdateStatus(object? sender, EventArgs e)
        {
            int seconds = _service.GetCurrentSeconds();
            int minutes = seconds / 60;
            int secs = seconds % 60;
            
            ActiveTimeText.Text = $"{minutes} menit {secs} detik";
            
            if (_service.IsBreakActive())
            {
                StatusText.Text = "Break Time! 🐱";
                StatusText.Foreground = System.Windows.Media.Brushes.Orange;
            }
            else
            {
                StatusText.Text = "Tracking...";
                StatusText.Foreground = System.Windows.Media.Brushes.Green;
            }
        }

        private void OnBreakTimeReached()
        {
            Dispatcher.Invoke(() =>
            {
                // Show cat overlay window
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

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
            e.Handled = true;
        }

        private void LoadSettings()
        {
            try
            {
                string configPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ZeroMix",
                    "cat_gatekeeper_config.json"
                );

                if (System.IO.File.Exists(configPath))
                {
                    string json = System.IO.File.ReadAllText(configPath);
                    var config = System.Text.Json.JsonSerializer.Deserialize<Config>(json);
                    
                    if (config != null)
                    {
                        UsageLimitSlider.Value = config.UsageLimitMinutes;
                        BreakTimeSlider.Value = config.BreakTimeMinutes;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CatGatekeeper] Failed to load settings: {ex.Message}");
            }
        }

        private void SaveSettings()
        {
            try
            {
                string configDir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ZeroMix"
                );
                
                if (!System.IO.Directory.Exists(configDir))
                {
                    System.IO.Directory.CreateDirectory(configDir);
                }

                var config = new Config
                {
                    UsageLimitMinutes = _service.UsageLimitMinutes,
                    BreakTimeMinutes = _service.BreakTimeMinutes
                };

                string json = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });

                string configPath = System.IO.Path.Combine(configDir, "cat_gatekeeper_config.json");
                System.IO.File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CatGatekeeper] Failed to save settings: {ex.Message}");
            }
        }

        private class Config
        {
            public int UsageLimitMinutes { get; set; }
            public int BreakTimeMinutes { get; set; }
        }
    }
}
