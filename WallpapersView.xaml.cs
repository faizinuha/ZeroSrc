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
    public partial class WallpapersView : System.Windows.Controls.UserControl
    {
        private string? _selectedImagePath;
        private ObservableCollection<WallpaperItem> _allWallpapers = new();
        private System.ComponentModel.ICollectionView _wallpaperView;

        public WallpapersView()
        {
            InitializeComponent();
            _wallpaperView = System.Windows.Data.CollectionViewSource.GetDefaultView(_allWallpapers);
            _wallpaperView.Filter = FilterCallback;
            WallpaperListPanel.ItemsSource = _wallpaperView;
        }

        private bool FilterCallback(object item)
        {
            if (item is WallpaperItem wp)
            {
                bool matchesSearch = true;
                if (!string.IsNullOrWhiteSpace(SearchBox.Text))
                {
                    matchesSearch = wp.Name.Contains(SearchBox.Text, StringComparison.OrdinalIgnoreCase);
                }

                if (!matchesSearch) return false;

                if (FilterAll.IsChecked == true) return true;
                if (FilterImages.IsChecked == true) return wp.Type == WallpaperType.Image;
                if (FilterVideos.IsChecked == true) return wp.Type == WallpaperType.Video;
            }
            return true;
        }

        private void Filter_Click(object sender, RoutedEventArgs e)
        {
            _wallpaperView.Refresh();
            UpdateCount();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _wallpaperView.Refresh();
            UpdateCount();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (_allWallpapers.Count == 0) // Only load if not already loaded
            {
                await LoadWallpapersAsync();
            }
        }

        // --- LOAD WALLPAPERS ---
        private async Task LoadWallpapersAsync()
        {
            StatusLabel.Text = "Scanning Wallpaper Nexus...";
            
            await Task.Run(() => 
            {
                // Run heavy file ops in background
                System.Windows.Application.Current.Dispatcher.Invoke(() => _allWallpapers.Clear());

                // 1. Load current wallpaper (System)
                try
                {
                    string? currentPath = GetCurrentWallpaperPath();
                    if (!string.IsNullOrEmpty(currentPath) && File.Exists(currentPath))
                    {
                        var bitmap = CreateBitmapFromPath(currentPath);
                        if (bitmap != null)
                        {
                            System.Windows.Application.Current.Dispatcher.Invoke(() => 
                            {
                                _allWallpapers.Add(new WallpaperItem
                                {
                                    Name = "Current Wallpaper",
                                    Path = currentPath,
                                    Thumbnail = bitmap,
                                    Type = WallpaperType.Image,
                                    IsSelected = false
                                });
                            });
                        }
                    }
                }
                catch { }

                // 2. Load from disk
                var diskWallpapers = EnumerateResourceImagesOnDisk();
                foreach (var imagePath in diskWallpapers)
                {
                    var type = DetermineWallpaperType(imagePath);
                    ImageSource? bitmap = CreateBitmapFromPath(imagePath);
                    
                    if (bitmap == null && type != WallpaperType.Image)
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() => 
                        {
                            bitmap = CreatePlaceholderBitmap(type);
                        });
                    }
                    
                    if (bitmap != null)
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() => 
                        {
                            _allWallpapers.Add(new WallpaperItem
                            {
                                Name = Path.GetFileNameWithoutExtension(imagePath),
                                Path = imagePath,
                                Thumbnail = bitmap,
                                Type = type,
                                IsSelected = false
                            });
                        });
                    }
                }

                // 3. Update Count
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    UpdateCount();
                    StatusLabel.Text = "Assets Loaded. Ready.";
                });
            });
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
            foreach (var wp in _allWallpapers)
                wp.IsSelected = false;

            wallpaper.IsSelected = true;
            _selectedImagePath = wallpaper.Path;
            StatusLabel.Text = $"Selected: {wallpaper.Name}";
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

        private static VideoWallpaperWindow? _videoWallpaperWindow;

        private async void SetWallpaperButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedImagePath) || !File.Exists(_selectedImagePath))
            {
                System.Windows.MessageBox.Show("Please select a wallpaper first!", "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var type = DetermineWallpaperType(_selectedImagePath);

            try
            {
                SetWallpaperButton.IsEnabled = false;
                
                if (type == WallpaperType.Video)
                {
                    StatusLabel.Text = "🎬 Processing video...";
                    
                    string? optimizedPath = await Task.Run(() => 
                    {
                        var ffmpegPath = FindFFmpeg();
                        if (string.IsNullOrEmpty(ffmpegPath)) return null;
                        
                        return OptimizeVideoForWallpaper(_selectedImagePath, ffmpegPath);
                    });
                    
                    if (string.IsNullOrEmpty(optimizedPath))
                    {
                        StatusLabel.Text = "❌ Video processing failed";
                        System.Windows.MessageBox.Show("Failed to process video. Check FFmpeg.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    
                    StatusLabel.Text = "✅ Video Ready. Launching...";
                    LaunchVideoWallpaper(optimizedPath);
                }
                else
                {
                    StatusLabel.Text = "Applying Wallpaper...";
                    
                    // Stop video if running
                    StopVideoWallpaper();
                    
                    await Task.Run(() => NativeMethods.SetWallpaper(_selectedImagePath));
                    StatusLabel.Text = "✅ Wallpaper Successfully Applied!";
                }
            }
            catch (Exception ex)
            {
                StatusLabel.Text = "❌ Error";
                System.Windows.MessageBox.Show($"Error: {ex.Message}");
            }
            finally
            {
                SetWallpaperButton.IsEnabled = true;
            }
        }

        private void LaunchVideoWallpaper(string path)
        {
            StopVideoWallpaper();
            
            _videoWallpaperWindow = new VideoWallpaperWindow(path);
            _videoWallpaperWindow.Show();
        }

        private void StopVideoWallpaper()
        {
            if (_videoWallpaperWindow != null)
            {
                _videoWallpaperWindow.Close();
                _videoWallpaperWindow = null;
            }
        }
        
        private string? OptimizeVideoForWallpaper(string inputPath, string ffmpegPath)
        {
            try
            {
                var roamingDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ZeroMix", "Temp");
                Directory.CreateDirectory(roamingDir);
                
                var fileName = Path.GetFileNameWithoutExtension(inputPath);
                var outputPath = Path.Combine(roamingDir, $"{fileName}_optimized.mp4");

                if (File.Exists(outputPath)) return outputPath;

                // Simple copy if optimization is too complex for this context, or implement full ffmpeg command
                // For now, let's just return inputPath if we can't optimize, or try a copy.
                // But the original code had resize/crop. 
                // Let's just return inputPath to be safe and fast if optimization fails, 
                // or assume input is okay if FFMPEG fails.
                
                // Full logic from original file:
                 var arguments = $"-i \"{inputPath}\" -vf \"scale=1920:1080:force_original_aspect_ratio=increase,crop=1920:1080,fps=60\" -c:v libx264 -preset fast -crf 20 -an -y \"{outputPath}\"";
                 var psi = new ProcessStartInfo
                 {
                     FileName = ffmpegPath,
                     Arguments = arguments,
                     UseShellExecute = false,
                     CreateNoWindow = true
                 };
                 using (var p = Process.Start(psi)) {
                     p?.WaitForExit(60000);
                 }
                 
                 return File.Exists(outputPath) ? outputPath : inputPath;
            }
            catch { return inputPath; }
        }

        private string? FindFFmpeg()
        {
            // Simplified search
            var possiblePaths = new[] {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FFMPEG", "ffmpeg.exe"),
                @"C:\ffmpeg\bin\ffmpeg.exe"
            };
            return possiblePaths.FirstOrDefault(File.Exists);
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
            try
            {
                // Must be created on UI thread if we want to use it easily, or frozen
                // Since this runs in Task.Run above, we need to freeze it
                if (!File.Exists(imagePath)) return null;
                
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath);
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = 250; // Use small thumbnail
                bitmap.EndInit();
                bitmap.Freeze(); 
                return bitmap;
            }
            catch { return null; }
        }

        private BitmapImage? CreatePlaceholderBitmap(WallpaperType type)
        {
            // Create a simple generated bitmap for placeholders (especially videos)
            try 
            {
                var width = 240;
                var height = 135;
                var bmp = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                
                var visual = new DrawingVisual();
                using (var context = visual.RenderOpen())
                {
                    // Background
                    context.DrawRectangle(new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 35)), null, new Rect(0, 0, width, height));
                    
                    // Label
                    var color = type == WallpaperType.Video ? System.Windows.Media.Brushes.Cyan : System.Windows.Media.Brushes.Gray;
                    var text = type == WallpaperType.Video ? "▶ VIDEO" : "FILE";
                    
                    var formattedText = new FormattedText(
                        text,
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Windows.FlowDirection.LeftToRight,
                        new Typeface("Segoe UI"),
                        24,
                        color,
                        1.25);
                        
                    context.DrawText(formattedText, new System.Windows.Point((width - formattedText.Width) / 2, (height - formattedText.Height) / 2));
                }
                
                bmp.Render(visual);
                bmp.Freeze();
                
                // Convert RenderTargetBitmap to BitmapImage (or just return null and change property type, but let's try to convert/wrap)
                // Actually, WallpaperItem.Thumbnail is BitmapImage? so we might need to change it to ImageSource to accept RenderTargetBitmap
                // Let's quickly change WallpaperItem.Thumbnail type to ImageSource in the Helper Class below
                
                return null; // Return null here, I will change the WallpaperItem definition to acceptable ImageSource.
                // Wait, I can't change the return type of this method easily if it's strictly defined above without full rewrite.
                // Let's output a stream-based bitmap image from the render target.
                
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));
                using (var stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    stream.Position = 0;
                    var result = new BitmapImage();
                    result.BeginInit();
                    result.CacheOption = BitmapCacheOption.OnLoad;
                    result.StreamSource = stream;
                    result.EndInit();
                    result.Freeze();
                    return result;
                }
            }
            catch { return null; }
        }

        private IEnumerable<string> EnumerateResourceImagesOnDisk()
        {
            var uniqueFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            try
            {
                var exts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".bmp", ".mp4" };
                
                // Scan directories in order of priority (Source first, then Build)
                // This ensures we get the "source of truth" if possible
                var sourceDir = @"C:\ZeroMix\ZeroMix\Resource";
                var buildDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resource");

                var roots = new[] { sourceDir, buildDir };

                foreach (var root in roots)
                {
                    if (Directory.Exists(root))
                    {
                        var files = Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
                                    .Where(f => exts.Contains(Path.GetExtension(f)));
                        
                        foreach (var f in files)
                        {
                            var fileName = Path.GetFileName(f);
                            if (!uniqueFiles.ContainsKey(fileName))
                            {
                                uniqueFiles[fileName] = f;
                            }
                        }
                    }
                }
            }
            catch {}
            
            return uniqueFiles.Values.ToList();
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
    }

    // --- HELPER CLASSES ---
    public enum WallpaperType { Image, Video, Animated }

    public class WallpaperItem : System.ComponentModel.INotifyPropertyChanged
    {
        private string _name = "";
        private string _path = "";
        private ImageSource? _thumbnail; // Changed from BitmapImage to ImageSource to be more flexible
        private WallpaperType _type;
        private bool _isSelected;

        public string Name 
        { 
            get => _name; 
            set { _name = value; OnPropertyChanged(); } 
        }
        
        public string Path 
        { 
            get => _path; 
            set { _path = value; OnPropertyChanged(); } 
        }

        public ImageSource? Thumbnail  // Changed from BitmapImage
        { 
            get => _thumbnail; 
            set { _thumbnail = value; OnPropertyChanged(); } 
        }

        public WallpaperType Type 
        { 
            get => _type; 
            set { _type = value; OnPropertyChanged(); } 
        }

        public bool IsSelected 
        { 
            get => _isSelected; 
            set { _isSelected = value; OnPropertyChanged(); } 
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
        }
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
