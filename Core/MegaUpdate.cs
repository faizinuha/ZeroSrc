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

namespace ZeroMix.Core
{
    public class MegaUpdater
    {
        private const string megaFolderLink = "https://mega.nz/folder/uEdWTbSJ#y1bCKlrXXy93gi3e5zeBXA";
        private readonly string _currentVersion;

        public MegaUpdater()
        {
            _currentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.6.6";
        }

        public async Task CheckAndUpdateAsync()
        {
            var client = new MegaApiClient();
            try
            {
                client.LoginAnonymous();

                var folderUri = new Uri(megaFolderLink);
                var nodes = await client.GetNodesFromLinkAsync(folderUri);

                var versionNode = nodes.FirstOrDefault(n => n.Name == "version.json");
                if (versionNode == null)
                {
                    // File version.json tidak ditemukan, hentikan proses secara diam-diam.
                    Debug.WriteLine("version.json not found on the update server.");
                    return;
                }

                // Download the version.json file to a temporary path
                string tempVersionFile = Path.Combine(Path.GetTempPath(), "version.json");
                await client.DownloadFileAsync(versionNode, tempVersionFile);

                // Read the content of the downloaded file
                string jsonContent = await File.ReadAllTextAsync(tempVersionFile);
                var info = JsonSerializer.Deserialize<UpdateInfo>(jsonContent);
                File.Delete(tempVersionFile); // Clean up the temporary file
                if (info == null || string.IsNullOrEmpty(info.LatestVersion))
                {
                    // Gagal membaca JSON, hentikan proses secara diam-diam.
                    Debug.WriteLine("Failed to parse version.json or it is invalid.");
                    return;
                }

                if (new Version(info.LatestVersion) > new Version(_currentVersion))
                {
                    // Gunakan jendela notifikasi kustom
                    string message = $"Versi baru {info.LatestVersion} tersedia!\n\nPerubahan:\n{info.Changelog}\n\nUpdate sekarang?";
                    var notificationWindow = new UpdateNotificationWindow(message);
                    bool? result = notificationWindow.ShowDialog();

                    if (result != true) return;

                    // PERBAIKAN KRITIS: Pastikan file update adalah installer (.exe)
                    // Nama file harus sesuai dengan yang ada di server (misal: "ZeroMix-Setup-1.7.0.exe")
                    if (string.IsNullOrEmpty(info.UpdateFile) || !info.UpdateFile.EndsWith(".exe"))
                    {
                        ShowMessage("Nama file pembaruan tidak valid.", "Update Error");
                        return;
                    }
                    var updateNode = nodes.FirstOrDefault(n => n.Name == info.UpdateFile);
                    if (updateNode == null)
                    {
                        ShowMessage($"File pembaruan '{info.UpdateFile}' tidak ditemukan di server.", "Update Error");
                        return;
                    }

                    string tempInstallerPath = Path.Combine(Path.GetTempPath(), info.UpdateFile);

                    // Tampilkan progress bar dan nonaktifkan tombol
                    notificationWindow.ShowProgress();

                    // Buat progress handler untuk di-pass ke downloader
                    var progressHandler = new Progress<double>(p => notificationWindow.UpdateProgress(p));

                    // Mulai unduhan dengan progress reporting
                    await client.DownloadFileAsync(updateNode, tempInstallerPath, progressHandler);

                    // PERBAIKAN KRITIS: Jalankan installer baru, jangan ekstrak ZIP.
                    // Ini akan menangani hak akses dan menimpa file dengan benar.
                    var processInfo = new ProcessStartInfo(tempInstallerPath)
                    {
                        // Gunakan mode silent agar installer berjalan di latar belakang.
                        Arguments = "/SILENT", 
                        UseShellExecute = true
                    };
                    Process.Start(processInfo);

                    // Beri tahu pengguna bahwa instalasi sedang berjalan
                    notificationWindow.ShowInstalling();

                    // Tutup aplikasi saat ini agar installer bisa berjalan.
                    // Pesan tidak lagi diperlukan karena installer akan menanganinya.
                    Application.Current.Shutdown();
                }
                else
                {
                    Debug.WriteLine("ZeroMix is up to date.");
                }
            }
            catch (Exception ex)
            {
                // Jangan tampilkan pesan error ke pengguna, cukup catat di debug console.
                // ShowMessage($"Terjadi kesalahan saat memeriksa pembaruan: {ex.Message}", "Update Error");
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
