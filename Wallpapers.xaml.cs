using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ZeroMix
{
    public partial class Wallpapers : Window
    {
        private string? _selectedImagePath;

        public Wallpapers()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadWallpapersAsync();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select a Wallpaper Image",
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedImagePath = openFileDialog.FileName;
                var bitmap = CreateBitmapFromPath(_selectedImagePath);
                if (bitmap != null)
                {
                    var wallpaperElement = CreateWallpaperElement(bitmap, _selectedImagePath);
                    WallpaperListPanel.Children.Insert(0, wallpaperElement);
                    MessageBox.Show("Image selected. Click 'Set as Wallpaper' to apply.", "Image Ready", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        /// <summary>
        /// Sets the selected image as the desktop wallpaper.
        /// </summary>
        private void SetWallpaperButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedImagePath) || !File.Exists(_selectedImagePath))
            {
                MessageBox.Show("Please select a valid image first.", "No Image Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                NativeMethods.SetWallpaper(_selectedImagePath);
                MessageBox.Show("Wallpaper has been successfully changed!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to set wallpaper: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"[ERROR] SetWallpaper: {ex}");
            }
        }

        /// <summary>
        /// Asynchronously loads the current desktop wallpaper and default wallpapers from resources.
        /// </summary>
        private async Task LoadWallpapersAsync()
        {
            // 1. Load current desktop wallpaper
            try
            {
                string? currentWallpaperPath = GetCurrentWallpaperPath();
                if (!string.IsNullOrEmpty(currentWallpaperPath) && File.Exists(currentWallpaperPath))
                {
                    var bitmap = CreateBitmapFromPath(currentWallpaperPath);
                    if (bitmap != null)
                    {
                        var element = CreateWallpaperElement(bitmap, currentWallpaperPath);
                        WallpaperListPanel.Children.Add(element);
                        await Task.Delay(20); // Yield to UI thread
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Failed to load current wallpaper: {ex.Message}");
            }

            // 2. Load default wallpapers from resource folders
            var resourceImagePaths = new List<string>
            {
                // Images from Resource/images
                "Resource/Images/1.jpg", "Resource/Images/2.jpg", "Resource/Images/3.jpg",
                "Resource/Images/4.jpg", "Resource/Images/5.jpg", "Resource/Images/6.jpg", "Resource/Images/7.jpg",
                // Images from Resource/anim
                "Resource/anim/Fieren.jpg", "Resource/anim/Fieren2.jpg", "Resource/anim/Fieren3.jpg",
                "Resource/anim/view.jpg", "Resource/anim/anime.jpg"
            };

            foreach (var path in resourceImagePaths)
            {
                try
                {
                    var uri = new Uri($"pack://application:,,,/{path}");
                    var bitmap = CreateBitmapFromUri(uri);
                    if (bitmap != null)
                    {
                        var element = CreateWallpaperElement(bitmap, uri.ToString());
                        WallpaperListPanel.Children.Add(element);
                        await Task.Delay(20); // Yield to allow UI to update
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ERROR] Failed to load resource image '{path}': {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Creates a UI element (a Border containing an Image) for a given wallpaper.
        /// </summary>
        /// <param name="bitmap">The BitmapSource of the image.</param>
        /// <param name="imageIdentifier">The path or URI of the image, used for selection.</param>
        /// <returns>A Border element ready to be added to the UI.</returns>
        private Border CreateWallpaperElement(BitmapSource bitmap, string imageIdentifier)
        {
            var image = new Image
            {
                Source = bitmap,
                Style = (Style)FindResource("WallpaperImageStyle")
            };

            var border = new Border
            {
                Style = (Style)FindResource("WallpaperBorderStyle"),
                Child = image
            };

            border.MouseLeftButtonUp += (s, e) =>
            {
                HandleWallpaperSelection(bitmap, imageIdentifier);
            };

            return border;
        }

        /// <summary>
        /// Handles the logic when a user clicks on a wallpaper image.
        /// </summary>
        private void HandleWallpaperSelection(BitmapSource bitmap, string imageIdentifier)
        {
            try
            {
                // If the image is from a resource pack, save it to a temporary file
                // so the Windows API can access it.
                if (imageIdentifier.StartsWith("pack://"))
                {
                    string tempPath = Path.Combine(Path.GetTempPath(), $"zeromix_wallpaper_{Guid.NewGuid()}.jpg");
                    var encoder = new JpegBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    using (var fileStream = new FileStream(tempPath, FileMode.Create))
                    {
                        encoder.Save(fileStream);
                    }
                    _selectedImagePath = tempPath;
                    MessageBox.Show($"Selected: {Path.GetFileName(imageIdentifier)}. Click 'Set as Wallpaper' to apply.", "Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    _selectedImagePath = imageIdentifier;
                    MessageBox.Show($"Selected: {Path.GetFileName(imageIdentifier)}", "Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to prepare the selected image.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"[ERROR] HandleWallpaperSelection: {ex}");
            }
}

        /// <summary>
        /// Creates a BitmapImage from a local file path.
        /// </summary>
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
                bitmap.Freeze(); // Optimize for performance
                return bitmap;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Failed to create bitmap from path '{imagePath}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates a BitmapImage from a resource URI.
        /// </summary>
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
                bitmap.Freeze(); // Optimize for performance
                return bitmap;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Failed to create bitmap from URI '{imageUri}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Retrieves the path of the current desktop wallpaper from the registry.
        /// </summary>
        private string? GetCurrentWallpaperPath()
        {
            const string keyPath = @"Control Panel\Desktop";
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(keyPath))
            {
                return key?.GetValue("Wallpaper") as string;
            }
        }
    }

    /// <summary>
    /// Provides access to native Windows API functions.
    /// </summary>
    internal static class NativeMethods
    {
        // Imports the SystemParametersInfo function from user32.dll to set the desktop wallpaper.
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        private const int SPI_SETDESKWALLPAPER = 20;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDCHANGE = 0x02;

        /// <summary>
        /// Sets the desktop wallpaper to the image at the specified path.
        /// </summary>
        /// <param name="path">The absolute path to the image file.</param>
        public static void SetWallpaper(string path)
        {
            SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, path, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
        }
    }
}