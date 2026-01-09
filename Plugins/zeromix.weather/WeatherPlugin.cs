using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Diagnostics;

namespace ZeroMix.Plugins.Weather
{
    public class WeatherPlugin
    {
        public string City { get; set; } = "Jambewangi";
        public bool IsActive { get; private set; }
        
        private DispatcherTimer? _timer;
        private readonly HttpClient _httpClient = new HttpClient();

        public WeatherPlugin()
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "ZeroMix/2.4");
        }

        public void Start()
        {
            IsActive = true;
            if (_timer == null)
            {
                _timer = new DispatcherTimer();
                _timer.Interval = TimeSpan.FromHours(1);
                _timer.Tick += (s, e) => _ = CheckWeatherAsync();
            }
            _timer.Start();
            _ = CheckWeatherAsync();
        }

        public void Stop()
        {
            IsActive = false;
            _timer?.Stop();
        }

        public async Task CheckWeatherAsync()
        {
            if (!IsActive) return;

            try
            {
                string url = $"https://wttr.in/{Uri.EscapeDataString(City)}?format=%C";
                var response = await _httpClient.GetStringAsync(url);
                string condition = response.Trim().ToLower();
                
                Debug.WriteLine($"[Weather] Current condition in {City}: {condition}");
                await ApplyWeatherWallpaperAsync(condition);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Weather] API Error: {ex.Message}");
                await ApplyWeatherWallpaperAsync("clear"); 
            }
        }

        private Task ApplyWeatherWallpaperAsync(string condition)
        {
            string fileName = "Sun.mp4";
            double volume = 0;
            
            DateTime now = DateTime.Now;
            int hour = now.Hour;

            // 1. Time-based logic: Sunset.mp4 (Spesial untuk Subuh & Maghrib)
            if ((hour == 5) || (hour == 17) || (hour == 18 && now.Minute <= 30))
            {
                fileName = "Sunset.mp4";
            }
            // 2. Weather condition triggers
            else if (condition.Contains("rain") || condition.Contains("drizzle") || condition.Contains("storm") || condition.Contains("thunder"))
            {
                fileName = "Rain.mp4";
                volume = 0.45; // 45% volume (Syahdu)
            }
            else if (condition.Contains("cloud") || condition.Contains("overcast") || condition.Contains("mist") || condition.Contains("fog") || condition.Contains("haze"))
            {
                fileName = "Cloud.mp4";
            }

            // --- SMART PATH DETECTION ---
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string assetPath = Path.Combine(baseDir, "Plugins", "zeromix.weather", "assets", fileName);

            if (!File.Exists(assetPath))
            {
                string devPath = Path.Combine(baseDir, "..", "..", "..", "Plugins", "zeromix.weather", "assets", fileName);
                if (File.Exists(devPath)) assetPath = devPath;
            }

            if (!File.Exists(assetPath))
            {
                string absolutePath = $@"C:\ZeroMix\ZeroMix\Plugins\zeromix.weather\assets\{fileName}";
                if (File.Exists(absolutePath)) assetPath = absolutePath;
            }

            if (File.Exists(assetPath))
            {
                Debug.WriteLine($"[Weather] SUCCESS: Found {fileName} at {assetPath}");
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    Wallpapers.LaunchVideoWallpaperStatic(assetPath, volume);
                });
            }
            else
            {
                Debug.WriteLine($"[Weather] ERROR: Could not find ANY video assets named {fileName}");
                string fallback = Path.Combine(baseDir, "Resource", "Video", "Vs (1).mp4");
                if (File.Exists(fallback))
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                        Wallpapers.LaunchVideoWallpaperStatic(fallback, 0);
                    });
                }
            }
            return Task.CompletedTask;
        }
    }
}
