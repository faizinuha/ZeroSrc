// File: SearchOverlay.xaml.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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
    public class StringToVisibilityConverter : IValueConverter
    {
        // Singleton biar bisa dipanggil lewat XAML: local:StringToVisibilityConverter.Instance
        public static readonly StringToVisibilityConverter Instance = new StringToVisibilityConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value as string;
            return string.IsNullOrWhiteSpace(text) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public partial class SearchOverlay : Window
    {
        private bool _isClosing = false;
        private readonly Dictionary<string, string> _appShortcuts;
        private readonly List<string> _suggestions = new();
        private readonly TextBlock _notificationText;
        private readonly List<string> _allSuggestions = new();
        private readonly List<string> _filteredSuggestions = new();

        public SearchOverlay()
        {
            InitializeComponent();
            _notificationText = (TextBlock)this.FindName("NotificationText");

            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            _appShortcuts = GetStartMenuShortcuts();

            // Semua suggestion asli (tetap ada di sini)
            _allSuggestions.AddRange(new[] {
                "https://www.google.com/search?q={Uri.EscapeDataString(query)}"
            });
            _allSuggestions.AddRange(_appShortcuts.Keys);
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
            _notificationText.Text = message;
            switch (type)
            {
                case NotificationType.Error:
                    _notificationText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                    break;
                case NotificationType.Warning:
                    _notificationText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange);
                    break;
                default:
                    _notificationText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
                    break;
            }
            MessageBoxImage icon = MessageBoxImage.Information;
            if (type == NotificationType.Error) icon = MessageBoxImage.Error;
            else if (type == NotificationType.Warning) icon = MessageBoxImage.Warning;
            MessageBox.Show(message, type.ToString(), MessageBoxButton.OK, icon);
        }

        private bool _isSelectingSuggestion = false;

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSelectingSuggestion) return;

            var searchBox = sender as TextBox;
            string query = searchBox?.Text ?? "";
            var suggestionList = this.FindName("SuggestionList") as ListBox;

            if (string.IsNullOrWhiteSpace(query))
            {
                suggestionList!.ItemsSource = null;
                suggestionList.Visibility = Visibility.Collapsed;
                return;
            }

            // cari semua suggestion yang cocok
            var filtered = _allSuggestions
                .Where(s => s.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Tambahkan opsi search Google
            filtered.Add($"Search Google for \"{query}\"");

            if (filtered.Count > 0)
            {
                // --- 1. Inline suggestion ---
                string best = filtered[0];
                if (best.Length > query.Length && !best.StartsWith("Search Google"))
                {
                    _isSelectingSuggestion = true;

                    searchBox!.Text = best;
                    searchBox.SelectionStart = query.Length;
                    searchBox.SelectionLength = best.Length - query.Length;

                    _isSelectingSuggestion = false;
                }

                // --- 2. Dropdown suggestion ---
                suggestionList!.ItemsSource = filtered.Skip(1).ToList();
                suggestionList.Visibility = Visibility.Visible;
            }
            else
            {
                suggestionList!.ItemsSource = null;
                suggestionList.Visibility = Visibility.Collapsed;
            }
        }



        private void SuggestionList_SelectionChanged(object sender, SelectionChangedEventArgs? e)
        {
            var lb = sender as ListBox;
            var searchBox = this.FindName("SearchBox") as TextBox;

            if (lb?.SelectedItem is string selected)
            {
                if (selected.StartsWith("Search Google for"))
                {
                    string query = searchBox?.Text ?? "";
                    ExecuteWebSearch(query);
                    BeginFadeOutAndClose();
                    return;
                }

                _isSelectingSuggestion = true; // lock biar TextChanged nggak jalan
                searchBox!.Text = selected;
                searchBox.CaretIndex = selected.Length;
                _isSelectingSuggestion = false;

                lb.Visibility = Visibility.Collapsed;
            }
        }


        private void SuggestionList_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            SuggestionList_SelectionChanged(sender, null);
        }

        // private void SuggestionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        // {
        //     var suggestionList = sender as ListBox;
        //     var searchBox = this.FindName("SearchBox") as TextBox;
        //     if (suggestionList?.SelectedItem is string selectedItem && searchBox != null)
        //     {
        //         searchBox.Text = selectedItem;
        //         searchBox.CaretIndex = selectedItem.Length;

        //         // Jika suggestion berasal dari env operations, tangani khusus
        //         switch (selectedItem.ToLower())
        //         {
        //             case "task manager":
        //                 Process.Start(new ProcessStartInfo("taskmgr") { UseShellExecute = true });
        //                 BeginFadeOutAndClose();
        //                 return;
        //             case "settings":
        //                 Process.Start(new ProcessStartInfo("ms-settings:") { UseShellExecute = true });
        //                 BeginFadeOutAndClose();
        //                 return;
        //             case "control panel":
        //                 Process.Start(new ProcessStartInfo("control") { UseShellExecute = true });
        //                 BeginFadeOutAndClose();
        //                 return;
        //             case "device manager":
        //                 Process.Start(new ProcessStartInfo("devmgmt.msc") { UseShellExecute = true });
        //                 BeginFadeOutAndClose();
        //                 return;
        //             case "file explorer":
        //                 Process.Start(new ProcessStartInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)) { UseShellExecute = true });
        //                 BeginFadeOutAndClose();
        //                 return;
        //             default:
        //                 ExecuteCommand(selectedItem);
        //                 return;
        //         }
        //     }
        // }

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
            var searchBox = this.FindName("SearchBox") as TextBox;

            if (e.Key == Key.Enter && lb?.SelectedItem is string s)
            {
                SuggestionList_SelectionChanged(
                    lb,
                    new SelectionChangedEventArgs(ListBox.SelectionChangedEvent, new List<string>(), new List<string>())
                );
            }
            else if (e.Key == Key.Up)
            {
                if (lb?.SelectedIndex == 0 && searchBox != null)
                {
                    searchBox.Focus(); // balik ke textbox kalau sudah di item paling atas
                }
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
        //   Event Handlers
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
