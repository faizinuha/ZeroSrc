using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

using KeyEventArgs    = System.Windows.Input.KeyEventArgs;
using Color           = System.Windows.Media.Color;
using Brushes         = System.Windows.Media.Brushes;
using Cursors         = System.Windows.Input.Cursors;
using Image           = System.Windows.Controls.Image;
using BitmapScalingMode = System.Windows.Media.BitmapScalingMode;
using RenderOptions   = System.Windows.Media.RenderOptions;

namespace ZeroMix.ZeroShell
{
    public partial class ZeroLaunchpad : Window
    {
        private List<AppEntry> _allApps = new();
        private List<AppEntry> _filtered = new();
        private CancellationTokenSource? _loadCts;
        private CancellationTokenSource? _searchCts;

        // Responsive tile sizing
        private const double TILE_WIDTH = 96;
        private const double TILE_HEIGHT = 104;
        private const double TILE_MARGIN = 6;
        private const double GRID_MARGIN = 40;

        private record AppEntry(string Name, string Path, ImageSource? Icon);

        public ZeroLaunchpad()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Cover ALL screens (multi-monitor)
            try
            {
                this.Left   = SystemParameters.VirtualScreenLeft;
                this.Top    = SystemParameters.VirtualScreenTop;
                this.Width  = SystemParameters.VirtualScreenWidth;
                this.Height = SystemParameters.VirtualScreenHeight;
            }
            catch
            {
                this.Left = 0; this.Top = 0;
                this.Width = SystemParameters.PrimaryScreenWidth;
                this.Height = SystemParameters.PrimaryScreenHeight;
            }

