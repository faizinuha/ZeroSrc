using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace ZeroMix.Recorder
{
    public class GameDetector
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT_WIN lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT_WIN
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private static readonly string[] GameProcessNames = {
            "GenshinImpact", "StarRail", "ZenlessZoneZero", "Cyberpunk2077", "EldenRing", 
            "VALORANT-Win64-Shipping", "Overwatch", "FortniteClient-Win64-Shipping",
            "League of Legends", "Dota2", "Minecraft", "RobloxPlayerBeta",
            "Steam", "steamwebhelper", "EpicGamesLauncher"
        };

        public static bool IsGameRunning(out string gameName, out IntPtr handle)
        {
            gameName = "Unknown Game";
            handle = IntPtr.Zero;

            IntPtr fgWindow = GetForegroundWindow();
            if (fgWindow == IntPtr.Zero) return false;

            GetWindowThreadProcessId(fgWindow, out int pid);
            try
            {
                var process = Process.GetProcessById(pid);
                string procName = process.ProcessName;

                // 1. Check by Process Name List
                if (GameProcessNames.Any(n => procName.Contains(n, StringComparison.OrdinalIgnoreCase)))
                {
                    gameName = process.MainWindowTitle;
                    handle = fgWindow;
                    return true;
                }

                // 2. Check by Fullscreen Status (Roughly)
                if (IsProbablyFullscreen(fgWindow))
                {
                    gameName = process.MainWindowTitle;
                    handle = fgWindow;
                    return true;
                }
            }
            catch { }

            return false;
        }

        private static bool IsProbablyFullscreen(IntPtr hWnd)
        {
            if (!GetWindowRect(hWnd, out RECT_WIN rect)) return false;

            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            // Check if window matches screen resolution
            int screenWidth = (int)System.Windows.SystemParameters.PrimaryScreenWidth;
            int screenHeight = (int)System.Windows.SystemParameters.PrimaryScreenHeight;

            return width >= screenWidth && height >= screenHeight;
        }
    }
}
