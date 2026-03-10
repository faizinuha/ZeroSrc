using System;
using System.IO;
using System.Windows;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Threading;
using Microsoft.Win32;
using ZeroMix.Hotkeys;

namespace ZeroMix
{
    public partial class App : System.Windows.Application
    {
        public static HotkeyCore? HotkeyCoreInstance { get; private set; }
        private DispatcherTimer? _memoryTimer;


        [DllImport("kernel32.dll", EntryPoint = "SetProcessWorkingSetSize")]
        internal static extern int SetProcessWorkingSetSize(IntPtr process, int minimumWorkingSetSize, int maximumWorkingSetSize);

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Load language resource dictionary
            LoadLanguageResources();
            
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
                SaveConfig(false, true); // Mark as prompted
            }

            if (isFirstRun)
            {
                var onboarding = new ZeroMix.Onboarding.OnboardingWindow();
                onboarding.OnOnboardingFinished += () => StartMainApp(e.Args);
                onboarding.Show();
            }
            else
            {
                StartMainApp(e.Args);
            }
        }

        private void LoadLanguageResources()
        {
            string languageCode = "en-US"; // Default

            // Baca language.ini jika ada
            string languageFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "language.ini");
            if (File.Exists(languageFile))
            {
                try
                {
                    languageCode = File.ReadAllText(languageFile).Trim();
                    
                    // Validasi language code
                    if (!new[] { "en-US", "id-ID", "ja-JP" }.Contains(languageCode))
                    {
                        languageCode = "en-US";
                    }
                }
                catch
                {
                    languageCode = "en-US";
                }
            }

            // Load resource dictionary berdasarkan language code
            string resourcePath = $"Resources/Locales/{languageCode}.xaml";
            try
            {
                var langDictionary = new ResourceDictionary 
                { 
                    Source = new Uri(resourcePath, UriKind.Relative) 
                };
                
                // PENTING: Jangan gunakan Clear() karena akan menghapus Styles.xaml
                // Cari dictionary lama yang merupakan locale (biasanya di index 0 atau check source)
                bool replaced = false;
                for (int i = 0; i < this.Resources.MergedDictionaries.Count; i++)
                {
                    if (this.Resources.MergedDictionaries[i].Source.OriginalString.Contains("Locales/"))
                    {
                        this.Resources.MergedDictionaries[i] = langDictionary;
                        replaced = true;
                        break;
                    }
                }

                if (!replaced)
                {
                    this.Resources.MergedDictionaries.Add(langDictionary);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load language {languageCode}: {ex.Message}");
                // Fallback ke en-US jika gagal
                var defaultDictionary = new ResourceDictionary 
                { 
                    Source = new Uri("Resources/Locales/en-US.xaml", UriKind.Relative) 
                };
                this.Resources.MergedDictionaries.Add(defaultDictionary);
            }
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

        private void StartMainApp(string[]? args = null)
        {
            var mainWindow = new MainWindow(args);
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
