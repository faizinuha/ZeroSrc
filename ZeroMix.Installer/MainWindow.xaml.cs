using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using WpfAnimatedGif;

using WpfApp = System.Windows.Application;
using WpfMsgBox = System.Windows.MessageBox;
using WpfMsgBoxButton = System.Windows.MessageBoxButton;
using WpfMsgBoxImage = System.Windows.MessageBoxImage;
using WpfMsgBoxResult = System.Windows.MessageBoxResult;

namespace ZeroMix.Installer;

public partial class MainWindow : Window
{
    private const string DownloadUrl =
        "https://github.com/faizinuha/ZeroMix/releases/latest/download/ZeroMix-Setup.zip";

    private CancellationTokenSource? _cts;
    private bool _isRunning = false;

    public MainWindow()
    {
        InitializeComponent();

        // Load GIF from embedded resource
        var gifUri = new Uri("pack://application:,,,/Load.gif");
        var gifImage = new BitmapImage(gifUri);
        ImageBehavior.SetAnimatedSource(LoadingGif, gifImage);
        ImageBehavior.SetRepeatBehavior(LoadingGif, System.Windows.Media.Animation.RepeatBehavior.Forever);

        SizeChanged += (_, _) =>
        {
            var parent = (System.Windows.Controls.Border)ProgressFill.Parent;
            UpdateProgressFill(ProgressFill.Width == 0 ? 0 :
                ProgressFill.Width / parent.ActualWidth * 100);
        };
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
            var result = WpfMsgBox.Show(
                "Instalasi sedang berjalan. Yakin ingin membatalkan?",
                "ZeroMix Installer", WpfMsgBoxButton.YesNo, WpfMsgBoxImage.Warning);
            if (result != WpfMsgBoxResult.Yes) return;
        }
        _cts?.Cancel();
        WpfApp.Current.Shutdown();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
        => CloseButton_Click(sender, e);

    private async void InstallButton_Click(object sender, RoutedEventArgs e)
    {
        _cts = new CancellationTokenSource();
        _isRunning = true;
        InstallButton.IsEnabled = false;
        CancelButton.Content = "Batalkan";

        // Show GIF, hide static icon
        LoadingGif.Visibility = Visibility.Visible;
        LogoIcon.Visibility = Visibility.Collapsed;

        var tempDir = Path.Combine(Path.GetTempPath(), "ZeroMixInstaller");
        var zipPath = Path.Combine(tempDir, "ZeroMix-Setup.zip");
        var extractDir = Path.Combine(tempDir, "extracted");

        try
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
            Directory.CreateDirectory(tempDir);

            await DownloadFileAsync(DownloadUrl, zipPath, _cts.Token);
            if (_cts.Token.IsCancellationRequested) return;

            SetStatus("Mengekstrak file...", 92, "Extracting...");
            await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, extractDir), _cts.Token);

            SetStatus("Memulai installer...", 98, "Launching...");
            var installerPath = FindInstaller(extractDir);

            if (installerPath == null)
            {
                WpfMsgBox.Show("File installer tidak ditemukan di dalam arsip.",
                    "Error", WpfMsgBoxButton.OK, WpfMsgBoxImage.Error);
                ResetUI();
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = installerPath,
                UseShellExecute = true
            });

            SetStatus("Installer diluncurkan! ✓", 100, "Done");
            await Task.Delay(1500);
            WpfApp.Current.Shutdown();
        }
        catch (OperationCanceledException)
        {
            SetStatus("Instalasi dibatalkan.", 0, "");
            ResetUI();
        }
        catch (Exception ex)
        {
            WpfMsgBox.Show($"Terjadi kesalahan:\n\n{ex.Message}",
                "Error", WpfMsgBoxButton.OK, WpfMsgBoxImage.Error);
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
                SetStatus(
                    $"Mengunduh ZeroMix... {FormatBytes(downloadedBytes)} / {FormatBytes(totalBytes)}",
                    percent,
                    $"Downloading  {FormatBytes(downloadedBytes)} / {FormatBytes(totalBytes)}");
            }
            else
            {
                SetStatus($"Mengunduh... {FormatBytes(downloadedBytes)}", -1, "Downloading...");
            }
        }
    }

    private void SetStatus(string message, double progressPercent, string stepLabel)
    {
        Dispatcher.Invoke(() =>
        {
            StatusText.Text = message;
            StepText.Text = stepLabel;

            if (progressPercent >= 0)
            {
                ProgressText.Text = $"{progressPercent:F0}%";
                UpdateProgressFill(progressPercent);
            }
        });
    }

    private void UpdateProgressFill(double percent)
    {
        var parent = (System.Windows.Controls.Border)ProgressFill.Parent;
        var containerWidth = parent.ActualWidth;
        if (containerWidth > 0)
            ProgressFill.Width = containerWidth * (percent / 100.0);
    }

    private void ResetUI()
    {
        Dispatcher.Invoke(() =>
        {
            InstallButton.IsEnabled = true;
            CancelButton.Content = "Batal";
            _isRunning = false;
            LoadingGif.Visibility = Visibility.Collapsed;
            LogoIcon.Visibility = Visibility.Visible;
            ProgressFill.Width = 0;
            ProgressText.Text = "";
            StepText.Text = "";
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
