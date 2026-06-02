using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using System.Windows;

namespace ZeroMix.ZeroShell.Commands
{
    /// <summary>
    /// Handler untuk system info commands: !task, !sys, !wifi, !ip, !battery, !disk, !apps, !startup
    /// </summary>
    public class SystemInfoCommands : IShellCommand
    {
        public bool CanHandle(string command)
        {
            var low = command.ToLower().Trim();
            return low == "!task" || low == "!sys" || low == "!wifi" || low == "!ip" || 
                   low == "!battery" || low == "!disk" || low == "!apps" || low == "!startup";
        }

        public async Task Execute(string command, TerminalTab activeTab, Action<TerminalTab, string, string> appendToTab)
        {
            var low = command.ToLower().Trim();

            if (low == "!task")
                HandleSystemMonitor(activeTab, appendToTab);
            else if (low == "!sys")
                await HandleSystemInfo(activeTab, appendToTab);
            else if (low == "!wifi")
                await HandleWiFiScan(activeTab, appendToTab);
            else if (low == "!ip")
                HandleNetworkInfo(activeTab, appendToTab);
            else if (low == "!battery")
                HandleBatteryStatus(activeTab, appendToTab);
            else if (low == "!disk")
                HandleDiskUsage(activeTab, appendToTab);
            else if (low == "!apps")
                await HandleInstalledApps(activeTab, appendToTab);
            else if (low == "!startup")
                await HandleStartupItems(activeTab, appendToTab);
        }

        private void HandleSystemMonitor(TerminalTab activeTab, Action<TerminalTab, string, string> appendToTab)
        {
            appendToTab(activeTab, "\n  📊 [ S Y S T E M  M O N I T O R  -  T H R O T T L E D ]\n", "#FFFFDA6B");
            appendToTab(activeTab, "  (Press Ctrl+C to stop in some terminals, or just wait for 5 updates)\n\n", "#888888");
            
            Task.Run(async () => {
                for (int i = 0; i < 5; i++) {
                    try {
                        double cpuLoad = 0;
                        using (var searcher = new ManagementObjectSearcher("select LoadPercentage from Win32_Processor"))
                            foreach (var obj in searcher.Get()) cpuLoad = Convert.ToDouble(obj["LoadPercentage"]);

                        double totalRam = 0, freeRam = 0;
                        using (var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize,FreePhysicalMemory FROM Win32_OperatingSystem"))
                            foreach (var obj in searcher.Get()) {
                                totalRam = Convert.ToDouble(obj["TotalVisibleMemorySize"]);
                                freeRam = Convert.ToDouble(obj["FreePhysicalMemory"]);
                            }
                        double ramUsage = ((totalRam - freeRam) / totalRam) * 100;

                        string gpuName = "Generic GPU";
                        using (var gpuSearcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController")) {
                            foreach (ManagementObject obj in gpuSearcher.Get()) { gpuName = obj["Name"]?.ToString() ?? "N/A"; }
                        }

                        var drive = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady && d.Name.Contains("C:"));
                        double diskUsage = drive != null ? (double)(drive.TotalSize - drive.TotalFreeSpace) / drive.TotalSize * 100 : 0;

                        System.Windows.Application.Current?.Dispatcher.Invoke(() => {
                            appendToTab(activeTab, $"  [ UPDATE {i+1} ] ── {DateTime.Now:HH:mm:ss}\n", "#44FFFFFF");
                            appendToTab(activeTab, $"  💠 CPU : {cpuLoad:F2}% \n", "#FF6BDDFF");
                            appendToTab(activeTab, $"  🧠 RAM : {ramUsage:F2}% ({((totalRam - freeRam)/1024/1024):F1} GB / {(totalRam/1024/1024):F1} GB)\n", "#FFCC6BFF");
                            appendToTab(activeTab, $"  🎮 GPU : {gpuName} \n", "#FF27C93F");
                            appendToTab(activeTab, $"  💾 Disk: {diskUsage:F2}% (C:)\n", "#FFFF9F43");
                            appendToTab(activeTab, "  ──────────────────────────────\n", "#22FFFFFF");
                        });

                        await Task.Delay(2000);
                    } catch { break; }
                }
             System.Windows.Application.Current?.Dispatcher.Invoke(() => appendToTab(activeTab, "  ✅ Monitoring finished.\n\n", "#FF27C93F"));
            });
        }

