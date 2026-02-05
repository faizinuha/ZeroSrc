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
        private readonly List<string> _chatMessages = new()
        {
            "Halo! Aku Frieren~ ✨",
            "Ada yang bisa aku bantu?",
            "Klik lagi dong~ 😊",
            "Aku suka magia...",
            "Himmel pasti bangga padamu!",
            "Jangan lupa istirahat ya~",
            "Semangat! 💪",
            "Kamu hebat!",
            "Aku akan menemanimu~",
            "Mau dengar cerita?"
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
        }

        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Initialize WebView2
                var env = await CoreWebView2Environment.CreateAsync();
                await Live2DView.EnsureCoreWebView2Async(env);
                
                // Settings for transparency
                Live2DView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                Live2DView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                
                // Handle messages from JavaScript
                Live2DView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
                
                // Load Live2D viewer HTML
                string htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                    "Virtual_Assisten", "live2d-viewer.html");
                
                if (File.Exists(htmlPath))
                {
                    Live2DView.CoreWebView2.Navigate(new Uri(htmlPath).AbsoluteUri);
                }
                else
                {
                    // Create the HTML file if not exists
                    CreateLive2DViewerHtml();
                    Live2DView.CoreWebView2.Navigate(new Uri(htmlPath).AbsoluteUri);
                }
                
                Debug.WriteLine("[VirtualAssistant] WebView2 initialized");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VirtualAssistant] Error: {ex.Message}");
            }
        }

        private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string message = e.TryGetWebMessageAsString();
            
            if (message == "character_clicked")
            {
                Dispatcher.Invoke(() => ShowNextChatMessage());
            }
        }

        private void ShowNextChatMessage()
        {
            // Shuffle through messages
            _messageIndex = _random.Next(_chatMessages.Count);
            ChatText.Text = _chatMessages[_messageIndex];
            
            // Show bubble with animation
            ChatBubble.Visibility = Visibility.Visible;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            ChatBubble.BeginAnimation(OpacityProperty, fadeIn);
            
            // Reset timer
            _hideChatTimer?.Stop();
            _hideChatTimer?.Start();
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
            if (Live2DView.CoreWebView2 == null) return;
            
            try
            {
                // Get mouse position relative to screen
                var screenPos = PointToScreen(e.GetPosition(this));
                var centerX = this.Left + this.Width / 2;
                var centerY = this.Top + this.Height / 2;
                
                // Calculate normalized values (-1 to 1)
                double eyeX = (screenPos.X - centerX) / 300.0;
                double eyeY = (screenPos.Y - centerY) / 300.0;
                
                eyeX = Math.Max(-1, Math.Min(1, eyeX));
                eyeY = Math.Max(-1, Math.Min(1, eyeY));
                
                await Live2DView.ExecuteScriptAsync($"updateEyeTracking({eyeX}, {eyeY})");
            }
            catch { }
        }

        private void CreateLive2DViewerHtml()
        {
            string htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                "Virtual_Assisten", "live2d-viewer.html");
            
            string dir = Path.GetDirectoryName(htmlPath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            
            // The HTML will be created separately
        }

        public void SetChatMessages(List<string> messages)
        {
            _chatMessages.Clear();
            _chatMessages.AddRange(messages);
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
            base.OnClosed(e);
        }
    }
}
