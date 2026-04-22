using System;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace ZeroMix.Widgets
{
    public partial class DesktopWidget : Window
    {
        private DispatcherTimer? _timer;
        private DispatcherTimer? _sysTimer;
        private readonly DateTime _startTime = DateTime.Now;

        #region P/Invoke
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        [DllImport("user32.dll")] static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
        [DllImport("user32.dll", SetLastError = true)] static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        const int GWL_EXSTYLE    = -20;
        const int WS_EX_TOOLWINDOW = 0x00000080;
        const int WS_EX_NOACTIVATE = 0x08000000;
        static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        const uint SWP_NOSIZE    = 0x0001;
        const uint SWP_NOMOVE    = 0x0002;
        const uint SWP_NOACTIVATE = 0x0010;
        #endregion

        public DesktopWidget()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Cover full primary screen
            this.Left   = 0;
            this.Top    = 0;
            this.Width  = SystemParameters.PrimaryScreenWidth;
            this.Height = SystemParameters.PrimaryScreenHeight;

            var helper = new WindowInteropHelper(this);
            IntPtr hwnd = helper.Handle;

            // WS_EX_TOOLWINDOW = hide from Alt+Tab
            // WS_EX_NOACTIVATE = never steal focus
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);

            // Embed into WorkerW (desktop layer — behind all windows, above wallpaper)
            EmbedIntoDesktop(hwnd);

            // Start timers
            StartClockTimer();
            StartSysTimer();

            // Initial update
            UpdateClock();
            UpdateGreeting();
        }

        /// <summary>
        /// Embed window into WorkerW so it sits on the desktop layer.
        /// Same technique used by live wallpaper apps.
        /// </summary>
        private void EmbedIntoDesktop(IntPtr hwnd)
        {
            try
            {
                IntPtr progman = FindWindow("Progman", null);
                // Spawn WorkerW
                SendMessage(progman, 0x052C, new IntPtr(0x0000000D), new IntPtr(0));
                SendMessage(progman, 0x052C, new IntPtr(0x0000000D), new IntPtr(1));

                IntPtr workerw = IntPtr.Zero;
                EnumWindows((topHandle, _) =>
                {
                    IntPtr p = FindWindowEx(topHandle, IntPtr.Zero, "SHELLDLL_DefView", null);
                    if (p != IntPtr.Zero)
                        workerw = FindWindowEx(IntPtr.Zero, topHandle, "WorkerW", null);
                    return true;
                }, IntPtr.Zero);

                if (workerw != IntPtr.Zero)
                    SetParent(hwnd, workerw);
                else
                    // Fallback: just push to bottom of Z-order
                    SetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
            }
            catch
            {
                SetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
            }
        }

        #region Clock
        private void StartClockTimer()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) => { UpdateClock(); UpdateGreeting(); };
            _timer.Start();
        }

        private void UpdateClock()
        {
            var now = DateTime.Now;
            ClockText.Text = now.ToString("HH:mm");
            DateText.Text  = now.ToString("dddd, MMMM d");

            // Uptime
            var up = DateTime.Now - _startTime;
            UptimeText.Text = $"Session: {(int)up.TotalHours:D2}h {up.Minutes:D2}m";
        }

        private void UpdateGreeting()
        {
            int h = DateTime.Now.Hour;
            GreetText.Text = h < 5  ? "Good night" :
                             h < 12 ? "Good morning" :
                             h < 17 ? "Good afternoon" :
                             h < 21 ? "Good evening" : "Good night";
        }
        #endregion

        #region System Info
        private void StartSysTimer()
        {
            _sysTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
            _sysTimer.Tick += (s, e) => UpdateSysInfo();
            _sysTimer.Start();
            UpdateSysInfo(); // immediate first read
        }

        private void UpdateSysInfo()
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    // CPU
                    double cpu = 0;
                    using (var s = new ManagementObjectSearcher("SELECT LoadPercentage FROM Win32_Processor"))
                        foreach (ManagementObject o in s.Get())
                            cpu = Convert.ToDouble(o["LoadPercentage"]);

                    // RAM
                    double total = 0, free = 0;
                    using (var s = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize,FreePhysicalMemory FROM Win32_OperatingSystem"))
                        foreach (ManagementObject o in s.Get())
                        {
                            total = Convert.ToDouble(o["TotalVisibleMemorySize"]);
                            free  = Convert.ToDouble(o["FreePhysicalMemory"]);
                        }
                    double ram = total > 0 ? (total - free) / total * 100 : 0;

                    // Battery
                    string bat = "—";
                    using (var s = new ManagementObjectSearcher("SELECT EstimatedChargeRemaining FROM Win32_Battery"))
                        foreach (ManagementObject o in s.Get())
                            bat = $"{o["EstimatedChargeRemaining"]}%";

                    Dispatcher.Invoke(() =>
                    {
                        CpuText.Text = $"{cpu:F0}%";
                        RamText.Text = $"{ram:F0}%";
                        BatText.Text = bat;
                    });
                }
                catch { }
            });
        }
        #endregion

        public void Shutdown()
        {
            _timer?.Stop();
            _sysTimer?.Stop();
            Close();
        }
    }
}
