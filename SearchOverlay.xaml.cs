// File: SearchOverlay.xaml.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Animation;

using System.Net.Http;
using System.Text.Json;
namespace ZeroSrc
{
    public enum SuggestionType
    {
        App,
        WebSearch
    }

    public class SuggestionItem
    {
        public string DisplayText { get; }
        public string FilePath { get; }
        public SuggestionType Type { get; }
        public string Icon { get; } // Ikon dari font Segoe MDL2 Assets

        public SuggestionItem(string displayText, string? filePath, SuggestionType type)
        {
            DisplayText = displayText;
            FilePath = filePath ?? "";
            Type = type;
            // Tetapkan ikon berdasarkan tipe
            Icon = Type == SuggestionType.App ? "\uE770" : "\uE773"; // E770: App, E773: Web
        }
    }

    public enum NotificationType
    {
        Info,
        Warning,
        Error
    }

    public class SuggestionTypeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SuggestionType type)
            {
                return type == SuggestionType.App ? "Aplikasi Desktop" : "Pencarian Web";
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
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
        private readonly TextBlock _notificationText;
        private readonly List<SuggestionItem> _allSuggestions = new();
        private readonly List<string> _filteredSuggestions = new();

        public SearchOverlay()
        {
            InitializeComponent();
            _notificationText = (TextBlock)this.FindName("NotificationText");

            // Load all suggestions on startup
            LoadAllSuggestions();
        }

        #region Window Blur Effect
        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        [StructLayout(LayoutKind.Sequential)]
        internal struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        internal enum WindowCompositionAttribute
        {
            WCA_ACCENT_POLICY = 19
        }

        internal enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_BLURBEHIND = 3, // Efek blur standar
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        internal void EnableBlur()
        {
            var windowHelper = new System.Windows.Interop.WindowInteropHelper(this);
            var accent = new AccentPolicy { AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND };
            var accentStructSize = Marshal.SizeOf(accent);
            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);
            var data = new WindowCompositionAttributeData { Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY, SizeOfData = accentStructSize, Data = accentPtr };
            SetWindowCompositionAttribute(windowHelper.Handle, ref data);
            Marshal.FreeHGlobal(accentPtr);
        }
        #endregion

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Position window like macOS Spotlight (center-top)
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            this.Left = (screenWidth - this.Width) / 2;
            this.Top = screenHeight * 0.2; // 20% from the top

            // Aktifkan efek blur saat window dimuat
            EnableBlur();

            var searchBox = this.FindName("SearchBox") as System.Windows.Controls.TextBox;
            searchBox?.Focus();

            // Ensure the list is collapsed on load
            SuggestionList.Visibility = Visibility.Collapsed;

            // Start fade-in animation
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
        private static readonly HttpClient _httpClient = new HttpClient();

                private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
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
        
                    // Filter local suggestions (apps)
                    var localSuggestions = _allSuggestions
                        .Where(s => s.DisplayText.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    
                    // Get suggestions from Google
                    var googleSuggestions = await GetGoogleSuggestionsAsync(query);
                    var webSuggestions = googleSuggestions
                        .Select(s => new SuggestionItem(s, null, SuggestionType.WebSearch))
                        .ToList();

                    var combined = localSuggestions.Concat(webSuggestions).ToList();
        
                    // Add a specific option to search on Google
                    combined.Add(new SuggestionItem($"Search Google for \"{query}\"", query, SuggestionType.WebSearch));
        
                    if (combined.Count > 0)
                    {
                        // --- Dropdown suggestion ---
                        // Gunakan CollectionViewSource untuk grouping
                        var collectionView = new ListCollectionView(combined);
                        collectionView.GroupDescriptions.Add(new PropertyGroupDescription("Type"));
                        suggestionList!.ItemsSource = collectionView;
                        suggestionList.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        suggestionList!.ItemsSource = null;
                        suggestionList.Visibility = Visibility.Collapsed;
                    }
                }
        private async Task<List<string>> GetGoogleSuggestionsAsync(string query)
        {
            var suggestions = new List<string>();
            if (string.IsNullOrWhiteSpace(query))
                return suggestions;

            try
            {
                var url = $"https://suggestqueries.google.com/complete/search?client=firefox&q={Uri.EscapeDataString(query)}";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();

                using (var jsonDoc = JsonDocument.Parse(content))
                {
                    var root = jsonDoc.RootElement;
                    if (root.GetArrayLength() > 1)
                    {
                        var suggestionsArray = root[1];
                        foreach (var suggestion in suggestionsArray.EnumerateArray())
                        {
                            string? sug = suggestion.GetString();
                            if (sug != null)
                                suggestions.Add(sug);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Bisa ditambahkan logging atau notifikasi jika perlu
                Debug.WriteLine($"Failed to get Google suggestions: {ex.Message}");
            }

            return suggestions;
        }




        private void SuggestionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Event ini sengaja dikosongkan untuk mencegah eksekusi otomatis
            // saat pengguna hanya menavigasi daftar saran dengan tombol panah.
            // Eksekusi hanya akan terjadi pada Enter atau DoubleClick.
        }


        private void SuggestionList_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Dihapus untuk mencegah eksekusi otomatis pada satu kali klik.
            // Klik ganda sudah ditangani oleh SuggestionList_MouseDoubleClick.
        }

        private void SuggestionList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            HandleSuggestionSelection((sender as ListBox)?.SelectedItem as SuggestionItem);
        }

        private void SuggestionList_KeyDown(object sender, KeyEventArgs e)
        {
            var lb = sender as ListBox;
            if (e.Key == Key.Enter && lb?.SelectedItem is SuggestionItem selectedItem)
            {
                HandleSuggestionSelection(selectedItem);
                e.Handled = true; // Mencegah event ini diproses lebih lanjut
            }
            var searchBox = this.FindName("SearchBox") as TextBox;
            if (e.Key == Key.Up)
            {
                if (lb?.SelectedIndex == 0 && searchBox != null)
                {
                    searchBox.Focus(); // balik ke textbox kalau sudah di item paling atas
                }
            }
        }

        private void HandleSuggestionSelection(SuggestionItem? selectedItem)
        {
            if (selectedItem == null) return;

            if (selectedItem.Type == SuggestionType.App)
            {
                // Langsung jalankan aplikasi
                ExecuteCommand(selectedItem.DisplayText);
            }
            else if (selectedItem.Type == SuggestionType.WebSearch)
            {
                // Jika ini adalah item "Search Google for...", gunakan query aslinya.
                // Jika tidak, gunakan DisplayText.
                string queryToSearch = selectedItem.DisplayText.StartsWith("Search Google for")
                    ? selectedItem.FilePath
                    : selectedItem.DisplayText;

                ExecuteWebSearch(queryToSearch);
                BeginFadeOutAndClose();
            }
        }


        private void ExecuteCommand(string query)
        {
            if (string.IsNullOrEmpty(query)) return;

            try
            {
                // Find a matching app from suggestions
                var appToLaunch = _allSuggestions.FirstOrDefault(
                    s => s.Type == SuggestionType.App &&
                         s.DisplayText.Equals(query, StringComparison.OrdinalIgnoreCase));

                if (appToLaunch != null)
                {
                    Process.Start(new ProcessStartInfo(appToLaunch.FilePath) { UseShellExecute = true });
                    BeginFadeOutAndClose();
                    return;
                }

                // If no app matches, perform a web search as a fallback
                ExecuteWebSearch(query);
                BeginFadeOutAndClose();
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
            var suggestionList = this.FindName("SuggestionList") as ListBox;

            if (e.Key == Key.Enter)
            {
                string query = searchBox?.Text.Trim() ?? string.Empty;
                if (!string.IsNullOrEmpty(query))
                {
                    SuggestionItem? itemToExecute = null;

                    // Prioritas 1: Item yang sedang dipilih di ListBox
                    if (suggestionList?.Visibility == Visibility.Visible && suggestionList.SelectedItem != null)
                    {
                        itemToExecute = suggestionList.SelectedItem as SuggestionItem;
                    }
                    // Prioritas 2: Jika tidak ada yang dipilih, ambil item pertama dari daftar
                    else if (suggestionList?.Visibility == Visibility.Visible && suggestionList.HasItems)
                    {
                        itemToExecute = (suggestionList.ItemsSource as ICollectionView)?.Cast<object>().FirstOrDefault() as SuggestionItem;
                    }

                    // Jika ada item dari saran, eksekusi. Jika tidak, jalankan perintah seperti biasa (fallback ke web search).
                    if (itemToExecute != null) HandleSuggestionSelection(itemToExecute);
                    else ExecuteCommand(query);
                }
            }
            else if (e.Key == Key.Escape)
            {
                BeginFadeOutAndClose();
            }
            else if (e.Key == Key.Down)
            {
                // var suggestionList is already defined in this scope
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

        private void LoadAllSuggestions()
        {
            _allSuggestions.Clear();
            var shortcutPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            string[] startMenuPaths = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
                Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)
            };

            var allLnkFiles = startMenuPaths
                .Where(Directory.Exists)
                .SelectMany(path => Directory.GetFiles(path, "*.lnk", SearchOption.AllDirectories));

            foreach (var file in allLnkFiles)
            {
                string name = Path.GetFileNameWithoutExtension(file);
                if (!string.IsNullOrEmpty(name) && !shortcutPaths.ContainsKey(name))
                    _allSuggestions.Add(new SuggestionItem(name, file, SuggestionType.App));
            }
        }

        public void BeginFadeOutAndCloseByMain()
        {
            BeginFadeOutAndClose();
        }
    }
}
