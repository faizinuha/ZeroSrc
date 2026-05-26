using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ZeroMix.Features.ZeroConnect.UI
{
    public partial class ZeroConnectPage : System.Windows.Controls.UserControl
    {
        private ZeroConnectServer? _server;
        private readonly PairingService _pairing = new();
        private ClipboardMonitor? _clipboardMonitor;

        public ZeroConnectPage()
        {
            InitializeComponent();
        }

        private async void OnStart(object sender, RoutedEventArgs e)
        {
            // Pastikan URL ACL terdaftar agar HttpListener bisa bind tanpa admin
            if (!await IsUrlAclRegistered())
            {
                var result = System.Windows.MessageBox.Show(
                    "ZeroConnect perlu registrasi port sekali (butuh akses admin).\nLanjutkan?",
                    "ZeroConnect Setup", MessageBoxButton.YesNo);

                if (result != MessageBoxResult.Yes) return;

                var ok = await FirewallHelper.RegisterUrlAclAsync();
                await FirewallHelper.AddFirewallRuleAsync();

                if (!ok)
                {
                    System.Windows.MessageBox.Show("Gagal registrasi. Coba jalankan ZeroMix sebagai Administrator.");
                    return;
                }
            }

            var clipboardSvc = new ClipboardBridgeService();
            var fileSvc = new FileTransferService();
            var pairingSvc = new PairingService();
            _server = new ZeroConnectServer(clipboardSvc, fileSvc, pairingSvc);

            _server.OnLog += (_, msg) => Dispatcher.Invoke(() =>
            {
                LogText.Text += $"[{DateTime.Now:HH:mm:ss}] {msg}\n";
                LogScroll.ScrollToBottom();
            });

            // Start server
            _ = _server.StartAsync();

            // QR Code
            var url = pairingSvc.GetPairingUrl(_server.Port, _server.PairingToken);
            QrImage.Source = pairingSvc.GenerateQrCode(url);
            UrlText.Text = url;

            // Start clipboard monitor (auto-push)
            var mainWindow = Window.GetWindow(this)!;
            _clipboardMonitor = new ClipboardMonitor();
            _clipboardMonitor.OnTextCopied += async (_, text) =>
            {
                try
                {
                    if (_server != null)
                    {
                        await _server.BroadcastClipboardAsync(text);
                        Dispatcher.Invoke(() =>
                        {
                            var snippet = text?.Length > 30 ? text.Substring(0, 30) + "..." : text;
                            LogText.Text += $"[{DateTime.Now:HH:mm:ss}] [Clipboard] Auto-push: \"{snippet}\"\n";
                            LogScroll.ScrollToBottom();
                        });
                    }
                }
                catch { }
            };
            _clipboardMonitor.Start(mainWindow);

            StatusText.Text = "● Running";
            StatusText.Foreground = System.Windows.Media.Brushes.LimeGreen;
            StartBtn.IsEnabled = false;
            StopBtn.IsEnabled = true;
        }

        private void OnStop(object sender, RoutedEventArgs e)
        {
            _server?.Stop();
            _clipboardMonitor?.Dispose();
            _clipboardMonitor = null;
            StatusText.Text = "● Stopped";
            StatusText.Foreground = System.Windows.Media.Brushes.OrangeRed;
            StartBtn.IsEnabled = true;
            StopBtn.IsEnabled = false;
        }

        private static async Task<bool> IsUrlAclRegistered()
        {
            var psi = new ProcessStartInfo("netsh", "http show urlacl url=http://+:9876/")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            var proc = Process.Start(psi)!;
            var output = await proc.StandardOutput.ReadToEndAsync();
            return output.Contains("9876");
        }
    }
}