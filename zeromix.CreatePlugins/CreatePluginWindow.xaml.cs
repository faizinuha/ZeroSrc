using System;
using System.Windows;

namespace ZeroMix.zeromix.CreatePlugins
{
    public partial class CreatePluginWindow : Window
    {
        public string PluginName { get; private set; } = "";
        public bool IsPublic { get; private set; }
        public bool IsConfirmed { get; private set; }

        public CreatePluginWindow()
        {
            InitializeComponent();
            PluginNameInput.Focus();
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PluginNameInput.Text))
            {
                System.Windows.MessageBox.Show("Silakan masukkan nama plugin.", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            PluginName = PluginNameInput.Text;
            IsPublic = PublicRadio.IsChecked == true;
            IsConfirmed = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