        private async Task HandleSystemInfo(TerminalTab activeTab, Action<TerminalTab, string, string> appendToTab)
        {
            appendToTab(activeTab, "\n  📊 [ N E K O  S Y S T E M  I N F O ]\n", "#FFFFDA6B");
            await Task.Run(() => {
                try {
                    var os = ""; var build = "";
                    using (var osSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem")) {
                        foreach (ManagementObject obj in osSearcher.Get()) { 
                            os = obj["Caption"]?.ToString(); 
                            build = obj["Version"]?.ToString(); 
                        }
                    }
                    
                    string cpu = "";
                    using (var cpuSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor")) {
                        foreach (ManagementObject obj in cpuSearcher.Get()) { cpu = obj["Name"]?.ToString(); }
                    }

                    string gpu = "";
                    using (var gpuSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController")) {
                        foreach (ManagementObject obj in gpuSearcher.Get()) { gpu = obj["Caption"]?.ToString(); }
                    }

                    System.Windows.Application.Current?.Dispatcher.Invoke(() => {
                        appendToTab(activeTab, $"  ✨ OS    : {os}\n", "#FF6BDDFF");
                        appendToTab(activeTab, $"  ✨ BUILD : {build}\n", "#FF6BDDFF");
                        appendToTab(activeTab, $"  ✨ CPU   : {cpu?.Trim()}\n", "#FF6BDDFF");
                        appendToTab(activeTab, $"  ✨ GPU   : {gpu}\n\n", "#FF6BDDFF");
                    });
                } catch { 
                    System.Windows.Application.Current?.Dispatcher.Invoke(() => appendToTab(activeTab, "  ❌ Gagal ambil info sistem.\n\n", "#FFFF6B6B")); 
                }
            });
        }

        private async Task HandleWiFiScan(TerminalTab activeTab, Action<TerminalTab, string, string> appendToTab)
        {
            appendToTab(activeTab, "\n  🔐 [ S C A N N I N G  W I F I ]\n", "#FFCC6BFF");
            await Task.Run(() => {
                try {
                    var proc = new Process { StartInfo = new ProcessStartInfo("netsh", "wlan show profiles") 
                    { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true } };
                    proc.Start(); 
                    string output = proc.StandardOutput.ReadToEnd(); 
                    proc.WaitForExit();
                    
                    var profiles = new List<string>();
                    foreach (var line in output.Split('\n')) 
                        if (line.Contains(":")) 
                            profiles.Add(line.Split(':')[1].Trim());
                    
                    foreach (var p in profiles) {
                        if (string.IsNullOrEmpty(p)) continue;
                        var p2 = new Process { StartInfo = new ProcessStartInfo("netsh", $"wlan show profile name=\"{p}\" key=clear") 
                        { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true } };
                        p2.Start(); 
                        string output2 = p2.StandardOutput.ReadToEnd(); 
                        p2.WaitForExit();
                        
                        foreach (var line in output2.Split('\n')) {
                            if (line.Contains("Key Content")) {
                                string? rawPw = line.Split(':')[1];
                                string pw = rawPw?.Trim() ?? "Unknown";
                                System.Windows.Application.Current?.Dispatcher.Invoke(() => appendToTab(activeTab, $"  ⠿ {p,-20} → {pw}\n", "#FF27C93F"));
                            }
                        }
                    }
                    System.Windows.Application.Current?.Dispatcher.Invoke(() => appendToTab(activeTab, "\n", "#888888"));
                } catch { 
                    System.Windows.Application.Current?.Dispatcher.Invoke(() => appendToTab(activeTab, "  ❌ Gagal scan WiFi.\n\n", "#FFFF6B6B")); 
                }
            });
        }

