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
                    ImageSource? bitmap = null;

                    // Try Shell Thumbnail for videos or even images for better quality/speed
                    if (type == WallpaperType.Video)
                    {
                         System.Windows.Application.Current.Dispatcher.Invoke(() => 
                         {
                             bitmap = GetShellThumbnail(imagePath);
                         });
                    }
                    
                    if (bitmap == null)
                    {
                         bitmap = CreateBitmapFromPath(imagePath);
                    }
                    
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

        private ImageSource? GetShellThumbnail(string path)
        {
            try
            {
                // Unmanaged resource usage to get the thumbnail from Windows Shell
                // IShellItem2 guid
                Guid shellItem2Guid = new Guid("7e9fb0d3-919f-4307-ab2e-9b1860310c93"); 
                int ret = NativeMethods.SHCreateItemFromParsingName(path, IntPtr.Zero, shellItem2Guid, out NativeMethods.IShellItem? nativeItem);
                
                if (ret == 0 && nativeItem != null)
                {
                    var imageFactory = nativeItem as NativeMethods.IShellItemImageFactory;
                    if (imageFactory != null)
                    {
                        var size = new NativeMethods.SIZE { cx = 256, cy = 144 }; // 16:9 thumbnail
                        imageFactory.GetImage(size, 0, out IntPtr hBitmap);
                        
                        if (hBitmap != IntPtr.Zero)
                        {
                            var imageSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                hBitmap,
                                IntPtr.Zero,
                                Int32Rect.Empty,
                                BitmapSizeOptions.FromEmptyOptions());
                            
                            NativeMethods.DeleteObject(hBitmap);
                            imageSource.Freeze();
                            return imageSource;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Shell thumbnail error: {ex.Message}");
            }
            return null;
        }

        private BitmapImage? CreatePlaceholderBitmap(WallpaperType type)
        {
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
                    
                    // Simple styling 
                    var color = type == WallpaperType.Video ? System.Windows.Media.Brushes.Cyan : System.Windows.Media.Brushes.Gray;
                    
                    // Draw a play icon style triangle for video if text fails or to keep it simple
                    if (type == WallpaperType.Video)
                    {
                        var playFigure = new PathGeometry();
                        playFigure.Figures.Add(new PathFigure(
                            new System.Windows.Point(width / 2 - 10, height / 2 - 15), 
                            new[] { 
                                new LineSegment(new System.Windows.Point(width / 2 - 10, height / 2 + 15), true), 
                                new LineSegment(new System.Windows.Point(width / 2 + 15, height / 2), true) 
                            }, 
                            true));
                        context.DrawGeometry(color, null, playFigure);
                    }
                    else
                    {
                        // Draw file icon shape
                        context.DrawRectangle(null, new System.Windows.Media.Pen(color, 2), new Rect(width/2 - 15, height/2 - 20, 30, 40));
                    }
                }
                
                bmp.Render(visual);
                bmp.Freeze();
                
                // Convert to BitmapImage
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
            catch (Exception ex)
            {
                Debug.WriteLine($"Placeholder error: {ex.Message}");
                return null; 
            }
        }

        private IEnumerable<string> EnumerateResourceImagesOnDisk()
        {
            var uniqueFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            try
            {
                var exts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".mp4", ".wmv", ".mov" };
                
                // Scan directories
                var sourceDir = @"C:\ZeroMix\ZeroMix\Resource";
                var videoDir = @"C:\ZeroMix\ZeroMix\Resource\Video"; // Explicitly add Video folder
                var buildDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resource");
                
                var roots = new[] { videoDir, sourceDir, buildDir }; // Priority to Video folder

                foreach (var root in roots)
                {
                    if (Directory.Exists(root))
                    {
                        var files = Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
                                    .Where(f => exts.Contains(Path.GetExtension(f)));
                        
                        foreach (var f in files)
                        {
                            var fileName = Path.GetFileName(f);
                            // Only add if not exists, respecting priority
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

        // SHELL THUMBNAIL SUPPORT
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
        public static extern int SHCreateItemFromParsingName(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            [In] IntPtr pbc,
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [Out, MarshalAs(UnmanagedType.Interface, IidParameterIndex = 2)] out IShellItem ppv);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
        public interface IShellItem
        {
            void BindToHandler([In, MarshalAs(UnmanagedType.Interface)] IntPtr pbc, [In] ref Guid bhid, [In] ref Guid riid, out IntPtr ppv);
            void GetParent([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
            void GetDisplayName([In] uint sigdnName, out IntPtr ppszName);
            void GetAttributes([In] uint sfgaoMask, out uint psfgaoAttribs);
            void Compare([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, [In] uint hint, out int piOrder);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
        public interface IShellItemImageFactory
        {
            void GetImage(
                [In, MarshalAs(UnmanagedType.Struct)] SIZE size,
                [In] int flags,
                [Out] out IntPtr phbm);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SIZE
        {
            public int cx;
            public int cy;
        }
    }
}
