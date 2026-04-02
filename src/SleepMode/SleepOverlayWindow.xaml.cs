using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

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
            _settings = settings;
            _startTime = DateTime.Now;
            this.Loaded += SleepOverlayWindow_Loaded;
            
            // Apply Brightness
            MainGrid.Opacity = _settings.Brightness;

            // Apply Visual Toggles
            TimeText.Visibility = _settings.ShowClock ? Visibility.Visible : Visibility.Collapsed;
            PixelCharContainer.Visibility = _settings.ShowPixelCharacter ? Visibility.Visible : Visibility.Collapsed;

            // Konfigurasi FPS (Low CPU)
            _animationTimer = new DispatcherTimer();
            _animationTimer.Interval = TimeSpan.FromMilliseconds(80); 
            _animationTimer.Tick += AnimationTimer_Tick;
            
            if (!_settings.DisableAnimations)
                _animationTimer.Start();
            else
            {
                // Jika animasi mati, tetap update waktu sesekali (setiap detik)
                _animationTimer.Interval = TimeSpan.FromSeconds(1);
                _animationTimer.Start();
            }

            // Sembunyikan kursor
            this.Cursor = System.Windows.Input.Cursors.None;
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

            // 1. Update Waktu
            if (_settings.ShowClock)
                TimeText.Text = DateTime.Now.ToString("HH:mm");

            if (!_settings.DisableAnimations)
            {
                // 2. Animasi Pulsing Ringan pada Opacity Jam
                _currentPulse += 0.01 * _pulseDir;
                if (_currentPulse > 0.7 || _currentPulse < 0.3) _pulseDir *= -1;
                TimeText.Opacity = _currentPulse;

                // 3. Efek Bergerak Perlahan (Anti Burn-in Style)
                if (DateTime.Now.Second % 10 == 0 && DateTime.Now.Millisecond < 100)
                {
                    double offsetX = _random.Next(-50, 50);
                    double offsetY = _random.Next(-50, 50);
                    
                    var moveAnimX = new DoubleAnimation(offsetX, TimeSpan.FromMilliseconds(1000));
                    var moveAnimY = new DoubleAnimation(offsetY, TimeSpan.FromMilliseconds(1000));
                    
                    AnimContainer.RenderTransform = new TranslateTransform();
                    AnimContainer.RenderTransform.BeginAnimation(TranslateTransform.XProperty, moveAnimX);
                    AnimContainer.RenderTransform.BeginAnimation(TranslateTransform.YProperty, moveAnimY);
                }

                // 4. Pixel Character Animation (Lompat kecil)
                if (_settings.ShowPixelCharacter && DateTime.Now.Second % 2 == 0 && DateTime.Now.Millisecond < 80)
                {
                    var jumpAnim = new DoubleAnimation(0, -10, TimeSpan.FromMilliseconds(200)) { AutoReverse = true };
                    PixelCharMove.BeginAnimation(TranslateTransform.YProperty, jumpAnim);
                }
            }
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
