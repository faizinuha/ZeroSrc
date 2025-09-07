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
    // Enum for different types of notifications
    public enum NotificationType
    {
        Info,
        Warning,
        Error
    }

    // Converter to check if a string is empty and return Visibility
    public class StringToVisibilityConverter : IValueConverter
    {
        // Singleton instance for easy access in XAML
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
        private readonly TextBlock _notificationText;
        private readonly ListBox _suggestionList;
        private readonly TextBox _searchBox;

        public SearchOverlay()
        {
            InitializeComponent();
            _notificationText = (TextBlock)this.FindName("NotificationText");
            _suggestionList = (ListBox)this.FindName("SuggestionList");
            _searchBox = (TextBox)this.FindName("SearchBox");

            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            _appShortcuts = GetStartMenuShortcuts();

            // Additional fixed suggestions for common tasks
            _appShortcuts.Add("task manager", "taskmgr");
            _appShortcuts.Add("settings", "ms-settings:");
            _appShortcuts.Add("control panel", "control");
            _appShortcuts.Add("device manager", "devmgmt.msc");
            _appShortcuts.Add("file explorer", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            _appShortcuts.Add("all apps", "show all apps");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _searchBox?.Focus();
            var fadeIn = (Storyboard)FindResource("FadeInStoryboard");
            fadeIn.Begin(this);
        }

        public void BeginFadeOutAndClose()
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
            _notificationText.Visibility = Visibility.Visible;
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
            string query = _searchBox.Text.ToLower();
            _notificationText.Visibility = Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(query))
            {
                _suggestionList.ItemsSource = null;
                _suggestionList.Visibility = Visibility.Collapsed;
                return;
            }

            var filtered = _appShortcuts.Keys
                .Where(s => s.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Add web search as the first suggestion
            filtered.Insert(0, $"Search Google for \"{query}\"");

            _suggestionList.ItemsSource = filtered;
            _suggestionList.Visibility = filtered.Any() ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SuggestionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suggestionList.SelectedItem is string selected)
            {
                _searchBox.Text = selected;
                _searchBox.CaretIndex = selected.Length;
                ExecuteCommand(selected.ToLower());
            }
        }

        private void SuggestionList_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Trigger selection logic on mouse click
            SuggestionList_SelectionChanged(sender, null);
        }

        private void SuggestionList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (_suggestionList.SelectedItem is string s)
                {
                    ExecuteCommand(s.ToLower());
                }
            }
            else if (e.Key == Key.Up && _suggestionList.SelectedIndex == 0)
            {
                // Move focus back to the search box when at the top of the list
                _searchBox.Focus();
            }
        }
        
        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string query = _searchBox.Text.Trim();
                if (!string.IsNullOrEmpty(query))
                {
                    // Execute the command directly
                    ExecuteCommand(query.ToLower());
                }
            }
            else if (e.Key == Key.Escape)
            {
                BeginFadeOutAndClose();
            }
            else if (e.Key == Key.Down)
            {
                if (_suggestionList.HasItems)
                {
                    _suggestionList.SelectedIndex = 0;
                    _suggestionList.Focus();
                }
            }
        }

        private void ExecuteCommand(string query)
        {
            try
            {
                if (query.StartsWith("search google for"))
                {
                    string searchQuery = query.Replace("search google for \"", "").TrimEnd('"');
                    ExecuteWebSearch(searchQuery);
                    BeginFadeOutAndClose();
                    return;
                }

                if (_appShortcuts.TryGetValue(query, out var commandPath))
                {
                    if (commandPath == "show all apps")
                    {
                        var allApps = _appShortcuts.Keys.Where(k => k != "all apps").OrderBy(k => k).ToList();
                        _suggestionList.ItemsSource = allApps;
                        _suggestionList.Visibility = Visibility.Visible;
                        ShowNotification("Showing all installed app shortcuts.");
                        return; // Do not close the overlay
                    }
                    Process.Start(new ProcessStartInfo(commandPath) { UseShellExecute = true });
                    ShowNotification($"Opening: {query}", NotificationType.Info);
                    BeginFadeOutAndClose();
                    return;
                }

                ShowNotification("No shortcut found. Press Ctrl+Enter to search the web or select a suggestion.", NotificationType.Warning);
            }
            catch (Exception ex)
            {
                ShowNotification($"Failed to execute command: {ex.Message}", NotificationType.Error);
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
                ShowNotification($"Failed to open browser: {ex.Message}", NotificationType.Error);
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
                        // Add only if the key doesn't already exist to avoid duplicates
                        if (!shortcuts.ContainsKey(name.ToLower()))
                            shortcuts.Add(name.ToLower(), file);
                    }
                }
            }
            return shortcuts;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

    internal void BeginFadeOutAndCloseByMain()
    {
      throw new NotImplementedException();
    }
  }
}
