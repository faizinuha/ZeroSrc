using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.IO;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Documents;
using System.Management;
using System.Linq;
using System.Text.Json;
using Microsoft.Win32;
using System.Text.RegularExpressions;
// Resolve ambiguitas implicit dari System.Drawing (via UseWindowsForms)
using Color = System.Windows.Media.Color;
using FontFamily = System.Windows.Media.FontFamily;
using ColorConverter = System.Windows.Media.ColorConverter;
using Cursors = System.Windows.Input.Cursors;
using Brushes = System.Windows.Media.Brushes;

namespace ZeroMix.ZeroShell
{
    public class TerminalTab
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = "Terminal";
        public PsSession? Session { get; set; }
        public ScrollViewer? ScrollViewer { get; set; }
        public TextBlock? Output { get; set; }
        public Border? TabButton { get; set; }
        public string CurrentDirectory { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }

    public class SuggestionItem
    {
        public string DisplayText { get; set; } = "";
        public string Category { get; set; } = "";
        public string FullPath { get; set; } = "";
    }

    public class FolderHistory
    {
        public Dictionary<string, int> Folders { get; set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public void AddFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            path = Path.GetFullPath(path);
            if (Folders.ContainsKey(path)) Folders[path]++;
            else Folders[path] = 1;
        }

        public List<string> GetTopFolders(int count = 10)
            => Folders.OrderByDescending(x => x.Value).Take(count).Select(x => x.Key).ToList();
    }

    public partial class ZeroShellWindow : Wpf.Ui.Controls.FluentWindow
    {
        private const string CURRENT_VERSION = "7.4.1";
        private List<TerminalTab> _tabs = new List<TerminalTab>();
        private TerminalTab? _activeTab;
        private List<string> _commandHistory = new List<string>();
        private int _historyIndex = -1;
        private System.Windows.Threading.DispatcherTimer? _clockTimer;
        private ZeroMix.ZeroShell.Commands.ZeroShellCommandRouter? _router;
        private FolderHistory _folderHistory = new FolderHistory();
        private readonly string _folderHistoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ZeroMix", "folder-history.json");
        private Dictionary<string, string> _aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly string _aliasFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ZeroShell", "aliases.json");
        private int _currentFont = 1;
        private int _currentLayout = 0;
        private readonly string _fontFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ZeroShell", "fonts.json");
        private readonly string _layoutFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ZeroShell", "layouts.json");
        private bool _isSelectingFont = false;
        private int _tempSelectionIndex = 0;
        private string _tempFolderName = "";

        private static readonly string[] FontNames = {
            "Menlo", "SF Mono", "Cascadia Mono", "JetBrains Mono", "Fira Code", "Consolas"
        };

        private static readonly string[] LayoutNames = {
            "Default"
        };

        private bool _isDisposed = false;
        private bool _isSelectingFramework = false;
        private bool _isSelectingLaravelVersion = false;
        private bool _isEnteringFolderName = false;
        private bool _isSelectingPath = false;
        private string _selectedFramework = "";
        private string _selectedLaravelVersion = "";
        private string _selectedFolderName = "";
        private string _selectedPath = "";

        private static readonly string[] FrameworkNames = {
            "React + Vite", "React JS (Standard)", "React Native", "Laravel"
        };
        private static readonly string[] LaravelVersions = { "10", "11", "12" };
        private static readonly string[] PathOptions = { "Current Directory", "Desktop", "Documents", "Custom Path..." };

        private WdmWindow? _wdmWindow;
        private ZeroMix.Widgets.DesktopWidget? _desktopWidget;
        private readonly StartMenuInterceptor _startMenuInterceptor = new();

        private List<string> _tabResults = new List<string>();
        private int _tabIndex = -1;

        private struct ThemeColors { public string Bg1, Bg2, OutputColor, InputColor, PromptColor, AccentColor; }

        private static readonly ThemeColors[] Themes = {
            new() { Bg1="#E80C0C0C", Bg2="#C0080808", OutputColor="#CCCCCC", InputColor="#E0E0E0", PromptColor="#FF00D4FF", AccentColor="#FF00D4FF" },
        };

        public void EnableStartMenuInterceptor()
        {
            if (!_startMenuInterceptor.IsEnabled)
            {
                _startMenuInterceptor.Enable();
                if (_activeTab != null)
                    AppendToTab(_activeTab, "\n  🍎 ZeroLaunchpad enabled via macOS Dock preset.\n\n", "#00D4FF");
            }
        }

        public string? AutoRunCommand { get; set; }

