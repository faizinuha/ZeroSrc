using System.Windows;

namespace ZeroMix.Features.ZeroConnect.UI
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsModel _working;
        public SettingsWindow()
        {
            InitializeComponent();
            // clone current settings so Cancel works
            _working = SettingsService.Instance.Model.Clone();

            Cb1.IsChecked = _working.ShowFeature1;
            Cb2.IsChecked = _working.ShowFeature2;
            Cb3.IsChecked = _working.ShowFeature3;
            Cb4.IsChecked = _working.ShowFeature4;
            Cb5.IsChecked = _working.ShowFeature5;
            Cb6.IsChecked = _working.ShowFeature6;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            _working.ShowFeature1 = Cb1.IsChecked ?? false;
            _working.ShowFeature2 = Cb2.IsChecked ?? false;
            _working.ShowFeature3 = Cb3.IsChecked ?? false;
            _working.ShowFeature4 = Cb4.IsChecked ?? false;
            _working.ShowFeature5 = Cb5.IsChecked ?? false;
            _working.ShowFeature6 = Cb6.IsChecked ?? false;

            SettingsService.Instance.Update(_working);
            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}