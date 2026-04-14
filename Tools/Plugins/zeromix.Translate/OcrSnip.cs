using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ZeroMix.Plugins.Translate
{
    /// <summary>
    /// OCR Snip: Screen capture + OCR + Translation
    /// Berguna dan multifungsi: capture area → extract text → translate
    /// </summary>
    public class OcrSnip : IDisposable
    {
        private readonly RealTimeTranslator _translator;
        private SnipOverlayWindow? _overlayWindow;

        public string SourceLang { get; set; } = "auto";
        public string TargetLang { get; set; } = "en";
        public event Action<string>? OnLog;

        public OcrSnip(RealTimeTranslator translator)
        {
            _translator = translator;
        }

        public void StartSnip()
        {
            try
            {
                OnLog?.Invoke("[OCR SNIP] Starting screen capture...");
                
                // Buat overlay untuk selection
                _overlayWindow = new SnipOverlayWindow();
                _overlayWindow.OnAreaSelected += async (rect) => await ProcessSnipAsync(rect);
                _overlayWindow.Show();
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"[OCR ERROR] {ex.Message}");
            }
        }

        private async Task ProcessSnipAsync(System.Drawing.Rectangle area)
        {
            try
            {
                OnLog?.Invoke($"[OCR] Processing area {area.Width}x{area.Height}...");

                // Capture screen area
                using var bitmap = CaptureScreen(area);
                if (bitmap == null)
                {
                    OnLog?.Invoke("[OCR ERROR] Failed to capture screen");
                    return;
                }

                // Convert to base64 for OCR API
                string base64Image = BitmapToBase64(bitmap);
                
                // Extract text using OCR
                string extractedText = await ExtractTextAsync(base64Image);
                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    OnLog?.Invoke("[OCR] No text found in captured area");
                    return;
                }

                OnLog?.Invoke($"[OCR] Extracted: {extractedText.Substring(0, Math.Min(50, extractedText.Length))}...");

                // Translate extracted text
                string translatedText = await _translator.TranslateApiAsync(extractedText, SourceLang, TargetLang);
                if (string.IsNullOrWhiteSpace(translatedText) || translatedText.StartsWith("[ERROR]"))
                {
                    OnLog?.Invoke($"[OCR] Translation failed: {translatedText}");
                    return;
                }

                // Show result in popup
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var resultWindow = new OcrResultWindow(extractedText, translatedText);
                    resultWindow.Show();
                    OnLog?.Invoke($"[OCR SUCCESS] {extractedText.Length} chars → translated");
                });
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"[OCR ERROR] {ex.Message}");
            }
        }

        private Bitmap? CaptureScreen(System.Drawing.Rectangle area)
        {
            try
            {
                var bitmap = new Bitmap(area.Width, area.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(area.X, area.Y, 0, 0, area.Size, CopyPixelOperation.SourceCopy);
                }
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        private string BitmapToBase64(Bitmap bitmap)
        {
            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            return Convert.ToBase64String(stream.ToArray());
        }

        private async Task<string> ExtractTextAsync(string base64Image)
        {
            try
            {
                // Menggunakan OCR.space API (free tier) - fallback ke Tesseract jika gagal
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(30);

                var formData = new MultipartFormDataContent();
                formData.Add(new StringContent(base64Image), "base64Image");
                formData.Add(new StringContent("true"), "isOverlayRequired");
                formData.Add(new StringContent("2"), "OCREngine"); // Engine 2 lebih akurat
                formData.Add(new StringContent("eng"), "language"); // Default English

                var response = await client.PostAsync("https://api.ocr.space/parse/image", formData);
                var jsonResponse = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(jsonResponse);
                var root = doc.RootElement;

                if (root.TryGetProperty("ParsedResults", out var results) && results.GetArrayLength() > 0)
                {
                    var firstResult = results[0];
                    if (firstResult.TryGetProperty("ParsedText", out var parsedText))
                    {
                        string text = parsedText.GetString()?.Trim() ?? "";
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            return text;
                        }
                    }
                }

                // Fallback: Coba dengan engine 1 jika engine 2 gagal
                OnLog?.Invoke("[OCR] Trying fallback engine...");
                formData = new MultipartFormDataContent();
                formData.Add(new StringContent(base64Image), "base64Image");
                formData.Add(new StringContent("1"), "OCREngine"); // Engine 1

                response = await client.PostAsync("https://api.ocr.space/parse/image", formData);
                jsonResponse = await response.Content.ReadAsStringAsync();

                using var doc2 = JsonDocument.Parse(jsonResponse);
                var root2 = doc2.RootElement;

                if (root2.TryGetProperty("ParsedResults", out var results2) && results2.GetArrayLength() > 0)
                {
                    var firstResult2 = results2[0];
                    if (firstResult2.TryGetProperty("ParsedText", out var parsedText2))
                    {
                        return parsedText2.GetString()?.Trim() ?? "";
                    }
                }

                return "";
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"[OCR API ERROR] {ex.Message}");
                return "";
            }
        }

        public void Dispose()
        {
            _overlayWindow?.Close();
        }
    }

    /// <summary>
    /// Overlay window untuk screen selection
    /// </summary>
    public class SnipOverlayWindow : Window
    {
        private bool _isSelecting = false;
        private System.Windows.Point _startPoint;
        private System.Windows.Shapes.Rectangle? _selectionRect;

        public event Action<System.Drawing.Rectangle>? OnAreaSelected;

        public SnipOverlayWindow()
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 0, 0, 0));
            Topmost = true;
            ShowInTaskbar = false;
            WindowState = WindowState.Maximized;
            Cursor = System.Windows.Input.Cursors.Cross;

            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
            KeyDown += OnKeyDown;
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isSelecting = true;
                _startPoint = e.GetPosition(this);
                
                _selectionRect = new System.Windows.Shapes.Rectangle
                {
                    Stroke = System.Windows.Media.Brushes.Red,
                    StrokeThickness = 2,
                    Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(30, 255, 0, 0))
                };
                
                Canvas.SetLeft(_selectionRect, _startPoint.X);
                Canvas.SetTop(_selectionRect, _startPoint.Y);
                
                if (Content == null)
                {
                    Content = new Canvas();
                }
                ((Canvas)Content).Children.Add(_selectionRect);
                
                CaptureMouse();
            }
        }

        private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isSelecting && _selectionRect != null)
            {
                var currentPoint = e.GetPosition(this);
                
                var left = Math.Min(_startPoint.X, currentPoint.X);
                var top = Math.Min(_startPoint.Y, currentPoint.Y);
                var width = Math.Abs(currentPoint.X - _startPoint.X);
                var height = Math.Abs(currentPoint.Y - _startPoint.Y);
                
                Canvas.SetLeft(_selectionRect, left);
                Canvas.SetTop(_selectionRect, top);
                _selectionRect.Width = width;
                _selectionRect.Height = height;
            }
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isSelecting && _selectionRect != null)
            {
                _isSelecting = false;
                ReleaseMouseCapture();
                
                var left = Canvas.GetLeft(_selectionRect);
                var top = Canvas.GetTop(_selectionRect);
                var width = _selectionRect.Width;
                var height = _selectionRect.Height;
                
                if (width > 10 && height > 10) // Minimum size
                {
                    var screenRect = new System.Drawing.Rectangle(
                        (int)left, (int)top, (int)width, (int)height);
                    OnAreaSelected?.Invoke(screenRect);
                }
                
                Close();
            }
        }

        private void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }
    }

    /// <summary>
    /// Window untuk menampilkan hasil OCR + Translation
    /// </summary>
    public class OcrResultWindow : Window
    {
        public OcrResultWindow(string originalText, string translatedText)
        {
            Title = "OCR Snip Result";
            Width = 500;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 15, 30));

            var panel = new StackPanel { Margin = new Thickness(20) };

            // Header
            panel.Children.Add(new TextBlock
            {
                Text = "🔍 OCR Snip Result",
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20)
            });

            // Original text
            panel.Children.Add(new TextBlock
            {
                Text = "Extracted Text:",
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)),
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 5)
            });

            var originalTextBox = new System.Windows.Controls.TextBox
            {
                Text = originalText,
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 45)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 80)),
                Padding = new Thickness(10),
                TextWrapping = TextWrapping.Wrap,
                Height = 120,
                IsReadOnly = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 0, 0, 20)
            };
            panel.Children.Add(originalTextBox);

            // Translated text
            panel.Children.Add(new TextBlock
            {
                Text = "Translation:",
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)),
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 5)
            });

            var translatedTextBox = new System.Windows.Controls.TextBox
            {
                Text = translatedText,
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 45)),
                Foreground = System.Windows.Media.Brushes.LightGreen,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 80)),
                Padding = new Thickness(10),
                TextWrapping = TextWrapping.Wrap,
                Height = 120,
                IsReadOnly = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 0, 0, 20)
            };
            panel.Children.Add(translatedTextBox);

            // Close button
            var closeBtn = new System.Windows.Controls.Button
            {
                Content = "Close",
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(124, 58, 237)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(20, 10, 20, 10),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
            };
            closeBtn.Click += (_, _) => Close();
            panel.Children.Add(closeBtn);

            Content = panel;
        }
    }
}