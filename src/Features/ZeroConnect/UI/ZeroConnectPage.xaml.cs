using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Linq;

namespace ZeroMix.Features.ZeroConnect.UI
{
    public partial class ZeroConnectPage : System.Windows.Controls.UserControl
    {
        private ZeroConnectServer? _server;
        private readonly PairingService _pairing = new();
        private ClipboardMonitor? _clipboardMonitor;
        private FileHistoryService? _fileHistory;
        private System.Windows.Forms.NotifyIcon? _trayIcon;

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
            // File history service
            var fileHistory = new FileHistoryService();
            var downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "ZeroConnect");
            var fileSvc = new FileTransferService(downloads, fileHistory);
            var pairingSvc = new PairingService();
            _server = new ZeroConnectServer(clipboardSvc, fileSvc, pairingSvc, fileHistory);
            // expose to page for UI updates
            _fileHistory = fileHistory;

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
            // Provide clipboard service so monitor can avoid reacting to our own Set calls
            _clipboardMonitor.ClipboardService = clipboardSvc;
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

            // Image clipboard support
            _clipboardMonitor.OnImageCopied += async (_, bytes) =>
            {
                try
                {
                    if (_server != null && bytes != null && bytes.Length > 0)
                    {
                        await _server.BroadcastClipboardImageAsync(bytes);
                        Dispatcher.Invoke(() =>
                        {
                            LogText.Text += $"[{DateTime.Now:HH:mm:ss}] [Clipboard] Auto-push image ({bytes.Length/1024}KB)\n";
                            LogScroll.ScrollToBottom();
                        });
                    }
                }
                catch { }
            };

            _clipboardMonitor.Start(mainWindow);

            // wire history change
            if (_fileHistory != null)
            {
                _fileHistory.OnChanged += async (_, __) => await Dispatcher.InvokeAsync(() => RefreshHistory());
                await RefreshHistory();
            }

            // wire devices change
            if (_server != null)
            {
                _server.OnDevicesChanged += (_, __) => Dispatcher.Invoke(() => RefreshDevices());
                await Dispatcher.InvokeAsync(() => RefreshDevices());
            }

            // tray icon
            try
            {
                _trayIcon = new System.Windows.Forms.NotifyIcon();
                _trayIcon.Icon = System.Drawing.SystemIcons.Application;
                _trayIcon.Visible = true;
                _trayIcon.Text = "ZeroConnect: 0 devices";
                var menu = new System.Windows.Forms.ContextMenuStrip();
                var stopItem = new System.Windows.Forms.ToolStripMenuItem("Stop ZeroConnect");
                stopItem.Click += (s, ev) => Dispatcher.Invoke(() => OnStop(null, null));
                menu.Items.Add(stopItem);
                _trayIcon.ContextMenuStrip = menu;
            }
            catch { }

            // auto-stop on window close
            try
            {
                var mw = Window.GetWindow(this);
                if (mw != null)
                    mw.Closed += (_, __) => { _server?.Stop(); _trayIcon?.Dispose(); };
            }
            catch { }

            StatusText.Text = "● Running";
            StatusText.Foreground = System.Windows.Media.Brushes.LimeGreen;
            StartBtn.IsEnabled = false;
            StopBtn.IsEnabled = true;
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var win = new SettingsWindow { Owner = Window.GetWindow(this) };
                var res = win.ShowDialog();
                if (res == true)
                {
                    // settings changed, UI consumers can subscribe to SettingsService.Instance.OnChanged
                    // For demo: update FileHistory visibility or other welcome elements here if present
                }
            }
            catch { }
        }

            private void DropZone_PreviewDragOver(object sender, System.Windows.DragEventArgs e)
            {
                e.Handled = true;
                                if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
                                    e.Effects = System.Windows.DragDropEffects.Copy;
                else
                                    e.Effects = System.Windows.DragDropEffects.None;
            }

            private async void DropZone_Drop(object sender, System.Windows.DragEventArgs e)
            {
                try
                {
                    if (!e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop)) return;
                                        var files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                    if (files == null || files.Length == 0) return;

                    foreach (var filePath in files)
                    {
                        try
                        {
                            var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
                            var fileName = System.IO.Path.GetFileName(filePath);
                            if (_server != null)
                            {
                                await _server.SendFileToAllAsync(fileName, bytes);
                                LogText.Text += $"[{DateTime.Now:HH:mm:ss}] [Send] {fileName} → HP ({bytes.Length/1024}KB)\n";
                                LogScroll.ScrollToBottom();
                            }
                        }
                        catch (Exception ex)
                        {
                            LogText.Text += $"[{DateTime.Now:HH:mm:ss}] [Error] {ex.Message}\n";
                            LogScroll.ScrollToBottom();
                        }
                    }
                }
                catch { }
            }

        private async Task RefreshHistory()
        {
            if (_fileHistory == null) return;
            var items = await _fileHistory.GetTodayEntriesAsync();
            FileHistoryList.ItemsSource = items.ToList();
        }

        private void OpenHistoryItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Button btn && btn.DataContext is FileHistoryEntry entry)
                {
                    if (!string.IsNullOrEmpty(entry.Path) && File.Exists(entry.Path))
                    {
                        Process.Start("explorer.exe", $"/select,\"{entry.Path}\"");
                    }
                    else
                    {
                        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "ZeroConnect");
                        Process.Start("explorer.exe", dir);
                    }
                }
            }
            catch { }
        }

        private async void DisconnectDevice_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Button btn && btn.Tag is string id && !string.IsNullOrEmpty(id))
                {
                    if (_server != null)
                        await _server.DisconnectClientAsync(id);
                }
            }
            catch { }
        }

        private async void RefreshDevices()
        {
            try
            {
                if (_server == null) return;
                var devices = _server.GetConnectedDevices();
                DevicesList.ItemsSource = devices;
                ConnectedText.Text = $"{devices.Length} perangkat terhubung";
                try { _trayIcon.Text = $"ZeroConnect: {devices.Length} device(s)"; } catch { }
            }
            catch { }
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