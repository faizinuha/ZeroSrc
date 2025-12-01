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
        Video
    }

    public class SuggestionItem : INotifyPropertyChanged
    {
        public string DisplayText { get; }
        public string Subtitle { get; }
        public string FilePath { get; }
        public SuggestionType Type { get; }
        public string Icon { get; }
        
        private System.Windows.Media.ImageSource? _iconSource;
        public System.Windows.Media.ImageSource? IconSource 
        { 
            get => _iconSource;
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
                    _ => ""
                };
            }
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

            // Cancel previous search
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
                // Small delay to avoid searching on every keystroke
                await Task.Delay(100, ct);
                
                var combined = new List<SuggestionItem>();

                // Check if it's a folder path
                if (IsFolderPath(query))
                {
                    var folderSuggestions = GetFolderSuggestions(query);
                    combined.AddRange(folderSuggestions);
                    
                    if (combined.Count > 0)
                    {
                        suggestionList!.ItemsSource = combined;
                        suggestionList.Visibility = Visibility.Visible;
                        return;
                    }
                }

                // Terminal Command - NO PREFIX NEEDED, auto-detect
                if (IsTerminalCommand(query))
                {
                    var terminalIcon = GetTerminalIcon();
                    combined.Add(new SuggestionItem(query, query, SuggestionType.Terminal, terminalIcon, "Run in Terminal"));
                }

                // Calculator - ONLY if contains numbers
                if (ContainsNumbers(query) && MathEvaluator.IsMathExpression(query))
                {
                    var (success, result, error) = MathEvaluator.Evaluate(query);
                    if (success)
                    {
                        string formattedResult = MathEvaluator.FormatResult(result);
                        combined.Add(new SuggestionItem($"= {formattedResult}", formattedResult, SuggestionType.Calculator));
                    }
                }

                // System Commands
                string queryLower = query.ToLower();
                if (queryLower.Contains("shutdown")) combined.Add(new SuggestionItem("Shutdown PC", "shutdown", SuggestionType.System));
                if (queryLower.Contains("restart")) combined.Add(new SuggestionItem("Restart PC", "restart", SuggestionType.System));
                if (queryLower.Contains("sleep")) combined.Add(new SuggestionItem("Sleep PC", "sleep", SuggestionType.System));

                // Fuzzy Matching Apps - LIMIT TO 5 for better results
                var localSuggestions = _allSuggestions
                    .AsParallel()
                    .Select(s => new { Item = s, Score = CalculateMatchScore(s.DisplayText, query) })
                    .Where(x => x.Score > 0)
                    .OrderByDescending(x => x.Score)
                    .Take(5)
                    .Select(x => x.Item)
                    .ToList();
                combined.AddRange(localSuggestions);

                if (ct.IsCancellationRequested) return;

                // Google Suggestions - LIMIT TO 3 for better UX
                var googleSuggestions = await GetGoogleSuggestionsAsync(query, ct);
                combined.AddRange(googleSuggestions.Take(3).Select(s => new SuggestionItem(s, s, SuggestionType.WebSearch)));
                
                if (ct.IsCancellationRequested) return;

                if (combined.Count > 0)
                {
                    // Sort by type priority for better organization
                    var sortedSuggestions = combined
                        .OrderBy(x => GetTypePriority(x.Type))
                        .ToList();
                    
                    suggestionList!.ItemsSource = sortedSuggestions;
                    suggestionList.Visibility = Visibility.Visible;
                }
                else
                {
                    suggestionList!.ItemsSource = null;
                    suggestionList.Visibility = Visibility.Collapsed;
                }
            }
            catch (OperationCanceledException)
            {
                // Search was canceled, ignore
            }
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
                SuggestionType.WebSearch => 6,
                _ => 7
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
            text = text.ToLower(); query = query.ToLower();
            if (text == query) return 100;
            if (text.StartsWith(query)) return 90;
            if (text.Contains(" " + query)) return 70;
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

        private void SuggestionList_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
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

            // Show preview untuk file/folder
            if ((selectedItem.Type == SuggestionType.File || selectedItem.Type == SuggestionType.System) && !string.IsNullOrEmpty(selectedItem.FilePath))
            {
                // Cek jika folder atau file
                if (Directory.Exists(selectedItem.FilePath))
                {
                    ShowFolderPreview(selectedItem.FilePath);
                    return;
                }
                else if (File.Exists(selectedItem.FilePath))
                {
                    ShowFilePreview(selectedItem.FilePath);
                    return;
                }
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
                    foreach (var path in paths)
                    {
                        if (Directory.Exists(path))
                        {
                            try
                            {
                                foreach (var file in Directory.GetFiles(path, "*.*", SearchOption.AllDirectories))
                                {
                                    if (file.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) || 
                                        file.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                                    {
                                        string name = Path.GetFileNameWithoutExtension(file);
                                        
                                        // Extract icon synchronously but with proper error handling
                                        System.Windows.Media.ImageSource? icon = null;
                                        try
                                        {
                                            icon = ExtractIconFromFile(file);
                                        }
                                        catch { }
                                        
                                        var item = new SuggestionItem(name, file, SuggestionType.App, icon);
                                        lock (suggestions)
                                        {
                                            suggestions.Add(item);
                                        }
                                    }
                                }
                            }
                            catch (UnauthorizedAccessException) { }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error scanning {path}: {ex.Message}");
                            }
                        }
                    }
                });

                Dispatcher.Invoke(() =>
                {
                    _allSuggestions.Clear();
                    _allSuggestions.AddRange(suggestions);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadAllSuggestionsAsync error: {ex.Message}");
            }
            finally
            {
                _isLoadingSuggestions = false;
            }
        }

        private System.Windows.Media.ImageSource? ExtractIconFromFile(string filePath)
        {
            // Check cache first
            if (_iconCache.TryGetValue(filePath, out var cachedIcon))
                return cachedIcon;

            try
            {
                string targetPath = filePath;
                
                // Handle .lnk shortcut files
                if (filePath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                {
                    targetPath = ResolveLnkTarget(filePath);
                    if (string.IsNullOrEmpty(targetPath)) targetPath = filePath;
                }

                // Try multiple methods to extract icon
                System.Drawing.Icon? icon = TryExtractIcon(targetPath);

                if (icon != null)
                {
                    var imageSource = ConvertIconToImageSource(icon);
                    if (imageSource != null)
                    {
                        imageSource.Freeze();
                        _iconCache[filePath] = imageSource;
                        return imageSource;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Icon extraction error for {filePath}: {ex.Message}");
            }
            
            return null;
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
        private void CustomShortcutButton_Click(object sender, RoutedEventArgs e) { }
        
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

        // File Preview Logic
        private void ShowFilePreview(string filePath)
        {
            if (!File.Exists(filePath)) return;

            var previewPanel = this.FindName("PreviewPanel") as Border;
            var imagePreviewContainer = this.FindName("ImagePreviewContainer") as Border;
            var videoPreviewContainer = this.FindName("VideoPreviewContainer") as Border;
            var filePreviewContainer = this.FindName("FilePreviewContainer") as Border;
            var imagePreview = this.FindName("ImagePreview") as System.Windows.Controls.Image;
            var videoPreview = this.FindName("VideoPreview") as MediaElement;
            var fileIconPreview = this.FindName("FileIconPreview") as System.Windows.Controls.Image;
            var fileNamePreview = this.FindName("FileNamePreview") as TextBlock;
            var fileSizePreview = this.FindName("FileSizePreview") as TextBlock;
            var previewTitle = this.FindName("PreviewTitle") as TextBlock;

            // Hide all preview containers
            imagePreviewContainer!.Visibility = Visibility.Collapsed;
            videoPreviewContainer!.Visibility = Visibility.Collapsed;
            filePreviewContainer!.Visibility = Visibility.Collapsed;

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
                    imagePreview!.Source = bitmap;
                    imagePreviewContainer.Visibility = Visibility.Visible;
                    previewTitle!.Text = $"Image Preview - {Path.GetFileName(filePath)}";
                }
                catch { }
            }
            // Video Preview
            else if (IsVideoFile(extension))
            {
                try
                {
                    videoPreview!.Source = new Uri(filePath);
                    videoPreviewContainer.Visibility = Visibility.Visible;
                    previewTitle!.Text = $"Video Preview - {Path.GetFileName(filePath)}";
                }
                catch { }
            }
            // File Icon Preview
            else
            {
                try
                {
                    var icon = ExtractIconFromFile(filePath);
                    if (icon != null)
                    {
                        fileIconPreview!.Source = icon;
                    }
                    fileNamePreview!.Text = Path.GetFileName(filePath);
                    fileSizePreview!.Text = FormatFileSize(fileInfo.Length);
                    filePreviewContainer.Visibility = Visibility.Visible;
                    previewTitle!.Text = "File Preview";
                }
                catch { }
            }

            // Show preview panel
            previewPanel!.Visibility = Visibility.Visible;
            SuggestionList.Visibility = Visibility.Collapsed;

            // Show video controls if video
            var videoControlsPanel = this.FindName("VideoControlsPanel") as StackPanel;
            if (IsVideoFile(extension))
            {
                videoControlsPanel!.Visibility = Visibility.Visible;
            }
            else
            {
                videoControlsPanel!.Visibility = Visibility.Collapsed;
            }

            // Hide folder preview
            var folderContentsList = this.FindName("FolderContentsList") as System.Windows.Controls.ListBox;
            folderContentsList!.Visibility = Visibility.Collapsed;
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

        private void ShowFolderPreview(string folderPath)
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
                SuggestionList.Visibility = Visibility.Collapsed;
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

        private void FolderContentsList_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var listBox = sender as System.Windows.Controls.ListBox;
            if (listBox?.SelectedItem is SuggestionItem item && Directory.Exists(item.FilePath))
            {
                // Navigate to the double-clicked folder
                ShowFolderPreview(item.FilePath);
                e.Handled = true;
            }
        }

        private void PreviewPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Prevent closing when clicking inside panel
            e.Handled = true;
        }

    internal void BeginFadeOutAndCloseByMain()
    {
      throw new NotImplementedException();
    }
  }
}
