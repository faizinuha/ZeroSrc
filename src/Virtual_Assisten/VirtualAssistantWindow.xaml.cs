using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Web.WebView2.Core;
using System.Windows.Forms;
using System.Windows.Controls;
using Newtonsoft.Json.Linq;
using System.Drawing;
using System.Threading.Tasks;
using ZeroMix.Services;

namespace ZeroMix.Virtual_Assisten
{
    public partial class VirtualAssistantWindow : Window
    {
        private bool _isDragging = false;
        private System.Windows.Point _dragOffset;
        private DispatcherTimer? _hideChatTimer;
        private DispatcherTimer? _autoTalkTimer;
        private DispatcherTimer? _eyeTrackingTimer;
        private string _currentCharacter = "Frieren";
        private bool _isWebViewDisposed = false;
        private bool _isWebViewInitialized = false;
        private bool _isScriptRunning = false;
        private AiVisionService? _visionService;
        private DispatcherTimer? _visionTimer;
        private string _apiKey = ApiKeys.OPENAI_API_KEY; 
        private string _currentLang = "id-ID";
        
        // WaifuChatService untuk AI chat interaktif
        private WaifuChatService? _waifuChatService;
        private string _openRouterApiKey = ApiKeys.OPENROUTER_API_KEY;

        private readonly Dictionary<string, List<string>> _characterMessages = new()
        {
            ["Frieren"] = new List<string> { "Halo! Aku Frieren~ ✨" },
            ["Fern"] = new List<string> { "Halo, namaku Fern." },
            ["Huohuo"] = new List<string> { "M-maaf... aku Huohuo. 🦊" },
            ["Jian"] = new List<string> { "Haloo Kakak... Aku Jian. 🦊" }
        };
        
        private Random _random = new Random();

        public VirtualAssistantWindow()
        {
            InitializeComponent();
            
            var workArea = SystemParameters.WorkArea;
            this.Left = workArea.Right - this.Width - 20;
            this.Top = workArea.Bottom - this.Height - 80;
            
            this.Loaded += OnWindowLoaded;
            this.MouseLeftButtonDown += OnMouseLeftButtonDown;
            this.MouseLeftButtonUp += OnMouseLeftButtonUp;
            this.MouseMove += OnMouseMove;
            this.Deactivated += OnWindowDeactivated;
            this.Activated += OnWindowActivated;
            
            _hideChatTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _hideChatTimer.Tick += (s, e) => HideChatBubble();

            _autoTalkTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
            _autoTalkTimer.Tick += (s, e) => ShowNextChatMessage();
            // Jangan start dulu — tunggu WebView siap

            _eyeTrackingTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _eyeTrackingTimer.Tick += UpdateEyeTracking;
            _eyeTrackingTimer.Start();

            _visionService = new AiVisionService(_apiKey);
            // Vision timer — jangan start dulu, tunggu WebView siap
            _visionTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
            _visionTimer.Tick += async (s, e) => await PerformAiObservation();
            
            // Initialize WaifuChatService untuk AI chat
            try
            {
                _waifuChatService = new WaifuChatService(_openRouterApiKey, WaifuChatService.ModelType.GeminiFlashThinking);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VA] Failed to init WaifuChatService: {ex.Message}");
            }
        }

        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var userDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ZeroMix", "WebView2_VA");
                
