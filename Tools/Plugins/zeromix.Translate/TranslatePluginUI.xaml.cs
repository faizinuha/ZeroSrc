using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace ZeroMix.Plugins.Translate
{
    public partial class TranslatePluginUI : System.Windows.Controls.UserControl
    {
        private RealTimeTranslator? _coreEngine;
        private SelectionBubble? _bubbleEngine;
        private OcrSnip? _ocrSnip;

        public TranslatePluginUI()
        {
            InitializeComponent();
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
                
                CoreSettings.Visibility = Visibility.Visible;
                Storyboard sb = (Storyboard)this.Resources["FadeIn"];
                sb?.Begin(CoreSettings);

                LogMsg("[OK] Engine V2 STARTED. Proteksi tabrakan ketikan AKTIF.");
            }
            else
            {
                // Engine Stop
                _coreEngine?.Dispose();
                _coreEngine = null;
                _bubbleEngine?.Dispose();
                _bubbleEngine = null;
                _ocrSnip?.Dispose();
                _ocrSnip = null;
                CoreSettings.Visibility = Visibility.Collapsed;
                LogMsg("[STOP] Engine DIMATIKAN.");
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
                _bubbleEngine.OnLog += (msg) => Dispatcher.Invoke(() => LogMsg(msg));
                LogMsg("[BUBBLE] Selection Bubble AKTIF — highlight + Ctrl+C untuk translate.");
            }
            else
            {
                _bubbleEngine?.Dispose();
                _bubbleEngine = null;
                LogMsg("[BUBBLE] Selection Bubble NONAKTIF.");
            }
        }

        private void OcrSnipButton_Click(object sender, RoutedEventArgs e)
        {
            if (_coreEngine == null)
            {
                LogMsg("[OCR ERROR] Engine harus diaktifkan terlebih dahulu!");
                return;
            }

            try
            {
                // Initialize OCR Snip if not exists
                if (_ocrSnip == null)
                {
                    _ocrSnip = new OcrSnip(_coreEngine);
                    _ocrSnip.SourceLang = (ComboSource?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "auto";
                    _ocrSnip.TargetLang = (ComboTarget?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "en";
                    _ocrSnip.OnLog += (msg) => Dispatcher.Invoke(() => LogMsg(msg));
                }

                // Update language settings
                _ocrSnip.SourceLang = (ComboSource?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "auto";
                _ocrSnip.TargetLang = (ComboTarget?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "en";

                // Start screen capture
                _ocrSnip.StartSnip();
                LogMsg("[OCR SNIP] Mulai screen capture — pilih area untuk extract text!");
            }
            catch (Exception ex)
            {
                LogMsg($"[OCR ERROR] {ex.Message}");
            }
        }
    }
}
