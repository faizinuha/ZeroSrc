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
        
        // Curated high quality video links (Acting as the "API" videos)
        private const string RainUrl = "https://assets.mixkit.co/videos/preview/mixkit-heavy-rain-in-the-city-at-night-27515-large.mp4";
        private const string CloudUrl = "https://assets.mixkit.co/videos/preview/mixkit-clouds-moving-fast-in-the-sky-4024-large.mp4";
        private const string SunUrl = "https://assets.mixkit.co/videos/preview/mixkit-sun-beams-shining-through-tree-leaves-2158-large.mp4";

        private DispatcherTimer? _timer;
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly string _cacheDir;

        public WeatherPlugin()
        {
            _cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ZeroMix", "WeatherCache");
            Directory.CreateDirectory(_cacheDir);
        }

        public void Start()
        {
            IsActive = true;
            if (_timer == null)
            {
                _timer = new DispatcherTimer();
                _timer.Interval = TimeSpan.FromMinutes(30);
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
                // Format %C to get condition string
                string url = $"https://wttr.in/{Uri.EscapeDataString(City)}?format=%C";
                var response = await _httpClient.GetStringAsync(url);
                string condition = response.Trim().ToLower();
                
                await ApplyWeatherWallpaperAsync(condition);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Weather Plugin Error: {ex.Message}");
            }
        }

        private async Task ApplyWeatherWallpaperAsync(string condition)
        {
            string targetUrl = SunUrl;
            string fileName = "sunny.mp4";

            if (condition.Contains("rain") || condition.Contains("drizzle") || condition.Contains("storm") || condition.Contains("thunder"))
            {
                targetUrl = RainUrl;
                fileName = "rainy.mp4";
            }
            else if (condition.Contains("cloud") || condition.Contains("overcast") || condition.Contains("mist") || condition.Contains("fog") || condition.Contains("haze"))
            {
                targetUrl = CloudUrl;
                fileName = "cloudy.mp4";
            }

            string localPath = Path.Combine(_cacheDir, fileName);

            // 1. Download if not exists
            if (!File.Exists(localPath))
            {
                try
                {
                    var data = await _httpClient.GetByteArrayAsync(targetUrl);
                    await File.WriteAllBytesAsync(localPath, data);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to download weather video: {ex.Message}");
                    // Fallback to internal if download fails
                    string fallbackName = fileName == "rainy.mp4" ? "Vs (2).mp4" : (fileName == "cloudy.mp4" ? "Vs (3).mp4" : "Vs (1).mp4");
                    localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resource", "Video", fallbackName);
                }
            }

            if (File.Exists(localPath))
            {
                // 2. Optimize for performance (using Wallpapers helper)
                string? optimizedPath = await Task.Run(() => Wallpapers.OptimizeVideoForWallpaperStatic(localPath));
                
                if (!string.IsNullOrEmpty(optimizedPath) && File.Exists(optimizedPath))
                {
                    Wallpapers.LaunchVideoWallpaperStatic(optimizedPath);
                }
                else
                {
                    // Fallback to non-optimized if FFmpeg fails
                    Wallpapers.LaunchVideoWallpaperStatic(localPath);
                }
            }
        }
    }
}
