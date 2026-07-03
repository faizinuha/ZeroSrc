using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using zeromix.SnapLayout.Models;
using Color = System.Windows.Media.Color;

namespace zeromix.SnapLayout
{
    /// <summary>
    /// Window indicator kecil untuk satu snap zone saat user drag window.
    /// Semi-transparan, hover highlight dengan scale animation,
    /// mouse tracking via DispatcherTimer 60fps.
    /// </summary>
    public partial class SnapZoneWindow : Window
    {
        private readonly SnapZone _zone;
        private readonly Rect _monitorBounds;
        private bool _isHovered;
        private bool _isClosed;

        // Ukuran minimum zona indicator
        private const double MIN_ZONE_WIDTH = 120;
        private const double MIN_ZONE_HEIGHT = 80;

        // Persentase dari layar
        private const double ZONE_WIDTH_RATIO = 0.10;
        private const double ZONE_HEIGHT_RATIO = 0.08;

        public SnapZoneWindow(SnapZone zone, Rect monitorBounds)
        {
            InitializeComponent();
            _zone = zone;
            _monitorBounds = monitorBounds;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ukuran indicator: proporsional atau minimal
                double indicatorW = Math.Max(MIN_ZONE_WIDTH, _monitorBounds.Width * ZONE_WIDTH_RATIO);
                double indicatorH = Math.Max(MIN_ZONE_HEIGHT, _monitorBounds.Height * ZONE_HEIGHT_RATIO);

                // Posisikan indicator di tepi zona (di dalam area zona)
                double zoneCenterX = _zone.ScreenRect.X + _zone.ScreenRect.Width / 2;
                double zoneCenterY = _zone.ScreenRect.Y + _zone.ScreenRect.Height / 2;

                // Letakkan indicator di area zona yang nyaman
                // — senter horizontal dalam zona, vertikal di tengah
                this.Width = indicatorW;
                this.Height = indicatorH;
                this.Left = zoneCenterX - indicatorW / 2;
                this.Top = zoneCenterY - indicatorH / 2;

                ZoneLabel.Text = _zone.Label;

                // Fade in
                this.Opacity = 0;
                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(80));
                this.BeginAnimation(OpacityProperty, fadeIn);

                // Pastikan z-order: window tetap di bawah dragged window
                // dengan memanggil SetWindowPos HWND_TOPMOST
                var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                if (hwnd != IntPtr.Zero)
                {
                    ZeroMix.Native.Win32.Window.SetWindowPos(
                        hwnd,
                        new IntPtr(-1), // HWND_TOPMOST
                        0, 0, 0, 0,
                        0x0001 | 0x0002 | 0x0010); // SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SnapZone] Load error: {ex.Message}");
            }
        }

        /// <summary>
        /// Cek apakah posisi mouse (screen-space) ada di dalam window ini.
        /// Dipanggil dari DispatcherTimer 60fps di SnapLayoutService.
        /// </summary>
        public void CheckHover(int mouseX, int mouseY)
        {
            if (_isClosed) return;

            bool inside = mouseX >= this.Left && mouseX <= this.Left + this.Width &&
                          mouseY >= this.Top && mouseY <= this.Top + this.Height;

            if (inside && !_isHovered)
            {
                _isHovered = true;
                // Animate background -> cyan lebih terang
                ZoneBorder.Background = new SolidColorBrush(Color.FromArgb(0x44, 0x00, 0xD4, 0xFF));

                // Scale up 1.0 → 1.05
                var scaleUp = new DoubleAnimation(1.0, 1.05, TimeSpan.FromMilliseconds(100));
                scaleUp.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };
                ZoneScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleUp);
                ZoneScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleUp);

                ZoneLabel.Opacity = 1.0;
            }
            else if (!inside && _isHovered)
            {
                _isHovered = false;
                // Kembalikan ke normal
                ZoneBorder.Background = new SolidColorBrush(Color.FromArgb(0x1A, 0x00, 0xD4, 0xFF));

                var scaleDown = new DoubleAnimation(1.05, 1.0, TimeSpan.FromMilliseconds(80));
                scaleDown.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn };
                ZoneScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleDown);
                ZoneScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleDown);

                ZoneLabel.Opacity = 0.6;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _isClosed = true;
            base.OnClosed(e);
        }
    }
}
