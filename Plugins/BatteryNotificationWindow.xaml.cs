using System;
using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;

namespace ZeroMix.Plugins
{
    public partial class BatteryNotificationWindow : Window
    {
        public BatteryNotificationWindow(int percent, BatteryPlugin plugin, bool isGreeting = false)
        {
            InitializeComponent();
            SetupUI(percent, plugin, isGreeting);
            PositionWindow();
            
            Loaded += (s, e) => {
                var sb = (Storyboard)FindResource("FadeIn");
                sb.Begin(this); // Target the window itself
                
                // Auto close greeting after 8 seconds
                if (isGreeting)
                {
                    var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(8) };
                    timer.Tick += (ss, ee) => { try { this.Close(); } catch { } timer.Stop(); };
                    timer.Start();
                }
            };
        }

        private void SetupUI(int percent, BatteryPlugin plugin, bool isGreeting)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string mascotFile = "Say_1.png";
            string title = "Battery Assistant";
            string body = "Kak, cek baterainya yuk?";
// Sapaan
            if (isGreeting)
            {
                int hour = DateTime.Now.Hour;
                if (hour >= 5 && hour < 11)
                {
                    title = "Selamat Pagi! ✨";
                    body = plugin.TextMorning;
                }
                else if (hour >= 11 && hour < 15)
                {
                    title = "Selamat Siang! ☀️";
                    body = plugin.TextAfternoon;
                }
                else if (hour >= 15 && hour < 18)
                {
                    title = "Selamat Sore! 🌆";
                    body = plugin.TextEvening;
                }
                else
                {
                    title = "Selamat Malam! 🌙";
                    body = plugin.TextNight;
                }
                mascotFile = "Say_1.png";
            }
            // Baterai
            else
            {
                if (percent <= 10)
                {
                    mascotFile = "10-5_%.png";
                    title = "KRITIS! 😱";
                    body = plugin.TextBatteryCritical;
                }else if (percent <= 60) {
                   mascotFile = "50_%.png";
                   title = "Baterai 60% 🔋";
                   body = plugin.TextBatteryWarn;
                }else if (percent <= 50)
                {
                    mascotFile = "50_%.png";
                    title = "Baterai 50% 🔋";
                    body = plugin.TextBatteryWarn;
                }
                else
                {
                    mascotFile = "Say_1.png";
                    title = "Info Baterai";
                    body = "Sekedar info kak, baterai sekarang ada di " + percent + "%. Masih aman kok!";
                }
            }
    // FindResource
            try
            {
                string imgPath = Path.Combine(baseDir, "Plugins", "Maskot", mascotFile);
                if (File.Exists(imgPath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(Path.GetFullPath(imgPath), UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    
                    // Assign to both icon and background overlay
                    MascotImgIcon.Source = bitmap;
                    MascotImgBg.Source = bitmap;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Image Load Error: " + ex.Message);
            }

            MsgTitle.Text = title;
            MsgBody.Text = body;
        }

        private void PositionWindow()
        {
            var desktopWorkingArea = SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.Width - 10;
            this.Top = desktopWorkingArea.Bottom - this.Height - 10;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
