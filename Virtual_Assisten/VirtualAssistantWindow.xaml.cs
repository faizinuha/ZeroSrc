using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Web.WebView2.Core;
using System.Windows.Forms;

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
        private bool _isWebViewInitialized = false;
        private bool _isScriptRunning = false;
        private AiVisionService? _visionService;
        private DispatcherTimer? _visionTimer;
        private string _apiKey = ApiKeys.OPENAI_API_KEY; 


        private readonly Dictionary<string, List<string>> _characterMessages = new()
        {
            ["Frieren"] = new List<string> { "Halo! Aku Frieren~ ✨", "Apa ada yang bisa aku bantu?", "Himmel pasti bangga padamu!", "Zoltraak!", "Hmm... bau buku sihir baru." },
            ["Fern"] = new List<string> { "Halo, Tuan Frieren.", "Jangan malas-malasan ya.", "Zoltraak!", "Tuan Stark memang merepotkan.", "Kecil sekali..." },
            ["Huohuo"] = new List<string> { "Aaaah! Ada hantu?! 👻", "Maaf... aku Huohuo.", "Tuan ekor... tolong!", "Jangan takut, ada aku (walaupun aku takut juga).", "Waaah! 🦊" }
        };
        
        private int _messageIndex = 0;
        private Random _random = new Random();

        public VirtualAssistantWindow()
        {
            InitializeComponent();
            
            // Position: Bottom Right
            var workArea = SystemParameters.WorkArea;
            this.Left = workArea.Right - this.Width - 20;
            this.Top = workArea.Bottom - this.Height - 20;
            
            this.Loaded += OnWindowLoaded;
            this.MouseLeftButtonDown += OnMouseLeftButtonDown;
            this.MouseLeftButtonUp += OnMouseLeftButtonUp;
            this.MouseMove += OnMouseMove;
            
            _hideChatTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _hideChatTimer.Tick += (s, e) => HideChatBubble();

            _autoTalkTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(45) };
            _autoTalkTimer.Tick += (s, e) => ShowNextChatMessage();
            _autoTalkTimer.Start();

            // Eye Tracking Timer
            _eyeTrackingTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _eyeTrackingTimer.Tick += UpdateEyeTracking;
            _eyeTrackingTimer.Start();

            // AI Vision Setup (Auto Observation)
            _visionService = new AiVisionService(_apiKey);

            _visionTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) }; // Setiap 30 detik dia "melihat"
            _visionTimer.Tick += async (s, e) => await PerformAiObservation();
            _visionTimer.Start();
        }

        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Debug.WriteLine("[VirtualAssistant] Initializing WebView2 (Memory Optimized Version)...");
                
                // Memory Optimization Arguments
                // --disable-features=LayoutService... reduces overhead
                // --disable-gpu-shader-disk-cache prevents disk I/O lag
                string extraArgs = "--disable-features=LayoutService,PrivacySandboxSettings4 " +
                                  "--disable-extensions " +
                                  "--mute-audio --no-proxy-server --disable-notifications";

                var userDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ZeroMix", "WebView2_VA");
                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder, new CoreWebView2EnvironmentOptions(extraArgs));
                
                await WebView.EnsureCoreWebView2Async(env);

                // EXTREME RAM OPTIMIZATION: Set memory target to low
                WebView.CoreWebView2.MemoryUsageTargetLevel = CoreWebView2MemoryUsageTargetLevel.Low;
                
                // WebView UI Tweaks
                WebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                WebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                WebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                WebView.CoreWebView2.Settings.IsZoomControlEnabled = false;
                WebView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
                
                // Map Virtual Folder to the ROOT directory (parent of Virtual_Assisten)
                // This allows URL like: https://zeromix.vercel.app/Virtual_Assisten/live2d-viewer.html
                string baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Virtual_Assisten");
                string? rootDir = Path.GetDirectoryName(baseDir); // Get the folder containing 'Virtual_Assisten'
                
                if (rootDir != null)
                {
                    WebView.CoreWebView2.SetVirtualHostNameToFolderMapping("zeromix.vercel.app", rootDir, CoreWebView2HostResourceAccessKind.Allow);
                }
                
                // Logging: Navigation State
                WebView.CoreWebView2.NavigationStarting += (s, args) => Console.WriteLine($"[WebView] Navigating to: {args.Uri}");
                WebView.CoreWebView2.NavigationCompleted += (s, args) => {
                    if (args.IsSuccess) 
                    {
                        Console.WriteLine("[WebView] Navigation Successful ✅");
                        // Load character ONLY after page is ready
                        SetCharacter(_currentCharacter);
                    }
                    else 
                    {
                        Console.WriteLine($"[WebView] Navigation Failed ❌ (Status: {args.WebErrorStatus})");
                    }
                };

                // Now use the full path as requested
                WebView.Source = new Uri("https://zeromix.vercel.app/Virtual_Assisten/live2d-viewer.html");
                WebView.WebMessageReceived += OnWebMessageReceived;
                
                _isWebViewInitialized = true;
                Console.WriteLine("[VirtualAssistant] WebView2 initialized with Root Directory mapping.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VirtualAssistant] WebView Init Error: {ex.Message}");
            }
        }

        private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try 
            {
                // CoreWebView2 returns JSON string for objects sent via postMessage
                string jsonMessage = e.WebMessageAsJson;
                
                if (jsonMessage.Contains("\"type\":\"log\""))
                {
                    // For logs, we just dump the JSON for now to see everything
                    Console.WriteLine($"[WebView-JS] {jsonMessage}");
                }
                else if (jsonMessage.Contains("\"type\":\"click\""))
                {
                    ShowNextChatMessage();
                }
                else if (jsonMessage.Contains("\"type\":\"model_loaded\""))
                {
                    Console.WriteLine($"[VirtualAssistant] Model Loaded by WebView");
                    App.OptimizeMemory();
                }
            } 
            catch (Exception ex)
            {
                Console.WriteLine($"[VirtualAssistant] Message Processing Error: {ex.Message}");
            }
        }

        public async void SetCharacter(string characterName)
        {
            _currentCharacter = characterName;
            
            if (!_isWebViewInitialized) return;

            string modelPath = GetModelPath(characterName);
            // Convert to Virtual Host Path (Domain points to ROOT)
            // Use Uri.EscapeUriString to handle spaces (e.g. "Sou Sou No Frieren")
            string webPath = modelPath.Replace(AppDomain.CurrentDomain.BaseDirectory, "https://zeromix.vercel.app/").Replace("\\", "/");
            webPath = Uri.EscapeUriString(webPath);
            
            Console.WriteLine($"[VirtualAssistant] Switching character to: {characterName} ({webPath})");
            
            await WebView.ExecuteScriptAsync($"if(typeof changeModel === 'function') changeModel('{webPath}');");
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

        private DateTime _lastMemoryCleanupTime = DateTime.MinValue;

        private async void UpdateEyeTracking(object? sender, EventArgs e)
        {
            if (!_isWebViewInitialized || !this.IsVisible) return;

            // Get Global Mouse Position
            var mousePos = GetMousePosition();
            
            // Window Center (Target for eye tracking 0,0)
            double centerX = this.Left + (this.Width / 2);
            double centerY = this.Top + (this.Height / 2) + 50; // Offset down as character is lower

            // Calculate Angle/Distance normalized (-1 to 1)
            double diffX = (mousePos.X - centerX) / 400.0;
            double diffY = (mousePos.Y - centerY) / 400.0;

            // Clamp values
            diffX = Math.Max(-1, Math.Min(1, diffX));
            diffY = Math.Max(-1, Math.Min(1, -diffY)); // Invert Y for correct JS logic Up is Positive

            // Send to WebView (Debounced/Skip if busy)
            if (!_isScriptRunning)
            {
                _isScriptRunning = true;
                try
                {
                    await WebView.ExecuteScriptAsync($"if(typeof updateEyeTracking === 'function') updateEyeTracking({diffX:F2}, {diffY:F2});");
                }
                finally
                {
                    _isScriptRunning = false;
                }
            }
            
            // Memory Sweep every 30 seconds, only ONCE per cycle
            if ((DateTime.Now - _lastMemoryCleanupTime).TotalSeconds > 30) 
            {
                 _lastMemoryCleanupTime = DateTime.Now;
                 App.OptimizeMemory();
            }
        }

        private System.Windows.Point GetMousePosition()
        {
            // Reliable way to get screen mouse position without dependencies
            var point = System.Windows.Forms.Control.MousePosition;
            return new System.Windows.Point(point.X, point.Y);
        }

        public void ShowNextChatMessage()
        {
            var messages = _characterMessages.ContainsKey(_currentCharacter) ? _characterMessages[_currentCharacter] : _characterMessages["Frieren"];
            _messageIndex = _random.Next(messages.Count);
            ChatText.Text = messages[_messageIndex];
            ChatBubble.Visibility = Visibility.Visible;
            _hideChatTimer?.Stop();
            _hideChatTimer?.Start();
        }

        private async Task PerformAiObservation()
        {
            if (_visionService == null) return;

            // Jangan ganggu kalau lagi dragging
            if (_isDragging) return;

            string windowTitle = _visionService.GetActiveWindowTitle();
            string aiComment = await _visionService.AnalyzeAppsAsync(windowTitle, _currentCharacter);
            
            // Tampilkan komentar AI di bubble
            ChatText.Text = aiComment;
            ChatBubble.Visibility = Visibility.Visible;
            
            _hideChatTimer?.Stop();
            _hideChatTimer?.Start();
            
            Console.WriteLine($"[VirtualAssistant] AI Observation: {aiComment}");
            App.OptimizeMemory(); // Bersihkan RAM setelah mikir
        }

        private async void ManualVision_Click(object sender, RoutedEventArgs e)
        {
            await PerformAiObservation();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void HideChatBubble()
        {
            ChatBubble.Visibility = Visibility.Collapsed;
            _hideChatTimer?.Stop();
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Drag area: anything not the ChatBubble
            _isDragging = true;
            _dragOffset = e.GetPosition(this);
            this.CaptureMouse();
            
            // Also notify JS to maybe stop eye tracking jitter
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
        }

        protected override void OnClosed(EventArgs e)
        {
            _eyeTrackingTimer?.Stop();
            _autoTalkTimer?.Stop();
            _visionTimer?.Stop();
            _hideChatTimer?.Stop();
            
            // Properly dispose WebView
            WebView?.Dispose();
            
            base.OnClosed(e);
            App.OptimizeMemory();
        }
    }
}
