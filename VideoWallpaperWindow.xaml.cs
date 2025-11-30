using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Collections.Generic;

namespace ZeroMix
{
    public partial class VideoWallpaperWindow : Window
    {
        private string? _videoPath;
        private IntPtr _windowHandle;
        private HwndSource? _hwndSource;
        private const int WM_HOTKEY = 0x0312;
        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MINIMIZE = 0xF020;

        public VideoWallpaperWindow(string videoPath)
        {
            InitializeComponent();
            _videoPath = videoPath;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            _windowHandle = new WindowInteropHelper(this).Handle;
            _hwndSource = HwndSource.FromHwnd(_windowHandle);
            _hwndSource?.AddHook(WndProc);
            
            // Monitor state changes
            this.StateChanged += Window_StateChanged;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // Cegah minimize command (termasuk dari Win+D)
            if (msg == WM_SYSCOMMAND && (int)wParam == SC_MINIMIZE)
            {
                System.Diagnostics.Debug.WriteLine("Minimize command intercepted! Ignoring...");
                handled = true;
                return IntPtr.Zero;
            }

            return IntPtr.Zero;
        }

        private void Window_StateChanged(object? sender, EventArgs e)
        {
            // Safety check: jika somehow minimize terjadi, restore
            if (this.WindowState == WindowState.Minimized)
            {
                System.Diagnostics.Debug.WriteLine("Window minimized detected, restoring immediately...");
                this.WindowState = WindowState.Maximized;
            }
        }

