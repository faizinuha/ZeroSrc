using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
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
        Terminal
    }

    public class SuggestionItem
    {
        public string DisplayText { get; }
        public string Subtitle { get; }
        public string FilePath { get; }
        public SuggestionType Type { get; }
        public string Icon { get; }
        public System.Windows.Media.ImageSource? IconSource { get; }
        public bool IsTerminal => Type == SuggestionType.Terminal;

        public SuggestionItem(string displayText, string? filePath, SuggestionType type, System.Windows.Media.ImageSource? iconSource = null, string subtitle = "")
        {
            DisplayText = displayText;
            FilePath = filePath ?? "";
            Type = type;
            IconSource = iconSource;
            Subtitle = subtitle;

            Icon = Type switch
            {
                SuggestionType.App => "\uE770",
                SuggestionType.Calculator => "\uE8EF",
                SuggestionType.Image => "\uEB9F",
                SuggestionType.System => "\uE7E8",
                SuggestionType.Terminal => "\uE756",
                SuggestionType.WebSearch => "\uE774",
                _ => "\uE773"
            };

            if (string.IsNullOrEmpty(Subtitle))
            {
                Subtitle = Type switch
                {
                    SuggestionType.App => "Application",
                    SuggestionType.Calculator => "Calculator Result",
                    SuggestionType.System => "System Command",
                    SuggestionType.WebSearch => "Search on Google",
                    SuggestionType.Terminal => "Run command",
                    _ => ""
                };
            }
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

            // Terminal Command
            if (query.StartsWith(">"))
            {
                string cmd = query.Substring(1).Trim();
                if (!string.IsNullOrEmpty(cmd))
                {
                    combined.Add(new SuggestionItem(cmd, cmd, SuggestionType.Terminal, null, "Run command in Terminal"));
                    suggestionList!.ItemsSource = combined;
                    suggestionList.Visibility = Visibility.Visible;
                    return;
                }
            }

            // Calculator
            if (MathEvaluator.IsMathExpression(query))
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

            // Fuzzy Matching Apps
            var localSuggestions = _allSuggestions
                .Select(s => new { Item = s, Score = CalculateMatchScore(s.DisplayText, query) })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Select(x => x.Item)
                .Take(5)
                .ToList();
            combined.AddRange(localSuggestions);

            // Google Suggestions
            var googleSuggestions = await GetGoogleSuggestionsAsync(query);
            combined.AddRange(googleSuggestions.Select(s => new SuggestionItem(s, null, SuggestionType.WebSearch)));
            combined.Add(new SuggestionItem($"Search Google for \"{query}\"", query, SuggestionType.WebSearch));

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

        private async Task<List<string>> GetGoogleSuggestionsAsync(string query)
        {
            var suggestions = new List<string>();
            try
            {
                var url = $"https://suggestqueries.google.com/complete/search?client=firefox&q={Uri.EscapeDataString(query)}";
                var response = await _httpClient.GetAsync(url);
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


        private void LoadAllSuggestions()
        {
            _allSuggestions.Clear();
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string startMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
            string commonStartMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu);

            var paths = new[] { desktopPath, startMenuPath, commonStartMenuPath };
            foreach (var path in paths)
            {
                if (Directory.Exists(path))
                {
                    foreach (var file in Directory.GetFiles(path, "*.*", SearchOption.AllDirectories))
                    {
                        if (file.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                        {
                            string name = Path.GetFileNameWithoutExtension(file);
                            var icon = ExtractIconFromFile(file);
                            _allSuggestions.Add(new SuggestionItem(name, file, SuggestionType.App, icon));
                        }
                    }
                }
            }
        }

        private System.Windows.Media.ImageSource? ExtractIconFromFile(string filePath)
        {
            try
            {
                string targetPath = filePath;
                if (filePath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                        dynamic shell = Activator.CreateInstance(shellType);
                        dynamic shortcut = shell.CreateShortcut(filePath);
                        targetPath = shortcut.TargetPath;
                        if (string.IsNullOrEmpty(targetPath)) targetPath = filePath;
                    }
                    catch { targetPath = filePath; }
                }

                System.Drawing.Icon? icon = null;
                if (File.Exists(targetPath))
                {
                    IntPtr hIcon = ExtractIcon(IntPtr.Zero, targetPath, 0);
                    if (hIcon != IntPtr.Zero && hIcon != (IntPtr)1) icon = System.Drawing.Icon.FromHandle(hIcon);
                    else icon = System.Drawing.Icon.ExtractAssociatedIcon(targetPath);
                }

                if (icon != null)
                {
                    using var bitmap = icon.ToBitmap();
                    var hBitmap = bitmap.GetHbitmap();
                    try
                    {
                        return Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    }
                    finally { DeleteObject(hBitmap); }
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
        private void SearchBox_Drop(object sender, System.Windows.DragEventArgs e) { }
        private void SearchBox_DragEnter(object sender, System.Windows.DragEventArgs e) { }
        private void SearchBox_DragLeave(object sender, System.Windows.DragEventArgs e) { }
        private void DragDropArea_Drop(object sender, System.Windows.DragEventArgs e) { }
        private void DragDropArea_DragEnter(object sender, System.Windows.DragEventArgs e) { }
        private void DragDropArea_DragLeave(object sender, System.Windows.DragEventArgs e) { }

    internal void BeginFadeOutAndCloseByMain()
    {
      throw new NotImplementedException();
    }
  }
}
