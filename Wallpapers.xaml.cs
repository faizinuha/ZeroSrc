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
        private static VideoWallpaperWindow? _videoWallpaperWindow;

        public Wallpapers()
        {
            InitializeComponent();
            WallpaperListPanel.ItemsSource = _allWallpapers;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadWallpapersAsync();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

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

            UpdateCount();
        }

        private void UpdateCount()
        {
            CountLabel.Text = $"{_allWallpapers.Count} items";
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
            WallpaperListPanel.ItemsSource = _allWallpapers;
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
                    UpdateCount();
                }
            }
        }

        private async void SetWallpaperButton_Click(object sender, RoutedEventArgs e)
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
                // Disable button saat processing
                SetWallpaperButton.IsEnabled = false;
                
                if (isVideo)
                {
                    StatusLabel.Text = "🎬 Processing video wallpaper...";
                    
                    // Jalankan di background thread
                    string? optimizedPath = await Task.Run(() => 
                    {
                        var ffmpegPath = FindFFmpeg();
                        if (string.IsNullOrEmpty(ffmpegPath))
                            return null;
                        
                        return OptimizeVideoForWallpaper(_selectedImagePath, ffmpegPath);
                    });
                    
                    if (string.IsNullOrEmpty(optimizedPath))
                    {
                        StatusLabel.Text = "❌ Video processing failed";
                        System.Windows.MessageBox.Show(
                            "Gagal memproses video. Pastikan FFmpeg tersedia dan file video valid.",
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return;
                    }
                    
                   StatusLabel.Text = "✅ Video optimized successfully!";
 
// Ask user
var result = System.Windows.MessageBox.Show(
    $"Video siap! Jalankan video wallpaper sekarang?\\n\\n" +
    $"Path: {optimizedPath}",
    "Launch Video Wallpaper?",
    MessageBoxButton.YesNo,
    MessageBoxImage.Question);

if (result == MessageBoxResult.Yes)
{
    LaunchVideoWallpaper(optimizedPath);
}
                }
                else
                {
                    StatusLabel.Text = "🖼️ Setting image wallpaper...";
                    
                    // Stop video wallpaper if running
                    StopVideoWallpaper();
                    
                    // Set image wallpaper (cepat, tidak perlu async)
                    NativeMethods.SetWallpaper(_selectedImagePath);
                    
                    StatusLabel.Text = "✅ Wallpaper set successfully!";
                    
                    System.Windows.MessageBox.Show("Wallpaper berhasil diubah!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                StatusLabel.Text = "❌ Error setting wallpaper";
                System.Windows.MessageBox.Show($"Gagal mengubah wallpaper: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Re-enable button
                SetWallpaperButton.IsEnabled = true;
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

                // Delete existing file if exists (prevent FFmpeg error)
                if (File.Exists(outputPath))
                {
                    try
                    {
                        File.Delete(outputPath);
                        Debug.WriteLine($"Deleted existing file: {outputPath}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Warning: Could not delete existing file: {ex.Message}");
                    }
                }

                // Get screen resolution
                var screenWidth = (int)SystemParameters.PrimaryScreenWidth;
                var screenHeight = (int)SystemParameters.PrimaryScreenHeight;

               var arguments = 
      $"-i \"{inputPath}\" " +
      $"-vf \"scale={screenWidth}:{screenHeight}:force_original_aspect_ratio=increase," +
      $"crop={screenWidth}:{screenHeight},fps=60\" " +
      $"-c:v libx264 " +
      $"-preset fast " +
      $"-crf 20 " +
      $"-tune film " +
      $"-pix_fmt yuv420p " +
      $"-an " +
      $"-movflags +faststart " +
      $"-y \"{outputPath}\"";

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

                    var errorOutput = process.StandardError.ReadToEnd();
                    process.WaitForExit(90000); // Max 90 detik (lebih lama untuk video besar)

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
                if (isVideo)
                {
                    var videoThumb = ExtractVideoThumbnail(imagePath);
                    return videoThumb ?? CreatePlaceholderBitmap(WallpaperType.Video);
                }
                
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

                string tempImagePath = Path.Combine(Path.GetTempPath(), $"thumb_{Guid.NewGuid():N}.jpg");
                
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
                    process?.WaitForExit(3000);
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
                Debug.WriteLine("=== Searching for FFmpeg ===");
                
                // Priority 1: Development - Project FFMPEG folder
                // C:\ZeroMix\ZeroMix\bin\Debug\net9.0-windows\FFMPEG\ffmpeg.exe
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                Debug.WriteLine($"Base Directory: {baseDir}");
                
                var localFFmpeg = Path.Combine(baseDir, "FFMPEG", "ffmpeg.exe");
                Debug.WriteLine($"Checking local: {localFFmpeg}");
                if (File.Exists(localFFmpeg))
                {
                    Debug.WriteLine($"✓ Found FFmpeg in bin directory: {localFFmpeg}");
                    return localFFmpeg;
                }

                // Priority 2: Development - Go up to project root
                // C:\ZeroMix\ZeroMix\FFMPEG\ffmpeg.exe
                var projectRoot = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(baseDir)));
                if (!string.IsNullOrEmpty(projectRoot))
                {
                    var projectFFmpeg = Path.Combine(projectRoot, "FFMPEG", "ffmpeg.exe");
                    Debug.WriteLine($"Checking project root: {projectFFmpeg}");
                    if (File.Exists(projectFFmpeg))
                    {
                        Debug.WriteLine($"✓ Found FFmpeg in project root: {projectFFmpeg}");
                        return projectFFmpeg;
                    }
                }

                // Priority 3: Hardcoded development path
                var devPath = @"C:\ZeroMix\ZeroMix\FFMPEG\ffmpeg.exe";
                Debug.WriteLine($"Checking hardcoded dev path: {devPath}");
                if (File.Exists(devPath))
                {
                    Debug.WriteLine($"✓ Found FFmpeg in dev path: {devPath}");
                    return devPath;
                }

                // Priority 4: Installed location (C:\Program Files\ZeroMix\FFMPEG\ffmpeg.exe)
                var programFilesFFmpeg = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "ZeroMix", "FFMPEG", "ffmpeg.exe"
                );
                Debug.WriteLine($"Checking Program Files: {programFilesFFmpeg}");
                if (File.Exists(programFilesFFmpeg))
                {
                    Debug.WriteLine($"✓ Found FFmpeg in Program Files: {programFilesFFmpeg}");
                    return programFilesFFmpeg;
                }

                // Priority 5: Check PATH environment variable
                var pathEnv = Environment.GetEnvironmentVariable("PATH");
                if (!string.IsNullOrEmpty(pathEnv))
                {
                    Debug.WriteLine("Checking PATH environment variable...");
                    foreach (var dir in pathEnv.Split(';'))
                    {
                        if (string.IsNullOrWhiteSpace(dir)) continue;
                        
                        var ffmpegPath = Path.Combine(dir.Trim(), "ffmpeg.exe");
                        if (File.Exists(ffmpegPath))
                        {
                            Debug.WriteLine($"✓ Found FFmpeg in PATH: {ffmpegPath}");
                            return ffmpegPath;
                        }
                    }
                }

                // Priority 6: Common installation paths
                var commonPaths = new[]
                {
                    @"C:\ffmpeg\bin\ffmpeg.exe",
                    @"C:\Program Files\ffmpeg\bin\ffmpeg.exe",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ffmpeg", "bin", "ffmpeg.exe")
                };

                Debug.WriteLine("Checking common paths...");
                foreach (var path in commonPaths)
                {
                    Debug.WriteLine($"Checking: {path}");
                    if (File.Exists(path))
                    {
                        Debug.WriteLine($"✓ Found FFmpeg in common path: {path}");
                        return path;
                    }
                }

                Debug.WriteLine("✗ FFmpeg not found in any location");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"✗ Error finding FFmpeg: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            return null;
        }

        private BitmapImage CreatePlaceholderBitmap(WallpaperType type)
        {
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
                
                var baseDirs = new[]
                {
                    AppDomain.CurrentDomain.BaseDirectory,
                    Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                    Environment.CurrentDirectory
                };

                foreach (var baseDir in baseDirs)
                {
                    if (baseDir == null) continue;
                    
                    string resourceRoot = Path.Combine(baseDir, "Resource");
                    if (Directory.Exists(resourceRoot))
                    {
                        var dirs = new[] { 
                            Path.Combine(resourceRoot, "Images"), 
                            Path.Combine(resourceRoot, "anim"), 
                            Path.Combine(resourceRoot, "Video") 
                        };
                        
                        foreach (var dir in dirs)
                        {
                            if (!Directory.Exists(dir)) continue;
                            foreach (var file in Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories))
                            {
                                if (exts.Contains(Path.GetExtension(file)))
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
