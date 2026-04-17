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

            // Auto-close setelah 4 detik
            _autoCloseTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
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
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string[] candidates =
                {
                    Path.Combine(baseDir, "Tools", "Plugins", "zeromix.Welcome", "Welcome.gif"),
                    Path.Combine(baseDir, "Tools", "Plugins", "zeromix.Welcome", "welcome.gif"),
                    Path.Combine(baseDir, "Tools", "Plugins", "zeromix.Welcome", "gif", "Welcome.gif"),
                };

                string? gifPath = null;
                foreach (var c in candidates)
                {
                    if (File.Exists(c)) { gifPath = c; break; }
                }

                if (gifPath != null)
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(gifPath, UriKind.Absolute);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    bmp.Freeze();
                    WpfAnimatedGif.ImageBehavior.SetAnimatedSource(WelcomeGif, bmp);
                }
                else
                {
                    // Kalau GIF tidak ada, langsung tutup
                    Loaded += (_, _) => BeginClose();
                }
            }
            catch
            {
                Loaded += (_, _) => BeginClose();
            }
        }

        private bool _closing = false;

        private void BeginClose()
        {
            if (_closing) return;
            _closing = true;
            _autoCloseTimer.Stop();
            ((Storyboard)FindResource("FadeOut")).Begin(this);
        }

        private void FadeOut_Completed(object sender, EventArgs e) => Close();
    }
}
