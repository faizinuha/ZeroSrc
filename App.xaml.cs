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

            // Inisialisasi dan jalankan pengecekan update secara Asynchronous
            var updater = new GithubUpdater();

            // Panggil metode async. 
            // Menggunakan 'async void' pada OnStartup aman karena ini adalah event handler.
            await Task.Run(() => updater.CheckAndUpdateAsync());
        }
    }
    }