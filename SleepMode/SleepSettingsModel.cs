using System;

namespace ZeroMix.SleepMode
{
    [Flags]
    public enum TriggerMode
    {
        None = 0,
        Manual = 1,
        Idle = 2,
        Shortcut = 4
    }

    public class SleepSettingsModel
    {
        // Sekarang bisa kombinasi: Manual | Idle | Shortcut
        public TriggerMode Mode { get; set; } = TriggerMode.Idle;
        public int IdleThresholdSeconds { get; set; } = 60;
        public string ShortcutKey { get; set; } = "Alt+S";
        public bool HideNotifications { get; set; } = false;
        public bool DisableAnimations { get; set; } = false;
        
        // Exit Behaviors
        public bool ExitOnMouseMove { get; set; } = true;
        public bool ExitOnMouseDown { get; set; } = true;
        public bool ExitOnKeyDown { get; set; } = true;

        // Visuals
        public bool ShowClock { get; set; } = true;
        public bool ShowPixelCharacter { get; set; } = false;
        public double Brightness { get; set; } = 1.0; // Opacity 0.2 - 1.0
        
        // Advanced
        public bool GlowEffect { get; set; } = false;
        public bool AutoDisableOnLowBattery { get; set; } = true;

        public bool HasMode(TriggerMode mode) => (Mode & mode) == mode;

        public SleepSettingsModel Clone()
        {
            return (SleepSettingsModel)this.MemberwiseClone();
        }
    }
}
