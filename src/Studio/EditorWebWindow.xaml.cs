using System;
using System.IO;
using System.Windows;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json.Linq;
using WinMsgBox = System.Windows.MessageBox;

namespace ZeroMix.Studio
{
    public partial class EditorWebWindow : Wpf.Ui.Controls.FluentWindow
    {
        private bool _initialized = false;

        public EditorWebWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await InitWebView();
        }

        private async Task InitWebView()
        {
            try
            {
                var userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ZeroMix", "WebView2_56Editor");

                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                await WebView.EnsureCoreWebView2Async(env);

                // Settings
                var s = WebView.CoreWebView2.Settings;
                s.IsStatusBarEnabled = false;
                s.IsZoomControlEnabled = false;
                s.AreDevToolsEnabled = false;
                s.IsGeneralAutofillEnabled = false;
                s.IsPasswordAutosaveEnabled = false;
                s.AreBrowserAcceleratorKeysEnabled = false;
                s.IsSwipeNavigationEnabled = false;

                // Map app folder to virtual host
                string appBase = AppDomain.CurrentDomain.BaseDirectory;
                WebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "56editor.zeromix.app", appBase,
                    CoreWebView2HostResourceAccessKind.Allow);

                // Inject Pixabay API key before page loads
                WebView.CoreWebView2.NavigationStarting += (_, args) =>
                {
                    if (args.Uri.Contains("56editor.zeromix.app"))
                    {
                        WebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
                            $"window.PIXABAY_KEY = '{ApiKeys.PixabayVideoKey}';");
                    }
                };

                // Handle messages from the web editor (export requests)
                WebView.WebMessageReceived += OnWebMessageReceived;

                // Navigate
                string htmlPath = Path.Combine(appBase, "Web", "56editor", "index.html");
                if (!File.Exists(htmlPath))
                {
                    WinMsgBox.Show($"56Editor files not found at:\n{htmlPath}", "56Editor", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Close();
                    return;
                }

                WebView.Source = new Uri("https://56editor.zeromix.app/Web/56editor/index.html");

                // Hide loading overlay when navigation completes
                var tcs = new TaskCompletionSource<bool>();
                void OnNavDone(object? s2, CoreWebView2NavigationCompletedEventArgs args)
                {
                    WebView.CoreWebView2.NavigationCompleted -= OnNavDone;
                    tcs.TrySetResult(true);
                }
                WebView.CoreWebView2.NavigationCompleted += OnNavDone;
                await tcs.Task;

                _initialized = true;
                Dispatcher.Invoke(() => LoadingOverlay.Visibility = Visibility.Collapsed);
            }
            catch (Exception ex)
            {
                WinMsgBox.Show($"Failed to initialize 56Editor:\n{ex.Message}", "56Editor", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var json = JObject.Parse(e.TryGetWebMessageAsString());
                var type = json["type"]?.ToString();

                if (type == "export")
                {
                    var data = json["data"];
                    HandleExport(data);
                }
            }
            catch { /* ignore malformed messages */ }
        }

        private void HandleExport(JToken? data)
        {
            if (data == null) return;

            Dispatcher.Invoke(() =>
            {
                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Export Video",
                    Filter = "MP4 Video|*.mp4|WebM Video|*.webm",
                    FileName = data["filename"]?.ToString() ?? "56editor-export",
                };

                if (dlg.ShowDialog() != true) return;

                // Pass to FFmpeg export (reuse StudioWindow export logic via process)
                var clips = data["clips"] as JArray;
                if (clips == null || clips.Count == 0)
                {
                    WinMsgBox.Show("No clips to export.", "56Editor", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Build FFmpeg command for the first video clip (basic export)
                var firstVideo = clips[0];
                string? srcPath = firstVideo["src"]?.ToString();

                // blob: URLs can't be used directly by FFmpeg — notify user
                if (srcPath?.StartsWith("blob:") == true)
                {
                    WinMsgBox.Show(
                        "Export via FFmpeg requires saving the source video file first.\n\n" +
                        "This feature will be fully implemented in v7.1.1.",
                        "56Editor — Export",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string ffmpeg = ResolveFFmpegPath();
                if (!File.Exists(ffmpeg))
                {
                    WinMsgBox.Show("FFmpeg not found. Please ensure Tools/FFMPEG/ffmpeg.exe exists.", "56Editor", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string output = dlg.FileName;
                string quality = data["quality"]?.ToString() ?? "medium";
                string crf = quality == "high" ? "18" : quality == "low" ? "28" : "23";

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = ffmpeg,
                    Arguments = $"-y -i \"{srcPath}\" -crf {crf} -preset fast \"{output}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                System.Diagnostics.Process.Start(psi);
                NotifyEditor("Export started. File will be saved to: " + output);
            });
        }

        private void NotifyEditor(string message)
        {
            if (!_initialized) return;
            var escaped = message.Replace("'", "\\'");
            WebView.CoreWebView2.ExecuteScriptAsync($"toast('{escaped}', 'success')");
        }

        private static string ResolveFFmpegPath()
        {
            string[] candidates = {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", "FFMPEG", "ffmpeg.exe"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FFMPEG", "ffmpeg.exe"),
                "ffmpeg.exe",
            };
            foreach (var p in candidates)
                if (File.Exists(p)) return p;
            return candidates[0];
        }
    }
}
