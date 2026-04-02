using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Diagnostics;
using ZeroMix.Wallpapers;

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
                // Step 1: Geocode city name ke koordinat menggunakan Open-Meteo Geocoding API
                string geoUrl = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(City)}&count=1&language=en&format=json";
                var geoResponse = await _httpClient.GetStringAsync(geoUrl);
                
                // Parse JSON sederhana untuk mendapat lat/lon
                double lat = 0, lon = 0;
                if (geoResponse.Contains("latitude"))
                {
                    var latMatch = System.Text.RegularExpressions.Regex.Match(geoResponse, @"""latitude"":([\d.-]+)");
                    var lonMatch = System.Text.RegularExpressions.Regex.Match(geoResponse, @"""longitude"":([\d.-]+)");
                    if (latMatch.Success && lonMatch.Success)
                    {
                        lat = double.Parse(latMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                        lon = double.Parse(lonMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                    }
                }
                
                if (lat == 0 && lon == 0)
                {
                    Debug.WriteLine($"[Weather] Could not geocode city: {City}, using default");
                    await ApplyWeatherWallpaperAsync("clear");
                    return;
                }
                
                // Step 2: Get weather data dari Open-Meteo Weather API
                string weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current=weather_code";
                var weatherResponse = await _httpClient.GetStringAsync(weatherUrl);
                
                // Parse weather_code (WMO code)
                int weatherCode = 0;
                var codeMatch = System.Text.RegularExpressions.Regex.Match(weatherResponse, @"""weather_code"":([\d]+)");
                if (codeMatch.Success)
                {
                    weatherCode = int.Parse(codeMatch.Groups[1].Value);
                }
                
                // Convert WMO weather code ke condition string
                string condition = weatherCode switch
                {
                    0 => "clear",
                    1 or 2 or 3 => "cloud",
                    45 or 48 => "fog",
                    51 or 53 or 55 => "drizzle",
                    56 or 57 => "freezing drizzle",
                    61 or 63 or 65 => "rain",
                    66 or 67 => "freezing rain",
                    71 or 73 or 75 => "snow",
                    77 => "snow grains",
                    80 or 81 or 82 => "rain showers",
                    85 or 86 => "snow showers",
                    95 => "thunderstorm",
                    96 or 99 => "thunderstorm hail",
                    _ => "clear"
                };
                
                Debug.WriteLine($"[Weather] OpenMeteo: {City} ({lat},{lon}) - Code: {weatherCode} => {condition}");
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
            string assetPath = Path.Combine(baseDir, "Tools", "Plugins", "zeromix.weather", "assets", fileName);

            if (!File.Exists(assetPath))
            {
                string devPath = Path.Combine(baseDir, "..", "..", "..", "Tools", "Plugins", "zeromix.weather", "assets", fileName);
                if (File.Exists(devPath)) assetPath = devPath;
            }

            if (!File.Exists(assetPath))
            {
                string absolutePath = $@"C:\ZeroMix\ZeroMix\Tools\Plugins\zeromix.weather\assets\{fileName}";
                if (File.Exists(absolutePath)) assetPath = absolutePath;
            }

            if (File.Exists(assetPath))
            {
                Debug.WriteLine($"[Weather] SUCCESS: Found {fileName} at {assetPath}");
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    WallpaperManager.LaunchVideoWallpaper(assetPath, volume);
                });
            }
            else
            {
                Debug.WriteLine($"[Weather] ERROR: Could not find ANY video assets named {fileName}");
                string fallback = Path.Combine(baseDir, "Assets", "Data", "Video", "Vs (1).mp4");
                if (File.Exists(fallback))
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                        WallpaperManager.LaunchVideoWallpaper(fallback, 0);
                    });
                }
            }
            return Task.CompletedTask;
        }
    }
}
