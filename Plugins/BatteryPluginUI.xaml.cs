using System;
using System.Windows;
using System.Windows.Controls;

namespace ZeroMix.Plugins
{
    public partial class BatteryPluginUI : System.Windows.Controls.UserControl
    {
        private BatteryPlugin _plugin = new BatteryPlugin();

        public BatteryPluginUI()
        {
            InitializeComponent();
            _plugin.OnBatteryThresholdReached += (percent, isCharging, isGreeting) => ShowNotification(percent, isCharging, isGreeting);
            
            // Fill initial values
            TxtMorning.Text = _plugin.TextMorning;
            TxtAfternoon.Text = _plugin.TextAfternoon;
            TxtEvening.Text = _plugin.TextEvening;
            TxtNight.Text = _plugin.TextNight;
            TxtBattWarn.Text = _plugin.TextBatteryWarn;
            TxtBattCrit.Text = _plugin.TextBatteryCritical;

            this.Loaded += async (s, e) => {
                // Beri jeda sebentar agar UI utama stabil dulu sebelum buka window baru
                await System.Threading.Tasks.Task.Delay(800);
                
                if (BatteryPluginToggle.IsChecked == true)
                {
                    ShowNotification(0, false, true); 
                }
            };
        }

        private async void BatteryPluginToggle_Click(object sender, RoutedEventArgs e)
        {
            if (BatteryPluginToggle.IsChecked == true)
            {
                await RunPluginProcess("Downloading Mascot Assets...");
                _plugin.Start();
                // Gunakan Task.Run agar tidak menghambat tombol saat memicu jendela
                System.Threading.Tasks.Task.Run(async () => {
                    await System.Threading.Tasks.Task.Delay(200);
                    ShowNotification(0, false, true); 
                });
            }
            else
            {
                _plugin.Stop();
            }
        }

        private void SetupBatteryBtn_Click(object sender, RoutedEventArgs e)
        {
            BatterySettingsBorder.Visibility = BatterySettingsBorder.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private async void BatteryCard_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var result = System.Windows.MessageBox.Show(
                "Apakah Anda yakin ingin menghapus (uninstall) Plugin Battery Assistant?",
                "Konfirmasi Uninstall",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                await RunPluginProcess("Uninstalling Battery Assistant...");
                // Hide the card after uninstall (mocking uninstall)
                this.Visibility = Visibility.Collapsed;
            }
        }

        private async System.Threading.Tasks.Task RunPluginProcess(string statusLabel)
        {
            PluginProgressPanel.Visibility = Visibility.Visible;
            PluginProgressBar.Value = 0;

            for (int i = 0; i <= 100; i += 5)
            {
                PluginStatusText.Text = $"{statusLabel} {i}%";
                PluginProgressBar.Value = i;
                await System.Threading.Tasks.Task.Delay(50);
            }

            await System.Threading.Tasks.Task.Delay(500);
            PluginProgressPanel.Visibility = Visibility.Collapsed;
        }

        private void SaveDialog_Click(object sender, RoutedEventArgs e)
        {
            _plugin.TextMorning = TxtMorning.Text;
            _plugin.TextAfternoon = TxtAfternoon.Text;
            _plugin.TextEvening = TxtEvening.Text;
            _plugin.TextNight = TxtNight.Text;
            _plugin.TextBatteryWarn = TxtBattWarn.Text;
            _plugin.TextBatteryCritical = TxtBattCrit.Text;
            
            System.Windows.MessageBox.Show("Mascot dialogs updated successfully!", "Saved");
            BatterySettingsBorder.Visibility = Visibility.Collapsed;
        }

        private void ShowNotification(int percent, bool isCharging, bool isGreeting = false)
        {
            var app = System.Windows.Application.Current;
            if (app == null) return;

            app.Dispatcher.BeginInvoke(new Action(() => {
                try
                {
                    var win = new BatteryNotificationWindow(percent, _plugin, isGreeting);
                    win.Show();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Notification Show Error: " + ex.Message);
                }
            }));
        }
    }
}
