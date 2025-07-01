
using System;
using System.Runtime.InteropServices;

namespace ZeroSrc
{
    public static class HotkeyManager
    {
        public const int HOTKEY_ID = 9000;
        private const uint MOD_ALT = 0x0001;
        private const uint VK_SPACE = 0x20;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public static void Register(IntPtr handle)
        {
            RegisterHotKey(handle, HOTKEY_ID, MOD_ALT, VK_SPACE);
        }

        public static void Unregister(IntPtr handle)
        {
            UnregisterHotKey(handle, HOTKEY_ID);
        }
    }
}
