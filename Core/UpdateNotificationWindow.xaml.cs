using System;
using System.Windows;

namespace ZeroMix.Core
{
    public partial class UpdateNotificationWindow : Window
    {
        public UpdateNotificationWindow(string message)
        {
            InitializeComponent();
            UpdateMessage.Text = message;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Position window at the bottom-left corner of the screen
            var desktopWorkingArea = SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Left + 10; // 10px margin
            this.Top = desktopWorkingArea.Bottom - this.Height - 10; // 10px margin
        }

        public void ShowProgress()
        {
            UpdateMessage.Visibility = Visibility.Collapsed;
            ProgressPanel.Visibility = Visibility.Visible;
            YesButton.Visibility = Visibility.Collapsed;
            NoButton.Visibility = Visibility.Collapsed;
        }

        public void UpdateProgress(double percentage)
        {
            DownloadProgressBar.Value = percentage;
            ProgressText.Text = $"{percentage:F0}%";
        }

        public void ShowInstalling()
        {
            ProgressText.Text = "Unduhan selesai. Menginstal...";
            InstallingText.Visibility = Visibility.Visible;
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
