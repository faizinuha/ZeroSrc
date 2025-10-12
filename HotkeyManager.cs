using System;
using System.Runtime.InteropServices;

namespace ZeroMix
{
    public static class HotkeyManager
    {
        public const int HOTKEY_ID = 9000;
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;
        public const uint MOD_NOREPEAT = 0x4000;
        private const uint VK_SPACE = 0x20;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        public static bool Register(IntPtr handle, uint modifiers)
        {
            try
            {
                return RegisterHotKey(handle, HOTKEY_ID, modifiers, VK_SPACE);
            }
            catch
            {
                return false;
            }
        }

        public static bool Unregister(IntPtr handle)
        {
            try
            {
                return UnregisterHotKey(handle, HOTKEY_ID);
            }
            catch
            {
                return false;
            }
        }

        public static bool IsHotkeyRegistered(IntPtr handle)
        {
            // Implementasi untuk memeriksa apakah hotkey sudah terdaftar
            return true; // Placeholder
        }
    }
}
