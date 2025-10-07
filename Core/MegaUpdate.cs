using CG.Web.MegaApiClient;
using System;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.IO;

namespace ZeroSrc.Core
{
    public class MegaUpdater
    {
        private const string megaFolderLink = "https://mega.nz/folder/uEdWTbSJ#y1bCKlrXXy93gi3e5zeBXA";
        private readonly string _currentVersion;

        public MegaUpdater()
        {
            _currentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.6.0";
        }

        public async Task CheckAndUpdateAsync()
        {
            var client = new MegaApiClient();
            try
            {
                client.LoginAnonymous();

                var folderUri = new Uri(megaFolderLink);
                var nodes = await client.GetNodesFromLinkAsync(folderUri);

                var versionNode = nodes.FirstOrDefault(n => n.Name == "../version.json");
                if (versionNode == null)
                {
                    ShowMessage("File version.json tidak ditemukan di server pembaruan.", "Update Check Failed");
                    return;
                }

                // Download the version.json file to a temporary path
                string tempVersionFile = Path.Combine(Path.GetTempPath(), "../version.json");
                await client.DownloadFileAsync(versionNode, tempVersionFile);

                // Read the content of the downloaded file
                string jsonContent = await File.ReadAllTextAsync(tempVersionFile);
                var info = JsonSerializer.Deserialize<UpdateInfo>(jsonContent);
                File.Delete(tempVersionFile); // Clean up the temporary file

                if (info == null || string.IsNullOrEmpty(info.LatestVersion))
                {
                    ShowMessage("Gagal membaca informasi versi dari server.", "Update Check Failed");
                    return;
                }

                if (new Version(info.LatestVersion) > new Version(_currentVersion))
                {
                    var result = MessageBox.Show(
                        $"Tersedia versi baru: {info.LatestVersion}\nVersi Anda: {_currentVersion}\n\nPerubahan:\n{info.Changelog}\n\nApakah Anda ingin mengunduh dan menginstal pembaruan sekarang?",
                        "Pembaruan Tersedia",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information
                    );

                    if (result != MessageBoxResult.Yes) return;

                    var updateNode = nodes.FirstOrDefault(n => n.Name == info.UpdateFile);
                    if (updateNode == null)
                    {
                        ShowMessage($"File pembaruan '{info.UpdateFile}' tidak ditemukan di server.", "Update Error");
                        return;
                    }

                    string tempZip = Path.Combine(Path.GetTempPath(), info.UpdateFile);
                    await client.DownloadFileAsync(updateNode, tempZip);

                    // For a real-world scenario, a separate updater process is safer.
                    // This approach is simple but can have file-locking issues.
                    string extractionPath = AppDomain.CurrentDomain.BaseDirectory;
                    ZipFile.ExtractToDirectory(tempZip, extractionPath, true);
                    File.Delete(tempZip);

                    MessageBox.Show("Pembaruan telah berhasil diunduh dan diekstrak. Silakan mulai ulang aplikasi untuk menerapkan perubahan.", "Update Selesai", MessageBoxButton.OK, MessageBoxImage.Information);
                    Application.Current.Shutdown();
                }
                else
                {
                    Debug.WriteLine("ZeroSrc is up to date.");
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Terjadi kesalahan saat memeriksa pembaruan: {ex.Message}", "Update Error");
                Debug.WriteLine($"Update check failed: {ex}");
            }
            finally
            {
                client.Logout();
            }
        }
        
        private void ShowMessage(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private class UpdateInfo
        {
            public string? LatestVersion { get; set; }
            public string? UpdateFile { get; set; }
            public string? Changelog { get; set; }
        }
    }
}
