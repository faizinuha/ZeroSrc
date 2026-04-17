using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

using RadioButton = System.Windows.Controls.RadioButton;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace ZeroMix.Plugins.Welcome
{
    public partial class WelcomePluginUI : System.Windows.Controls.UserControl
    {
        private readonly DispatcherTimer _statusTimer;
        private bool _isExpanded = false;

        public WelcomePluginUI()
        {
            InitializeComponent();
            _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _statusTimer.Tick += (s, e) => UpdateStatus();
            _statusTimer.Start();
            LoadCurrentMode();
            UpdateStatus();
        }

        private void LoadCurrentMode()
        {
            var mode = WelcomePlugin.GetWelcomeMode();
            switch (mode)
            {
                case WelcomePlugin.WelcomeMode.FreshBootOnly: FreshBootRadio.IsChecked = true; break;
                case WelcomePlugin.WelcomeMode.EveryStart:    EveryStartRadio.IsChecked = true; break;
                case WelcomePlugin.WelcomeMode.Disabled:      DisabledRadio.IsChecked = true;   break;
            }
            AutoStartCheckbox.IsChecked = WelcomePlugin.IsAutoStartEnabled();
            ApplyCardHighlight(mode);
        }

        private void UpdateStatus()
        {
            try
            {
                var mode = WelcomePlugin.GetWelcomeMode();
                double uptimeMin = Environment.TickCount64 / 1000.0 / 60.0;
                UptimeLabel.Text = uptimeMin < 60
                    ? $"{uptimeMin:F0}m uptime"
                    : $"{uptimeMin / 60:F1}h uptime";
                StatusLabel.Text = mode switch
                {
                    WelcomePlugin.WelcomeMode.FreshBootOnly => "Fresh Boot Only",
                    WelcomePlugin.WelcomeMode.EveryStart    => "Every Start",
                    WelcomePlugin.WelcomeMode.Disabled      => "Disabled",
                    _ => "Unknown"
                };
            }
            catch { }
        }

        // Card click handlers
        private void FreshBootCard_Click(object sender, MouseButtonEventArgs e)
        {
            FreshBootRadio.IsChecked = true;
        }

        private void EveryStartCard_Click(object sender, MouseButtonEventArgs e)
        {
            EveryStartRadio.IsChecked = true;
        }

        private void DisabledCard_Click(object sender, MouseButtonEventArgs e)
        {
            DisabledRadio.IsChecked = true;
        }

        private void ModeRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is not RadioButton radio || radio.IsChecked != true) return;
            try
            {
                WelcomePlugin.WelcomeMode newMode;
                if (radio == FreshBootRadio)      newMode = WelcomePlugin.WelcomeMode.FreshBootOnly;
                else if (radio == EveryStartRadio) newMode = WelcomePlugin.WelcomeMode.EveryStart;
                else if (radio == DisabledRadio)   newMode = WelcomePlugin.WelcomeMode.Disabled;
                else return;

                WelcomePlugin.SetWelcomeMode(newMode);
                ApplyCardHighlight(newMode);
                UpdateStatus();
            }
            catch { }
        }

        private void ApplyCardHighlight(WelcomePlugin.WelcomeMode mode)
        {
            var accent     = (Brush)FindResource("NeonBlueBrush");
            var borderDim  = new SolidColorBrush(Color.FromRgb(22, 28, 40));
            var bgSelected = new SolidColorBrush(Color.FromRgb(17, 22, 32));
            var bgNormal   = new SolidColorBrush(Color.FromRgb(17, 22, 32));

            if (CardFreshBoot  != null) { CardFreshBoot.BorderBrush  = mode == WelcomePlugin.WelcomeMode.FreshBootOnly ? accent : borderDim; CardFreshBoot.BorderThickness  = mode == WelcomePlugin.WelcomeMode.FreshBootOnly ? new Thickness(1.5) : new Thickness(1); }
            if (CardEveryStart != null) { CardEveryStart.BorderBrush = mode == WelcomePlugin.WelcomeMode.EveryStart    ? accent : borderDim; CardEveryStart.BorderThickness = mode == WelcomePlugin.WelcomeMode.EveryStart    ? new Thickness(1.5) : new Thickness(1); }
            if (CardDisabled   != null) { CardDisabled.BorderBrush   = mode == WelcomePlugin.WelcomeMode.Disabled      ? accent : borderDim; CardDisabled.BorderThickness   = mode == WelcomePlugin.WelcomeMode.Disabled      ? new Thickness(1.5) : new Thickness(1); }
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            try { new WelcomeWindow().Show(); } catch { }
        }

        private void AutoStartCheckbox_Changed(object sender, RoutedEventArgs e)
        {
            try { WelcomePlugin.SetAutoStart(AutoStartCheckbox.IsChecked == true); } catch { }
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _isExpanded = !_isExpanded;
            if (_isExpanded)
            {
                ((Storyboard)Resources["ExpandAnim"]).Begin();
                IconRotation.BeginAnimation(RotateTransform.AngleProperty,
                    new DoubleAnimation(0, 180, TimeSpan.FromMilliseconds(250)));
            }
            else
            {
                ((Storyboard)Resources["CollapseAnim"]).Begin();
                IconRotation.BeginAnimation(RotateTransform.AngleProperty,
                    new DoubleAnimation(180, 0, TimeSpan.FromMilliseconds(200)));
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e) => _statusTimer?.Stop();
    }
}
