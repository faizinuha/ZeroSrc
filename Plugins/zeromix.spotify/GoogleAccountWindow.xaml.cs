using System;
using System.Windows;
using System.Windows.Input;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using System.Diagnostics;

namespace ZeroMix.Plugins.Spotify
{
    public partial class GoogleAccountWindow : Window
    {
        private readonly SpotifyService _googleService;
        private static GoogleAccountWindow? _instance;

        public static GoogleAccountWindow Instance
        {
            get
            {
                if (_instance == null || !_instance.IsLoaded)
                {
                    _instance = new GoogleAccountWindow();
                }
                return _instance;
            }
        }

        public GoogleAccountWindow()
        {
            InitializeComponent();
            _googleService = new SpotifyService();
            
            this.Loaded += async (s, e) => await LoadSavedTokenAsync();
            LoginBtn.Click += async (s, e) => await HandleLogin();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                // Double-click to close
                this.Close();
            }
            else
            {
                this.DragMove();
            }
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
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
                            var tokenStr = token.GetString() ?? "";
                            _googleService.SetToken(tokenStr);
                            
                            if (_googleService.IsConnected)
                            {
                                await UpdateUIConnected();
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
                LoginBtn.Content = "⏳ Menunggu browser...";
                LoginBtn.IsEnabled = false;
                StatusText.Text = "Buka browser untuk login";

                string token = await _googleService.InitiateLogin();

                if (!string.IsNullOrEmpty(token))
                {
                    SaveToken(token);
                    _googleService.SetToken(token);
                    await UpdateUIConnected();
                }
                else
                {
                    LoginBtn.Content = "❌ Login Gagal";
                    LoginBtn.IsEnabled = true;
                    StatusText.Text = "Coba lagi";
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Login Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                LoginBtn.Content = "🔄 Coba Lagi";
                LoginBtn.IsEnabled = true;
                StatusText.Text = "";
            }
        }

        private async Task UpdateUIConnected()
        {
            LoginBtn.Content = "✓ Terhubung";
            LoginBtn.IsEnabled = false;
            LogoutBtn.Visibility = Visibility.Visible;
            UserStatusText.Text = "Terhubung ke Google";
            StatusText.Text = "";

            try
            {
                var profile = await _googleService.GetUserProfile();
                UserNameText.Text = profile.userName;
                
                if (!string.IsNullOrEmpty(profile.profileImageUrl))
                {
                    try
                    {
                        ProfileImg.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(profile.profileImageUrl));
                        DefaultIcon.Visibility = Visibility.Collapsed;
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void SaveToken(string token)
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dataDir = Path.Combine(appData, "ZeroMix", "Plugins", "Spotify");
            
            if (!Directory.Exists(dataDir)) 
                Directory.CreateDirectory(dataDir);

            string configPath = Path.Combine(dataDir, "auth.json");
            var data = new { user = "User", token = token };
            File.WriteAllText(configPath, JsonSerializer.Serialize(data));
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string path = Path.Combine(appData, "ZeroMix", "Plugins", "Spotify", "auth.json");
                
                if (File.Exists(path)) 
                    File.Delete(path);
                
                _googleService.SetToken("");
                
                // Reset UI
                LoginBtn.IsEnabled = true;
                LoginBtn.Content = "🔗  Hubungkan Google";
                LogoutBtn.Visibility = Visibility.Collapsed;
                UserStatusText.Text = "Belum terhubung";
                UserNameText.Text = "Guest";
                ProfileImg.Source = null;
                DefaultIcon.Visibility = Visibility.Visible;
                StatusText.Text = "";
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Google Auth] Logout Error: " + ex.Message);
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Hide instead of close to keep instance alive
            e.Cancel = true;
            this.Hide();
        }

        public void ShowWindow()
        {
            this.Show();
            this.Activate();
            this.Focus();
        }
    }
}
