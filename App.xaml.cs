using System;
using System.Windows;
using ZeroMix.Core;

namespace ZeroMix
{
    public partial class App : Application
    {
        public static HotkeyCore? HotkeyCoreInstance { get; private set; }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // First, create and show the main window.
            // This becomes the main window for the application's lifetime.
            var mainWindow = new MainWindow();
            // this.MainWindow = mainWindow;
            mainWindow.Show();

            // Now, run the background hotkey service.
            HotkeyCoreInstance = new HotkeyCore();
            HotkeyCoreInstance.Show(); // This window is invisible by design.

            // Check for updates in the background.
            try
            {
                var updater = new GithubUpdater();
                await updater.CheckAndUpdateAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during update check: {ex.Message}");
                // Don't show a blocking MessageBox on startup for update errors.
            }
        }
    }
}