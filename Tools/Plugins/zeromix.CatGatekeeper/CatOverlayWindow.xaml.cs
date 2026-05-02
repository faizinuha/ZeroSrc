using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace zeromix.CatGatekeeper
{
    public partial class CatOverlayWindow : Window
    {
        private int _remainingSeconds;
        private DispatcherTimer? _countdownTimer;
        private string _video1Path;
        private string _video2Path;
        private bool _isVideo1Playing = true;
        private bool _isClosed = false;

        // Low-level hooks untuk block input
        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL    = 14;
        private IntPtr _keyboardHookID   = IntPtr.Zero;
        private IntPtr _mouseHookID      = IntPtr.Zero;
        private LowLevelKeyboardProc? _keyboardProc;
        private LowLevelMouseProc?    _mouseProc;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")] private static extern IntPtr SetWindowsHookEx(int idHook, Delegate lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll")] private static extern IntPtr GetModuleHandle(string lpModuleName);

        public CatOverlayWindow(int breakMinutes)
        {
            InitializeComponent();

            _remainingSeconds = breakMinutes * 60;

            string assetsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "Tools", "Plugins", "zeromix.CatGatekeeper", "assets");

            _video1Path = Path.Combine(assetsDir, "neko1.mp4");
            _video2Path = Path.Combine(assetsDir, "neko2.mp4");

            this.PreviewKeyDown += (s, e) => e.Handled = true;
            _keyboardProc = KeyboardHookCallback;
            _mouseProc    = MouseHookCallback;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Install input hooks
            try
            {
                using var proc = System.Diagnostics.Process.GetCurrentProcess();
                var module = proc.MainModule;
                if (module != null)
                {
                    _keyboardHookID = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc!, GetModuleHandle(module.ModuleName), 0);
                    _mouseHookID    = SetWindowsHookEx(WH_MOUSE_LL,    _mouseProc!,    GetModuleHandle(module.ModuleName), 0);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CatGatekeeper] Hook error: {ex.Message}");
            }

            // Update countdown dulu sebelum timer start
            UpdateCountdownText();

            // Play video 1 — slide in dari kanan seperti Chrome extension
            if (File.Exists(_video1Path))
            {
                CatVideo.Source = new Uri(_video1Path);
                CatVideo.Play();

                // Slide in dari kanan ke posisi normal
                var slideIn = new DoubleAnimation
                {
                    From           = SystemParameters.PrimaryScreenWidth,
                    To             = 0,
                    Duration       = TimeSpan.FromSeconds(2.5),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                VideoTranslate.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, slideIn);
            }
            else
            {
                Console.WriteLine($"[CatGatekeeper] Video not found: {_video1Path}");
            }

            // Start countdown timer
            _countdownTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _countdownTimer.Tick += CountdownTick;
            _countdownTimer.Start();
        }

        private void CatVideo_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (_isClosed) return;

            if (_isVideo1Playing)
            {
                _isVideo1Playing = false;

                if (File.Exists(_video2Path))
                {
                    // Switch ke neko2 (sleeping loop)
                    CatVideo.Source = new Uri(_video2Path);
                    CatVideo.Play();

                    // Loop neko2
                    CatVideo.MediaEnded -= CatVideo_MediaEnded;
                    CatVideo.MediaEnded += LoopVideo2;
                }
            }
        }

        private void LoopVideo2(object sender, RoutedEventArgs e)
        {
            if (_isClosed) return;
            CatVideo.Position = TimeSpan.Zero;
            CatVideo.Play();
        }

        private void CountdownTick(object? sender, EventArgs e)
        {
            if (_isClosed) return;

            _remainingSeconds--;
            UpdateCountdownText();

            if (_remainingSeconds <= 0)
            {
                _countdownTimer?.Stop();
                CloseWithFade();
            }
        }

        private void UpdateCountdownText()
        {
            int m = _remainingSeconds / 60;
            int s = _remainingSeconds % 60;
            CountdownText.Text = $"{m}:{s:D2}";
        }

        private void CloseWithFade()
        {
            if (_isClosed) return;
            _isClosed = true;

            // Slide out kucing ke kanan
            var slideOut = new DoubleAnimation
            {
                From     = 0,
                To       = SystemParameters.PrimaryScreenWidth,
                Duration = TimeSpan.FromSeconds(1.5),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            // Fade out countdown
            var fadeOut = new DoubleAnimation(1.0, 0.0, TimeSpan.FromSeconds(1));
            fadeOut.Completed += (s, e) =>
            {
                UnhookAll();
                this.Close();
            };

            VideoTranslate.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, slideOut);
            RootGrid.BeginAnimation(OpacityProperty, fadeOut);
        }

        private void UnhookAll()
        {
            if (_keyboardHookID != IntPtr.Zero) { UnhookWindowsHookEx(_keyboardHookID); _keyboardHookID = IntPtr.Zero; }
            if (_mouseHookID    != IntPtr.Zero) { UnhookWindowsHookEx(_mouseHookID);    _mouseHookID    = IntPtr.Zero; }
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
            => (IntPtr)1; // Block semua keyboard

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            const int WM_LBUTTONDOWN = 0x0201;
            const int WM_RBUTTONDOWN = 0x0204;
            const int WM_MBUTTONDOWN = 0x0207;
            int msg = wParam.ToInt32();
            if (msg == WM_LBUTTONDOWN || msg == WM_RBUTTONDOWN || msg == WM_MBUTTONDOWN)
                return (IntPtr)1;
            return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
        }

        protected override void OnClosed(EventArgs e)
        {
            _isClosed = true;
            _countdownTimer?.Stop();
            try { CatVideo.Stop(); CatVideo.Close(); } catch { }
            UnhookAll();
            base.OnClosed(e);
        }
    }
}
