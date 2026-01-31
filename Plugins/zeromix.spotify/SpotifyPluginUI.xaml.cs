using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ZeroMix.Plugins.Spotify
{
    public partial class SpotifyPluginUI : System.Windows.Controls.UserControl
    {
        private SpotifyPlugin _plugin = new SpotifyPlugin();

        public SpotifyPluginUI()
        {
            InitializeComponent();
        }

        private async void SpotifyPluginToggle_Click(object sender, RoutedEventArgs e)
        {
            if (SpotifyPluginToggle.IsChecked == true)
            {
                await RunPluginProcess("Downloading Spotify Assets...");
                _plugin.Start();
                System.Windows.MessageBox.Show("Spotify Pink Plugin is now ACTIVE and visible in System Tray.", "Success");
            }
            else
            {
                // Jika user mematikan toggle, tanyakan uninstall
                var result = System.Windows.MessageBox.Show(
                    "Do you want to completely UNINSTALL Spotify Pink Plugin?",
                    "Confirm Uninstall",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await RunPluginProcess("Uninstalling Spotify Plugin...");
                    _plugin.Stop();
                }
                else
                {
                    // Batalkan pematian toggle jika tidak jadi uninstall
                    SpotifyPluginToggle.IsChecked = true;
                }
            }
        }

        private async void SpotifyCard_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount != 2) return;

            var result = System.Windows.MessageBox.Show(
                "Are you sure you want to uninstall Spotify Pink Plugin?",
                "Confirm Uninstall",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                await RunPluginProcess("Uninstalling Spotify Plugin...");
                _plugin.Stop();
                // Card tetap dibiarkan visible agar user bisa install lagi tanpa refresh page
                SpotifyPluginToggle.IsChecked = false;
            }
        }

        private async Task RunPluginProcess(string statusLabel)
        {
            PluginProgressPanel.Visibility = Visibility.Visible;
            PluginProgressBar.Value = 0;
            SpotifyPluginToggle.IsEnabled = false;

            for (int i = 0; i <= 100; i += 10)
            {
                PluginStatusText.Text = $"{statusLabel} {i}%";
                PluginProgressBar.Value = i;
                await Task.Delay(50);
            }

            await Task.Delay(200);
            SpotifyPluginToggle.IsEnabled = true;
            PluginProgressPanel.Visibility = Visibility.Collapsed;
        }
    }
}
