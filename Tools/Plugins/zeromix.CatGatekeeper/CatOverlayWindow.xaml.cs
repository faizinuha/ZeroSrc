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

            string pluginDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "Tools", "Plugins", "zeromix.CatGatekeeper", "assets");

            _video1Path = Path.Combine(pluginDir, "neko1.mp4");
            _video2Path = Path.Combine(pluginDir, "neko2.mp4");

            this.PreviewKeyDown += (s, e) => e.Handled = true;
            _keyboardProc = KeyboardHookCallback;
            _mouseProc    = MouseHookCallback;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Install input hooks
            using var proc = System.Diagnostics.Process.GetCurrentProcess();
            var module = proc.MainModule;
            if (module != null)
            {
                _keyboardHookID = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc!, GetModuleHandle(module.ModuleName), 0);
                _mouseHookID    = SetWindowsHookEx(WH_MOUSE_LL,    _mouseProc!,    GetModuleHandle(module.ModuleName), 0);
            }

            // Play video 1 dengan slide-in animation
            if (File.Exists(_video1Path))
            {
                CatVideo.Source = new Uri(_video1Path);
                CatVideo.Play();

                // Slide in dari kanan
                var slideIn = new DoubleAnimation
                {
                    From     = SystemParameters.PrimaryScreenWidth,
                    To       = 0,
                    Duration = TimeSpan.FromSeconds(3),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                VideoTranslate.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, slideIn);
            }

            // Start countdown
            UpdateCountdownText();
            _countdownTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _countdownTimer.Tick += CountdownTick;
            _countdownTimer.Start();
        }

        private void CatVideo_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (_isVideo1Playing && File.Exists(_video2Path))
            {
                _isVideo1Playing = false;
                CatVideo.Source  = new Uri(_video2Path);
                CatVideo.Play();
                // Loop neko2
                CatVideo.MediaEnded -= CatVideo_MediaEnded;
                CatVideo.MediaEnded += (s, ev) =>
                {
                    CatVideo.Position = TimeSpan.Zero;
                    CatVideo.Play();
                };
            }
        }

        private void CountdownTick(object? sender, EventArgs e)
        {
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
            var fadeOut = new DoubleAnimation(1.0, 0.0, TimeSpan.FromSeconds(1));
            fadeOut.Completed += (s, e) => { UnhookAll(); this.Close(); };
            RootGrid.BeginAnimation(OpacityProperty, fadeOut);
        }

        private void UnhookAll()
        {
            if (_keyboardHookID != IntPtr.Zero) { UnhookWindowsHookEx(_keyboardHookID); _keyboardHookID = IntPtr.Zero; }
            if (_mouseHookID    != IntPtr.Zero) { UnhookWindowsHookEx(_mouseHookID);    _mouseHookID    = IntPtr.Zero; }
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
            => (IntPtr)1;

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
            _countdownTimer?.Stop();
            CatVideo.Stop();
            CatVideo.Close();
            UnhookAll();
            base.OnClosed(e);
        }
    }
}
