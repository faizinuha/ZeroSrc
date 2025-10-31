using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace ZeroMix.Core
{
    public class GithubUpdater
    {
        private const string GithubApiUrl = "https://api.github.com/repos/faizinuha/ZeroMix/releases/latest";
        // private const string GithubApiUrl = "https://github.com/faizinuha/ZeroMix/releases/latest";
        private readonly string _currentVersion;

        public GithubUpdater()
        {
            _currentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? " 1.7.2";
        }

        public async Task CheckAndUpdateAsync()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ZeroMix", _currentVersion));

            try
            {
                var response = await client.GetStringAsync(GithubApiUrl);
                var release = JsonSerializer.Deserialize<GithubRelease>(response);

                if (release == null || string.IsNullOrEmpty(release.TagName))
                {
                    Debug.WriteLine("Failed to parse GitHub release info.");
                    return;
                }

                string latestVersionStr = release.TagName.TrimStart('v');

                if (new Version(latestVersionStr) > new Version(_currentVersion))
                {
                    string message = $"Versi baru {latestVersionStr} tersedia!\n\nPerubahan:\n{release.Body}\n\nUpdate sekarang?";
                    var notificationWindow = new UpdateNotificationWindow(message);
                    bool? result = notificationWindow.ShowDialog();

                    if (result != true) return;

                    var installerAsset = release.Assets?.FirstOrDefault(a => a.Name != null && a.Name.EndsWith(".exe"));
                    if (installerAsset == null || string.IsNullOrEmpty(installerAsset.BrowserDownloadUrl))
                    {
                        ShowMessage("File installer (.exe) tidak ditemukan di rilis terbaru.", "Update Error");
                        return;
                    }

                    string tempInstallerPath = Path.Combine(Path.GetTempPath(), installerAsset.Name);

                    notificationWindow.ShowProgress();

                    using (var downloadClient = new HttpClient())
                    {
                        using (var fileStream = new FileStream(tempInstallerPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            var downloadResponse = await downloadClient.GetAsync(installerAsset.BrowserDownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                            downloadResponse.EnsureSuccessStatusCode();

                            long? totalBytes = downloadResponse.Content.Headers.ContentLength;
                            long totalBytesRead = 0;
                            var buffer = new byte[8192];
                            int bytesRead;

                            using (var stream = await downloadResponse.Content.ReadAsStreamAsync())
                            {
                                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                {
                                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                                    totalBytesRead += bytesRead;
                                    if (totalBytes.HasValue)
                                    {
                                        double progressPercentage = (double)totalBytesRead / totalBytes.Value * 100;
                                        notificationWindow.UpdateProgress(progressPercentage);
                                    }
                                }
                            }
                        }
                    }

                    var processInfo = new ProcessStartInfo(tempInstallerPath)
                    {
                        Arguments = "/SILENT",
                        UseShellExecute = true
                    };
                    Process.Start(processInfo);

                    notificationWindow.ShowInstalling();
                    Application.Current.Shutdown();
                }
                else
                {
                    Debug.WriteLine("ZeroMix is up to date.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Update check failed: {ex}");
            }
        }

        private void ShowMessage(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private class GithubRelease
        {
            [JsonPropertyName("tag_name")]
            public string TagName { get; set; }

            [JsonPropertyName("body")]
            public string Body { get; set; }

            [JsonPropertyName("assets")]
            public List<GithubAsset> Assets { get; set; }
        }

        private class GithubAsset
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("browser_download_url")]
            public string BrowserDownloadUrl { get; set; }
        }
    }
}
