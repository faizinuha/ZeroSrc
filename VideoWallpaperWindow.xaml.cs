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

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            // 1. Set style awal
            SetWindowStyles();
            
            // 2. Monitor state changes (untuk handle Win+D)
            this.StateChanged += Window_StateChanged;
        }

        private void Window_StateChanged(object? sender, EventArgs e)
        {
            // Jika window di-minimize (misal karena Win+D), restore kembali
            if (this.WindowState == WindowState.Minimized)
            {
                System.Diagnostics.Debug.WriteLine("Window minimized, restoring...");
                this.WindowState = WindowState.Maximized;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== VideoWallpaperWindow.Window_Loaded ===");

                // 2. Pindahkan ke Background (WorkerW)
                SendWindowToBackground();

                // 3. PAKSA UKURAN FULLSCREEN SETELAH PINDAH
                // Ini kunci perbaikan fullscreen yang gagal sebelumnya
                ForceFullScreen();

                // 4. Load dan Play Video
                if (!string.IsNullOrEmpty(_videoPath) && System.IO.File.Exists(_videoPath))
                {
                    VideoPlayer.Source = new Uri(_videoPath);
                    VideoPlayer.Play();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
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
            var hwnd = new WindowInteropHelper(this).Handle;
            
            // Style: ToolWindow (hide alt-tab), NoActivate, dan TRANSPARENT (kunci untuk icons & klik kanan!)
            int exStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
            exStyle |= NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_TRANSPARENT;
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, exStyle);
            
            System.Diagnostics.Debug.WriteLine("Window styles set: TOOLWINDOW | NOACTIVATE | TRANSPARENT");
        }

        private void SendWindowToBackground()
        {
            IntPtr progman = NativeMethods.FindWindow("Progman", null);
            IntPtr result = IntPtr.Zero;

            // Kirim pesan ke Progman untuk spawn WorkerW
            NativeMethods.SendMessageTimeout(progman, 
                0x052C, 
                new IntPtr(0), 
                IntPtr.Zero, 
                NativeMethods.SendMessageTimeoutFlags.SMTO_NORMAL, 
                1000, 
                out result);

            IntPtr workerw = IntPtr.Zero;

            // Cari WorkerW yang benar (yang ada di belakang SHELLDLL_DefView)
            NativeMethods.EnumWindows((tophandle, topparamhandle) =>
            {
                IntPtr p = NativeMethods.FindWindowEx(tophandle, IntPtr.Zero, "SHELLDLL_DefView", IntPtr.Zero);

                if (p != IntPtr.Zero)
                {
                    // WorkerW adalah sibling dari SHELLDLL_DefView yang kita cari
                    workerw = NativeMethods.FindWindowEx(IntPtr.Zero, tophandle, "WorkerW", IntPtr.Zero);
                }
                return true;
            }, IntPtr.Zero);

            IntPtr windowHandle = new WindowInteropHelper(this).Handle;

            // Jika WorkerW ketemu, tempel ke sana. Jika tidak, tempel ke Progman.
            IntPtr parent = (workerw != IntPtr.Zero) ? workerw : progman;
            NativeMethods.SetParent(windowHandle, parent);
            
            // PENTING: Paksa window kita ke layer paling bawah agar tidak menutupi icons
            // HWND_BOTTOM = 1
            NativeMethods.SetWindowPos(windowHandle, new IntPtr(1), 0, 0, 0, 0, 
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
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

            public const int GWL_EXSTYLE = -20;
            public const int WS_EX_TOOLWINDOW = 0x00000080;
            public const int WS_EX_NOACTIVATE = 0x08000000;
            public const int WS_EX_TRANSPARENT = 0x00000020;
            
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
