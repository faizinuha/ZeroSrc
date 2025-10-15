﻿using System.Configuration;
using System.Data;
using System.Windows;
using ZeroMix.Core;
using System.Threading.Tasks;

namespace ZeroMix
{
    using System.Windows;

    public partial class App : Application
    {
        // Buat properti statis agar instance HotkeyCore bisa diakses dari mana saja.
        public static HotkeyCore? HotkeyCoreInstance { get; private set; }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);


            // Periksa pembaruan saat aplikasi dimulai
            try
            {
                var updater = new MegaUpdater();
                await updater.CheckAndUpdateAsync();
            }
            catch (Exception ex)
            {

                System.Diagnostics.Debug.WriteLine($"Error during update check: {ex.Message}");
                MessageBox.Show("Terjadi kesalahan saat memeriksa pembaruan. Silakan coba lagi nanti.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Jalankan HotkeyCore yang mendaftar hotkey global dan menampilkan overlay saat ditekan.
            HotkeyCoreInstance = new HotkeyCore();
            HotkeyCoreInstance.Show();
        }
    }
}
