// File: SearchOverlay.xaml.cs
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace ZeroSrc
{
    public enum NotificationType
    {
        Info,
        Warning,
        Error
    }

    public partial class SearchOverlay : Window
    {
        private bool _isClosing = false;
        private readonly Dictionary<string, string> _appShortcuts;

        public SearchOverlay()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            _appShortcuts = GetStartMenuShortcuts();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var searchBox = this.FindName("SearchBox") as System.Windows.Controls.TextBox;
            searchBox?.Focus();
            var fadeIn = (Storyboard)FindResource("FadeInStoryboard");
            fadeIn.Begin(this);
        }

        private void BeginFadeOutAndClose()
        {
            if (_isClosing) return;
            _isClosing = true;
            var fadeOut = (Storyboard)FindResource("FadeOutStoryboard");
            fadeOut.Completed += (s, e) => this.Close();
            fadeOut.Begin(this);
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            var searchBox = sender as System.Windows.Controls.TextBox;
            if (e.Key == Key.Enter)
            {
                string query = searchBox?.Text.Trim().ToLower() ?? string.Empty;
                if (!string.IsNullOrEmpty(query))
                {
                    try
                    {
                        if (query == "desktop shortcuts" || query == "open desktop shortcuts" || query == "buka semua shortcut")
                        {
                            OpenAllDesktopShortcuts();
                        }
                        else if (_appShortcuts.TryGetValue(query, out string shortcutPath))
                        {
                            Process.Start(new ProcessStartInfo(shortcutPath) { UseShellExecute = true });
                        }
                        else
                        {
                            string url = $"https://www.google.com/search?q={Uri.EscapeDataString(query)}";
                            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Gagal menjalankan perintah: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                BeginFadeOutAndClose();
            }
            else if (e.Key == Key.Escape)
            {
                BeginFadeOutAndClose();
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void OpenAllDesktopShortcuts()
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var shortcutFiles = Directory.GetFiles(desktopPath, "*.lnk");
            foreach (var shortcut in shortcutFiles)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = shortcut,
                        UseShellExecute = true
                    });
                }
                catch (Exception)
                {
                    MessageBox.Show($"Gagal membuka: {Path.GetFileName(shortcut)}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private Dictionary<string, string> GetStartMenuShortcuts()
        {
            var shortcuts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string[] startMenuPaths = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
                Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)
            };

            foreach (string basePath in startMenuPaths)
            {
                if (Directory.Exists(basePath))
                {
                    var files = Directory.GetFiles(basePath, "*.lnk", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        string name = Path.GetFileNameWithoutExtension(file);
                        if (!shortcuts.ContainsKey(name.ToLower()))
                            shortcuts.Add(name.ToLower(), file);
                    }
                }
            }
            return shortcuts;
        }

        public void BeginFadeOutAndCloseByMain()
        {
            BeginFadeOutAndClose();
        }
    }
}
