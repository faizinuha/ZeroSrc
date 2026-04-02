using System;
using System.Windows.Threading;

namespace ZeroMix.SleepMode
{
    public class SleepManager
    {
        private DispatcherTimer _idleCheckTimer;
        private SleepOverlayWindow? _overlayWindow;
        private bool _isEnabled = true;
        private IntPtr _hotkeyHandle = IntPtr.Zero;
        private const int SLEEP_HOTKEY_ID = 9500;
        
        public SleepSettingsModel Settings { get; private set; } = new SleepSettingsModel();

        public SleepManager()
        {
            _idleCheckTimer = new DispatcherTimer();
            _idleCheckTimer.Interval = TimeSpan.FromSeconds(1);
            _idleCheckTimer.Tick += IdleCheckTimer_Tick;
        }

        public void Start()
        {
            _isEnabled = true;
            _idleCheckTimer.Start();
        }

        public void Stop()
        {
            _isEnabled = false;
            _idleCheckTimer.Stop();
            UnregisterHotkey();
            CloseOverlay();
        }

        public void SetHotkeyHandle(IntPtr handle)
        {
            _hotkeyHandle = handle;
            if (Settings.HasMode(TriggerMode.Shortcut))
                RegisterHotkey();
        }

        public void ApplySettings(SleepSettingsModel settings)
        {
            Settings = settings;
            
            // 1. Manage Timer (aktif jika mode Idle atau perlu cek baterai)
            if (Settings.HasMode(TriggerMode.Idle) || Settings.AutoDisableOnLowBattery)
                Start();
            else
                _idleCheckTimer.Stop();

            // 2. Manage Hotkey
            if (Settings.HasMode(TriggerMode.Shortcut))
                RegisterHotkey();
            else
                UnregisterHotkey();
        }

        private void RegisterHotkey()
        {
            if (_hotkeyHandle == IntPtr.Zero) return;
            
            UnregisterHotkey(); // Clear current

            if (ZeroMix.Hotkeys.HotkeyParser.TryParse(Settings.ShortcutKey, out uint modifiers, out uint vk))
            {
                ZeroMix.Hotkeys.HotkeyManager.Register(_hotkeyHandle, SLEEP_HOTKEY_ID, modifiers, vk, () => ShowOverlay());
            }
        }

        private void UnregisterHotkey()
        {
            if (_hotkeyHandle != IntPtr.Zero)
            {
                ZeroMix.Hotkeys.HotkeyManager.Unregister(_hotkeyHandle, SLEEP_HOTKEY_ID);
            }
        }

        private void IdleCheckTimer_Tick(object? sender, EventArgs e)
        {
            if (!_isEnabled) return;

            // 1. Cek Baterai (jika diaktifkan)
            if (Settings.AutoDisableOnLowBattery && IsBatteryLow())
            {
                CloseOverlay();
                return;
            }

            // 2. Cek Idle (jika mode Idle dicentang)
            if (Settings.HasMode(TriggerMode.Idle))
            {
                // Jika overlay sudah ada, tidak perlu cek lagi
                if (_overlayWindow != null && _overlayWindow.IsVisible) return;

                TimeSpan idleTime = IdleDetector.GetIdleTime();
                if (idleTime.TotalSeconds >= Settings.IdleThresholdSeconds)
                {
                    ShowOverlay();
                }
            }
        }

        private bool IsBatteryLow()
        {
            try
            {
                var power = System.Windows.Forms.SystemInformation.PowerStatus;
                // Anggap rendah jika di bawah 20% dan tidak sedang dicharge
                return power.BatteryLifePercent < 0.20 && power.PowerLineStatus == System.Windows.Forms.PowerLineStatus.Offline;
            }
            catch { return false; }
        }

        public void ShowOverlay()
        {
            try
            {
                if (_overlayWindow != null)
                {
                    _overlayWindow.Close();
                }

                _overlayWindow = new SleepOverlayWindow(Settings);
                _overlayWindow.Closed += (s, ev) => _overlayWindow = null;
                _overlayWindow.Show();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to show Sleep Overlay: {ex.Message}");
            }
        }

        public void CloseOverlay()
        {
            if (_overlayWindow != null)
            {
                _overlayWindow.WakeUp();
            }
        }

        public void TestSleep()
        {
            ShowOverlay();
        }
    }
}
