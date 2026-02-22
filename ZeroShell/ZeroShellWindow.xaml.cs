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

namespace ZeroMix.ZeroShell
{
    public partial class ZeroShellWindow : Window
    {
        private Process? _shellProcess;
        private StreamWriter? _shellInput;
        private List<string> _commandHistory = new List<string>();
        private int _historyIndex = -1;
        private System.Windows.Threading.DispatcherTimer? _clockTimer;

        public ZeroShellWindow()
        {
            InitializeComponent();
        }

        #region System Info (Neofetch)
        private void LoadNeofetchInfo()
        {
            try {
                // OS
                OsInfoText.Text = $"{Environment.OSVersion.Platform} {Environment.OSVersion.Version}";
                KernelText.Text = $"NT {Environment.OSVersion.Version.Major}.{Environment.OSVersion.Version.Minor}.{Environment.OSVersion.Version.Build}";
                
                // Uptime
                var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
                UptimeText.Text = uptime.Hours > 0 
                    ? $"{uptime.Hours}h {uptime.Minutes}m" 
                    : $"{uptime.Minutes} mins";

                // Shell
                ShellText.Text = "PowerShell 7 / ZeroMix Engine";

                // User
                UserNameText.Text = $" {Environment.UserName} ";

                // Background: CPU, GPU, RAM via WMI
                Task.Run(() => {
                    try {
                        string cpu = "Unknown CPU";
                        string gpu = "Unknown GPU";
                        long totalRam = 0;
                        long freeRam = 0;

                        using (var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor"))
                            foreach (var obj in searcher.Get()) { cpu = obj["Name"]?.ToString() ?? cpu; break; }

                        using (var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController"))
                            foreach (var obj in searcher.Get()) { gpu = obj["Name"]?.ToString() ?? gpu; break; }

                        using (var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem"))
                            foreach (var obj in searcher.Get()) {
                                totalRam = Convert.ToInt64(obj["TotalVisibleMemorySize"]) / 1024;
                                freeRam = Convert.ToInt64(obj["FreePhysicalMemory"]) / 1024;
                                break;
                            }

                        long usedRam = totalRam - freeRam;
                        double ramPercent = totalRam > 0 ? (usedRam * 100.0 / totalRam) : 0;

                        Dispatcher.Invoke(() => {
                            CpuInfoText.Text = cpu;
                            GpuInfoText.Text = gpu;
                            RamInfoText.Text = $"{usedRam / 1024.0:F2} GiB / {totalRam / 1024.0:F2} GiB ({ramPercent:F0}%)";

                            // Also update OS properly
                            using (var s = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem"))
                                foreach (var o in s.Get()) { OsInfoText.Text = o["Caption"]?.ToString() ?? OsInfoText.Text; break; }
                        });
                    } catch { }
                });
            } catch { }
        }

        private void LoadAnimeCharacter()
        {
            // Try to find a character image in ZeroShell folder
            string[] possiblePaths = new[] {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ZeroShell", "character.png"),
                Path.Combine(Directory.GetCurrentDirectory(), "ZeroShell", "character.png"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "ZeroShell", "character.png")
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    try {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(path, UriKind.Absolute);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        AnimeCharImage.Source = bitmap;
                        CharPlaceholder.Visibility = Visibility.Collapsed;
                    } catch { }
                    break;
                }
            }
        }
        #endregion

        #region PowerShell Terminal
        private void StartTerminal()
        {
            try
            {
                _shellProcess = new Process();
                _shellProcess.StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoLogo -NoProfile -ExecutionPolicy Bypass",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8
                };
                
                _shellProcess.Start();
                _shellInput = _shellProcess.StandardInput;
                _shellInput.AutoFlush = true;

                Task.Run(() => ReadOutputAsync(_shellProcess.StandardOutput));
                Task.Run(() => ReadOutputAsync(_shellProcess.StandardError));
            }
            catch (Exception ex) { AppendTerminalText($"[ERROR]: {ex.Message}\n", "#FFFF6B6B"); }
        }

        private async Task ReadOutputAsync(StreamReader reader)
        {
            char[] buffer = new char[512];
            while (!reader.EndOfStream)
            {
                int count = await reader.ReadAsync(buffer, 0, buffer.Length);
                if (count > 0)
                {
                    string text = new string(buffer, 0, count);
                    Dispatcher.Invoke(() => {
                        AppendTerminalText(text, "#CCCCCC");
                        TerminalScroll.ScrollToEnd();
                    });
                }
            }
        }

        private void AppendTerminalText(string text, string colorHex)
        {
            var run = new Run(text) { 
                Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorHex)) 
            };
            TerminalOutput.Inlines.Add(run);
            if (TerminalOutput.Inlines.Count > 1000) TerminalOutput.Inlines.Remove(TerminalOutput.Inlines.FirstInline);
            TerminalScroll.ScrollToEnd();
        }

