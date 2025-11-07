using System;
using System.Windows;
using ZeroMix.Core;

namespace ZeroMix
{
    public partial class App : Application
    {
        public static HotkeyCore? HotkeyCoreInstance { get; private set; }
    }
}