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
            if (sender is System.Windows.Controls.Button btn)
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
                    var type = DetermineWallpaperType(imagePath);
                    BitmapImage? bitmap = null;
                    
                    // Try to load image
                    bitmap = CreateBitmapFromPath(imagePath);
                    
                    // If it's a video and no bitmap, create placeholder
                    if (bitmap == null && type != WallpaperType.Image)
                    {
                        bitmap = CreatePlaceholderBitmap(type);
                    }
                    
                    if (bitmap != null)
                    {
                        loadedFromDisk = true;
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

        // --- WALLPAPER ITEM CLICK ---
        private void WallpaperBorder_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is WallpaperItem wallpaper)
            {
                SelectWallpaper(wallpaper);
            }
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
            var dialog = new Microsoft.Win32.OpenFileDialog
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
                System.Windows.MessageBox.Show("Silakan pilih wallpaper terlebih dahulu.", "Tidak Ada File", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var ext = Path.GetExtension(_selectedImagePath).ToLowerInvariant();
            var isVideo = new[] { ".mp4", ".wmv", ".mov", ".avi" }.Contains(ext);

            try
            {
                if (isVideo)
                {
                    // Coba set video wallpaper menggunakan Windows API
                    if (!SetVideoWallpaper(_selectedImagePath))
                    {
                        System.Windows.MessageBox.Show("Video wallpaper tidak didukung di sistem ini. Coba dengan Windows 10/11 atau gunakan image wallpaper.", "Fitur Tidak Tersedia", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                }
                else
                {
                    NativeMethods.SetWallpaper(_selectedImagePath);
                }

                System.Windows.MessageBox.Show("Wallpaper berhasil diubah!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Gagal mengubah wallpaper: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool SetVideoWallpaper(string videoPath)
        {
            try
            {
                var ffmpegPath = FindFFmpeg();
                if (string.IsNullOrEmpty(ffmpegPath))
                {
                    System.Windows.MessageBox.Show(
                        "FFmpeg tidak ditemukan di sistem Anda.\n\n" +
                        "Untuk menggunakan video wallpaper, silakan install FFmpeg:\n" +
                        "1. Download dari https://ffmpeg.org/download.html\n" +
                        "2. Extract ke C:\\ffmpeg\n" +
                        "3. Tambahkan C:\\ffmpeg\\bin ke PATH environment variable\n\n" +
                        "Atau gunakan: winget install FFmpeg",
                        "FFmpeg Diperlukan",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return false;
                }

                // Create optimized video for wallpaper
                StatusLabel.Text = "🎬 Mengoptimasi video untuk wallpaper...";
                var optimizedVideoPath = OptimizeVideoForWallpaper(videoPath, ffmpegPath);
                
                if (string.IsNullOrEmpty(optimizedVideoPath))
                {
                    System.Windows.MessageBox.Show(
                        "Gagal mengoptimasi video. Pastikan file video valid.",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return false;
                }

                // Set registry untuk video wallpaper
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
                {
                    key?.SetValue("VideoWallpaper", optimizedVideoPath, Microsoft.Win32.RegistryValueKind.String);
                }

                StatusLabel.Text = $"✓ Video wallpaper berhasil diset: {Path.GetFileName(optimizedVideoPath)}";
                
                // Launch video as wallpaper using Windows process
                LaunchVideoWallpaper(optimizedVideoPath);
                
                return true;
            }
            catch (Exception ex)
            {
                StatusLabel.Text = $"Error: {ex.Message}";
                return false;
            }
        }

        private string? OptimizeVideoForWallpaper(string inputPath, string ffmpegPath)
        {
            try
            {
                // Create output path in temp directory
                var tempDir = Path.Combine(Path.GetTempPath(), "ZeroMix", "Wallpapers");
                Directory.CreateDirectory(tempDir);
                
                var fileName = Path.GetFileNameWithoutExtension(inputPath);
                var outputPath = Path.Combine(tempDir, $"{fileName}_optimized.mp4");

                // Get screen resolution
                var screenWidth = (int)SystemParameters.PrimaryScreenWidth;
                var screenHeight = (int)SystemParameters.PrimaryScreenHeight;

                // FFmpeg arguments untuk video yang ringan dan berkualitas
                // - VP9 codec untuk ukuran file lebih kecil
                // - CRF 30-35 untuk balance antara kualitas dan ukuran
                // - Scale ke resolusi layar
                // - 30fps untuk smooth playback
                var arguments = $"-i \"{inputPath}\" " +
                               $"-c:v libx264 " +                    // H.264 codec (lebih kompatibel daripada VP9)
                               $"-preset veryfast " +                // Fast encoding
                               $"-crf 28 " +                         // Constant Rate Factor (18-28 good, 28 lebih kecil)
                               $"-vf \"scale={screenWidth}:{screenHeight}:force_original_aspect_ratio=increase,crop={screenWidth}:{screenHeight}\" " + // Scale & crop
                               $"-r 60 " +                           // 30 FPS
                               $"-an " +                             // No audio (lebih ringan)
                               $"-movflags +faststart " +            // Fast start untuk streaming
                               $"-y \"{outputPath}\"";               // Overwrite output

                var psi = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    if (process == null) return null;

                    // Read output untuk monitoring (opsional)
                    var errorOutput = process.StandardError.ReadToEnd();
                    process.WaitForExit(60000); // Max 60 detik

                    if (process.ExitCode != 0)
                    {
                        Debug.WriteLine($"FFmpeg error: {errorOutput}");
                        return null;
                    }
                }

                if (File.Exists(outputPath))
                    return outputPath;

                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error optimizing video: {ex.Message}");
                return null;
            }
        }

        private void LaunchVideoWallpaper(string videoPath)
        {
            try
            {
                // Option 1: Menggunakan Windows Media Player sebagai wallpaper layer
                // Note: Untuk implementasi penuh, Anda butuh aplikasi terpisah
                // atau library seperti mpv/vlc dengan --no-video-deco flag
                
                // Untuk sekarang, kita bisa menunjukkan file location
                var message = $"Video telah dioptimasi dan disimpan di:\n{videoPath}\n\n" +
                             "Untuk menggunakan sebagai wallpaper:\n" +
                             "1. Gunakan aplikasi seperti Lively Wallpaper (gratis di Microsoft Store)\n" +
                             "2. Atau gunakan VLC: Media → Open File → Tools → Effects → Advanced → Wall\n" +
                             "3. Atau mpv dengan: mpv --loop --no-border --ontop \"" + videoPath + "\"";
                
                // Auto-copy path to clipboard
                System.Windows.Clipboard.SetText(videoPath);
                
                System.Windows.MessageBox.Show(
                    message + "\n\n📋 Path sudah dicopy ke clipboard!",
                    "Video Wallpaper Ready",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error launching video wallpaper: {ex.Message}");
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
            
            var ext = Path.GetExtension(imagePath).ToLowerInvariant();
            var isVideo = new[] { ".mp4", ".wmv", ".mov", ".avi" }.Contains(ext);
            
            try
            {
                // Untuk video, ekstrak first frame atau pakai placeholder
                if (isVideo)
                {
                    var videoThumb = ExtractVideoThumbnail(imagePath);
                    return videoThumb ?? CreatePlaceholderBitmap(WallpaperType.Video);
                }
                
                // Untuk image biasa
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
            catch { return CreatePlaceholderBitmap(WallpaperType.Image); }
        }

        private BitmapImage? ExtractVideoThumbnail(string videoPath)
        {
            try
            {
                var ffmpegPath = FindFFmpeg();
                if (string.IsNullOrEmpty(ffmpegPath))
                    return null;

                string tempImagePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"thumb_{Guid.NewGuid():N}.jpg");
                
                // Optimized FFmpeg arguments untuk thumbnail yang cepat
                // -ss 00:00:01 = seek to 1 second
                // -vframes 1 = extract only 1 frame
                // -vf scale=200:-1 = scale width to 200px, maintain aspect ratio
                // -q:v 5 = quality (2-31, lower = better quality, 5 is good balance)
                var arguments = $"-ss 00:00:01 -i \"{videoPath}\" -vframes 1 -vf scale=200:-1 -q:v 5 \"{tempImagePath}\" -y";
                
                var psi = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    process?.WaitForExit(3000); // 3 second timeout
                }

                if (File.Exists(tempImagePath))
                {
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(tempImagePath);
                        bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.DecodePixelWidth = 200;
                        bitmap.EndInit();
                        bitmap.Freeze();
                        
                        // Clean up temp file after a delay
                        Task.Delay(500).ContinueWith(_ => 
                        {
                            try { File.Delete(tempImagePath); } catch { }
                        });
                        
                        return bitmap;
                    }
                    catch
                    {
                        try { File.Delete(tempImagePath); } catch { }
                    }
                }
            }
            catch { }

            return null;
        }

        private string? FindFFmpeg()
        {
            try
            {
                var pathEnv = Environment.GetEnvironmentVariable("PATH");
                if (!string.IsNullOrEmpty(pathEnv))
                {
                    foreach (var dir in pathEnv.Split(';'))
                    {
                        var ffmpegPath = System.IO.Path.Combine(dir, "ffmpeg.exe");
                        if (File.Exists(ffmpegPath))
                            return ffmpegPath;
                    }
                }

                // Cek di folder project dulu (FFMPEG di folder yang sama dengan exe)
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var projectFFmpeg = System.IO.Path.Combine(baseDir, "FFMPEG", "ffmpeg.exe");
                
                var commonPaths = new[]
                {
                    projectFFmpeg,                                  // c:\ZeroMix\ZeroMix\FFMPEG\ffmpeg.exe
                    "ffmpeg.exe",                                   // Current directory
                    "C:\\ffmpeg\\bin\\ffmpeg.exe",                 // Default install location
                    "C:\\Program Files\\ffmpeg\\bin\\ffmpeg.exe"   // Program Files location
                };

                foreach (var path in commonPaths)
                {
                    if (File.Exists(path))
                        return path;
                }
            }
            catch { }

            return null;
        }

        private BitmapImage CreatePlaceholderBitmap(WallpaperType type)
        {
            // Create visual with text
            var canvas = new System.Windows.Shapes.Rectangle
            {
                Width = 200,
                Height = 125,
                Fill = new SolidColorBrush(type == WallpaperType.Video ? System.Windows.Media.Colors.DarkRed : System.Windows.Media.Colors.DarkBlue)
            };

            var grid = new Grid { Width = 200, Height = 125 };
            grid.Children.Add(canvas);

            var textBlock = new System.Windows.Controls.TextBlock
            {
                Text = type == WallpaperType.Video ? "🎬 Video" : "✨ Animated",
                FontSize = 16,
                FontWeight = System.Windows.FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Colors.White),
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
            };
            grid.Children.Add(textBlock);

            var renderTargetBitmap = new RenderTargetBitmap(200, 125, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            renderTargetBitmap.Render(grid);
            
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
            var ms = new System.IO.MemoryStream();
            encoder.Save(ms);
            ms.Seek(0, System.IO.SeekOrigin.Begin);
            bitmap.StreamSource = ms;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            
            return bitmap;
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
                
                // Try multiple paths
                var baseDirs = new[]
                {
                    AppDomain.CurrentDomain.BaseDirectory,
                    System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                    System.Environment.CurrentDirectory
                };

                foreach (var baseDir in baseDirs)
                {
                    if (baseDir == null) continue;
                    
                    string resourceRoot = System.IO.Path.Combine(baseDir, "Resource");
                    if (Directory.Exists(resourceRoot))
                    {
                        var dirs = new[] { 
                            System.IO.Path.Combine(resourceRoot, "Images"), 
                            System.IO.Path.Combine(resourceRoot, "anim"), 
                            System.IO.Path.Combine(resourceRoot, "Video") 
                        };
                        
                        foreach (var dir in dirs)
                        {
                            if (!Directory.Exists(dir)) continue;
                            foreach (var file in Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories))
                            {
                                if (exts.Contains(System.IO.Path.GetExtension(file)))
                                    uniquePaths.Add(file);
                            }
                        }
                        
                        if (uniquePaths.Count > 0)
                            return uniquePaths;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error enumerating resources: {ex.Message}");
            }
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
