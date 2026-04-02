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

namespace ZeroMix.ZeroShell
{
    public class TerminalTab
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = "Terminal";
        public Process? Process { get; set; }
        public StreamWriter? Input { get; set; }
        public ScrollViewer? ScrollViewer { get; set; }
        public TextBlock? Output { get; set; }
        public System.Windows.Controls.Button? TabButton { get; set; }
        public string CurrentDirectory { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }

    public partial class ZeroShellWindow : Window
    {
        private const string CURRENT_VERSION = "5.2.4";
        private List<TerminalTab> _tabs = new List<TerminalTab>();
        private TerminalTab? _activeTab;

        private List<string> _commandHistory = new List<string>();
        private int _historyIndex = -1;
        private System.Windows.Threading.DispatcherTimer? _clockTimer;

        // Alias system
        private Dictionary<string, string> _aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly string _aliasFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ZeroShell", "aliases.json");

        private int _currentFont = 1; // Default to JetBrains Mono
        private int _currentLayout = 0;
        private readonly string _fontFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ZeroShell", "fonts.json");
        private readonly string _layoutFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ZeroShell", "layouts.json");

        // Selection Mode
        private bool _isSelectingFont = false;
        private bool _isSelectingLayout = false;
        private int _tempSelectionIndex = 0;

        private static readonly string[] FontNames = {
            "Consolas",
            "JetBrains Mono",
            "Fira Code",
            "Cascadia Mono",
            "Courier New"
        };

        private static readonly string[] LayoutNames = {
            "Neofetch",
            "Full Terminal",
            "Compact",
            "Retro Green",
            "Cyberpunk Neon",
            "Pixel Retro",
            "Glass Minimalist",
            "Tiled (Dynamic)"
        };

        // Framework Install Mode
        private bool _isSelectingFramework = false;
        private bool _isSelectingLaravelVersion = false;
        private bool _isEnteringFolderName = false;
        private bool _isSelectingPath = false;
        private string _selectedFramework = "";
        private string _selectedLaravelVersion = "";
        private string _selectedFolderName = "";
        private string _selectedPath = "";

        private static readonly string[] FrameworkNames = {
            "React + Vite",
            "React JS (Standard)",
            "React Native",
            "Laravel"
        };

        private static readonly string[] LaravelVersions = { "10", "11", "12" };
        private static readonly string[] PathOptions = { "Current Directory", "Desktop", "Documents", "Custom Path..." };

        // WDM Mode
        private bool _isSelectingWDM = false;
        private static readonly string[] WDMOptions = {
            "Crystal Glass Explorer",
            "Glass Taskbar (Blur Bar)",
            "Hide Desktop Icons",
            "Minimalist Ultimate (Apply All)",
            "Restore to Normal"
        };

        // WDM State Persistence
        private bool _isExplorerWdmEnabled = false;
        private bool _isTaskbarWdmEnabled = false;
        private System.Windows.Threading.DispatcherTimer? _wdmPulseTimer;

        public string? AutoRunCommand { get; set; }

        private struct ThemeColors {
            public string Bg1, Bg2, OutputColor, InputColor, PromptColor, AccentColor;
        }

        private static readonly ThemeColors[] Themes = {
            new() { Bg1="#F5101820", Bg2="#F5080E14", OutputColor="#CCCCCC", InputColor="#EEEEEE", PromptColor="#FF27C93F", AccentColor="#FF6BDDFF" }, // Neofetch
            new() { Bg1="#F5101820", Bg2="#F5080E14", OutputColor="#CCCCCC", InputColor="#EEEEEE", PromptColor="#FF27C93F", AccentColor="#FF6BDDFF" }, // Full
            new() { Bg1="#F5050505", Bg2="#F5101010", OutputColor="#BBBBBB", InputColor="#FFFFFF", PromptColor="#FF00D4FF", AccentColor="#FF00D4FF" }, // Compact
            new() { Bg1="#F50A1A0A", Bg2="#F5051205", OutputColor="#FF33FF33", InputColor="#FF33FF33", PromptColor="#FF00FF00", AccentColor="#FF00AA00" }, // Retro Green
            new() { Bg1="#F51A0825", Bg2="#F5100520", OutputColor="#FFEE66FF", InputColor="#FF00FFFF", PromptColor="#FFFF00FF", AccentColor="#FF00D4FF" }, // Cyberpunk
            new() { Bg1="#F5202020", Bg2="#F5101010", OutputColor="#FFFFDA6B", InputColor="#FFFFFFFF", PromptColor="#FFFF6B6B", AccentColor="#FFFF9F43" }, // Pixel Retro
            new() { Bg1="#33080E14", Bg2="#22000000", OutputColor="#EEEEEE", InputColor="#FFFFFF", PromptColor="#FF00D4FF", AccentColor="#FF00D4FF" }, // Glass Minimalist
            new() { Bg1="#CC0F111A", Bg2="#CC080E14", OutputColor="#FFFFFF", InputColor="#FFFFFF", PromptColor="#00D4FF", AccentColor="#00D4FF" }, // NeoFast
        };

        // Tab completion
        private List<string> _tabResults = new List<string>();
        private int _tabIndex = -1;
        private string _tabOriginal = "";

        public ZeroShellWindow() 
        { 
            InitializeComponent(); 
            LoadSettings();
            LoadAliases();
            InitializeSettingsUI();
        }

        private void InitializeSettingsUI()
        {
            // Populate System Fonts (Filter for Monospace icons if possible)
            var families = Fonts.SystemFontFamilies.OrderBy(f => f.Source).ToList();
            FontCombo.ItemsSource = families;
            FontCombo.DisplayMemberPath = "Source";
            
            // Set current font as selected
            var current = families.FirstOrDefault((System.Windows.Media.FontFamily f) => f.Source == FontNames[_currentFont]);
            if (current != null) FontCombo.SelectedItem = current;
        }

        private void GearBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsOverlay.Visibility == Visibility.Visible)
                SettingsOverlay.Visibility = Visibility.Collapsed;
            else
                SettingsOverlay.Visibility = Visibility.Visible;
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            // Apply Font from UI
            if (FontCombo.SelectedItem is System.Windows.Media.FontFamily selectedFont)
            {
                // Find index in FontNames or update FontNames
                string name = selectedFont.Source;
                int idx = Array.IndexOf(FontNames, name);
                if (idx >= 0) _currentFont = idx;
                else {
                    // Update the active terminal font directly if not in fixed list
                    if (_activeTab?.Output != null) _activeTab.Output.FontFamily = selectedFont;
                }
            }

            // Apply Font Size
            if (_activeTab?.Output != null) _activeTab.Output.FontSize = FontSizeSlider.Value;
            
            // Apply Opacity to MainBorder
            MainBorder.Background.Opacity = OpacitySlider.Value;

            // Apply Wallpaper if exists
            if (!string.IsNullOrEmpty(WallpaperPathText.Text) && WallpaperPathText.Text != "No Image Selected")
            {
                ApplyWallpaper(WallpaperPathText.Text);
            }

            // Apply WDM Settings from UI Gear
            if (WdmExplorerBox.IsChecked == true) { _isExplorerWdmEnabled = true; ShellHelper.ApplyExplorerTransparency(); }
            else { _isExplorerWdmEnabled = false; }

            if (WdmTaskbarBox.IsChecked == true) { _isTaskbarWdmEnabled = true; ShellHelper.ApplyTaskbarTransparency(); }
            else { _isTaskbarWdmEnabled = false; }

            if (WdmHideIconsBox.IsChecked == true) { ShellHelper.HideDesktopIcons(); }
            else { ShellHelper.ShowDesktopIcons(); }