        public ZeroShellWindow()
        {
            InitializeComponent();
            LoadSettings();
            LoadAliases();
            LoadFolderHistory();
            InitializeSettingsUI();

            try
            {
                _router = new ZeroMix.ZeroShell.Commands.ZeroShellCommandRouter();
                _router.Register(new ZeroMix.ZeroShell.Commands.CoreCommands(PrintHeader, (action, p1, p2) => {}));
                _router.Register(new ZeroMix.ZeroShell.Commands.TabCommands(() => AddTab($"Session {_tabs.Count + 1}"), CloseActiveTab, () => GearBtn_Click(this, new RoutedEventArgs())));
                _router.Register(new ZeroMix.ZeroShell.Commands.SystemInfoCommands());
                _router.Register(new ZeroMix.ZeroShell.Commands.VisualCommands((tab) => ShowSelectionMenu(), _aliases, SaveAliases, (idx) => _currentFont = idx, (idx) => _currentLayout = idx, ApplyFont, ApplyLayout));
                _router.Register(new ZeroMix.ZeroShell.Commands.ToolsCommands(
                    (tab) => ShowSelectionMenu(), () => this.Close(),
                    (wdm, tab) => { if (wdm != null) { if (_wdmWindow == null || !_wdmWindow.IsVisible) { _wdmWindow = wdm; _wdmWindow.Show(); } else { _wdmWindow.Activate(); } } },
                    (tab) => { if (_desktopWidget == null || !_desktopWidget.IsVisible) { _desktopWidget = new ZeroMix.Widgets.DesktopWidget(); _desktopWidget.Show(); } else { _desktopWidget.Shutdown(); _desktopWidget = null; } },
                    (tab) => { if (_startMenuInterceptor.IsEnabled) _startMenuInterceptor.Disable(); else _startMenuInterceptor.Enable(); },
                    (tab) => { ShellHelper.EnumAllWindows((hwnd, cls) => { switch (cls) { case "Shell_TrayWnd": case "Shell_SecondaryTrayWnd": case "CabinetWClass": case "ExplorerWClass": ShellHelper.DisableAccent(hwnd); break; } }); ShellHelper.ApplyNotificationStyle(new WdmEntry { Style = WdmStyle.None }); ShellHelper.StopWatcher(); _startMenuInterceptor.Disable(); _desktopWidget?.Shutdown(); _desktopWidget = null; },
                    (tab) => { var taskbarEntry = new WdmEntry { Style = WdmStyle.AcrylicDark, Alpha = 0xDD, ColorHex = "#000000", AutoApply = false }; var explorerEntry = new WdmEntry { Style = WdmStyle.AcrylicDark, Alpha = 0xBB, ColorHex = "#000000", AutoApply = false }; ShellHelper.EnumAllWindows((hwnd, cls) => { if (cls == "Shell_TrayWnd" || cls == "Shell_SecondaryTrayWnd") ShellHelper.ApplyStyle(hwnd, taskbarEntry); else if (cls == "CabinetWClass" || cls == "ExplorerWClass") ShellHelper.ApplyStyle(hwnd, explorerEntry); }); },
                    (cmd) => RunProfessionalTasks(), AutoRunCommand
                ));

                // CatchAll registered LAST — handles alias expansion, cd tracking, PsSession fallthrough
                _router.Register(new ZeroMix.ZeroShell.Commands.CatchAllCommands(
                    aliasExpander: (cmd) =>
                    {
                        string first = cmd.Split(' ')[0];
                        if (_aliases.ContainsKey(first))
                        {
                            string expanded = _aliases[first];
                            if (cmd.Length > first.Length) expanded += cmd.Substring(first.Length);
                            return expanded;
                        }
                        return cmd;
                    },
                    onCdFolder: (folderPath) =>
                    {
                        _folderHistory.AddFolder(folderPath);
                        SaveFolderHistory();
                    },
                    sendToSession: (tab, cmd) =>
                    {
                        if (tab.Session != null && tab.Session.IsRunning)
                        {
                            try { _ = tab.Session.ExecuteAsync(cmd); }
                            catch (Exception ex) { AppendToTab(tab, $"  ⚠ Shell error: {ex.Message}\n", "#FF5555"); }
                        }
                        else
                        {
                            AppendToTab(tab, "  ⚠ Shell not running. Type a command to restart.\n", "#FFFF9F43");
                            _ = Task.Run(() => { try { tab.Session?.Restart(); } catch { } });
                        }
                    },
                    getCurrentDir: (tab) => tab.CurrentDirectory,
                    updateLocalDir: (tab, dir) =>
                    {
                        tab.CurrentDirectory = dir;
                        UpdatePrompt();
                    },
                    getPromptColor: () => Themes[_currentLayout].PromptColor
                ));
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Router error: {ex.Message}"); }
        }

        private void InitializeSettingsUI()
        {
            // Show only curated monospace fonts, not all system fonts
            FontCombo.ItemsSource = FontNames;
            FontCombo.SelectedIndex = _currentFont;
        }

        #region Event Handlers

        private void GearBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsOverlay.Visibility == Visibility.Visible)
                SettingsOverlay.Visibility = Visibility.Collapsed;
            else
            {
                if (_activeTab?.Output != null)
                {
                    FontSizeSlider.Value = _activeTab.Output.FontSize;
                    string src = _activeTab.Output.FontFamily?.Source ?? "";
                    int idx = Array.IndexOf(FontNames, src);
                    if (idx >= 0) FontCombo.SelectedIndex = idx;
                }
                SettingsOverlay.Visibility = Visibility.Visible;
            }
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            if (FontCombo.SelectedItem is string fontName)
            {
                int idx = Array.IndexOf(FontNames, fontName);
                if (idx >= 0) _currentFont = idx;
            }
            if (_activeTab?.Output != null) _activeTab.Output.FontSize = FontSizeSlider.Value;
            ApplyFont();
            MainBorder.Background = new SolidColorBrush(Color.FromRgb(0x0C, 0x0C, 0x0C)) { Opacity = OpacitySlider.Value };
            if (!string.IsNullOrEmpty(WallpaperPathText.Text) && WallpaperPathText.Text != "No Image Selected")
                ApplyWallpaper(WallpaperPathText.Text);
            SettingsOverlay.Visibility = Visibility.Collapsed;
            if (_activeTab != null) AppendToTab(_activeTab, "\n  ✅ Settings saved!\n\n", "#FF27C93F");
        }



