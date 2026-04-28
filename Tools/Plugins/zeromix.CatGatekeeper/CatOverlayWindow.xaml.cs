using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
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
        
        // Low-level keyboard hook
        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;
        private IntPtr _keyboardHookID = IntPtr.Zero;
        private IntPtr _mouseHookID = IntPtr.Zero;
        private LowLevelKeyboardProc? _keyboardProc;
        private LowLevelMouseProc? _mouseProc;
        
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, Delegate lpfn, IntPtr hMod, uint dwThreadId);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        
        public CatOverlayWindow(int breakMinutes)
        {
            InitializeComponent();
            
            _remainingSeconds = breakMinutes * 60;
            
            // Get video paths
            string pluginDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", "Plugins", "zeromix.CatGatekeeper");
            _video1Path = Path.Combine(pluginDir, "assets", "neko1.webm");
            _video2Path = Path.Combine(pluginDir, "assets", "neko2.webm");
            
            // Block Alt+F4, Alt+Tab, Windows key
            this.PreviewKeyDown += (s, e) => e.Handled = true;
            
            // Install hooks to block all input
            _keyboardProc = KeyboardHookCallback;
            _mouseProc = MouseHookCallback;
        }
        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Install input hooks
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                if (curModule != null)
                {
                    _keyboardHookID = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc!, GetModuleHandle(curModule.ModuleName), 0);
                    _mouseHookID = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc!, GetModuleHandle(curModule.ModuleName), 0);
                }
            }
            
            // Start video 1 (slide in animation)
            if (File.Exists(_video1Path))
            {
                CatVideo.Source = new Uri(_video1Path);
                CatVideo.Play();
                
                // Slide in animation
                var slideIn = new DoubleAnimation
                {
                    From = SystemParameters.PrimaryScreenWidth,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(3),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                CatVideo.RenderTransform = new System.Windows.Media.TranslateTransform();
                CatVideo.RenderTransform.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, slideIn);
            }
            
            // Start countdown
            _countdownTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _countdownTimer.Tick += CountdownTick;
            _countdownTimer.Start();
            UpdateCountdownText();
        }
        
        private void CatVideo_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (_isVideo1Playing && File.Exists(_video2Path))
            {
                // Switch to sleeping video (loop)
                _isVideo1Playing = false;
                CatVideo.Source = new Uri(_video2Path);
                CatVideo.Play();
                CatVideo.MediaEnded -= CatVideo_MediaEnded;
                CatVideo.MediaEnded += (s, ev) => CatVideo.Position = TimeSpan.Zero; // Loop
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
            int minutes = _remainingSeconds / 60;
            int seconds = _remainingSeconds % 60;
            CountdownText.Text = $" {minutes}:{seconds:D2} ";
        }
        
        private void CloseWithFade()
        {
            var fadeOut = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromSeconds(1)
            };
            fadeOut.Completed += (s, e) =>
            {
                // Unhook before close
                if (_keyboardHookID != IntPtr.Zero) UnhookWindowsHookEx(_keyboardHookID);
                if (_mouseHookID != IntPtr.Zero) UnhookWindowsHookEx(_mouseHookID);
                this.Close();
            };
            RootGrid.BeginAnimation(OpacityProperty, fadeOut);
        }
        
        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            // Block all keyboard input
            return (IntPtr)1;
        }
        
        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            // Block all mouse clicks (but allow movement for countdown visibility)
            const int WM_LBUTTONDOWN = 0x0201;
            const int WM_RBUTTONDOWN = 0x0204;
            const int WM_MBUTTONDOWN = 0x0207;
            
            int msg = wParam.ToInt32();
            if (msg == WM_LBUTTONDOWN || msg == WM_RBUTTONDOWN || msg == WM_MBUTTONDOWN)
            {
                return (IntPtr)1; // Block clicks
            }
            
            return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
        }
        
        protected override void OnClosed(EventArgs e)
        {
            _countdownTimer?.Stop();
            CatVideo.Stop();
            CatVideo.Close();
            
            if (_keyboardHookID != IntPtr.Zero) UnhookWindowsHookEx(_keyboardHookID);
            if (_mouseHookID != IntPtr.Zero) UnhookWindowsHookEx(_mouseHookID);
            
            base.OnClosed(e);
        }
    }
}
