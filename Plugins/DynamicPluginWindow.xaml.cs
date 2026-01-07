using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ZeroMix.Plugins
{
    public partial class DynamicPluginWindow : Window
    {
        public DynamicPluginWindow()
        {
            InitializeComponent();
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public void AddControl(FrameworkElement control)
        {
            control.Margin = new Thickness(0, 0, 0, 15);
            MainContainer.Children.Add(control);
        }
    }
}
