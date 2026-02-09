using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using System.Diagnostics;
using System.Collections.Generic;

namespace ZeroMix.Virtual_Assisten
{
    public partial class VirtualAssistantWindow : Window
    {
        private bool _isDragging = false;
        private System.Windows.Point _dragOffset;
        private DispatcherTimer? _hideChatTimer;
        private DispatcherTimer? _autoTalkTimer;
        private DispatcherTimer? _eyeTrackingTimer;
        private bool _isModelLoaded = false;
        private string _currentCharacter = "Frieren";
        private readonly Dictionary<string, List<string>> _characterMessages = new()
        {
            ["Frieren"] = new List<string>
            {
                "Halo! Aku Frieren~ ✨",
                "Ada yang bisa aku bantu?",
                "Klik lagi dong~ 😊",
                "Aku suka sihir (magia)...",
                "Himmel pasti bangga padamu!",
                "Jangan lupa istirahat ya~",
                "Semangat! 💪",
                "Kamu hebat!",
                "Aku akan menemanimu~",
                "Mau dengar cerita?",
                "Stark sedang apa ya sekarang?"
            },
            ["Fern"] = new List<string>
            {
                "Halo, Tuan Frieren.",
                "Jangan malas-malasan ya.",
                "Apa ada yang bisa dibantu?",
                "Jangan lupa makan...",
                "Apa sih...",
                "E-ecchi...",
                "Aku akan menemani Tuan.",
                "Tolong lebih serius sedikit.",
                "Zoltraak!",
                "Stark bodoh..."
            },
            ["Huohuo"] = new List<string>
            {
                "Aaaah! Ada hantu?! 👻",
                "Maaf... aku Huohuo, dari Ten-Lords Commission.",
                "Tuan ekor... tolong jangan galak-galak.",
                "Aku sebenarnya penakut, tapi aku akan berusaha!",
                "Jangan tinggalkan aku sendirian ya...",
                "Bolehkah aku bersembunyi di belakangmu?",
                "Siapa itu?! Oh, ternyata cuma Kakak.",
                "Semangat kerjanya! Jangan sampai stres~",
                "Huohuo siap membantu (dengan gemetar)...",
                "Kenapa Kakak melihatku seperti itu? 🥺"
            }
        };
        private int _messageIndex = 0;
        private Random _random = new Random();

        public VirtualAssistantWindow()
        {
            InitializeComponent();
            
            // Position at bottom right
            var workArea = SystemParameters.WorkArea;
            this.Left = workArea.Right - this.Width - 20;
            this.Top = workArea.Bottom - this.Height - 20;
            
            this.Loaded += OnWindowLoaded;
            this.MouseLeftButtonDown += OnMouseLeftButtonDown;
            this.MouseLeftButtonUp += OnMouseLeftButtonUp;
            this.MouseMove += OnMouseMove;
            
            // Timer to hide chat bubble
            _hideChatTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(4)
            };
            _hideChatTimer.Tick += (s, e) =>
            {
                _hideChatTimer.Stop();
                HideChatBubble();
            };

            // Timer for automatic talking
            _autoTalkTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(_random.Next(20, 40))
            };
            _autoTalkTimer.Tick += (s, e) =>
            {
                ShowNextChatMessage();
                // Randomize next interval
                _autoTalkTimer.Interval = TimeSpan.FromSeconds(_random.Next(30, 60));
            };
            _autoTalkTimer.Start();

