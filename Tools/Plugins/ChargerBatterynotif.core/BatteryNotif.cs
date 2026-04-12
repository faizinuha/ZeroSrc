using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

using Color  = System.Windows.Media.Color;
using Brush  = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace ZeroMix.Plugins.ChargerNotif
{
    /// <summary>
    /// Notification window khusus untuk event CHARGER:
    ///   - Charging  : charger dicolok
    ///   - Unplugged : charger dicabut
    ///   - Full      : baterai penuh (100%)
    ///
    /// Low / Critical ditangani oleh zeromix.Battery — bukan plugin ini.
    /// </summary>
    public partial class BatteryNotifWindow : Window
    {
        private readonly DispatcherTimer _autoCloseTimer;

        // Folder plugin ini
        private static readonly string PluginDir = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Tools", "Plugins", "ChargerBatterynotif.core");

        // Folder maskot dari zeromix.Battery (referensi langsung, tidak duplikat)
        private static readonly string MaskotDir = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Tools", "Plugins", "zeromix.Battery", "Maskot");

        public enum NotifType { Charging, Unplugged, Full }

        public BatteryNotifWindow(NotifType type, int percent,
                                  string? customMessage   = null,
                                  string? customLottieJson = null,
                                  string? customSoundPath  = null)
        {
            InitializeComponent();

            SetupContent(type, percent, customMessage);
            SetupVisual(type, customLottieJson);
            SetupBatteryBar(percent, type);
            PositionWindow();
            PlaySound(type, customSoundPath);

            // Auto-close setelah 6 detik
            _autoCloseTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };
            _autoCloseTimer.Tick += (s, e) => BeginClose();
            _autoCloseTimer.Start();

            Loaded += (s, e) => ((Storyboard)FindResource("SlideIn")).Begin(this);
        }

        // ── Teks & warna per event ─────────────────────────────────────────
        private void SetupContent(NotifType type, int percent, string? custom)
        {
            switch (type)
            {
                case NotifType.Charging:
                    StatusLabel.Text       = "CHARGING";
                    StatusLabel.Foreground = new SolidColorBrush(Color.FromRgb(0, 214, 143));
                    TitleText.Text         = "Charger Dicolok ⚡";
                    BodyText.Text          = custom ?? $"Baterai sedang mengisi daya — {percent}% sekarang.";
                    BorderColor1.Color     = Color.FromRgb(0, 214, 143);
                    BorderColor2.Color     = Color.FromRgb(0, 102, 255);
                    break;

                case NotifType.Unplugged:
                    StatusLabel.Text       = "DICABUT";
                    StatusLabel.Foreground = new SolidColorBrush(Color.FromRgb(255, 170, 0));
                    TitleText.Text         = "Charger Dicabut 🔌";
                    BodyText.Text          = custom ?? $"Berjalan dengan baterai — sisa {percent}%.";
                    BorderColor1.Color     = Color.FromRgb(255, 170, 0);
                    BorderColor2.Color     = Color.FromRgb(255, 80, 0);
                    break;

                case NotifType.Full:
                    StatusLabel.Text       = "PENUH";
                    StatusLabel.Foreground = new SolidColorBrush(Color.FromRgb(0, 214, 143));
                    TitleText.Text         = "Baterai Penuh 🎉";
                    BodyText.Text          = custom ?? "Boleh dicabut charger-nya sekarang, Kak~";
                    BorderColor1.Color     = Color.FromRgb(0, 214, 143);
                    BorderColor2.Color     = Color.FromRgb(0, 214, 143);
                    break;
            }
        }

        // ── Visual: GIF animasi → fallback PNG maskot ──────────────────────
        private void SetupVisual(NotifType type, string? customLottieJson)
        {
            string gifDir = Path.Combine(PluginDir, "gif");
            // Pilih GIF per event
            string gifFile = type switch
            {
                NotifType.Charging  => "Welcome.gif",
                NotifType.Unplugged => "Loader cat.gif",
                NotifType.Full      => "Welcome.gif",
                _                   => "Welcome.gif"
            };

            string gifPath = Path.Combine(gifDir, gifFile);

            // Fallback ke gif lain jika tidak ada
            if (!File.Exists(gifPath))
                gifPath = Path.Combine(gifDir, "Loader cat.gif");
            if (!File.Exists(gifPath))
                gifPath = Path.Combine(gifDir, "Welcome.gif");

            if (File.Exists(gifPath))
            {
                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource   = new Uri(gifPath, UriKind.Absolute);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    bmp.Freeze();

                    WpfAnimatedGif.ImageBehavior.SetAnimatedSource(GifImage, bmp);
                    GifImage.Visibility    = Visibility.Visible;
                    MaskotImage.Visibility = Visibility.Collapsed;
                    return;
                }
                catch { }
            }

            // Fallback PNG maskot dari zeromix.Battery
            string pngFile = type switch
            {
                NotifType.Charging  => "Say_1.png",
                NotifType.Unplugged => "Say.png",
                NotifType.Full      => "70%.png",
                _                   => "Say_1.png"
            };

            string pngPath = Path.Combine(MaskotDir, pngFile);
            if (!File.Exists(pngPath))
                pngPath = Path.Combine(MaskotDir, "Say_1.png");

            if (!File.Exists(pngPath)) return;

            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource   = new Uri(pngPath, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                MaskotImage.Source     = bmp;
                MaskotImage.Visibility = Visibility.Visible;
                GifImage.Visibility    = Visibility.Collapsed;
            }
            catch { }
        }

        // ── Battery bar ────────────────────────────────────────────────────
        private void SetupBatteryBar(int percent, NotifType type)
        {
            PercentText.Text = $"{percent}%";

            const double maxWidth = 160;
            double target = maxWidth * (Math.Clamp(percent, 0, 100) / 100.0);
            var anim = new DoubleAnimation(0, target, TimeSpan.FromSeconds(0.8))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            BatteryFill.BeginAnimation(FrameworkElement.WidthProperty, anim);

            // Warna bar sesuai level
            if (percent <= 20)
            {
                BarColor1.Color = Color.FromRgb(255, 77, 77);
                BarColor2.Color = Color.FromRgb(200, 0, 0);
                PercentText.Foreground = new SolidColorBrush(Color.FromRgb(255, 77, 77));
            }
            else if (percent <= 50)
            {
                BarColor1.Color = Color.FromRgb(255, 170, 0);
                BarColor2.Color = Color.FromRgb(255, 100, 0);
                PercentText.Foreground = new SolidColorBrush(Color.FromRgb(255, 170, 0));
            }
            else
            {
                BarColor1.Color = Color.FromRgb(0, 214, 143);
                BarColor2.Color = Color.FromRgb(0, 170, 255);
                PercentText.Foreground = new SolidColorBrush(Color.FromRgb(0, 214, 143));
            }
        }

        // ── Sound ──────────────────────────────────────────────────────────
        private void PlaySound(NotifType type, string? customSoundPath)
        {
            try
            {
                // 1. Custom sound dari user
                if (!string.IsNullOrEmpty(customSoundPath) && File.Exists(customSoundPath))
                {
                    new System.Media.SoundPlayer(customSoundPath).Play();
                    return;
                }

                // 2. Built-in WAV bawaan plugin
                string soundDir = Path.Combine(PluginDir, "sounds");
                string wavFile = type switch
                {
                    NotifType.Charging  => "charging.wav",
                    NotifType.Unplugged => "unplug.wav",
                    NotifType.Full      => "full.wav",
                    _                   => ""
                };

                string wavPath = Path.Combine(soundDir, wavFile);
                if (!string.IsNullOrEmpty(wavFile) && File.Exists(wavPath))
                {
                    new System.Media.SoundPlayer(wavPath).Play();
                    return;
                }

                // 3. Windows system sound fallback
                System.Media.SystemSounds.Asterisk.Play();
            }
            catch { /* Sound selalu opsional */ }
        }

        // ── Posisi & close ─────────────────────────────────────────────────
        private void PositionWindow()
        {
            var area = SystemParameters.WorkArea;
            Left = area.Right - Width - 12;
            Top  = area.Bottom - Height - 12;
        }

        private void BeginClose()
        {
            _autoCloseTimer.Stop();
            ((Storyboard)FindResource("SlideOut")).Begin(this);
        }

        private void SlideOut_Completed(object sender, EventArgs e) => Close();
        private void Close_Click(object sender, RoutedEventArgs e)   => BeginClose();
    }
}
