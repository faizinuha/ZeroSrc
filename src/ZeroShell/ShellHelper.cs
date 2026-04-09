using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Drawing;

namespace ZeroMix.ZeroShell
{
    public static class ShellHelper
    {
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

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
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
            ACCENT_NORMAL = 0
        }

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


        // Dark acrylic — hitam dominan, wallpaper tidak tembus, tidak ikut warna background
        private static void ApplyBlur(IntPtr hwnd, int alpha = 0xCC, int rgb = 0x000000)
        {
            var accent = new AccentPolicy {
                AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
                AccentFlags = 0,
                GradientColor = (alpha << 24) | rgb
            };

            var size = Marshal.SizeOf(accent);
            var ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(accent, ptr, false);

            var data = new WindowCompositionAttributeData {
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = size,
                Data = ptr
            };

            SetWindowCompositionAttribute(hwnd, ref data);
            Marshal.FreeHGlobal(ptr);
        }

        public static void ApplyTaskbarTransparency()
        {
            // Taskbar: paling gelap (alpha 0xDD)
            IntPtr taskbarHwnd = FindWindow("Shell_TrayWnd", null);
            if (taskbarHwnd != IntPtr.Zero) ApplyBlur(taskbarHwnd, 0xDD);

            EnumWindows((hWnd, lParam) =>
            {
                StringBuilder className = new StringBuilder(256);
                GetClassName(hWnd, className, className.Capacity);
                if (className.ToString() == "Shell_SecondaryTrayWnd") ApplyBlur(hWnd, 0xDD);
                return true;
            }, IntPtr.Zero);
        }