        private void TerminalInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string cmd = TerminalInput.Text.Trim();
                if (!string.IsNullOrWhiteSpace(cmd)) {
                    _commandHistory.Add(cmd);
                    _historyIndex = _commandHistory.Count;
                    ProcessCommand(cmd);
                }
                TerminalInput.Text = "";
                e.Handled = true;
            }
            else if (e.Key == Key.Up && _commandHistory.Count > 0) {
                if (_historyIndex > 0) _historyIndex--;
                TerminalInput.Text = _commandHistory[_historyIndex];
                TerminalInput.CaretIndex = TerminalInput.Text.Length;
                e.Handled = true;
            }
            else if (e.Key == Key.Down && _commandHistory.Count > 0) {
                if (_historyIndex < _commandHistory.Count - 1) _historyIndex++;
                TerminalInput.Text = _commandHistory[_historyIndex];
                TerminalInput.CaretIndex = TerminalInput.Text.Length;
                e.Handled = true;
            }
        }

        private void ProcessCommand(string cmd)
        {
            string low = cmd.ToLower();

            if (low == "cls" || low == "clear") {
                TerminalOutput.Inlines.Clear();
                return;
            }
            if (low == "!help") {
                AppendTerminalText("\n", "#CCCCCC");
                AppendTerminalText("  ╔═══════════════════════════════════════════╗\n", "#FF6BDDFF");
                AppendTerminalText("  ║     Z E R O   T E R M I N A L   H E L P  ║\n", "#FF6BDDFF");
                AppendTerminalText("  ╚═══════════════════════════════════════════╝\n\n", "#FF6BDDFF");
                AppendTerminalText("  !help          Tampilkan bantuan ini\n", "#FF27C93F");
                AppendTerminalText("  !wifi          Lihat semua password WiFi tersimpan\n", "#FF27C93F");
                AppendTerminalText("  !sys           Info detail sistem (CPU/RAM/OS)\n", "#FF27C93F");
                AppendTerminalText("  !ip            Tampilkan IP Address\n", "#FF27C93F");
                AppendTerminalText("  !battery       Cek status baterai\n", "#FF27C93F");
                AppendTerminalText("  !disk          Info disk/storage\n", "#FF27C93F");
                AppendTerminalText("  !apps          List aplikasi terinstal\n", "#FF27C93F");
                AppendTerminalText("  !startup       List program startup\n", "#FF27C93F");
                AppendTerminalText("  cls / clear    Bersihkan layar terminal\n", "#FFFFDA6B");
                AppendTerminalText("  !exit          Tutup terminal\n", "#FFFF6B6B");
                AppendTerminalText("  [command]      Jalankan perintah PowerShell\n\n", "#88FFFFFF");
                return;
            }
            if (low == "!wifi") {
                AppendTerminalText("\n  Scanning WiFi passwords...\n\n", "#FFCC6BFF");
                string wifiCmd = "(netsh wlan show profiles) | Select-String '\\:(.+)$' | %{$name=$_.Matches.Groups[1].Value.Trim(); netsh wlan show profile name=\"$name\" key=clear} | Select-String 'Key Content\\W+\\:(.+)$' | %{$pass=$_.Matches.Groups[1].Value.Trim(); Write-Host \"  WIFI: $name | PASS: $pass\"}";
                _shellInput?.WriteLine(wifiCmd);
                return;
            }
            if (low == "!sys") {
                AppendTerminalText("\n  Loading system info...\n", "#FFFFDA6B");
                _shellInput?.WriteLine("Get-CimInstance Win32_OperatingSystem | Select-Object Caption, Version, FreePhysicalMemory, TotalVisibleMemorySize | Format-List");
                return;
            }
            if (low == "!ip") {
                _shellInput?.WriteLine("(Get-NetIPAddress -AddressFamily IPv4 | Where-Object {$_.InterfaceAlias -notmatch 'Loopback'}) | Select-Object InterfaceAlias, IPAddress | Format-Table -AutoSize");
                return;
            }
            if (low == "!battery") {
                _shellInput?.WriteLine("Get-CimInstance Win32_Battery | Select-Object Name, EstimatedChargeRemaining, BatteryStatus | Format-List");
                return;
            }
            if (low == "!disk") {
                _shellInput?.WriteLine("Get-PSDrive -PSProvider FileSystem | Select-Object Name, @{N='Used(GB)';E={[math]::Round($_.Used/1GB,2)}}, @{N='Free(GB)';E={[math]::Round($_.Free/1GB,2)}} | Format-Table -AutoSize");
                return;
            }
            if (low == "!apps") {
                _shellInput?.WriteLine("Get-ItemProperty HKLM:\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\* | Select-Object DisplayName, DisplayVersion | Where-Object {$_.DisplayName} | Sort-Object DisplayName | Select-Object -First 30 | Format-Table -AutoSize");
                return;
            }
            if (low == "!startup") {
                _shellInput?.WriteLine("Get-CimInstance Win32_StartupCommand | Select-Object Name, Command, Location | Format-Table -AutoSize");
                return;
            }
            if (low == "!exit") { this.Close(); return; }

            // Standard PowerShell
            _shellInput?.WriteLine(cmd);
            AppendTerminalText($"  > {cmd}\n", "#FF27C93F");
        }
        #endregion

        #region Window Controls
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }
        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;
        private void CloseButton_Click(object sender, RoutedEventArgs e) => this.Close();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadNeofetchInfo();
            LoadAnimeCharacter();
            StartTerminal();
            TerminalInput.Focus();

            // Clock Timer
            _clockTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (s, ev) => DateTimeText.Text = DateTime.Now.ToString("MMM dd, hh:mm tt");
            _clockTimer.Start();
            DateTimeText.Text = DateTime.Now.ToString("MMM dd, hh:mm tt");
        }

        protected override void OnClosed(EventArgs e)
        {
            _clockTimer?.Stop();
            try {
                if (_shellProcess != null && !_shellProcess.HasExited) _shellProcess.Kill();
            } catch { }
            base.OnClosed(e);
        }
        #endregion
    }
}
