using System.Configuration;
using System.Data;
using System.Windows;
using ZeroMix.Core;
using System.Threading.Tasks;

namespace ZeroMix
{
    using System.Windows;

    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);


            // Periksa pembaruan saat aplikasi dimulai
            try
            {
                var updater = new MegaUpdater();
                await updater.CheckAndUpdateAsync();
            }
            catch (Exception ex)
            {

                System.Diagnostics.Debug.WriteLine($"Error during update check: {ex.Message}");
                MessageBox.Show("Terjadi kesalahan saat memeriksa pembaruan. Silakan coba lagi nanti.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Jalankan HotkeyCore yang mendaftar hotkey global dan menampilkan overlay saat ditekan.
            var hotkeyCore = new HotkeyCore();
            hotkeyCore.Show();
        }
    }
}
