using System;
using System.Windows;
using System.Windows.Input;

namespace ZeroMix.SleepMode
{
    public partial class SleepSettingsWindow : Window
    {
        public SleepSettingsModel? ResultSettings { get; private set; }

        public SleepSettingsWindow(SleepSettingsModel? initialSettings = null)
        {
            InitializeComponent();
            ApplySettingsToUI(initialSettings ?? new SleepSettingsModel());
        }

        private void ApplySettingsToUI(SleepSettingsModel settings)
        {
            // Trigger Mode (Flags — bisa multi-select)
            ManualModeChk.IsChecked = settings.HasMode(TriggerMode.Manual);
            IdleModeChk.IsChecked = settings.HasMode(TriggerMode.Idle);
            ShortcutModeChk.IsChecked = settings.HasMode(TriggerMode.Shortcut);
            IdleSecondsTxt.Text = settings.IdleThresholdSeconds.ToString();
            ShortcutTxt.Text = settings.ShortcutKey;

            // Exit Behavior
            ExitMouseMoveChk.IsChecked = settings.ExitOnMouseMove;
            ExitMouseDownChk.IsChecked = settings.ExitOnMouseDown;
            ExitKeyDownChk.IsChecked = settings.ExitOnKeyDown;

            // Visuals
            ShowClockChk.IsChecked = settings.ShowClock;
            ShowPixelCharChk.IsChecked = settings.ShowPixelCharacter;
            GlowEffectChk.IsChecked = settings.GlowEffect;
            BrightnessSld.Value = settings.Brightness;

            // Advanced
            HideNotificationsChk.IsChecked = settings.HideNotifications;
            DisableAnimsChk.IsChecked = settings.DisableAnimations;
            LowBatteryDisableChk.IsChecked = settings.AutoDisableOnLowBattery;
        }

        private SleepSettingsModel GetSettingsFromUI()
        {
            var settings = new SleepSettingsModel();

            // Trigger Mode (Flags — gabungkan semua yang dicentang)
            settings.Mode = TriggerMode.None;
            if (ManualModeChk.IsChecked == true) settings.Mode |= TriggerMode.Manual;
            if (IdleModeChk.IsChecked == true) settings.Mode |= TriggerMode.Idle;
            if (ShortcutModeChk.IsChecked == true) settings.Mode |= TriggerMode.Shortcut;

            if (int.TryParse(IdleSecondsTxt.Text, out int seconds))
                settings.IdleThresholdSeconds = seconds;

            settings.ShortcutKey = ShortcutTxt.Text;

            // Exit Behavior
            settings.ExitOnMouseMove = ExitMouseMoveChk.IsChecked ?? true;
            settings.ExitOnMouseDown = ExitMouseDownChk.IsChecked ?? true;
            settings.ExitOnKeyDown = ExitKeyDownChk.IsChecked ?? true;

            // Visuals
            settings.ShowClock = ShowClockChk.IsChecked ?? true;
            settings.ShowPixelCharacter = ShowPixelCharChk.IsChecked ?? false;
            settings.GlowEffect = GlowEffectChk.IsChecked ?? false;
            settings.Brightness = BrightnessSld.Value;

            // Advanced
            settings.HideNotifications = HideNotificationsChk.IsChecked ?? false;
            settings.DisableAnimations = DisableAnimsChk.IsChecked ?? false;
            settings.AutoDisableOnLowBattery = LowBatteryDisableChk.IsChecked ?? true;

            return settings;
        }

        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            ResultSettings = GetSettingsFromUI();
            this.DialogResult = true;
            this.Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }
    }
}
