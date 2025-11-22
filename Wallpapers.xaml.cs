using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Navigation;

namespace ZeroMix
{
    public partial class Wallpapers : Window
    {
        private string? _selectedImagePath;
        private ObservableCollection<WallpaperItem> _allWallpapers = new();
        private ObservableCollection<WallpaperItem> _filteredWallpapers = new();
        private string _currentFilter = "all";
        private string _currentSearch = "";
        private Random _random = new();

        public Wallpapers()
        {
            InitializeComponent();
            WallpaperListPanel.ItemsSource = _filteredWallpapers;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            FilterAllBtn.Background = (SolidColorBrush)FindResource("AccentBrush");
            await LoadWallpapersAsync();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        // --- SEARCH & FILTER ---
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _currentSearch = SearchBox.Text.ToLowerInvariant();
            ApplyFilter();
        }

        private void FilterBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                // Reset all buttons
                FilterAllBtn.Background = (SolidColorBrush)FindResource("ControlHoverBrush");
                FilterImagesBtn.Background = (SolidColorBrush)FindResource("ControlHoverBrush");
                FilterVideosBtn.Background = (SolidColorBrush)FindResource("ControlHoverBrush");
                FilterAnimatedBtn.Background = (SolidColorBrush)FindResource("ControlHoverBrush");

                // Highlight selected
                btn.Background = (SolidColorBrush)FindResource("AccentBrush");

                // Set filter
                if (btn == FilterAllBtn) _currentFilter = "all";
                else if (btn == FilterImagesBtn) _currentFilter = "images";
                else if (btn == FilterVideosBtn) _currentFilter = "videos";
                else if (btn == FilterAnimatedBtn) _currentFilter = "animated";

