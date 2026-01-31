using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Forms; // Untuk NotifyIcon
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Text.Json;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ZeroMix.Plugins.Spotify
{
    public partial class SpotifySidebar : Window
    {
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private NotifyIcon? _trayIcon;
        private bool _isSidebarVisible = false;
        private readonly SpotifyService _spotify;
        private DispatcherTimer? _syncTimer;
        private BookmarkManager _bookmarkManager;

        public SpotifySidebar()
        {
            InitializeComponent();
            _spotify = new SpotifyService();
            _bookmarkManager = new BookmarkManager();
            _isSidebarVisible = false; // Initialize here
            InitializeTray(); // Initialize here
            SetupSidebarPosition(); // Initialize here
            
            // Auto-hide saat klik di luar (pindah focus ke aplikasi lain)
            this.Deactivated += (s, e) => {
                if (_isSidebarVisible) ToggleSidebar();
            };
            
            this.Loaded += async (s, e) => {
                // Sembunyikan dari Alt+Tab
                IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TOOLWINDOW);

                SetupSidebarPosition(); // Re-ensure position on load
                // InitializeTray(); // Moved to constructor
                await LoadSavedTokenAsync();
            };
            
            _syncTimer = new DispatcherTimer();
            _syncTimer.Interval = TimeSpan.FromSeconds(3);
            _syncTimer.Tick += async (s, e) => {
                if (_spotify.IsConnected) await _spotify.CheckPlayerState();
            };

            _spotify.OnTrackChanged += (title, artist, cover) => {
                this.Dispatcher.Invoke(() => UpdatePlayerUI(title, artist, cover));
            };

            // Refresh UI when bookmarks change
            _bookmarkManager.OnBookmarksChanged += () => {
                this.Dispatcher.Invoke(() => RefreshBookmarksUI());
            };

            // Handle search
            SearchBox.TextChanged += (s, e) => {
                PlaceholderText.Visibility = string.IsNullOrEmpty(SearchBox.Text) ? Visibility.Visible : Visibility.Collapsed;
            };

            LoginBtn.Click += async (s, e) => await HandleLogin();
        }

        // removed Close_Click as per UI change (no close 'X')

        private void SearchResult_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var btn = sender as System.Windows.Controls.Button;
                var track = btn?.DataContext as SpotifyTrack;
                if (track == null) return;

                if (!string.IsNullOrEmpty(track.PreviewUrl))
                {
                    Process.Start(new ProcessStartInfo(track.PreviewUrl) { UseShellExecute = true });
                }
                else if (!string.IsNullOrEmpty(track.Id))
                {
                    var url = $"https://open.spotify.com/track/{track.Id}";
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[SpotifySidebar] SearchResult_Click Error: " + ex.Message);
            }
        }

        private void LoadSavedToken()
        {
            // kept for backward compatibility; prefer async loader
            // no-op
        }

        private async System.Threading.Tasks.Task LoadSavedTokenAsync()
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
                                StatusText.Text = "Connected - Syncing with Spotify";
                                _syncTimer?.Start();

                                // Fetch profile
                                try {
                                    var profile = await _spotify.GetUserProfile();
                                    UserNameText.Text = profile.userName;
                                    if (!string.IsNullOrEmpty(profile.profileImageUrl))
                                    {
                                        try { ProfileImg.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(profile.profileImageUrl)); } catch {}
                                    }
                                } catch {}

                                // Merge cloud liked songs into local bookmarks
                                try {
                                    var saved = await _spotify.GetUserSavedTracks(50);
                                    foreach (var s in saved)
                                    {
                                        var bm = new SpotifyBookmark { Id = s.Id, Name = s.Name, Artist = s.Artist, CoverUrl = s.CoverUrl, Source = "spotify" };
                                        _bookmarkManager.AddBookmark(bm);
                                    }
                                } catch {}
                            }
                        }
                    }
                }

                // Always load local bookmarks to UI
                RefreshBookmarksUI();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[SpotifySidebar] LoadSavedTokenAsync Error: " + ex.Message);
            }
        }

        private void UpdatePlayerUI(string title, string artist, string coverUrl)
        {
            SongTitleText.Text = title;
            ArtistNameText.Text = artist;
            try {
                AlbumCoverImg.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(coverUrl));
            } catch {}
        }

        private void RefreshBookmarksUI()
        {
            try
            {
                var list = _bookmarkManager.GetAllBookmarks();
                LikedSongsList.ItemsSource = list;
                EmptyBookmarksMsg.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[SpotifySidebar] RefreshBookmarksUI Error: " + ex.Message);
            }
        }

        private async Task HandleLogin()
        {
            try
            {
                LoginBtn.Content = "WAITING FOR BROWSER...";
                LoginBtn.IsEnabled = false;

                // Call the new Auth Flow
                string token = await _spotify.InitiateLogin();

                if (!string.IsNullOrEmpty(token))
                {
                    SaveToken(token);
                    _spotify.SetToken(token);
                    _syncTimer?.Start();
                    LoginBtn.Content = "CONNECTED";
                    LoginBtn.IsEnabled = false;
                    LogoutBtn.Visibility = Visibility.Visible;
                    StatusText.Text = "Connected - Syncing with Spotify";

                    // Sync after login: fetch profile and saved tracks
                    try {
                        var profile = await _spotify.GetUserProfile();
                        UserNameText.Text = profile.userName;
                        if (!string.IsNullOrEmpty(profile.profileImageUrl))
                        {
                            try { ProfileImg.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(profile.profileImageUrl)); } catch {}
                        }
                    } catch {}

                    try {
                        var saved = await _spotify.GetUserSavedTracks(50);
                        foreach (var s in saved)
                        {
                            var bm = new SpotifyBookmark { Id = s.Id, Name = s.Name, Artist = s.Artist, CoverUrl = s.CoverUrl, Source = "spotify" };
                            _bookmarkManager.AddBookmark(bm);
                        }
                    } catch {}

                    RefreshBookmarksUI();
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
            File.WriteAllText(configPath, $"{{\"user\": \"User\", \"token\": \"{token}\"}}");
        }

        private async void SearchBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                string q = SearchBox.Text?.Trim() ?? "";
                if (string.IsNullOrEmpty(q)) return;

                SearchResultsPanel.Visibility = Visibility.Visible;
                try
                {
                    var results = await _spotify.Search(q);
                    SearchResultsList.ItemsSource = results;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[SpotifySidebar] Search Error: " + ex.Message);
                }
            }
        }

        private async void BookmarkTrack_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var btn = sender as System.Windows.Controls.Button;
                if (btn == null) return;
                var track = btn.Tag as SpotifyTrack;
                if (track == null) return;

                if (_bookmarkManager.IsBookmarked(track.Id)) return;

                var bm = new SpotifyBookmark { Id = track.Id, Name = track.Name, Artist = track.Artist, CoverUrl = track.CoverUrl, Source = _spotify.IsConnected ? "spotify" : "local" };
                _bookmarkManager.AddBookmark(bm);

                if (_spotify.IsConnected)
                {
                    await _spotify.SaveTrack(track.Id);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[SpotifySidebar] BookmarkTrack_Click Error: " + ex.Message);
            }
        }

        private async void RemoveBookmark_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var btn = sender as System.Windows.Controls.Button;
                if (btn == null) return;
                var id = btn.Tag as string;
                if (string.IsNullOrEmpty(id)) return;

                _bookmarkManager.RemoveBookmark(id);
                if (_spotify.IsConnected)
                {
                    await _spotify.RemoveTrack(id);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[SpotifySidebar] RemoveBookmark_Click Error: " + ex.Message);
            }
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
                LoginBtn.Content = "CONNECT SPOTIFY ACCOUNT";
                LogoutBtn.Visibility = Visibility.Collapsed;
                StatusText.Text = "Offline - Using Local Bookmarks";
                _syncTimer?.Stop();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[SpotifySidebar] Logout Error: " + ex.Message);
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
            _trayIcon.Text = "Spotify Pink - ZeroMix";
            
            try {
                // Gunakan PNG Pink yang Kakak siapkan
                string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins", "zeromix.spotify", "Logo", "Spotify_Primary_Logo_RGB_Pink-300x300.png");
                if (File.Exists(logoPath))
                {
                    using (var stream = File.OpenRead(logoPath))
                    {
                        var bitmap = (System.Drawing.Bitmap)System.Drawing.Image.FromStream(stream);
                        IntPtr hIcon = bitmap.GetHicon();
                        _trayIcon.Icon = System.Drawing.Icon.FromHandle(hIcon);
                        // Catatan: Handle Icon harus dilepas, tapi NotifyIcon butuh handle ini tetap hidup sampai diganti.
                        // Biasanya NotifyIcon.Icon tidak menduplikasi handle, jadi hIcon jangan langsung di-Destroy di sini.
                        // Kita biarkan NotifyIcon yang pegang, atau kita tiru cara C# internal.
                    }
                }
                else
                {
                    _trayIcon.Icon = SystemIcons.Application;
                }
            } catch { _trayIcon.Icon = SystemIcons.Application; }

            _trayIcon.Visible = true;
            _trayIcon.DoubleClick += (s, e) => ToggleSidebar();
            // Tambahkan klik kiri tunggal agar lebih responsif
            _trayIcon.MouseClick += (s, e) => {
                if (e.Button == MouseButtons.Left) ToggleSidebar();
            };

            var menu = new ContextMenuStrip();
            menu.Items.Add("Show/Hide Spotify", null, (s, e) => ToggleSidebar());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Search Song...", null, (s, e) => { 
                if (!_isSidebarVisible) ToggleSidebar();
                SearchBox.Focus();
            });
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
