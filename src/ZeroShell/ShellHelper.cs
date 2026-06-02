using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media;
using ZeroMix.Native;

namespace ZeroMix.ZeroShell
{
    public static class ShellHelper
    {
        #region Win32 Bridge

        // All Win32 interop is now centralized in ZeroMix.Native.Win32.
        // This file references the Win32 helpers directly instead of redeclaring DllImport signatures.

        #endregion

        #region Structs & Enums

        [StructLayout(LayoutKind.Sequential)]
        struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        enum WindowCompositionAttribute
        {
            WCA_ACCENT_POLICY = 19
        }

        [StructLayout(LayoutKind.Sequential)]
        struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4
        }

        #endregion

        #region Accent / Blur

        public static void ApplyBlur(IntPtr hwnd, int alpha = 0xCC, int rgb = 0x000000)
        {
            var accent = new AccentPolicy
            {
                AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
                AccentFlags = 0,
                GradientColor = (alpha << 24) | rgb
            };

            var size = Marshal.SizeOf(accent);
            var ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(accent, ptr, false);

            var data = new Win32.DWM.WindowCompositionAttributeData
            {
                Attribute = Win32.DWM.WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = size,
                Data = ptr
            };

            Win32.DWM.SetWindowCompositionAttribute(hwnd, ref data);
            Marshal.FreeHGlobal(ptr);
        }

        public static void ApplyBlurOnly(IntPtr hwnd)
        {
            var accent = new AccentPolicy { AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND };
            var size = Marshal.SizeOf(accent);
            var ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(accent, ptr, false);
            var data = new Win32.DWM.WindowCompositionAttributeData
            {
                Attribute = Win32.DWM.WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = size,
                Data = ptr
            };
            Win32.DWM.SetWindowCompositionAttribute(hwnd, ref data);
            Marshal.FreeHGlobal(ptr);
        }

        public static void DisableAccent(IntPtr hwnd)
        {
            var accent = new AccentPolicy { AccentState = AccentState.ACCENT_DISABLED };
            var size = Marshal.SizeOf(accent);
            var ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(accent, ptr, false);
            var data = new Win32.DWM.WindowCompositionAttributeData
            {
                Attribute = Win32.DWM.WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = size,
                Data = ptr
            };
            Win32.DWM.SetWindowCompositionAttribute(hwnd, ref data);
            Marshal.FreeHGlobal(ptr);
        }

        private static int ParseRgb(string hex)
        {
            hex = hex.TrimStart('#');
            if (hex.Length == 6)
            {
                int r = Convert.ToInt32(hex.Substring(0, 2), 16);
                int g = Convert.ToInt32(hex.Substring(2, 2), 16);
                int b = Convert.ToInt32(hex.Substring(4, 2), 16);
                return (r << 16) | (g << 8) | b;
            }
            return 0;
        }

        public static void ApplyStyle(IntPtr hwnd, WdmEntry entry)
        {
            switch (entry.Style)
            {
                case WdmStyle.AcrylicDark:
                case WdmStyle.AcrylicLight:
                case WdmStyle.GlassClear:
                case WdmStyle.FullTransparent:
                    ApplyBlur(hwnd, entry.Alpha, ParseRgb(entry.ColorHex));
                    break;
                case WdmStyle.BlurOnly:
                    ApplyBlurOnly(hwnd);
                    break;
                case WdmStyle.FloatingMacOS:
                    ApplyFloatingMacOS(hwnd);
                    break;
                case WdmStyle.None:
                default:
                    DisableAccent(hwnd);
                    break;
            }
        }

        // FloatingMacOS: fully transparent + cyan border glow via DWM
        public static void ApplyFloatingMacOS(IntPtr hwnd)
        {
            // Full transparent background
            ApplyBlur(hwnd, 0x00, 0x000000);

            // DWM border color — cyan glow (#00D4FF in ABGR = 0x00FFD400)
            // DWMWA_BORDER_COLOR = 34 (Win11+), fallback gracefully on Win10
            try
            {
                int color = unchecked((int)0x00FFD400); // ABGR: A=0, B=0xFF, G=0xD4, R=0x00
                Win32.DWM.DwmSetWindowAttribute(hwnd, 34, ref color, sizeof(int));
            }
            catch { }
        }

