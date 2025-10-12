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
	}
}