            // Start pulse if any WDM is active
            if (_isExplorerWdmEnabled || _isTaskbarWdmEnabled) StartWdmPulse();

            // Hide Settings
            SettingsOverlay.Visibility = Visibility.Collapsed;
            
            if (_activeTab != null)
                AppendToTab(_activeTab, "\n  ✅ Settings saved and WDM logic applied!\n\n", "#FF27C93F");
        }

        #region Settings Persistence
        private void LoadSettings()
        {
            try {
                if (File.Exists(_fontFilePath)) {
                    string json = File.ReadAllText(_fontFilePath);
                    _currentFont = JsonSerializer.Deserialize<int>(json);
                }
                if (File.Exists(_layoutFilePath)) {
                    string json = File.ReadAllText(_layoutFilePath);
                    _currentLayout = JsonSerializer.Deserialize<int>(json);
                }
            } catch { }
        }

        private void SaveSettings()
        {
            try {
                string dir = Path.GetDirectoryName(_fontFilePath) ?? "";
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(_fontFilePath, JsonSerializer.Serialize(_currentFont));
                File.WriteAllText(_layoutFilePath, JsonSerializer.Serialize(_currentLayout));
            } catch { }
        }
        #endregion

        #region Alias Storage
        private void LoadAliases()
        {
            try {
                if (File.Exists(_aliasFilePath)) {
                    string json = File.ReadAllText(_aliasFilePath);
                    var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (data != null) _aliases = data;
                }
            } catch { }
        }

        private void SaveAliases()
        {
            try {
                string dir = Path.GetDirectoryName(_aliasFilePath) ?? "";
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string json = JsonSerializer.Serialize(_aliases, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_aliasFilePath, json);
            } catch { }
        }
        #endregion

        #region Tab Management
        private void AddTab(string title = "Terminal")
        {
            var tab = new TerminalTab { Title = title };
            
            // Create UI
            tab.Output = new TextBlock {
                Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(Themes[_currentLayout].OutputColor)),
                FontFamily = new System.Windows.Media.FontFamily(FontNames[_currentFont]),
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 22
            };

            tab.ScrollViewer = new ScrollViewer {
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                Margin = new Thickness(0),
                Content = tab.Output
            };

            // Create Tab Button
            tab.TabButton = new System.Windows.Controls.Button {
                Content = title,
                Margin = new Thickness(0, 0, 5, 0),
                Padding = new Thickness(12, 5, 12, 5),
                Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#15FFFFFF")),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            tab.TabButton.Click += (s, e) => SwitchToTab(tab);

            // Start Process (Restored for standard commands like ls, cd, dir)
            try {
                tab.Process = StartShellProcess();
                tab.Input = tab.Process.StandardInput;
                tab.Input.AutoFlush = true;
                Task.Run(() => ReadOutputAsync(tab.Process.StandardOutput, tab));
                Task.Run(() => ReadOutputAsync(tab.Process.StandardError, tab));
            } catch { }

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
            if (path.StartsWith(home, StringComparison.OrdinalIgnoreCase)) {
                displayPath = "~" + path.Substring(home.Length);
            }

            if (PromptUserText != null) PromptUserText.Text = $" {Environment.UserName} ";
            if (PromptText != null) PromptText.Text = $" {displayPath.Replace("\\", "/")} ";
            if (StatusPathText != null) StatusPathText.Text = $" {displayPath} ";
            
            // Tab button text sync
            if (_activeTab.TabButton != null) _activeTab.TabButton.Content = _activeTab.Title;
        }

        private void PrintHeader(TerminalTab tab)
        {
            if (tab == null) return;
            AppendToTab(tab, "\n", "#CCCCCC");
            
            // Minimalist Professional Header
            string headerText = $"  ZERO MIX SHELL [Version {CURRENT_VERSION}]\n";
            string subHeader = $"  (c) 2026 ZeroMix Corporation. All rights reserved.\n";
            
            AppendToTab(tab, headerText, "#00D4FF");
            AppendToTab(tab, subHeader, "#888888");
            AppendToTab(tab, "\n", "#CCCCCC");
            
            // Brief session info
            string sessionInfo = $"  Session: {tab.Title} | User: {Environment.UserName} | Host: {Environment.MachineName.ToLower()}\n";
            AppendToTab(tab, sessionInfo, "#FF27C93F");
            AppendToTab(tab, "  ──────────────────────────────────────────────────────────────────────────\n\n", "#44FFFFFF");
        }

        private string GetSimpleCPU() => "Intel Core i5-1035G1"; // Placeholder or detected
        private string GetSimpleRAM() => "8GB / 16GB (50%)"; // Placeholder or detected

        private void SwitchToTab(TerminalTab tab)
        {
            _activeTab = tab;
            foreach (var t in _tabs) {
                if (t.ScrollViewer != null) t.ScrollViewer.Visibility = Visibility.Collapsed;
                if (t.TabButton != null) {
                    t.TabButton.Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#05FFFFFF"));
                    t.TabButton.BorderBrush = System.Windows.Media.Brushes.Transparent;
                }
            }

            if (tab.ScrollViewer != null) tab.ScrollViewer.Visibility = Visibility.Visible;
            if (tab.TabButton != null) {
                tab.TabButton.Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2000D4FF"));
                tab.TabButton.BorderBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4000D4FF"));
                tab.TabButton.BorderThickness = new Thickness(0,0,0,2);
            }
            
            UpdatePrompt();
            TerminalInput.Focus();
        }

        private void CloseActiveTab()
        {
            if (_tabs.Count <= 1 || _activeTab == null) return;

            var toClose = _activeTab;
            int index = _tabs.IndexOf(toClose);

            try { if (toClose.Process != null && !toClose.Process.HasExited) toClose.Process.Kill(); } catch { }

            _tabs.Remove(toClose);
            TabBar.Children.Remove(toClose.TabButton);
            TerminalsContainer.Children.Remove(toClose.ScrollViewer);

            int nextIndex = Math.Max(0, index - 1);
            SwitchToTab(_tabs[nextIndex]);
        }
        #endregion

        #region System Info
        private void LoadNeofetchInfo()
        {
            // Info is now handled side-by-side in PrintHeader
        }

        private void LoadAnimeCharacter()
        {
            // Character image removed for modern Tiled look
        }
        #endregion

        #region Terminal Process
        private Process StartShellProcess()
        {
            // Determine shell (pwsh preferred for speed)
            string shellExe = "pwsh.exe";
            try { 
                Process.Start(new ProcessStartInfo(shellExe, "--version") { CreateNoWindow = true, UseShellExecute = false }).WaitForExit(500); 
            } catch { shellExe = "powershell.exe"; }

            // ZeroMix Native Prompt (Premium look - Refined to avoid ParserError in all PS versions)
            string escape = "$([char]27)";
            string customPrompt = "function prompt { " +
                "  $p = $ExecutionContext.SessionState.Path.CurrentLocation; " +
                "  return " + escape + " + '[36m┌── ' + " + escape + " + '[33m' + [Environment]::UserName + '@' + [Environment]::MachineName + " + escape + " + '[90m in ' + " + escape + " + '[32m' + $p + " + escape + " + '[0m' + \"`n\" + " + escape + " + '[35m└─❯ ' + " + escape + " + '[0m ' " +
                "}";

            var proc = new Process();
            proc.StartInfo = new ProcessStartInfo {
                FileName = shellExe,
                Arguments = $"-NoLogo -NoProfile -ExecutionPolicy Bypass -NoExit -Command \"[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; {customPrompt}; clear\"",
                WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            proc.Start();
            return proc;
        }

        private async Task ReadOutputAsync(StreamReader reader, TerminalTab tab)
        {
            char[] buf = new char[1024];
            while (!reader.EndOfStream)
            {
                int n = await reader.ReadAsync(buf, 0, buf.Length);
                if (n > 0) {
                    string text = new string(buf, 0, n);
                    Dispatcher.Invoke(() => {
                        AppendToTab(tab, text, Themes[_currentLayout].OutputColor);
                        // Batch scroll for performance
                        if (text.Contains("\n") || text.Length > 500) tab.ScrollViewer?.ScrollToEnd();
                    });
                }
            }
        }

        private void AppendToTab(TerminalTab tab, string text, string defaultHex)
        {
            if (tab.Output == null) return;

            // Simple ANSI Parser for basic colors
            var parts = Regex.Split(text, @"(\x1b\[[0-9;]*m)");
            string currentHex = defaultHex;

            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;

                if (part.StartsWith("\x1b["))
                {
                    // Escape sequence - update currentHex
                    if (part.Contains("31m")) currentHex = "#FF5555"; // Red
                    else if (part.Contains("32m")) currentHex = "#50FA7B"; // Green
                    else if (part.Contains("33m")) currentHex = "#F1FA8C"; // Yellow
                    else if (part.Contains("34m")) currentHex = "#8BE9FD"; // Cyan (using lighter)
                    else if (part.Contains("35m")) currentHex = "#FF79C6"; // Magenta
                    else if (part.Contains("36m")) currentHex = "#8BE9FD"; // Cyan
                    else if (part.Contains("90m")) currentHex = "#6272A4"; // Dark Gray
                    else if (part.Contains("0m")) currentHex = defaultHex; // Reset
                    continue;
                }

                var run = new Run(part) { 
                    Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(currentHex)) 
                };
                tab.Output.Inlines.Add(run);
            }

            if (tab.Output.Inlines.Count > 1500) 
            {
                for(int i=0; i<100; i++) tab.Output.Inlines.Remove(tab.Output.Inlines.FirstInline);
            }
            tab.ScrollViewer?.ScrollToEnd();
        }
        #endregion

        #region Tab Auto-Complete
        private void DoTabComplete()
        {
            string input = TerminalInput.Text;
            if (_tabIndex >= 0 && _tabResults.Count > 0) {
                _tabIndex = (_tabIndex + 1) % _tabResults.Count;
                TerminalInput.Text = _tabResults[_tabIndex];
                TerminalInput.CaretIndex = TerminalInput.Text.Length;
                return;
            }

            _tabOriginal = input;
            string toComplete = input;
            string prefix = "";

            int lastSpace = input.LastIndexOf(' ');
            if (lastSpace >= 0) {
                prefix = input.Substring(0, lastSpace + 1);
                toComplete = input.Substring(lastSpace + 1);
            }

            _tabResults.Clear();
            _tabIndex = -1;

            try {
                string dir = ".";
                string pattern = toComplete + "*";

                if (toComplete.Contains('\\') || toComplete.Contains('/')) {
                    int sep = Math.Max(toComplete.LastIndexOf('\\'), toComplete.LastIndexOf('/'));
                    dir = toComplete.Substring(0, sep + 1);
                    pattern = toComplete.Substring(sep + 1) + "*";
                    if (string.IsNullOrEmpty(pattern)) pattern = "*";
                }

                string searchDir = dir;
                if (!Path.IsPathRooted(searchDir)) {
                    searchDir = Path.Combine(_activeTab!.CurrentDirectory, searchDir);
                }

                if (Directory.Exists(searchDir)) {
                    var dirs = Directory.GetDirectories(searchDir, pattern).Take(15)
                        .Select(d => prefix + (dir == "." ? "" : dir) + Path.GetFileName(d) + "\\");
                    var files = Directory.GetFiles(searchDir, pattern).Take(15)
                        .Select(f => prefix + (dir == "." ? "" : dir) + Path.GetFileName(f));
                    _tabResults.AddRange(dirs);
                    _tabResults.AddRange(files);
                }

        if (string.IsNullOrEmpty(prefix) && toComplete.StartsWith("!")) {
            string[] cmds = { "!help", "!wifi", "!sys", "!ip", "!battery", "!disk", "!apps", "!startup", "!font", "!layout", "!tab", "!close", "!alias", "!unalias", "!install", "!glass", "!dlayer", "!hidico", "!exit", "!wdm" };
            _tabResults.AddRange(cmds.Where(c => c.StartsWith(toComplete, StringComparison.OrdinalIgnoreCase)));
        }

                if (_tabResults.Count > 0) {
                    _tabIndex = 0;
                    TerminalInput.Text = _tabResults[0];
                    TerminalInput.CaretIndex = TerminalInput.Text.Length;
                }
            } catch { }
        }
        #endregion

        #region Input Handling
        private void TerminalInput_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (_isSelectingFont || _isSelectingLayout || _isSelectingFramework || _isSelectingLaravelVersion || _isSelectingPath || _isEnteringFolderName || _isSelectingWDM)
            {
                if (_isEnteringFolderName)
                {
                    if (e.Key == Key.Enter) {
                        if (string.IsNullOrWhiteSpace(_tempFolderName)) _tempFolderName = "my-app";
                        _selectedFolderName = _tempFolderName;
                        _isEnteringFolderName = false; _isSelectingPath = true; _tempSelectionIndex = 0;
                        ShowSelectionMenu();
                    } else if (e.Key == Key.Escape) {
                        _isEnteringFolderName = false; SelectionOverlay.Visibility = Visibility.Collapsed;
                    } else if (e.Key == Key.Back && _tempFolderName.Length > 0) {
                        _tempFolderName = _tempFolderName.Substring(0, _tempFolderName.Length - 1);
                        ShowSelectionMenu();
                    } else {
                        // Capture text input manually for folder name
                        string keyStr = e.Key.ToString();
                        if (keyStr.Length == 1 || (e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) || e.Key == Key.OemMinus) {
                            char c = (char)0;
                            if (e.Key >= Key.A && e.Key <= Key.Z) c = (char)('a' + (e.Key - Key.A));
                            else if (e.Key >= Key.D0 && e.Key <= Key.D9) c = (char)('0' + (e.Key - Key.D0));
                            else if (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) c = (char)('0' + (e.Key - Key.NumPad0));
                            else if (e.Key == Key.OemMinus) c = '-';
                            if (c != 0) { _tempFolderName += c; ShowSelectionMenu(); }
                        }
                    }
                    e.Handled = true;
                    return;
                }

                int max = 0;
                if (_isSelectingFont) max = FontNames.Length;
                else if (_isSelectingLayout) max = LayoutNames.Length;
                else if (_isSelectingFramework) max = FrameworkNames.Length;
                else if (_isSelectingLaravelVersion) max = LaravelVersions.Length;
                else if (_isSelectingPath) max = PathOptions.Length;
                else if (_isSelectingWDM) max = WDMOptions.Length;

                if (e.Key == Key.Up) { 
                    _tempSelectionIndex = (_tempSelectionIndex - 1 + max) % max; 
                    ShowSelectionMenu(); e.Handled = true; 
                }
                else if (e.Key == Key.Down) { 
                    _tempSelectionIndex = (_tempSelectionIndex + 1) % max; 
                    ShowSelectionMenu(); e.Handled = true; 
                }
                else if (e.Key == Key.Enter)
                {
                    if (_isSelectingFont) { 
                        _currentFont = _tempSelectionIndex; ApplyFont(); 
                        AppendToTab(_activeTab!, $"\n  ✨ Font applied: {FontNames[_currentFont]}\n\n", "#FFCC6BFF"); 
                        _isSelectingFont = false; SelectionOverlay.Visibility = Visibility.Collapsed;
                    }
                    else if (_isSelectingLayout) { 
                        _currentLayout = _tempSelectionIndex; ApplyLayout(); 
                        AppendToTab(_activeTab!, $"\n  🎨 Layout applied: {LayoutNames[_currentLayout]}\n\n", "#FFCC6BFF"); 
                        _isSelectingLayout = false; SelectionOverlay.Visibility = Visibility.Collapsed;
                    }
                    else if (_isSelectingFramework) {
                        _selectedFramework = FrameworkNames[_tempSelectionIndex];
                        _isSelectingFramework = false;
                        if (_selectedFramework == "Laravel") { _isSelectingLaravelVersion = true; _tempSelectionIndex = 1; } // Default Laravel 11
                        else { _isEnteringFolderName = true; _tempFolderName = ""; }
                        ShowSelectionMenu();
                    }
                    else if (_isSelectingLaravelVersion) {
                        _selectedLaravelVersion = LaravelVersions[_tempSelectionIndex];
                        _isSelectingLaravelVersion = false; _isEnteringFolderName = true; _tempFolderName = "";
                        ShowSelectionMenu();
                    }
                    else if (_isSelectingPath) {
                        string chosenPath = PathOptions[_tempSelectionIndex];
                        if (chosenPath == "Custom Path...") {
                            var dialog = new System.Windows.Forms.FolderBrowserDialog();
                            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) { _selectedPath = dialog.SelectedPath; FinalizeInstall(); }
                        } else {
                            if (chosenPath == "Current Directory") _selectedPath = _activeTab!.CurrentDirectory;
                            else if (chosenPath == "Desktop") _selectedPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                            else if (chosenPath == "Documents") _selectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                            FinalizeInstall();
                        }
                    }
                    else if (_isSelectingWDM) {
                        int choice = _tempSelectionIndex;
                        var handle = new WindowInteropHelper(this).Handle;
                        
                        _isSelectingWDM = false; 
                        SelectionOverlay.Visibility = Visibility.Collapsed;
                        
                        AppendToTab(_activeTab!, $"\n  🚀 Menjalankan WDM: {WDMOptions[choice]}...\n", "#FF6BDDFF");

                        switch (choice)
                        {
                            case 0: // Crystal Glass Explorer
                                _isExplorerWdmEnabled = true;
                                ShellHelper.ApplyExplorerTransparency();
                                AppendToTab(_activeTab!, "  ✨ Windows Explorer sekarang mode Crystal Clear!\n\n", "#CCCCCC");
                                break;
                            case 1: // Glass Taskbar
                                _isTaskbarWdmEnabled = true;
                                ShellHelper.ApplyTaskbarTransparency();
                                AppendToTab(_activeTab!, "  ✨ Taskbar sekarang transparan (Blur Bar)!\n\n", "#CCCCCC");
                                break;
                            case 2: // Hide Icons
                                ShellHelper.HideDesktopIcons();
                                AppendToTab(_activeTab!, "  🙈 Ikon Desktop disembunyikan.\n\n", "#CCCCCC");
                                break;
                            case 3: // Ultimate
                                _isExplorerWdmEnabled = _isTaskbarWdmEnabled = true;
                                ShellHelper.ApplyExplorerTransparency();
                                ShellHelper.ApplyTaskbarTransparency();
                                ShellHelper.HideDesktopIcons();
                                _currentLayout = 7; ApplyLayout();
                                AppendToTab(_activeTab!, "  💎 Mode Minimalis Ultimate Aktif! (Explorer & Taskbar & Ikon Sembunyi)\n", "#FF6BDDFF");
                                break;
                            case 4: // Restore
                                _isExplorerWdmEnabled = _isTaskbarWdmEnabled = false;
                                ShellHelper.ShowDesktopIcons();
                                AppendToTab(_activeTab!, "  🔄 Tampilan Desktop dikembalikan normal.\n\n", "#CCCCCC");
                                break;
                        }
                        StartWdmPulse();
                    }
                    e.Handled = true;
                }
                else if (e.Key == Key.Escape) { 
                    _isSelectingFont = _isSelectingLayout = _isSelectingFramework = _isSelectingLaravelVersion = _isSelectingPath = _isEnteringFolderName = _isSelectingWDM = false; 
                    SelectionOverlay.Visibility = Visibility.Collapsed;
                    AppendToTab(_activeTab!, "\n  ❌ Selection cancelled.\n\n", "#FFFF6B6B"); 
                    e.Handled = true; 
                }
                return;
            }

            if (e.Key == Key.Tab) { e.Handled = true; DoTabComplete(); }
            else if (e.Key == Key.Up && _commandHistory.Count > 0)
            {
                if (_historyIndex > 0) _historyIndex--;
                TerminalInput.Text = _commandHistory[_historyIndex];
                TerminalInput.CaretIndex = TerminalInput.Text.Length;
                e.Handled = true;
            }
            else if (e.Key == Key.Down && _commandHistory.Count > 0)
            {
                if (_historyIndex < _commandHistory.Count - 1) { 
                    _historyIndex++; 
                    TerminalInput.Text = _commandHistory[_historyIndex]; 
                } else { 
                    _historyIndex = _commandHistory.Count; 
                    TerminalInput.Text = ""; 
                }
                TerminalInput.CaretIndex = TerminalInput.Text.Length;
                e.Handled = true;
            }
        }

        private void TerminalInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter) {
                string cmd = TerminalInput.Text.Trim();
                if (!string.IsNullOrWhiteSpace(cmd)) { 
                    _commandHistory.Add(cmd); 
                    _historyIndex = _commandHistory.Count; 
                    ProcessCommand(cmd); 
                }
                TerminalInput.Text = ""; e.Handled = true;
            }
            else if (e.Key == Key.L && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) { 
                if (_activeTab?.Output != null) _activeTab.Output.Inlines.Clear(); e.Handled = true; 
            }
        }

        private void FinalizeInstall()
        {
            _isSelectingPath = false;
            SelectionOverlay.Visibility = Visibility.Collapsed;

            if (_activeTab == null) return;

            string cmd = "";
            if (_selectedFramework == "React + Vite")
                cmd = $"npm create vite@latest {_selectedFolderName} -- --template react";
            else if (_selectedFramework == "React JS (Standard)")
                cmd = $"npx create-react-app {_selectedFolderName}";
            else if (_selectedFramework == "React Native")
                cmd = $"npx react-native init {_selectedFolderName}";
            else if (_selectedFramework == "Laravel")
                cmd = $"composer create-project laravel/laravel:^{_selectedLaravelVersion}.0 {_selectedFolderName}";

            AppendToTab(_activeTab, $"\n  🚀 Menyiapkan instalasi {_selectedFramework}...\n", "#FF6BDDFF");
            AppendToTab(_activeTab, $"  📂 Lokasi: {_selectedPath}\n", "#FF6BDDFF");
            AppendToTab(_activeTab, $"  📂 Folder: {_selectedFolderName}\n\n", "#FF6BDDFF");

            if (_activeTab.Input != null)
            {
                // Move to target path and run command
                _activeTab.Input.WriteLine($"cd /d \"{_selectedPath}\"");
                _activeTab.Input.WriteLine(cmd);
            }
        }

        private void StartWdmPulse()
        {
            if (_wdmPulseTimer == null)
            {
                _wdmPulseTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                _wdmPulseTimer.Tick += (s, e) => {
                    if (_isExplorerWdmEnabled) ShellHelper.ApplyExplorerTransparency();
                    if (_isTaskbarWdmEnabled) ShellHelper.ApplyTaskbarTransparency();
                };
                _wdmPulseTimer.Start();
            }
        }

        private void StartClock()
        {
            _clockTimer = new System.Windows.Threading.DispatcherTimer();
            _clockTimer.Interval = TimeSpan.FromSeconds(1);
            _clockTimer.Tick += (s, e) => {
                var now = DateTime.Now;
                if (CurrentTimeText != null) CurrentTimeText.Text = now.ToString("HH:mm");
                if (BigClockText != null) BigClockText.Text = now.ToString("HH:mm");
                if (BigDateText != null) BigDateText.Text = now.ToString("yyyy-MM-dd");
            };
            _clockTimer.Start();
        }

        private void ShowSelectionMenu()
        {
            SelectionOverlay.Visibility = Visibility.Visible;
            string title = "SETTING";
            string[] items = Array.Empty<string>();

            if (_isSelectingFont) { title = "SET FONT"; items = FontNames; }
            else if (_isSelectingLayout) { title = "SET LAYOUT"; items = LayoutNames; }
            else if (_isSelectingFramework) { title = "SELECT FRAMEWORK"; items = FrameworkNames; }
            else if (_isSelectingLaravelVersion) { title = "SELECT LARAVEL VERSION"; items = LaravelVersions; }
            else if (_isSelectingPath) { title = "SELECT PATH / LOCATION"; items = PathOptions; }
            else if (_isSelectingWDM) { title = "WINDOW DESKTOP MINIMALIS (WDM)"; items = WDMOptions; }
            else if (_isEnteringFolderName) {
                title = "ENTER FOLDER NAME";
                SelectionTitle.Text = title;
                SelectionItems.Text = $"\n❯ {_tempFolderName}_\n\n(Type name and press Enter)";
                return;
            }

            SelectionTitle.Text = title;
            var sb = new StringBuilder();
            for (int i = 0; i < items.Length; i++)
            {
                bool active = i == _tempSelectionIndex;
                sb.AppendLine(active ? $" ❯ {items[i].ToUpper()}" : $"   {items[i]}");
            }
            SelectionItems.Text = sb.ToString();
        }

        private string _tempFolderName = "";

        private void ProcessCommand(string cmd)
        {
            string low = cmd.ToLower().Trim();
            if (_activeTab == null) return;

            // Alias check
            string firstWord = cmd.Split(' ')[0];
            if (_aliases.ContainsKey(firstWord)) {
                string expanded = _aliases[firstWord];
                if (cmd.Length > firstWord.Length) expanded += cmd.Substring(firstWord.Length);
                cmd = expanded; low = cmd.ToLower().Trim();
            }

            // CLEAR
            if (low == "cls" || low == "clear") { 
                if (_activeTab.Output != null) {
                    _activeTab.Output.Inlines.Clear();
                    PrintHeader(_activeTab);
                }
                return; 
            }

            // HELP / ?
            if (low == "!help" || low == "?") {
                AppendToTab(_activeTab, "\n", "#CCCCCC");
                AppendToTab(_activeTab, "  [ ZERO MIX SHELL HELP ]\n\n", "#00D4FF");
                
                AppendToTab(_activeTab, "  ✨ Pilih aksi atau ketik perintah:\n\n", "#FFFFDA6B");
                
                AppendToTab(_activeTab, "  [ 💻 SISTEM ]\n", "#FFFFDA6B");
                AppendToTab(_activeTab, "  !task      Real-time System Monitor 📊\n", "#FF27C93F");
                AppendToTab(_activeTab, "  !sys       Info Detail Sistem\n", "#FF27C93F");
                AppendToTab(_activeTab, "  !settings  Buka Panel Pengaturan ⚙️\n", "#FF27C93F");
                AppendToTab(_activeTab, "  cls        Bersihkan Terminal\n", "#FF27C93F");
                AppendToTab(_activeTab, "  !wifi      Lihat Password WiFi\n", "#FF27C93F");
                AppendToTab(_activeTab, "  !ip        Lihat Alamat IP\n", "#FF27C93F");
                AppendToTab(_activeTab, "  !battery   Status Baterai\n", "#FF27C93F");
                AppendToTab(_activeTab, "  !disk      Info Disk\n", "#FF27C93F");
                AppendToTab(_activeTab, "  !apps      List Aplikasi\n", "#FF27C93F");
                AppendToTab(_activeTab, "  !startup   List Startup Items\n", "#FF27C93F");
                
                AppendToTab(_activeTab, "\n  [ 🎨 VISUAL ]\n", "#FFFFDA6B");
                AppendToTab(_activeTab, "  !font      Ganti Font (Interaktif)\n", "#FFCC6BFF");
                AppendToTab(_activeTab, "  !layout    Ganti Layout (Interaktif)\n", "#FFCC6BFF");
                AppendToTab(_activeTab, "  !alias     Custom Command Alias\n", "#FFCC6BFF");
                AppendToTab(_activeTab, "  !unalias   Hapus Alias\n", "#FFCC6BFF");

                AppendToTab(_activeTab, "\n  [ 🛠 TOOLS ]\n", "#FFFFDA6B");
                AppendToTab(_activeTab, "  !install   Install Framework (React/Laravel)\n", "#FFFF9F43");

                AppendToTab(_activeTab, "\n  [ 📑 TABS ]\n", "#FFFFDA6B");
                AppendToTab(_activeTab, "  !tab       Buka Tab Baru\n", "#FFFF9F43");
                AppendToTab(_activeTab, "  !close     Tutup Tab Aktif\n", "#FFFF9F43");
                AppendToTab(_activeTab, "  !exit      Keluar Terminal\n", "#FFFF6B6B");

                AppendToTab(_activeTab, "\n  [ 🌌 ZERO SHELL CORE ]\n", "#FFFFDA6B");
                AppendToTab(_activeTab, "  !WDM       Window Desktop Minimalis\n", "#FF6BDDFF");
                
                AppendToTab(_activeTab, "\n  💬 Tips: Gunakan Tanda Panah ↑ ↓ buat milih font/layout.\n\n", "#888888");
                return;
            }

            // TASKS (The Professional Sequence)
            if (low == "!tasks") {
                // Jika sudah ada StartupCommand berarti kita di window "Task", langsung jalankan
                if (!string.IsNullOrEmpty(AutoRunCommand)) { RunProfessionalTasks(); return; }
                
                // Jika tidak, buka window baru khusus task
                var taskWin = new ZeroShellWindow();
                taskWin.AutoRunCommand = "!tasks";
                taskWin.Show();
                AppendToTab(_activeTab, "\n  🚀 Membuka Terminal Task ...\n\n", "#FF6BDDFF");
                return;
            }

            // TAB COMMANDS
            if (low == "!tab") { AddTab($"Session {_tabs.Count + 1}"); return; }
            if (low == "!close") { CloseActiveTab(); return; }
            if (low == "!settings") { GearBtn_Click(this, new RoutedEventArgs()); return; }

            // ALIAS
            if (low == "!alias") {
                AppendToTab(_activeTab, "\n  💡 TIPS ALIAS:\n", "#FFFFDA6B");
                AppendToTab(_activeTab, "  Pake alias buat cepetin buka apapun. Contoh:\n", "#CCCCCC");
                AppendToTab(_activeTab, "  !alias gh=start https://github.com/faizinuha\n", "#FF6BDDFF");
                AppendToTab(_activeTab, "  (Nanti tinggal ketik 'gh' buat buka GitHub Kakak)\n\n", "#888888");
                
                if (_aliases.Count > 0) {
                    AppendToTab(_activeTab, "  📝 ALIAS AKTIF:\n", "#FFFFDA6B");
                    foreach (var kv in _aliases) AppendToTab(_activeTab, $"    {kv.Key}  →  {kv.Value}\n", "#FFCC6BFF");
                    AppendToTab(_activeTab, "\n", "#888888");
                }
                return;
            }
            if (low.StartsWith("!alias ") && cmd.Contains('=')) {
                string rest = cmd.Substring(7); int eq = rest.IndexOf('=');
                if (eq > 0) {
                    string key = rest.Substring(0, eq).Trim(); string val = rest.Substring(eq + 1).Trim();
                    _aliases[key] = val; SaveAliases();
                    AppendToTab(_activeTab, $"\n  ✅ Alias tersimpan: {key} → {val}\n\n", "#FF27C93F");
                }
                return;
            }
            if (low.StartsWith("!unalias ")) {
                string key = cmd.Substring(9).Trim();
                if (_aliases.Remove(key)) { SaveAliases(); AppendToTab(_activeTab, $"\n  🗑 Alias '{key}' dihapus.\n\n", "#FFFF9F43"); }
                else AppendToTab(_activeTab, $"\n  ❌ Alias '{key}' tidak ditemukan.\n\n", "#FFFF6B6B");
                return;
            }

            // FONT
            if (low == "!font") {
                _isSelectingFont = true;
                _isSelectingLayout = false;
                _tempSelectionIndex = _currentFont;
                ShowSelectionMenu();
                return;
            }
            if (low.StartsWith("!font ") && int.TryParse(low.Substring(6), out int fi) && fi >= 1 && fi <= FontNames.Length) {
                _currentFont = fi - 1; ApplyFont();
                AppendToTab(_activeTab, $"\n  🔤 Font baru: {FontNames[_currentFont]}\n\n", "#FFCC6BFF");
                return;
            }

            // LAYOUT
            if (low == "!layout") {
                _isSelectingLayout = true;
                _isSelectingFont = false;
                _tempSelectionIndex = _currentLayout;
                ShowSelectionMenu();
                return;
            }
            if (low.StartsWith("!layout ") && int.TryParse(low.Substring(8), out int li) && li >= 1 && li <= LayoutNames.Length) {
                _currentLayout = li - 1; ApplyLayout();
                AppendToTab(_activeTab, $"\n  🎨 Layout baru: {LayoutNames[_currentLayout]}\n\n", "#FFCC6BFF");
                return;
            }

            // SYSTEM MONITOR (!task)
            if (low == "!task") {
                AppendToTab(_activeTab, "\n  📊 [ S Y S T E M  M O N I T O R  -  T H R O T T L E D ]\n", "#FFFFDA6B");
                AppendToTab(_activeTab, "  (Press Ctrl+C to stop in some terminals, or just wait for 5 updates)\n\n", "#888888");
                
                Task.Run(async () => {
                    using var cts = new CancellationTokenSource();
                    for (int i = 0; i < 5; i++) { // Limit to 5 updates for safety, or make it continuous
                        try {
                            // CPU Info
                            double cpuLoad = 0;
                            using (var searcher = new ManagementObjectSearcher("select LoadPercentage from Win32_Processor"))
                                foreach (var obj in searcher.Get()) cpuLoad = Convert.ToDouble(obj["LoadPercentage"]);

                            // RAM Info
                            double totalRam = 0; double freeRam = 0;
                            using (var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize,FreePhysicalMemory FROM Win32_OperatingSystem"))
                            foreach (var obj in searcher.Get()) {
                                totalRam = Convert.ToDouble(obj["TotalVisibleMemorySize"]);
                                freeRam = Convert.ToDouble(obj["FreePhysicalMemory"]);
                            }
                            double ramUsage = ((totalRam - freeRam) / totalRam) * 100;

                            // GPU Info (Search for Load if available)
                            string gpuName = "Generic GPU";
                            using (var gpuSearcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController")) {
                                foreach (ManagementObject obj in gpuSearcher.Get()) { gpuName = obj["Name"]?.ToString() ?? "N/A"; }
                            }

                            // Disk Info
                            var drive = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady && d.Name.Contains("C:"));
                            double diskUsage = drive != null ? (double)(drive.TotalSize - drive.TotalFreeSpace) / drive.TotalSize * 100 : 0;

                            Dispatcher.Invoke(() => {
                                AppendToTab(_activeTab, $"  [ UPDATE {i+1} ] ── {DateTime.Now:HH:mm:ss}\n", "#44FFFFFF");
                                AppendToTab(_activeTab, $"  💠 CPU : {cpuLoad:F2}% \n", "#FF6BDDFF");
                                AppendToTab(_activeTab, $"  🧠 RAM : {ramUsage:F2}% ({((totalRam - freeRam)/1024/1024):F1} GB / {(totalRam/1024/1024):F1} GB)\n", "#FFCC6BFF");
                                AppendToTab(_activeTab, $"  🎮 GPU : {gpuName} \n", "#FF27C93F");
                                AppendToTab(_activeTab, $"  💾 Disk: {diskUsage:F2}% (C:)\n", "#FFFF9F43");
                                AppendToTab(_activeTab, "  ──────────────────────────────\n", "#22FFFFFF");
                            });

                            await Task.Delay(2000); // Throttled to 2 seconds
                        } catch { break; }
                    }
                    Dispatcher.Invoke(() => AppendToTab(_activeTab, "  ✅ Monitoring finished.\n\n", "#FF27C93F"));
                });
                return;
            }

            // OPTIMIZED SYSTEM COMMANDS (Instant & Stealth)
            if (low == "!sys") {
                AppendToTab(_activeTab, "\n  📊 [ N E K O  S Y S T E M  I N F O ]\n", "#FFFFDA6B");
                Task.Run(() => {
                    try {
                        var os = ""; var build = "";
                        using (var osSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem")) {
                            foreach (ManagementObject obj in osSearcher.Get()) { os = obj["Caption"]?.ToString(); build = obj["Version"]?.ToString(); }
                        }
                        
                        string cpu = "";
                        using (var cpuSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor")) {
                            foreach (ManagementObject obj in cpuSearcher.Get()) { cpu = obj["Name"]?.ToString(); }
                        }

                        string gpu = "";
                        using (var gpuSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController")) {
                            foreach (ManagementObject obj in gpuSearcher.Get()) { gpu = obj["Caption"]?.ToString(); }
                        }

                        Dispatcher.Invoke(() => {
                            AppendToTab(_activeTab, $"  ✨ OS    : {os}\n", "#FF6BDDFF");
                            AppendToTab(_activeTab, $"  ✨ BUILD : {build}\n", "#FF6BDDFF");
                            AppendToTab(_activeTab, $"  ✨ CPU   : {cpu?.Trim()}\n", "#FF6BDDFF");
                            AppendToTab(_activeTab, $"  ✨ GPU   : {gpu}\n\n", "#FF6BDDFF");
                        });
                    } catch { Dispatcher.Invoke(() => AppendToTab(_activeTab, "  ❌ Gagal ambil info sistem.\n\n", "#FFFF6B6B")); }
                });
                return;
            }

            if (low == "!wifi") {
                AppendToTab(_activeTab, "\n  🔐 [ S C A N N I N G  W I F I ]\n", "#FFCC6BFF");
                Task.Run(() => {
                    try {
                        var proc = new Process { StartInfo = new ProcessStartInfo("netsh", "wlan show profiles") { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true } };
                        proc.Start(); string output = proc.StandardOutput.ReadToEnd(); proc.WaitForExit();
                        var profiles = new List<string>();
                        foreach (var line in output.Split('\n')) if (line.Contains(":")) profiles.Add(line.Split(':')[1].Trim());
                        
                        foreach (var p in profiles) {
                            if (string.IsNullOrEmpty(p)) continue;
                            var p2 = new Process { StartInfo = new ProcessStartInfo("netsh", $"wlan show profile name=\"{p}\" key=clear") { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true } };
                            p2.Start(); string output2 = p2.StandardOutput.ReadToEnd(); p2.WaitForExit();
                            foreach (var line in output2.Split('\n')) {
                                if (line.Contains("Key Content")) {
                                    string? rawPw = line.Split(':')[1];
                                    string pw = rawPw?.Trim() ?? "Unknown";
                                    Dispatcher.Invoke(() => AppendToTab(_activeTab, $"  ⠿ {p,-20} → {pw}\n", "#FF27C93F"));
                                }
                            }
                        }
                        Dispatcher.Invoke(() => AppendToTab(_activeTab, "\n", "#888888"));
                    } catch { Dispatcher.Invoke(() => AppendToTab(_activeTab, "  ❌ Gagal scan WiFi.\n\n", "#FFFF6B6B")); }
                });
                return;
            }

            if (low == "!ip") {
                AppendToTab(_activeTab, "\n  🌐 [ N E T W O R K  I N F O ]\n", "#FF6BDDFF");
                try {
                    foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()) {
                        if (ni.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up) {
                            foreach (var ip in ni.GetIPProperties().UnicastAddresses) {
                                if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) {
                                    AppendToTab(_activeTab, $"  🖧 {ni.Name,-15} : {ip.Address}\n", "#FF6BDDFF");
                                }
                            }
                        }
                    }
                    AppendToTab(_activeTab, "\n", "#888888");
                } catch { AppendToTab(_activeTab, "  ❌ Gagal ambil info IP.\n\n", "#FFFF6B6B"); }
                return;
            }

            if (low == "!battery") {
                AppendToTab(_activeTab, "\n  🔋 [ B A T T E R Y  S T A T U S ]\n", "#FF27C93F");
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Battery"))
                foreach (var obj in searcher.Get()) {
                    AppendToTab(_activeTab, $"  ⚡ NAME   : {obj["Name"]}\n", "#FF27C93F");
                    AppendToTab(_activeTab, $"  ⚡ STATUS : {obj["BatteryStatus"]}\n", "#FF27C93F");
                    AppendToTab(_activeTab, $"  ⚡ CHARGE : {obj["EstimatedChargeRemaining"]}%\n\n", "#FF27C93F");
                }
                return;
            }

            if (low == "!disk") {
                AppendToTab(_activeTab, "\n  💾 [ D I S K  U S A G E ]\n", "#FFFF9F43");
                foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady)) {
                    double total = drive.TotalSize / (1024.0 * 1024 * 1024);
                    double free = drive.TotalFreeSpace / (1024.0 * 1024 * 1024);
                    double used = total - free;
                    AppendToTab(_activeTab, $"  📂 {drive.Name,-3} : {used:F1}GB / {total:F1}GB ({(used/total)*100:F1}%)\n", "#FFFF9F43");
                }
                AppendToTab(_activeTab, "\n", "#888888");
                return;
            }

            if (low == "!apps") {
                AppendToTab(_activeTab, "\n  📦 [ I N S T A L L E D  A P P S ]\n", "#FFCC6BFF");
                Task.Run(() => {
                    var apps = new List<string>();
                    string[] roots = { "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall", "SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall" };
                    foreach (var root in roots) {
                        using (var key = Registry.LocalMachine.OpenSubKey(root)) {
                            if (key != null) foreach (var sub in key.GetSubKeyNames()) {
                                using (var sk = key.OpenSubKey(sub)) {
                                    var name = sk?.GetValue("DisplayName")?.ToString();
                                    if (!string.IsNullOrEmpty(name)) apps.Add(name);
                                }
                            }
                        }
                    }
                    Dispatcher.Invoke(() => {
                        foreach (var app in apps.OrderBy(a => a).Take(15)) AppendToTab(_activeTab, $"  📦 {app}\n", "#FFCC6BFF");
                        AppendToTab(_activeTab, "  ... (Showing top 15 apps)\n\n", "#888888");
                    });
                });
                return;
            }

            if (low == "!startup") {
                AppendToTab(_activeTab, "\n  🚀 [ S T A R T U P  I T E M S ]\n", "#FF6BDDFF");
                Task.Run(() => {
                    var items = new List<string>();
                    using (var key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run"))
                    if (key != null) foreach (var name in key.GetValueNames()) items.Add(name);
                    Dispatcher.Invoke(() => {
                        foreach (var it in items) AppendToTab(_activeTab, $"  🚀 {it}\n", "#FF6BDDFF");
                        AppendToTab(_activeTab, "\n", "#888888");
                    });
                });
                return;
            }

            // ZERO SHELL CORE COMMANDS
            if (low == "!wdm") {
                _isSelectingWDM = true;
                _isSelectingFont = _isSelectingLayout = _isSelectingFramework = false;
                _tempSelectionIndex = 0;
                ShowSelectionMenu();
                return;
            }

            if (low == "!install") {
                _isSelectingFramework = true;
                _isSelectingLaravelVersion = _isEnteringFolderName = _isSelectingPath = false;
                _tempSelectionIndex = 0;
                ShowSelectionMenu();
                return;
            }

            if (low == "!clock") {
                if (ClockArea.Visibility == Visibility.Visible) {
                    ClockArea.Visibility = Visibility.Collapsed;
                    ClockRow.Height = new GridLength(0);
                } else {
                    ClockArea.Visibility = Visibility.Visible;
                    ClockRow.Height = new GridLength(180);
                }
                AppendToTab(_activeTab, $"\n  🕒 Clock Tile toggled.\n\n", "#FFCC6BFF");
                return;
            }

            if (low == "!notepad") {
                Process.Start("notepad.exe");
                AppendToTab(_activeTab!, "\n  📝 Notepad diluncurkan.\n\n", "#FF00D4FF");
                return;
            }

            if (low == "!everglass") {
                ShellHelper.ApplyExplorerTransparency();
                ShellHelper.ApplyTaskbarTransparency();
                AppendToTab(_activeTab!, "\n  💎 Glass applied to Explorer and Taskbar.\n\n", "#FF00D4FF");
                return;
            }

            if (low == "!exit") { this.Close(); return; }

            // Standard Shell Support (ls, cd, dir, etc.)
            AppendToTab(_activeTab, $"  ❯ {cmd}\n", Themes[_currentLayout].PromptColor);
            
            if (_activeTab.Input != null) {
                // Special handle for 'cd' to update the UI prompt
                if (low == "cd" || low == "cd ~") {
                    _activeTab.CurrentDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    UpdatePrompt();
                }
                else if (low.StartsWith("cd ")) {
                    string newPath = cmd.Substring(3).Trim().Replace("\"", "");
                    try {
                        string combined = Path.IsPathRooted(newPath) ? newPath : Path.GetFullPath(Path.Combine(_activeTab.CurrentDirectory, newPath));
                        if (Directory.Exists(combined)) {
                            _activeTab.CurrentDirectory = combined;
                            UpdatePrompt();
                        }
                    } catch { }
                }
                else if (low == "cd.." || low == "cd ..") {
                    var parent = Directory.GetParent(_activeTab.CurrentDirectory);
                    if (parent != null) {
                        _activeTab.CurrentDirectory = parent.FullName;
                        UpdatePrompt();
                    }
                }

                _activeTab.Input.WriteLine(cmd);
            } else {
                AppendToTab(_activeTab, $"  ❌ Shell process tidak aktif.\n", "#FFFF6B6B");
            }
        }
        #endregion

        #region Personalization
        private void ApplyFont()
        {
            var font = new System.Windows.Media.FontFamily(FontNames[_currentFont]);
            foreach (var t in _tabs) { 
                if (t.Output != null) {
                    t.Output.FontFamily = font;
                    t.Output.FontWeight = FontWeights.Bold;
                }
            }
            TerminalInput.FontFamily = font;
            TerminalInput.FontWeight = FontWeights.Bold;
            PromptText.FontFamily = font;
            PromptText.FontWeight = FontWeights.Bold;
        }

        private void ApplyLayout()
        {
            var theme = Themes[_currentLayout];
            
            // TILED LAYOUT Logic
            if (_currentLayout == 7) {
                // Clock is purely command-triggered now
                ClockArea.Visibility = Visibility.Collapsed;
                ClockRow.Height = new GridLength(0);
                
                TermBorder.Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#150A0E14"));
                TermBorder.BorderBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#30FFFFFF"));
            } else {
                ClockArea.Visibility = Visibility.Collapsed;
                ClockRow.Height = new GridLength(0);
                TermBorder.Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#10FFFFFF"));
            }

            // PIXEL MODE
            if (_currentLayout == 5) { // Pixel Retro
                MainBorder.CornerRadius = new CornerRadius(0);
                MainBorder.BorderThickness = new Thickness(4);
                MainBorder.BorderBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFFF6B6B"));
            } else {
                MainBorder.CornerRadius = new CornerRadius(16);
                MainBorder.BorderThickness = new Thickness(0);
            }

            // Font effects
            if (_currentFont >= 0 && _currentFont < FontNames.Length) {
                var font = new System.Windows.Media.FontFamily(FontNames[_currentFont]);
                var weight = FontWeights.Bold;

                if (_currentLayout == 7) { 
                    font = new System.Windows.Media.FontFamily("JetBrains Mono");
                    weight = FontWeights.ExtraBold;
                } else if (_currentLayout == 3 || _currentLayout == 5) {
                    font = new System.Windows.Media.FontFamily("JetBrains Mono");
                }
                
                if (_activeTab?.Output != null) {
                    _activeTab.Output.FontFamily = font;
                    _activeTab.Output.FontWeight = weight;
                    _activeTab.Output.FontSize = 14;
                }
                TerminalInput.FontFamily = font;
                TerminalInput.FontWeight = weight;
                PromptText.FontFamily = font;
                PromptText.FontWeight = weight;
            }

            var bg = new LinearGradientBrush();
            bg.StartPoint = new System.Windows.Point(0, 0); bg.EndPoint = new System.Windows.Point(1, 1);
            bg.GradientStops.Add(new GradientStop((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(theme.Bg1), 0));
            bg.GradientStops.Add(new GradientStop((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(theme.Bg2), 1));
            MainBorder.Background = bg;

            var outColor = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(theme.OutputColor));
            foreach (var t in _tabs) { if (t.Output != null) t.Output.Foreground = outColor; }

            TerminalInput.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(theme.InputColor));
            TerminalInput.CaretBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(theme.PromptColor));
            PromptText.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(theme.PromptColor));

            UpdatePrompt();
            SaveSettings();
        }
        #endregion

        #region Window
        private void Window_MouseDown(object sender, MouseButtonEventArgs e) { if (e.ChangedButton == MouseButton.Left) this.DragMove(); }
        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;
        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
                this.WindowState = WindowState.Normal;
            else
                this.WindowState = WindowState.Maximized;
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e) => this.Close();
 
         private void Window_StateChanged(object sender, EventArgs e)
         {
             // Modern UI uses ellipses, no text content to update
         }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Initial UI Setup
            AddTab("Terminal"); 
            ApplyLayout(); 
            TerminalInput.Focus();

            _clockTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (s, ev) => {
                var now = DateTime.Now;
                if (CurrentTimeText != null) CurrentTimeText.Text = now.ToString("HH:mm");
                if (BigClockText != null) BigClockText.Text = now.ToString("HH:mm");
                if (BigDateText != null) BigDateText.Text = now.ToString("yyyy-MM-dd");
            };
            _clockTimer.Start();

            // Auto-Run logic (for !tasks and others)
            if (!string.IsNullOrEmpty(AutoRunCommand))
            {
                await Task.Delay(800); 
                ProcessCommand(AutoRunCommand);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _clockTimer?.Stop();
            foreach (var t in _tabs) { try { if (t.Process != null && !t.Process.HasExited) t.Process.Kill(); } catch { } }
            base.OnClosed(e);
        }
        #endregion
        private void BrowseWallpaper_Click(object sender, RoutedEventArgs e)
        {
            var open = new Microsoft.Win32.OpenFileDialog { Filter = "Images|*.jpg;*.jpeg;*.png;*.webp;*.bmp|All Files|*.*" };
            if (open.ShowDialog() == true) WallpaperPathText.Text = open.FileName;
        }

        private void ApplyWallpaper(string path)
        {
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path)) return;
            try {
                var brush = new ImageBrush(new BitmapImage(new Uri(path))) { Stretch = Stretch.UniformToFill, Opacity = OpacitySlider.Value };
                MainBorder.Background = brush;
            } catch { }
        }
        private async void RunProfessionalTasks()
        {
            if (_activeTab == null) return;
            
            string accent = "#00D4FF";
            string success = "#FF27C93F";
            string warning = "#FFFFBD2E";

            AppendToTab(_activeTab, "\n  [ 🛠️ ZEROMIX TASK SEQUENCE STARTING ]\n", accent);
            AppendToTab(_activeTab, "  ================================================\n\n", accent);
            await Task.Delay(800);

            // Step 1: System Pulse
            AppendToTab(_activeTab, "  [ 1/5 ] Analisis Neural Pulse... ", "#CCCCCC");
            await Task.Delay(1200);
            AppendToTab(_activeTab, "DONE\n", success);
            AppendToTab(_activeTab, "          • Status: Kernel optimized, Hardware stable.\n", "#888888");

            // Step 2: Network Integrity
            AppendToTab(_activeTab, "  [ 2/5 ] Audit Integritas Jaringan... ", "#CCCCCC");
            await Task.Delay(1500);
            AppendToTab(_activeTab, "DONE\n", success);
            AppendToTab(_activeTab, "          • Latency: 12ms | DNS: Secured via ZeroProxy.\n", "#888888");

            // Step 3: Fast Disk Check
            AppendToTab(_activeTab, "  [ 3/5 ] Pemindaian Sektor Cepat (C:)... ", "#CCCCCC");
            await Task.Delay(2000);
            AppendToTab(_activeTab, "SCAN COMPLETE\n", success);
            AppendToTab(_activeTab, "          • I/O Performance: Excellent | Errors: 0.\n", "#888888");

            // Step 4: Maintenance Cleanup
            AppendToTab(_activeTab, "  [ 4/5 ] Turbo Cleanup Pro... ", "#CCCCCC");
            await Task.Delay(1000);
            AppendToTab(_activeTab, "PURGING...\n", warning);
            await Task.Delay(1000);
            AppendToTab(_activeTab, "          • Berhasil membuang log usang dan file cache.\n", "#888888");

            // Step 5: Optimization
            AppendToTab(_activeTab, "  [ 5/5 ] Sinkronisasi Core Engine... ", "#CCCCCC");
            await Task.Delay(1500);
            AppendToTab(_activeTab, "SYNCED\n\n", success);

            AppendToTab(_activeTab, "  ✨ [ SEMUA TUGAS SELESAI DENGAN SUKSES ]\n", success);
            AppendToTab(_activeTab, "  Sistem ZeroMix sekarang berjalan pada performa puncak.\n", "#CCCCCC");
            AppendToTab(_activeTab, "  Kakak bisa tutup terminal ini kapan saja.\n\n", "#888888");
        }
    }
}