        /// <summary>
        /// Apply style ke windows milik process tertentu dengan class name filter.
        /// Digunakan untuk Start Menu (StartMenuExperienceHost) dan Notification (ShellExperienceHost).
        /// </summary>
        public static void ApplyStyleByProcess(string processName, string[] classNames, WdmEntry entry)
        {
            var pids = new System.Collections.Generic.HashSet<uint>();
            foreach (var p in Process.GetProcessesByName(processName))
                pids.Add((uint)p.Id);

            if (pids.Count == 0) return;

            Win32.Window.EnumWindows((hwnd, _) =>
            {
                Win32.Window.GetWindowThreadProcessId(hwnd, out uint pid);
                if (!pids.Contains(pid)) return true;

                var sb = new StringBuilder(256);
                Win32.Window.GetClassName(hwnd, sb, sb.Capacity);
                string cls = sb.ToString();

                if (System.Array.IndexOf(classNames, cls) >= 0)
                {
                    ApplyStyle(hwnd, entry);
                    // Apply ke children juga — penting untuk XAML Islands
                    Win32.Window.EnumChildWindows(hwnd, (child, _2) =>
                    {
                        ApplyStyle(child, entry);
                        return true;
                    }, IntPtr.Zero);
                }
                return true;
            }, IntPtr.Zero);
        }

        /// <summary>
        /// Apply style ke Start Menu (Win10: StartMenuExperienceHost, Win7/8: DV2ControlHost)
        /// </summary>
        public static void ApplyStartMenuStyle(WdmEntry entry)
        {
            // Win10/11
            ApplyStyleByProcess("StartMenuExperienceHost",
                new[] { "Windows.UI.Core.CoreWindow" }, entry);
            // Win10 fallback
            ApplyStyleByProcess("explorer",
                new[] { "DV2ControlHost" }, entry);
        }

        /// <summary>
        /// Apply style ke Notification Panel (Win10: ShellExperienceHost)
        /// </summary>
        public static void ApplyNotificationStyle(WdmEntry entry)
        {
            ApplyStyleByProcess("ShellExperienceHost",
                new[] { "Windows.UI.Core.CoreWindow" }, entry);
        }

        public static void ApplyStyleToChildren(IntPtr parent, WdmEntry entry)
        {
            Win32.Window.EnumChildWindows(parent, (child, lParam) =>
            {
                ApplyStyle(child, entry);
                return true;
            }, IntPtr.Zero);
        }

        /// <summary>
        /// Apply style ke Explorer windows dengan delay 300ms agar window fully rendered.
        /// </summary>
        public static async System.Threading.Tasks.Task ApplyExplorerStyleDelayed(WdmEntry entry)
        {
            await System.Threading.Tasks.Task.Delay(300);
            var pids = new System.Collections.Generic.HashSet<uint>();
            foreach (var p in Process.GetProcessesByName("explorer"))
                pids.Add((uint)p.Id);

            Win32.Window.EnumWindows((hwnd, _) =>
            {
                Win32.Window.GetWindowThreadProcessId(hwnd, out uint pid);
                if (!pids.Contains(pid)) return true;
                var sb = new StringBuilder(256);
                Win32.Window.GetClassName(hwnd, sb, sb.Capacity);
                string cls = sb.ToString();
                if (cls == "CabinetWClass" || cls == "ExplorerWClass")
                {
                    ApplyStyle(hwnd, entry);
                    ApplyStyleToChildren(hwnd, entry);
                }
                return true;
            }, IntPtr.Zero);
        }

        #endregion

        #region Window Enumeration

