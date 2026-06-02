using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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

            // Check semua monitors (multi-monitor support)
            // Bukan hanya primary screen seperti sebelumnya
            try
            {
                var screens = System.Windows.Forms.Screen.AllScreens;
                foreach (var screen in screens)
                {
                    // Check apakah window covers seluruh monitor bounds
                    int screenWidth = screen.Bounds.Width;
                    int screenHeight = screen.Bounds.Height;

                    // Window considered fullscreen jika cover area >= 95% dari monitor
                    if (width >= screenWidth * 0.95 && height >= screenHeight * 0.95)
                    {
                        // Additional check: window position harus mendekati screen position
                        int xOffset = Math.Abs(rect.Left - screen.Bounds.Left);
                        int yOffset = Math.Abs(rect.Top - screen.Bounds.Top);

                        // Allow some offset untuk window frame
                        if (xOffset <= 10 && yOffset <= 10)
                        {
                            Console.WriteLine($"[GameDetector] Fullscreen detected on monitor: {screen.DeviceName}");
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameDetector] Error checking multi-monitor fullscreen: {ex.Message}");
            }

            return false;
        }

            // --- Simple poll-based monitor to raise events for fullscreen apps ---
            private static System.Threading.Timer? _pollTimer;
            private static bool _running = false;
            private static bool _hadFullscreen = false;
            private static IntPtr _lastHandle = IntPtr.Zero;
            private static string _lastName = "";

            public static event EventHandler<FullscreenAppEventArgs>? FullscreenAppDetected;
            public static event EventHandler<FullscreenAppEventArgs>? FullscreenAppClosed;
        public static void Start()
            {
                if (_running) return;
                _running = true;
                _pollTimer = new System.Threading.Timer(_ => Poll(), null, 0, 1000);
            }

            public static void Stop()
            {
                _running = false;
                _pollTimer?.Dispose();
                _pollTimer = null;
            }

            private static void Poll()
            {
                try
                {
                    if (IsGameRunning(out var name, out var handle))
                    {
                        if (!_hadFullscreen)
                        {
                            _hadFullscreen = true;
                            _lastHandle = handle;
                            _lastName = name;
                            FullscreenAppDetected?.Invoke(null, new FullscreenAppEventArgs { AppName = name, Handle = handle, ScreenIndex = 0 });
                        }
                    }
                    else
                    {
                        if (_hadFullscreen)
                        {
                            _hadFullscreen = false;
                            FullscreenAppClosed?.Invoke(null, new FullscreenAppEventArgs { AppName = _lastName, Handle = _lastHandle, ScreenIndex = 0 });
                            _lastHandle = IntPtr.Zero;
                            _lastName = "";
                        }
                    }
                }
                catch { }
            }
    }
}
