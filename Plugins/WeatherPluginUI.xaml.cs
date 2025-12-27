using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ZeroMix.Plugins
{
    public partial class WeatherPluginUI : System.Windows.Controls.UserControl
    {
        private WeatherPlugin _plugin = new WeatherPlugin();
        private string? _initialWallpaperPath;

        public WeatherPluginUI()
        {
            InitializeComponent();
            _initialWallpaperPath = GetSystemWallpaperPath();
            
            // Initial UI Sync
            WeatherCityInput.Text = _plugin.City;
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

        private async void WeatherPluginToggle_Click(object sender, RoutedEventArgs e)
        {
            if (WeatherPluginToggle.IsChecked == true)
            {
                PluginProgressPanel.Visibility = Visibility.Visible;
                WeatherPluginToggle.IsEnabled = false;

                for (int i = 0; i <= 100; i += 5)
                {
                    PluginStatusText.Text = $"Downloading Plugin Dynamic Progress... {i}%";
                    PluginProgressBar.Value = i;
                    await Task.Delay(100);
                }

                PluginProgressPanel.Visibility = Visibility.Collapsed;
                WeatherPluginToggle.IsEnabled = true;
                UninstallPluginBtn.Visibility = Visibility.Visible;
                
                // Auto-integrate default videos (Optional custom files)
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                _plugin.SunnyVideoPath = Path.Combine(baseDir, "Resource", "Video", "Vs (1).mp4");
                _plugin.RainyVideoPath = Path.Combine(baseDir, "Resource", "Video", "Vs (2).mp4");
                _plugin.CloudyVideoPath = Path.Combine(baseDir, "Resource", "Video", "Vs (3).mp4");

                WeatherConfigBorder.Visibility = Visibility.Visible;
                _plugin.Start();
            }
            else
            {
                WeatherConfigBorder.Visibility = Visibility.Collapsed;
                UninstallPluginBtn.Visibility = Visibility.Collapsed;
                _plugin.Stop();
                Wallpapers.StopVideoWallpaper();
            }
        }

        private async void UninstallPluginBtn_Click(object sender, RoutedEventArgs e)
        {
            var result = System.Windows.MessageBox.Show("Are you sure you want to uninstall this plugin? Settings will be reset.", "Confirm Uninstall", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            PluginProgressPanel.Visibility = Visibility.Visible;
            UninstallPluginBtn.IsEnabled = false;
            WeatherPluginToggle.IsEnabled = false;

            for (int i = 100; i >= 0; i -= 5)
            {
                PluginStatusText.Text = $"Uninstalling Plugin... {i}%";
                PluginProgressBar.Value = i;
                await Task.Delay(50);
            }

            WeatherPluginToggle.IsChecked = false;
            WeatherPluginToggle.IsEnabled = true;
            UninstallPluginBtn.IsEnabled = true;
            UninstallPluginBtn.Visibility = Visibility.Collapsed;
            PluginProgressPanel.Visibility = Visibility.Collapsed;
            WeatherConfigBorder.Visibility = Visibility.Collapsed;
            
            _plugin.Stop();
            Wallpapers.StopVideoWallpaper();
            RestoreDefaultWallpaper();
            
            System.Windows.MessageBox.Show("Plugin uninstalled and wallpaper reverted.", "Success");
        }

        private void RestoreDefaultWallpaper()
        {
            Wallpapers.StopVideoWallpaper();
            if (!string.IsNullOrEmpty(_initialWallpaperPath) && File.Exists(_initialWallpaperPath))
            {
                try { NativeMethods.SetWallpaper(_initialWallpaperPath); } catch { }
            }
        }

        private void BrowseWeatherVideo_Click(object sender, RoutedEventArgs e)
        {
            var btn = (System.Windows.Controls.Button)sender;
            string type = btn.Tag.ToString();

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = $"Select Video for {type} Weather",
                Filter = "Video Files|*.mp4;*.wmv;*.mov;*.avi|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                switch (type)
                {
                    case "Sunny": _plugin.SunnyVideoPath = dialog.FileName; btn.Content = "☀️ Selected"; break;
                    case "Rainy": _plugin.RainyVideoPath = dialog.FileName; btn.Content = "🌧️ Selected"; break;
                    case "Cloudy": _plugin.CloudyVideoPath = dialog.FileName; btn.Content = "☁️ Selected"; break;
                }
                if (_plugin.IsActive) _ = _plugin.CheckWeatherAsync();
            }
        }

        private void UpdateWeatherLocation_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(WeatherCityInput.Text))
            {
                _plugin.City = WeatherCityInput.Text;
                _ = _plugin.CheckWeatherAsync();
                System.Windows.MessageBox.Show($"Location updated to {_plugin.City}", "Success");
            }
        }
    }
}
