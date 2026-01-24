using System.Windows;
using Microsoft.Win32;

namespace ZeroMix.Studio
{
    public partial class ExportOptionsDialog : Window
    {
        public string OutputPath { get; private set; } = "";
        public int SelectedWidth { get; private set; } = 1920;
        public int SelectedHeight { get; private set; } = 1080;
        public int SelectedFPS { get; private set; } = 30;
        public string SelectedFade { get; private set; } = "none";

        public ExportOptionsDialog()
        {
            InitializeComponent();
            OutputPathBox.Text = "ZeroMix_Export_" + System.DateTime.Now.ToString("yyyyMMdd_HHmm") + ".mp4";
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Filter = "MP4 Video (*.mp4)|*.mp4";
            dlg.FileName = OutputPathBox.Text;
            if (dlg.ShowDialog() == true)
            {
                OutputPathBox.Text = dlg.FileName;
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            var res = (ResolutionCombo.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag?.ToString()?.Split(',');
            if (res != null && res.Length == 2)
            {
                int.TryParse(res[0], out int w);
                int.TryParse(res[1], out int h);
                SelectedWidth = w;
                SelectedHeight = h;
            }
            var fpsTag = (FPSCombo.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag?.ToString();
            int.TryParse(fpsTag, out int fps);
            SelectedFPS = fps > 0 ? fps : 30;
            
            SelectedFade = (FadeCombo.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag?.ToString() ?? "none";
            
            OutputPath = OutputPathBox.Text;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}