                ApplyFilter();
            }
        }

        private void ApplyFilter()
        {
            _filteredWallpapers.Clear();

            var filtered = _allWallpapers.Where(w =>
            {
                // Search filter
                if (!string.IsNullOrEmpty(_currentSearch) && !w.Name.ToLowerInvariant().Contains(_currentSearch))
                    return false;

                // Type filter
                return _currentFilter switch
                {
                    "images" => w.Type == WallpaperType.Image,
                    "videos" => w.Type == WallpaperType.Video,
                    "animated" => w.Type == WallpaperType.Animated,
                    _ => true
                };
            }).ToList();

            foreach (var item in filtered)
                _filteredWallpapers.Add(item);

            CountLabel.Text = $"{_filteredWallpapers.Count} items";
        }

        // --- RANDOM WALLPAPER ---
        private void RandomBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_filteredWallpapers.Count == 0) return;

            int randomIndex = _random.Next(_filteredWallpapers.Count);
            var randomWallpaper = _filteredWallpapers[randomIndex];
            SelectWallpaper(randomWallpaper);
        }

        // --- LOAD WALLPAPERS ---
        private async Task LoadWallpapersAsync()
        {
            _allWallpapers.Clear();

            // Load current wallpaper
            try
            {
                string? currentPath = GetCurrentWallpaperPath();
                if (!string.IsNullOrEmpty(currentPath) && File.Exists(currentPath))
                {
                    var bitmap = CreateBitmapFromPath(currentPath);
                    if (bitmap != null)
                    {
                        _allWallpapers.Add(new WallpaperItem
                        {
                            Name = "Current Wallpaper",
                            Path = currentPath,
                            Thumbnail = bitmap,
                            Type = WallpaperType.Image,
                            IsSelected = false
                        });
                    }
                }
            }
            catch { }

            // Load from disk
            bool loadedFromDisk = false;
            try
            {
                var diskWallpapers = EnumerateResourceImagesOnDisk();
                foreach (var imagePath in diskWallpapers)
                {
                    var bitmap = CreateBitmapFromPath(imagePath);
                    if (bitmap != null)
                    {
                        loadedFromDisk = true;
                        var type = DetermineWallpaperType(imagePath);
                        _allWallpapers.Add(new WallpaperItem
                        {
                            Name = Path.GetFileNameWithoutExtension(imagePath),
                            Path = imagePath,
                            Thumbnail = bitmap,
                            Type = type,
                            IsSelected = false
                        });
                    }
                }
            }
            catch { }

            // Fallback to pack resources
            if (!loadedFromDisk)
            {
                var resourcePaths = new[]
                {
                    "Resource/Images/1.jpg", "Resource/Images/2.jpg", "Resource/Images/3.jpg",
                    "Resource/Images/4.jpg", "Resource/Images/5.jpg", "Resource/Images/6.jpg",
                    "Resource/Images/7.jpg", "Resource/anim/Fieren.jpg", "Resource/anim/Fieren2.jpg",
                    "Resource/anim/Fieren3.jpg", "Resource/anim/view.jpg", "Resource/anim/anime.jpg"
                };

                foreach (var path in resourcePaths)
                {
                    try
                    {
                        var uri = new Uri($"pack://application:,,,/{path}");
                        var bitmap = CreateBitmapFromUri(uri);
                        if (bitmap != null)
                        {
                            _allWallpapers.Add(new WallpaperItem
                            {
                                Name = Path.GetFileNameWithoutExtension(path),
                                Path = uri.ToString(),
                                Thumbnail = bitmap,
                                Type = WallpaperType.Image,
                                IsSelected = false
                            });
                        }
                    }
                    catch { }
                }
            }

            ApplyFilter();
        }

        // --- SELECTION & PREVIEW ---
        private void SelectWallpaper(WallpaperItem wallpaper)
        {
            // Unselect all
            foreach (var wp in _allWallpapers)
                wp.IsSelected = false;

            // Select new
            wallpaper.IsSelected = true;
            _selectedImagePath = wallpaper.Path;
            StatusLabel.Text = $"Selected: {wallpaper.Name}";

            RefreshUI();
        }

        private void RefreshUI()
        {
            WallpaperListPanel.ItemsSource = null;
            WallpaperListPanel.ItemsSource = _filteredWallpapers;
        }

        // --- BROWSE & SET ---
        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select a Wallpaper",
                Filter = "All Media|*.jpg;*.jpeg;*.png;*.bmp;*.mp4;*.wmv;*.mov|Image Files|*.jpg;*.jpeg;*.png;*.bmp|Video Files|*.mp4;*.wmv;*.mov|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                var bitmap = CreateBitmapFromPath(dialog.FileName);
                if (bitmap != null)
                {
                    var type = DetermineWallpaperType(dialog.FileName);
                    var item = new WallpaperItem
                    {
                        Name = Path.GetFileNameWithoutExtension(dialog.FileName),
                        Path = dialog.FileName,
                        Thumbnail = bitmap,
                        Type = type,
                        IsSelected = false
                    };

                    _allWallpapers.Insert(0, item);
                    SelectWallpaper(item);
                    ApplyFilter();
                }
            }
        }

        private void SetWallpaperButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedImagePath) || !File.Exists(_selectedImagePath))
            {
                MessageBox.Show("Please select a valid image first.", "No File Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var ext = Path.GetExtension(_selectedImagePath).ToLowerInvariant();
            if (new[] { ".mp4", ".wmv", ".mov" }.Contains(ext))
            {
                MessageBox.Show("Video wallpapers are not yet supported on this system.", "Feature Not Available", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                NativeMethods.SetWallpaper(_selectedImagePath);
                MessageBox.Show("Wallpaper changed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to set wallpaper: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- HELPERS ---
        private WallpaperType DetermineWallpaperType(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (new[] { ".mp4", ".wmv", ".mov", ".avi" }.Contains(ext))
                return WallpaperType.Video;
            if (path.Contains("anim", StringComparison.OrdinalIgnoreCase))
                return WallpaperType.Animated;
            return WallpaperType.Image;
        }

        private BitmapImage? CreateBitmapFromPath(string imagePath)
        {
            if (!File.Exists(imagePath)) return null;
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath);
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = 200;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch { return null; }
        }

        private BitmapImage? CreateBitmapFromUri(Uri imageUri)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = imageUri;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = 200;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch { return null; }
        }

        private IEnumerable<string> EnumerateResourceImagesOnDisk()
        {
            var uniquePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var exts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".bmp", ".mp4", ".wmv", ".mov" };
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var dirInfo = new DirectoryInfo(baseDir);

                for (int i = 0; i < 4 && dirInfo != null; i++)
                {
                    string resourceRoot = Path.Combine(dirInfo.FullName, "Resource");
                    if (Directory.Exists(resourceRoot))
                    {
                        var dirs = new[] { Path.Combine(resourceRoot, "Images"), Path.Combine(resourceRoot, "anim"), Path.Combine(resourceRoot, "Video") };
                        foreach (var dir in dirs)
                        {
                            if (!Directory.Exists(dir)) continue;
                            foreach (var file in Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories))
                            {
                                if (exts.Contains(Path.GetExtension(file)))
                                    uniquePaths.Add(file);
                            }
                        }
                        return uniquePaths;
                    }
                    dirInfo = dirInfo.Parent;
                }
            }
            catch { }
            return uniquePaths;
        }

        private string? GetCurrentWallpaperPath()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop"))
                {
                    return key?.GetValue("Wallpaper") as string;
                }
            }
            catch { return null; }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                e.Handled = true;
            }
            catch { }
        }
    }

    // --- HELPER CLASSES ---
    public enum WallpaperType { Image, Video, Animated }

    public class WallpaperItem
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public BitmapImage? Thumbnail { get; set; }
        public WallpaperType Type { get; set; }
        public bool IsSelected { get; set; }
    }

    internal static class NativeMethods
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        private const int SPI_SETDESKWALLPAPER = 20;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDCHANGE = 0x02;

        public static void SetWallpaper(string path)
        {
            SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, path, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
        }
    }
}
