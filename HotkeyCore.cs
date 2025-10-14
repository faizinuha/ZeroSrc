using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Newtonsoft.Json;

namespace ZeroMix
{
    public class HotkeyCore : Window
    {
        private HwndSource? _source;
        private SearchOverlay? _overlay;
        private const string ShortcutsFilePath = "custom_shortcuts.json";

        public HotkeyCore()
        {
            this.Visibility = Visibility.Hidden;
            this.ShowInTaskbar = false;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var helper = new WindowInteropHelper(this);
            _source = HwndSource.FromHwnd(helper.Handle);
            _source.AddHook(HwndHook);

            RegisterOverlayHotkey();
            RegisterCustomHotkeys();
        }

        private void RegisterOverlayHotkey()
        {
            if (_source == null) return;
            // The action for the overlay hotkey
            Action overlayAction = () => HandleOverlayHotkey();

            // Try to register Alt+Space, then Ctrl+Space, then Win+Space
            bool registered = HotkeyManager.Register(_source.Handle, HotkeyManager.OVERLAY_HOTKEY_ID, HotkeyManager.MOD_ALT | HotkeyManager.MOD_NOREPEAT, (uint)KeyInterop.VirtualKeyFromKey(Key.Space), overlayAction);
            if (!registered)
                registered = HotkeyManager.Register(_source.Handle, HotkeyManager.OVERLAY_HOTKEY_ID, HotkeyManager.MOD_CONTROL | HotkeyManager.MOD_NOREPEAT, (uint)KeyInterop.VirtualKeyFromKey(Key.Space), overlayAction);
            if (!registered)
                registered = HotkeyManager.Register(_source.Handle, HotkeyManager.OVERLAY_HOTKEY_ID, HotkeyManager.MOD_WIN | HotkeyManager.MOD_NOREPEAT, (uint)KeyInterop.VirtualKeyFromKey(Key.Space), overlayAction);

            if (!registered)
            {
                MessageBox.Show("Failed to register the global overlay hotkey. Please check for conflicts or run as administrator.");
            }
        }

        private void RegisterCustomHotkeys()
        {
            if (_source == null) return;
            if (!File.Exists(ShortcutsFilePath)) return;

            var json = File.ReadAllText(ShortcutsFilePath);
            var shortcuts = JsonConvert.DeserializeObject<List<CustomShortcut>>(json);

            if (shortcuts == null) return;

            foreach (var shortcut in shortcuts)
            {
                if (HotkeyParser.TryParse(shortcut.Hotkey, out uint modifiers, out uint vk))
                {
                    HotkeyManager.RegisterCustom(_source.Handle, modifiers, vk, shortcut.ApplicationPath);
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_source != null)
            {
                HotkeyManager.UnregisterAll(_source.Handle);
                _source.RemoveHook(HwndHook);
            }
            base.OnClosed(e);
        }

        private void HandleOverlayHotkey()
        {
            if (_overlay == null || !_overlay.IsVisible)
            {
                _overlay = new SearchOverlay();
                _overlay.Closed += (s, e) => _overlay = null;
                _overlay.Show();
            }
            else
            {
                _overlay.BeginFadeOutAndCloseByMain();
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                HotkeyManager.HandleHotkey(id);
                handled = true;
            }
            return IntPtr.Zero;
        }
    }

    public static class HotkeyParser
    {
        public static bool TryParse(string hotkeyString, out uint modifiers, out uint vk)
        {
            modifiers = HotkeyManager.MOD_NONE;
            vk = 0;

            if (string.IsNullOrWhiteSpace(hotkeyString)) return false;

            var parts = hotkeyString.Split('+').Select(p => p.Trim().ToUpper()).ToList();
            if (parts.Count == 0) return false;

            var keyPart = parts.Last();
            parts.RemoveAt(parts.Count - 1);

            foreach (var part in parts)
            {
                switch (part)
                {
                    case "CTRL":
                    case "CONTROL":
                        modifiers |= HotkeyManager.MOD_CONTROL;
                        break;
                    case "ALT":
                        modifiers |= HotkeyManager.MOD_ALT;
                        break;
                    case "SHIFT":
                        modifiers |= HotkeyManager.MOD_SHIFT;
                        break;
                    case "WIN":
                    case "WINDOWS":
                        modifiers |= HotkeyManager.MOD_WIN;
                        break;
                }
            }

            try
            {
                var key = (Key)Enum.Parse(typeof(Key), keyPart, true);
                vk = (uint)KeyInterop.VirtualKeyFromKey(key);
                return true;
            }
            catch
            {
                return false; // Failed to parse key
            }
        }
    }
}