using System;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace ZeroMix.Widgets
{
    public partial class ClockWidget : Window
    {
        private DispatcherTimer? _clockTimer;
        private DispatcherTimer? _weatherTimer;
        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(8) };

        // Open-Meteo — free, no API key required
        // https://open-meteo.com/en/docs
        private const string WEATHER_URL =
            "https://api.open-meteo.com/v1/forecast" +
            "?latitude={0}&longitude={1}" +
            "&current=temperature_2m,weathercode,windspeed_10m" +
            "&timezone=auto";

        // Default coords — Jakarta. Will be overridden by IP geolocation.
        private double _lat = -6.2088;
        private double _lon = 106.8456;

        #region P/Invoke
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        static readonly IntPtr HWND_BOTTOM  = new(1);
        const uint SWP_NOSIZE    = 0x0001;
        const uint SWP_NOMOVE    = 0x0002;
        const uint SWP_NOACTIVATE = 0x0010;
        const int  GWL_EXSTYLE   = -20;
        const int  WS_EX_TOOLWINDOW = 0x00000080;
        #endregion

        public ClockWidget()
        {
            InitializeComponent();
            SetupContextMenu();
        }

        private void SetupContextMenu()
        {
            var cm = new System.Windows.Controls.ContextMenu();
            var close = new System.Windows.Controls.MenuItem { Header = "Close Clock" };
            close.Click += (s, e) => Close();
            var refresh = new System.Windows.Controls.MenuItem { Header = "Refresh Weather" };
            refresh.Click += async (s, e) => await FetchWeatherAsync();
            cm.Items.Add(refresh);
            cm.Items.Add(close);
            this.ContextMenu = cm;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Position: top-right corner
            this.Left = SystemParameters.PrimaryScreenWidth - this.Width - 20;
            this.Top  = 20;

            var helper = new WindowInteropHelper(this);
            int exStyle = GetWindowLong(helper.Handle, GWL_EXSTYLE);
            SetWindowLong(helper.Handle, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW);
            SetWindowPos(helper.Handle, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);

            // Clock — every second
            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (s, ev) => UpdateClock();
            _clockTimer.Start();
            UpdateClock();

            // Weather — fetch now, then every 15 min
            _weatherTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(15) };
            _weatherTimer.Tick += async (s, ev) => await FetchWeatherAsync();
            _weatherTimer.Start();

            // Geo + weather on startup (fire and forget)
            _ = InitWeatherAsync();
        }

        private void UpdateClock()
        {
            var now = DateTime.Now;
            TimeDisplay.Text = now.ToString("HH:mm");
            DateDisplay.Text = now.ToString("dddd, MMMM d").ToUpper();

            var offset = TimeZoneInfo.Local.GetUtcOffset(now);
            string sign = offset < TimeSpan.Zero ? "-" : "+";
            UtcDisplay.Text = $"UTC {sign}{Math.Abs(offset.Hours):D2}:{Math.Abs(offset.Minutes):D2}";
        }

        #region Weather
        private async System.Threading.Tasks.Task InitWeatherAsync()
        {
            try
            {
                // IP geolocation via ip-api.com (free, no key)
                string geoJson = await _http.GetStringAsync("http://ip-api.com/json/?fields=lat,lon,city");
                using var doc = JsonDocument.Parse(geoJson);
                var root = doc.RootElement;
                if (root.TryGetProperty("lat", out var latEl)) _lat = latEl.GetDouble();
                if (root.TryGetProperty("lon", out var lonEl)) _lon = lonEl.GetDouble();
            }
            catch { /* use default coords */ }

            await FetchWeatherAsync();
        }

        private async System.Threading.Tasks.Task FetchWeatherAsync()
        {
            try
            {
                Dispatcher.Invoke(() => WeatherDisplay.Text = "⟳ Updating...");

                string url = string.Format(WEATHER_URL, _lat.ToString("F4", System.Globalization.CultureInfo.InvariantCulture),
                                                         _lon.ToString("F4", System.Globalization.CultureInfo.InvariantCulture));
                string json = await _http.GetStringAsync(url);

                using var doc  = JsonDocument.Parse(json);
                var current    = doc.RootElement.GetProperty("current");
                double temp    = current.GetProperty("temperature_2m").GetDouble();
                int    wcode   = current.GetProperty("weathercode").GetInt32();
                double wind    = current.GetProperty("windspeed_10m").GetDouble();

                string icon    = WeatherCodeToIcon(wcode);
                string desc    = WeatherCodeToDesc(wcode);

                Dispatcher.Invoke(() =>
                    WeatherDisplay.Text = $"{icon} {temp:F0}°C  {desc}  💨 {wind:F0} km/h");
            }
            catch
            {
                Dispatcher.Invoke(() => WeatherDisplay.Text = "— weather unavailable");
            }
        }

        // WMO Weather interpretation codes
        // https://open-meteo.com/en/docs#weathervariables
        private static string WeatherCodeToIcon(int code) => code switch
        {
            0            => "☀️",
            1            => "🌤",
            2            => "⛅",
            3            => "☁️",
            45 or 48     => "🌫",
            51 or 53 or 55 => "🌦",
            61 or 63 or 65 => "🌧",
            71 or 73 or 75 => "❄️",
            80 or 81 or 82 => "🌦",
            95           => "⛈",
            96 or 99     => "⛈",
            _            => "🌡"
        };

        private static string WeatherCodeToDesc(int code) => code switch
        {
            0            => "Clear",
            1            => "Mostly Clear",
            2            => "Partly Cloudy",
            3            => "Overcast",
            45 or 48     => "Foggy",
            51 or 53 or 55 => "Drizzle",
            61 or 63 or 65 => "Rain",
            71 or 73 or 75 => "Snow",
            80 or 81 or 82 => "Showers",
            95           => "Thunderstorm",
            96 or 99     => "Hail Storm",
            _            => "—"
        };
        #endregion

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) { Close(); return; }
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _clockTimer?.Stop();
            _weatherTimer?.Stop();
        }
    }
}
