using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace ZeroMix.Updater
{
    public partial class MainWindow : Window
    {
        private const string Repo        = "faizinuha/ZeroMix";
        private const string ApiUrl      = $"https://api.github.com/repos/{Repo}/releases/latest";
        private const string CurrentVer  = "6.9.9"; // di-update tiap release via CI

        private static readonly HttpClient Http = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(8) // Timeout cepat — jangan tunggu lama
        };

        private string? _downloadUrl;
        private string? _fileName;
        private long    _fileSize;
        private string? _latestTag;
        private CancellationTokenSource? _cts;

        // State machine
        private enum State { Checking, ReadyToDownload, Downloading, ReadyToInstall, UpToDate, Error }
        private State _state = State.Checking;

        public MainWindow()
        {
            InitializeComponent();
            Http.DefaultRequestHeaders.UserAgent.ParseAdd("ZeroMix-Updater");

            Loaded += async (_, _) =>
            {
                // Animasi pulse dot
                ((Storyboard)Resources["PulseAnim"]).Begin();
                await CheckForUpdateAsync();
            };
        }

        // ── Check update ──────────────────────────────────────────────────
        private async Task CheckForUpdateAsync()
        {
            SetState(State.Checking);
            try
            {
                // Set timeout per-request agar tidak nunggu lama
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));

                var json      = await Http.GetStringAsync(ApiUrl, cts.Token);
                using var doc = JsonDocument.Parse(json);
                var root      = doc.RootElement;

                _latestTag   = root.GetProperty("tag_name").GetString() ?? "";
                string latestVer = _latestTag.TrimStart('v');

                CurrentVerText.Text = $"v{CurrentVer}";
                LatestVerText.Text  = _latestTag;

                // Ambil changelog dari body release
                string releaseBody = root.TryGetProperty("body", out var bodyEl)
                    ? bodyEl.GetString() ?? "" : "";

                // Cari asset Setup*.exe
                foreach (var asset in root.GetProperty("assets").EnumerateArray())
                {
                    string name = asset.GetProperty("name").GetString() ?? "";
                    if (name.Contains("Setup") && name.EndsWith(".exe"))
                    {
                        _downloadUrl = asset.GetProperty("browser_download_url").GetString();
                        _fileName    = name;
                        _fileSize    = asset.GetProperty("size").GetInt64();
                        break;
                    }
                }

                // Tampilkan changelog
                ShowChangelog(releaseBody, _latestTag ?? "");

                if (IsNewerVersion(latestVer, CurrentVer))
                {
                    TitleText.Text = "Update tersedia!";
                    SubText.Text   = $"ZeroMix {_latestTag} siap didownload";
                    SetState(State.ReadyToDownload);
                }
                else
                {
                    TitleText.Text = "Sudah versi terbaru";
                    SubText.Text   = $"ZeroMix v{CurrentVer} adalah versi terbaru";
                    SetState(State.UpToDate);
                }
            }
            catch (OperationCanceledException)
            {
                TitleText.Text  = "Timeout";
                SubText.Text    = "Koneksi terlalu lambat, coba lagi";
                StatusText.Text = "Request timeout setelah 8 detik";
                SetState(State.Error);
            }
            catch (Exception ex)
            {
                TitleText.Text  = "Gagal cek update";
                SubText.Text    = "Periksa koneksi internet kamu";
                StatusText.Text = ex.Message;
                SetState(State.Error);
            }
        }

        private void ShowChangelog(string body, string tag)
        {
            if (ChangelogPanel == null) return;

            // Ambil max 20 baris pertama dari release body
            var lines = body.Split('\n');
            int maxLines = Math.Min(lines.Length, 20);
            string preview = string.Join("\n", lines[..maxLines]);
            if (lines.Length > maxLines) preview += "\n...";

            ChangelogTag.Text  = $"📋 What's new in {tag}";
            ChangelogText.Text = string.IsNullOrWhiteSpace(preview)
                ? "Lihat release notes di GitHub untuk detail perubahan."
                : preview;

            ChangelogPanel.Visibility = Visibility.Visible;
        }

        // ── Download ──────────────────────────────────────────────────────
        private async Task DownloadAsync()
        {
            if (_downloadUrl == null || _fileName == null) return;

            SetState(State.Downloading);
            _cts = new CancellationTokenSource();

            string targetPath = Path.Combine(Path.GetTempPath(), _fileName);

            try
            {
                using var response = await Http.GetAsync(
                    _downloadUrl, HttpCompletionOption.ResponseHeadersRead, _cts.Token);
                response.EnsureSuccessStatusCode();

                long total = response.Content.Headers.ContentLength ?? _fileSize;
                long downloaded = 0;
                var sw = Stopwatch.StartNew();
                long lastBytes = 0;

                using var stream = await response.Content.ReadAsStreamAsync(_cts.Token);
                using var fs     = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

                var buffer = new byte[81920]; // 80KB buffer
                int read;

                while ((read = await stream.ReadAsync(buffer, _cts.Token)) > 0)
                {
                    await fs.WriteAsync(buffer.AsMemory(0, read), _cts.Token);
                    downloaded += read;

                    // Update UI setiap 200ms
                    if (sw.ElapsedMilliseconds > 200)
                    {
                        double speed = (downloaded - lastBytes) / sw.Elapsed.TotalSeconds;
                        lastBytes = downloaded;
                        sw.Restart();

                        double pct = total > 0 ? (double)downloaded / total * 100 : 0;
                        UpdateProgress(pct, downloaded, total, speed);
                    }
                }

                UpdateProgress(100, total, total, 0);
                TitleText.Text = "Download selesai!";
                SubText.Text   = $"Siap install ZeroMix {_latestTag}";
                _downloadUrl   = targetPath; // reuse untuk launch
                SetState(State.ReadyToInstall);
            }
            catch (OperationCanceledException)
            {
                StatusText.Text = "Download dibatalkan.";
                SetState(State.ReadyToDownload);
                if (File.Exists(targetPath)) File.Delete(targetPath);
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Download gagal: {ex.Message}";
                SetState(State.Error);
            }
        }

        private void UpdateProgress(double pct, long downloaded, long total, double speedBps)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressPct.Text  = $"{pct:F0}%";
                SizeText.Text     = $"{FormatBytes(downloaded)} / {FormatBytes(total)}";
                SpeedText.Text    = speedBps > 0 ? $"{FormatBytes((long)speedBps)}/s" : "";

                // Animate progress bar width
                double trackWidth = ProgressFill.Parent is System.Windows.Controls.Border track
                    ? track.ActualWidth : 416;
                double targetW = trackWidth * (pct / 100.0);
                var anim = new DoubleAnimation(targetW, TimeSpan.FromMilliseconds(150));
                ProgressFill.BeginAnimation(WidthProperty, anim);
            });
        }

        // ── State machine ─────────────────────────────────────────────────
        private void SetState(State s)
        {
            _state = s;
            Dispatcher.Invoke(() =>
            {
                ProgressPanel.Visibility = s == State.Downloading
                    ? Visibility.Visible : Visibility.Collapsed;

                switch (s)
                {
                    case State.Checking:
                        ActionBtn.IsEnabled = false;
                        ActionBtn.Content   = "Mengecek...";
                        CancelBtn.Content   = "Tutup";
                        break;

                    case State.ReadyToDownload:
                        ActionBtn.IsEnabled = true;
                        ActionBtn.Content   = $"Download & Install  ({FormatBytes(_fileSize)})";
                        CancelBtn.Content   = "Nanti saja";
                        StatusText.Text     = "";
                        break;

                    case State.Downloading:
                        ActionBtn.IsEnabled = false;
                        ActionBtn.Content   = "Downloading...";
                        CancelBtn.Content   = "Batalkan";
                        break;

                    case State.ReadyToInstall:
                        ActionBtn.IsEnabled = true;
                        ActionBtn.Content   = "Install Sekarang ✓";
                        CancelBtn.Content   = "Nanti saja";
                        break;

                    case State.UpToDate:
                        ActionBtn.IsEnabled = false;
                        ActionBtn.Content   = "Sudah terbaru ✓";
                        CancelBtn.Content   = "Tutup";
                        StatusText.Text     = "Tidak ada update tersedia.";
                        break;

                    case State.Error:
                        ActionBtn.IsEnabled = true;
                        ActionBtn.Content   = "Coba Lagi";
                        CancelBtn.Content   = "Tutup";
                        break;
                }
            });
        }

        // ── Button handlers ───────────────────────────────────────────────
        private async void ActionBtn_Click(object sender, RoutedEventArgs e)
        {
            switch (_state)
            {
                case State.ReadyToDownload:
                    await DownloadAsync();
                    break;

                case State.ReadyToInstall:
                    if (_downloadUrl != null && File.Exists(_downloadUrl))
                    {
                        // Kill ZeroMix dulu
                        foreach (var p in Process.GetProcessesByName("ZeroMix"))
                            try { p.Kill(); } catch { }

                        await Task.Delay(800);
                        Process.Start(new ProcessStartInfo
                        {
                            FileName  = _downloadUrl,
                            Arguments = "/closeapplications /restartapplications",
                            UseShellExecute = true
                        });
                        Application.Current.Shutdown();
                    }
                    break;

                case State.Error:
                    await CheckForUpdateAsync();
                    break;
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_state == State.Downloading)
            {
                _cts?.Cancel();
            }
            else
            {
                Application.Current.Shutdown();
            }
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            Application.Current.Shutdown();
        }

        private void TitleBar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                DragMove();
        }

        // ── Helpers ───────────────────────────────────────────────────────
        private static bool IsNewerVersion(string latest, string current)
        {
            if (Version.TryParse(latest, out var l) && Version.TryParse(current, out var c))
                return l > c;
            return string.Compare(latest, current, StringComparison.Ordinal) > 0;
        }

        private static string FormatBytes(long bytes) => bytes switch
        {
            >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F1} GB",
            >= 1_048_576     => $"{bytes / 1_048_576.0:F1} MB",
            >= 1_024         => $"{bytes / 1_024.0:F0} KB",
            _                => $"{bytes} B"
        };
    }
}
