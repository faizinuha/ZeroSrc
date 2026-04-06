using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Win32;

namespace ZeroMix.SleepMode
{
    public partial class SleepOverlayWindow : Window
    {
        private DispatcherTimer _animationTimer;
        private bool _isClosing = false;
        private Random _random = new Random();
        private double _pulseDir = 1;
        private double _currentPulse = 0.5;
        private DateTime _startTime;
        private SleepSettingsModel _settings;

        public SleepOverlayWindow(SleepSettingsModel settings)
        {
            InitializeComponent();
            _settings  = settings;
            _startTime = DateTime.Now;
            this.Loaded += SleepOverlayWindow_Loaded;

            MainGrid.Opacity = _settings.Brightness;
            ApplyStyle(_settings.Style);

            _animationTimer = new DispatcherTimer();
            _animationTimer.Interval = TimeSpan.FromMilliseconds(
                _settings.DisableAnimations ? 1000 : 80);
            _animationTimer.Tick += AnimationTimer_Tick;
            _animationTimer.Start();

            this.Cursor = System.Windows.Input.Cursors.None;

            // ── Dengerin event power Windows ─────────────────────────────
            // Kalau laptop suspend/hibernate/wake → tutup overlay otomatis
            SystemEvents.PowerModeChanged += OnPowerModeChanged;
            this.Closed += (s, e) => SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        }

        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            // Resume = laptop baru wake up dari sleep/hibernate
            // Suspend = laptop mau masuk sleep
            // Keduanya → tutup overlay agar tidak crash saat desktop kembali
            if (e.Mode == PowerModes.Resume || e.Mode == PowerModes.Suspend)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (!_isClosing) WakeUpSilent();
                }));
            }
        }

        // WakeUp tanpa animasi — untuk kasus power event agar tidak crash
        private void WakeUpSilent()
        {
            if (_isClosing) return;
            _isClosing = true;
            _animationTimer?.Stop();
            this.Close();
        }

        private void ApplyStyle(AodStyle style)
        {
            // Reset semua dulu
            TimeText.Visibility          = Visibility.Collapsed;
            StatusText.Visibility        = Visibility.Collapsed;
            PixelCharContainer.Visibility = Visibility.Collapsed;

            switch (style)
            {
                case AodStyle.MinimalClock:
                    TimeText.FontSize   = 72;
                    TimeText.Foreground = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255));
                    TimeText.Effect     = null;
                    TimeText.Visibility = Visibility.Visible;
                    StatusText.Visibility = Visibility.Visible;
                    break;

                case AodStyle.DigitalGlow:
                    TimeText.FontSize   = 80;
                    TimeText.Foreground = new SolidColorBrush(Color.FromRgb(0, 217, 255));
                    TimeText.Effect     = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color       = Color.FromRgb(0, 217, 255),
                        BlurRadius  = 20,
                        ShadowDepth = 0,
                        Opacity     = 0.8
                    };
                    TimeText.Visibility = Visibility.Visible;
                    break;

                case AodStyle.Analog:
                    TimeText.Visibility = Visibility.Collapsed;
                    AnalogClockContainer.Visibility = Visibility.Visible;
                    break;

                case AodStyle.DateFocus:
                    DateText.Visibility = Visibility.Visible;
                    TimeText.FontSize   = 28;
                    TimeText.Foreground = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
                    TimeText.Visibility = Visibility.Visible;
                    break;

                case AodStyle.Blank:
                    // Semua tersembunyi, layar hitam saja
                    break;
            }
        }

        private void SleepOverlayWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.Focus();
            Keyboard.Focus(this);
            this.Activate();

            if (!_settings.DisableAnimations)
            {
                DoubleAnimation fadeIn = new DoubleAnimation(0, _settings.Brightness, TimeSpan.FromMilliseconds(500));
                this.BeginAnimation(Window.OpacityProperty, fadeIn);
            }
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            if (_isClosing) return;

            var now = DateTime.Now;

            // Update teks waktu
            switch (_settings.Style)
            {
                case AodStyle.MinimalClock:
                case AodStyle.DigitalGlow:
                    TimeText.Text = now.ToString("HH:mm");
                    break;
                case AodStyle.DateFocus:
                    DateText.Text = now.ToString("MMM d").ToUpper();
                    TimeText.Text = now.ToString("HH:mm");
                    break;
                case AodStyle.Analog:
                    UpdateAnalogClock(now);
                    break;
            }

            if (_settings.DisableAnimations) return;

            // Pulse opacity
            _currentPulse += 0.008 * _pulseDir;
            if (_currentPulse > 0.75 || _currentPulse < 0.25) _pulseDir *= -1;

            if (_settings.Style == AodStyle.DigitalGlow)
            {
                // Glow intensity pulse
                if (TimeText.Effect is System.Windows.Media.Effects.DropShadowEffect glow)
                    glow.Opacity = _currentPulse + 0.2;
            }
            else
            {
                TimeText.Opacity = _currentPulse + 0.2;
            }

            // Anti burn-in: geser posisi setiap 10 detik
            if (now.Second % 10 == 0 && now.Millisecond < 100)
            {
                double ox = _random.Next(-60, 60);
                double oy = _random.Next(-40, 40);
                AnimContainer.RenderTransform = new TranslateTransform();
                AnimContainer.RenderTransform.BeginAnimation(TranslateTransform.XProperty,
                    new DoubleAnimation(ox, TimeSpan.FromMilliseconds(1500)));
                AnimContainer.RenderTransform.BeginAnimation(TranslateTransform.YProperty,
                    new DoubleAnimation(oy, TimeSpan.FromMilliseconds(1500)));
            }
        }

        private void UpdateAnalogClock(DateTime now)
        {
            double sec   = now.Second   * 6;
            double min   = now.Minute   * 6 + now.Second * 0.1;
            double hour  = (now.Hour % 12) * 30 + now.Minute * 0.5;

            SecondHand.RenderTransform  = new RotateTransform(sec,  1, 40);
            MinuteHand.RenderTransform  = new RotateTransform(min,  1, 40);
            HourHand.RenderTransform    = new RotateTransform(hour, 1, 40);
        }

        // --- Deteksi Input dengan Delay Protection & Config --- //

        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (_settings.ExitOnKeyDown) CheckWakeUp();
        }

        protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_settings.ExitOnMouseMove) CheckWakeUp();
        }

        protected override void OnMouseDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (_settings.ExitOnMouseDown) CheckWakeUp();
        }

        private void CheckWakeUp()
        {
            if ((DateTime.Now - _startTime).TotalMilliseconds < 500) return;
            WakeUp();
        }

        public void WakeUp()
        {
            if (_isClosing) return;
            _isClosing = true;

            _animationTimer?.Stop();

            if (!_settings.DisableAnimations)
            {
                DoubleAnimation fadeOut = new DoubleAnimation(this.Opacity, 0, TimeSpan.FromMilliseconds(200));
                fadeOut.Completed += (s, ev) => this.Close();
                this.BeginAnimation(Window.OpacityProperty, fadeOut);
            }
            else
            {
                this.Close();
            }
        }
    }
}