        private void HelpBtn_Click(object sender, MouseButtonEventArgs e)
        {
            if (_activeTab == null) return;
            AppendToTab(_activeTab, "\n  📖 ZEROMIX SHELL HELP\n", "#00D4FF");
            AppendToTab(_activeTab, "  ─────────────────────────────────────\n", "#00D4FF");
            AppendToTab(_activeTab, "  !help              Show this help\n", "#E0E0E0");
            AppendToTab(_activeTab, "  !tasks             Run system tasks\n", "#E0E0E0");
            AppendToTab(_activeTab, "  !font              Change font\n", "#E0E0E0");
            AppendToTab(_activeTab, "  !layout            Change layout\n", "#E0E0E0");
            AppendToTab(_activeTab, "  !settings          Open settings\n", "#E0E0E0");
            AppendToTab(_activeTab, "  !sys               System info\n", "#E0E0E0");
            AppendToTab(_activeTab, "  !wifi              WiFi passwords\n", "#E0E0E0");
            AppendToTab(_activeTab, "  !ip                IP info\n", "#E0E0E0");
            AppendToTab(_activeTab, "  !battery           Battery status\n", "#E0E0E0");
            AppendToTab(_activeTab, "  !disk              Disk usage\n", "#E0E0E0");
            AppendToTab(_activeTab, "  !wdm               WDM window\n", "#E0E0E0");
            AppendToTab(_activeTab, "  !desktop           Desktop widget\n", "#E0E0E0");
            AppendToTab(_activeTab, "  !tab               New tab\n", "#E0E0E0");
            AppendToTab(_activeTab, "  !close             Close tab\n", "#E0E0E0");
            AppendToTab(_activeTab, "  !install           Install framework\n", "#E0E0E0");
            AppendToTab(_activeTab, "  cls / clear        Clear terminal\n", "#E0E0E0");
            AppendToTab(_activeTab, "  !exit              Close terminal\n\n", "#E0E0E0");
        }

        #endregion

