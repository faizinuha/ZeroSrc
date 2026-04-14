using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using WpfApp = System.Windows.Application;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfColor = System.Windows.Media.Color;

namespace ZeroMix.Plugins.Translate
{
    /// <summary>
    /// Selection Bubble: Monitor clipboard changes → show translation popup
    /// Sederhana: user highlight + Ctrl+C → bubble muncul dengan terjemahan
    /// </summary>
    public class SelectionBubble : IDisposable
    {
        private readonly RealTimeTranslator _translator;
        private string _lastClipboard = "";
        private TranslateBubbleWindow? _currentBubble;

        public string SourceLang { get; set; } = "auto";
        public string TargetLang { get; set; } = "en";
        public event Action<string>? OnLog;

        public SelectionBubble(RealTimeTranslator translator)
        {
            _translator = translator;
            StartClipboardMonitor();
        }

        private void StartClipboardMonitor()
        {
            // Monitor clipboard setiap 500ms
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            timer.Tick += async (_, _) => await CheckClipboardAsync();
            timer.Start();
        }

        private async Task CheckClipboardAsync()
        {
            try
            {
                string current = "";
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        if (System.Windows.Clipboard.ContainsText())
                            current = System.Windows.Clipboard.GetText();
                    }
                    catch { }
                });

                if (string.IsNullOrWhiteSpace(current)) return;
                if (current == _lastClipboard) return;
                if (current.Length > 300) return; // terlalu panjang
                if (current.Length < 2) return;   // terlalu pendek
                if (!current.Any(char.IsLetter)) return; // tidak ada huruf

                _lastClipboard = current;
                OnLog?.Invoke($"[BUBBLE] Detected: {current.Substring(0, Math.Min(30, current.Length))}...");

                string result = await _translator.TranslateApiAsync(current, SourceLang, TargetLang);
                if (string.IsNullOrWhiteSpace(result) || result.StartsWith("[ERROR]"))
                {
                    OnLog?.Invoke($"[BUBBLE] {result}");
                    return;
                }

                // Tampilkan bubble di posisi mouse
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    _currentBubble?.Close();
                    
                    GetCursorPos(out var pt);
                    _currentBubble = new TranslateBubbleWindow(current, result, pt.X, pt.Y);
                    _currentBubble.Show();
                    
                    OnLog?.Invoke($"[BUBBLE] {current.Substring(0, Math.Min(20, current.Length))} → {result}");
                });
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"[BUBBLE ERROR] {ex.Message}");
            }
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out System.Drawing.Point pt);

        public void Dispose()
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() => _currentBubble?.Close());
        }
    }

    /// <summary>
    /// Popup window yang menampilkan hasil terjemahan
    /// </summary>
    public class TranslateBubbleWindow : Window
    {
        public TranslateBubbleWindow(string original, string translated, int mouseX, int mouseY)
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = System.Windows.Media.Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            ResizeMode = ResizeMode.NoResize;
            SizeToContent = SizeToContent.WidthAndHeight;

            // Posisi di atas kursor
            Left = mouseX - 10;
            Top = mouseY - 80;

            var border = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(240, 15, 15, 30)),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(14, 10, 14, 10),
                MaxWidth = 320,
                Effect = new DropShadowEffect
                {
                    BlurRadius = 20,
                    ShadowDepth = 0,
                    Color = Colors.Black,
                    Opacity = 0.7
                }
            };

            var panel = new StackPanel();

            // Original text (kecil, abu-abu)
            panel.Children.Add(new TextBlock
            {
                Text = original.Length > 60 ? original.Substring(0, 60) + "…" : original,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)),
                FontSize = 10,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 4)
            });

            // Divider
            panel.Children.Add(new System.Windows.Shapes.Rectangle
            {
                Height = 1,
                Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(60, 255, 255, 255)),
                Margin = new Thickness(0, 0, 0, 6)
            });

            // Translated text (besar, putih)
            panel.Children.Add(new TextBlock
            {
                Text = translated,
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap
            });

            // Branding
            panel.Children.Add(new TextBlock
            {
                Text = "⚡ ZeroMix Translate",
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(124, 58, 237)),
                FontSize = 9,
                Margin = new Thickness(0, 6, 0, 0),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            });

            border.Child = panel;
            Content = border;

            // Fade in animation
            Opacity = 0;
            Loaded += (_, _) =>
            {
                var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
                BeginAnimation(OpacityProperty, anim);

                // Auto close setelah 4 detik
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(4)
                };
                timer.Tick += (_, _) =>
                {
                    timer.Stop();
                    var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
                    fadeOut.Completed += (_, _) => Close();
                    BeginAnimation(OpacityProperty, fadeOut);
                };
                timer.Start();
            };

            // Klik untuk tutup
            MouseDown += (_, _) => Close();
        }
    }
}