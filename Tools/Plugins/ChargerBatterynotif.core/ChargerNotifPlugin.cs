using System;
using System.Windows.Forms;
using System.Windows.Threading;

namespace ZeroMix.Plugins.ChargerNotif
{
    /// <summary>
    /// Monitor khusus event CHARGER:
    ///   ⚡ Charger dicolok  → notif Charging
    ///   🔌 Charger dicabut  → notif Unplugged
    ///   🎉 Baterai penuh    → notif Full
    ///
    /// Low / Critical TIDAK ditangani di sini — sudah ada di zeromix.Battery.
    /// </summary>
    public class ChargerNotifPlugin
    {
        // ── Pesan yang bisa dikustomisasi ──────────────────────────────────
        public string MsgCharging  { get; set; } = "Charger terhubung! Baterai sedang mengisi daya ⚡";
        public string MsgUnplugged { get; set; } = "Charger dicabut. Hemat daya ya, Kak!";
        public string MsgFull      { get; set; } = "Baterai sudah penuh! Boleh dicabut charger-nya, Kak~";

        // ── Custom sound path (opsional, user pilih via Browse) ────────────
        public string? CustomSoundCharging  { get; set; }
        public string? CustomSoundUnplug    { get; set; }
        public string? CustomSoundFull      { get; set; }

        // ── Custom Lottie JSON (opsional) ──────────────────────────────────
        public string? CustomJsonCharging  { get; set; }
        public string? CustomJsonUnplugged { get; set; }
        public string? CustomJsonFull      { get; set; }

        // ── State internal ─────────────────────────────────────────────────
        private bool _wasCharging;
        private bool _fullNotified;
        private readonly DispatcherTimer _timer;

        public ChargerNotifPlugin()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
            _timer.Tick += (s, e) => Poll();

            // Seed state awal tanpa trigger notif
            _wasCharging = SystemInformation.PowerStatus.PowerLineStatus == PowerLineStatus.Online;
        }

        public void Start()
        {
            _timer.Start();
            Poll(); // cek langsung saat pertama kali aktif
        }

        public void Stop() => _timer.Stop();

        // ── Polling ────────────────────────────────────────────────────────
        private void Poll()
        {
            var status   = SystemInformation.PowerStatus;
            int percent  = (int)(status.BatteryLifePercent * 100);
            bool charging = status.PowerLineStatus == PowerLineStatus.Online;

            // Charger baru dicolok
            if (charging && !_wasCharging)
            {
                _wasCharging  = true;
                _fullNotified = false;
                Show(BatteryNotifWindow.NotifType.Charging, percent,
                     MsgCharging, CustomJsonCharging, CustomSoundCharging);
                return;
            }

            // Charger baru dicabut
            if (!charging && _wasCharging)
            {
                _wasCharging  = false;
                _fullNotified = false;
                Show(BatteryNotifWindow.NotifType.Unplugged, percent,
                     MsgUnplugged, CustomJsonUnplugged, CustomSoundUnplug);
                return;
            }

            // Baterai penuh (hanya sekali per siklus cas)
            if (charging && percent >= 100 && !_fullNotified)
            {
                _fullNotified = true;
                Show(BatteryNotifWindow.NotifType.Full, percent,
                     MsgFull, CustomJsonFull, CustomSoundFull);
            }
        }

        // ── Tampilkan notif di UI thread ───────────────────────────────────
        private static void Show(BatteryNotifWindow.NotifType type, int percent,
                                  string msg, string? customJson, string? customSound)
        {
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    new BatteryNotifWindow(type, percent, msg, customJson, customSound).Show();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ChargerNotif] {ex.Message}");
                }
            });
        }
    }
}
