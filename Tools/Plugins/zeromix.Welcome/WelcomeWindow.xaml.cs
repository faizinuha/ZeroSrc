using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ZeroMix.Plugins.Welcome
{
    public partial class WelcomeWindow : Window
    {
        private readonly DispatcherTimer _autoCloseTimer;

        public WelcomeWindow()
        {
            InitializeComponent();

            LoadGif();
            SetGreeting();
            PositionCenter();

            // Auto-close setelah 5 detik
            _autoCloseTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _autoCloseTimer.Tick += (s, e) => BeginClose();
            _autoCloseTimer.Start();

            // Klik mana saja untuk dismiss
            MouseLeftButtonDown += (s, e) => BeginClose();

            Loaded += (s, e) => ((Storyboard)FindResource("FadeIn")).Begin(this);
        }

        private void LoadGif()
        {
            try
            {
                string gifDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    "Tools", "Plugins", "zeromix.Welcome");

                string gifPath = Path.Combine(gifDir, "Welcome.gif");
                if (!File.Exists(gifPath))
                    gifPath = Path.Combine(gifDir, "welcome.gif");
                if (!File.Exists(gifPath)) return;

                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource   = new Uri(gifPath, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();

                WpfAnimatedGif.ImageBehavior.SetAnimatedSource(WelcomeGif, bmp);
            }
            catch { }
        }

        private void SetGreeting()
        {
            int hour = DateTime.Now.Hour;
            WelcomeTitle.Text = hour switch
            {
                >= 5  and < 11 => "Selamat Pagi! ☀️",
                >= 11 and < 15 => "Selamat Siang! 🌤️",
                >= 15 and < 18 => "Selamat Sore! 🌆",
                _              => "Selamat Malam! 🌙"
            };
            WelcomeSubtitle.Text = "ZeroMix siap menemanimu~";
        }

        private void PositionCenter()
        {
            var screen = SystemParameters.WorkArea;
            Left = (screen.Width  - Width)  / 2;
            Top  = (screen.Height - Height) / 2;
        }

        private void BeginClose()
        {
            _autoCloseTimer.Stop();
            ((Storyboard)FindResource("FadeOut")).Begin(this);
        }

        private void FadeOut_Completed(object sender, EventArgs e) => Close();
    }
}
