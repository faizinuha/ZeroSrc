using System;
using System.Windows;

namespace ZeroMix
{
    public partial class App : System.Windows.Application
    {
        public static HotkeyCore? HotkeyCoreInstance { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            var hotkeyCore = new HotkeyCore();
            hotkeyCore.Show();
            base.OnStartup(e);
        }
    }
}