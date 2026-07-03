using System;
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
                // Ambil pilihan user dari registry
                var selectedGif = WelcomePlugin.GetSelectedGif();
                string? gifPath = WelcomePlugin.ResolveGifPath(selectedGif);

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
