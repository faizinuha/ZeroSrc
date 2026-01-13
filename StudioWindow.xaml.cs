using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;

namespace ZeroMix
{
    public partial class StudioWindow : Window
    {
        private string? _selectedVideoPath;
        private string? _selectedMusicPath;
        private string? _customWatermarkPath;
        private DispatcherTimer _timer;

        public StudioWindow()
        {
            InitializeComponent();
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(500);
            _timer.Tick += Timer_Tick;
            
            // Link event
            WatermarkTypeSelector.SelectionChanged += WatermarkType_Changed;
        }

        private void WatermarkType_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (WatermarkTypeSelector.SelectedItem is ComboBoxItem selected)
            {
                string tag = selected.Tag?.ToString() ?? "None";
                CustomWatermarkBtn.Visibility = (tag == "Custom") ? Visibility.Visible : Visibility.Collapsed;
                
                if (tag == "None") WatermarkOverlay.Visibility = Visibility.Collapsed;
                else {
                    WatermarkOverlay.Visibility = Visibility.Visible;
                    WatermarkOverlay.Text = (tag == "Default") ? "ZeroMix Studio" : (string.IsNullOrEmpty(_customWatermarkPath) ? "Your Brand" : Path.GetFileName(_customWatermarkPath));
                }
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (VideoPreview.NaturalDuration.HasTimeSpan && !TimelineSlider.IsMouseCaptureWithin)
            {
                TimelineSlider.Value = VideoPreview.Position.TotalSeconds;
                UpdateDurationLabel();
            }
        }

        private void UpdateDurationLabel()
        {
            if (VideoPreview.NaturalDuration.HasTimeSpan)
            {
                DurationText.Text = $"{VideoPreview.Position:mm\\:ss} / {VideoPreview.NaturalDuration.TimeSpan:mm\\:ss}";
            }
        }

        private void ImportVideo_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Video files (*.mp4;*.avi;*.mov)|*.mp4;*.avi;*.mov";
            if (openFileDialog.ShowDialog() == true)
            {
                _selectedVideoPath = openFileDialog.FileName;
                FilePathLabel.Text = Path.GetFileName(_selectedVideoPath);
                VideoPreview.Source = new Uri(_selectedVideoPath);
                VideoPreview.Play();
                VideoPreview.Pause(); // Pause to let user seek
                
                _timer.Start();

                // Setup initial trim values
                VideoPreview.MediaOpened += (s, ev) => {
                    if (VideoPreview.NaturalDuration.HasTimeSpan)
                    {
                        TimelineSlider.Maximum = VideoPreview.NaturalDuration.TimeSpan.TotalSeconds;
                        EndTimeTxt.Text = VideoPreview.NaturalDuration.TimeSpan.ToString("mm\\:ss");
                        StartTimeTxt.Text = "00:00";
                    }
                };
            }
        }

        private void SelectMusic_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Audio files (*.mp3;*.wav;*.m4a)|*.mp3;*.wav;*.m4a";
            if (openFileDialog.ShowDialog() == true)
            {
                _selectedMusicPath = openFileDialog.FileName;
                MusicInfoLabel.Text = "♫ " + Path.GetFileName(_selectedMusicPath);
                System.Windows.MessageBox.Show("Music applied to project: " + Path.GetFileName(_selectedMusicPath));
            }
        }

        private void YTStudioMusic_Click(object sender, RoutedEventArgs e)
        {
            // Open YouTube Studio Audio Library in browser
            string url = "https://studio.youtube.com/channel/UC/music";
            try 
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                System.Windows.MessageBox.Show("Membuka Youtube Studio Audio Library...\nSilakan download musik favorit Kakak dan 'Import' menggunakan tombol Local File.", "YouTube Studio Integration");
            }
            catch { }
        }

        private void CustomWatermarkBtn_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpg;*.bmp)|*.png;*.jpg;*.bmp";
            if (openFileDialog.ShowDialog() == true)
            {
                _customWatermarkPath = openFileDialog.FileName;
                WatermarkOverlay.Text = "BRAND: " + Path.GetFileName(_customWatermarkPath);
                System.Windows.MessageBox.Show("Custom Watermark Loaded: " + Path.GetFileName(_customWatermarkPath));
            }
        }

        private void TimelineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (VideoPreview.Source != null)
            {
                VideoPreview.Position = TimeSpan.FromSeconds(TimelineSlider.Value);
            }
        }

        private void TogglePlay_Click(object sender, RoutedEventArgs e)
        {
            if (VideoPreview.Source == null) return;

            if (PlayToggleBtn.Content.ToString() == "▶")
            {
                VideoPreview.Play();
                PlayToggleBtn.Content = "⏸";
            }
            else
            {
                VideoPreview.Pause();
                PlayToggleBtn.Content = "▶";
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedVideoPath))
            {
                System.Windows.MessageBox.Show("Please import a video first!", "Studio Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string wmType = (WatermarkTypeSelector.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "None";

            // Simple logic: In the real app, we would build the FFMPEG command here.
            // For now, we show the intention to keep UI light.
            System.Windows.MessageBox.Show("Prepare to Render...\n\nFitur Eksport akan menggunakan FFmpeg untuk memproses:\n- Trim\n- Volume: " + VolumeSlider.Value + "%\n- Watermark: " + wmType + "\n- Filter: " + (FilterSelector.SelectedItem as ComboBoxItem)?.Content, 
                "Studio Engine (FFmpeg Context)", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
