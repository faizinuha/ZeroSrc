using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ZeroMix.Native
{
    /// <summary>
    /// Centralized Win32 P/Invoke declarations organized by category.
    /// Single source of truth for all Windows API interop.
    /// Replaces duplicated declarations across StartMenuInterceptor and ShellHelper.
    /// </summary>
    public static class Win32
    {
        #region Window Management

        public static class Window
        {
            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

            [DllImport("user32.dll")]
            public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

            [DllImport("user32.dll")]
            public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

            [DllImport("user32.dll")]
            public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

            [DllImport("user32.dll")]
            public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

            [DllImport("user32.dll")]
            public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

            [DllImport("user32.dll")]
            public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

            [StructLayout(LayoutKind.Sequential)]
            public struct RECT { public int Left, Top, Right, Bottom; }

            public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        }

        #endregion

        #region Hook Management (Start Menu Interception)

        public static class Shell
        {
            public delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool UnhookWindowsHookEx(IntPtr hhk);

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern IntPtr GetModuleHandle(string lpModuleName);

            [DllImport("user32.dll")]
            public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax,
                IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc,
                uint idProcess, uint idThread, uint dwFlags);

            [DllImport("user32.dll")]
            public static extern bool UnhookWinEvent(IntPtr hWinEventHook);

            public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
                int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

            // Hook constants
            public const int WH_MOUSE_LL = 14;
            public const int WM_LBUTTONDOWN = 0x0201;
        }

        #endregion

        #region DWM (Desktop Window Manager)

        public static class DWM
        {
            [DllImport("dwmapi.dll")]
            public static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

            [DllImport("user32.dll")]
            public static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

            [StructLayout(LayoutKind.Sequential)]
            public struct WindowCompositionAttributeData
            {
                public WindowCompositionAttribute Attribute;
                public IntPtr Data;
                public int SizeOfData;
            }

            public enum WindowCompositionAttribute
            {
                WCA_ACCENT_POLICY = 19
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct AccentPolicy
            {
                public AccentState AccentState;
                public int AccentFlags;
                public int GradientColor;
                public int AnimationId;
            }

            public enum AccentState
            {
                ACCENT_DISABLED = 0,
                ACCENT_ENABLE_GRADIENT = 1,
                ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
                ACCENT_ENABLE_BLURBEHIND = 3,
                ACCENT_ENABLE_ACRYLICBLURBEHIND = 4
            }
        }

        #endregion

        #region Graphics (GDI+)

        public static class Graphics
        {
            [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
            public static extern IntPtr CreateFont(int nHeight, int nWidth, int nEscapement, int nOrientation, int fnWeight,
                uint fdwItalic, uint fdwUnderline, uint fdwStrikeOut, uint fdwCharSet, uint fdwOutputPrecision,
                uint fdwClipPrecision, uint fdwQuality, uint fdwPitchAndFamily, string lpszFace);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Apply acrylic blur effect to window.
        /// </summary>
        public static void ApplyBlur(IntPtr hwnd, int alpha = 0xCC, int rgb = 0x000000)
        {
            var accent = new DWM.AccentPolicy
            {
                AccentState = DWM.AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
                AccentFlags = 0,
                GradientColor = (alpha << 24) | rgb
            };

            var size = Marshal.SizeOf(accent);
            var ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(accent, ptr, false);

            var data = new DWM.WindowCompositionAttributeData
            {
                Attribute = DWM.WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = size,
                Data = ptr
            };

            DWM.SetWindowCompositionAttribute(hwnd, ref data);
            Marshal.FreeHGlobal(ptr);
        }

        /// <summary>
        /// Apply blur effect only (no acrylic).
        /// </summary>
        public static void ApplyBlurOnly(IntPtr hwnd)
        {
            var accent = new DWM.AccentPolicy { AccentState = DWM.AccentState.ACCENT_ENABLE_BLURBEHIND };
            var size = Marshal.SizeOf(accent);
            var ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(accent, ptr, false);
            var data = new DWM.WindowCompositionAttributeData
            {
                Attribute = DWM.WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = size,
                Data = ptr
            };
            DWM.SetWindowCompositionAttribute(hwnd, ref data);
            Marshal.FreeHGlobal(ptr);
        }

        #endregion
    }
}