            // Timer for global eye tracking
            _eyeTrackingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            _eyeTrackingTimer.Tick += (s, e) => UpdateGlobalEyeTracking();
            _eyeTrackingTimer.Start();
        }

        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Initialize WebView2
                var env = await CoreWebView2Environment.CreateAsync();
                await Live2DView.EnsureCoreWebView2Async(env);
                
                // SECURE: Use WebResourceRequested instead of mapping to physical folder
                Live2DView.CoreWebView2.AddWebResourceRequestedFilter("https://live2d.local/*", CoreWebView2WebResourceContext.All);
                Live2DView.CoreWebView2.WebResourceRequested += OnWebResourceRequested;
                
                // Settings for transparency and performance
                Live2DView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                Live2DView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                Live2DView.CoreWebView2.Settings.IsZoomControlEnabled = false;
                
                // Handle messages from JavaScript
                Live2DView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
                
                // Load Live2D viewer HTML via virtual host URL
                Live2DView.CoreWebView2.Navigate("https://live2d.local/live2d-viewer.html");
                _isModelLoaded = true;
                
                Debug.WriteLine("[VirtualAssistant] Secure Resource Server initialized (Embedded Mode)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VirtualAssistant] Error: {ex.Message}");
            }
        }

        private void OnWebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            try
            {
                string uri = e.Request.Uri;
                if (!uri.StartsWith("https://live2d.local/")) return;

                // Map URL path to Embedded Resource name
                // Format: ZeroMix.Virtual_Assisten.Path.To.File
                string relativePath = uri.Replace("https://live2d.local/", "").Replace("/", ".");
                
                // Fix for spaces or special characters in folder names
                relativePath = relativePath.Replace(" ", "_");
                
                string resourceName = $"ZeroMix.Virtual_Assisten.{relativePath}";
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var resourceStream = assembly.GetManifestResourceStream(resourceName);

                if (resourceStream != null)
                {
                    string contentType = GetMimeType(uri);
                    e.Response = Live2DView.CoreWebView2.Environment.CreateWebResourceResponse(
                        resourceStream, 200, "OK", $"Content-Type: {contentType}");
                }
                else
                {
                    Debug.WriteLine($"[VirtualAssistant] Resource not found: {resourceName}");
                    e.Response = Live2DView.CoreWebView2.Environment.CreateWebResourceResponse(
                        null, 404, "Not Found", "");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VirtualAssistant] Serving error: {ex.Message}");
            }
        }

        private string GetMimeType(string uri)
        {
            string ext = Path.GetExtension(uri).ToLower();
            return ext switch
            {
                ".html" => "text/html",
                ".js" => "application/javascript",
                ".css" => "text/css",
                ".json" => "application/json",
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".moc3" => "application/octet-stream",
                ".model3" => "application/json",
                _ => "application/octet-stream"
            };
        }

        private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string json = e.WebMessageAsJson;
                var message = Newtonsoft.Json.Linq.JObject.Parse(json);
                string type = message["type"]?.ToString() ?? "";
                
                if (type == "click")
                {
                    Dispatcher.Invoke(() => ShowNextChatMessage());
                }
                else if (type == "model_loaded")
                {
                    _isModelLoaded = true;
                }
                else if (type == "drag")
                {
                    double dx = (double)(message["deltaX"] ?? 0);
                    double dy = (double)(message["deltaY"] ?? 0);
                    Dispatcher.Invoke(() =>
                    {
                        this.Left += dx;
                        this.Top += dy;
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VirtualAssistant] Message error: {ex.Message}");
            }
        }

        private void ShowNextChatMessage()
        {
            var messages = _characterMessages.ContainsKey(_currentCharacter) 
                ? _characterMessages[_currentCharacter] 
                : _characterMessages["Frieren"];
                
            // Shuffle through messages
            _messageIndex = _random.Next(messages.Count);
            ChatText.Text = messages[_messageIndex];
            
            // Show bubble with animation
            ChatBubble.Visibility = Visibility.Visible;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            ChatBubble.BeginAnimation(OpacityProperty, fadeIn);
            
            // Reset timers
            _hideChatTimer?.Stop();
            _hideChatTimer?.Start();
            
            // Push auto-talk further back so it doesn't interrupt manual click
            _autoTalkTimer?.Stop();
            _autoTalkTimer?.Start();
        }

        private void HideChatBubble()
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
            fadeOut.Completed += (s, e) => ChatBubble.Visibility = Visibility.Collapsed;
            ChatBubble.BeginAnimation(OpacityProperty, fadeOut);
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Check if clicking on the character area (not the chat bubble)
            var pos = e.GetPosition(this);
            if (pos.Y > 60) // Below chat bubble
            {
                _isDragging = true;
                _dragOffset = pos;
                this.CaptureMouse();
            }
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            this.ReleaseMouseCapture();
        }

        private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isDragging)
            {
                var pos = e.GetPosition(this);
                this.Left += pos.X - _dragOffset.X;
                this.Top += pos.Y - _dragOffset.Y;
            }
            
            // Send mouse position to Live2D for eye tracking
            UpdateEyeTracking(e);
        }

        private async void UpdateEyeTracking(System.Windows.Input.MouseEventArgs e)
        {
            if (Live2DView.CoreWebView2 == null || !_isModelLoaded) return;
            
            try
            {
                // Get mouse position relative to window center
                var pos = e.GetPosition(this);
                
                // Adjust for the margin top (60) and offset
                double centerX = this.Width / 2;
                double centerY = this.Height / 2 + 100; // Match JS offset
                
                double eyeX = (pos.X - centerX) / (this.Width / 2);
                double eyeY = (pos.Y - centerY) / (this.Height / 2);
                
                // Clamp values
                eyeX = Math.Max(-1.0, Math.Min(1.0, eyeX));
                eyeY = Math.Max(-1.0, Math.Min(1.0, eyeY));
                
                await Live2DView.CoreWebView2.ExecuteScriptAsync($"updateEyeTracking({eyeX.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {eyeY.ToString(System.Globalization.CultureInfo.InvariantCulture)})");
            }
            catch { }
        }

        private async void UpdateGlobalEyeTracking()
        {
            if (Live2DView.CoreWebView2 == null || !_isModelLoaded || _isDragging) return;

            try
            {
                // Native screen coordinates
                var mousePos = GetMousePosition();
                
                // Calculate center of the window
                double centerX = this.Left + (this.Width / 2);
                double centerY = this.Top + (this.Height / 2) + 100; // Offset for head position

                double eyeX = (mousePos.X - centerX) / 500.0;
                double eyeY = (centerY - mousePos.Y) / 500.0; // Dibalik: Mouse di atas (Y kecil) jadi nilai positif (Atas)

                // Clamp
                eyeX = Math.Max(-1.0, Math.Min(1.0, eyeX));
                eyeY = Math.Max(-1.0, Math.Min(1.0, eyeY));

                await Live2DView.CoreWebView2.ExecuteScriptAsync($"updateEyeTracking({eyeX.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {eyeY.ToString(System.Globalization.CultureInfo.InvariantCulture)})");
            }
            catch { }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        private System.Windows.Point GetMousePosition()
        {
            POINT lpPoint;
            GetCursorPos(out lpPoint);
            return new System.Windows.Point(lpPoint.X, lpPoint.Y);
        }

        private void CreateLive2DViewerHtml()
        {
            string htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                "Virtual_Assisten", "live2d-viewer.html");
            
            string dir = Path.GetDirectoryName(htmlPath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            
            // The HTML will be created separately
        }

        public async void SetCharacter(string characterName)
        {
            _isModelLoaded = false;
            _currentCharacter = characterName;
            
            if (Live2DView.CoreWebView2 == null) return;
            
            string modelSubPath = characterName switch
            {
                "Fern" => "Sou Sou No Frieren/fern/fern.model3.json",
                "Huohuo" => "Mihoyo/Honkai_Star_Rail/huohuo2/huohuo/huohuo.model3.json",
                _ => "Sou Sou No Frieren/Frieren/Frieren.model3.json"
            };
                
            await Live2DView.CoreWebView2.ExecuteScriptAsync($"changeModel('{modelSubPath}')");
            ShowNextChatMessage();
        }

        public void ShowMessage(string message)
        {
            ChatText.Text = message;
            ChatBubble.Visibility = Visibility.Visible;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            ChatBubble.BeginAnimation(OpacityProperty, fadeIn);
            
            _hideChatTimer?.Stop();
            _hideChatTimer?.Start();
        }

        protected override void OnClosed(EventArgs e)
        {
            _hideChatTimer?.Stop();
            _autoTalkTimer?.Stop();
            base.OnClosed(e);
        }
    }
}
