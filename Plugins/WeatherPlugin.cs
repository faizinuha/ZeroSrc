using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace ZeroMix.Plugins
{
    public class WeatherPlugin
    {
        public string City { get; set; } = "Jambewangi";
        public bool IsActive { get; private set; }
        
        public string? SunnyVideoPath { get; set; }
        public string? RainyVideoPath { get; set; }
        public string? CloudyVideoPath { get; set; }

        private DispatcherTimer? _timer;
        private readonly HttpClient _httpClient = new HttpClient();

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
                string url = $"https://wttr.in/{Uri.EscapeDataString(City)}?format=%C";
                var response = await _httpClient.GetStringAsync(url);
                string condition = response.Trim().ToLower();
                
                ApplyWeatherWallpaper(condition);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Weather Plugin Error: {ex.Message}");
            }
        }

        private void ApplyWeatherWallpaper(string condition)
        {
            string? videoPath = null;

            if (condition.Contains("rain") || condition.Contains("drizzle") || condition.Contains("storm"))
                videoPath = RainyVideoPath;
            else if (condition.Contains("cloud") || condition.Contains("overcast") || condition.Contains("mist"))
                videoPath = CloudyVideoPath;
            else
                videoPath = SunnyVideoPath;

            // Fallback to defaults
            if (string.IsNullOrEmpty(videoPath) || !File.Exists(videoPath))
            {
                string videoFile = "Vs (1).mp4";
                if (condition.Contains("rain") || condition.Contains("drizzle") || condition.Contains("storm"))
                    videoFile = "Vs (2).mp4";
                else if (condition.Contains("cloud") || condition.Contains("overcast") || condition.Contains("mist"))
                    videoFile = "Vs (3).mp4";

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                videoPath = Path.Combine(baseDir, "Resource", "Video", videoFile);
            }
            
            if (File.Exists(videoPath))
            {
                Wallpapers.LaunchVideoWallpaperStatic(videoPath);
            }
        }
    }
}
