// File: HotkeyCore.cs
using System;
using System.Windows;
using System.Windows.Interop;

namespace ZeroSrc
{
    public class HotkeyCore : Window
    {
        private HwndSource? _source;
        private SearchOverlay? _overlay;

        public HotkeyCore()
        {
            this.Visibility = Visibility.Hidden;
            this.ShowInTaskbar = false;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            _source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            _source.AddHook(HwndHook);
            HotkeyManager.Register(_source.Handle);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_source != null)
                HotkeyManager.Unregister(_source.Handle);
            base.OnClosed(e);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY && wParam.ToInt32() == HotkeyManager.HOTKEY_ID)
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
                handled = true;
            }
            return IntPtr.Zero;
        }
    }
}
