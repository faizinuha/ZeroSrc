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


        private static void ApplyBlur(IntPtr hwnd)
        {
            var accent = new AccentPolicy();
            // Lighten the tint to fix 'Bold' font look (0x33 instead of 0x66)
            accent.AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND;
            accent.GradientColor = (0x33 << 24) | (0x020202 & 0xFFFFFF); 

            var accentStructSize = Marshal.SizeOf(accent);
            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData();
            data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
            data.SizeOfData = accentStructSize;
            data.Data = accentPtr;

            SetWindowCompositionAttribute(hwnd, ref data);
            Marshal.FreeHGlobal(accentPtr);
        }

        public static void ApplyTaskbarTransparency()
        {
            IntPtr taskbarHwnd = FindWindow("Shell_TrayWnd", null);
            if (taskbarHwnd != IntPtr.Zero) ApplyBlur(taskbarHwnd);

            // For Secondary monitors
            EnumWindows((hWnd, lParam) =>
            {
                StringBuilder className = new StringBuilder(256);
                GetClassName(hWnd, className, className.Capacity);
                if (className.ToString() == "Shell_SecondaryTrayWnd") ApplyBlur(hWnd);
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
                        ApplyCrystalBlur(hWnd);
                        InjectCustomFont(hWnd);
                    }
                }
                return true;
            }, IntPtr.Zero);
        }

        private static void ApplyCrystalBlur(IntPtr hwnd)
        {
            var accent = new AccentPolicy();
            // Windows 12 Style: Transparent center, blurred borders
            accent.AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND;
            // Draw left, top, right, bottom borders with blur (0x20 | 0x40 | 0x80 | 0x100)
            accent.AccentFlags = 0x20 | 0x40 | 0x80 | 0x100;
            // Very light white tint for the border (0x10 alpha to keep it subtle)
            accent.GradientColor = (0x10 << 24) | (0xFFFFFF & 0xFFFFFF); 

            var accentStructSize = Marshal.SizeOf(accent);
            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData();
            data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
            data.SizeOfData = accentStructSize;
            data.Data = accentPtr;

            SetWindowCompositionAttribute(hwnd, ref data);
            Marshal.FreeHGlobal(accentPtr);
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
