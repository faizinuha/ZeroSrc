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
                _coreEngine = new RealTimeTranslator();
                _coreEngine.OnTranslated += CoreEngine_OnTranslated;
                _coreEngine.OnError += CoreEngine_OnError;
                UpdateEngineConfig();
                UpdateStatus();
                LogMsg("[OK] Engine V2 STARTED.");

                // Aktifkan keyboard hook hanya jika toggle keyboard ON
                if (KeyboardTranslateToggle?.IsChecked == true)
                    _coreEngine.EnableKeyboardHook();
            }
            else
            {
                _coreEngine?.DisableKeyboardHook();
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

            // Keyboard toggle — install/uninstall hook sesuai state
            if (sender == KeyboardTranslateToggle && _coreEngine != null)
            {
                if (KeyboardTranslateToggle.IsChecked == true)
                {
                    _coreEngine.EnableKeyboardHook();
                    LogMsg("[KEYBOARD] Keyboard translate AKTIF.");
                }
                else
                {
                    _coreEngine.DisableKeyboardHook();
                    LogMsg("[KEYBOARD] Keyboard translate NONAKTIF.");
                }
            }

            // Bubble toggle
            if (BubbleModeSwitch != null)
            {
                BubbleTimePanel.Visibility = BubbleModeSwitch.IsChecked == true
                    ? Visibility.Visible : Visibility.Collapsed;
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
            // Ambil tag (language code) dari item yang sedang dipilih
            string srcTag = (ComboSource.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "id";
            string tgtTag = (ComboTarget.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "en";

            // Cari item di ComboSource yang tag-nya = tgtTag, set sebagai selected
            foreach (ComboBoxItem item in ComboSource.Items)
            {
                if (item.Tag?.ToString() == tgtTag)
                {
                    ComboSource.SelectedItem = item;
                    break;
                }
            }

            // Cari item di ComboTarget yang tag-nya = srcTag
            foreach (ComboBoxItem item in ComboTarget.Items)
            {
                if (item.Tag?.ToString() == srcTag)
                {
                    ComboTarget.SelectedItem = item;
                    break;
                }
            }

            UpdateEngineConfig();
            LogMsg($"[SWAP] {srcTag} ⇄ {tgtTag}");
        }

        private void PurgeBtn_Click(object sender, RoutedEventArgs e)
        {
            _coreEngine?.ForceClear();
            LogMsg("[PURGE] Memory Buffer telah dikosongkan.");
        }

        private void BubbleModeSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (BubbleModeSwitch.IsChecked == true)
            {
                if (_coreEngine == null)
                {
                    LogMsg("[BUBBLE] Aktifkan Engine terlebih dahulu!");
                    BubbleModeSwitch.IsChecked = false;
                    return;
                }

                _bubbleEngine?.Dispose();
                _bubbleEngine = new SelectionBubble(_coreEngine);
                _bubbleEngine.SourceLang = (ComboSource?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "auto";
                _bubbleEngine.TargetLang = (ComboTarget?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "en";
                _bubbleEngine.BubbleDisplaySeconds = (int)(BubbleTimeSlider?.Value ?? 4);
                _bubbleEngine.OnLog += (msg) => Dispatcher.Invoke(() => LogMsg(msg));
                BubbleTimePanel.Visibility = Visibility.Visible;
                LogMsg("[BUBBLE] AKTIF — drag teks untuk translate.");
            }
            else
            {
                _bubbleEngine?.Dispose();
                _bubbleEngine = null;
                BubbleTimePanel.Visibility = Visibility.Collapsed;
                LogMsg("[BUBBLE] NONAKTIF.");
            }
            UpdateFeatureCount();
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
