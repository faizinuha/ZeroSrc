using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ZeroMix
{
    public static class HotkeyManager
    {
        // P/Invoke for registering and unregistering hotkeys
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // Modifiers
        public const uint MOD_NONE = 0x0000;
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;
        public const uint MOD_NOREPEAT = 0x4000;

        // Main Overlay Hotkey ID
        public const int OVERLAY_HOTKEY_ID = 9000;

        private static int _nextCustomHotkeyId = 9001;
        private static readonly Dictionary<int, Action> _hotkeyActions = new Dictionary<int, Action>();

        public static bool Register(IntPtr handle, int id, uint modifiers, uint vk, Action action)
        {
            if (RegisterHotKey(handle, id, modifiers, vk))
            {
                _hotkeyActions[id] = action;
                return true;
            }
            return false;
        }

        public static int RegisterCustom(IntPtr handle, uint modifiers, uint vk, string applicationPath)
        {
            int id = _nextCustomHotkeyId++;
            Action action = () => {
                try
                {
                    Process.Start(new ProcessStartInfo(applicationPath) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    // Handle exception (e.g., file not found)
                    System.Windows.MessageBox.Show($"Failed to start application: {applicationPath}\nError: {ex.Message}");
                }
            };

            if (Register(handle, id, modifiers, vk, action))
            {
                return id;
            }
            return 0; // Failed
        }

        public static void UnregisterAll(IntPtr handle)
        {
            // Unregister main overlay hotkey
            UnregisterHotKey(handle, OVERLAY_HOTKEY_ID);

            // Unregister all custom hotkeys
            foreach (var id in new List<int>(_hotkeyActions.Keys))
            {
                if (id != OVERLAY_HOTKEY_ID)
                {
                    UnregisterHotKey(handle, id);
                }
            }
            _hotkeyActions.Clear();
        }

        public static void HandleHotkey(int id)
        {
            if (_hotkeyActions.TryGetValue(id, out var action))
            {
                action?.Invoke();
            }
        }
    }
}