        public static void EnumAllWindows(Action<IntPtr, string> callback)
        {
            Win32.Window.EnumWindows((hWnd, lParam) =>
            {
                var sb = new StringBuilder(256);
                Win32.Window.GetClassName(hWnd, sb, sb.Capacity);
                callback(hWnd, sb.ToString());
                return true;
            }, IntPtr.Zero);
        }

        #endregion

        #region WinEvent Watcher

        // Multiple hooks for different event ranges
        private static readonly System.Collections.Generic.List<IntPtr> _hooks = new();
        private static Win32.Shell.WinEventDelegate? _winEventDelegate;

        // Pulse timer fallback — re-apply every 3s for taskbar redraws
        private static System.Threading.Timer? _pulseTimer;
        private static Action<IntPtr, string>? _currentCallback;

        public static void StartWatcher(Action<IntPtr, string> onWindow)
        {
            StopWatcher();
            _currentCallback = onWindow;

            _winEventDelegate = (hook, type, hwnd, idObj, idChild, thread, time) =>
            {
                // idObj == 0 = OBJID_WINDOW, but also allow -4 (OBJID_CLIENT) for taskbar redraws
                if (idObj != 0 && idObj != -4) return;
                var sb = new StringBuilder(256);
                Win32.Window.GetClassName(hwnd, sb, sb.Capacity);
                onWindow(hwnd, sb.ToString());
            };

            const uint WINEVENT_OUTOFCONTEXT = 0x0000;

            // Hook 1: Object show/hide (new windows)
            const uint EVENT_OBJECT_SHOW        = 0x8002;
            // Hook 2: Foreground change (taskbar redraws on click)
            const uint EVENT_SYSTEM_FOREGROUND  = 0x0003;
            // Hook 3: Object reorder (taskbar icon changes)
            const uint EVENT_OBJECT_REORDER     = 0x8004;
            // Hook 4: Object namechange (Start Menu open)
            const uint EVENT_OBJECT_NAMECHANGE  = 0x800C;

            _hooks.Add(Win32.Shell.SetWinEventHook(EVENT_OBJECT_SHOW, EVENT_OBJECT_SHOW,
                IntPtr.Zero, _winEventDelegate, 0, 0, WINEVENT_OUTOFCONTEXT));

            _hooks.Add(Win32.Shell.SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND,
                IntPtr.Zero, _winEventDelegate, 0, 0, WINEVENT_OUTOFCONTEXT));

            _hooks.Add(Win32.Shell.SetWinEventHook(EVENT_OBJECT_REORDER, EVENT_OBJECT_REORDER,
                IntPtr.Zero, _winEventDelegate, 0, 0, WINEVENT_OUTOFCONTEXT));

            _hooks.Add(Win32.Shell.SetWinEventHook(EVENT_OBJECT_NAMECHANGE, EVENT_OBJECT_NAMECHANGE,
                IntPtr.Zero, _winEventDelegate, 0, 0, WINEVENT_OUTOFCONTEXT));