        private void RegisterForShellEvents()
        {
            try
            {
                // Hook ke shell untuk monitor desktop events
                NativeMethods.RegisterShellHookWindow(_windowHandle);
                System.Diagnostics.Debug.WriteLine("Shell hook registered successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to register shell hook: {ex.Message}");
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== VideoWallpaperWindow.Window_Loaded ===");

                // 1. PENTING: Set window styles SEBELUM move ke background
                SetWindowStyles();

                // 2. Register shell hook untuk monitor desktop events
                RegisterForShellEvents();

                // 3. Pindahkan ke Background (WorkerW)
                SendWindowToBackground();

                // 4. PAKSA UKURAN FULLSCREEN SETELAH PINDAH
                ForceFullScreen();

                // 5. Load dan Play Video
                if (!string.IsNullOrEmpty(_videoPath) && System.IO.File.Exists(_videoPath))
                {
                    VideoPlayer.Source = new Uri(_videoPath);
                    VideoPlayer.Play();
                    System.Diagnostics.Debug.WriteLine($"Video started playing: {_videoPath}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Video file not found or invalid: {_videoPath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Window_Loaded: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void ForceFullScreen()
        {
            try
            {
                // Ambil ukuran layar fisik menggunakan SystemParameters
                double screenWidth = SystemParameters.PrimaryScreenWidth;
                double screenHeight = SystemParameters.PrimaryScreenHeight;

                this.WindowState = WindowState.Normal; // Reset dulu
                this.Left = 0;
                this.Top = 0;
                this.Width = screenWidth;
                this.Height = screenHeight;
                this.WindowState = WindowState.Maximized; // Maximize lagi

                // Update layout
                this.UpdateLayout();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error forcing fullscreen: {ex.Message}");
            }
        }

        private void VideoPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            VideoPlayer.Position = TimeSpan.Zero;
            VideoPlayer.Play();
        }

        private void VideoPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Media failed: {e.ErrorException?.Message}");
        }

        private void SetWindowStyles()
        {
            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                
                if (hwnd == IntPtr.Zero)
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: Window handle is zero!");
                    return;
                }

                // Get current extended style
                int exStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
                System.Diagnostics.Debug.WriteLine($"Current exStyle: 0x{exStyle:X8}");
                
                // Remove transparent flag agar icons & klik kanan bisa tembus
                exStyle &= ~NativeMethods.WS_EX_TRANSPARENT;
                
                // Add flags: TOOLWINDOW (hide from alt-tab), NOACTIVATE, LAYERED
                exStyle |= NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_LAYERED;
                
                // Apply new style
                int result = NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, exStyle);
                System.Diagnostics.Debug.WriteLine($"SetWindowLong result: {result}, New exStyle: 0x{exStyle:X8}");
                
                // Set window transparency dengan alpha blend
                // LWA_ALPHA = 0x00000002, alpha value = 200 (out of 255) = ~78% opaque
                bool alphaResult = NativeMethods.SetLayeredWindowAttributes(hwnd, 0, 200, 0x00000002);
                System.Diagnostics.Debug.WriteLine($"SetLayeredWindowAttributes result: {alphaResult}");
                
                System.Diagnostics.Debug.WriteLine("✓ Window styles successfully applied: TOOLWINDOW | NOACTIVATE | LAYERED with alpha");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR in SetWindowStyles: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void SendWindowToBackground()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== SendWindowToBackground START ===");
                
                IntPtr progman = NativeMethods.FindWindow("Progman", null);
                System.Diagnostics.Debug.WriteLine($"Progman window found: {(progman != IntPtr.Zero ? "YES" : "NO")}");
                
                if (progman == IntPtr.Zero)
                {
                    System.Diagnostics.Debug.WriteLine("WARNING: Progman not found!");
                    return;
                }

                IntPtr result = IntPtr.Zero;

                // Kirim pesan ke Progman untuk spawn WorkerW
                NativeMethods.SendMessageTimeout(progman, 
                    0x052C, 
                    new IntPtr(0), 
                    IntPtr.Zero, 
                    NativeMethods.SendMessageTimeoutFlags.SMTO_NORMAL, 
                    1000, 
                    out result);
                System.Diagnostics.Debug.WriteLine("Message sent to Progman for WorkerW spawn");

                IntPtr workerw = IntPtr.Zero;

                // Cari WorkerW yang benar (yang ada di belakang SHELLDLL_DefView)
                NativeMethods.EnumWindows((tophandle, topparamhandle) =>
                {
                    IntPtr p = NativeMethods.FindWindowEx(tophandle, IntPtr.Zero, "SHELLDLL_DefView", IntPtr.Zero);

                    if (p != IntPtr.Zero)
                    {
                        // WorkerW adalah sibling dari SHELLDLL_DefView yang kita cari
                        workerw = NativeMethods.FindWindowEx(IntPtr.Zero, tophandle, "WorkerW", IntPtr.Zero);
                        System.Diagnostics.Debug.WriteLine($"WorkerW found: {(workerw != IntPtr.Zero ? "YES" : "NO")}");
                    }
                    return true;
                }, IntPtr.Zero);

                IntPtr windowHandle = new WindowInteropHelper(this).Handle;

                // Jika WorkerW ketemu, tempel ke sana. Jika tidak, tempel ke Progman.
                IntPtr parent = (workerw != IntPtr.Zero) ? workerw : progman;
                string parentName = (workerw != IntPtr.Zero) ? "WorkerW" : "Progman";
                System.Diagnostics.Debug.WriteLine($"Parent window: {parentName}");
                
                NativeMethods.SetParent(windowHandle, parent);
                System.Diagnostics.Debug.WriteLine("Window parented successfully");
                
                // PENTING: Paksa window kita ke layer paling bawah agar tidak menutupi icons
                // HWND_BOTTOM = 1
                bool posResult = NativeMethods.SetWindowPos(windowHandle, new IntPtr(1), 0, 0, 0, 0, 
                    NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
                System.Diagnostics.Debug.WriteLine($"SetWindowPos result: {posResult}");
                
                System.Diagnostics.Debug.WriteLine("✓ SendWindowToBackground completed successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR in SendWindowToBackground: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static class NativeMethods
        {
            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, IntPtr windowTitle);

            [DllImport("user32.dll")]
            public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

            [DllImport("user32.dll")]
            public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

            [DllImport("user32.dll")]
            public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

            [DllImport("user32.dll")]
            public static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
            public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

            [DllImport("user32.dll")]
            public static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, SendMessageTimeoutFlags fuFlags, uint uTimeout, out IntPtr lpdwResult);

            [DllImport("user32.dll")]
            public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

            [DllImport("user32.dll")]
            public static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool RegisterShellHookWindow(IntPtr hWnd);

            public const int GWL_EXSTYLE = -20;
            public const int WS_EX_TOOLWINDOW = 0x00000080;
            public const int WS_EX_NOACTIVATE = 0x08000000;
            public const int WS_EX_TRANSPARENT = 0x00000020;
            public const int WS_EX_LAYERED = 0x00080000;
            
            // Flags untuk SetWindowPos
            public const uint SWP_NOSIZE = 0x0001;
            public const uint SWP_NOMOVE = 0x0002;
            public const uint SWP_NOACTIVATE = 0x0010;

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
