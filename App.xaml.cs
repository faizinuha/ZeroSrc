using System;
using System.Windows;

namespace ZeroMix
{
    public partial class App : Application
    {
        public static HotkeyCore? HotkeyCoreInstance { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
        }
    }
}