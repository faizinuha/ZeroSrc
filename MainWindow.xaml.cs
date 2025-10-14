using System.Windows;

namespace ZeroMix
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		// Removed automatic overlay display on startup so main window is useful.

		private void OpenOverlay_Click(object sender, RoutedEventArgs e)
		{
			var overlay = new SearchOverlay();
			overlay.Show();
			// overlay.ShowInTaskbar = false; // Prevent overlay from appearing in taskbar
			// overlay.WindowStartupLocation = WindowStartupLocation.CenterScreen;
		}

        private void Home_Click(object sender, RoutedEventArgs e)
        {
            // For now, this button doesn't do much, but it could be used
            // to switch back to the main content view if other views are added.
        }

        private void CustomButton_Click(object sender, RoutedEventArgs e)
        {
            var customShortcutWindow = new CustomShortcutWindow();
            customShortcutWindow.ShowDialog();
        }
	}
}
