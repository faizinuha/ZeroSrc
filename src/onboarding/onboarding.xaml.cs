using System;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ZeroMix.Onboarding
{
    public partial class OnboardingWindow : Window
    {
        public event Action? OnOnboardingFinished;
        private int _currentSlide = 0;
        private const int TotalSlides = 5;
        private System.Windows.Shapes.Ellipse[] _dots = Array.Empty<System.Windows.Shapes.Ellipse>();

        private static readonly HttpClient _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(8)
        };

        public OnboardingWindow()
        {
            InitializeComponent();
            _dots = new[] { Dot0, Dot1, Dot2, Dot3, Dot4 };
            UpdateUI();
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSlide < TotalSlides - 1)
            {
                _currentSlide++;
                UpdateUI();

                // Load changelog lazy saat slide 3 (What's New)
                if (_currentSlide == 3 && ReleasesParams.ItemsSource == null)
                    _ = LoadChangelogAsync();
            }
            else
            {
                FinishOnboarding();
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSlide > 0)
            {
                _currentSlide--;
                UpdateUI();
            }
        }

        private void UpdateUI()
        {
            // Show/hide slides
            Slide0.Visibility = _currentSlide == 0 ? Visibility.Visible : Visibility.Collapsed;
            Slide1.Visibility = _currentSlide == 1 ? Visibility.Visible : Visibility.Collapsed;
            Slide2.Visibility = _currentSlide == 2 ? Visibility.Visible : Visibility.Collapsed;
            Slide3.Visibility = _currentSlide == 3 ? Visibility.Visible : Visibility.Collapsed;
            Slide4.Visibility = _currentSlide == 4 ? Visibility.Visible : Visibility.Collapsed;

            // Update dots
            var active   = (SolidColorBrush)new BrushConverter().ConvertFrom("#58A6FF")!;
            var inactive = (SolidColorBrush)new BrushConverter().ConvertFrom("#30363D")!;
            for (int i = 0; i < _dots.Length; i++)
                _dots[i].Fill = i == _currentSlide ? active : inactive;

            // Back button
            BtnBack.Visibility = _currentSlide == 0 ? Visibility.Collapsed : Visibility.Visible;

            // Next button label
            BtnNext.Content = _currentSlide == TotalSlides - 1 ? "Get Started" : "Continue";
        }

        private void FinishOnboarding()
        {
            try
            {
                string configPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ZeroMix", "config.json");
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(configPath)!);
                File.WriteAllText(configPath, "{\"IsFirstRun\": false}");
            }
            catch { }

            OnOnboardingFinished?.Invoke();
            this.Close();
        }

        private async Task LoadChangelogAsync()
        {
            try
            {
                _http.DefaultRequestHeaders.UserAgent.Clear();
                _http.DefaultRequestHeaders.UserAgent.ParseAdd("ZeroMix-App");

                var json = await _http.GetStringAsync("https://api.github.com/repos/faizinuha/ZeroMix/releases");
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var releases = JsonSerializer.Deserialize<List<GithubRelease>>(json, options);
                if (releases == null || releases.Count == 0) return;

                foreach (var r in releases)
                {
                    if (!string.IsNullOrWhiteSpace(r.Body))
                        r.Body = r.Body.Replace("### ", "").Replace("## ", "").Replace("**", "").Trim();
                }

                Dispatcher.Invoke(() => ReleasesParams.ItemsSource = releases.Take(4));
            }
            catch { }
        }
    }

    public class GithubRelease
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }

        [JsonPropertyName("body")]
        public string? Body { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
