using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ZeroMix
{
    public partial class VideoWallpaperWindow : Window
    {
        private string? _videoPath;

        public VideoWallpaperWindow(string videoPath)
        {
            InitializeComponent();
            _videoPath = videoPath;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== VideoWallpaperWindow.Window_Loaded ===");
                
                // Set window to fullscreen
                this.WindowState = WindowState.Maximized;
                this.Width = SystemParameters.PrimaryScreenWidth;
                this.Height = SystemParameters.PrimaryScreenHeight;
                this.Left = 0;
                this.Top = 0;

                // IMPORTANT: Send to background BEFORE loading video
                SendWindowToBackground();
                
                // Hide from Alt+Tab
                HideFromAltTab();

                // Small delay to ensure window is in correct position
                System.Threading.Thread.Sleep(100);

                // Load and play video
                if (!string.IsNullOrEmpty(_videoPath) && System.IO.File.Exists(_videoPath))
                {
                    VideoPlayer.Source = new Uri(_videoPath);
                    VideoPlayer.Play();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading video wallpaper: {ex.Message}");
            }
        }

        private void VideoPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            // Loop video
            VideoPlayer.Position = TimeSpan.Zero;
            VideoPlayer.Play();
        }

        private void VideoPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"MediaElement failed: {e.ErrorException?.Message}");
        }

        private void HideFromAltTab()
        {
            try
            {
                IntPtr windowHandle = new WindowInteropHelper(this).Handle;
                
                // Get current window style
                int exStyle = NativeMethods.GetWindowLong(windowHandle, NativeMethods.GWL_EXSTYLE);
                
                // Add WS_EX_TOOLWINDOW to hide from Alt+Tab
                // Add WS_EX_TRANSPARENT to allow mouse clicks to pass through
                exStyle |= NativeMethods.WS_EX_TOOLWINDOW;
                exStyle |= NativeMethods.WS_EX_TRANSPARENT;
                
                // Set new style
                NativeMethods.SetWindowLong(windowHandle, NativeMethods.GWL_EXSTYLE, exStyle);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error hiding from Alt+Tab: {ex.Message}");
            }
        }

        private void SendWindowToBackground()
        {
            try
            {
                // Get window handle
                IntPtr windowHandle = new WindowInteropHelper(this).Handle;
                
                // Method 1: Try to find existing WorkerW
                IntPtr progman = NativeMethods.FindWindow("Progman", null);
                
                // Send message to create WorkerW if it doesn't exist
                IntPtr result = IntPtr.Zero;
                NativeMethods.SendMessageTimeout(
                    progman,
                    0x052C,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    NativeMethods.SendMessageTimeoutFlags.SMTO_NORMAL,
                    1000,
                    out result);

                // Find the WorkerW window that sits between Progman and desktop icons
                IntPtr workerw = IntPtr.Zero;
                
                NativeMethods.EnumWindows((tophandle, topparamhandle) =>
                {
                    IntPtr p = NativeMethods.FindWindowEx(
                        tophandle,
                        IntPtr.Zero,
                        "SHELLDLL_DefView",
                        IntPtr.Zero);

                    if (p != IntPtr.Zero)
                    {
                        // Found the WorkerW window that contains SHELLDLL_DefView
                        workerw = NativeMethods.FindWindowEx(
                            IntPtr.Zero,
                            tophandle,
                            "WorkerW",
                            IntPtr.Zero);
                    }

                    return true;
                }, IntPtr.Zero);

                // If WorkerW found, set our window as its child
                if (workerw != IntPtr.Zero)
                {
                    NativeMethods.SetParent(windowHandle, workerw);
                }
                else
                {
                    // Fallback: Set as child of Progman directly
                    NativeMethods.SetParent(windowHandle, progman);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending window to background: {ex.Message}");
            }
        }

        // Native methods for window manipulation
        private static class NativeMethods
        {
            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, IntPtr windowTitle);

            [DllImport("user32.dll")]
            public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

            public const int GWL_EXSTYLE = -20;
            public const int WS_EX_TOOLWINDOW = 0x00000080;
            public const int WS_EX_TRANSPARENT = 0x00000020;

            [DllImport("user32.dll")]
            public static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

            public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr SendMessageTimeout(
                IntPtr hWnd,
                uint Msg,
                IntPtr wParam,
                IntPtr lParam,
                SendMessageTimeoutFlags fuFlags,
                uint uTimeout,
                out IntPtr lpdwResult);

            [Flags]
            public enum SendMessageTimeoutFlags : uint
            {
                SMTO_NORMAL = 0x0,
                SMTO_BLOCK = 0x1,
                SMTO_ABORTIFHUNG = 0x2,
                SMTO_NOTIMEOUTIFNOTHUNG = 0x8
            }
        }
    }
}
