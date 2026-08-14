using System;
using System.IO;
using System.Windows;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Threading;
using Microsoft.Win32;
using ZeroMix.Hotkeys;
using ZeroMix.Wallpapers;
using System.Threading;

namespace ZeroMix
{
    public partial class App : System.Windows.Application
    {
        public static HotkeyCore? HotkeyCoreInstance { get; private set; }
        private DispatcherTimer? _memoryTimer;
        private static Mutex? _mutex;
        private static bool _mutexOwned = false;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("kernel32.dll", EntryPoint = "SetProcessWorkingSetSize")]
        internal static extern int SetProcessWorkingSetSize(IntPtr process, int minimumWorkingSetSize, int maximumWorkingSetSize);

        // EnumWindows untuk cari window handle saat MainWindowHandle = zero
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        private static IntPtr FindMainWindowByPid(int pid)
        {
            IntPtr found = IntPtr.Zero;
            EnumWindows((hWnd, _) =>
            {
                GetWindowThreadProcessId(hWnd, out uint winPid);
                if (winPid == (uint)pid && IsWindowVisible(hWnd))
                {
                    found = hWnd;
                    return false; // stop enum
                }
                return true;
            }, IntPtr.Zero);
            return found;
        }

        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ZeroMix", "config.json");

        protected override void OnStartup(StartupEventArgs e)
        {
            // ── Crash Logger (diagnosa error VA / unhandled exception) ──
            // Semua unhandled exception dicatat ke crash.log di samping exe,
            // supaya error yang mematikan proses tidak lagi jadi misteri.
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
                LogCrash("AppDomain", args.ExceptionObject as Exception);
            DispatcherUnhandledException += (s, args) =>
            {
                LogCrash("Dispatcher", args.Exception);
                // Tidak di-handle — biarkan crash, tapi exception sudah tercatat.
            };
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, args) =>
            {
                LogCrash("Task", args.Exception);
                args.SetObserved();
            };

            // ── Single Instance Guard ─────────────────────────────────────
            _mutex = new Mutex(true, "ZeroMix_SingleInstance", out bool isNewInstance);
            _mutexOwned = isNewInstance;
            if (!isNewInstance)
            {
                // Sudah ada instance — cari via named pipe / window message
                // Cari semua proses ZeroMix selain diri sendiri
                string procName = Path.GetFileNameWithoutExtension(
                    Process.GetCurrentProcess().MainModule?.FileName ?? "ZeroMix");

                var existing = Process.GetProcessesByName(procName)
                    .Where(p => p.Id != Environment.ProcessId)
                    .OrderBy(p => p.StartTime)
                    .FirstOrDefault();

                if (existing != null)
                {
                    // Coba bring to front — handle bisa zero kalau window belum ready
                    IntPtr hwnd = existing.MainWindowHandle;

                    // Kalau handle zero, cari via EnumWindows
                    if (hwnd == IntPtr.Zero)
                    {
                        hwnd = FindMainWindowByPid(existing.Id);
                    }

                    if (hwnd != IntPtr.Zero)
                    {
                        ShowWindow(hwnd, 9); // SW_RESTORE
                        SetForegroundWindow(hwnd);
                    }
                }

                Shutdown();
                return;
            }

            base.OnStartup(e);

            // Required for WinForms NotifyIcon to work correctly
            System.Windows.Forms.Application.EnableVisualStyles();

            // Pastikan folder AppData/ZeroMix ada
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);

            // Load language resource dictionary
            LoadLanguageResources();
            
            // Start memory optimization timer (every 1 minute)
            _memoryTimer = new DispatcherTimer();
            _memoryTimer.Interval = TimeSpan.FromMinutes(1);
            _memoryTimer.Tick += (s, ev) => OptimizeMemory();
            _memoryTimer.Start();

            bool isFirstRun = true;
            bool startupPrompted = false;

            if (File.Exists(ConfigPath))
            {
                try
                {
                    string jsonString = File.ReadAllText(ConfigPath);
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

            // Skip onboarding — go straight to main app
            StartMainApp(e.Args);
        }

        /// <summary>Catat unhandled exception ke crash.log (di samping exe) untuk diagnosa.</summary>
        internal static void LogCrash(string source, Exception? ex)
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log");
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {source}");
                if (ex == null)
                {
                    sb.AppendLine("(exception null)");
                }
                else
                {
                    sb.AppendLine($"Type: {ex.GetType().FullName}");
                    sb.AppendLine($"Message: {ex.Message}");
                    sb.AppendLine($"Stack: {ex.StackTrace}");
                    for (Exception? inner = ex.InnerException; inner != null; inner = inner.InnerException)
                    {
                        sb.AppendLine($"Inner: {inner.GetType().FullName}: {inner.Message}");
                        sb.AppendLine(inner.StackTrace);
                    }
                }
                sb.AppendLine(new string('-', 70));
                File.AppendAllText(path, sb.ToString());
            }
            catch { }
        }

        private void LoadLanguageResources()
        {
            string languageCode = "en-US";

            // Cek di AppData dulu (user preference), fallback ke BaseDirectory (installer default)
            string appDataFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ZeroMix", "language.ini");
            string baseFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "language.ini");
            string languageFile = File.Exists(appDataFile) ? appDataFile : baseFile;

            if (File.Exists(languageFile))
            {
                try
                {
                    string code = File.ReadAllText(languageFile).Trim();
                    var valid = new[] { "en-US", "id-ID", "ja-JP", "zh-CN" };
                    if (valid.Contains(code))
                        languageCode = code;
                }
                catch { }
            }

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string localeFile = Path.Combine(baseDir, "Assets", "Resources", "Locales", $"{languageCode}.xaml");

            if (!File.Exists(localeFile))
                localeFile = Path.Combine(baseDir, "Assets", "Resources", "Locales", "en-US.xaml");

            try
            {
                var langDictionary = new ResourceDictionary
                {
                    Source = new Uri(localeFile, UriKind.Absolute)
                };

                bool replaced = false;
                for (int i = 0; i < this.Resources.MergedDictionaries.Count; i++)
                {
                    if (this.Resources.MergedDictionaries[i].Source?.OriginalString.Contains("Locales") == true)
                    {
                        this.Resources.MergedDictionaries[i] = langDictionary;
                        replaced = true;
                        break;
                    }
                }

                if (!replaced)
                    this.Resources.MergedDictionaries.Add(langDictionary);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Language] Failed to load {languageCode}: {ex.Message}");
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
            File.WriteAllText(ConfigPath, json);
        }

        private void StartMainApp(string[]? args = null)
        {
            var mainWindow = new MainWindow(args);
            mainWindow.Show();

            HotkeyCoreInstance = new HotkeyCore();
            // Keep HotkeyCore window hidden (runs in background for hotkey handling)
            // WAJIB di-Show() agar message pump window aktif untuk menerima WM_HOTKEY
            // Window sudah di-set Visibility=Hidden & ShowInTaskbar=false di constructor,
            // jadi tidak akan muncul di layar.
            HotkeyCoreInstance.Show();

            

            // Restore video wallpaper dari session terakhir
            WallpaperManager.RestoreSession();

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

        protected override void OnExit(ExitEventArgs e)
        {
            if (_mutexOwned)
            {
                try { _mutex?.ReleaseMutex(); } catch (ApplicationException) { }
            }
            _mutex?.Dispose();
            base.OnExit(e);
        }
        
    }
}