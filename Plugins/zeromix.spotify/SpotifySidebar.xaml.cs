using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Text.Json;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ZeroMix.Plugins.Spotify
{
    // NOTE: File ini sudah digantikan oleh GoogleAccountWindow.xaml.cs
    // File ini disimpan untuk kompatibilitas mundur
    public partial class SpotifySidebar : Window
    {
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private NotifyIcon? _trayIcon;
        private bool _isSidebarVisible = false;
        private readonly SpotifyService _spotify;

        public SpotifySidebar()
        {
            InitializeComponent();
            _spotify = new SpotifyService();
            _isSidebarVisible = false;
            InitializeTray();
            SetupSidebarPosition();
            
            this.Deactivated += (s, e) => {
                if (_isSidebarVisible) ToggleSidebar();
            };
            
            this.Loaded += async (s, e) => {
                IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TOOLWINDOW);

                SetupSidebarPosition();
                await LoadSavedTokenAsync();
            };
            
            LoginBtn.Click += async (s, e) => await HandleLogin();
        }

        private async Task LoadSavedTokenAsync()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string path = Path.Combine(appData, "ZeroMix", "Plugins", "Spotify", "auth.json");
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    using (JsonDocument doc = JsonDocument.Parse(json))
                    {
                        if (doc.RootElement.TryGetProperty("token", out var token))
                        {
                            var tstr = token.GetString() ?? "";
                            _spotify.SetToken(tstr);
                            if (_spotify.IsConnected)
                            {
                                LoginBtn.Content = "CONNECTED";
                                LoginBtn.IsEnabled = false;
                                LogoutBtn.Visibility = Visibility.Visible;
                                StatusText.Text = "Connected to Google";

                                try { 
                                    var profile = await _spotify.GetUserProfile();
                                    UserNameText.Text = profile.userName;
                                    if (!string.IsNullOrEmpty(profile.profileImageUrl))
                                    {
                                        try { ProfileImg.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(profile.profileImageUrl)); } catch {}
                                    }
                                } catch {}
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Google Auth] LoadSavedTokenAsync Error: " + ex.Message);
            }
        }

        private async Task HandleLogin()
        {
            try
            {
                LoginBtn.Content = "WAITING FOR BROWSER...";
                LoginBtn.IsEnabled = false;

                string token = await _spotify.InitiateLogin();

                if (!string.IsNullOrEmpty(token))
                {
                    SaveToken(token);
                    _spotify.SetToken(token);
                    LoginBtn.Content = "CONNECTED";
                    LoginBtn.IsEnabled = false;
                    LogoutBtn.Visibility = Visibility.Visible;
                    StatusText.Text = "Connected to Google";

                    try { 
                        var profile = await _spotify.GetUserProfile();
                        UserNameText.Text = profile.userName;
                        if (!string.IsNullOrEmpty(profile.profileImageUrl))
                        {
                            try { ProfileImg.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(profile.profileImageUrl)); } catch {}
                        }
                    } catch {}
                }
                else
                {
                    LoginBtn.Content = "LOGIN FAILED";
                    LoginBtn.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                 System.Windows.MessageBox.Show("Login Error: " + ex.Message);
                 LoginBtn.Content = "RETRY LOGIN";
                 LoginBtn.IsEnabled = true;
            }
        }

        private void SaveToken(string token)
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string spotifyDataDir = Path.Combine(appData, "ZeroMix", "Plugins", "Spotify");
            if (!Directory.Exists(spotifyDataDir)) Directory.CreateDirectory(spotifyDataDir);

            string configPath = Path.Combine(spotifyDataDir, "auth.json");
            var data = new { user = "User", token = token };
            File.WriteAllText(configPath, JsonSerializer.Serialize(data));
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string path = Path.Combine(appData, "ZeroMix", "Plugins", "Spotify", "auth.json");
                if (File.Exists(path)) File.Delete(path);
                _spotify.SetToken("");
                LoginBtn.IsEnabled = true;
                LoginBtn.Content = "Login accout Google";
                LogoutBtn.Visibility = Visibility.Collapsed;
                StatusText.Text = "Offline";
                UserNameText.Text = "Guest";
                ProfileImg.Source = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Google Auth] Logout Error: " + ex.Message);
            }
        }

        private void SetupSidebarPosition()
        {
            var desktopWorkingArea = SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.Width; 
            this.Top = desktopWorkingArea.Top;
            this.Height = desktopWorkingArea.Height;
        }

        private void InitializeTray()
        {
            _trayIcon = new NotifyIcon();
            _trayIcon.Text = "Google Account - ZeroMix";
            
            _trayIcon.Icon = SystemIcons.Application;

            _trayIcon.Visible = true;
            _trayIcon.DoubleClick += (s, e) => ToggleSidebar();
            _trayIcon.MouseClick += (s, e) => {
                if (e.Button == MouseButtons.Left) ToggleSidebar();
            };

            var menu = new ContextMenuStrip();
            menu.Items.Add("Show/Hide", null, (s, e) => ToggleSidebar());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit Plugin", null, (s, e) => ClosePlugin());
            _trayIcon.ContextMenuStrip = menu;
        }

        public void ToggleSidebar()
        {
            if (_isSidebarVisible) AnimateOut();
            else AnimateIn();
            _isSidebarVisible = !_isSidebarVisible;
        }

        private void AnimateIn()
        {
            var desktopWorkingArea = SystemParameters.WorkArea;
            DoubleAnimation slide = new DoubleAnimation
            {
                To = desktopWorkingArea.Right - this.Width,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
            };
            this.BeginAnimation(Window.LeftProperty, slide);
        }

        private void AnimateOut()
        {
            var desktopWorkingArea = SystemParameters.WorkArea;
            DoubleAnimation slide = new DoubleAnimation
            {
                To = desktopWorkingArea.Right,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseIn }
            };
            this.BeginAnimation(Window.LeftProperty, slide);
        }

        private void ClosePlugin()
        {
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }
            this.Close();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }
            base.OnClosing(e);
        }
    }
}