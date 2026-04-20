using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

using TextBox      = System.Windows.Controls.TextBox;
using UserControl  = System.Windows.Controls.UserControl;
using Button       = System.Windows.Controls.Button;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace ZeroMix.Plugins.ChargerNotif
{
    public partial class ChargerNotifUI : UserControl
    {
        private readonly ChargerNotifPlugin _plugin = new();

        private Dictionary<string, TextBox> _soundBoxes = null!;

        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ZeroMix", "charger_notif_config.json");

        private static readonly string BuiltInSoundDir = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Tools", "Plugins", "ChargerBatterynotif.core", "sounds");

        public ChargerNotifUI()
        {
            InitializeComponent();

            _soundBoxes = new()
            {
                ["Charging"] = SndCharging,
                ["Unplug"]   = SndUnplug,
                ["Full"]     = SndFull,
            };

            TxtCharging.Text  = _plugin.MsgCharging;
            TxtUnplugged.Text = _plugin.MsgUnplugged;
            TxtFull.Text      = _plugin.MsgFull;

            LoadConfig();

            // Restore toggle state
            var state = PluginStateManager.Load();
            ToggleSwitch.IsChecked = state.ChargerNotif;
            if (state.ChargerNotif) _plugin.Start();
        }

        private void ToggleSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (ToggleSwitch.IsChecked == true) _plugin.Start();
            else _plugin.Stop();

            // Simpan state
            var state = PluginStateManager.Load();
            state.ChargerNotif = ToggleSwitch.IsChecked == true;
            PluginStateManager.Save(state);
        }

        private void SettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            SettingsPanel.Visibility = SettingsPanel.Visibility == Visibility.Visible
                ? Visibility.Collapsed : Visibility.Visible;
        }

        // ── Browse: buka file dialog pilih .wav ────────────────────────────
        private void BrowseSound_Click(object sender, RoutedEventArgs e)
        {
            string key = ((Button)sender).Tag?.ToString() ?? "";
            if (!_soundBoxes.TryGetValue(key, out var box)) return;

            var dlg = new OpenFileDialog
            {
                Title           = $"Pilih suara untuk: {GetEventLabel(key)}",
                Filter          = "Audio WAV (*.wav)|*.wav|Semua file (*.*)|*.*",
                CheckFileExists = true,
            };

            if (!string.IsNullOrEmpty(box.Text) && File.Exists(box.Text))
                dlg.InitialDirectory = Path.GetDirectoryName(box.Text);

            if (dlg.ShowDialog() == true)
            {
                box.Text       = dlg.FileName;
                box.Foreground = System.Windows.Media.Brushes.White;
                ApplySound(key, dlg.FileName);
            }
        }

        // ── Preview: putar suara yang dipilih (atau bawaan) ────────────────
        private void PreviewSound_Click(object sender, RoutedEventArgs e)
        {
            string key = ((Button)sender).Tag?.ToString() ?? "";
            if (!_soundBoxes.TryGetValue(key, out var box)) return;

            string? custom = string.IsNullOrEmpty(box.Text) ? null : box.Text;
            PlayPreview(key, custom);
        }

        // ── Reset: hapus custom → kembali ke bawaan ────────────────────────
        private void ResetSound_Click(object sender, RoutedEventArgs e)
        {
            string key = ((Button)sender).Tag?.ToString() ?? "";
            if (!_soundBoxes.TryGetValue(key, out var box)) return;

            box.Text       = "";
            box.Foreground = (System.Windows.Media.Brush)FindResource("SubTextBrush");
            ApplySound(key, null);
        }

        // ── Simpan semua settings ──────────────────────────────────────────
        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            _plugin.MsgCharging  = TxtCharging.Text;
            _plugin.MsgUnplugged = TxtUnplugged.Text;
            _plugin.MsgFull      = TxtFull.Text;

            foreach (var kv in _soundBoxes)
                ApplySound(kv.Key, string.IsNullOrEmpty(kv.Value.Text) ? null : kv.Value.Text);

            SaveConfig();
            SettingsPanel.Visibility = Visibility.Collapsed;
        }

        // ── Test: tampilkan preview notif charging ─────────────────────────
        private void TestBtn_Click(object sender, RoutedEventArgs e)
        {
            new BatteryNotifWindow(
                BatteryNotifWindow.NotifType.Charging, 75,
                TxtCharging.Text, null,
                string.IsNullOrEmpty(SndCharging.Text) ? null : SndCharging.Text
            ).Show();
        }

        // ── Helpers ────────────────────────────────────────────────────────
        private void ApplySound(string key, string? path)
        {
            switch (key)
            {
                case "Charging": _plugin.CustomSoundCharging = path; break;
                case "Unplug":   _plugin.CustomSoundUnplug   = path; break;
                case "Full":     _plugin.CustomSoundFull     = path; break;
            }
        }

        private static string GetEventLabel(string key) => key switch
        {
            "Charging" => "Charger Dicolok ⚡",
            "Unplug"   => "Charger Dicabut 🔌",
            "Full"     => "Baterai Penuh 🎉",
            _          => key
        };

        private static void PlayPreview(string key, string? customPath)
        {
            // Jalankan di background thread agar tidak block UI
            Task.Run(() =>
            {
                try
                {
                    if (!string.IsNullOrEmpty(customPath) && File.Exists(customPath))
                    {
                        using var p = new System.Media.SoundPlayer(customPath);
                        p.PlaySync();
                        return;
                    }

                    string wavFile = key switch
                    {
                        "Charging" => "charging.wav",
                        "Unplug"   => "unplug.wav",
                        "Full"     => "full.wav",
                        _          => ""
                    };

                    string wavPath = Path.Combine(BuiltInSoundDir, wavFile);
                    if (File.Exists(wavPath))
                    {
                        using var p = new System.Media.SoundPlayer(wavPath);
                        p.PlaySync();
                    }
                    else
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(
                            () => System.Media.SystemSounds.Asterisk.Play());
                    }
                }
                catch { }
            });
        }

        // ── Persist config ─────────────────────────────────────────────────
        private void SaveConfig()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
                var cfg = new ConfigData
                {
                    MsgCharging  = TxtCharging.Text,
                    MsgUnplugged = TxtUnplugged.Text,
                    MsgFull      = TxtFull.Text,
                    SndCharging  = SndCharging.Text,
                    SndUnplug    = SndUnplug.Text,
                    SndFull      = SndFull.Text,
                };
                File.WriteAllText(ConfigPath,
                    JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        private void LoadConfig()
        {
            try
            {
                if (!File.Exists(ConfigPath)) return;
                var cfg = JsonSerializer.Deserialize<ConfigData>(File.ReadAllText(ConfigPath));
                if (cfg == null) return;

                if (!string.IsNullOrEmpty(cfg.MsgCharging))  TxtCharging.Text  = cfg.MsgCharging;
                if (!string.IsNullOrEmpty(cfg.MsgUnplugged)) TxtUnplugged.Text = cfg.MsgUnplugged;
                if (!string.IsNullOrEmpty(cfg.MsgFull))      TxtFull.Text      = cfg.MsgFull;

                SetSoundBox(SndCharging, "Charging", cfg.SndCharging);
                SetSoundBox(SndUnplug,   "Unplug",   cfg.SndUnplug);
                SetSoundBox(SndFull,     "Full",     cfg.SndFull);
            }
            catch { }
        }

        private void SetSoundBox(TextBox box, string key, string? path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
            box.Text       = path;
            box.Foreground = System.Windows.Media.Brushes.White;
            ApplySound(key, path);
        }

        private class ConfigData
        {
            public string? MsgCharging  { get; set; }
            public string? MsgUnplugged { get; set; }
            public string? MsgFull      { get; set; }
            public string? SndCharging  { get; set; }
            public string? SndUnplug    { get; set; }
            public string? SndFull      { get; set; }
        }
    }
}
