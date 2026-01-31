using System;
using System.IO;
using System.Windows;
using System.Diagnostics;

namespace ZeroMix.Plugins.Spotify
{
    public class SpotifyPlugin
    {
        private SpotifySidebar? _sidebar;
        private readonly string _pluginDir;

        public SpotifyPlugin()
        {
            _pluginDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins", "zeromix.spotify");
            if (!Directory.Exists(_pluginDir)) Directory.CreateDirectory(_pluginDir);
        }

        public void Start()
        {
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher == null) return;

            if (dispatcher.CheckAccess())
            {
                InitializeSidebar();
            }
            else
            {
                dispatcher.Invoke(InitializeSidebar);
            }
        }

        private void InitializeSidebar()
        {
            _sidebar = new SpotifySidebar();
            // Don't set owner to MainWindow to allow it to stay alive independently
            _sidebar.Show();
            Debug.WriteLine("[SpotifyPink] Plugin Started as Standalone Window");
        }

        public void Stop()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _sidebar?.Close();
            });
        }
    }
}
