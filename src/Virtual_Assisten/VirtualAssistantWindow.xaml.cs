using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows.Media.Animation;
using ZeroMix.Rendering;
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
        private DispatcherTimer? _fadeInCheckTimer;
        private string _currentCharacter = "Frieren";
        private bool _isHostDisposed = false;
        private AiVisionService? _visionService;
        private DispatcherTimer? _visionTimer;
        private string _apiKey = ApiKeys.OPENAI_API_KEY; 
        private string _currentLang = "id-ID";
        // null = auto-detect (CPU cores, sama seperti JS lama), "1" = force on, "0" = force off
        private string? _antialiasOverride = null;
        
        // AI Chat Service (OpenRouter via WaifuChatService)
        private WaifuChatService? _waifuChatService;
        private string _openRouterApiKey = ApiKeys.OPENROUTER_API_KEY;
        
        // Chat history untuk context
        private readonly List<(string Role, string Message)> _chatHistory = new();
        private const int MaxChatHistory = 10;

        private readonly Dictionary<string, List<string>> _characterMessages = new()
        {
            ["Frieren"] = new List<string> { "Halo! Aku Frieren~ ✨" },
            ["Fern"] = new List<string> { "Halo, namaku Fern." },
            ["Huohuo"] = new List<string> { "M-maaf... aku Huohuo. 🦊" },
            ["Jian"] = new List<string> { "Haloo Kakak... Aku Jian. 🦊" }
        };
        
        private Random _random = new Random();

        // ── TTS (pengganti Web Speech API — System.Speech.Synthesis) ──────
        private System.Speech.Synthesis.SpeechSynthesizer? _synthesizer;

        // ── STT (pengganti Web Speech API recognition — System.Speech.Recognition) ──
        private System.Speech.Recognition.SpeechRecognitionEngine? _recognizer;
        private bool _isListening;

        public VirtualAssistantWindow()
        {
            InitializeComponent();
            
            var workArea = SystemParameters.WorkArea;
            this.Left = workArea.Right - this.Width - 20;
            this.Top = workArea.Bottom - this.Height - 80;
            
            // ★ 5.5 — cegah flash warna solid: window tetap tersembunyi
            // sampai frame OpenGL pertama sukses ter-render.
            this.Opacity = 0;

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
            // Jangan start dulu — tunggu model siap

            _eyeTrackingTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _eyeTrackingTimer.Tick += UpdateEyeTracking;
            _eyeTrackingTimer.Start();

            _visionService = new AiVisionService(_apiKey);
            // Vision timer — jangan start dulu, tunggu model siap
            _visionTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
            _visionTimer.Tick += async (s, e) => await PerformAiObservation();
            
            // Initialize WaifuChatService untuk AI chat (OpenRouter)
            try
            {
                _waifuChatService = new WaifuChatService(_openRouterApiKey, WaifuChatService.ModelType.GeminiFlashThinking);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VA] Failed to init WaifuChatService: {ex.Message}");
            }

            // TTS native (System.Speech) — pengganti speechSynthesis WebView2
            try
            {
                _synthesizer = new System.Speech.Synthesis.SpeechSynthesizer();
                _synthesizer.SpeakCompleted += (s, e) =>
                {
                    if (GlHost != null) GlHost.IsSpeaking = false;
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VA] TTS init error: {ex.Message}");
            }

            // STT native (System.Speech) — pengganti webkitSpeechRecognition
            InitSpeechRecognition();
        }

        private void InitSpeechRecognition()
        {
            try
            {
                var cultures = new[]
                {
                    System.Globalization.CultureInfo.GetCultureInfo("id-ID"),
                    System.Globalization.CultureInfo.GetCultureInfo("en-US")
                };

                _recognizer = null;
                foreach (var c in cultures)
                {
                    try
                    {
                        _recognizer = new System.Speech.Recognition.SpeechRecognitionEngine(c);
                        break;
                    }
                    catch { }
                }

                if (_recognizer == null)
                {
                    Console.WriteLine("[VA] STT: tidak ada recognizer yang tersedia (perlu language pack).");
                    return;
                }

                _recognizer.LoadGrammar(new System.Speech.Recognition.DictationGrammar());
                _recognizer.SetInputToDefaultAudioDevice();
                _recognizer.SpeechRecognized += (s, e) =>
                {
                    _isListening = false;
                    string text = e.Result?.Text ?? "";
                    Dispatcher.BeginInvoke(new Action(() => ProcessUserVoice(text)));
                };
                _recognizer.SpeechRecognitionRejected += (s, e) =>
                {
                    _isListening = false;
                    Console.WriteLine("[VA] STT: tidak mendengar dengan jelas.");
                };
                _recognizer.RecognizeCompleted += (s, e) =>
                {
                    _isListening = false;
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VA] STT init error: {ex.Message}");
            }
        }

        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Antialias: override dari Settings kalau ada, selain itu auto-detect (CPU cores)
                if (_antialiasOverride != null)
                    GlHost.Antialias = _antialiasOverride == "1";
                else
                    GlHost.Antialias = Environment.ProcessorCount > 4;

                // Drag window via child GL surface (fitur yang tidak ada di versi WebView2)
                GlHost.DragDelta += (dx, dy) =>
                {
                    this.Left += dx;
                    this.Top += dy;
                };

                GlHost.Clicked += OnGlHostClicked;
                GlHost.ModelLoaded += () =>
                {
                    Console.WriteLine("[VA] Model loaded & frame pertama siap.");
                    App.OptimizeMemory();
                };

                await LoadModelToHost(_currentCharacter);

                // ★ 5.5 — mulai timers & fade-in hanya setelah frame pertama benar-benar ada
                _visionTimer?.Start();
                _autoTalkTimer?.Start();

                _fadeInCheckTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
                _fadeInCheckTimer.Tick += (s2, e2) =>
                {
                    if (GlHost.FirstFrameRendered)
                    {
                        _fadeInCheckTimer?.Stop();
                        FadeIn();
                    }
                };
                _fadeInCheckTimer.Start();

                ShowNextChatMessage();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VA] Init error: {ex.Message}");
                // Kalau gagal total, tetap tampilkan window biar user bisa lihat pesan error
                FadeIn();
            }
        }

        /// <summary>Fade-in window dari opacity 0 (dipakai setelah frame pertama sukses).</summary>
        private void FadeIn()
        {
            var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            this.BeginAnimation(OpacityProperty, anim);
        }

        private void OnGlHostClicked(System.Windows.Point hostPoint)
        {
            // Klik di area chat panel → fokuskan input chat (bukan reaksi karakter)
            if (ChatPanel.Visibility == Visibility.Visible && IsPointInChatPanel(hostPoint))
            {
                ChatInput.Focus();
                return;
            }

            GlHost.PlayTapMotion();
            ShowNextChatMessage();
        }

        private bool IsPointInChatPanel(System.Windows.Point hostPoint)
        {
            try
            {
                var origin = ChatPanel.TransformToAncestor(this).Transform(new System.Windows.Point(0, 0));
                var size = new System.Windows.Size(ChatPanel.ActualWidth, ChatPanel.ActualHeight);
                return hostPoint.X >= origin.X && hostPoint.X <= origin.X + size.Width
                    && hostPoint.Y >= origin.Y && hostPoint.Y <= origin.Y + size.Height;
            }
            catch { return false; }
        }

        private async Task LoadModelToHost(string characterName)
        {
            string modelPath = GetModelPath(characterName);
            if (!File.Exists(modelPath))
            {
                Console.WriteLine($"[VA] Model not found: {modelPath}");
                ShowNotification($"Model {characterName} tidak ditemukan di:\n{modelPath}");
                return;
            }

            Console.WriteLine($"[VA] Model file exists: {modelPath}");
            Console.WriteLine($"[VA] Loading model: {modelPath}");

            var sw = Stopwatch.StartNew();
            long wsBefore = Process.GetCurrentProcess().WorkingSet64;
            long gcBefore = GC.GetTotalMemory(false);
            Console.WriteLine($"[VA] Memory before load: GC={gcBefore} bytes, WorkingSet={wsBefore} bytes");

            await Dispatcher.InvokeAsync(() => GlHost.LoadModel(modelPath));

            sw.Stop();
            long wsAfter = Process.GetCurrentProcess().WorkingSet64;
            long gcAfter = GC.GetTotalMemory(false);
            Console.WriteLine($"[VA] Model load completed in {sw.ElapsedMilliseconds}ms. Memory after: GC={gcAfter} bytes, WorkingSet={wsAfter} bytes");

            App.OptimizeMemory();
        }

        public async void SetCharacter(string characterName)
        {
            _currentCharacter = characterName;
            if (_isHostDisposed) return;

            // Stop eye tracking saat ganti model — cegah conflict & freeze
            _eyeTrackingTimer?.Stop();

            await LoadModelToHost(characterName);

            // Resume setelah model di-load ke host
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
        private void UpdateEyeTracking(object? sender, EventArgs e)
        {
            if (!this.IsVisible || _isHostDisposed || !GlHost.IsModelLoaded) return;
            var point = System.Windows.Forms.Control.MousePosition;
            
            // Optimization: Only update if mouse moved enough (> 5 pixels)
            if (Math.Abs(point.X - _lastMousePoint.X) < 5 && Math.Abs(point.Y - _lastMousePoint.Y) < 5) return;
            _lastMousePoint = new System.Windows.Point(point.X, point.Y);

            double diffX = Math.Max(-1, Math.Min(1, (point.X - (this.Left + Width/2)) / 400.0));
            double diffY = Math.Max(-1, Math.Min(1, -(point.Y - (this.Top + Height/2 + 50)) / 400.0));
            GlHost.SetEyeTarget((float)diffX, (float)diffY);
        }

        public void ShowNextChatMessage()
        {
            // Gunakan AI chat jika tersedia, fallback ke manual messages
            if (_waifuChatService != null)
            {
                ShowAiChatMessage();
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
        
        private void ShowAiChatMessage()
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
            if (string.IsNullOrWhiteSpace(userMessage)) return;
            
            try
            {
                // Show user message
                ChatText.Text = $"💬 {userMessage}";
                ChatBubble.Visibility = Visibility.Visible;
                
                // Simpan ke history
                _chatHistory.Add(("user", userMessage));
                if (_chatHistory.Count > MaxChatHistory)
                    _chatHistory.RemoveAt(0);
                
                string response;
                
                if (_waifuChatService != null)
                {
                    response = await _waifuChatService.ChatAsync(userMessage, _currentCharacter, _chatHistory);
                }
                else
                {
                    response = "Service AI belum tersedia. Cek API key di Settings.";
                }
                
                // Simpan response ke history
                _chatHistory.Add(("assistant", response));
                if (_chatHistory.Count > MaxChatHistory)
                    _chatHistory.RemoveAt(0);
                
                // Show AI response
                ChatText.Text = response;
                ChatBubble.Visibility = Visibility.Visible;
                
                // Speak the response (TTS native + lip-sync)
                SpeakText(response, _currentLang);
                
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
            SpeakText(aiComment, _currentLang);

            _hideChatTimer?.Stop(); _hideChatTimer?.Start();
        }

        private void ShowNotification(string msg) { ChatText.Text = msg; ChatBubble.Visibility = Visibility.Visible; _hideChatTimer?.Stop(); _hideChatTimer?.Start(); }
        public void PreConfigure(string lang, bool enableMic)
        {
            _currentLang = lang;
        }

        // ── TTS native (pengganti speakText di JS) ────────────────────────
        private void SpeakText(string text, string lang = "id-ID")
        {
            try
            {
                if (_synthesizer == null || string.IsNullOrWhiteSpace(text)) return;

                _synthesizer.SpeakAsyncCancelAll();

                // Voice mapping per karakter (mirip JS lama)
                var (langPref, rate) = GetVoiceSettings(_currentCharacter);
                System.Speech.Synthesis.InstalledVoice? voice = null;

                try
                {
                    foreach (var v in _synthesizer.GetInstalledVoices())
                    {
                        if (v.Enabled && (v.VoiceInfo.Culture?.Name ?? "").StartsWith(langPref, StringComparison.OrdinalIgnoreCase))
                        {
                            voice = v;
                            break;
                        }
                    }
                }
                catch { }

                _synthesizer.Rate = rate;
                if (voice != null)
                    _synthesizer.SelectVoice(voice.VoiceInfo.Name);

                if (GlHost != null) GlHost.IsSpeaking = true;
                _synthesizer.SpeakAsync(text);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VA] TTS speak error: {ex.Message}");
                if (GlHost != null) GlHost.IsSpeaking = false;
            }
        }

        private (string LangPrefix, int Rate) GetVoiceSettings(string character)
        {
            return character switch
            {
                "Fern"   => ("en", 1),
                "Huohuo" => ("zh", 2),
                "Jian"   => ("zh", 2),
                _        => ("ja", 0) // Frieren
            };
        }

        // ── STT native (pengganti startSpeech di JS) ──────────────────────
        private void StartListening()
        {
            try
            {
                if (_recognizer == null)
                {
                    ShowNotification("🎤 Speech recognition tidak tersedia (perlu language pack Windows).");
                    return;
                }
                if (_isListening) return;

                _isListening = true;
                _recognizer.RecognizeAsync(System.Speech.Recognition.RecognizeMode.Single);
                ShowNotification("🎤 Listening...");
            }
            catch (Exception ex)
            {
                _isListening = false;
                Console.WriteLine($"[VA] STT start error: {ex.Message}");
                ShowNotification("🎤 Gagal memulai voice input.");
            }
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
                SpeakText(aiResponse, _currentLang);

                _hideChatTimer?.Stop();
                _hideChatTimer?.Start();
            }
            
            App.OptimizeMemory();
        }

        private async void ManualVision_Click(object sender, RoutedEventArgs e)
        {
            await PerformAiObservation();
        }
        
        private void ManualChat_Click(object sender, RoutedEventArgs e)
        {
            // Toggle inline chat panel
            ChatPanel.Visibility = ChatPanel.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
            if (ChatPanel.Visibility == Visibility.Visible)
            {
                ChatInput.Text = "";
                ChatInput.Focus();
            }
        }
        
        private void ManualMic_Click(object sender, RoutedEventArgs e)
        {
            StartListening();
        }
        
        // ── Inline Chat Panel ────────────────────────────────────
        
        private void ChatToggleBtn_Click(object sender, MouseButtonEventArgs e)
        {
            ManualChat_Click(sender, null!);
        }
        
        private void ChatInput_GotFocus(object sender, RoutedEventArgs e)
        {
            if (ChatInput.Text == "Type a message...")
                ChatInput.Text = "";
        }
        
        private void ChatInput_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ChatInput.Text))
                ChatInput.Text = "Type a message...";
        }
        
        private async void ChatInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                await SendChatMessage();
            }
        }
        
        private async void ChatSendBtn_Click(object sender, MouseButtonEventArgs e)
        {
            await SendChatMessage();
        }
        
        private async Task SendChatMessage()
        {
            string msg = ChatInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(msg) || msg == "Type a message...") return;
            
            ChatInput.Text = "";
            ChatPanel.Visibility = Visibility.Collapsed;
            await HandleUserChat(msg);
        }
        
        // ── Character Switching ──────────────────────────────────
        
        private void SetFrieren_Click(object sender, RoutedEventArgs e)
        {
            _currentCharacter = "Frieren";
            UpdateCharacterMenu();
            SetCharacter("Frieren");
        }
        
        private void SetFern_Click(object sender, RoutedEventArgs e)
        {
            _currentCharacter = "Fern";
            UpdateCharacterMenu();
            SetCharacter("Fern");
        }
        
        private void SetHuohuo_Click(object sender, RoutedEventArgs e)
        {
            _currentCharacter = "Huohuo";
            UpdateCharacterMenu();
            SetCharacter("Huohuo");
        }
        
        private void UpdateCharacterMenu()
        {
            CharFrieren.IsChecked = _currentCharacter == "Frieren";
            CharFern.IsChecked = _currentCharacter == "Fern";
            CharHuohuo.IsChecked = _currentCharacter == "Huohuo";
            
            // Welcome message for new character
            if (_waifuChatService != null)
            {
                string greeting = _waifuChatService.GetGreeting(_currentCharacter);
                ChatText.Text = greeting;
                ChatBubble.Visibility = Visibility.Visible;
                _hideChatTimer?.Stop(); _hideChatTimer?.Start();
            }
        }
        private void MicButton_Click(object sender, MouseButtonEventArgs e)
        {
            StartListening();

            // Visual feedback — pulse mic button
            MicButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0x99, 0x00, 0xD4, 0xFF));
            var resetTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(2000) };
            resetTimer.Tick += (s2, e2) =>
            {
                resetTimer.Stop();
                MicButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0x66, 0x00, 0x00, 0x00));
            };
            resetTimer.Start();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => this.Close();
        private void HideChatBubble() => ChatBubble.Visibility = Visibility.Collapsed;
        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) { _isDragging = true; _dragOffset = e.GetPosition(this); CaptureMouse(); }
        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) { _isDragging = false; ReleaseMouseCapture(); }
        private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e) { if (_isDragging) { var pos = e.GetPosition(this); this.Left += pos.X - _dragOffset.X; this.Top += pos.Y - _dragOffset.Y; } }
        protected override void OnClosed(EventArgs e)
        {
            _eyeTrackingTimer?.Stop(); _autoTalkTimer?.Stop(); _visionTimer?.Stop(); _hideChatTimer?.Stop();
            _fadeInCheckTimer?.Stop();
            _isHostDisposed = true;
            try { _synthesizer?.SpeakAsyncCancelAll(); _synthesizer?.Dispose(); } catch { }
            try { _recognizer?.RecognizeAsyncCancel(); _recognizer?.Dispose(); } catch { }
            try { GlHost?.Dispose(); } catch { }
            base.OnClosed(e);
            App.OptimizeMemory();
        }

        private void OnWindowDeactivated(object? sender, EventArgs e)
        {
            // Ganti TrySuspendAsync() WebView2 → pause render loop native (hemat CPU/GPU)
            try { if (GlHost != null) GlHost.IsPaused = true; } catch { }
        }

        private void OnWindowActivated(object? sender, EventArgs e)
        {
            try { if (GlHost != null) GlHost.IsPaused = false; } catch { }
        }
    }
}