                var options = new CoreWebView2EnvironmentOptions();
                // Minimal flags — avoid disabling GPU entirely as it breaks Live2D rendering
                options.AdditionalBrowserArguments = "--disable-features=AudioServiceOutOfProcess,MediaRouter --disable-background-timer-throttling --disable-extensions --disable-default-apps --js-flags=--max-old-space-size=128";
                
                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder, options);
                await WebView.EnsureCoreWebView2Async(env);
                
                WebView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = false;
                WebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                WebView.CoreWebView2.Settings.IsZoomControlEnabled = false;
                WebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                WebView.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
                WebView.CoreWebView2.Settings.IsPasswordAutosaveEnabled = false;
                WebView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
                WebView.CoreWebView2.Settings.IsSwipeNavigationEnabled = false;
                WebView.CoreWebView2.MemoryUsageTargetLevel = CoreWebView2MemoryUsageTargetLevel.Low;
                WebView.CoreWebView2.PermissionRequested += (s, args) => args.State = CoreWebView2PermissionState.Deny;

                string appBase = AppDomain.CurrentDomain.BaseDirectory;
                WebView.CoreWebView2.SetVirtualHostNameToFolderMapping("zeromix.vercel.app", appBase, CoreWebView2HostResourceAccessKind.Allow);
                
                WebView.WebMessageReceived += OnWebMessageReceived;

                // Cek file HTML ada dulu sebelum navigate
                string htmlLocalPath = Path.Combine(appBase, "Virtual_Assisten", "live2d-viewer.html");
                
                if (!File.Exists(htmlLocalPath))
                {
                    Console.WriteLine($"[VA] live2d-viewer.html not found at: {htmlLocalPath}");
                    return;
                }

                // Pakai virtual host — lebih cepat karena tidak perlu resolve file:/// path
                // Virtual host sudah di-map ke appBase folder
                WebView.Source = new Uri("https://zeromix.vercel.app/Virtual_Assisten/live2d-viewer.html");

                // Wait for navigation to complete before marking initialized
                var tcs = new TaskCompletionSource<bool>();
                void OnNavCompleted(object? s2, CoreWebView2NavigationCompletedEventArgs args2)
                {
                    WebView.CoreWebView2.NavigationCompleted -= OnNavCompleted;
                    tcs.TrySetResult(true);
                }
                WebView.CoreWebView2.NavigationCompleted += OnNavCompleted;
                await tcs.Task;

                _isWebViewInitialized = true;

                // Start timers hanya setelah WebView siap
                _visionTimer?.Start();
                _autoTalkTimer?.Start();

                // Now safe to send the model path
                await SendModelToWebView(_currentCharacter);
            }
            catch (Exception ex) { Console.WriteLine($"[VA] WebView init error: {ex.Message}"); }
        }

        private async Task SendModelToWebView(string characterName)
        {
            if (!_isWebViewInitialized || _isWebViewDisposed) return;
            
            string modelPath = GetModelPath(characterName);
            
            if (!File.Exists(modelPath))
            {
                Console.WriteLine($"[VA] Model not found: {modelPath}");
                return;
            }

            // Convert ke virtual host URL — konsisten dengan cara HTML di-load
            // Cari beberapa kandidat folder background (user mungkin memindahkan folder)
            string[] bgCandidates = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Virtual_Assisten", "Background", "img"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Virtual_Assisten", "Background"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Virtual_Assisten", "Va_Background"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Virtual_Assisten", "VA_Background"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Virtual_Assisten", "VA_Thumbnails"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Virtual_Assisten", "VA_Thumbnails")
            };
            string? bgDir = null;
            foreach (var cand in bgCandidates)
            {
                if (Directory.Exists(cand)) { bgDir = cand; break; }
            }
            if (!string.IsNullOrEmpty(bgDir))
            {
                Console.WriteLine($"[VA] Using background dir: {bgDir}");
                try
                {
                    foreach (var jf in Directory.GetFiles(bgDir, "*.jfif"))
                    {
                        var png = Path.ChangeExtension(jf, ".png");
                        if (!File.Exists(png))
                        {
                            try
                            {
                                using (var img = System.Drawing.Image.FromFile(jf))
                                {
                                    img.Save(png, System.Drawing.Imaging.ImageFormat.Png);
                                }
                            }
                            catch (Exception ex) { Console.WriteLine($"[VA] Background convert error: {ex.Message}"); }
                        }
                    }
                }
                catch (Exception ex) { Console.WriteLine($"[VA] Background scan error: {ex.Message}"); }
            }
            else
            {
                Console.WriteLine("[VA] No background folder found in candidates.");
            }

            string appBase = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');
            string relativePath = modelPath.Replace(appBase, "").TrimStart('\\', '/').Replace("\\", "/");
            string webPath = "https://zeromix.vercel.app/" + relativePath;
            
            Console.WriteLine($"[VA] Loading model: {webPath}");
            
            try
            {
                string escaped = Uri.EscapeUriString(webPath).Replace("'", "\\'");
                var sw = Stopwatch.StartNew();
                Console.WriteLine($"[VA] Sending model to WebView: {webPath}");
                long memBefore = GC.GetTotalMemory(false);
                long wsBefore = Process.GetCurrentProcess().WorkingSet64;
                Console.WriteLine($"[VA] Memory before send: GC={memBefore} bytes, WorkingSet={wsBefore} bytes");
                await WebView.ExecuteScriptAsync($"if(typeof changeModel === 'function') changeModel('{escaped}');");
                sw.Stop();
                long memAfter = GC.GetTotalMemory(false);
                long wsAfter = Process.GetCurrentProcess().WorkingSet64;
                Console.WriteLine($"[VA] Model send completed in {sw.ElapsedMilliseconds}ms. Memory after: GC={memAfter} bytes, WorkingSet={wsAfter} bytes");
                // Prompt .NET GC and working set trim after model change to free native resources
                App.OptimizeMemory();
            }
            catch (Exception ex) { Console.WriteLine($"[VA] SendModel error: {ex.Message}"); }
        }

        private async void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try 
            {
                string jsonMessage = e.WebMessageAsJson;
                JObject msg = JObject.Parse(jsonMessage);
                var type = msg["type"]?.ToString();
                if (type == "click")
                {
                    ShowNextChatMessage();
                }
                else if (type == "model_loaded")
                {
                    bool heavy = msg["heavy"]?.ToObject<bool>() ?? false;
                    long? loadMs = msg["loadTimeMs"]?.ToObject<long?>();
                    long? jsMem = msg["jsMemoryUsed"]?.ToObject<long?>();
                    Console.WriteLine($"[VA] WebView model_loaded: heavy={heavy}, loadTimeMs={loadMs}ms, jsMemory={jsMem}");
                    // Suggest GC/trim after model fully loaded in WebView
                    App.OptimizeMemory();
                    ShowNextChatMessage();
                }
                else if (type == "speech_result")
                {
                    JObject data = msg;
                    ProcessUserVoice(data["text"]?.ToString() ?? "");
                }
            } 
            catch (Exception ex) { Console.WriteLine($"[VA] OnWebMessageReceived parse error: {ex.Message}"); }
        }

        public async void SetCharacter(string characterName)
        {
            _currentCharacter = characterName;
            if (!_isWebViewInitialized || _isWebViewDisposed) return;

            // Stop eye tracking saat ganti model — cegah script conflict & freeze
            _eyeTrackingTimer?.Stop();

            await SendModelToWebView(characterName);

            // Resume setelah model dikirim ke WebView
            _eyeTrackingTimer?.Start();
        }

        private string GetModelPath(string characterName)
        {
            string baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Virtual_Assisten");
            // Gunakan Path.Combine agar konsisten di Windows (backslash)
            return characterName switch
            {
                "Fern"   => Path.Combine(baseDir, "Sou Sou No Frieren", "fern", "fern.model3.json"),
                "Huohuo" => Path.Combine(baseDir, "Mihoyo", "Honkai_Star_Rail", "huohuo", "huohuo.model3.json"),
                "Jian"   => Path.Combine(baseDir, "简__1_", "简", "简.model3.json"),
                "简"     => Path.Combine(baseDir, "简__1_", "简", "简.model3.json"),
                _        => Path.Combine(baseDir, "Sou Sou No Frieren", "Frieren", "Frieren.model3.json")
            };
        }

        private System.Windows.Point _lastMousePoint;
        private async void UpdateEyeTracking(object? sender, EventArgs e)
        {
            if (!_isWebViewInitialized || !this.IsVisible || _isScriptRunning) return;
            var point = System.Windows.Forms.Control.MousePosition;
            
            // Optimization: Only update if mouse moved enough (> 5 pixels)
            if (Math.Abs(point.X - _lastMousePoint.X) < 5 && Math.Abs(point.Y - _lastMousePoint.Y) < 5) return;
            _lastMousePoint = new System.Windows.Point(point.X, point.Y);

            double diffX = Math.Max(-1, Math.Min(1, (point.X - (this.Left + Width/2)) / 400.0));
            double diffY = Math.Max(-1, Math.Min(1, -(point.Y - (this.Top + Height/2 + 50)) / 400.0));
            _isScriptRunning = true;
            try { await WebView.ExecuteScriptAsync($"if(typeof updateEyeTracking === 'function') updateEyeTracking({diffX:F2}, {diffY:F2});"); }
            finally { _isScriptRunning = false; }
        }

        public void ShowNextChatMessage()
        {
            // Gunakan AI chat jika tersedia, fallback ke manual messages
            if (_waifuChatService != null)
            {
                _ = ShowAiChatMessage();
            }
            else
            {
                // Fallback: manual messages
                var msgs = _characterMessages.ContainsKey(_currentCharacter) ? _characterMessages[_currentCharacter] : _characterMessages["Frieren"];
                ChatText.Text = msgs[_random.Next(msgs.Count)];
                ChatBubble.Visibility = Visibility.Visible;
                _hideChatTimer?.Stop(); _hideChatTimer?.Start();
            }
        }
        
        private async Task ShowAiChatMessage()
        {
            if (_waifuChatService == null) return;
            
            try
            {
                // Get greeting dari WaifuChatService
                string greeting = _waifuChatService.GetGreeting(_currentCharacter);
                ChatText.Text = greeting;
                ChatBubble.Visibility = Visibility.Visible;
                _hideChatTimer?.Stop(); _hideChatTimer?.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VA] AI chat error: {ex.Message}");
                // Fallback ke manual message
                var msgs = _characterMessages[_currentCharacter];
                ChatText.Text = msgs[_random.Next(msgs.Count)];
                ChatBubble.Visibility = Visibility.Visible;
            }
        }
        
        // Method untuk user chat dengan AI (bisa dipanggil dari UI atau voice input)
        public async Task HandleUserChat(string userMessage)
        {
            if (_waifuChatService == null || string.IsNullOrWhiteSpace(userMessage)) return;
            
            try
            {
                // Show user message
                ChatText.Text = $"💬: {userMessage}";
                ChatBubble.Visibility = Visibility.Visible;
                
                // Get AI response
                string response = await _waifuChatService.ChatAsync(userMessage, _currentCharacter);
                
                // Show AI response
                ChatText.Text = response;
                ChatBubble.Visibility = Visibility.Visible;
                
                // Speak the response
                await WebView.ExecuteScriptAsync($"speakText('{response.Replace("'", "\\'")}', '{_currentLang}');");
                
                _hideChatTimer?.Stop(); _hideChatTimer?.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VA] User chat error: {ex.Message}");
                ChatText.Text = "Maaf, ada error...";
                ChatBubble.Visibility = Visibility.Visible;
            }
        }

        private async Task PerformAiObservation()
        {
            if (_visionService == null || _isDragging) return;
            string aiComment = await _visionService.AnalyzeAppsAsync(_visionService.GetActiveWindowTitle(), _currentCharacter);
            ChatText.Text = aiComment;
            ChatBubble.Visibility = Visibility.Visible;
            
            // Speak the observation!
            await WebView.ExecuteScriptAsync($"speakText('{aiComment.Replace("'", "\\'")}', '{_currentLang}');");

            _hideChatTimer?.Stop(); _hideChatTimer?.Start();
        }

        private void ShowNotification(string msg) { ChatText.Text = msg; ChatBubble.Visibility = Visibility.Visible; _hideChatTimer?.Stop(); _hideChatTimer?.Start(); }
        public void PreConfigure(string lang, bool enableMic)
        {
            _currentLang = lang;
        }

        private async void ProcessUserVoice(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            Console.WriteLine($"[VirtualAssistant] User Said: {text}");
            
            // Gunakan WaifuChatService jika tersedia (lebih cepat & lebih character-focused)
            if (_waifuChatService != null)
            {
                await HandleUserChat(text);
            }
            else if (_visionService != null)
            {
                // Fallback ke AiVisionService (lebih lambat tapi bisa vision)
                ChatText.Text = $"💬: {text}";
                ChatBubble.Visibility = Visibility.Visible;
                
                string aiResponse = await _visionService.AskAiAsync(text, _currentCharacter);
                Console.WriteLine($"[VirtualAssistant] AI Answer: {aiResponse}");

                ChatText.Text = aiResponse;
                ChatBubble.Visibility = Visibility.Visible;
                
                // Speak the response!
                await WebView.ExecuteScriptAsync($"speakText('{aiResponse.Replace("'", "\\'")}', '{_currentLang}');");

                _hideChatTimer?.Stop();
                _hideChatTimer?.Start();
            }
            
            App.OptimizeMemory();
        }

        private async void ManualVision_Click(object sender, RoutedEventArgs e)
        {
            await PerformAiObservation();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => this.Close();
        private void HideChatBubble() => ChatBubble.Visibility = Visibility.Collapsed;
        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) { _isDragging = true; _dragOffset = e.GetPosition(this); CaptureMouse(); }
        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) { _isDragging = false; ReleaseMouseCapture(); }
        private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e) { if (_isDragging) { var pos = e.GetPosition(this); this.Left += pos.X - _dragOffset.X; this.Top += pos.Y - _dragOffset.Y; } }
        protected override void OnClosed(EventArgs e) { _eyeTrackingTimer?.Stop(); _autoTalkTimer?.Stop(); _visionTimer?.Stop(); _hideChatTimer?.Stop(); _isWebViewDisposed = true; WebView?.Dispose(); base.OnClosed(e); App.OptimizeMemory(); }

        private async void OnWindowDeactivated(object sender, EventArgs e)
        {
            try
            {
                if (!_isWebViewDisposed && WebView?.CoreWebView2 != null)
                    await WebView.CoreWebView2.TrySuspendAsync();
            }
            catch { }
        }

        private void OnWindowActivated(object sender, EventArgs e)
        {
            try
            {
                if (!_isWebViewDisposed && WebView?.CoreWebView2 != null)
                    WebView.CoreWebView2.Resume();
            }
            catch { }
        }
    }
}
