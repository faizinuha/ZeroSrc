using System;
using System.Windows.Forms;
using System.Windows.Threading;

namespace ZeroMix.Plugins.Battery
{
    public class BatteryPlugin
    {
        public event Action<int, bool, bool, bool>? OnBatteryStatusChanged;
        
        // Custom Texts
        public string TextMorning { get; set; } = "Selamat pagi kak! Udah siap buat produktif hari ini? Nyam2~";
        public string TextAfternoon { get; set; } = "Siang kak! Rehat sejenak yuk, jangan lupa makan siang ya!";
        public string TextEvening { get; set; } = "Sore kak! Sebentar lagi selesai nih, tetap semangat ya!";
        public string TextNight { get; set; } = "Malam kak! Jangan begadang ya, istirahat yang cukup biar besok seger!";
        
        public string TextBatteryWarn { get; set; } = "Baterai kakak tinggal setengah nih. Siap-siap ambil charger ya?";
        public string TextBatteryCritical { get; set; } = "Hwaaa! Baterainya udah kritis banget kak! Cepetan cas ya!";

        private DispatcherTimer _timer;
        private int _lastThreshold = -1;
        private bool _hasGreetedToday = false;
        private int _lastGreetDay = -1;

        public BatteryPlugin()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMinutes(1);
            _timer.Tick += (s, e) => CheckBattery(false);
        }

        public void Start()
        {
            _timer.Start();
            CheckBattery(true); // Is initial check
        }

        public void Stop()
        {
            _timer.Stop();
        }

        private void CheckBattery(bool isInitial)
        {
            var status = SystemInformation.PowerStatus;
            int percent = (int)(status.BatteryLifePercent * 100);
            bool isCharging = status.PowerLineStatus == PowerLineStatus.Online;
            
            DateTime now = DateTime.Now;
            int hour = now.Hour;

            // Reset greeting if it's a new day
            if (now.Day != _lastGreetDay)
            {
                _hasGreetedToday = false;
                _lastGreetDay = now.Day;
            }

            bool shouldNotify = false;
            bool isGreeting = false;

            // 1. Sapaan (Greeting) - Muncul saat aplikasi start atau pertama kali di hari tsb
            if (isInitial || !_hasGreetedToday)
            {
                shouldNotify = true;
                isGreeting = true;
                _hasGreetedToday = true;
            }

            // 2. Threshold Alerts (Warn & Critical)
            // Notifikasi muncul saat turun/naik melewati angka sakral
            int currentThreshold = -1;
            if (percent <= 20) currentThreshold = 20;
            else if (percent <= 50) currentThreshold = 50;
            else if (percent >= 100 && isCharging) currentThreshold = 100;

            if (currentThreshold != -1 && currentThreshold != _lastThreshold)
            {
                _lastThreshold = currentThreshold;
                shouldNotify = true;
            }

            if (shouldNotify)
            {
                OnBatteryStatusChanged?.Invoke(percent, isCharging, isGreeting, false);
            }
        }
    }
}