            // Pulse timer: re-apply taskbar every 3 seconds as safety net
            _pulseTimer = new System.Threading.Timer(_ =>
            {
                try
                {
                    // Re-apply to taskbar (most likely to reset)
                    var taskbarHwnd = Win32.Window.FindWindow("Shell_TrayWnd", null);
                    if (taskbarHwnd != IntPtr.Zero)
                        onWindow(taskbarHwnd, "Shell_TrayWnd");

                    Win32.Window.EnumWindows((hWnd, lParam) =>
                    {
                        var sb = new StringBuilder(256);
                        Win32.Window.GetClassName(hWnd, sb, sb.Capacity);
                        string cls = sb.ToString();
                        if (cls == "Shell_SecondaryTrayWnd")
                            onWindow(hWnd, cls);
                        return true;
                    }, IntPtr.Zero);
                }
                catch { }
            }, null, 3000, 3000);
        }

        public static void StopWatcher()
        {
            _pulseTimer?.Dispose();
            _pulseTimer = null;
            _currentCallback = null;

            foreach (var h in _hooks)
                if (h != IntPtr.Zero) Win32.Shell.UnhookWinEvent(h);
            _hooks.Clear();
            _winEventDelegate = null;
        }

        #endregion

        #region Icon & Font

        public static System.Windows.Media.ImageSource? GetIcon(string path)
        {
            try
            {
                if (System.IO.File.Exists(path) || System.IO.Directory.Exists(path))
                {
                    using (var sysIcon = System.Drawing.Icon.ExtractAssociatedIcon(path))
                    {
                        if (sysIcon != null)
                        {
                            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                                sysIcon.Handle,
                                System.Windows.Int32Rect.Empty,
                                System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        private static IntPtr _customFontPtr = IntPtr.Zero;

        public static void InjectCustomFont(IntPtr hwnd)
        {
            if (_customFontPtr == IntPtr.Zero)
            {
                // FW_NORMAL = 400, DEFAULT_CHARSET = 1, CLEARTYPE_QUALITY = 5
                _customFontPtr = Win32.Graphics.CreateFont(16, 0, 0, 0, 400, 0, 0, 0, 1, 0, 0, 5, 0, "JetBrains Mono");
            }

            if (_customFontPtr != IntPtr.Zero)
            {
                const uint WM_SETFONT = 0x0030;
                Win32.Window.SendMessage(hwnd, WM_SETFONT, _customFontPtr, new IntPtr(1));

                Win32.Window.EnumChildWindows(hwnd, (childHwnd, lParam) =>
                {
                    Win32.Window.SendMessage(childHwnd, WM_SETFONT, _customFontPtr, new IntPtr(1));
                    return true;
                }, IntPtr.Zero);
            }
        }

        #endregion

        #region Desktop Layer & Icons

        public static void ForceBottom(IntPtr hwnd)
        {
            Win32.Window.SetWindowPos(hwnd, new IntPtr(1), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0010);
        }

        public static void SetAsDesktopLayer(IntPtr hwnd)
        {
            IntPtr progman = Win32.Window.FindWindow("Progman", null);
            Win32.Window.SendMessage(progman, 0x052C, new IntPtr(0x0000000D), new IntPtr(0));
            Win32.Window.SendMessage(progman, 0x052C, new IntPtr(0x0000000D), new IntPtr(1));

            IntPtr workerw = IntPtr.Zero;
            Win32.Window.EnumWindows((tophandle, topparamhandle) =>
            {
                IntPtr p = Win32.Window.FindWindowEx(tophandle, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (p != IntPtr.Zero)
                {
                    workerw = Win32.Window.FindWindowEx(IntPtr.Zero, tophandle, "WorkerW", null);
                }
                return true;
            }, IntPtr.Zero);

            if (workerw != IntPtr.Zero)
            {
            Win32.Window.SetParent(hwnd, workerw);
            }
        }

        public static void HideDesktopIcons() => SetDesktopIconsVisibility(0);
        public static void ShowDesktopIcons() => SetDesktopIconsVisibility(5); // SW_SHOW

        public static void SetDesktopIconsVisibility(int nCmdShow)
        {
            IntPtr progman = Win32.Window.FindWindow("Progman", null);
            IntPtr shellView = Win32.Window.FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (shellView == IntPtr.Zero)
            {
                Win32.Window.EnumWindows((hwnd, lParam) =>
                {
                    shellView = Win32.Window.FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                    return shellView == IntPtr.Zero;
                }, IntPtr.Zero);
            }

            if (shellView != IntPtr.Zero)
            {
            IntPtr listView = Win32.Window.FindWindowEx(shellView, IntPtr.Zero, "SysListView32", null);
            if (listView != IntPtr.Zero) Win32.Window.ShowWindow(listView, nCmdShow);
            }
        }

        internal static void GetWindowThreadProcessId(nint hwnd, out uint pid)
        {
            pid = 0;
            try {
                Win32.Window.GetWindowThreadProcessId(new IntPtr(hwnd), out pid);
            } catch { pid = 0; }
        }

        #endregion
    }
}
