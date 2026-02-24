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
using System.Windows.Documents;
using System.Management;
using System.Linq;
using System.Text.Json;
using Microsoft.Win32;

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
    }

    public partial class ZeroShellWindow : Window
    {
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
            "Cyberpunk Neon"
        };

        private struct ThemeColors {
            public string Bg1, Bg2, OutputColor, InputColor, PromptColor, AccentColor;
        }

        private static readonly ThemeColors[] Themes = {
            new() { Bg1="#F5101820", Bg2="#F5080E14", OutputColor="#CCCCCC", InputColor="#EEEEEE", PromptColor="#FF27C93F", AccentColor="#FF6BDDFF" },
            new() { Bg1="#F5101820", Bg2="#F5080E14", OutputColor="#CCCCCC", InputColor="#EEEEEE", PromptColor="#FF27C93F", AccentColor="#FF6BDDFF" },
            new() { Bg1="#F5101820", Bg2="#F5080E14", OutputColor="#CCCCCC", InputColor="#EEEEEE", PromptColor="#FF27C93F", AccentColor="#FF6BDDFF" },
            new() { Bg1="#F50A1A0A", Bg2="#F5051205", OutputColor="#FF33FF33", InputColor="#FF33FF33", PromptColor="#FF00FF00", AccentColor="#FF00AA00" },
            new() { Bg1="#F51A0825", Bg2="#F5100520", OutputColor="#FFEE66FF", InputColor="#FF00FFFF", PromptColor="#FFFF00FF", AccentColor="#FF00D4FF" },
        };

        // Tab completion
        private List<string> _tabResults = new List<string>();
        private int _tabIndex = -1;
        private string _tabOriginal = "";

        public ZeroShellWindow() 
        { 
            InitializeComponent(); 
            LoadAliases();
        }

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

            // Start Process (Removed cmd.exe fallback for Pure Neko experience)
            tab.Process = null;
            tab.Input = null;

            _tabs.Add(tab);
            TabBar.Children.Add(tab.TabButton);
            TerminalsContainer.Children.Add(tab.ScrollViewer);

            SwitchToTab(tab);
            PrintHeader(tab);
        }

        private void PrintHeader(TerminalTab tab)
        {
            if (tab == null) return;
            AppendToTab(tab, "\n", "#CCCCCC");
            AppendToTab(tab, "  █▄  █ █▀▀ █ █ █▀▀█ \n", "#FF6BDDFF");
            AppendToTab(tab, "  █ █ █ █▀▀ █▄▀ █  █ \n", "#FF6BDDFF");
            AppendToTab(tab, "  ▀  ▀▀ ▀▀▀ ▀  ▀ ▀▀▀▀ \n", "#FF6BDDFF");
            AppendToTab(tab, "  [ N E K O  T E R M I N A L ]\n\n", "#FF6BDDFF");
        }

        private void SwitchToTab(TerminalTab tab)
        {
            _activeTab = tab;
            foreach (var t in _tabs) {
                if (t.ScrollViewer != null) t.ScrollViewer.Visibility = Visibility.Collapsed;
                if (t.TabButton != null) t.TabButton.Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#15FFFFFF"));
            }

            if (tab.ScrollViewer != null) tab.ScrollViewer.Visibility = Visibility.Visible;
            if (tab.TabButton != null) tab.TabButton.Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#30FFFFFF"));
            
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
            try {
                OsInfoText.Text = $"{Environment.OSVersion.Platform} {Environment.OSVersion.Version}";
                KernelText.Text = $"NT {Environment.OSVersion.Version.Major}.{Environment.OSVersion.Version.Minor}.{Environment.OSVersion.Version.Build}";
                var up = TimeSpan.FromMilliseconds(Environment.TickCount64);
                UptimeText.Text = up.Hours > 0 ? $"{up.Hours}h {up.Minutes}m" : $"{up.Minutes} mins";
                ShellText.Text = "PowerShell / ZeroMix Engine";
                UserNameText.Text = $" {Environment.UserName} ";

                Task.Run(() => {
                    try {
                        string cpu = "Unknown", gpu = "Unknown";
                        long totalRam = 0, freeRam = 0;
                        using (var s = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor"))
                            foreach (var o in s.Get()) { cpu = o["Name"]?.ToString() ?? cpu; break; }
                        using (var s = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController"))
                            foreach (var o in s.Get()) { gpu = o["Name"]?.ToString() ?? gpu; break; }
                        using (var s = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem"))
                            foreach (var o in s.Get()) { totalRam = Convert.ToInt64(o["TotalVisibleMemorySize"]) / 1024; freeRam = Convert.ToInt64(o["FreePhysicalMemory"]) / 1024; break; }
                        long used = totalRam - freeRam;
                        double pct = totalRam > 0 ? (used * 100.0 / totalRam) : 0;
                        Dispatcher.Invoke(() => {
                            CpuInfoText.Text = cpu; GpuInfoText.Text = gpu;
                            RamInfoText.Text = $"{used / 1024.0:F2} GiB / {totalRam / 1024.0:F2} GiB ({pct:F0}%)";
                            try { using (var s2 = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem")) foreach (var o2 in s2.Get()) { OsInfoText.Text = o2["Caption"]?.ToString() ?? OsInfoText.Text; break; } } catch { }
                        });
                    } catch { }
                });
            } catch { }
        }

        private void LoadAnimeCharacter()
        {
            string[] paths = {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ZeroShell", "character.png"),
                Path.Combine(Directory.GetCurrentDirectory(), "ZeroShell", "character.png"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "ZeroShell", "character.png")
            };
            foreach (var p in paths) {
                if (File.Exists(p)) {
                    try {
                        var bmp = new BitmapImage(); bmp.BeginInit(); bmp.UriSource = new Uri(p, UriKind.Absolute);
                        bmp.CacheOption = BitmapCacheOption.OnLoad; bmp.EndInit();
                        AnimeCharImage.Source = bmp; CharPlaceholder.Visibility = Visibility.Collapsed;
                    } catch { }
                    break;
                }
            }
        }
        #endregion

        #region Terminal Process
        private Process StartShellProcess()
        {
            var proc = new Process();
            proc.StartInfo = new ProcessStartInfo {
                FileName = "cmd.exe",
                Arguments = "-NoLogo -NoProfile -ExecutionPolicy Bypass",
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
            char[] buf = new char[512];
            while (!reader.EndOfStream)
            {
                int n = await reader.ReadAsync(buf, 0, buf.Length);
                if (n > 0) {
                    string text = new string(buf, 0, n);
                    Dispatcher.Invoke(() => {
                        AppendToTab(tab, text, Themes[_currentLayout].OutputColor);
                        tab.ScrollViewer?.ScrollToEnd();
                    });
                }
            }
        }

        private void AppendToTab(TerminalTab tab, string text, string hex)
        {
            if (tab.Output == null) return;
            var run = new Run(text) { Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex)) };
            tab.Output.Inlines.Add(run);
            if (tab.Output.Inlines.Count > 1200) tab.Output.Inlines.Remove(tab.Output.Inlines.FirstInline);
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
                    string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    searchDir = Path.Combine(home, searchDir);
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
            string[] cmds = { "!help", "!wifi", "!sys", "!ip", "!battery", "!disk", "!apps", "!startup", "!font", "!layout", "!tab", "!close", "!alias", "!unalias", "!exit" };
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
            if (_isSelectingFont || _isSelectingLayout)
            {
                int max = _isSelectingFont ? FontNames.Length : LayoutNames.Length;
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
                        _currentFont = _tempSelectionIndex; 
                        ApplyFont(); 
                        AppendToTab(_activeTab!, $"\n  ✨ Font applied: {FontNames[_currentFont]}\n\n", "#FFCC6BFF"); 
                    }
                    else { 
                        _currentLayout = _tempSelectionIndex; 
                        ApplyLayout(); 
                        AppendToTab(_activeTab!, $"\n  🎨 Layout applied: {LayoutNames[_currentLayout]}\n\n", "#FFCC6BFF"); 
                    }
                    _isSelectingFont = _isSelectingLayout = false;
                    SelectionOverlay.Visibility = Visibility.Collapsed;
                    e.Handled = true;
                }
                else if (e.Key == Key.Escape) { 
                    _isSelectingFont = _isSelectingLayout = false; 
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

        private void ShowSelectionMenu()
        {
            SelectionOverlay.Visibility = Visibility.Visible;
            SelectionTitle.Text = _isSelectingFont ? "SET FONT" : "SET LAYOUT";
            string[] items = _isSelectingFont ? FontNames : LayoutNames;

            var sb = new StringBuilder();
            for (int i = 0; i < items.Length; i++)
            {
                bool active = i == _tempSelectionIndex;
                sb.AppendLine(active ? $" ❯ {items[i].ToUpper()}" : $"   {items[i]}");
            }
            SelectionItems.Text = sb.ToString();
        }

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
                AppendToTab(_activeTab, "  ███╗   ██╗███████╗██╗  ██╗ ██████╗ \n", "#FF6BDDFF");
                AppendToTab(_activeTab, "  ████╗  ██║██╔════╝██║ ██╔╝██╔═══██╗\n", "#FF6BDDFF");
                AppendToTab(_activeTab, "  ██╔██╗ ██║█████╗  █████╔╝ ██║   ██║\n", "#FF6BDDFF");
                AppendToTab(_activeTab, "  ██║╚██╗██║██╔══╝  ██╔═██╗ ██║   ██║\n", "#FF6BDDFF");
                AppendToTab(_activeTab, "  ██║ ╚████║███████╗██║  ██╗╚██████╔╝\n", "#FF6BDDFF");
                AppendToTab(_activeTab, "  ╚═╝  ╚═══╝╚══════╝╚═╝  ╚═╝ ╚═════╝ \n", "#FF6BDDFF");
                AppendToTab(_activeTab, "           N E K O  T E R M I N A L\n\n", "#FF6BDDFF");
                
                AppendToTab(_activeTab, "  ✨ Pilih aksi atau ketik perintah:\n\n", "#FFFFDA6B");
                
                AppendToTab(_activeTab, "  [ 💻 SISTEM ]\n", "#FFFFDA6B");
                AppendToTab(_activeTab, "  !sys       Info Detail Sistem\n", "#FF27C93F");
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

                AppendToTab(_activeTab, "\n  [ 📑 TABS ]\n", "#FFFFDA6B");
                AppendToTab(_activeTab, "  !tab       Buka Tab Baru\n", "#FFFF9F43");
                AppendToTab(_activeTab, "  !close     Tutup Tab Aktif\n", "#FFFF9F43");
                AppendToTab(_activeTab, "  !exit      Keluar Terminal\n", "#FFFF6B6B");
                
                AppendToTab(_activeTab, "\n  💬 Tips: Gunakan Tanda Panah ↑ ↓ buat milih font/layout.\n\n", "#888888");
                return;
            }

            // TAB COMMANDS
            if (low == "!tab") { AddTab($"Session {_tabs.Count + 1}"); return; }
            if (low == "!close") { CloseActiveTab(); return; }

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

            // OPTIMIZED SYSTEM COMMANDS (Instant & Stealth)
            if (low == "!sys") {
                AppendToTab(_activeTab, "\n  📊 [ N E K O  S Y S T E M  I N F O ]\n", "#FFFFDA6B");
                Task.Run(() => {
                    try {
                        var os = ""; var build = "";
                        using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem"))
                        foreach (var obj in searcher.Get()) { os = obj["Caption"]?.ToString(); build = obj["Version"]?.ToString(); }
                        
                        string cpu = "";
                        using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
                        foreach (var obj in searcher.Get()) cpu = obj["Name"]?.ToString();

                        string gpu = "";
                        using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
                        foreach (var obj in searcher.Get()) gpu = obj["Caption"]?.ToString();

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
                                    string pw = line.Split(':')[1].Trim();
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

            if (low == "!exit") { this.Close(); return; }

            // DEFAULT: Command not found in Pure Neko Terminal
            // Since we are not using CMD/PS anymore, we show an exclusive error.
            AppendToTab(_activeTab, $"  ❯ {cmd}\n", Themes[_currentLayout].PromptColor);
            AppendToTab(_activeTab, $"  ❌ Perintah '{cmd}' tidak dikenali, Kak.\n", "#FFFF6B6B");
            AppendToTab(_activeTab, "  💡 Neko Terminal ini eksklusif. Ketik '!help' buat lihat fitur-fiturnya!\n\n", "#888888");
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

            switch (_currentLayout) {
                case 0: NeofetchArea.Visibility = Visibility.Visible; StatusBar.Visibility = Visibility.Visible; NeofetchRow.Height = GridLength.Auto; PromptText.Text = " ~/zero ❯ "; break;
                case 1: NeofetchArea.Visibility = Visibility.Collapsed; StatusBar.Visibility = Visibility.Visible; NeofetchRow.Height = new GridLength(0); PromptText.Text = " ~/zero ❯ "; break;
                case 2: NeofetchArea.Visibility = Visibility.Visible; StatusBar.Visibility = Visibility.Visible; NeofetchRow.Height = new GridLength(300); PromptText.Text = " ~/zero ❯ "; break;
                case 3: NeofetchArea.Visibility = Visibility.Collapsed; StatusBar.Visibility = Visibility.Visible; NeofetchRow.Height = new GridLength(0); PromptText.Text = " C:\\> "; break;
                case 4: NeofetchArea.Visibility = Visibility.Visible; StatusBar.Visibility = Visibility.Visible; NeofetchRow.Height = GridLength.Auto; PromptText.Text = " ⚡ ZERO ❯ "; break;
            }
        }
        #endregion

        #region Window
        private void Window_MouseDown(object sender, MouseButtonEventArgs e) { if (e.ChangedButton == MouseButton.Left) this.DragMove(); }
        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;
        private void CloseButton_Click(object sender, RoutedEventArgs e) => this.Close();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadNeofetchInfo();
            LoadAnimeCharacter();
            AddTab("Main"); // Initial Tab
            TerminalInput.Focus();

            _clockTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (s, ev) => DateTimeText.Text = DateTime.Now.ToString("MMM dd, hh:mm tt");
            _clockTimer.Start();
        }

        protected override void OnClosed(EventArgs e)
        {
            _clockTimer?.Stop();
            foreach (var t in _tabs) { try { if (t.Process != null && !t.Process.HasExited) t.Process.Kill(); } catch { } }
            base.OnClosed(e);
        }
        #endregion
    }
}
