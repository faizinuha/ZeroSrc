using System;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using System.Windows.Controls;

namespace ZeroMix.Onboarding
{
    public partial class OnboardingWindow : Window
    {
        public event Action? OnOnboardingFinished;
        private int _currentSlideIndex = 0;
        private const int TotalSlides = 7;
        private List<Grid> _slides = new List<Grid>();
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(8) // batas waktu agar tidak hang lama
        };

        public OnboardingWindow()
        {
            InitializeComponent();
            InitializeSlides();
            UpdateUI();
            // Changelog dimuat lazy — hanya saat user sampai slide 5
        }

        private void InitializeSlides()
        {
            _slides.Add(Slide0);
            _slides.Add(Slide1);
            _slides.Add(Slide2);
            _slides.Add(Slide3);
            _slides.Add(Slide4);
            _slides.Add(Slide5);
            _slides.Add(Slide6);
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSlideIndex < TotalSlides - 1)
            {
                _currentSlideIndex++;
                UpdateUI();
                PlayTransition();

                // Load changelog lazy hanya saat slide 5 pertama kali dibuka
                if (_currentSlideIndex == 5 && ReleasesParams.ItemsSource == null)
                    _ = LoadChangelogAsync();
            }
            else
            {
                FinishOnboarding();
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSlideIndex > 0)
            {
                _currentSlideIndex--;
                UpdateUI();
                PlayTransition();
            }
        }

        private void UpdateUI()
        {
            // Update Slide Visibility
            for (int i = 0; i < _slides.Count; i++)
            {
                _slides[i].Visibility = (i == _currentSlideIndex) ? Visibility.Visible : Visibility.Collapsed;
            }

            // Update Navigation Buttons
            BtnBack.Visibility = (_currentSlideIndex == 0) ? Visibility.Collapsed : Visibility.Visible;
            
            if (_currentSlideIndex == TotalSlides - 1)
            {
                BtnNext.Content = "🚀 Get Started";
                BtnNext.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(10, 132, 255));
            }
            else
            {
                BtnNext.Content = "Next →";
                BtnNext.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(10, 132, 255));
            }

            SlideIndicator.Text = $"STEP {_currentSlideIndex + 1} OF {TotalSlides}";
        }

        private void PlayTransition()
        {
            var sb = (Storyboard)FindResource("FadeIn");
            sb.Begin(_slides[_currentSlideIndex]);
        }

        private void FinishOnboarding()
        {
            try
            {
                string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ZeroMix", "config.json");
                Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
                File.WriteAllText(configPath, "{\"IsFirstRun\": false}");
            }
            catch { /* ignore */ }

            OnOnboardingFinished?.Invoke();
            this.Close();
        }

        private async Task LoadChangelogAsync()
        {
            try
            {
                _httpClient.DefaultRequestHeaders.UserAgent.Clear();
                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("ZeroMix-App");

                var url = "https://api.github.com/repos/faizinuha/ZeroMix/releases";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return;

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var releases = JsonSerializer.Deserialize<List<GithubRelease>>(json, options);
                if (releases == null || releases.Count == 0) return;

                foreach (var release in releases)
                {
                    if (!string.IsNullOrWhiteSpace(release.Body))
                        release.Body = release.Body.Replace("### ", "").Replace("## ", "").Replace("**", "").Trim();
                }

                Dispatcher.Invoke(() => ReleasesParams.ItemsSource = releases.Take(5));
            }
            catch { /* fail silently — tidak block UI */ }
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
