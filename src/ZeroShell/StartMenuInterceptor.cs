using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using ZeroMix.Native;

namespace ZeroMix.ZeroShell
{
    /// <summary>
    /// Intercepts Start button clicks via WH_MOUSE_LL global hook.
    /// When Start button is clicked: hides Windows Start Menu, shows ZeroLaunchpad.
    /// Uses centralized Win32 API from ZeroMix.Native.Win32 for P/Invoke declarations.
    /// Includes health check timer to validate hook is alive and re-install if dead.
    /// </summary>
    public class StartMenuInterceptor : IDisposable
    {
        #region Structures & Constants

        [StructLayout(LayoutKind.Sequential)]
        struct POINT { public int x, y; }

        [StructLayout(LayoutKind.Sequential)]
        struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData, flags, time;
            public IntPtr dwExtraInfo;
        }

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;
        #endregion

        private IntPtr _hookHandle = IntPtr.Zero;
        private Win32.Shell.LowLevelMouseProc? _proc;
        private ZeroLaunchpad? _launchpad;
        private bool _enabled = false;
        private System.Windows.Threading.DispatcherTimer? _healthCheckTimer;
        // Timestamp of last hook callback — used to detect stale/timeout of WH_MOUSE_LL
        private DateTime _lastCallbackUtc = DateTime.MinValue;

        public bool IsEnabled => _enabled;

        public void Enable()
        {
            if (_enabled) return;
            _proc = HookCallback;
            using var curProcess = Process.GetCurrentProcess();
            using var curModule  = curProcess.MainModule!;
            _hookHandle = Win32.Shell.SetWindowsHookEx(Win32.Shell.WH_MOUSE_LL, _proc,
                Win32.Shell.GetModuleHandle(curModule.ModuleName), 0);

            // record initial callback time if hook installed successfully
            if (_hookHandle != IntPtr.Zero) _lastCallbackUtc = DateTime.UtcNow;
            _enabled = true;

            // Start health check timer to validate hook is alive
            StartHealthCheck();
        }

        public void Disable()
        {
            if (!_enabled) return;
            
            // Stop health check
            if (_healthCheckTimer != null)
            {
                _healthCheckTimer.Stop();
                _healthCheckTimer = null;
            }

            if (_hookHandle != IntPtr.Zero)
            {
                Win32.Shell.UnhookWindowsHookEx(_hookHandle);
                _hookHandle = IntPtr.Zero;
            }
            _enabled = false;
        }

        private void StartHealthCheck()
        {
            _healthCheckTimer = new System.Windows.Threading.DispatcherTimer 
            { 
                Interval = TimeSpan.FromSeconds(5) 
            };
            _healthCheckTimer.Tick += (s, e) => ValidateHook();
            _healthCheckTimer.Start();
        }

        private void ValidateHook()
        {
            try
            {
                // If hook handle is gone or we haven't seen callbacks for a while, re-install
                var now = DateTime.UtcNow;
                if (_hookHandle == IntPtr.Zero || (now - _lastCallbackUtc) > TimeSpan.FromSeconds(12))
                {
                    // Re-install the low-level mouse hook (best-effort)
                    Disable();
                    Enable();
                }
            }
            catch { /* swallow errors — this is a resiliency check */ }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            // Update last callback time for health checks
            _lastCallbackUtc = DateTime.UtcNow;

            if (nCode >= 0 && wParam == (IntPtr)Win32.Shell.WM_LBUTTONDOWN)
            {
                var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                int x = hookStruct.pt.x;
                int y = hookStruct.pt.y;

                if (IsStartButtonClick(x, y))
                {
                    // Run on UI thread
                    System.Windows.Application.Current?.Dispatcher.Invoke(() => HandleStartClick());
                    // Consume the click — don't pass to Windows
                    return new IntPtr(1);
                }
            }
            return Win32.Shell.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        private bool IsStartButtonClick(int x, int y)
        {
            // Step 1: Cari Shell_TrayWnd di ALL monitors via EnumWindows
            // Multi-monitor: Win10 creates Shell_TrayWnd per monitor + Shell_SecondaryTrayWnd
            bool found = false;
            int btnLeft = 0, btnRight = 0, btnTop = 0, btnBottom = 0;

            Win32.Window.EnumWindows((hwnd, _) =>
            {
                var sb = new System.Text.StringBuilder(256);
                Win32.Window.GetClassName(hwnd, sb, sb.Capacity);
                string cls = sb.ToString();

                // Check primary and secondary taskbars
                if (cls == "Shell_TrayWnd" || cls == "Shell_SecondaryTrayWnd")
                {
                    IntPtr startBtn = Win32.Window.FindWindowEx(hwnd, IntPtr.Zero, "Start", null);
                    if (startBtn == IntPtr.Zero)
                        startBtn = Win32.Window.FindWindowEx(hwnd, IntPtr.Zero, "Button", null);

                    if (startBtn != IntPtr.Zero && Win32.Window.GetWindowRect(startBtn, out Win32.Window.RECT r))
                    {
                        // Check if click falls within this Start button
                        if (x >= r.Left && x <= r.Right && y >= r.Top && y <= r.Bottom)
                        {
                            btnLeft = r.Left; btnRight = r.Right;
                            btnTop = r.Top; btnBottom = r.Bottom;
                            found = true;
                            return false; // stop enumeration
                        }
                    }
                }
                return true;
            }, IntPtr.Zero);

            if (found)
                return true;

            // Step 2 (LAST RESORT): bottom-left corner heuristic — hanya jika FindWindow gagal total
            // Gunakan screen bounds dari monitor where click occurred, bukan PrimaryScreen
            double screenH = SystemParameters.PrimaryScreenHeight;
            double taskbarH = 40;
            return x < 60 && y > (screenH - taskbarH - 5);
        }

        private void HandleStartClick()
        {
            // Hide Windows Start Menu if it's open
            HideWindowsStartMenu();

            // Toggle launchpad
            if (_launchpad != null && _launchpad.IsVisible)
            {
                _launchpad.Close();
                _launchpad = null;
            }
            else
            {
                _launchpad = new ZeroLaunchpad();
                _launchpad.Closed += (s, e) => _launchpad = null;
                _launchpad.Show();
            }
        }

        private void HideWindowsStartMenu()
        {
            try
            {
                // Win10: StartMenuExperienceHost
                foreach (var p in System.Diagnostics.Process.GetProcessesByName("StartMenuExperienceHost"))
                {
                    if (p.MainWindowHandle != IntPtr.Zero)
                        Win32.Window.ShowWindow(p.MainWindowHandle, SW_HIDE);
                }
            }
            catch { }
        }

        public void Dispose()
        {
            Disable();
        }
    }
}
