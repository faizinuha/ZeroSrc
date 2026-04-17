using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace ZeroMix.Plugins.Translate
{
    /// <summary>
    /// Selection Bubble: Deteksi drag-select langsung via global mouse hook.
    /// Saat mouse dilepas setelah drag, langsung ambil teks dari clipboard → translate → tampilkan bubble.
    /// Tidak perlu Ctrl+C manual — jauh lebih cepat.
    /// </summary>
    public class SelectionBubble : IDisposable
    {
        private readonly RealTimeTranslator _translator;
        private string _lastTranslated = "";
        private TranslateBubbleWindow? _currentBubble;
        private IntPtr _hookHandle = IntPtr.Zero;
        private NativeMethods.LowLevelMouseProc? _mouseProc;
        private CancellationTokenSource? _cts;

        // State drag detection
        private bool _isDragging = false;
        private NativeMethods.POINT _dragStart;
        private const int MIN_DRAG_PX = 5;

        public string SourceLang { get; set; } = "auto";
        public string TargetLang { get; set; } = "en";
        public int BubbleDisplaySeconds { get; set; } = 4;
        public event Action<string>? OnLog;

        public SelectionBubble(RealTimeTranslator translator)
        {
            _translator = translator;
            InstallMouseHook();
        }

        private void InstallMouseHook()
        {
            _mouseProc = MouseHookCallback;
            _hookHandle = NativeMethods.SetWindowsHookEx(
                NativeMethods.WH_MOUSE_LL, _mouseProc, IntPtr.Zero, 0);
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var msg = (NativeMethods.MouseMessages)wParam;
                var info = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);

                if (msg == NativeMethods.MouseMessages.WM_LBUTTONDOWN)
                {
                    _isDragging = true;
                    _dragStart = info.pt;
                }
                else if (msg == NativeMethods.MouseMessages.WM_LBUTTONUP && _isDragging)
                {
                    _isDragging = false;
                    int dx = Math.Abs(info.pt.X - _dragStart.X);
                    int dy = Math.Abs(info.pt.Y - _dragStart.Y);

                    // Hanya proses kalau benar-benar drag (bukan klik biasa)
                    if (dx > MIN_DRAG_PX || dy > MIN_DRAG_PX)
                    {
                        int rx = info.pt.X, ry = info.pt.Y;
                        System.Windows.Application.Current?.Dispatcher.BeginInvoke(async () =>
                        {
                            await Task.Delay(80);
                            await TryTranslateSelectionAsync(rx, ry);
                        });
                    }
                }
            }
            return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        private async Task TryTranslateSelectionAsync(int mouseX, int mouseY)
        {
            try
            {
                // Simpan clipboard lama
                string oldClip = "";
                string selected = "";

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    try { oldClip = System.Windows.Clipboard.ContainsText()
                            ? System.Windows.Clipboard.GetText() : ""; }
                    catch { }
                });

                // Kirim Ctrl+C untuk copy selection
                NativeMethods.SimulateCtrlC();
                await Task.Delay(120); // tunggu clipboard update

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        if (System.Windows.Clipboard.ContainsText())
                            selected = System.Windows.Clipboard.GetText();
                    }
                    catch { }
                });

                // Validasi: harus beda dari clipboard lama, ada huruf, tidak terlalu panjang
                if (string.IsNullOrWhiteSpace(selected)) return;
                if (selected == oldClip) return;
                if (selected.Length < 2 || selected.Length > 500) return;
                if (!selected.Any(char.IsLetter)) return;

                // Hindari translate ulang teks yang sama
                if (selected.Trim() == _lastTranslated) return;
                _lastTranslated = selected.Trim();

                OnLog?.Invoke($"[BUBBLE] → {selected.Substring(0, Math.Min(30, selected.Length))}");

                // Cancel terjemahan sebelumnya jika masih berjalan
                _cts?.Cancel();
                _cts = new CancellationTokenSource();
                var token = _cts.Token;

                string result = await _translator.TranslateApiAsync(selected, SourceLang, TargetLang);

                if (token.IsCancellationRequested) return;
                if (string.IsNullOrWhiteSpace(result) || result.StartsWith("[ERROR]"))
                {
                    OnLog?.Invoke($"[BUBBLE] {result}");
                    return;
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    _currentBubble?.Close();
                    _currentBubble = new TranslateBubbleWindow(selected, result, mouseX, mouseY, BubbleDisplaySeconds);
                    _currentBubble.Show();
                    OnLog?.Invoke($"[BUBBLE OK] {selected.Substring(0, Math.Min(20, selected.Length))} → {result}");
                });
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"[BUBBLE ERROR] {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_hookHandle != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_hookHandle);
                _hookHandle = IntPtr.Zero;
            }
            _cts?.Cancel();
            System.Windows.Application.Current?.Dispatcher.Invoke(() => _currentBubble?.Close());
        }
    }

    internal static class NativeMethods
    {
        public const int WH_MOUSE_LL = 14;

        public enum MouseMessages
        {
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP   = 0x0202,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT { public int X; public int Y; }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData, flags, time;
            public IntPtr dwExtraInfo;
        }

        public delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        // Simulate Ctrl+C via SendInput
        [DllImport("user32.dll")]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        private const int INPUT_KEYBOARD = 1;
        private const ushort VK_CONTROL = 0x11;
        private const ushort VK_C = 0x43;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT { public ushort wVk, wScan; public uint dwFlags, time; public IntPtr dwExtraInfo; }

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type;
            public KEYBDINPUT ki;
        }

        public static void SimulateCtrlC()
        {
            var inputs = new[]
            {
                new INPUT { type = INPUT_KEYBOARD, ki = new KEYBDINPUT { wVk = VK_CONTROL } },
                new INPUT { type = INPUT_KEYBOARD, ki = new KEYBDINPUT { wVk = VK_C } },
                new INPUT { type = INPUT_KEYBOARD, ki = new KEYBDINPUT { wVk = VK_C, dwFlags = KEYEVENTF_KEYUP } },
                new INPUT { type = INPUT_KEYBOARD, ki = new KEYBDINPUT { wVk = VK_CONTROL, dwFlags = KEYEVENTF_KEYUP } },
            };
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        }
    }

    /// <summary>
    /// Popup window yang menampilkan hasil terjemahan
    /// </summary>
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
            Top  = mouseY - 90;

            var border = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(245, 13, 17, 28)),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(14, 10, 14, 10),
                MaxWidth = 340,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(80, 0, 191, 165)),
                Effect = new DropShadowEffect
                {
                    BlurRadius = 24,
                    ShadowDepth = 0,
                    Color = Colors.Black,
                    Opacity = 0.75
                }
            };

            var panel = new StackPanel();

            // Original text
            panel.Children.Add(new TextBlock
            {
                Text = original.Length > 70 ? original.Substring(0, 70) + "…" : original,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(90, 100, 120)),
                FontSize = 10,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 6)
            });

            panel.Children.Add(new System.Windows.Shapes.Rectangle
            {
                Height = 1,
                Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 255, 255, 255)),
                Margin = new Thickness(0, 0, 0, 7)
            });

            // Translated text
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
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 60, 210)),
                FontSize = 9,
                Margin = new Thickness(0, 7, 0, 0),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            });

            border.Child = panel;
            Content = border;

            Opacity = 0;
            Loaded += (_, _) =>
            {
                BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150)));

                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(displaySeconds)
                };
                timer.Tick += (_, _) =>
                {
                    timer.Stop();
                    var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(250));
                    fadeOut.Completed += (_, _) => Close();
                    BeginAnimation(OpacityProperty, fadeOut);
                };
                timer.Start();
            };

            MouseDown += (_, _) => Close();
        }
    }
}