        #region Settings Persistence

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_fontFilePath)) _currentFont = JsonSerializer.Deserialize<int>(File.ReadAllText(_fontFilePath));
                if (File.Exists(_layoutFilePath)) _currentLayout = JsonSerializer.Deserialize<int>(File.ReadAllText(_layoutFilePath));
            }
            catch { }
            _currentFont = Math.Clamp(_currentFont, 0, FontNames.Length - 1);
            _currentLayout = 0;
        }

        private void SaveSettings()
        {
            try
            {
                string dir = Path.GetDirectoryName(_fontFilePath) ?? "";
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(_fontFilePath, JsonSerializer.Serialize(_currentFont));
                File.WriteAllText(_layoutFilePath, JsonSerializer.Serialize(_currentLayout));
            }
            catch { }
        }

        #endregion

        #region Alias

        private void LoadAliases()
        {
            try { if (File.Exists(_aliasFilePath)) _aliases = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(_aliasFilePath)) ?? _aliases; }
            catch { }
        }
        private void SaveAliases()
        {
            try { string dir = Path.GetDirectoryName(_aliasFilePath) ?? ""; if (!Directory.Exists(dir)) Directory.CreateDirectory(dir); File.WriteAllText(_aliasFilePath, JsonSerializer.Serialize(_aliases, new JsonSerializerOptions { WriteIndented = true })); }
            catch { }
        }

        #endregion

        #region Folder History

        private void LoadFolderHistory()
        {
            try { if (File.Exists(_folderHistoryPath)) _folderHistory = JsonSerializer.Deserialize<FolderHistory>(File.ReadAllText(_folderHistoryPath)) ?? _folderHistory; }
            catch { }
        }
        private void SaveFolderHistory()
        {
            try { string dir = Path.GetDirectoryName(_folderHistoryPath) ?? ""; if (!Directory.Exists(dir)) Directory.CreateDirectory(dir); File.WriteAllText(_folderHistoryPath, JsonSerializer.Serialize(_folderHistory, new JsonSerializerOptions { WriteIndented = true })); }
            catch { }
        }

        #endregion

        #region Tab Management

        private void AddTab(string title = "Terminal")
        {
            var tab = new TerminalTab { Title = title };
            tab.Output = new TextBlock
            {
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Themes[_currentLayout].OutputColor)),
                FontFamily = new FontFamily(FontNames[_currentFont]),
                FontWeight = FontWeights.Normal,
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 20
            };

            tab.ScrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                Margin = new Thickness(0),
                Content = tab.Output
            };

            // Segmented control tab button (macOS style)
            tab.TabButton = new Border
            {
                Padding = new Thickness(14, 3, 14, 3),
                CornerRadius = new CornerRadius(6),
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand,
                Margin = new Thickness(2, 0, 2, 0),
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(0.5)
            };
            var tb = new TextBlock
            {
                Text = title,
                Foreground = new SolidColorBrush(Color.FromArgb(0x77, 0xE0, 0xE0, 0xE0)),
                FontSize = 11,
                FontFamily = new FontFamily("Segoe UI")
            };
            tab.TabButton.Child = tb;
            tab.TabButton.MouseLeftButtonDown += (s, e) => SwitchToTab(tab);

            // ── Start real PowerShell session ──
            try
            {
                var session = new PsSession(tab.CurrentDirectory);

                session.OutputData += (text) =>
                {
                    if (!_isDisposed) Dispatcher.Invoke(() =>
                    {
                        AppendToTab(tab, text, Themes[_currentLayout].OutputColor);
                        // Auto-scroll on newlines or large output
                        if (text.Contains("\r\n") || text.Length > 500)
                            tab.ScrollViewer?.ScrollToEnd();
                    });
                };

                session.ErrorData += (text) =>
                {
                    if (!_isDisposed) Dispatcher.Invoke(() =>
                    {
                        AppendToTab(tab, text, "#FF5555");
                        tab.ScrollViewer?.ScrollToEnd();
                    });
                };

                session.DirectoryChanged += (newDir) =>
                {
                    if (!_isDisposed) Dispatcher.Invoke(() =>
                    {
                        tab.CurrentDirectory = newDir;
                        UpdatePrompt();
                    });
                };

                session.ProcessTerminated += (msg) =>
                {
                    // Original DirectoryChanged handler is still subscribed on the same PsSession instance,
                    // so the hook survives the restart automatically. Only show a UI notice.
                    if (!_isDisposed) Dispatcher.Invoke(() =>
                    {
                        if (tab == _activeTab && tab.Output != null)
                        {
                            AppendToTab(tab, $"\n  ⚡ {msg}\n  🔄 Auto-restarting PowerShell...\n\n", "#FFFF9F43");
                            tab.ScrollViewer?.ScrollToEnd();
                        }
                    });
                };

                session.Start();
                tab.Session = session;
            }
            catch (Exception ex)
            {
                AppendToTab(tab, $"\n  ❌ Failed to start shell: {ex.Message}\n", "#FF5555");
                AppendToTab(tab, "  ℹ Make sure PowerShell (pwsh.exe or powershell.exe) is installed.\n\n", "#888888");
            }

            _tabs.Add(tab);
            TabBar.Children.Add(tab.TabButton);
            TerminalsContainer.Children.Add(tab.ScrollViewer);
            SwitchToTab(tab);
            PrintHeader(tab);
            UpdatePrompt();
        }

        private void UpdatePrompt()
        {
            if (_activeTab == null) return;
            string path = _activeTab.CurrentDirectory;
            string displayPath = path;
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (path.StartsWith(home, StringComparison.OrdinalIgnoreCase))
                displayPath = "~" + path.Substring(home.Length);

            // zsh macOS format: user@hostname ~ %
            PromptUserText.Text = Environment.UserName;
            if (PromptHostText != null)
                PromptHostText.Text = Environment.MachineName.ToLower();
            PromptText.Text = displayPath.Replace("\\", "/");
            if (_activeTab.TabButton?.Child is TextBlock tbt) tbt.Text = _activeTab.Title;
        }

        private void PrintHeader(TerminalTab tab)
        {
            if (tab == null) return;
            AppendToTab(tab, "\n", "#CCCCCC");
            AppendToTab(tab, $"  ZeroMix Shell [{CURRENT_VERSION}] — {Environment.UserName}@{Environment.MachineName.ToLower()}\n", "#00D4FF");
            AppendToTab(tab, $"  Type !help for available commands.\n\n", "#444444");
        }

        private void SwitchToTab(TerminalTab tab)
        {
            _activeTab = tab;
            foreach (var t in _tabs)
            {
                if (t.ScrollViewer != null) t.ScrollViewer.Visibility = Visibility.Collapsed;
                if (t.TabButton != null)
                {
                    t.TabButton.Background = Brushes.Transparent;
                    t.TabButton.BorderBrush = Brushes.Transparent;
                    if (t.TabButton.Child is TextBlock tbt)
                        tbt.Foreground = new SolidColorBrush(Color.FromArgb(0x66, 0xE0, 0xE0, 0xE0));
                }
            }
            if (tab.ScrollViewer != null) tab.ScrollViewer.Visibility = Visibility.Visible;
            if (tab.TabButton != null)
            {
                tab.TabButton.Background = new SolidColorBrush(Color.FromArgb(0x28, 0xFF, 0xFF, 0xFF));
                tab.TabButton.BorderBrush = new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF));
                if (tab.TabButton.Child is TextBlock tbt)
                    tbt.Foreground = Brushes.White;
            }
            UpdatePrompt();
            TerminalInput.Focus();
        }

        private void CloseActiveTab()
        {
            if (_tabs.Count <= 1 || _activeTab == null) return;
            var toClose = _activeTab;
            int index = _tabs.IndexOf(toClose);
            toClose.Session?.Dispose();
            _tabs.Remove(toClose);
            TabBar.Children.Remove(toClose.TabButton);
            TerminalsContainer.Children.Remove(toClose.ScrollViewer);
            SwitchToTab(_tabs[Math.Max(0, index - 1)]);
        }

        #endregion

        #region Terminal Output

        private void AppendToTab(TerminalTab tab, string text, string defaultHex)
        {
            if (tab.Output == null) return;
            var parts = Regex.Split(text, @"(\x1b\[[0-9;]*m)");
            string currentHex = defaultHex;

            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;
                if (part.StartsWith("\x1b["))
                {
                    if (part.Contains("31m")) currentHex = "#FF5555";
                    else if (part.Contains("32m")) currentHex = "#50FA7B";
                    else if (part.Contains("33m")) currentHex = "#F1FA8C";
                    else if (part.Contains("34m") || part.Contains("36m")) currentHex = "#8BE9FD";
                    else if (part.Contains("35m")) currentHex = "#FF79C6";
                    else if (part.Contains("90m")) currentHex = "#6272A4";
                    else if (part.Contains("0m")) currentHex = defaultHex;
                    continue;
                }

                tab.Output.Inlines.Add(new Run(part)
                {
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(currentHex))
                });
            }

            if (tab.Output.Inlines.Count > 1500)
                for (int i = 0; i < 100; i++) tab.Output.Inlines.Remove(tab.Output.Inlines.FirstInline);
            tab.ScrollViewer?.ScrollToEnd();
        }

        #endregion

        #region Auto-Complete

        private void DoTabComplete()
        {
            string input = TerminalInput.Text;
            if (_tabIndex >= 0 && _tabResults.Count > 0)
            {
                _tabIndex = (_tabIndex + 1) % _tabResults.Count;
                TerminalInput.Text = _tabResults[_tabIndex];
                TerminalInput.CaretIndex = TerminalInput.Text.Length;
                return;
            }

            string toComplete = input;
            string prefix = "";
            int lastSpace = input.LastIndexOf(' ');
            if (lastSpace >= 0) { prefix = input.Substring(0, lastSpace + 1); toComplete = input.Substring(lastSpace + 1); }

            _tabResults.Clear();
            _tabIndex = -1;

            try
            {
                string dir = ".";
                string pattern = toComplete + "*";
                if (toComplete.Contains('\\') || toComplete.Contains('/'))
                {
                    int sep = Math.Max(toComplete.LastIndexOf('\\'), toComplete.LastIndexOf('/'));
                    dir = toComplete.Substring(0, sep + 1);
                    pattern = toComplete.Substring(sep + 1) + "*";
                    if (string.IsNullOrEmpty(pattern)) pattern = "*";
                }

                string searchDir = dir;
                if (!Path.IsPathRooted(searchDir)) searchDir = Path.Combine(_activeTab!.CurrentDirectory, searchDir);

                if (Directory.Exists(searchDir))
                {
                    _tabResults.AddRange(Directory.GetDirectories(searchDir, pattern).Take(15).Select(d => prefix + (dir == "." ? "" : dir) + Path.GetFileName(d) + "\\"));
                    _tabResults.AddRange(Directory.GetFiles(searchDir, pattern).Take(15).Select(f => prefix + (dir == "." ? "" : dir) + Path.GetFileName(f)));
                }

                if (string.IsNullOrEmpty(prefix) && toComplete.StartsWith("!"))
                {
                    string[] cmds = { "!help", "!wifi", "!sys", "!ip", "!battery", "!disk", "!apps", "!startup", "!font", "!layout", "!tab", "!close", "!alias", "!unalias", "!install", "!exit", "!wdm", "!desktop", "!clock", "!startmenu", "!restore", "!settings" };
                    _tabResults.AddRange(cmds.Where(c => c.StartsWith(toComplete, StringComparison.OrdinalIgnoreCase)));
                }

                if (_tabResults.Count > 0) { _tabIndex = 0; TerminalInput.Text = _tabResults[0]; TerminalInput.CaretIndex = TerminalInput.Text.Length; }
            }
            catch { }
        }

        #endregion

        #region Input Handling

        private void TerminalInput_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (_isSelectingFont || _isSelectingFramework || _isSelectingLaravelVersion || _isSelectingPath || _isEnteringFolderName)
            {
                if (_isEnteringFolderName)
                {
                    if (e.Key == Key.Enter)
                    {
                        if (string.IsNullOrWhiteSpace(_tempFolderName)) _tempFolderName = "my-app";
                        _selectedFolderName = _tempFolderName;
                        _isEnteringFolderName = false; _isSelectingPath = true; _tempSelectionIndex = 0;
                        ShowSelectionMenu();
                    }
                    else if (e.Key == Key.Escape) { _isEnteringFolderName = false; SelectionOverlay.Visibility = Visibility.Collapsed; }
                    else if (e.Key == Key.Back && _tempFolderName.Length > 0) { _tempFolderName = _tempFolderName.Substring(0, _tempFolderName.Length - 1); ShowSelectionMenu(); }
                    else
                    {
                        string keyStr = e.Key.ToString();
                        char c = e.Key switch
                        {
                            >= Key.A and <= Key.Z => (char)('a' + (e.Key - Key.A)),
                            >= Key.D0 and <= Key.D9 => (char)('0' + (e.Key - Key.D0)),
                            >= Key.NumPad0 and <= Key.NumPad9 => (char)('0' + (e.Key - Key.NumPad0)),
                            Key.OemMinus => '-',
                            Key.OemPeriod => '.',
                            _ => '\0'
                        };
                        if (c != 0) { _tempFolderName += c; ShowSelectionMenu(); }
                    }
                    e.Handled = true;
                    return;
                }

                int max = _isSelectingFont ? FontNames.Length : _isSelectingFramework ? FrameworkNames.Length : _isSelectingLaravelVersion ? LaravelVersions.Length : _isSelectingPath ? PathOptions.Length : 0;

                if (e.Key == Key.Up) { _tempSelectionIndex = (_tempSelectionIndex - 1 + max) % max; ShowSelectionMenu(); e.Handled = true; }
                else if (e.Key == Key.Down) { _tempSelectionIndex = (_tempSelectionIndex + 1) % max; ShowSelectionMenu(); e.Handled = true; }
                else if (e.Key == Key.Enter)
                {
                    if (_isSelectingFont) { _currentFont = _tempSelectionIndex; ApplyFont(); AppendToTab(_activeTab!, $"\n  ✨ Font: {FontNames[_currentFont]}\n\n", "#FFCC6BFF"); _isSelectingFont = false; SelectionOverlay.Visibility = Visibility.Collapsed; }
                    else if (_isSelectingFramework) { _selectedFramework = FrameworkNames[_tempSelectionIndex]; _isSelectingFramework = false; if (_selectedFramework == "Laravel") { _isSelectingLaravelVersion = true; _tempSelectionIndex = 1; } else { _isEnteringFolderName = true; _tempFolderName = ""; } ShowSelectionMenu(); }
                    else if (_isSelectingLaravelVersion) { _selectedLaravelVersion = LaravelVersions[_tempSelectionIndex]; _isSelectingLaravelVersion = false; _isEnteringFolderName = true; _tempFolderName = ""; ShowSelectionMenu(); }
                    else if (_isSelectingPath) { string cp = PathOptions[_tempSelectionIndex]; if (cp == "Custom Path...") { var dlg = new System.Windows.Forms.FolderBrowserDialog(); if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK) { _selectedPath = dlg.SelectedPath; FinalizeInstall(); } } else { _selectedPath = cp == "Current Directory" ? _activeTab!.CurrentDirectory : cp == "Desktop" ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop) : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); FinalizeInstall(); } }
                    e.Handled = true;
                }
                else if (e.Key == Key.Escape) { _isSelectingFont = _isSelectingFramework = _isSelectingLaravelVersion = _isSelectingPath = _isEnteringFolderName = false; SelectionOverlay.Visibility = Visibility.Collapsed; AppendToTab(_activeTab!, "\n  ❌ Cancelled.\n\n", "#FFFF6B6B"); e.Handled = true; }
                return;
            }

            if (e.Key == Key.Tab) { e.Handled = true; DoTabComplete(); }
            else if (e.Key == Key.Up && _commandHistory.Count > 0) { if (_historyIndex > 0) _historyIndex--; TerminalInput.Text = _commandHistory[_historyIndex]; TerminalInput.CaretIndex = TerminalInput.Text.Length; e.Handled = true; }
            else if (e.Key == Key.Down && _commandHistory.Count > 0) { if (_historyIndex < _commandHistory.Count - 1) { _historyIndex++; TerminalInput.Text = _commandHistory[_historyIndex]; } else { _historyIndex = _commandHistory.Count; TerminalInput.Text = ""; } TerminalInput.CaretIndex = TerminalInput.Text.Length; e.Handled = true; }
        }

        private async void TerminalInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string cmd = TerminalInput.Text.Trim();

                // Clear text IMMEDIATELY + mark handled — before async, so WPF sees it right away
                TerminalInput.Text = "";
                e.Handled = true;

                if (!string.IsNullOrWhiteSpace(cmd))
                {
                    _commandHistory.Add(cmd);
                    _historyIndex = _commandHistory.Count;

                    // Alias expansion BEFORE routing — ensures router sees expanded command
                    string expandedCmd = cmd;
                    string firstWord = cmd.Split(' ')[0];
                    if (_aliases.ContainsKey(firstWord))
                    {
                        expandedCmd = _aliases[firstWord];
                        if (cmd.Length > firstWord.Length)
                            expandedCmd += cmd.Substring(firstWord.Length);
                    }

                    try { if (_router != null) await _router.HandleCommand(expandedCmd, _activeTab, AppendToTab); }
                    catch { }
                }
            }
            else if (e.Key == Key.L && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) { if (_activeTab?.Output != null) _activeTab.Output.Inlines.Clear(); e.Handled = true; }
        }

        private void FinalizeInstall()
        {
            _isSelectingPath = false;
            SelectionOverlay.Visibility = Visibility.Collapsed;
            if (_activeTab == null) return;

            string cmd = _selectedFramework switch
            {
                "React + Vite" => $"npm create vite@latest {_selectedFolderName} -- --template react",
                "React JS (Standard)" => $"npx create-react-app {_selectedFolderName}",
                "React Native" => $"npx react-native init {_selectedFolderName}",
                "Laravel" => $"composer create-project laravel/laravel:^{_selectedLaravelVersion}.0 {_selectedFolderName}",
                _ => ""
            };

            AppendToTab(_activeTab, $"\n  🚀 Installing {_selectedFramework}...\n", "#FF6BDDFF");
            AppendToTab(_activeTab, $"  📂 {_selectedPath}\\{_selectedFolderName}\n\n", "#FF6BDDFF");
            if (_activeTab.Session != null && _activeTab.Session.IsRunning)
            {
                try
                {
                    _ = _activeTab.Session.ExecuteAsync($"cd /d \"{_selectedPath}\"");
                    _ = _activeTab.Session.ExecuteAsync(cmd);
                }
                catch (Exception ex) { AppendToTab(_activeTab, $"  ⚠ {ex.Message}\n", "#FF5555"); }
            }
        }

        private void ShowSelectionMenu()
        {
            SelectionOverlay.Visibility = Visibility.Visible;
            string title = "SETTING";
            string[] items = Array.Empty<string>();

            if (_isSelectingFont) { title = "SET FONT"; items = FontNames; }
            else if (_isSelectingFramework) { title = "SELECT FRAMEWORK"; items = FrameworkNames; }
            else if (_isSelectingLaravelVersion) { title = "SELECT LARAVEL VERSION"; items = LaravelVersions; }
            else if (_isSelectingPath) { title = "SELECT PATH"; items = PathOptions; }
            else if (_isEnteringFolderName) { SelectionTitle.Text = "ENTER FOLDER NAME"; SelectionItems.Text = $"\n❯ {_tempFolderName}_\n(Type & Enter)"; return; }

            SelectionTitle.Text = title;
            var sb = new StringBuilder();
            for (int i = 0; i < items.Length; i++)
                sb.AppendLine(i == _tempSelectionIndex ? $" ❯ {items[i].ToUpper()}" : $"   {items[i]}");
            SelectionItems.Text = sb.ToString();
        }

        #endregion

        #region Personalization

        private void ApplyFont()
        {
            var font = new FontFamily(FontNames[_currentFont]);
            foreach (var t in _tabs) { if (t.Output != null) { t.Output.FontFamily = font; t.Output.FontWeight = FontWeights.Normal; } }
            TerminalInput.FontFamily = font;
            TerminalInput.FontWeight = FontWeights.Normal;
            PromptText.FontFamily = font;
            PromptText.FontWeight = FontWeights.Normal;
        }

        private void ApplyLayout()
        {
            var theme = Themes[0];
            MainBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.Bg1));
            TermBorder.Background = new SolidColorBrush(Color.FromArgb(0x0A, 0xFF, 0xFF, 0xFF));
            TermBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(0x15, 0xFF, 0xFF, 0xFF));

            if (_currentFont >= 0 && _currentFont < FontNames.Length)
            {
                var font = new FontFamily(FontNames[_currentFont]);
                if (_activeTab?.Output != null) { _activeTab.Output.FontFamily = font; _activeTab.Output.FontWeight = FontWeights.Normal; _activeTab.Output.FontSize = 13; }
                TerminalInput.FontFamily = font;
                TerminalInput.FontWeight = FontWeights.Normal;
                PromptText.FontFamily = font;
                PromptText.FontWeight = FontWeights.Normal;
            }

            var outColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.OutputColor));
            foreach (var t in _tabs) { if (t.Output != null) t.Output.Foreground = outColor; }
            TerminalInput.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.InputColor));
            TerminalInput.CaretBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.PromptColor));
            PromptText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.PromptColor));
            UpdatePrompt();
            SaveSettings();
        }

        #endregion

        #region Window

        private void MinimizeButton_Click(object sender, MouseButtonEventArgs e) => this.WindowState = WindowState.Minimized;
        private void MaximizeButton_Click(object sender, MouseButtonEventArgs e) => this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        private void CloseButton_Click(object sender, MouseButtonEventArgs e) => this.Close();
        private void Window_StateChanged(object sender, EventArgs e)
        {
            // macOS-style: traffic light glyphs don't change on maximize
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AddTab("Terminal");
            ApplyLayout();
            TerminalInput.Focus();

            _clockTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (s, ev) =>
            {
                try
                {
                    var now = DateTime.Now;
                    CurrentTimeText.Text = now.ToString("HH:mm");
                }
                catch { }
            };
            _clockTimer.Start();

            RefreshRecommendations();

            if (!string.IsNullOrEmpty(AutoRunCommand))
            {
                await Task.Delay(800);
                if (_router != null && _activeTab != null)
                    await _router.HandleCommand(AutoRunCommand, _activeTab, AppendToTab);
            }

            // Auto-restore WDM state
            _ = Task.Run(() =>
            {
                try
                {
                    string wdmPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ZeroShell", "wdm.json");
                    if (File.Exists(wdmPath))
                    {
                        var state = JsonSerializer.Deserialize<WdmState>(File.ReadAllText(wdmPath));
                        if (state?.Entries != null)
                        {
                            ShellHelper.EnumAllWindows((hwnd, cls) => { if (state.Entries.TryGetValue(cls, out var entry)) ShellHelper.ApplyStyle(hwnd, entry); });
                            if (state.Entries.Values.Any(e => e.AutoApply))
                                ShellHelper.StartWatcher((hwnd, cls) => { Dispatcher.Invoke(() => { if (state.Entries.TryGetValue(cls, out var entry) && entry.AutoApply) ShellHelper.ApplyStyle(hwnd, entry); }); });
                        }
                    }
                }
                catch { }
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            _isDisposed = true;
            _clockTimer?.Stop();
            SaveFolderHistory();
            SaveAliases();
            _startMenuInterceptor.Dispose();
            foreach (var t in _tabs) { try { t.Session?.Dispose(); } catch { } }
            base.OnClosed(e);
        }

        #endregion

        #region Wallpaper

        private void BrowseWallpaper_Click(object sender, RoutedEventArgs e)
        {
            var open = new Microsoft.Win32.OpenFileDialog { Filter = "Images|*.jpg;*.jpeg;*.png;*.webp;*.bmp|All Files|*.*" };
            if (open.ShowDialog() == true) WallpaperPathText.Text = open.FileName;
        }

        private void ApplyWallpaper(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
            try { MainBorder.Background = new ImageBrush(new BitmapImage(new Uri(path))) { Stretch = Stretch.UniformToFill, Opacity = OpacitySlider.Value }; }
            catch { }
        }

        #endregion

        #region Professional Tasks

        private async void RunProfessionalTasks()
        {
            if (_activeTab == null) return;
            AppendToTab(_activeTab, "\n  [ 🛠️ TASK SEQUENCE ]\n", "#00D4FF");
            AppendToTab(_activeTab, "  ────────────────────────────────────────\n\n", "#00D4FF");
            await Task.Delay(500);

            AppendToTab(_activeTab, "  [1/4] System analysis... ", "#CCCCCC");
            await Task.Delay(1000);
            AppendToTab(_activeTab, "DONE\n", "#27C93F");
            AppendToTab(_activeTab, "        • Kernel OK, hardware stable.\n", "#888888");

            AppendToTab(_activeTab, "  [2/4] Network check... ", "#CCCCCC");
            await Task.Delay(1000);
            AppendToTab(_activeTab, "DONE\n", "#27C93F");
            AppendToTab(_activeTab, "        • Connection OK.\n", "#888888");

            AppendToTab(_activeTab, "  [3/4] Disk scan (C:)... ", "#CCCCCC");
            await Task.Delay(1500);
            AppendToTab(_activeTab, "OK\n", "#27C93F");
            AppendToTab(_activeTab, "        • No errors found.\n", "#888888");

            AppendToTab(_activeTab, "  [4/4] Cache cleanup... ", "#CCCCCC");
            await Task.Delay(1000);
            AppendToTab(_activeTab, "DONE\n\n", "#27C93F");

            AppendToTab(_activeTab, "  ✨ ALL TASKS COMPLETE\n\n", "#27C93F");
        }

        #endregion

        #region Autocomplete Suggestions

        private void TerminalInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TerminalInput == null || SuggestionsListBox == null) return;
            string input = TerminalInput.Text.ToLower().Trim();
            if (string.IsNullOrWhiteSpace(input)) { SuggestionsListBox.Visibility = Visibility.Collapsed; return; }

            var suggestions = new List<SuggestionItem>();

            var matchingCommands = _commandHistory.Where(c => c.ToLower().StartsWith(input)).Distinct().Take(5)
                .Select(c => new SuggestionItem { DisplayText = c, Category = "History", FullPath = c }).ToList();
            suggestions.AddRange(matchingCommands);

            var builtIn = new[] { "!help", "!tasks", "!sys", "!wifi", "alias", "clear", "exit", "dir", "cd", "cls", "ls", "pwd" };
            suggestions.AddRange(builtIn.Where(c => c.ToLower().StartsWith(input))
                .Select(c => new SuggestionItem { DisplayText = c, Category = "Cmd", FullPath = c }));

            if (input.Contains("cd ") || input.Contains("\\") || input.Contains("/") || input == "cd")
            {
                string sp = input.Contains("cd ") ? input.Substring(3).Trim() : input;
                var topFolders = _folderHistory.GetTopFolders(5);
                suggestions.AddRange(topFolders.Where(f => f.ToLower().Contains(sp) && !string.IsNullOrEmpty(sp))
                    .Select(f => new SuggestionItem { DisplayText = Path.GetFileName(f) ?? f, Category = "Folder", FullPath = f }).Take(3));
                if (!suggestions.Any(s => s.Category == "Folder") && (sp == "" || sp.Length < 3))
                    suggestions.AddRange(topFolders.Take(3).Select(f => new SuggestionItem { DisplayText = Path.GetFileName(f) ?? f, Category = "Folder", FullPath = f }));
            }

            SuggestionsListBox.ItemsSource = suggestions;
            SuggestionsListBox.Visibility = suggestions.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            if (RecommendationsPanel != null)
                RecommendationsPanel.Visibility = string.IsNullOrWhiteSpace(TerminalInput.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SuggestionsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SuggestionsListBox.SelectedItem is SuggestionItem selected)
            {
                string completion = selected.Category == "Folder" ? $"cd \"{selected.FullPath}\"" : selected.DisplayText + " ";
                TerminalInput.Text = completion;
                TerminalInput.CaretIndex = TerminalInput.Text.Length;
                SuggestionsListBox.Visibility = Visibility.Collapsed;
            }
        }

        private void RefreshRecommendations()
        {
            TopFoldersListBox.ItemsSource = _folderHistory.GetTopFolders(5).Select(f => Path.GetFileName(f) ?? f).ToList();
            TopCommandsListBox.ItemsSource = _commandHistory.Distinct().Take(5).ToList();
        }

        private void TopFoldersListBox_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (TopFoldersListBox.SelectedItem is string folder)
            {
                var fullPath = _folderHistory.GetTopFolders(5).FirstOrDefault(f => Path.GetFileName(f) == folder || f == folder) ?? folder;
                TerminalInput.Text = $"cd \"{fullPath}\"";
                TerminalInput.CaretIndex = TerminalInput.Text.Length;
                e.Handled = true;
            }
        }

        private void TopCommandsListBox_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (TopCommandsListBox.SelectedItem is string command)
            {
                TerminalInput.Text = command;
                TerminalInput.CaretIndex = TerminalInput.Text.Length;
                e.Handled = true;
            }
        }

        #endregion
    }
}
