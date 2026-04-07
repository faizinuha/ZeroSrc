using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using Color = System.Windows.Media.Color;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

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

            MainGrid.Opacity = _settings.Brightness;
            ApplyStyle(_settings.Style);

            // Timer hanya untuk clock update + anti burn-in
            // Interval 1000ms jika animasi off, 80ms jika on — hemat CPU
            _animationTimer = new DispatcherTimer(DispatcherPriority.Background);
            _animationTimer.Interval = TimeSpan.FromMilliseconds(
                _settings.DisableAnimations ? 1000 : 80);
            _animationTimer.Tick += AnimationTimer_Tick;
            _animationTimer.Start();

            this.Cursor = System.Windows.Input.Cursors.None;

            // Load background langsung — no delay
            LoadCustomBackground();

            // Load music jika ada (opsional)
            LoadMusic();

            SystemEvents.PowerModeChanged += OnPowerModeChanged;
            this.Closed += (s, e) =>
            {
                SystemEvents.PowerModeChanged -= OnPowerModeChanged;
                StopMusic();
                // Bebaskan resource MediaElement
                CustomBgVideo.Source = null;
                CustomBgVideo.Close();
            };

            this.Loaded += (s, e) =>
            {
                this.Focus();
                Keyboard.Focus(this);
                this.Activate();

                if (!_settings.DisableAnimations)
                {
                    var fadeIn = new DoubleAnimation(0, _settings.Brightness, TimeSpan.FromMilliseconds(300));
                    this.BeginAnimation(Window.OpacityProperty, fadeIn);
                }
            };
        }

        // ── Power Event ──────────────────────────────────────────────────
        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Resume || e.Mode == PowerModes.Suspend)
                Dispatcher.BeginInvoke(new Action(() => { if (!_isClosing) WakeUpSilent(); }));
        }

        private void WakeUpSilent()
        {
            if (_isClosing) return;
            _isClosing = true;
            _animationTimer?.Stop();
            StopMusic();
            this.Close();
        }

        // ── Background ───────────────────────────────────────────────────
        private void LoadCustomBackground()
        {
            var path = _settings.CustomBackgroundPath;
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            {
                // Pakai default — opacity rendah agar hemat GPU
                DefaultBgImage.Visibility = Visibility.Visible;
                return;
            }

            DefaultBgImage.Visibility = Visibility.Collapsed;
            var ext = System.IO.Path.GetExtension(path).ToLower();

            if (ext is ".mp4" or ".webm" or ".mkv")
            {
                // Video: langsung play, loop, muted (audio dari MusicPlayer terpisah)
                CustomBgVideo.Source = new Uri(path, UriKind.Absolute);
                CustomBgVideo.Visibility = Visibility.Visible;
                CustomBgVideo.Play();
            }
            else
            {
                // Gambar: decode sekali, freeze agar tidak makan RAM berulang
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(path, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad; // load sekali, lepas file handle
                bmp.EndInit();
                bmp.Freeze(); // immutable → tidak perlu GC tracking
                CustomBgImage.Source = bmp;
                CustomBgImage.Visibility = Visibility.Visible;
            }
        }

        private void CustomBgVideo_MediaEnded(object sender, RoutedEventArgs e)
        {
            CustomBgVideo.Position = TimeSpan.Zero;
            CustomBgVideo.Play();
        }

        // ── Music (opsional) ─────────────────────────────────────────────
        private void LoadMusic()
        {
            var path = _settings.MusicPath;
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path)) return;

            MusicPlayer.Source = new Uri(path, UriKind.Absolute);
            MusicPlayer.Volume = _settings.MusicVolume;
            MusicPlayer.Play();
        }

        private void MusicPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            MusicPlayer.Position = TimeSpan.Zero;
            MusicPlayer.Play();
        }

        private void StopMusic()
        {
            try
            {
                MusicPlayer.Stop();
                MusicPlayer.Source = null;
                MusicPlayer.Close();
            }
            catch { }
        }

        // ── Style ────────────────────────────────────────────────────────
        private void ApplyStyle(AodStyle style)
        {
            TimeText.Visibility           = Visibility.Collapsed;
            StatusText.Visibility         = Visibility.Collapsed;
            DateText.Visibility           = Visibility.Collapsed;
            AnalogClockContainer.Visibility = Visibility.Collapsed;
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
                        Color = Color.FromRgb(0, 217, 255), BlurRadius = 20, ShadowDepth = 0, Opacity = 0.8
                    };
                    TimeText.Visibility = Visibility.Visible;
                    break;

                case AodStyle.Analog:
                    AnalogClockContainer.Visibility = Visibility.Visible;
                    break;

                case AodStyle.DateFocus:
                    DateText.Visibility = Visibility.Visible;
                    TimeText.FontSize   = 28;
                    TimeText.Foreground = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
                    TimeText.Visibility = Visibility.Visible;
                    break;

                case AodStyle.Blank:
                    break;
            }
        }

        // ── Animation Timer ──────────────────────────────────────────────
        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            if (_isClosing) return;

            var now = DateTime.Now;

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

            _currentPulse += 0.008 * _pulseDir;
            if (_currentPulse > 0.75 || _currentPulse < 0.25) _pulseDir *= -1;

            if (_settings.Style == AodStyle.DigitalGlow)
            {
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
                var tt = new TranslateTransform();
                AnimContainer.RenderTransform = tt;
                tt.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(ox, TimeSpan.FromMilliseconds(1500)));
                tt.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(oy, TimeSpan.FromMilliseconds(1500)));
            }
        }

        private void UpdateAnalogClock(DateTime now)
        {
            double sec  = now.Second * 6;
            double min  = now.Minute * 6 + now.Second * 0.1;
            double hour = (now.Hour % 12) * 30 + now.Minute * 0.5;
            SecondHand.RenderTransform = new RotateTransform(sec,  1, 40);
            MinuteHand.RenderTransform = new RotateTransform(min,  1, 40);
            HourHand.RenderTransform   = new RotateTransform(hour, 1, 40);
        }

        // ── Input Detection ──────────────────────────────────────────────
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
            // 500ms grace period agar tidak langsung keluar saat baru masuk
            if ((DateTime.Now - _startTime).TotalMilliseconds < 500) return;
            WakeUp();
        }

        public void WakeUp()
        {
            if (_isClosing) return;
            _isClosing = true;
            _animationTimer?.Stop();
            StopMusic();

            if (!_settings.DisableAnimations)
            {
                var fadeOut = new DoubleAnimation(this.Opacity, 0, TimeSpan.FromMilliseconds(200));
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
