using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ZeroMix.Plugins.Translate
{
    public partial class TranslatePluginUI : System.Windows.Controls.UserControl
    {
        private RealTimeTranslator? _coreEngine;
        private SelectionBubble? _bubbleEngine;
        private bool _isExpanded = false;

        public TranslatePluginUI()
        {
            InitializeComponent();
            UpdateFeatureCount();
        }

        private void PowerSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (PowerSwitch.IsChecked == true)
            {
                // Engine Start
                _coreEngine = new RealTimeTranslator();
                _coreEngine.OnTranslated += CoreEngine_OnTranslated;
                _coreEngine.OnError += CoreEngine_OnError;
                
                UpdateEngineConfig();
                UpdateStatus();
                
                LogMsg("[OK] Engine V2 STARTED. Proteksi tabrakan ketikan AKTIF.");
            }
            else
            {
                // Engine Stop
                _coreEngine?.Dispose();
                _coreEngine = null;
                _bubbleEngine?.Dispose();
                _bubbleEngine = null;
                UpdateStatus();
                LogMsg("[STOP] Engine DIMATIKAN.");
            }
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _isExpanded = !_isExpanded;
            if (_isExpanded)
            {
                ((Storyboard)Resources["ExpandAnim"]).Begin();
                IconRotation.BeginAnimation(RotateTransform.AngleProperty,
                    new DoubleAnimation(0, 180, TimeSpan.FromMilliseconds(250)));
            }
            else
            {
                ((Storyboard)Resources["CollapseAnim"]).Begin();
                IconRotation.BeginAnimation(RotateTransform.AngleProperty,
                    new DoubleAnimation(180, 0, TimeSpan.FromMilliseconds(200)));
            }
        }

        private void FeatureToggle_Changed(object sender, RoutedEventArgs e)
        {
            UpdateFeatureCount();
            UpdateStatus();
            
            // Update bubble time panel visibility
            if (BubbleModeSwitch.IsChecked == true)
            {
                BubbleTimePanel.Visibility = Visibility.Visible;
            }
            else
            {
                BubbleTimePanel.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateFeatureCount()
        {
            int activeCount = 0;
            if (KeyboardTranslateToggle?.IsChecked == true) activeCount++;
            if (GameModeSwitch?.IsChecked == true) activeCount++;
            if (BubbleModeSwitch?.IsChecked == true) activeCount++;
            
            FeatureCount.Text = $"{activeCount}/3 features active";
        }

        private void UpdateStatus()
        {
            if (PowerSwitch.IsChecked == true)
            {
                StatusText.Text = "Engine Active";
                StatusText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 255, 136)); // Green
            }
            else
            {
                StatusText.Text = "Standby";
                StatusText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)); // Gray
            }
        }

        private void CoreEngine_OnTranslated(string original, string result)
        {
            Dispatcher.Invoke(() => {
                LogMsg($"> {original} => {result}");
            });
        }

        private void CoreEngine_OnError(string errMsg)
        {
            Dispatcher.Invoke(() => {
                LogMsg($"[ERROR] {errMsg}");
            });
        }

        private void LogMsg(string msg)
        {
            string time = DateTime.Now.ToString("HH:mm:ss");
            LiveLog.Text = $"[{time}] {msg}\n" + LiveLog.Text;
            
            // Batasi panjang log agar memori tidak penuh
            if (LiveLog.Text.Length > 2000)
            {
                LiveLog.Text = LiveLog.Text.Substring(0, 2000);
            }
        }

        private void Langs_Changed(object sender, SelectionChangedEventArgs e)
        {
            UpdateEngineConfig();
            if (_bubbleEngine != null)
            {
                _bubbleEngine.SourceLang = (ComboSource?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "auto";
                _bubbleEngine.TargetLang = (ComboTarget?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "en";
            }
        }

        private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SpeedLabel != null)
            {
                int val = (int)e.NewValue;
                string status = val < 500 ? "(Sangat Cepat - Risiko Tabrakan)" : (val < 1000 ? "(Stabil & Direkomendasikan)" : "(Lambat & Ekstra Aman)");
                SpeedLabel.Text = $"{val} ms {status}";
                
                if (_coreEngine != null) _coreEngine.DebounceMs = val;
            }
        }

        private void UpdateEngineConfig()
        {
            if (_coreEngine != null && ComboSource != null && ComboTarget != null)
            {
                _coreEngine.SourceLang = (ComboSource.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "id";
                _coreEngine.TargetLang = (ComboTarget.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "en";
                _coreEngine.DebounceMs = (int)(SpeedSlider?.Value ?? 800);
                _coreEngine.GameMode = GameModeSwitch?.IsChecked == true;
            }
        }

        private void GameModeSwitch_Click(object sender, RoutedEventArgs e)
        {
            bool isGame = GameModeSwitch.IsChecked == true;
            if (_coreEngine != null) _coreEngine.GameMode = isGame;
            LogMsg(isGame
                ? "[GAME MODE] Aktif — terjemahan via Clipboard Paste (Ctrl+V). Cocok untuk game chat."
                : "[NORMAL MODE] Aktif — terjemahan via Unicode Inject. Cocok untuk browser & app.");
        }

        private void SwapBtn_Click(object sender, RoutedEventArgs e)
        {
            int s = ComboSource.SelectedIndex;
            int t = ComboTarget.SelectedIndex;
            ComboSource.SelectedIndex = t;
            ComboTarget.SelectedIndex = s;
        }

        private void PurgeBtn_Click(object sender, RoutedEventArgs e)
        {
            _coreEngine?.ForceClear();
            LogMsg("[PURGE] Memory Buffer telah dikosongkan.");
        }

        private void BubbleModeSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (_coreEngine == null) return;
            
            if (BubbleModeSwitch.IsChecked == true)
            {
                _bubbleEngine?.Dispose();
                _bubbleEngine = new SelectionBubble(_coreEngine);
                _bubbleEngine.SourceLang = (ComboSource?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "auto";
                _bubbleEngine.TargetLang = (ComboTarget?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "en";
                _bubbleEngine.BubbleDisplaySeconds = (int)(BubbleTimeSlider?.Value ?? 4);
                _bubbleEngine.OnLog += (msg) => Dispatcher.Invoke(() => LogMsg(msg));
                
                // Show bubble time slider
                BubbleTimePanel.Visibility = Visibility.Visible;
                
                LogMsg("[BUBBLE] Selection Bubble AKTIF — highlight + Ctrl+C untuk translate.");
            }
            else
            {
                _bubbleEngine?.Dispose();
                _bubbleEngine = null;
                
                // Hide bubble time slider
                BubbleTimePanel.Visibility = Visibility.Collapsed;
                
                LogMsg("[BUBBLE] Selection Bubble NONAKTIF.");
            }
        }

        private void BubbleTimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (BubbleTimeLabel != null)
            {
                int seconds = (int)e.NewValue;
                BubbleTimeLabel.Text = $"{seconds} detik";
                
                if (_bubbleEngine != null)
                {
                    _bubbleEngine.BubbleDisplaySeconds = seconds;
                }
            }
        }

    }
}
