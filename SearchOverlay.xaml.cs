using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ZeroMix
{
    public enum SuggestionType
    {
        App,
        WebSearch,
        Calculator,
        Image,
        System,
        Terminal,
        File,
        Video,
        Music
    }

    public class SuggestionItem : INotifyPropertyChanged
    {
        public string DisplayText { get; }
        public string Subtitle { get; }
        public string FilePath { get; }
        public SuggestionType Type { get; }
        public string Icon { get; }
        
        // Lazy loading support
        private bool _isIconLoaded = false;
        private System.Windows.Media.ImageSource? _iconSource;
        public System.Windows.Media.ImageSource? IconSource
        {
            get 
            {
                if (!_isIconLoaded && _iconSource == null && !string.IsNullOrEmpty(FilePath))
                {
                    _isIconLoaded = true; // Prevent multiple triggers
                    LoadIconAsync();
                }
                return _iconSource;
            }
            set
            {
                if (_iconSource != value)
                {
                    _iconSource = value;
                    OnPropertyChanged(nameof(IconSource));
                }
            }
        }

        public bool IsTerminal => Type == SuggestionType.Terminal;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public SuggestionItem(string displayText, string? filePath, SuggestionType type, System.Windows.Media.ImageSource? iconSource = null, string subtitle = "")
        {
            DisplayText = displayText;
            FilePath = filePath ?? "";
            Type = type;
            _iconSource = iconSource;
            if (iconSource != null) _isIconLoaded = true; // If pre-loaded, mark as loaded
            Subtitle = subtitle;

            Icon = Type switch
            {
                SuggestionType.App => "\uE770",
                SuggestionType.Calculator => "\uE8EF",
                SuggestionType.Image => "\uEB9F",
                SuggestionType.System => "\uE7E8",
                SuggestionType.Terminal => "\uE756",
                SuggestionType.WebSearch => "\uE774",
                SuggestionType.File => "\uE8A5",
                SuggestionType.Video => "\uE714",
                SuggestionType.Music => "\uE93C",
                _ => "\uE773"
            };

            if (string.IsNullOrEmpty(Subtitle))
            {
                Subtitle = Type switch
                {
                    SuggestionType.App => GetAppCategory(displayText),
                    SuggestionType.Calculator => "Calculator Result",
                    SuggestionType.System => "System Command",
                    SuggestionType.WebSearch => "Search on Google",
                    SuggestionType.Terminal => "Run command in Terminal",
                    SuggestionType.File => "File",
                    SuggestionType.Video => "Video",
                    SuggestionType.Music => "Music",
                    _ => ""
                };
            }
        }

        private async void LoadIconAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(FilePath)) return;

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => 
                {
                    // Basic caching check (could be enhanced globally)
                    System.Windows.Media.ImageSource? icon = null;
                    
                    await Task.Run(() => 
                    {
                        try 
                        {
                            // Need to access SearchOverlay's ExtractIconFromFile static method or move it to helper
                            // For now, we assume we can call a static helper or just extract here if possible, 
                            // but ExtractIconFromFile is currently private in SearchOverlay.
                            // To fix this cleanly, we'll use a delegate or event in a real architecture, 
                            // but here let's assume SearchOverlay exposes a helper or we move logic.
                            // For this Refactor, I will modify SearchOverlay to expose a public static helper.
                            
                            icon = ZeroMix.SearchOverlay.GetIconForFile(FilePath);
                            icon?.Freeze();
                        }
                        catch {}
                    });

                    if (icon != null)
                    {
                        IconSource = icon;
                    }
                });
            }
            catch {}
        }
        


        private static string GetAppCategory(string appName)
        {
            string lower = appName.ToLower();

            // Browsers
            if (lower.Contains("chrome") || lower.Contains("firefox") || lower.Contains("edge") ||
                lower.Contains("brave") || lower.Contains("opera"))
                return "Browser";

            // Media Players
            if (lower.Contains("youtube") || lower.Contains("spotify") || lower.Contains("vlc") ||
                lower.Contains("media player"))
                return "Media Player";

            // Terminal/Command Line
            if (lower.Contains("terminal") || lower.Contains("cmd") || lower.Contains("powershell") ||
                lower.Contains("command"))
                return "Terminal";

            // Office Apps
            if (lower.Contains("word") || lower.Contains("excel") || lower.Contains("powerpoint") ||
                lower.Contains("outlook") || lower.Contains("office"))
                return "Office Application";

            // Dev Tools
            if (lower.Contains("code") || lower.Contains("studio") || lower.Contains("git"))
                return "Development Tool";

            return "Application";
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
                    SuggestionType.Terminal => "Terminal",
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
        public static readonly StringToVisibilityConverter Instance = new StringToVisibilityConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value as string;
            return string.IsNullOrWhiteSpace(text) ? Visibility.Collapsed : Visibility.Visible;
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
                return isNull ? Visibility.Visible : Visibility.Collapsed;
            return isNull ? Visibility.Collapsed : Visibility.Visible;
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
        private static readonly HttpClient _httpClient = new HttpClient();
        private bool _isSelectingSuggestion = false;
        private bool _isLoadingSuggestions = false;
        private CancellationTokenSource? _searchCts;

        // Icon Cache for performance
        private static readonly Dictionary<string, System.Windows.Media.ImageSource> _iconCache = new();

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);

        public SearchOverlay()
        {
            InitializeComponent();
            _notificationText = (TextBlock)this.FindName("NotificationText");
            _clockText = (TextBlock)this.FindName("ClockText");
            _dateText = (TextBlock)this.FindName("DateText");
            _dragDropArea = (Border)this.FindName("DragDropArea");

            _clockTimer = new DispatcherTimer();
            _clockTimer.Interval = TimeSpan.FromSeconds(1);
            _clockTimer.Tick += (s, e) => UpdateClock();
            _clockTimer.Start();
            UpdateClock();

            // Lazy load suggestions on background thread
            Task.Run(() => LoadAllSuggestionsAsync());
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
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
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
            var windowHelper = new WindowInteropHelper(this);

            // ACCENT_ENABLE_ACRYLICBLURBEHIND = 4 (Modern Windows 10/11)
            // ACCENT_ENABLE_BLURBEHIND = 3 (Legacy Windows 10)
            var accent = new AccentPolicy
            {
                AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
                AccentFlags = 2,
                GradientColor = 0x01FFFFFF // Very slight tint
            };

            var accentStructSize = Marshal.SizeOf(accent);
            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData
            {
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = accentStructSize,
                Data = accentPtr
            };

            SetWindowCompositionAttribute(windowHelper.Handle, ref data);
            Marshal.FreeHGlobal(accentPtr);
        }
        #endregion

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            this.Left = (screenWidth - this.Width) / 2;
            this.Top = screenHeight * 0.2;

            this.Background = System.Windows.Media.Brushes.Transparent;
            EnableBlur();

            var searchBox = this.FindName("SearchBox") as System.Windows.Controls.TextBox;
            searchBox?.Focus();

            SuggestionList.Visibility = Visibility.Collapsed;

            var fadeIn = (Storyboard)FindResource("FadeInStoryboard");
            fadeIn.Begin(this);
        }

        public void BeginFadeOutAndClose()
        {
            var fadeOut = (Storyboard)FindResource("FadeOutStoryboard");
            fadeOut.Completed += (s, e) => this.Close();
            fadeOut.Begin(this);
        }

        private void UpdateClock()
        {
            var now = DateTime.Now;
            _clockText.Text = now.ToString("HH:mm:ss");
            _dateText.Text = now.ToString("ddd, dd MMM yyyy");
        }

        private void ShowNotification(string message, NotificationType type = NotificationType.Info)
        {
            _notificationText.Text = message;
            _notificationText.Foreground = type switch
            {
                NotificationType.Error => System.Windows.Media.Brushes.Red,
                NotificationType.Warning => System.Windows.Media.Brushes.Orange,
                _ => System.Windows.Media.Brushes.Black
            };
            _notificationText.Visibility = Visibility.Visible;

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            timer.Tick += (s, e) => { _notificationText.Visibility = Visibility.Collapsed; timer.Stop(); };
            timer.Start();
        }

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSelectingSuggestion) return;

            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var ct = _searchCts.Token;

            var searchBox = sender as System.Windows.Controls.TextBox;
            string query = searchBox?.Text ?? "";
            var suggestionList = this.FindName("SuggestionList") as System.Windows.Controls.ListBox;

            if (string.IsNullOrWhiteSpace(query))
            {
                suggestionList!.ItemsSource = null;
                suggestionList.Visibility = Visibility.Collapsed;
                return;
            }

            try
            {
                await Task.Delay(150, ct); // Debounce a bit longer for smoother feel

                // Offload all logic to background thread except UI component access
                var combined = await Task.Run(async () =>
                {
                    var results = new List<SuggestionItem>();
                    string queryLower = query.ToLower();

                    // 1. Folder Path detection
                    if (IsFolderPath(query))
                    {
                        var folderSuggestions = GetFolderSuggestions(query);
                        results.AddRange(folderSuggestions);
                    }

                    if (ct.IsCancellationRequested) return results;

                    // 2. Terminal
                    if (IsTerminalCommand(query))
                    {
                        var terminalIcon = GetTerminalIcon();
                        results.Add(new SuggestionItem(query, query, SuggestionType.Terminal, terminalIcon, "Run in Terminal"));
                    }

                    // 3. Calculator
                    if (ContainsNumbers(query) && MathEvaluator.IsMathExpression(query))
                    {
                        var (success, result, _) = MathEvaluator.Evaluate(query);
                        if (success)
                        {
                            string formattedResult = MathEvaluator.FormatResult(result);
                            results.Add(new SuggestionItem($"= {formattedResult}", formattedResult, SuggestionType.Calculator));
                        }
                    }

                    // 4. System Commands
                    if (queryLower.Contains("shutdown")) results.Add(new SuggestionItem("Shutdown PC", "shutdown", SuggestionType.System));
                    if (queryLower.Contains("restart")) results.Add(new SuggestionItem("Restart PC", "restart", SuggestionType.System));
                    if (queryLower.Contains("sleep")) results.Add(new SuggestionItem("Sleep PC", "sleep", SuggestionType.System));

                    // 5. Fuzzy Matching Apps & Indexed suggestions
                    var appMatches = _allSuggestions
                        .Select(s => new { Item = s, Score = CalculateMatchScore(s.DisplayText, queryLower) })
                        .Where(x => x.Score > 0)
                        .OrderByDescending(x => x.Score)
                        .Take(8)
                        .Select(x => x.Item)
                        .ToList();
                    results.AddRange(appMatches);

                    if (ct.IsCancellationRequested) return results;

                    // 6. Google Suggestions
                    try
                    {
                        var googleResults = await GetGoogleSuggestionsAsync(query, ct);
                        foreach (var s in googleResults.Take(4))
                        {
                            results.Add(new SuggestionItem(s, s, SuggestionType.WebSearch));
                        }
                    }
                    catch { }

                    return results;
                }, ct);

                if (ct.IsCancellationRequested) return;

                if (combined.Count > 0)
                {
                    var sorted = combined.OrderBy(x => GetTypePriority(x.Type)).ToList();
                    suggestionList!.ItemsSource = sorted;
                    suggestionList.Visibility = Visibility.Visible;
                }
                else
                {
                    suggestionList!.ItemsSource = null;
                    suggestionList.Visibility = Visibility.Collapsed;
                }
            }
            catch (OperationCanceledException) { }
        }

        private int GetTypePriority(SuggestionType type)
        {
            return type switch
            {
                SuggestionType.Terminal => 1,
                SuggestionType.Calculator => 2,
                SuggestionType.System => 3,
                SuggestionType.App => 4,
                SuggestionType.File => 5,
                SuggestionType.Video => 6,
                SuggestionType.Music => 7,
                SuggestionType.WebSearch => 8,
                _ => 9
            };
        }

        private bool ContainsNumbers(string text)
        {
            return text.Any(char.IsDigit);
        }

        private bool IsTerminalCommand(string query)
        {
            string[] terminalKeywords = { "cmd", "powershell", "terminal", "bash", "sh", "git", "npm", "node", "python", "pip", "dotnet", "docker" };
            string lower = query.ToLower();
            return terminalKeywords.Any(k => lower.Contains(k)) || query.Contains("/") || query.Contains("\\");
        }

        private System.Windows.Media.ImageSource? GetTerminalIcon()
        {
            // Try to get Windows Terminal icon
            string terminalPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft\\WindowsApps\\wt.exe");

            if (File.Exists(terminalPath))
            {
                return ExtractIconFromFile(terminalPath);
            }

            // Fallback to cmd.exe
            string cmdPath = Path.Combine(Environment.SystemDirectory, "cmd.exe");
            if (File.Exists(cmdPath))
            {
                return ExtractIconFromFile(cmdPath);
            }

            return null;
        }

        private bool IsFolderPath(string query)
        {
            // Check if query looks like a path
            if (query.Contains(":\\") || query.Contains(":/") ||
                query.StartsWith("documents", StringComparison.OrdinalIgnoreCase) ||
                query.StartsWith("downloads", StringComparison.OrdinalIgnoreCase) ||
                query.StartsWith("pictures", StringComparison.OrdinalIgnoreCase) ||
                query.StartsWith("images", StringComparison.OrdinalIgnoreCase) ||
                query.StartsWith("videos", StringComparison.OrdinalIgnoreCase) ||
                query.StartsWith("music", StringComparison.OrdinalIgnoreCase) ||
                query.StartsWith("desktop", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }

        private List<SuggestionItem> GetFolderSuggestions(string query)
        {
            var suggestions = new List<SuggestionItem>();

            try
            {
                string folderPath = "";

                // Map common folder names to actual paths
                string queryLower = query.ToLower().Trim();
                if (queryLower.StartsWith("documents"))
                    folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                else if (queryLower.StartsWith("downloads"))
                    folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                else if (queryLower.StartsWith("pictures") || queryLower.StartsWith("images"))
                    folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                else if (queryLower.StartsWith("videos"))
                    folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
                else if (queryLower.StartsWith("music"))
                    folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
                else if (queryLower.StartsWith("desktop"))
                    folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                else if (queryLower.Equals("c:") || queryLower.Equals("c:/") || queryLower.Equals("c:\\"))
                    folderPath = "C:\\";
                else if (queryLower.StartsWith("programfiles"))
                    folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                // Support any drive letter: c:, d:, e:, etc.
                else if (queryLower.Length == 2 && queryLower[1] == ':' && char.IsLetter(queryLower[0]))
                    folderPath = $"{queryLower[0]}:\\";
                else if (query.Contains(":\\") || query.Contains(":/"))
                    folderPath = query;

                if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
                {
                    try
                    {
                        // Get files and folders in the directory - show 50 items max (folders first)
                        var items = Directory.GetFileSystemEntries(folderPath)
                            .OrderByDescending(x => Directory.Exists(x))  // Folders first
                            .Take(50)
                            .ToList();

                        foreach (var item in items)
                        {
                            bool isDirectory = Directory.Exists(item);
                            string name = Path.GetFileName(item);
                            ImageSource? thumbnail = null;

                            // Try to create image thumbnail for image files
                            if (!isDirectory && IsImageFile(Path.GetExtension(item)))
                            {
                                try
                                {
                                    var bitmap = new BitmapImage();
                                    bitmap.BeginInit();
                                    bitmap.UriSource = new Uri(item);
                                    bitmap.DecodePixelWidth = 32;
                                    bitmap.DecodePixelHeight = 32;
                                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                    bitmap.EndInit();
                                    bitmap.Freeze();
                                    thumbnail = bitmap;
                                }
                                catch { }
                            }

                            var icon = thumbnail ?? ExtractIconFromFile(item);
                             var type = isDirectory ? SuggestionType.System :
                                        IsImageFile(Path.GetExtension(item)) ? SuggestionType.Image :
                                        IsVideoFile(Path.GetExtension(item)) ? SuggestionType.Video :
                                        IsMusicFile(Path.GetExtension(item)) ? SuggestionType.Music :
                                        SuggestionType.File;

                            suggestions.Add(new SuggestionItem(name, item, type, icon, isDirectory ? "📁 Folder" : "File"));
                        }
                    }
                    catch (UnauthorizedAccessException) { }
                }
            }
            catch { }

            return suggestions;
        }

        private int CalculateMatchScore(string text, string query)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(query)) return 0;
            text = text.ToLower();
            query = query.ToLower();

            if (text == query) return 100;
            if (text.StartsWith(query)) return 90;
            
            // Fuzzy: check if all characters of query exist in text in order
            int lastIndex = -1;
            bool allFound = true;
            foreach (char c in query)
            {
                int nextIndex = text.IndexOf(c, lastIndex + 1);
                if (nextIndex == -1)
                {
                    allFound = false;
                    break;
                }
                lastIndex = nextIndex;
            }

            if (allFound)
            {
                // Score based on how compact the match is
                return Math.Max(10, 80 - (text.Length - query.Length));
            }

            if (text.Contains(query)) return 50;
            
            return 0;
        }

        private async Task<List<string>> GetGoogleSuggestionsAsync(string query, CancellationToken ct = default)
        {
            var suggestions = new List<string>();
            try
            {
                var url = $"https://suggestqueries.google.com/complete/search?client=firefox&q={Uri.EscapeDataString(query)}";
                var response = await _httpClient.GetAsync(url, ct);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                using var jsonDoc = JsonDocument.Parse(content);
                if (jsonDoc.RootElement.GetArrayLength() > 1)
                {
                    foreach (var s in jsonDoc.RootElement[1].EnumerateArray())
                        suggestions.Add(s.GetString() ?? "");
                }
            }
            catch { }
            return suggestions;
        }

        private void SuggestionList_SelectionChanged(object sender, SelectionChangedEventArgs e) 
        {
            if (SuggestionList.SelectedItem is SuggestionItem item)
            {
                // Auto-preview on selection (keyboard navigation or single click)
                if (!string.IsNullOrEmpty(item.FilePath) && (File.Exists(item.FilePath) || Directory.Exists(item.FilePath)))
                {
                    if (Directory.Exists(item.FilePath))
                        ShowFolderPreview(item.FilePath, false); // Don't hide suggestion list yet
                    else
                        ShowFilePreview(item.FilePath, false);
                }
            }
        }
        private void SuggestionList_MouseDoubleClick(object sender, MouseButtonEventArgs e) => HandleSuggestionSelection((sender as System.Windows.Controls.ListBox)?.SelectedItem as SuggestionItem);

        private void SuggestionList_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (sender as System.Windows.Controls.ListBox)?.SelectedItem is SuggestionItem item)
            {
                HandleSuggestionSelection(item);
                e.Handled = true;
            }
        }

        private void HandleSuggestionSelection(SuggestionItem? selectedItem)
        {
            if (selectedItem == null) return;

            // Show preview untuk file/folder/image/video
            if (!string.IsNullOrEmpty(selectedItem.FilePath) && File.Exists(selectedItem.FilePath))
            {
                // Handle Image files
                if (selectedItem.Type == SuggestionType.Image || IsImageFile(Path.GetExtension(selectedItem.FilePath)))
                {
                    ShowFilePreview(selectedItem.FilePath);
                    return;
                }
                // Handle Video files
                else if (selectedItem.Type == SuggestionType.Video || IsVideoFile(Path.GetExtension(selectedItem.FilePath)))
                {
                    ShowFilePreview(selectedItem.FilePath, true);
                    return;
                }
                // Handle Music files
                else if (selectedItem.Type == SuggestionType.Music || IsMusicFile(Path.GetExtension(selectedItem.FilePath)))
                {
                    ShowFilePreview(selectedItem.FilePath, true);
                    return;
                }
                // Handle other files
                else if (selectedItem.Type == SuggestionType.File)
                {
                    ShowFilePreview(selectedItem.FilePath, true);
                    return;
                }
            }

            // Show preview untuk folder
            if ((selectedItem.Type == SuggestionType.System) && !string.IsNullOrEmpty(selectedItem.FilePath) && Directory.Exists(selectedItem.FilePath))
            {
                ShowFolderPreview(selectedItem.FilePath);
                return;
            }

            // Handle other types yang perlu close
            if (selectedItem.Type == SuggestionType.App)
            {
                ExecuteCommand(selectedItem.DisplayText);
            }
            else if (selectedItem.Type == SuggestionType.Calculator)
            {
                System.Windows.Clipboard.SetText(selectedItem.FilePath);
                ShowNotification($"✓ Copied: {selectedItem.FilePath}");
                BeginFadeOutAndClose();
            }
            else if (selectedItem.Type == SuggestionType.System)
            {
                ExecuteSystemCommand(selectedItem.FilePath);
            }
            else if (selectedItem.Type == SuggestionType.Terminal)
            {
                try
                {
                    Process.Start(new ProcessStartInfo { FileName = "cmd.exe", Arguments = $"/k {selectedItem.FilePath}", UseShellExecute = true });
                    BeginFadeOutAndClose();
                }
                catch (Exception ex) { ShowNotification($"Error: {ex.Message}", NotificationType.Error); }
            }
            else if (selectedItem.Type == SuggestionType.WebSearch)
            {
                string q = selectedItem.DisplayText.StartsWith("Search Google for") ? selectedItem.FilePath : selectedItem.DisplayText;
                ExecuteWebSearch(q);
                BeginFadeOutAndClose();
            }
        }

        private void ExecuteCommand(string query)
        {
            try
            {
                var app = _allSuggestions.FirstOrDefault(s => s.Type == SuggestionType.App && s.DisplayText.Equals(query, StringComparison.OrdinalIgnoreCase));
                if (app != null)
                {
                    Process.Start(new ProcessStartInfo(app.FilePath) { UseShellExecute = true });
                    BeginFadeOutAndClose();
                }
                else
                {
                    ExecuteWebSearch(query);
                    BeginFadeOutAndClose();
                }
            }
            catch (Exception ex) { ShowNotification($"Error: {ex.Message}", NotificationType.Error); }
        }

        private void ExecuteWebSearch(string query)
        {
            try { Process.Start(new ProcessStartInfo($"https://www.google.com/search?q={Uri.EscapeDataString(query)}") { UseShellExecute = true }); }
            catch { }
        }

        private void ExecuteSystemCommand(string command)
        {
            string action = command.ToLower();
            string msg = action == "shutdown" ? "Shutdown PC?" : action == "restart" ? "Restart PC?" : "Sleep PC?";
            if (System.Windows.MessageBox.Show(msg, "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                if (action == "sleep") System.Windows.Forms.Application.SetSuspendState(System.Windows.Forms.PowerState.Suspend, false, false);
                else Process.Start(new ProcessStartInfo("shutdown", action == "shutdown" ? "/s /t 0" : "/r /t 0") { CreateNoWindow = true, UseShellExecute = false });
                BeginFadeOutAndClose();
            }
        }


        // 5. Public Static Helper for Lazy Loading
        public static System.Windows.Media.ImageSource? GetIconForFile(string path)
        {
            if (!File.Exists(path) && !Directory.Exists(path)) return null;
            
            // Check cache
            if (_iconCache.TryGetValue(path, out var cachedIcon)) return cachedIcon;

            try 
            {
                var icon = System.Drawing.Icon.ExtractAssociatedIcon(path);
                if (icon != null)
                {
                    var imageSource = Imaging.CreateBitmapSourceFromHIcon(
                        icon.Handle,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                        
                    imageSource.Freeze(); // Crucial for cross-thread access
                    
                    // Simple Cache policy
                    if (_iconCache.Count < 500) _iconCache[path] = imageSource;
                    
                    return imageSource;
                }
            }
            catch {}
            return null;
        }

        private async Task LoadAllSuggestionsAsync()
        {
            if (_isLoadingSuggestions) return;
            _isLoadingSuggestions = true;

            try
            {
                var suggestions = new List<SuggestionItem>();
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string startMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
                string commonStartMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu);

                var paths = new[] { desktopPath, startMenuPath, commonStartMenuPath };

                await Task.Run(() =>
                {
                    // Use Parallel loop for faster disk scanning
                    Parallel.ForEach(paths, path => 
                    {
                        if (Directory.Exists(path))
                        {
                            try
                            {
                                var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
                                foreach (var file in files)
                                {
                                    if (file.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) ||
                                        file.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                                    {
                                        string name = Path.GetFileNameWithoutExtension(file);

                                        // OPTIMIZATION: Do NOT load icon here. Pass null.
                                        // The Item itself will load it when displayed (Lazy).
                                        var item = new SuggestionItem(name, file, SuggestionType.App, null);
                                        
                                        lock (suggestions)
                                        {
                                            suggestions.Add(item);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error scanning {path}: {ex.Message}");
                            }
                        }
                    });
                });
                
                // Add unique items only
                var uniqueItems = suggestions.GroupBy(x => x.DisplayText).Select(g => g.First()).ToList();
                
                System.Windows.Application.Current.Dispatcher.Invoke(() => 
                {
                    _allSuggestions.Clear();
                    _allSuggestions.AddRange(uniqueItems);
                });
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Load Error: " + ex.Message);
            }
            finally
            {
                _isLoadingSuggestions = false;
            }
        }
        
        // Helper wrapper for compatibility
        private System.Windows.Media.ImageSource? ExtractIconFromFile(string path)
        {
           return GetIconForFile(path);
        }


        private string ResolveLnkTarget(string lnkPath)
        {
            try
            {
                // Method 1: Using WScript.Shell COM
                try
                {
                    Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                    if (shellType != null)
                    {
                        dynamic? shell = Activator.CreateInstance(shellType);
                        if (shell != null)
                        {
                            dynamic? shortcut = shell.CreateShortcut(lnkPath);
                            string? targetPath = shortcut?.TargetPath;
                            if (!string.IsNullOrEmpty(targetPath))
                                return targetPath;
                        }
                    }
                }
                catch { }

                // Method 2: Direct file read (fallback)
                if (System.IO.File.Exists(lnkPath))
                {
                    byte[] data = System.IO.File.ReadAllBytes(lnkPath);
                    if (data.Length > 76)
                    {
                        // LNK format parsing (simplified)
                        int iconLocationOffset = BitConverter.ToInt32(data, 0x40);
                        if (iconLocationOffset > 0 && iconLocationOffset < data.Length - 4)
                        {
                            // Try to find executable reference
                            string asciString = System.Text.Encoding.ASCII.GetString(data);
                            var match = System.Text.RegularExpressions.Regex.Match(asciString, @"([A-Za-z]:\\[^\x00]+\.exe)");
                            if (match.Success)
                                return match.Groups[1].Value;
                        }
                    }
                }
            }
            catch { }

            return lnkPath;
        }

        private System.Drawing.Icon? TryExtractIcon(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            try
            {
                // Method 1: ExtractIcon API (best for exe files)
                IntPtr hIcon = ExtractIcon(IntPtr.Zero, filePath, 0);
                if (hIcon != IntPtr.Zero && hIcon != (IntPtr)1)
                {
                    try
                    {
                        return System.Drawing.Icon.FromHandle(hIcon);
                    }
                    catch { }
                }

                // Method 2: ExtractAssociatedIcon (works for most file types)
                return System.Drawing.Icon.ExtractAssociatedIcon(filePath);
            }
            catch { }

            return null;
        }

        private System.Windows.Media.ImageSource? ConvertIconToImageSource(System.Drawing.Icon icon)
        {
            try
            {
                using var bitmap = icon.ToBitmap();

                // Convert to proper DPI-aware bitmap
                var hBitmap = bitmap.GetHbitmap();
                try
                {
                    var imageSource = Imaging.CreateBitmapSourceFromHBitmap(
                        hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

                    return imageSource;
                }
                finally
                {
                    DeleteObject(hBitmap);
                }
            }
            catch { }

            return null;
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) { if (e.Key == Key.Escape) BeginFadeOutAndClose(); }
        private void Window_Deactivated(object sender, EventArgs e) => BeginFadeOutAndClose();
        private void SearchBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string query = (sender as System.Windows.Controls.TextBox)?.Text.Trim() ?? "";
                if (!string.IsNullOrEmpty(query))
                {
                    var list = SuggestionList;
                    if (list.Visibility == Visibility.Visible && list.SelectedItem is SuggestionItem item) HandleSuggestionSelection(item);
                    else if (list.Visibility == Visibility.Visible && list.HasItems) HandleSuggestionSelection(list.Items[0] as SuggestionItem);
                    else ExecuteCommand(query);
                }
            }
            else if (e.Key == Key.Down)
            {
                if (SuggestionList.HasItems) { SuggestionList.SelectedIndex = 0; SuggestionList.Focus(); }
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.ChangedButton == MouseButton.Left) DragMove(); }

        private void CustomShortcutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var shortcutWindow = new CustomShortcutWindow();
                shortcutWindow.Show();
                BeginFadeOutAndClose();
            }
            catch (Exception ex)
            {
                ShowNotification("Error opening settings", NotificationType.Error);
                System.Diagnostics.Debug.WriteLine($"Error opening shortcut window: {ex.Message}");
            }
        }

        // Drag & Drop Event Handlers
        private void SearchBox_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    ShowFilePreview(files[0]);
                }
            }
        }

        private void SearchBox_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                e.Effects = System.Windows.DragDropEffects.Copy;
                _dragDropArea.Visibility = Visibility.Visible;
            }
        }

        private void SearchBox_DragLeave(object sender, System.Windows.DragEventArgs e)
        {
            _dragDropArea.Visibility = Visibility.Collapsed;
        }

        private void DragDropArea_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    ShowFilePreview(files[0]);
                }
            }
            _dragDropArea.Visibility = Visibility.Collapsed;
        }

        private void DragDropArea_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                e.Effects = System.Windows.DragDropEffects.Copy;
            }
        }

        private void DragDropArea_DragLeave(object sender, System.Windows.DragEventArgs e)
        {
            // Keep visible if still dragging
        }

        // Preview Panel Event Handlers
        private void PreviewPanel_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    ShowFilePreview(files[0]);
                }
            }
        }

        private void PreviewPanel_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                e.Effects = System.Windows.DragDropEffects.Copy;
            }
        }

        private void PreviewPanel_DragLeave(object sender, System.Windows.DragEventArgs e)
        {
            // Keep preview visible
        }

        // Video Control Handlers
        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            var videoPreview = this.FindName("VideoPreview") as MediaElement;
            videoPreview?.Play();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            var videoPreview = this.FindName("VideoPreview") as MediaElement;
            videoPreview?.Pause();
        }

        private void ShowFilePreview(string filePath, bool hideSuggestions = true)
        {
            try
            {
                if (!File.Exists(filePath)) return;

                var previewPanel = this.FindName("PreviewPanel") as Border;
                var imagePreviewContainer = this.FindName("ImagePreviewContainer") as Border;
                var videoPreviewContainer = this.FindName("VideoPreviewContainer") as Border;
                var filePreviewContainer = this.FindName("FilePreviewContainer") as StackPanel;
                var imagePreview = this.FindName("ImagePreview") as System.Windows.Controls.Image;
                var videoPreview = this.FindName("VideoPreview") as MediaElement;
                var fileIconPreview = this.FindName("FileIconPreview") as System.Windows.Controls.Image;
                var fileNamePreview = this.FindName("FileNamePreview") as TextBlock;
                var fileSizePreview = this.FindName("FileSizePreview") as TextBlock;
                var previewTitle = this.FindName("PreviewTitle") as TextBlock;

                // Hide all preview containers
                if (imagePreviewContainer != null) imagePreviewContainer.Visibility = Visibility.Collapsed;
                if (videoPreviewContainer != null) videoPreviewContainer.Visibility = Visibility.Collapsed;
                if (filePreviewContainer != null) filePreviewContainer.Visibility = Visibility.Collapsed;

                string extension = Path.GetExtension(filePath).ToLower();
                FileInfo fileInfo = new FileInfo(filePath);

                // Image Preview
                if (IsImageFile(extension))
                {
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.UriSource = new Uri(filePath);
                        bitmap.EndInit();
                        if (imagePreview != null) imagePreview.Source = bitmap;
                        if (imagePreviewContainer != null) imagePreviewContainer.Visibility = Visibility.Visible;
                        if (previewTitle != null) previewTitle.Text = $"Image Preview - {Path.GetFileName(filePath)}";
                    }
                    catch { }
                }
                // Video Preview
                else if (IsVideoFile(extension))
                {
                    try
                    {
                        if (videoPreview != null) 
                        {
                            videoPreview.Source = new Uri(filePath);
                            videoPreview.Position = TimeSpan.Zero;
                            videoPreview.Play();
                        }
                        if (videoPreviewContainer != null) videoPreviewContainer.Visibility = Visibility.Visible;
                        if (previewTitle != null) previewTitle.Text = $"Video Preview - {Path.GetFileName(filePath)}";
                    }
                    catch { }
                }
                // Music Preview
                else if (IsMusicFile(extension))
                {
                    try
                    {
                        if (videoPreview != null) 
                        {
                            videoPreview.Source = new Uri(filePath);
                            videoPreview.Position = TimeSpan.Zero;
                            videoPreview.Play();
                        }
                        // Reuse video container for audio (will just show controls/waveform if any, but mostly just play)
                        if (videoPreviewContainer != null) videoPreviewContainer.Visibility = Visibility.Visible;
                        if (previewTitle != null) previewTitle.Text = $"Music Playing - {Path.GetFileName(filePath)}";
                    }
                    catch { }
                }
                // File Icon Preview
                else
                {
                    try
                    {
                        var icon = ExtractIconFromFile(filePath);
                        if (icon != null && fileIconPreview != null)
                        {
                            fileIconPreview.Source = icon;
                        }
                        if (fileNamePreview != null) fileNamePreview.Text = Path.GetFileName(filePath);
                        if (fileSizePreview != null) fileSizePreview.Text = FormatFileSize(fileInfo.Length);
                        if (filePreviewContainer != null) filePreviewContainer.Visibility = Visibility.Visible;
                        if (previewTitle != null) previewTitle.Text = "File Preview";
                    }
                    catch { }
                }

                // Show preview panel
                if (previewPanel != null) previewPanel.Visibility = Visibility.Visible;
                if (hideSuggestions) SuggestionList.Visibility = Visibility.Collapsed;

                // Show video controls if video
                var videoControlsPanel = this.FindName("VideoControlsPanel") as StackPanel;
                if (IsVideoFile(extension) || IsMusicFile(extension))
                {
                    if (videoControlsPanel != null) videoControlsPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    if (videoControlsPanel != null) videoControlsPanel.Visibility = Visibility.Collapsed;
                }

                // Hide folder preview
                var folderContentsList = this.FindName("FolderContentsList") as System.Windows.Controls.ListBox;
                if (folderContentsList != null) folderContentsList.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                ShowNotification($"Error showing preview: {ex.Message}", NotificationType.Error);
            }
        }

        private bool IsImageFile(string extension)
        {
            string[] imageExtensions = { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".svg", ".ico" };
            return imageExtensions.Contains(extension);
        }

        private bool IsVideoFile(string extension)
        {
            string[] videoExtensions = { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm" };
            return videoExtensions.Contains(extension);
        }

        private bool IsMusicFile(string extension)
        {
            string[] musicExtensions = { ".mp3", ".wav", ".wma", ".m4a", ".flac", ".ogg", ".aac" };
            return musicExtensions.Contains(extension);
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private void ShowFolderPreview(string folderPath, bool hideSuggestions = true)
        {
            if (!Directory.Exists(folderPath)) return;

            try
            {
                var previewPanel = this.FindName("PreviewPanel") as Border;
                var folderContentsList = this.FindName("FolderContentsList") as System.Windows.Controls.ListBox;
                var previewTitle = this.FindName("PreviewTitle") as TextBlock;

                // Hide all other preview types
                var imagePreviewContainer = this.FindName("ImagePreviewContainer") as Border;
                var videoPreviewContainer = this.FindName("VideoPreviewContainer") as Border;
                var filePreviewContainer = this.FindName("FilePreviewContainer") as StackPanel;
                var videoControlsPanel = this.FindName("VideoControlsPanel") as StackPanel;

                imagePreviewContainer!.Visibility = Visibility.Collapsed;
                videoPreviewContainer!.Visibility = Visibility.Collapsed;
                filePreviewContainer!.Visibility = Visibility.Collapsed;
                videoControlsPanel!.Visibility = Visibility.Collapsed;

                // Load folder contents
                var folderItems = new List<SuggestionItem>();
                try
                {
                    var items = Directory.GetFileSystemEntries(folderPath)
                        .OrderByDescending(x => Directory.Exists(x))  // Folders first
                        .Take(50)
                        .ToList();

                    foreach (var item in items)
                    {
                        bool isDirectory = Directory.Exists(item);
                        string name = Path.GetFileName(item);
                        ImageSource? thumbnail = null;

                        if (isDirectory)
                        {
                            // For folders, use folder icon - no thumbnail needed
                        }
                        else if (IsImageFile(Path.GetExtension(item)))
                        {
                            // For images, create thumbnail
                            try
                            {
                                var bitmap = new BitmapImage();
                                bitmap.BeginInit();
                                bitmap.UriSource = new Uri(item);
                                bitmap.DecodePixelWidth = 64;
                                bitmap.DecodePixelHeight = 64;
                                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                bitmap.EndInit();
                                bitmap.Freeze();
                                thumbnail = bitmap;
                            }
                            catch { }
                        }

                        var icon = isDirectory ? null : (thumbnail ?? ExtractIconFromFile(item));
                        var type = isDirectory ? SuggestionType.System :
                                   IsImageFile(Path.GetExtension(item)) ? SuggestionType.Image :
                                   IsVideoFile(Path.GetExtension(item)) ? SuggestionType.Video :
                                   IsMusicFile(Path.GetExtension(item)) ? SuggestionType.Music :
                                   SuggestionType.File;

                        var suggestion = new SuggestionItem(name, item, type, icon, isDirectory ? "📁 Folder" : "File");
                        folderItems.Add(suggestion);
                    }
                }
                catch { }

                folderContentsList!.ItemsSource = folderItems;

                // Add click handler for navigation
                folderContentsList.MouseDoubleClick -= FolderContentsList_DoubleClick;
                folderContentsList.MouseDoubleClick += FolderContentsList_DoubleClick;

                folderContentsList.Visibility = Visibility.Visible;
                previewTitle!.Text = $"📁 {Path.GetFileName(folderPath ?? "Root")}";

                previewPanel!.Visibility = Visibility.Visible;
                if (hideSuggestions) SuggestionList.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ShowFolderPreview error: {ex.Message}");
            }
        }

        private void BackFromPreviewButton_Click(object sender, RoutedEventArgs e)
        {
            var previewPanel = this.FindName("PreviewPanel") as Border;
            previewPanel!.Visibility = Visibility.Collapsed;
            SuggestionList.Visibility = Visibility.Visible;
        }

        // Drag & Drop support for folder items
        private System.Windows.Point _dragStartPoint;
        private bool _isDragging = false;

        private void FolderContentsList_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
            _isDragging = false;
        }

        private void FolderContentsList_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed && !_isDragging)
            {
                System.Windows.Point currentPos = e.GetPosition(null);
                System.Windows.Vector dragVector = _dragStartPoint - currentPos;

                if (dragVector.Length > System.Windows.SystemParameters.MinimumHorizontalDragDistance)
                {
                    _isDragging = true;
                    var listBox = sender as System.Windows.Controls.ListBox;
                    if (listBox?.SelectedItem is SuggestionItem item && File.Exists(item.FilePath))
                    {
                        try
                        {
                            var dataObject = new System.Windows.DataObject(System.Windows.DataFormats.FileDrop, new[] { item.FilePath });
                            System.Windows.DragDrop.DoDragDrop(listBox, dataObject, System.Windows.DragDropEffects.Copy);
                        }
                        catch { }
                    }
                }
            }
        }

        private void FolderContentsList_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isDragging = false;
        }

        private void FolderContentsList_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var listBox = sender as System.Windows.Controls.ListBox;
            if (listBox?.SelectedItem is SuggestionItem item)
            {
                // If folder, navigate into it
                if (Directory.Exists(item.FilePath))
                {
                    ShowFolderPreview(item.FilePath);
                }
                // If image/video, show preview
                else if (File.Exists(item.FilePath))
                {
                    ShowFilePreview(item.FilePath);
                }
                e.Handled = true;
            }
        }

        private void PreviewPanel_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Prevent closing when clicking inside panel
            e.Handled = true;
        }

        internal void BeginFadeOutAndCloseByMain()
        {
            BeginFadeOutAndClose();
        }
    }
}
