using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows.Media;
using ZeroMix.Virtual_Assisten.Native;

namespace ZeroMix.Virtual_Assisten
{
    public partial class VirtualAssistantWindow : Window
    {
        private bool _isDragging = false;
        private System.Windows.Point _dragOffset;
        private DispatcherTimer? _hideChatTimer;
        private DispatcherTimer? _autoTalkTimer;
        private DispatcherTimer? _renderTimer;
        private bool _isModelLoaded = false;
        private string _currentCharacter = "Frieren";

        // Native Native
        private NativeRenderer? _renderer;
        private Live2DModelNative? _activeModel;

        private readonly Dictionary<string, List<string>> _characterMessages = new()
        {
            ["Frieren"] = new List<string> { "Halo! Aku Frieren~ ✨", "Apa ada yang bisa aku bantu?", "Himmel pasti bangga padamu!" },
            ["Fern"] = new List<string> { "Halo, Tuan Frieren.", "Jangan malas-malasan ya.", "Zoltraak!" },
            ["Huohuo"] = new List<string> { "Aaaah! Ada hantu?! 👻", "Maaf... aku Huohuo.", "Tuan ekor... tolong!" }
        };
        
        private int _messageIndex = 0;
        private Random _random = new Random();

        public VirtualAssistantWindow()
        {
            InitializeComponent();
            
            var workArea = SystemParameters.WorkArea;
            this.Left = workArea.Right - this.Width - 20;
            this.Top = workArea.Bottom - this.Height - 20;
            
            this.Loaded += OnWindowLoaded;
            this.MouseLeftButtonDown += OnMouseLeftButtonDown;
            this.MouseLeftButtonUp += OnMouseLeftButtonUp;
            this.MouseMove += OnMouseMove;
            
            _hideChatTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
            _hideChatTimer.Tick += (s, e) => HideChatBubble();

            _autoTalkTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            _autoTalkTimer.Tick += (s, e) => ShowNextChatMessage();
            _autoTalkTimer.Start();

            // Native Render Timer (~60 FPS)
            _renderTimer = new DispatcherTimer(DispatcherPriority.Render);
            _renderTimer.Interval = TimeSpan.FromMilliseconds(16);
            _renderTimer.Tick += (s, e) => _renderer?.Render(_activeModel!);
            _renderTimer.Start();
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _renderer = new NativeRenderer();
                NativeScene.Source = _renderer.ImageSource;
                
                // Load default character
                SetCharacter(_currentCharacter);
                
                Debug.WriteLine("[VirtualAssistant] Native DirectX 11 Renderer Initialized");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VirtualAssistant] Native Init Error: {ex.Message}");
                System.Windows.MessageBox.Show("Gagal inisialisasi Native Render: " + ex.Message);
            }
        }

        public void SetCharacter(string characterName)
        {
            _currentCharacter = characterName;
            _isModelLoaded = false;

            try
            {
                // In Industrial version, paths would be to the extracted .moc3 files
                string mocPath = GetMocPath(characterName);
                if (File.Exists(mocPath))
                {
                    _activeModel?.Dispose();
                    _activeModel = new Live2DModelNative(mocPath);
                    _isModelLoaded = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VirtualAssistant] Model Load Error: {ex.Message}");
            }
        }

        private string GetMocPath(string characterName)
        {
            string baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Virtual_Assisten");
            return characterName switch
            {
                "Fern" => Path.Combine(baseDir, "Sou Sou No Frieren/fern/fern.moc3"),
                "Huohuo" => Path.Combine(baseDir, "Mihoyo/Honkai_Star_Rail/huohuo2/huohuo/huohuo.moc3"),
                _ => Path.Combine(baseDir, "Sou Sou No Frieren/Frieren/Frieren.moc3")
            };
        }

        private void ShowNextChatMessage()
        {
            var messages = _characterMessages.ContainsKey(_currentCharacter) ? _characterMessages[_currentCharacter] : _characterMessages["Frieren"];
            _messageIndex = _random.Next(messages.Count);
            ChatText.Text = messages[_messageIndex];
            ChatBubble.Visibility = Visibility.Visible;
            _hideChatTimer?.Stop();
            _hideChatTimer?.Start();
        }

        private void HideChatBubble()
        {
            ChatBubble.Visibility = Visibility.Collapsed;
            _hideChatTimer?.Stop();
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.GetPosition(this).Y > 60)
            {
                _isDragging = true;
                _dragOffset = e.GetPosition(this);
                this.CaptureMouse();
                ShowNextChatMessage();
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
        }

        protected override void OnClosed(EventArgs e)
        {
            _renderTimer?.Stop();
            _renderer?.Dispose();
            _activeModel?.Dispose();
            base.OnClosed(e);
        }
    }
}