        [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr CreateFont(int nHeight, int nWidth, int nEscapement, int nOrientation, int fnWeight, uint fdwItalic, uint fdwUnderline, uint fdwStrikeOut, uint fdwCharSet, uint fdwOutputPrecision, uint fdwClipPrecision, uint fdwQuality, uint fdwPitchAndFamily, string lpszFace);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        private static IntPtr _customFontPtr = IntPtr.Zero;

        public static void InjectCustomFont(IntPtr hwnd)
        {
            if (_customFontPtr == IntPtr.Zero)
            {
                // FW_NORMAL = 400
                // DEFAULT_CHARSET = 1
                // CLEARTYPE_QUALITY = 5
                _customFontPtr = CreateFont(16, 0, 0, 0, 400, 0, 0, 0, 1, 0, 0, 5, 0, "JetBrains Mono");
            }

            if (_customFontPtr != IntPtr.Zero)
            {
                // 0x0030 = WM_SETFONT, 1 = Redraw
                const uint WM_SETFONT = 0x0030;
                
                // Set font to parent window
                SendMessage(hwnd, WM_SETFONT, _customFontPtr, new IntPtr(1));

                // Enum children and set font
                EnumChildWindows(hwnd, (childHwnd, lParam) =>
                {
                    SendMessage(childHwnd, WM_SETFONT, _customFontPtr, new IntPtr(1));
                    return true;
                }, IntPtr.Zero);
            }
        }

        public static void ApplyExplorerTransparency()
        {
            var targetPids = new HashSet<uint>();
            foreach (var p in Process.GetProcesses()) {
                string name = p.ProcessName.ToLower();
                if (name == "explorer") targetPids.Add((uint)p.Id);
            }

            EnumWindows((hWnd, lParam) => {
                uint pid;
                GetWindowThreadProcessId(hWnd, out pid);
                if (targetPids.Contains(pid)) {
                    StringBuilder className = new StringBuilder(256);
                    GetClassName(hWnd, className, className.Capacity);
                    string cls = className.ToString();
                    if (cls == "CabinetWClass" || cls == "ExplorerWClass") {
                        // Explorer: alpha 0xBB — cukup gelap, wallpaper tidak tembus
                        ApplyBlur(hWnd, 0xBB);
                    }
                }
                return true;
            }, IntPtr.Zero);
        }

        /// <summary>
        /// Apply glass/blur ke Start Menu — apply ke semua child windows agar bagian dalam ikut berubah
        /// </summary>
        public static void ApplyStartMenuGlass()
        {
            // Windows 10
            IntPtr startMenu10 = FindWindow("Windows.UI.Core.CoreWindow", "Start");
            if (startMenu10 == IntPtr.Zero)
                startMenu10 = FindWindow("DV2ControlHost", null);
            if (startMenu10 != IntPtr.Zero) {
                ApplyBlur(startMenu10, 0x66);
                ApplyBlurToChildren(startMenu10, 0x66);
            }

            // Windows 11 — StartMenuExperienceHost
            var targetPids = new HashSet<uint>();
            foreach (var p in Process.GetProcesses()) {
                if (p.ProcessName.ToLower() == "startmenuexperiencehost")
                    targetPids.Add((uint)p.Id);
            }

            EnumWindows((hWnd, lParam) => {
                uint pid;
                GetWindowThreadProcessId(hWnd, out pid);
                if (targetPids.Contains(pid)) {
                    ApplyBlur(hWnd, 0x66);
                    ApplyBlurToChildren(hWnd, 0x66);
                }
                return true;
            }, IntPtr.Zero);
        }

        /// <summary>
        /// Apply glass/blur ke Notification Panel (Action Center)
        /// Windows 11: ControlCenterWindow di proses explorer atau ShellExperienceHost
        /// </summary>
        public static void ApplyNotificationPanelGlass()
        {
            var targetPids = new HashSet<uint>();
            foreach (var p in Process.GetProcesses()) {
                string name = p.ProcessName.ToLower();
                // Windows 11 notif panel ada di explorer atau shellexperiencehost
                if (name == "shellexperiencehost" || name == "explorer" || name == "shellhost")
                    targetPids.Add((uint)p.Id);
            }

            EnumWindows((hWnd, lParam) => {
                uint pid;
                GetWindowThreadProcessId(hWnd, out pid);
                if (!targetPids.Contains(pid)) return true;

                StringBuilder cls = new StringBuilder(256);
                GetClassName(hWnd, cls, cls.Capacity);
                string clsName = cls.ToString();

                // Windows 11: ControlCenterWindow, Windows 10: ActionCenter
                if (clsName.Contains("ControlCenter") || clsName.Contains("ActionCenter") ||
                    clsName.Contains("NotifyIcon") || clsName == "Windows.UI.Core.CoreWindow") {
                    ApplyBlur(hWnd, 0x66);
                    ApplyBlurToChildren(hWnd, 0x66);
                }
                return true;
            }, IntPtr.Zero);
        }

        // Apply blur ke semua child window langsung (agar bagian dalam ikut berubah)
        private static void ApplyBlurToChildren(IntPtr parent, int alpha)
        {
            EnumChildWindows(parent, (child, lParam) => {
                ApplyBlur(child, alpha);
                return true;
            }, IntPtr.Zero);
        }

        /// <summary>
        /// Restore semua WDM ke normal — disable accent policy
        /// </summary>
        public static void RestoreAllWDM()
        {
            // Restore Taskbar
            IntPtr taskbarHwnd = FindWindow("Shell_TrayWnd", null);
            if (taskbarHwnd != IntPtr.Zero) DisableAccent(taskbarHwnd);

            EnumWindows((hWnd, lParam) => {
                StringBuilder className = new StringBuilder(256);
                GetClassName(hWnd, className, className.Capacity);
                string cls = className.ToString();
                if (cls == "Shell_SecondaryTrayWnd" || cls == "CabinetWClass" || cls == "ExplorerWClass")
                    DisableAccent(hWnd);
                return true;
            }, IntPtr.Zero);
        }

        private static void DisableAccent(IntPtr hwnd)
        {
            var accent = new AccentPolicy { AccentState = AccentState.ACCENT_DISABLED };
            var size = Marshal.SizeOf(accent);
            var ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(accent, ptr, false);
            var data = new WindowCompositionAttributeData {
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = size,
                Data = ptr
            };
            SetWindowCompositionAttribute(hwnd, ref data);
            Marshal.FreeHGlobal(ptr);
        }


        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll")]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public static void ForceBottom(IntPtr hwnd)
        {
            SetWindowPos(hwnd, new IntPtr(1), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0010);
        }

        public static void SetAsDesktopLayer(IntPtr hwnd)
        {
            IntPtr progman = FindWindow("Progman", null);
            SendMessage(progman, 0x052C, new IntPtr(0x0000000D), new IntPtr(0));
            SendMessage(progman, 0x052C, new IntPtr(0x0000000D), new IntPtr(1));

            IntPtr workerw = IntPtr.Zero;
            EnumWindows((tophandle, topparamhandle) =>
            {
                IntPtr p = FindWindowEx(tophandle, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (p != IntPtr.Zero)
                {
                    workerw = FindWindowEx(IntPtr.Zero, tophandle, "WorkerW", null);
                }
                return true;
            }, IntPtr.Zero);

            if (workerw != IntPtr.Zero)
            {
                SetParent(hwnd, workerw);
            }
        }

        public static void HideDesktopIcons() => SetDesktopIconsVisibility(0);
        public static void ShowDesktopIcons() => SetDesktopIconsVisibility(5); // SW_SHOW

        private static void SetDesktopIconsVisibility(int nCmdShow)
        {
            IntPtr progman = FindWindow("Progman", null);
            IntPtr shellView = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (shellView == IntPtr.Zero)
            {
                EnumWindows((hwnd, lParam) =>
                {
                    shellView = FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                    return shellView == IntPtr.Zero;
                }, IntPtr.Zero);
            }

            if (shellView != IntPtr.Zero)
            {
                IntPtr listView = FindWindowEx(shellView, IntPtr.Zero, "SysListView32", null);
                if (listView != IntPtr.Zero) ShowWindow(listView, nCmdShow);
            }
        }

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    }
}
