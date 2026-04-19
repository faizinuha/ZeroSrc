using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace ZeroMix.Plugins.Translate
{
    /// <summary>
    /// Selection Bubble: Monitor clipboard → highlight + Ctrl+C → bubble terjemahan muncul
    /// </summary>
    public class SelectionBubble : IDisposable
    {
        private readonly RealTimeTranslator _translator;
        private string _lastClipboard = "";
        private TranslateBubbleWindow? _currentBubble;
        private System.Windows.Threading.DispatcherTimer? _timer;

        public string SourceLang { get; set; } = "auto";
        public string TargetLang { get; set; } = "en";
        public int BubbleDisplaySeconds { get; set; } = 4;
        public event Action<string>? OnLog;

        public SelectionBubble(RealTimeTranslator translator)
        {
            _translator = translator;
            StartClipboardMonitor();
        }

        private void StartClipboardMonitor()
        {
            _timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _timer.Tick += async (_, _) => await CheckClipboardAsync();
            _timer.Start();
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
                if (current.Length > 300) return;
                if (current.Length < 2) return;
                if (!current.Any(char.IsLetter)) return;

                _lastClipboard = current;
                OnLog?.Invoke($"[BUBBLE] Detected: {current.Substring(0, Math.Min(30, current.Length))}...");

                string result = await _translator.TranslateApiAsync(current, SourceLang, TargetLang);
                if (string.IsNullOrWhiteSpace(result) || result.StartsWith("[ERROR]"))
                {
                    OnLog?.Invoke($"[BUBBLE] {result}");
                    return;
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    _currentBubble?.Close();
                    GetCursorPos(out var pt);
                    _currentBubble = new TranslateBubbleWindow(current, result, pt.X, pt.Y, BubbleDisplaySeconds);
                    _currentBubble.Show();
                    OnLog?.Invoke($"[BUBBLE OK] {current.Substring(0, Math.Min(20, current.Length))} → {result}");
                    // Reset agar teks yang sama bisa di-translate lagi
                    _lastClipboard = "";
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
            _timer?.Stop();
            System.Windows.Application.Current?.Dispatcher.Invoke(() => _currentBubble?.Close());
        }
    }

    public class TranslateBubbleWindow : Window
    {
        public TranslateBubbleWindow(string original, string translated, int mouseX, int mouseY, int displaySeconds = 4)
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = System.Windows.Media.Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            ResizeMode = ResizeMode.NoResize;
            SizeToContent = SizeToContent.WidthAndHeight;

            Left = mouseX - 10;
            Top  = mouseY - 80;

            var border = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(240, 15, 15, 30)),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(14, 10, 14, 10),
                MaxWidth = 320,
                Effect = new DropShadowEffect { BlurRadius = 20, ShadowDepth = 0, Color = Colors.Black, Opacity = 0.7 }
            };

            var panel = new StackPanel();

            panel.Children.Add(new TextBlock
            {
                Text = original.Length > 60 ? original.Substring(0, 60) + "…" : original,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)),
                FontSize = 10,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 4)
            });

            panel.Children.Add(new System.Windows.Shapes.Rectangle
            {
                Height = 1,
                Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(60, 255, 255, 255)),
                Margin = new Thickness(0, 0, 0, 6)
            });

            panel.Children.Add(new TextBlock
            {
                Text = translated,
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap
            });

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

            Opacity = 0;
            Loaded += (_, _) =>
            {
                BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200)));

                var t = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(displaySeconds) };
                t.Tick += (_, _) =>
                {
                    t.Stop();
                    var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
                    fadeOut.Completed += (_, _) => Close();
                    BeginAnimation(OpacityProperty, fadeOut);
                };
                t.Start();
            };

            MouseDown += (_, _) => Close();
        }
    }
}