            // Fade in
            this.Opacity = 0;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180));
            this.BeginAnimation(OpacityProperty, fadeIn);

            // Load apps async with cancellation token
            _loadCts = new CancellationTokenSource();
            var token = _loadCts.Token;
            System.Threading.Tasks.Task.Run(() =>
            {
                try { LoadApps(token); }
                catch (OperationCanceledException) { }
                catch { }
            }, token).ContinueWith(_ =>
            {
                if (!token.IsCancellationRequested)
                {
                    try { Dispatcher.Invoke(RenderApps); }
                    catch { } // window might be closed
                }
            }, TaskContinuationOptions.NotOnCanceled);

            SearchBox.Focus();
        }

        private void Window_Deactivated(object sender, EventArgs e) => CloseSmooth();
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) CloseSmooth();
        }

        private void CloseSmooth()
        {
            // Cancel ongoing operations
            _loadCts?.Cancel();
            _searchCts?.Cancel();

            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150));
            fadeOut.Completed += (s, ev) =>
            {
                try { Close(); }
                catch { }
            };
            this.BeginAnimation(OpacityProperty, fadeOut);
        }

        protected override void OnClosed(EventArgs e)
        {
            _loadCts?.Cancel();
            _loadCts?.Dispose();
            _searchCts?.Cancel();
            _searchCts?.Dispose();
            base.OnClosed(e);
        }

        #region App Loading
        private void LoadApps(CancellationToken token)
        {
            var apps = new List<AppEntry>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. Start Menu shortcuts (user + common)
            var startMenuPaths = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Microsoft", "Windows", "Start Menu"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "Microsoft", "Windows", "Start Menu"),
            };

            foreach (var dir in startMenuPaths)
            {
                token.ThrowIfCancellationRequested();
                if (!Directory.Exists(dir)) continue;
                foreach (var lnk in Directory.GetFiles(dir, "*.lnk", SearchOption.AllDirectories))
                {
                    token.ThrowIfCancellationRequested();
                    try
                    {
                        string name = Path.GetFileNameWithoutExtension(lnk);
                        if (seen.Contains(name)) continue;
                        seen.Add(name);

                        var icon = ShellHelper.GetIcon(lnk);
                        apps.Add(new AppEntry(name, lnk, icon));
                    }
                    catch { }
                }
            }

            // 2. Running processes (quick access)
            foreach (var p in Process.GetProcesses())
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    if (string.IsNullOrEmpty(p.MainWindowTitle)) continue;
                    string name = p.ProcessName;
                    if (seen.Contains(name)) continue;
                    seen.Add(name);
                    string? path = p.MainModule?.FileName;
                    if (path == null) continue;
                    var icon = ShellHelper.GetIcon(path);
                    apps.Add(new AppEntry(name, path, icon));
                }
                catch { }
            }

            token.ThrowIfCancellationRequested();
            _allApps = apps.OrderBy(a => a.Name).ToList();
            _filtered = _allApps;
        }

        private void RenderApps()
        {
            AppGrid.Children.Clear();

            // Calculate responsive tile count based on available width
            double availWidth = Math.Max(200, AppGrid.ActualWidth - GRID_MARGIN * 2);
            int cols = Math.Max(1, (int)((availWidth + TILE_MARGIN) / (TILE_WIDTH + TILE_MARGIN)));
            int maxTiles = Math.Min(_filtered.Count, cols * 3); // 3 rows max

            foreach (var app in _filtered.Take(maxTiles))
                AppGrid.Children.Add(MakeAppTile(app));
        }

        private Border MakeAppTile(AppEntry app)
        {
            var tile = new Border
            {
                Width = TILE_WIDTH, Height = TILE_HEIGHT,
                Margin = new Thickness(TILE_MARGIN),
                CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0x18, 0xFF, 0xFF, 0xFF)),
                Cursor = Cursors.Hand,
                Tag = app
            };

            var sp = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            // Icon
            if (app.Icon != null)
            {
                var img = new Image
                {
                    Source = app.Icon,
                    Width = 48, Height = 48,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    Margin = new Thickness(0, 10, 0, 6)
                };
                System.Windows.Media.RenderOptions.SetBitmapScalingMode(img, System.Windows.Media.BitmapScalingMode.HighQuality);
                sp.Children.Add(img);
            }
            else
            {
                sp.Children.Add(new Border
                {
                    Width = 48, Height = 48,
                    CornerRadius = new CornerRadius(12),
                    Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    Margin = new Thickness(0, 10, 0, 6),
                    Child = new TextBlock
                    {
                        Text = app.Name.Length > 0 ? app.Name[0].ToString().ToUpper() : "?",
                        FontSize = 20, FontWeight = FontWeights.Bold,
                        Foreground = Brushes.White,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                });
            }

            // Name
            sp.Children.Add(new TextBlock
            {
                Text = app.Name.Length > 12 ? app.Name.Substring(0, 11) + "…" : app.Name,
                FontSize = 10, Foreground = Brushes.White,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(4, 0, 4, 8)
            });

            tile.Child = sp;

            // Hover effect
            tile.MouseEnter += (s, e) =>
                tile.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF));
            tile.MouseLeave += (s, e) =>
                tile.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0x18, 0xFF, 0xFF, 0xFF));

            // Click — launch
            tile.MouseLeftButtonDown += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo(app.Path) { UseShellExecute = true }); }
                catch { }
                CloseSmooth();
            };

            return tile;
        }
        #endregion

        #region Search
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Debounce: cancel previous search timer
            _searchCts?.Cancel();
            _searchCts?.Dispose();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            string q = SearchBox.Text.Trim().ToLower();
            SearchHint.Visibility = string.IsNullOrEmpty(q) ? Visibility.Visible : Visibility.Collapsed;

            // Debounce 150ms to avoid re-render on every keystroke
            System.Threading.Tasks.Task.Delay(150, token).ContinueWith(_ =>
            {
                if (token.IsCancellationRequested) return;
                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        _filtered = string.IsNullOrEmpty(q)
                            ? _allApps
                            : _allApps.Where(a => a.Name.ToLower().Contains(q)).ToList();
                        RenderApps();
                    });
                }
                catch { }
            }, token);
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && _filtered.Count > 0)
            {
                var first = _filtered[0];
                try { Process.Start(new ProcessStartInfo(first.Path) { UseShellExecute = true }); }
                catch { }
                CloseSmooth();
            }
        }
        #endregion
    }
}
