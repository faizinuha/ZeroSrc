// File: SearchOverlay.xaml.cs
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Drawing;

using System.Net.Http;
using System.Text.Json;
using System.Windows.Threading;
namespace ZeroMix
{
    public enum SuggestionType
    {
        App,
        WebSearch,
        Calculator,
        Image,
        System
    }

    public class SuggestionItem
    {
        public string DisplayText { get; }
        public string FilePath { get; }
        public SuggestionType Type { get; }
        public string Icon { get; } // Ikon dari font Segoe MDL2 Assets
        public System.Windows.Media.ImageSource? IconSource { get; } // Ikon dari file .exe

        public SuggestionItem(string displayText, string? filePath, SuggestionType type, System.Windows.Media.ImageSource? iconSource = null)
        {
            DisplayText = displayText;
            FilePath = filePath ?? "";
            Type = type;
            IconSource = iconSource;
            
            // Tetapkan ikon berdasarkan tipe
            Icon = Type switch
            {
                SuggestionType.App => "\uE770",        // App icon
                SuggestionType.Calculator => "\uE8EF", // Calculator icon
                SuggestionType.Image => "\uEB9F",      // Image icon
                SuggestionType.System => "\uE7E8",     // Power icon
                _ => "\uE773"                           // Web search icon
            };
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
                return type switch
                {
                    SuggestionType.App => "Aplikasi Desktop",
                    SuggestionType.Calculator => "Calculator",
                    SuggestionType.WebSearch => "Pencarian Web",
                    SuggestionType.Image => "Pencarian Gambar",
                    SuggestionType.System => "Sistem",
                    _ => string.Empty
                };
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

    public class NullToVisibilityConverter : IValueConverter
    {
        public static readonly NullToVisibilityConverter Instance = new NullToVisibilityConverter();
        public static readonly NullToVisibilityConverter InstanceInverse = new NullToVisibilityConverter { IsInverse = true };

        public bool IsInverse { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isNull = value == null;
            
            if (IsInverse)
                return isNull ? Visibility.Visible : Visibility.Collapsed; // Jika null, tampilkan (untuk fallback text)
            
            return isNull ? Visibility.Collapsed : Visibility.Visible; // Jika tidak null, tampilkan (untuk image)
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public partial class SearchOverlay : Window
    {

        private readonly TextBlock _notificationText;
        private readonly TextBlock _clockText;
        private readonly TextBlock _dateText;
        private readonly Border _dragDropArea;
        private readonly DispatcherTimer _clockTimer;
        private readonly List<SuggestionItem> _allSuggestions = new();
        private readonly List<string> _filteredSuggestions = new();

        public SearchOverlay()
        {
            InitializeComponent();
            _notificationText = (TextBlock)this.FindName("NotificationText");
            _clockText = (TextBlock)this.FindName("ClockText");
            _dateText = (TextBlock)this.FindName("DateText");
            _dragDropArea = (Border)this.FindName("DragDropArea");

            // Initialize clock timer
            _clockTimer = new DispatcherTimer();
            _clockTimer.Interval = TimeSpan.FromSeconds(1);
            _clockTimer.Tick += ClockTimer_Tick;
            _clockTimer.Start();
            
            // Update clock immediately
            UpdateClock();

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
            var accent = new AccentPolicy { AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND, AccentFlags = 2, GradientColor = 0 };
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

            // Atur background window menjadi transparan agar efek blur dari DWM terlihat.
            // Latar belakang visual sekarang diatur pada MainBorder di XAML.
            this.Background = System.Windows.Media.Brushes.Transparent;
            // Aktifkan efek blur.
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
            System.Windows.MessageBox.Show(message, type.ToString(), MessageBoxButton.OK, icon);
        }

        private bool _isSelectingSuggestion = false;
        private static readonly HttpClient _httpClient = new HttpClient();

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSelectingSuggestion) return;

            var searchBox = sender as System.Windows.Controls.TextBox;
            string query = searchBox?.Text ?? "";
            var suggestionList = this.FindName("SuggestionList") as System.Windows.Controls.ListBox;

            if (string.IsNullOrWhiteSpace(query))
            {
                suggestionList!.ItemsSource = null;
                suggestionList.Visibility = Visibility.Collapsed;
                return;
            }

            var combined = new List<SuggestionItem>();

            // ========================================
            // 🧮 CALCULATOR FEATURE - INLINE RESULT
            // ========================================
            if (MathEvaluator.IsMathExpression(query))
            {
                var (success, result, error) = MathEvaluator.Evaluate(query);
                
                if (success)
                {
                    string formattedResult = MathEvaluator.FormatResult(result);
                    
                    // Add calculator result at TOP (priority #1)
                    combined.Add(new SuggestionItem(
                        displayText: $"= {formattedResult}",
                        filePath: formattedResult, // Store result for copy
                        type: SuggestionType.Calculator
                    ));
                }
            }

            // ========================================
            // ⚡ SYSTEM COMMANDS - SHUTDOWN & RESTART
            // ========================================
            string queryLower = query.ToLower();
            if (queryLower.Contains("shutdown") || queryLower.Contains("shut down"))
            {
                combined.Add(new SuggestionItem(
                    displayText: "Shutdown PC",
                    filePath: "shutdown",
                    type: SuggestionType.System
                ));
            }
            
            if (queryLower.Contains("restart") || queryLower.Contains("reboot"))
            {
                combined.Add(new SuggestionItem(
                    displayText: "Restart PC",
                    filePath: "restart",
                    type: SuggestionType.System
                ));
            }

            if (queryLower.Contains("sleep"))
            {
                combined.Add(new SuggestionItem(
                    displayText: "Sleep PC",
                    filePath: "sleep",
                    type: SuggestionType.System
                ));
            }

            // ========================================
            // 📱 FUZZY MATCHING - BETTER APP SEARCH
            // ========================================
            var localSuggestions = _allSuggestions
                .Select(s => new
                {
                    Item = s,
                    Score = CalculateMatchScore(s.DisplayText, query)
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Select(x => x.Item)
                .Take(5)
                .ToList();

            combined.AddRange(localSuggestions);

            // ========================================
            // 🔍 GOOGLE SUGGESTIONS
            // ========================================
            var googleSuggestions = await GetGoogleSuggestionsAsync(query);
            var webSuggestions = googleSuggestions
                .Select(s => new SuggestionItem(s, null, SuggestionType.WebSearch))
                .ToList();

            combined.AddRange(webSuggestions);
            combined.Add(new SuggestionItem("Search Google for \"" + query + "\"", query, SuggestionType.WebSearch));

            if (combined.Count > 0)
            {
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

        /// <summary>
        /// Calculate fuzzy match score (0-100)
        /// </summary>
        private int CalculateMatchScore(string text, string query)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(query))
                return 0;

            text = text.ToLower();
            query = query.ToLower();

            if (text == query) return 100;
            if (text.StartsWith(query)) return 90;
            if (text.Contains(" " + query)) return 70;
            if (text.Contains(query)) return 50;

            int queryIndex = 0;
            for (int i = 0; i < text.Length && queryIndex < query.Length; i++)
            {
                if (text[i] == query[queryIndex])
                    queryIndex++;
            }
            if (queryIndex == query.Length) return 30;

            return 0;
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
            HandleSuggestionSelection((sender as System.Windows.Controls.ListBox)?.SelectedItem as SuggestionItem);
        }

        private void SuggestionList_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var lb = sender as System.Windows.Controls.ListBox;
            if (e.Key == Key.Enter && lb?.SelectedItem is SuggestionItem selectedItem)
            {
                HandleSuggestionSelection(selectedItem);
                e.Handled = true; // Mencegah event ini diproses lebih lanjut
            }
            var searchBox = this.FindName("SearchBox") as System.Windows.Controls.TextBox;
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
            else if (selectedItem.Type == SuggestionType.Calculator)
            {
                // Copy result to clipboard
                try
                {
                    System.Windows.Clipboard.SetText(selectedItem.FilePath);
                    
                    _notificationText.Text = $"✓ Copied: {selectedItem.FilePath}";
                    _notificationText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 122, 255));
                    _notificationText.Visibility = Visibility.Visible;
                    
                    var timer = new System.Windows.Threading.DispatcherTimer();
                    timer.Interval = TimeSpan.FromSeconds(1.5);
                    timer.Tick += (s, e) =>
                    {
                        _notificationText.Visibility = Visibility.Collapsed;
                        timer.Stop();
                    };
                    timer.Start();
                    
                    BeginFadeOutAndClose();
                }
                catch (Exception ex)
                {
                    ShowNotification($"Failed to copy: {ex.Message}", NotificationType.Error);
                }
            }
            else if (selectedItem.Type == SuggestionType.System)
            {
                // Handle system commands (shutdown, restart, sleep)
                ExecuteSystemCommand(selectedItem.FilePath);
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
            else if (selectedItem.Type == SuggestionType.Image)
            {
                // Handle image search or operations
                ExecuteWebSearch(selectedItem.DisplayText + " images");
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
        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                BeginFadeOutAndClose();
            }
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            BeginFadeOutAndClose();
        }

        //   Event Handlers
        private void SearchBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var searchBox = sender as System.Windows.Controls.TextBox;
            var suggestionList = this.FindName("SuggestionList") as System.Windows.Controls.ListBox;

            if (e.Key == Key.Enter)
            {
                string query = searchBox?.Text.Trim() ?? string.Empty;
                if (!string.IsNullOrEmpty(query))
                {
                    if (query.StartsWith("ytc ", StringComparison.OrdinalIgnoreCase))
                    {
                        string channelName = query.Substring(4).Trim();
                        if (!string.IsNullOrEmpty(channelName))
                        {
                            ExecuteYouTubeSearch(channelName);
                            BeginFadeOutAndClose();
                            return;
                        }
                    }

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
                BeginFadeOutAndClose();
            }
            catch (Exception ex)
            {
                ShowNotification($"Gagal membuka browser: {ex.Message}", NotificationType.Error);
            }
        }

        private void ExecuteYouTubeSearch(string channelName)
        {
            try
            {
                string url = $"https://www.youtube.com/results?search_query={Uri.EscapeDataString(channelName)}";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                BeginFadeOutAndClose();
            }
            catch (Exception ex)
            {
                ShowNotification($"Gagal membuka browser: {ex.Message}", NotificationType.Error);
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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

            var result = System.Windows.MessageBox.Show($"Akan membuka {shortcutFiles.Length} shortcut di Desktop. Lanjutkan?", "Konfirmasi", MessageBoxButton.YesNo, MessageBoxImage.Question);
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
                {
                    // Extract icon from the shortcut file
                    var iconSource = ExtractIconFromFile(file);
                    _allSuggestions.Add(new SuggestionItem(name, file, SuggestionType.App, iconSource));
                }
            }
        }

        #region Icon Extraction
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

        private System.Windows.Media.ImageSource? ExtractIconFromFile(string filePath)
        {
            try
            {
                // Untuk .lnk file, coba resolve ke target file
                string targetPath = filePath;
                if (filePath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                        dynamic shell = Activator.CreateInstance(shellType);
                        dynamic shortcut = shell.CreateShortcut(filePath);
                        targetPath = shortcut.TargetPath;
                        if (string.IsNullOrEmpty(targetPath))
                            targetPath = filePath;
                    }
                    catch
                    {
                        targetPath = filePath;
                    }
                }

                // Extract icon dari file
                System.Drawing.Icon? icon = null;
                
                if (File.Exists(targetPath))
                {
                    // Try to extract icon from exe/dll
                    IntPtr hIcon = ExtractIcon(IntPtr.Zero, targetPath, 0);
                    if (hIcon != IntPtr.Zero && hIcon != (IntPtr)1)
                    {
                        icon = System.Drawing.Icon.FromHandle(hIcon);
                    }
                    else
                    {
                        // Fallback to Icon.ExtractAssociatedIcon
                        icon = System.Drawing.Icon.ExtractAssociatedIcon(targetPath);
                    }
                }

                if (icon != null)
                {
                    // Convert Icon to ImageSource
                    using (var bitmap = icon.ToBitmap())
                    {
                        var hBitmap = bitmap.GetHbitmap();
                        try
                        {
                            return Imaging.CreateBitmapSourceFromHBitmap(
                                hBitmap,
                                IntPtr.Zero,
                                System.Windows.Int32Rect.Empty,
                                BitmapSizeOptions.FromEmptyOptions());
                        }
                        finally
                        {
                            DeleteObject(hBitmap);
                        }
                    }
                }
            }
            catch
            {
                // Jika gagal extract icon, return null (akan pakai icon default)
            }

            return null;
        }

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);
        #endregion

