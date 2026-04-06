using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace ZeroMix.SleepMode
{
    public partial class SleepSettingsWindow : Window
    {
        public SleepSettingsModel? ResultSettings { get; private set; }
        private AodStyle _selectedStyle = AodStyle.MinimalClock;

        public SleepSettingsWindow(SleepSettingsModel? initialSettings = null)
        {
            InitializeComponent();
            ApplySettingsToUI(initialSettings ?? new SleepSettingsModel());
        }

        private void ApplySettingsToUI(SleepSettingsModel settings)
        {
            ManualModeChk.IsChecked  = settings.HasMode(TriggerMode.Manual);
            IdleModeChk.IsChecked    = settings.HasMode(TriggerMode.Idle);
            ShortcutModeChk.IsChecked = settings.HasMode(TriggerMode.Shortcut);
            IdleSecondsTxt.Text      = settings.IdleThresholdSeconds.ToString();
            ShortcutTxt.Text         = settings.ShortcutKey;

            ExitMouseMoveChk.IsChecked = settings.ExitOnMouseMove;
            ExitMouseDownChk.IsChecked = settings.ExitOnMouseDown;
            ExitKeyDownChk.IsChecked   = settings.ExitOnKeyDown;

            BrightnessSld.Value = settings.Brightness;
            _selectedStyle      = settings.Style;
            SelectStyleCard(_selectedStyle);
        }

        private SleepSettingsModel GetSettingsFromUI()
        {
            var s = new SleepSettingsModel();

            s.Mode = TriggerMode.None;
            if (ManualModeChk.IsChecked  == true) s.Mode |= TriggerMode.Manual;
            if (IdleModeChk.IsChecked    == true) s.Mode |= TriggerMode.Idle;
            if (ShortcutModeChk.IsChecked == true) s.Mode |= TriggerMode.Shortcut;

            if (int.TryParse(IdleSecondsTxt.Text, out int sec)) s.IdleThresholdSeconds = sec;
            s.ShortcutKey = ShortcutTxt.Text;

            s.ExitOnMouseMove = ExitMouseMoveChk.IsChecked ?? true;
            s.ExitOnMouseDown = ExitMouseDownChk.IsChecked ?? true;
            s.ExitOnKeyDown   = ExitKeyDownChk.IsChecked   ?? true;

            s.Style      = _selectedStyle;
            s.Brightness = BrightnessSld.Value;
            s.AutoDisableOnLowBattery = true;

            return s;
        }

        private void StyleCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border b && b.Tag is string tag &&
                System.Enum.TryParse<AodStyle>(tag, out var style))
            {
                _selectedStyle = style;
                SelectStyleCard(style);
            }
        }

        private void SelectStyleCard(AodStyle style)
        {
            var neon   = (Brush)FindResource("NeonBlueBrush");
            var border = new SolidColorBrush(Color.FromRgb(26, 32, 48));

            var cards = new[]
            {
                (StyleCardMinimal, AodStyle.MinimalClock),
                (StyleCardGlow,    AodStyle.DigitalGlow),
                (StyleCardAnalog,  AodStyle.Analog),
                (StyleCardDate,    AodStyle.DateFocus),
                (StyleCardBlank,   AodStyle.Blank),
            };

            foreach (var (card, s) in cards)
            {
                if (card == null) continue;
                card.BorderBrush     = s == style ? neon : border;
                card.BorderThickness = s == style ? new Thickness(2) : new Thickness(1);
            }
        }

        private void BrightnessSld_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (BrightnessLabel != null)
                BrightnessLabel.Text = $" — {(int)(e.NewValue * 100)}%";
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
            if (e.LeftButton == MouseButtonState.Pressed) this.DragMove();
        }
    }
}
