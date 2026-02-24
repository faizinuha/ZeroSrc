using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Http;
using System.Web;
using System.Text.RegularExpressions;

namespace ZeroMix.Plugins.Translate
{
    public partial class TranslatePluginUI : System.Windows.Controls.UserControl
    {
        private bool _isExpanded = false;
        private RealTimeTranslator? _realTimeTranslator;

        public TranslatePluginUI()
        {
            InitializeComponent();
        }

        private void TranslateCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (TranslatePluginToggle.IsChecked != true) 
            {
                System.Windows.MessageBox.Show("Aktifkan (Check) plugin dulu ya Kak, baru bisa buka pengaturannya! ✨", "ZeroMix Translate");
                return;
            }
            _isExpanded = !_isExpanded;
            TranslateConfigBorder.Visibility = _isExpanded ? Visibility.Visible : Visibility.Collapsed;
        }

        private void TranslatePluginToggle_Click(object sender, RoutedEventArgs e)
        {
            if (TranslatePluginToggle.IsChecked == true)
            {
                _realTimeTranslator = new RealTimeTranslator();
                UpdateRealTimeLangs();
                _isExpanded = true;
                TranslateConfigBorder.Visibility = Visibility.Visible;
                System.Windows.MessageBox.Show("Magic Translate AKTIF! 🚀\n\nCara Pakai:\n1. Ketik di mana saja (Discord/Notepad/Browser).\n2. Tekan [Ctrl + Space] untuk sulap teksnya!\n\nPastikan bahasa asal & tujuan sudah benar ya Kak.", "ZeroMix Translate");
            }
            else
            {
                _realTimeTranslator?.Dispose();
                _realTimeTranslator = null;
                _isExpanded = false;
                TranslateConfigBorder.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateRealTimeLangs()
        {
            if (_realTimeTranslator != null)
            {
                _realTimeTranslator.SourceLang = (SourceLangCombo.SelectedItem as ComboBoxItem)?.Tag.ToString() ?? "id";
                _realTimeTranslator.TargetLang = (TargetLangCombo.SelectedItem as ComboBoxItem)?.Tag.ToString() ?? "en";
            }
        }

        private void SwapLanguages_Click(object sender, RoutedEventArgs e)
        {
            int sourceIdx = SourceLangCombo.SelectedIndex;
            int targetIdx = TargetLangCombo.SelectedIndex;
            
            SourceLangCombo.SelectedIndex = targetIdx;
            TargetLangCombo.SelectedIndex = sourceIdx;
            UpdateRealTimeLangs();
            
            // Auto swap text if output exists
            if (!string.IsNullOrEmpty(TranslateOutput.Text))
            {
                string oldInput = TranslateInput.Text;
                TranslateInput.Text = TranslateOutput.Text;
                TranslateOutput.Text = oldInput;
            }
        }

        private async void DoTranslate_Click(object sender, RoutedEventArgs e)
        {
            string text = TranslateInput.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            string from = (SourceLangCombo.SelectedItem as ComboBoxItem)?.Tag.ToString() ?? "id";
            string to = (TargetLangCombo.SelectedItem as ComboBoxItem)?.Tag.ToString() ?? "en";
            UpdateRealTimeLangs();

            TranslateOutput.Text = "Translating...";
            
            try
            {
                string result = await TranslateText(text, from, to);
                TranslateOutput.Text = result;
            }
            catch (Exception ex)
            {
                TranslateOutput.Text = "Error: " + ex.Message;
            }
        }

        private async Task<string> TranslateText(string input, string from, string to)
        {
            try
            {
                // Use Google Translate free endpoint (experimental/standard web)
                string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={from}&tl={to}&dt=t&q={HttpUtility.UrlEncode(input)}";
                
                using (HttpClient client = new HttpClient())
                {
                    string json = await client.GetStringAsync(url);
                    
                    // Simple regex to extract the first translated segment from the json array response
                    // Example response: [[["Hello","Halo",null,null,1]],null,"id"]
                    var matches = Regex.Matches(json, "\"(.*?)\"");
                    if (matches.Count > 0)
                    {
                        return matches[0].Groups[1].Value;
                    }
                    return "Translation failed.";
                }
            }
            catch
            {
                return "Network error.";
            }
        }
    }
}
