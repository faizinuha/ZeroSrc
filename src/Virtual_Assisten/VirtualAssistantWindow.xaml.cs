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
using System.Threading.Tasks;

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

        private readonly Dictionary<string, List<string>> _characterMessages = new()
        {
            ["Frieren"] = new List<string> { "Halo! Aku Frieren~ ✨" },
            ["Fern"] = new List<string> { "Halo, namaku Fern." },
            ["Huohuo"] = new List<string> { "M-maaf... aku Huohuo. 🦊" }
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

                // Navigate to the viewer — model will be sent after page signals ready via model_loaded
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
            string appBase = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');
            string webPath = "https://zeromix.vercel.app/" + modelPath.Replace(appBase, "").TrimStart('\\', '/').Replace("\\", "/");
            try
            {
                await WebView.ExecuteScriptAsync($"if(typeof changeModel === 'function') changeModel('{Uri.EscapeUriString(webPath)}');");
            }
            catch (Exception ex) { Console.WriteLine($"[VA] SendModel error: {ex.Message}"); }
        }

        private async void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try 
            {
                string jsonMessage = e.WebMessageAsJson;
                if (jsonMessage.Contains("\"type\":\"click\"")) ShowNextChatMessage();
                else if (jsonMessage.Contains("\"type\":\"model_loaded\"")) ShowNextChatMessage();
                else if (jsonMessage.Contains("\"type\":\"speech_result\""))
                {
                    JObject data = JObject.Parse(jsonMessage);
                    ProcessUserVoice(data["text"]?.ToString() ?? "");
                }
            } 
            catch { }
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
            return characterName switch
            {
                "Fern" => Path.Combine(baseDir, "Sou Sou No Frieren/fern/fern.model3.json"),
                "Huohuo" => Path.Combine(baseDir, "Mihoyo/Honkai_Star_Rail/huohuo/huohuo.model3.json"),
                _ => Path.Combine(baseDir, "Sou Sou No Frieren/Frieren/Frieren.model3.json")
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
            var msgs = _characterMessages.ContainsKey(_currentCharacter) ? _characterMessages[_currentCharacter] : _characterMessages["Frieren"];
            ChatText.Text = msgs[_random.Next(msgs.Count)];
            ChatBubble.Visibility = Visibility.Visible;
            _hideChatTimer?.Stop(); _hideChatTimer?.Start();
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
            
            ChatText.Text = $"💬: {text}";
            ChatBubble.Visibility = Visibility.Visible;
            
            string aiResponse = await _visionService!.AskAiAsync(text, _currentCharacter);
            Console.WriteLine($"[VirtualAssistant] AI Answer: {aiResponse}");

            ChatText.Text = aiResponse;
            ChatBubble.Visibility = Visibility.Visible;
            
            // Speak the response!
            await WebView.ExecuteScriptAsync($"speakText('{aiResponse.Replace("'", "\\'")}', '{_currentLang}');");

            _hideChatTimer?.Stop();
            _hideChatTimer?.Start();
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
