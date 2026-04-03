using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;

namespace ZeroMix.Installer;

public partial class MainWindow : Window
{
    // =====================================================================
    // Ganti URL ini dengan link GitHub Releases kamu
    // Format: https://github.com/USERNAME/REPO/releases/latest/download/ZeroMix-Setup.zip
    // =====================================================================
    private const string DownloadUrl = "https://github.com/YOUR_USERNAME/ZeroMix/releases/latest/download/ZeroMix-Setup.zip";

    private CancellationTokenSource? _cts;
    private bool _isRunning = false;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning)
        {
            var result = MessageBox.Show(
                "Instalasi sedang berjalan. Yakin ingin membatalkan?",
                "ZeroMix Installer", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;
        }
        _cts?.Cancel();
        Application.Current.Shutdown();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
        => CloseButton_Click(sender, e);

    private async void InstallButton_Click(object sender, RoutedEventArgs e)
    {
        _cts = new CancellationTokenSource();
        _isRunning = true;
        InstallButton.IsEnabled = false;
        CancelButton.Content = "Batalkan";

        var tempDir = Path.Combine(Path.GetTempPath(), "ZeroMixInstaller");
        var zipPath = Path.Combine(tempDir, "ZeroMix-Setup.zip");
        var extractDir = Path.Combine(tempDir, "extracted");

        try
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
            Directory.CreateDirectory(tempDir);

            // Step 1: Download
            await DownloadFileAsync(DownloadUrl, zipPath, _cts.Token);
            if (_cts.Token.IsCancellationRequested) return;

            // Step 2: Extract
            SetStatus("Mengekstrak file...", 92);
            await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, extractDir), _cts.Token);

            // Step 3: Cari & jalankan installer
            SetStatus("Memulai installer...", 98);
            var installerPath = FindInstaller(extractDir);

            if (installerPath == null)
            {
                MessageBox.Show("File installer tidak ditemukan di dalam arsip.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ResetUI();
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = installerPath,
                UseShellExecute = true
            });

            SetStatus("Installer diluncurkan!", 100);
            await Task.Delay(1500);
            Application.Current.Shutdown();
        }
        catch (OperationCanceledException)
        {
            SetStatus("Instalasi dibatalkan.", 0);
            ResetUI();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Terjadi kesalahan:\n\n{ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            ResetUI();
        }
        finally
        {
            _isRunning = false;
            try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { }
        }
    }

    private async Task DownloadFileAsync(string url, string destPath, CancellationToken ct)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "ZeroMix-Bootstrap/1.0");

        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
        var buffer = new byte[81920];
        long downloadedBytes = 0;

        await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
        await using var fileStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None);

        int bytesRead;
        while ((bytesRead = await contentStream.ReadAsync(buffer, ct)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
            downloadedBytes += bytesRead;

            if (totalBytes > 0)
            {
                var percent = (double)downloadedBytes / totalBytes * 90.0;
                SetStatus($"Mengunduh... {FormatBytes(downloadedBytes)} / {FormatBytes(totalBytes)}", percent);
            }
            else
            {
                SetStatus($"Mengunduh... {FormatBytes(downloadedBytes)}", -1);
            }
        }
    }

    private void SetStatus(string message, double progressPercent)
    {
        Dispatcher.Invoke(() =>
        {
            StatusText.Text = message;
            if (progressPercent >= 0)
            {
                ProgressBar.Value = progressPercent;
                ProgressText.Text = $"{progressPercent:F0}%";
            }
        });
    }

    private void ResetUI()
    {
        Dispatcher.Invoke(() =>
        {
            InstallButton.IsEnabled = true;
            CancelButton.Content = "Batal";
            _isRunning = false;
        });
    }

    private static string? FindInstaller(string dir)
    {
        foreach (var file in Directory.GetFiles(dir, "*.exe", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(file).ToLower();
            if (name.Contains("setup") || name.Contains("install") || name.Contains("zeromix"))
                return file;
        }
        return Directory.GetFiles(dir, "*.exe", SearchOption.AllDirectories).FirstOrDefault();
    }

    private static string FormatBytes(long bytes) => bytes switch
    {
        >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F1} GB",
        >= 1_048_576 => $"{bytes / 1_048_576.0:F1} MB",
        >= 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes} B"
    };
}
