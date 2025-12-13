using System;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ZeroMix.Onboarding
{
    public partial class OnboardingWindow : Window
    {
        public event Action? OnOnboardingFinished;

        public OnboardingWindow()
        {
            InitializeComponent();
            LoadChangelog();
        }

        // Dipanggil saat onboarding selesai
        private void FinishOnboarding()
        {
            try
            {
                File.WriteAllText("config.json", "{\"IsFirstRun\": false}");
            }
            catch
            {
                // ignore
            }

            OnOnboardingFinished?.Invoke();
            this.Close();
        }

        private async void LoadChangelog()
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("ZeroMix-App");

                var url = "https://api.github.com/repos/faizinuha/ZeroMix/releases";
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return;

                var json = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var releases = JsonSerializer.Deserialize<List<GithubRelease>>(json, options);

                if (releases == null || releases.Count == 0)
                    return;

                foreach (var release in releases)
                {
                    if (!string.IsNullOrWhiteSpace(release.Body))
                    {
                        release.Body = release.Body
                            .Replace("### ", "")
                            .Replace("## ", "")
                            .Replace("**", "")
                            .Trim();
                    }
                }

                ReleasesParams.ItemsSource = releases.Take(5);
            }
            catch
            {
                // fail silently (offline / rate limit / dll)
            }
        }

        // Button Finish / Continue
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            FinishOnboarding();
        }
    }

    public class GithubRelease
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("body")]
        public string? Body { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
