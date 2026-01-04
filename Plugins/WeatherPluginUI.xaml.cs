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

                for (int i = 0; i <= 100; i += 10)
                {
                    PluginStatusText.Text = $"Fetching Weather API Progress... {i}%";
                    PluginProgressBar.Value = i;
                    await Task.Delay(50);
                }

                PluginProgressPanel.Visibility = Visibility.Collapsed;
                WeatherPluginToggle.IsEnabled = true;
                
                WeatherConfigBorder.Visibility = Visibility.Visible;
                _plugin.Start();
            }
            else
            {
                WeatherConfigBorder.Visibility = Visibility.Collapsed;
                _plugin.Stop();
                RestoreDefaultWallpaper();
            }
        }

        private async void WeatherCard_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount != 2) return;

            var result = System.Windows.MessageBox.Show("Are you sure you want to uninstall this plugin? Settings will be reset.", "Confirm Uninstall", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            PluginProgressPanel.Visibility = Visibility.Visible;
            WeatherPluginToggle.IsEnabled = false;

            for (int i = 100; i >= 0; i -= 10)
            {
                PluginStatusText.Text = $"Uninstalling Plugin... {i}%";
                PluginProgressBar.Value = i;
                await Task.Delay(30);
            }

            WeatherPluginToggle.IsChecked = false;
            WeatherPluginToggle.IsEnabled = true;
            PluginProgressPanel.Visibility = Visibility.Collapsed;
            WeatherConfigBorder.Visibility = Visibility.Collapsed;
            
            _plugin.Stop();
            RestoreDefaultWallpaper();
            
            System.Windows.MessageBox.Show("Plugin uninstalled and wallpaper reverted.", "Success");
        }

        private void RestoreDefaultWallpaper()
        {
            Wallpapers.StopVideoWallpaper();
            
            // Explicitly set the original wallpaper to ensure clean return
            if (!string.IsNullOrEmpty(_initialWallpaperPath) && File.Exists(_initialWallpaperPath))
            {
                try { NativeMethods.SetWallpaper(_initialWallpaperPath); } catch { }
            }
            
            // Refresh desktop one last time
            Wallpapers.RefreshDesktop();
        }

        private void BrowseWeatherVideo_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Using automated high-quality weather videos from API. Browse option is disabled for better performance optimization.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UpdateWeatherLocation_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(WeatherCityInput.Text))
            {
                _plugin.City = WeatherCityInput.Text;
                _ = _plugin.CheckWeatherAsync();
                System.Windows.MessageBox.Show($"Location updated to {_plugin.City}. Weather assets will sync.", "Success");
            }
        }
    }
}
