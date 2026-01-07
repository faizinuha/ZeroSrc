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
        private int _periodicCounter = 0;

        public BatteryPlugin()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMinutes(1);
            _timer.Tick += Timer_Tick;
        }

        public void Start()
        {
            _timer.Start();
            CheckBattery(false);
        }

        public void Stop()
        {
            _timer.Stop();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            _periodicCounter++;
            bool isPeriodic = false;
            
            if (_periodicCounter >= 3)
            {
                _periodicCounter = 0;
                isPeriodic = true;
            }
            
            CheckBattery(isPeriodic);
        }

        private void CheckBattery(bool isPeriodic)
        {
            var status = SystemInformation.PowerStatus;
            int percent = (int)(status.BatteryLifePercent * 100);
            bool isCharging = status.PowerLineStatus == PowerLineStatus.Online;

            bool shouldNotify = isPeriodic;

            // Trigger based on specific thresholds if not charging
            if (!isCharging)
            {
                int currentThreshold = -1;
                if (percent <= 10) currentThreshold = 10;
                else if (percent <= 50) currentThreshold = 50;
                else if (percent <= 70) currentThreshold = 70;

                if (currentThreshold != -1 && currentThreshold != _lastThreshold)
                {
                    _lastThreshold = currentThreshold;
                    shouldNotify = true;
                }
            }
            else
            {
                _lastThreshold = -1; // Reset when charging
            }

            if (shouldNotify)
            {
                OnBatteryStatusChanged?.Invoke(percent, isCharging, false, isPeriodic);
            }
        }
    }
}
