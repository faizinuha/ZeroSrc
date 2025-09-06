using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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
        // Instans tunggal dari konverter
        public static StringToVisibilityConverter Instance = new StringToVisibilityConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrEmpty(value as string) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    public partial class SearchOverlay : Window
    {
        private bool _isClosing = false;
        private readonly Dictionary<string, string> _appShortcuts;
        private readonly List<string> _suggestions = new();

        // Properti yang terikat ke UI
        public ObservableCollection<string> Suggestions { get; set; } = new ObservableCollection<string>();

        private readonly TextBlock _notificationText;

        public SearchOverlay()
        {
            InitializeComponent();
            
            // Mengatur DataContext agar binding ke properti Suggestions berfungsi
            this.DataContext = this;
            
            _notificationText = (TextBlock)this.FindName("NotificationText");

            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            _appShortcuts = GetStartMenuShortcuts();

            // Inisialisasi suggestions
            _suggestions.AddRange(new[] {
                "desktop shortcuts",
                "open desktop shortcuts",
                "buka semua shortcut",
                "task manager",
                "settings",
                "control panel",
                "device manager",
                "file explorer",
                "all apps",
                "installed apps",
                "installed",
                "env",
                "environment",
                "operations"
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
            // Tidak menggunakan MessageBox untuk pengalaman yang lebih baik
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
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchBox = sender as TextBox;
            var suggestionList = this.FindName("SuggestionList") as ListBox;
            if (searchBox == null || suggestionList == null) return;
            
            string query = searchBox.Text.Trim().ToLower();

            // Kosongkan koleksi yang terikat ke UI sebelum mengisi ulang
            Suggestions.Clear();
            var filteredSuggestions = _suggestions
                .Where(s => s.ToLower().Contains(query))
                .Take(10)
                .ToList();

            foreach (var s in filteredSuggestions)
            {
                Suggestions.Add(s);
            }

            if (string.IsNullOrEmpty(query) || !Suggestions.Any())
            {
                suggestionList.Visibility = Visibility.Collapsed;
            }
            else
            {
                suggestionList.Visibility = Visibility.Visible;
            }
        }


        private void SuggestionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var suggestionList = sender as ListBox;
            var searchBox = this.FindName("SearchBox") as TextBox;
            if (suggestionList?.SelectedItem is string selectedItem && searchBox != null)
            {
                searchBox.Text = selectedItem;
                searchBox.CaretIndex = selectedItem.Length;

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
        //  Event Handlers
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

            // Ganti MessageBox dengan ShowNotification
            ShowNotification($"Mencoba membuka {shortcutFiles.Length} shortcut di Desktop.", NotificationType.Info);
            
            // Logika untuk membuka shortcut
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