        private void HandleNetworkInfo(TerminalTab activeTab, Action<TerminalTab, string, string> appendToTab)
        {
            appendToTab(activeTab, "\n  🌐 [ N E T W O R K  I N F O ]\n", "#FF6BDDFF");
            try {
                foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()) {
                    if (ni.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up) {
                        foreach (var ip in ni.GetIPProperties().UnicastAddresses) {
                            if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) {
                                appendToTab(activeTab, $"  🖧 {ni.Name,-15} : {ip.Address}\n", "#FF6BDDFF");
                            }
                        }
                    }
                }
                appendToTab(activeTab, "\n", "#888888");
            } catch { 
                appendToTab(activeTab, "  ❌ Gagal ambil info IP.\n\n", "#FFFF6B6B"); 
            }
        }

        private void HandleBatteryStatus(TerminalTab activeTab, Action<TerminalTab, string, string> appendToTab)
        {
            appendToTab(activeTab, "\n  🔋 [ B A T T E R Y  S T A T U S ]\n", "#FF27C93F");
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Battery"))
                foreach (var obj in searcher.Get()) {
                    appendToTab(activeTab, $"  ⚡ NAME   : {obj["Name"]}\n", "#FF27C93F");
                    appendToTab(activeTab, $"  ⚡ STATUS : {obj["BatteryStatus"]}\n", "#FF27C93F");
                    appendToTab(activeTab, $"  ⚡ CHARGE : {obj["EstimatedChargeRemaining"]}%\n\n", "#FF27C93F");
                }
        }

        private void HandleDiskUsage(TerminalTab activeTab, Action<TerminalTab, string, string> appendToTab)
        {
            appendToTab(activeTab, "\n  💾 [ D I S K  U S A G E ]\n", "#FFFF9F43");
            foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady)) {
                double total = drive.TotalSize / (1024.0 * 1024 * 1024);
                double free = drive.TotalFreeSpace / (1024.0 * 1024 * 1024);
                double used = total - free;
                appendToTab(activeTab, $"  📂 {drive.Name,-3} : {used:F1}GB / {total:F1}GB ({(used/total)*100:F1}%)\n", "#FFFF9F43");
            }
            appendToTab(activeTab, "\n", "#888888");
        }

        private async Task HandleInstalledApps(TerminalTab activeTab, Action<TerminalTab, string, string> appendToTab)
        {
            appendToTab(activeTab, "\n  📦 [ I N S T A L L E D  A P P S ]\n", "#FFCC6BFF");
            await Task.Run(() => {
                try {
                    var apps = new List<string>();
                    string[] roots = { "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall", "SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall" };
                    foreach (var root in roots) {
                        using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(root)) {
                            if (key != null) foreach (var sub in key.GetSubKeyNames()) {
                                using (var sk = key.OpenSubKey(sub)) {
                                    var name = sk?.GetValue("DisplayName")?.ToString();
                                    if (!string.IsNullOrEmpty(name)) apps.Add(name);
                                }
                            }
                        }
                    }
                    System.Windows.Application.Current?.Dispatcher.Invoke(() => {
                        foreach (var app in apps.OrderBy(a => a).Take(15)) 
                            appendToTab(activeTab, $"  📦 {app}\n", "#FFCC6BFF");
                        appendToTab(activeTab, "  ... (Showing top 15 apps)\n\n", "#888888");
                    });
                } catch { }
            });
        }

        private async Task HandleStartupItems(TerminalTab activeTab, Action<TerminalTab, string, string> appendToTab)
        {
            appendToTab(activeTab, "\n  🚀 [ S T A R T U P  I T E M S ]\n", "#FF6BDDFF");
            await Task.Run(() => {
                try {
                    var items = new List<string>();
                    using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run"))
                        if (key != null) foreach (var name in key.GetValueNames()) items.Add(name);
                    
                    System.Windows.Application.Current?.Dispatcher.Invoke(() => {
                        foreach (var it in items) appendToTab(activeTab, $"  🚀 {it}\n", "#FF6BDDFF");
                        appendToTab(activeTab, "\n", "#888888");
                    });
                } catch { }
            });
        }
    }
}
