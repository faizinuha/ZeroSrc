using System;
using System.IO;
using System.Windows;
using System.Diagnostics;

namespace ZeroMix.Plugins.Spotify
{
    public class SpotifyPlugin
    {
        private GoogleAccountWindow? _window;
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
                InitializeWindow();
            }
            else
            {
                dispatcher.Invoke(InitializeWindow);
            }
        }

        private void InitializeWindow()
        {
            _window = GoogleAccountWindow.Instance;
            _window.ShowWindow();
            Debug.WriteLine("[GoogleAccount] Plugin Started as Lightweight Window");
        }

        public void Stop()
        {
            System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
            {
                _window?.Close();
            });
        }

        /// <summary>
        /// Menampilkan window Google Account (bisa dipanggil dari luar)
        /// </summary>
        public void ShowWindow()
        {
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher == null) return;

            if (dispatcher.CheckAccess())
            {
                _window?.ShowWindow();
            }
            else
            {
                dispatcher.Invoke(() => _window?.ShowWindow());
            }
        }
    }
}
