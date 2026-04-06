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

    public enum AodStyle
    {
        MinimalClock,    // Jam besar, teks putih tipis, bergerak pelan (default)
        DigitalGlow,     // Jam dengan efek neon glow + partikel
        Analog,          // Jam analog minimalis
        DateFocus,       // Tanggal besar + jam kecil di bawah
        Blank            // Layar hitam total, tidak ada elemen
    }

    public class SleepSettingsModel
    {
        public TriggerMode Mode { get; set; } = TriggerMode.Idle;
        public int IdleThresholdSeconds { get; set; } = 60;
        public string ShortcutKey { get; set; } = "Alt+S";
        public bool HideNotifications { get; set; } = false;
        public bool DisableAnimations { get; set; } = false;

        // Exit Behaviors
        public bool ExitOnMouseMove { get; set; } = true;
        public bool ExitOnMouseDown { get; set; } = true;
        public bool ExitOnKeyDown { get; set; } = true;

        // AOD Style
        public AodStyle Style { get; set; } = AodStyle.MinimalClock;
        public double Brightness { get; set; } = 1.0;

        // Advanced
        public bool AutoDisableOnLowBattery { get; set; } = true;

        public bool HasMode(TriggerMode mode) => (Mode & mode) == mode;

        public SleepSettingsModel Clone() => (SleepSettingsModel)this.MemberwiseClone();
    }
}
