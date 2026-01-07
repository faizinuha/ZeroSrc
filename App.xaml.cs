using System;
using System.IO;
using System.Windows;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Threading;
using Microsoft.Win32;

namespace ZeroMix
{
    public partial class App : System.Windows.Application
    {
        public static HotkeyCore? HotkeyCoreInstance { get; private set; }
        private DispatcherTimer? _memoryTimer;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        static extern bool FreeConsole();

        [DllImport("kernel32.dll", EntryPoint = "SetProcessWorkingSetSize")]
        internal static extern int SetProcessWorkingSetSize(IntPtr process, int minimumWorkingSetSize, int maximumWorkingSetSize);

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Start memory optimization timer (every 1 minute)
            _memoryTimer = new DispatcherTimer();
            _memoryTimer.Interval = TimeSpan.FromMinutes(1);
            _memoryTimer.Tick += (s, ev) => OptimizeMemory();
            _memoryTimer.Start();

            bool isFirstRun = true;
            bool startupPrompted = false;

            if (File.Exists("config.json"))
            {
                try
                {
                    string jsonString = File.ReadAllText("config.json");
                    using (JsonDocument doc = JsonDocument.Parse(jsonString))
                    {
                        if (doc.RootElement.TryGetProperty("IsFirstRun", out JsonElement element))
                            isFirstRun = element.GetBoolean();
                        
                        if (doc.RootElement.TryGetProperty("StartupPrompted", out JsonElement promptedElement))
                            startupPrompted = promptedElement.GetBoolean();
                    }
                }
                catch { isFirstRun = true; }
            }

            if (!startupPrompted)
            {
                ShowStartupTerminal();
                SaveConfig(false, true); // Mark as prompted
            }

            if (isFirstRun)
            {
                var onboarding = new ZeroMix.Onboarding.OnboardingWindow();
                onboarding.OnOnboardingFinished += StartMainApp;
                onboarding.Show();
            }
            else
            {
                StartMainApp();
            }
        }

        private void ShowStartupTerminal()
        {
            AllocConsole();
            
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("========================================");
            Console.WriteLine("        ZeroMix Startup Manager         ");
            Console.WriteLine("========================================");
            Console.ResetColor();
            Console.WriteLine("\nSelamat! ZeroMix telah berhasil terpasang.");
            Console.WriteLine("Apakah Anda ingin ZeroMix otomatis berjalan saat Windows dimulai?");
            Console.Write("\nKetik 'Y' untuk Aktifkan atau 'N' untuk Lewati: ");
            
            string? input = Console.ReadLine()?.Trim().ToUpper();
            
            if (input == "Y")
            {
                EnableStartup(true);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n[SUKSES] Startup telah diaktifkan!");
            }
            else
            {
                EnableStartup(false);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n[INFO] Startup dilewati.");
            }
            
            Console.ResetColor();
            Console.WriteLine("Terminal akan menutup dalam 2 detik...");
            System.Threading.Thread.Sleep(2000);
            
            FreeConsole();
        }

        private void EnableStartup(bool enable)
        {
            const string runKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
            const string appName = "ZeroMix";
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(runKey, true)!)
                {
                    if (enable)
                    {
                        string path = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                        if (!string.IsNullOrEmpty(path))
                            key.SetValue(appName, $"\"{path}\"");
                    }
                    else
                    {
                        key.DeleteValue(appName, false);
                    }
                }
            }
            catch (Exception ex) { Debug.WriteLine("Startup Registry Error: " + ex.Message); }
        }

        private void SaveConfig(bool isFirstRun, bool startupPrompted)
        {
            var config = new { IsFirstRun = isFirstRun, StartupPrompted = startupPrompted };
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText("config.json", json);
        }

        private void StartMainApp()
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();

            HotkeyCoreInstance = new HotkeyCore();
            HotkeyCoreInstance.Show();
            
            // Initial optimization
            OptimizeMemory();
        }

        public static void OptimizeMemory()
        {
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1);
                }
            }
            catch { }
        }
    }
}
