using System.Configuration;
using System.Data;
using System.Windows;

namespace ZeroSrc
{
    using System.Windows;

    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // Jalankan HotkeyCore yang mendaftar hotkey global dan menampilkan overlay saat ditekan.
            var hotkeyCore = new HotkeyCore();
            hotkeyCore.Show();
        }
    }
}