        private void CustomShortcutButton_Click(object sender, RoutedEventArgs e)
        {
            // Hide the overlay to focus on the settings window
            this.Visibility = Visibility.Hidden;

            var customShortcutWindow = new CustomShortcutWindow();
            bool? result = customShortcutWindow.ShowDialog(); // Blocks until closed

            // Karena hotkey sekarang dimuat ulang secara dinamis, kita tidak perlu menutup aplikasi.
            // Cukup tampilkan kembali overlay.
            // if (result == true)
            // {
            //     // Pengguna menyimpan, cukup tampilkan kembali overlay
            //     this.Visibility = Visibility.Visible;
            //     SearchBox.Focus();
            // }
            // else // Jika pengguna menekan "Cancel" atau menutup jendela
            // {
            //     this.Visibility = Visibility.Visible; // Tampilkan kembali overlay
            //     SearchBox.Focus();
            // }
        }

        #region Clock Methods
        private void ClockTimer_Tick(object sender, EventArgs e)
        {
            UpdateClock();
        }

        private void UpdateClock()
        {
            var now = DateTime.Now;
            _clockText.Text = now.ToString("HH:mm:ss");
            _dateText.Text = now.ToString("ddd, dd MMM yyyy");
        }
        #endregion

        #region Drag & Drop Methods
        private void SearchBox_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (files.Length > 0 && IsImageFile(files[0]))
                {
                    e.Effects = System.Windows.DragDropEffects.Copy;
                    _dragDropArea.Visibility = Visibility.Visible;
                    _dragDropArea.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0x80, 0x00, 0x7A, 0xFF));
                    return;
                }
            }
            e.Effects = System.Windows.DragDropEffects.None;
        }

        private void SearchBox_DragLeave(object sender, System.Windows.DragEventArgs e)
        {
            _dragDropArea.Visibility = Visibility.Collapsed;
        }

        private void SearchBox_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (files.Length > 0 && IsImageFile(files[0]))
                {
                    HandleImageDrop(files[0]);
                }
            }
            _dragDropArea.Visibility = Visibility.Collapsed;
        }

        private void DragDropArea_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                e.Effects = System.Windows.DragDropEffects.Copy;
                _dragDropArea.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xFF, 0x00, 0x7A, 0xFF));
            }
            else
            {
                e.Effects = System.Windows.DragDropEffects.None;
            }
        }

        private void DragDropArea_DragLeave(object sender, System.Windows.DragEventArgs e)
        {
            _dragDropArea.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0x30, 0x00, 0x7A, 0xFF));
        }

        private void DragDropArea_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (files.Length > 0 && IsImageFile(files[0]))
                {
                    HandleImageDrop(files[0]);
                }
            }
            _dragDropArea.Visibility = Visibility.Collapsed;
        }

        private bool IsImageFile(string filePath)
        {
            string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".ico" };
            string extension = Path.GetExtension(filePath).ToLower();
            return imageExtensions.Contains(extension);
        }

        private void HandleImageDrop(string imagePath)
        {
            try
            {
                // Show notification
                _notificationText.Text = $"📷 Image: {Path.GetFileName(imagePath)}";
                _notificationText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 122, 255));
                _notificationText.Visibility = Visibility.Visible;

                // Ask user what to do
                var result = System.Windows.MessageBox.Show(
                    $"What would you like to do with this image?\n\n{Path.GetFileName(imagePath)}\n\nYes: Open containing folder\nNo: Search on Google Images",
                    "Image Action",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Open folder and select the file
                    Process.Start("explorer.exe", $"/select,\"{imagePath}\"");
                    BeginFadeOutAndClose();
                }
                else if (result == MessageBoxResult.No)
                {
                    // Search on Google Images (open Google Images)
                    string url = "https://images.google.com/";
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                    BeginFadeOutAndClose();
                }
                
                _notificationText.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                ShowNotification($"Failed to handle image: {ex.Message}", NotificationType.Error);
            }
        }
        #endregion

        #region Power Options
        private void ExecuteSystemCommand(string command)
        {
            string action = command.ToLower();
            string message = "";
            string title = "";
            string shutdownArgs = "";

            switch (action)
            {
                case "shutdown":
                    message = "Are you sure you want to shutdown your PC?";
                    title = "Shutdown PC";
                    shutdownArgs = "/s /t 0";
                    break;
                case "restart":
                    message = "Are you sure you want to restart your PC?";
                    title = "Restart PC";
                    shutdownArgs = "/r /t 0";
                    break;
                case "sleep":
                    message = "Are you sure you want to put your PC to sleep?";
                    title = "Sleep PC";
                    shutdownArgs = ""; // Sleep uses different approach
                    break;
                default:
                    return;
            }

            var result = System.Windows.MessageBox.Show(
                message,
                title,
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    if (action == "sleep")
                    {
                        // Use Windows Forms for sleep
                        System.Windows.Forms.Application.SetSuspendState(
                            System.Windows.Forms.PowerState.Suspend,
                            false,
                            false);
                    }
                    else
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "shutdown",
                            Arguments = shutdownArgs,
                            CreateNoWindow = true,
                            UseShellExecute = false
                        });
                    }
                    BeginFadeOutAndClose();
                }
                catch (Exception ex)
                {
                    ShowNotification($"Failed to {action}: {ex.Message}", NotificationType.Error);
                }
            }
        }
        #endregion

        public void BeginFadeOutAndCloseByMain()
        {
            BeginFadeOutAndClose();
        }
    }
}
