// File: SearchOverlay.xaml.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
        private readonly List<string> _suggestions = new();

        public SearchOverlay()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            _appShortcuts = GetStartMenuShortcuts();
            
            // Inisialisasi suggestions
            _suggestions.AddRange(new[] {
                "desktop shortcuts",
                "open desktop shortcuts",
                "buka semua shortcut"
            });
            _suggestions.AddRange(_appShortcuts.Keys);
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

        private void ShowNotification(string message, NotificationType type = NotificationType.Info)
        {
            MessageBoxImage icon = MessageBoxImage.Information;
            if (type == NotificationType.Error) icon = MessageBoxImage.Error;
            else if (type == NotificationType.Warning) icon = MessageBoxImage.Warning;
            MessageBox.Show(message, type.ToString(), MessageBoxButton.OK, icon);
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchBox = sender as TextBox;
            var suggestionList = this.FindName("SuggestionList") as ListBox;
            if (searchBox == null || suggestionList == null) return;

            string query = searchBox.Text.Trim().ToLower();
            var filteredSuggestions = _suggestions
                .Where(s => s.ToLower().Contains(query))
                .Take(5)
                .ToList();

            suggestionList.ItemsSource = filteredSuggestions;
            suggestionList.Visibility = !string.IsNullOrEmpty(query) && filteredSuggestions.Any() 
                ? Visibility.Visible 
                : Visibility.Collapsed;
        }

        private void SuggestionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var suggestionList = sender as ListBox;
            var searchBox = this.FindName("SearchBox") as TextBox;
            if (suggestionList?.SelectedItem is string selectedItem && searchBox != null)
            {
                searchBox.Text = selectedItem;
                searchBox.CaretIndex = selectedItem.Length;

                // Jika suggestion berasal dari env operations, tangani khusus
                switch (selectedItem.ToLower())
                {
                    case "task manager":
                        Process.Start(new ProcessStartInfo("taskmgr") { UseShellExecute = true });
                        BeginFadeOutAndClose();
                        return;
                    case "settings":
                        Process.Start(new ProcessStartInfo("ms-settings:") { UseShellExecute = true });
                        BeginFadeOutAndClose();
                        return;
                    case "control panel":
                        Process.Start(new ProcessStartInfo("control") { UseShellExecute = true });
                        BeginFadeOutAndClose();
                        return;
                    case "device manager":
                        Process.Start(new ProcessStartInfo("devmgmt.msc") { UseShellExecute = true });
                        BeginFadeOutAndClose();
                        return;
                    case "file explorer":
                        Process.Start(new ProcessStartInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)) { UseShellExecute = true });
                        BeginFadeOutAndClose();
                        return;
                    default:
                        ExecuteCommand(selectedItem);
                        return;
                }
            }
        }

        private void SuggestionList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var lb = sender as ListBox;
            if (lb?.SelectedItem is string s)
            {
                // reuse existing selection logic
                SuggestionList_SelectionChanged(lb, new SelectionChangedEventArgs(ListBox.SelectionChangedEvent, new List<string>(), new List<string>()));
            }
        }

        private void SuggestionList_KeyDown(object sender, KeyEventArgs e)
        {
            var lb = sender as ListBox;
            if (e.Key == Key.Enter && lb?.SelectedItem is string s)
            {
                SuggestionList_SelectionChanged(lb, new SelectionChangedEventArgs(ListBox.SelectionChangedEvent, new List<string>(), new List<string>()));
            }
        }

        private void ExecuteCommand(string query)
        {
            if (string.IsNullOrEmpty(query)) return;
            try
            {
                // Perintah khusus
                if (query == "desktop shortcuts" || query == "open desktop shortcuts" || query == "buka semua shortcut")
                {
                    OpenAllDesktopShortcuts();
                    ShowNotification("Semua shortcut desktop dibuka.", NotificationType.Info);
                    return;
                }

                if (query == "all apps" || query == "installed apps" || query == "installed")
                {
                    var suggestionList = this.FindName("SuggestionList") as ListBox;
                    if (suggestionList != null)
                    {
                        var items = _appShortcuts.Keys.OrderBy(k => k).ToList();
                        suggestionList.ItemsSource = items;
                        suggestionList.Visibility = Visibility.Visible;
                    }
                    return; // jangan tutup overlay
                }

                // Jika shortcut ditemukan, buka
                if (_appShortcuts.TryGetValue(query.ToLower(), out var shortcutPath) && !string.IsNullOrEmpty(shortcutPath))
                {
                    Process.Start(new ProcessStartInfo(shortcutPath) { UseShellExecute = true });
                    ShowNotification($"Membuka: {query}", NotificationType.Info);
                    BeginFadeOutAndClose();
                    return;
                }

                // Perintah environment / operations
                if (query == "env" || query == "environment" || query == "operations")
                {
                    // Tampilkan daftar aksi yang bisa dijalankan
                    var suggestionList = this.FindName("SuggestionList") as ListBox;
                    var ops = new List<string> { "Task Manager", "Settings", "Control Panel", "Device Manager", "File Explorer" };
                    if (suggestionList != null)
                    {
                        suggestionList.ItemsSource = ops;
                        suggestionList.Visibility = Visibility.Visible;
                    }
                    return;
                }

                // Jika belum ditemukan, jangan auto-buka browser.
                ShowNotification("Tidak ditemukan shortcut. Tekan Ctrl+Enter untuk mencari di web atau pilih suggestion.", NotificationType.Warning);
                var suggestion = this.FindName("SuggestionList") as ListBox;
                if (suggestion != null)
                {
                    var items = _suggestions.Where(s => s.Contains(query)).Take(10).ToList();
                    suggestion.ItemsSource = items;
                    suggestion.Visibility = items.Any() ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                ShowNotification($"Gagal menjalankan perintah: {ex.Message}", NotificationType.Error);
            }
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            var searchBox = sender as TextBox;
            if (e.Key == Key.Enter)
            {
                string query = searchBox?.Text.Trim() ?? string.Empty;
                if (!string.IsNullOrEmpty(query))
                {
                    // Jika Ctrl+Enter -> cari di web
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                    {
                        ExecuteWebSearch(query);
                        BeginFadeOutAndClose();
                        e.Handled = true;
                        return;
                    }

                    ExecuteCommand(query.ToLower());
                }
            }
            else if (e.Key == Key.Escape)
            {
                BeginFadeOutAndClose();
            }
            else if (e.Key == Key.Down)
            {
                var suggestionList = this.FindName("SuggestionList") as ListBox;
                if (suggestionList?.HasItems == true)
                {
                    suggestionList.SelectedIndex = 0;
                    suggestionList.Focus();
                }
            }
        }

        private void ExecuteWebSearch(string query)
        {
            try
            {
                string url = $"https://www.google.com/search?q={Uri.EscapeDataString(query)}";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                ShowNotification($"Gagal membuka browser: {ex.Message}", NotificationType.Error);
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
            if (shortcutFiles.Length == 0)
            {
                ShowNotification("Tidak ada shortcut di Desktop.", NotificationType.Info);
                return;
            }

            var result = MessageBox.Show($"Akan membuka {shortcutFiles.Length} shortcut di Desktop. Lanjutkan?", "Konfirmasi", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                // buka folder desktop sebagai alternatif
                Process.Start(new ProcessStartInfo(desktopPath) { UseShellExecute = true });
                return;
            }

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
                    // jangan ganggu loop, hanya catat.
                }
            }
            ShowNotification($"Mencoba membuka {shortcutFiles.Length} shortcut.", NotificationType.Info);
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
