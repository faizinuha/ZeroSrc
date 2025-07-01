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
            new HotkeyCore().Show(); // Ganti nama sesuai class kamu
        }
    }
}
