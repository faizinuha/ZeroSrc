using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace ZeroMix.ZeroShell
{
    /// <summary>
    /// Intercepts Start button clicks via WH_MOUSE_LL global hook.
    /// When Start button is clicked: hides Windows Start Menu, shows ZeroLaunchpad.
    /// </summary>
    public class StartMenuInterceptor : IDisposable
    {
        #region P/Invoke
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [StructLayout(LayoutKind.Sequential)]
        struct POINT { public int x, y; }

        [StructLayout(LayoutKind.Sequential)]
        struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData, flags, time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct RECT { public int Left, Top, Right, Bottom; }

        const int WH_MOUSE_LL  = 14;
        const int WM_LBUTTONDOWN = 0x0201;
        const int SW_HIDE = 0;
        const int SW_SHOW = 5;
        #endregion

        private IntPtr _hookHandle = IntPtr.Zero;
        private LowLevelMouseProc? _proc;
        private ZeroLaunchpad? _launchpad;
        private bool _enabled = false;

        public bool IsEnabled => _enabled;

        public void Enable()
        {
            if (_enabled) return;
            _proc = HookCallback;
            using var curProcess = Process.GetCurrentProcess();
            using var curModule  = curProcess.MainModule!;
            _hookHandle = SetWindowsHookEx(WH_MOUSE_LL, _proc,
                GetModuleHandle(curModule.ModuleName), 0);
            _enabled = true;
        }

        public void Disable()
        {
            if (!_enabled) return;
            if (_hookHandle != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookHandle);
                _hookHandle = IntPtr.Zero;
            }
            _enabled = false;
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_LBUTTONDOWN)
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
            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        private bool IsStartButtonClick(int x, int y)
        {
            // Find Start button — Win10: "Start" button inside Shell_TrayWnd
            IntPtr taskbar = FindWindow("Shell_TrayWnd", null);
            if (taskbar == IntPtr.Zero) return false;

            // Try to find the Start button child
            IntPtr startBtn = FindWindowEx(taskbar, IntPtr.Zero, "Start", null);
            if (startBtn == IntPtr.Zero)
                startBtn = FindWindowEx(taskbar, IntPtr.Zero, "Button", null);

            if (startBtn != IntPtr.Zero)
            {
                if (GetWindowRect(startBtn, out RECT r))
                    return x >= r.Left && x <= r.Right && y >= r.Top && y <= r.Bottom;
            }

            // Fallback: bottom-left corner heuristic (Win10 default Start button position)
            double screenH = SystemParameters.PrimaryScreenHeight;
            double taskbarH = 40; // typical taskbar height
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
                        ShowWindow(p.MainWindowHandle, SW_HIDE);
                }
            }
            catch { }
        }

        public void Dispose() => Disable();
    }
}
