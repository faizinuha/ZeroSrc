using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Windows.Media.Imaging;
using System.Text;

namespace ZeroMix
{
    public partial class StudioWindow : Window
    {
        private string? _selectedVideoPath;
        private string? _selectedMusicPath;
        private string? _customWatermarkPath;
        private DispatcherTimer _timer;

        // Model for Visual Storyboard
        public class VideoClip
        {
            public string Path { get; set; } = "";
            public string FileName { get; set; } = "";
            public string DurationStr { get; set; } = "00:00";
            public TimeSpan TotalDuration { get; set; }
            public TimeSpan StartTime { get; set; }
            public TimeSpan EndTime { get; set; }
            public System.Windows.Media.ImageSource? Thumbnail { get; set; }
        }

        // Model for Pixabay Assets
        public class PixabayAsset
        {
            public string Tags { get; set; } = "";
            public int Duration { get; set; }
            public string DurationStr => $"{Duration / 60:00}:{Duration % 60:00}";
            public string VideoUrl { get; set; } = "";
            public string PreviewImage { get; set; } = "";
        }

        // API Key is now moved to ApiKeys.cs (Excluded from Git)
        private string PIXABAY_KEY => ApiKeys.PixabayKey;

        public StudioWindow()
        {
            InitializeComponent();
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(50); // Faster update for smooth movement
            _timer.Tick += Timer_Tick;
        }

        private void Sidebar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is string tab)
            {
                // Hide all
                StoryboardView.Visibility = Visibility.Collapsed;
                LibraryPanel.Visibility = Visibility.Collapsed;
                EffectsPanel.Visibility = Visibility.Collapsed;

                // Reset icons opacity (use explicit button names)
                MediaTabBtn.Opacity = 0.5; LibraryTabBtn.Opacity = 0.5; EffectsTabBtn.Opacity = 0.5;
                MediaTabBtn.Foreground = System.Windows.Media.Brushes.White;
                LibraryTabBtn.Foreground = System.Windows.Media.Brushes.White;
                EffectsTabBtn.Foreground = System.Windows.Media.Brushes.White;

                // Show selected
                switch (tab)
                {
                    case "Media":
                        StoryboardView.Visibility = Visibility.Visible;
                        MediaTabBtn.Opacity = 1; MediaTabBtn.Foreground = (System.Windows.Media.Brush)FindResource("AccentBrush");
                        break;
                    case "Stock":
                        LibraryPanel.Visibility = Visibility.Visible;
                        LibraryTabBtn.Opacity = 1; LibraryTabBtn.Foreground = (System.Windows.Media.Brush)FindResource("AccentBrush");
                        if (PixabayMusicList.ItemsSource == null) _ = SearchPixabay("lofi hip hop");
                        break;
                    case "Effects":
                        EffectsPanel.Visibility = Visibility.Visible;
                        EffectsTabBtn.Opacity = 1; EffectsTabBtn.Foreground = (System.Windows.Media.Brush)FindResource("AccentBrush");
                        break;
                }
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void RealTimeFilters_Changed(object sender, EventArgs e)
        {
            if (BrightnessOverlay == null || ContrastOverlay == null || FilterOverlay == null || 
                BrightnessSlider == null || ContrastSlider == null || FilterSelector == null) return;

            // Brightness Simulation (0 is normal)
            double bVal = BrightnessSlider.Value;
            if (bVal >= 0) {
                BrightnessOverlay.Background = System.Windows.Media.Brushes.White;
                BrightnessOverlay.Opacity = bVal * 0.4;
            } else {
                BrightnessOverlay.Background = System.Windows.Media.Brushes.Black;
                BrightnessOverlay.Opacity = Math.Abs(bVal) * 0.6;
            }

            // Contrast Simulation
            double cVal = ContrastSlider.Value; // 0 to 2, 1 is normal
            ContrastOverlay.Opacity = cVal < 1 ? (1 - cVal) * 0.4 : 0;

            // Filter Simulation
            string filter = (FilterSelector.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "No Filter";
            FilterOverlay.Opacity = filter == "No Filter" ? 0 : 0.35;
            
            if (filter == "Cinematic") FilterOverlay.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(60, 255, 120, 0));
            else if (filter == "Cyberpunk") FilterOverlay.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(60, 0, 180, 255));
            else if (filter == "Greyscale") {
                FilterOverlay.Background = System.Windows.Media.Brushes.DimGray;
                FilterOverlay.Opacity = 0.6;
            }
        }


        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (VideoPreview.NaturalDuration.HasTimeSpan)
            {
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
            openFileDialog.Filter = "Video files (*.mp4;*.avi;*.mov;*.mkv)|*.mp4;*.avi;*.mov;*.mkv";
            openFileDialog.Multiselect = true;

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filename in openFileDialog.FileNames)
                {
                    var clip = new VideoClip { 
                        Path = filename, 
                        FileName = Path.GetFileName(filename),
                        DurationStr = "...", 
                        Thumbnail = GetVideoThumbnail(filename)
                    };
                    VideoQueueList.Items.Add(clip);
                    
                    // Sync to Timeline Track
                    TimelineTrack.Items.Add(clip);

                    if (string.IsNullOrEmpty(_selectedVideoPath)) 
                        LoadVideoToPreview(filename);
                }
            }
        }

        private void LoadVideoToPreview(string path)
        {
            _selectedVideoPath = path;
            VideoPreview.Source = new System.Uri(_selectedVideoPath);
            VideoPreview.Play();
            VideoPreview.Pause();
            _timer.Start();

            // Clear old handler to prevent stacking
            VideoPreview.MediaOpened -= OnMediaOpened;
            VideoPreview.MediaOpened += OnMediaOpened;
        }

        private void OnMediaOpened(object sender, RoutedEventArgs e)
        {
            if (VideoPreview.NaturalDuration.HasTimeSpan)
            {
                var duration = VideoPreview.NaturalDuration.TimeSpan;

                // Update the clip data if selected
                if (VideoQueueList.SelectedItem is VideoClip clip)
                {
                    clip.TotalDuration = duration;
                    clip.EndTime = duration;
                    clip.DurationStr = duration.ToString("mm\\:ss");
                    VideoQueueList.Items.Refresh();
                    TimelineTrack.Items.Refresh();
                }
            }
        }

        private void VideoQueueList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VideoQueueList.SelectedItem is VideoClip clip)
            {
                LoadVideoToPreview(clip.Path);
            }
        }

        private void ClearQueue_Click(object sender, RoutedEventArgs e)
        {
            VideoQueueList.Items.Clear();
            TimelineTrack.Items.Clear();
            VideoPreview.Source = null;
            _selectedVideoPath = null;
            DurationText.Text = "00:00 / 00:00";
        }

        private void RemoveFromQueue_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            var clip = btn?.Tag as VideoClip;
            if (clip != null)
            {
                VideoQueueList.Items.Remove(clip);
                if (clip.Path == _selectedVideoPath)
                {
                    VideoPreview.Source = null;
                    _selectedVideoPath = null;
                }
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

        // YT music engine removed to keep it lightweight. 
        // Pixabay Stock Library integration
        private void ToggleLibrary_Click(object sender, RoutedEventArgs e)
        {
            if (LibraryPanel.Visibility == Visibility.Visible)
            {
                LibraryPanel.Visibility = Visibility.Collapsed;
                StoryboardView.Visibility = Visibility.Visible;
            }
            else
            {
                LibraryPanel.Visibility = Visibility.Visible;
                StoryboardView.Visibility = Visibility.Collapsed;
                // Search default if list is empty
                if (PixabayMusicList.ItemsSource == null)
                    _ = SearchPixabay("lofi hip hop");
            }
        }

        // Timeline Editing Functions
        private void TimelineClip_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is VideoClip clip)
            {
                VideoQueueList.SelectedItem = clip;
                LoadVideoToPreview(clip.Path);
            }
        }

        private void CutClip_Click(object sender, RoutedEventArgs e)
        {
            if (VideoQueueList.SelectedItem is VideoClip clip)
            {
                // Split at current playback position
                var currentPos = VideoPreview.Position;
                if (currentPos > TimeSpan.Zero && currentPos < clip.TotalDuration)
                {
                    // Create second half
                    var newClip = new VideoClip
                    {
                        Path = clip.Path,
                        FileName = clip.FileName + " (Part 2)",
                        StartTime = currentPos,
                        EndTime = clip.EndTime,
                        TotalDuration = clip.TotalDuration,
                        Thumbnail = clip.Thumbnail
                    };
                    newClip.DurationStr = (newClip.EndTime - newClip.StartTime).ToString("mm\\:ss");

                    // Update first half
                    clip.EndTime = currentPos;
                    clip.DurationStr = (clip.EndTime - clip.StartTime).ToString("mm\\:ss");

                    // Insert after current
                    int idx = VideoQueueList.Items.IndexOf(clip);
                    VideoQueueList.Items.Insert(idx + 1, newClip);
                    TimelineTrack.Items.Insert(idx + 1, newClip);

                    VideoQueueList.Items.Refresh();
                    TimelineTrack.Items.Refresh();
                }
            }
        }

        private void CopyClip_Click(object sender, RoutedEventArgs e)
        {
            if (VideoQueueList.SelectedItem is VideoClip clip)
            {
                var duplicate = new VideoClip
                {
                    Path = clip.Path,
                    FileName = clip.FileName + " (Copy)",
                    StartTime = clip.StartTime,
                    EndTime = clip.EndTime,
                    TotalDuration = clip.TotalDuration,
                    DurationStr = clip.DurationStr,
                    Thumbnail = clip.Thumbnail
                };

                int idx = VideoQueueList.Items.IndexOf(clip);
                VideoQueueList.Items.Insert(idx + 1, duplicate);
                TimelineTrack.Items.Insert(idx + 1, duplicate);
            }
        }

        private void DeleteClip_Click(object sender, RoutedEventArgs e)
        {
            if (VideoQueueList.SelectedItem is VideoClip clip)
            {
                VideoQueueList.Items.Remove(clip);
                TimelineTrack.Items.Remove(clip);
            }
        }

        private void JumpStart_Click(object sender, RoutedEventArgs e)
        {
            if (VideoPreview.Source != null)
            {
                VideoPreview.Position = TimeSpan.Zero;
            }
        }

        private void JumpEnd_Click(object sender, RoutedEventArgs e)
        {
            if (VideoPreview.NaturalDuration.HasTimeSpan)
            {
                VideoPreview.Position = VideoPreview.NaturalDuration.TimeSpan;
            }
        }

        private void Volume_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (VideoPreview != null && VolumeSlider != null && VolumeText != null)
            {
                VideoPreview.Volume = VolumeSlider.Value / 100.0;
                VolumeText.Text = $"{(int)VolumeSlider.Value}%";
            }
        }

        private async void SearchPixabay_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is string keyword)
            {
                await SearchPixabay(keyword);
            }
        }

        private void MusicSearch_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(MusicSearchTxt.Text))
                _ = SearchPixabay(MusicSearchTxt.Text);
        }

        private void MusicSearchTxt_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
                MusicSearch_Click(sender, e);
        }

        private async Task SearchPixabay(string query)
        {
            try
            {
                if (string.IsNullOrEmpty(PIXABAY_KEY) || PIXABAY_KEY.Contains("YOUR_")) 
                {
                    System.Windows.MessageBox.Show("Please set your PIXABAY_KEY in ApiKeys.cs first!", "API Key Needed");
                    return;
                }

                LibraryStatusText.Text = "Searching...";
                LibraryStatusText.Opacity = 1;

                using HttpClient client = new HttpClient();
                // Increased to 40 for more choices (Lazy feel)
                string url = $"https://pixabay.com/api/videos/?key={PIXABAY_KEY}&q={Uri.EscapeDataString(query)}&per_page=40";
                
                var response = await client.GetStringAsync(url);
                var data = JObject.Parse(response);
                var hits = data["hits"] as JArray;

                var assets = new List<PixabayAsset>();
                if (hits != null && hits.Count > 0)
                {
                    foreach (var hit in hits)
                    {
                        string picId = hit["picture_id"]?.ToString() ?? "";
                        assets.Add(new PixabayAsset
                        {
                            Tags = hit["tags"]?.ToString() ?? "Untitled",
                            Duration = hit["duration"]?.Value<int>() ?? 0,
                            VideoUrl = hit["videos"]?["medium"]?["url"]?.ToString() ?? "",
                            PreviewImage = $"https://i.vimeocdn.com/video/{picId}_200x150.jpg"
                        });
                    }
                    LibraryStatusText.Text = $"{hits.Count} results found";
                }
                else
                {
                    LibraryStatusText.Text = "No results found";
                }

                PixabayMusicList.ItemsSource = assets;
                LibraryStatusText.Opacity = 0.5;
            }
            catch (Exception ex)
            {
                LibraryStatusText.Text = "API Error";
                Debug.WriteLine($"Pixabay API Error: {ex.Message}");
            }
        }

        private void ApplyPixabayMusic_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is PixabayAsset asset)
            {
                _selectedMusicPath = asset.VideoUrl;
                string cleanName = asset.Tags.Split(',')[0].Trim();
                
                // Visual feedback to user
                btn.Content = "✓ ADDED";
                btn.Foreground = (System.Windows.Media.Brush)FindResource("AccentBrush");
                btn.IsEnabled = false;

                MusicInfoLabel.Text = "♫ " + (cleanName.Length > 25 ? cleanName.Substring(0, 22) + "..." : cleanName);
                MusicInfoLabel.Foreground = (System.Windows.Media.Brush)FindResource("AccentBrush");
                
                Debug.WriteLine($"Music synced: {_selectedMusicPath}");
            }
        }


        private void CustomWatermarkBtn_Click(object? sender, RoutedEventArgs? e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpg;*.bmp)|*.png;*.jpg;*.bmp";
            if (openFileDialog.ShowDialog() == true)
            {
                _customWatermarkPath = openFileDialog.FileName;
                System.Windows.MessageBox.Show("Branding Logo Loaded: " + Path.GetFileName(_customWatermarkPath));
            }
        }


        private TimeSpan ParseTime(string text)
        {
            if (TimeSpan.TryParseExact(text, "mm\\:ss", null, out TimeSpan res)) return res;
            if (double.TryParse(text, out double sec)) return TimeSpan.FromSeconds(sec);
            return TimeSpan.Zero;
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

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (VideoQueueList.Items.Count == 0)
            {
                System.Windows.MessageBox.Show("Please add at least one video clip to the storyboard!", "ZeroMix Studio", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.Filter = "MP4 Video (*.mp4)|*.mp4";
            saveFileDialog.FileName = "ZeroMix_Export_" + DateTime.Now.ToString("yyyyMMdd_HHmm");

            if (saveFileDialog.ShowDialog() != true) return;

            string outputPath = saveFileDialog.FileName;
            string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FFMPEG", "ffmpeg.exe");

            if (!File.Exists(ffmpegPath))
            {
                System.Windows.MessageBox.Show("FFmpeg engine not found! Please ensure FFmpeg is in the FFMPEG folder.", "Engine Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ExportButton.IsEnabled = false;
            ExportProgressPanel.Visibility = Visibility.Visible;
            ExportProgressBar.IsIndeterminate = true;
            ExportStatusText.Text = "ZeroMix Engine: Processing media architecture...";

            try
            {
                // We will build a complex filter complex for trimming and concatenation
                // For simplicity in this lightweight version, we'll use an input list if no music,
                // BUT to support Trimming per clip + Music, we need the -filter_complex approach.
                
                StringBuilder inputs = new StringBuilder();
                StringBuilder filter = new StringBuilder();
                int idx = 0;

                foreach (VideoClip clip in VideoQueueList.Items)
                {
                    double clipStart = clip.StartTime.TotalSeconds;
                    double clipDuration = (clip.EndTime - clip.StartTime).TotalSeconds;
                    if (clipDuration <= 0) clipDuration = 0.5; // Final safety

                    inputs.Append($"-ss {clipStart} -t {clipDuration} -i \"{clip.Path}\" ");
                    filter.Append($"[{idx}:v][{idx}:a]");
                    idx++;
                }

                bool hasBackgroundMusic = !string.IsNullOrEmpty(_selectedMusicPath);
                if (hasBackgroundMusic)
                {
                    inputs.Append($"-i \"{_selectedMusicPath}\" ");
                    // We need to mix the audio
                }

                // Simplified command for multi-clip concat
                // Using a temporary concat file is more robust for many clips
                string tempDir = Path.Combine(Path.GetTempPath(), "ZeroMix");
                if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
                string concatFile = Path.Combine(tempDir, "list.txt");
                
                StringBuilder fileContent = new StringBuilder();
                foreach (VideoClip clip in VideoQueueList.Items)
                {
                    // Note: FFmpeg concat demuxer doesn't support in-line trimming well with different codecs
                    // So we use the filter_complex method instead for accuracy.
                }

                // Constructing the complex Filter
                // [0:v][0:a][1:v][1:a]... concat=n=N:v=1:a=1 [outv][outa]
                string filterStr = "";
                for(int i=0; i<idx; i++) filterStr += $"[{i}:v][{i}:a]";
                filterStr += $"concat=n={idx}:v=1:a=1[vv][aa]";

                string audioMix = "[aa]";
                if (hasBackgroundMusic)
                {
                    // Mix original audio with background music (0.3 volume for bg)
                    filterStr += $";[{idx}:a]volume=0.3[bg];[aa][bg]amix=inputs=2:duration=first[outa]";
                    audioMix = "[outa]";
                }
                else
                {
                    audioMix = "[aa]";
                }

                string finalArgs = $"{inputs} -filter_complex \"{filterStr}\" -map \"[vv]\" -map \"{audioMix}\" -c:v libx264 -preset fast -crf 22 -c:a aac -b:a 192k \"{outputPath}\" -y";

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = finalArgs,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true
                };

                await Task.Run(() => {
                    using var process = Process.Start(psi);
                    if (process != null)
                    {
                        string error = process.StandardError.ReadToEnd();
                        process.WaitForExit();
                        if (process.ExitCode != 0)
                        {
                            throw new Exception("FFmpeg Error: " + error);
                        }
                    }
                });

                if (File.Exists(outputPath))
                {
                    System.Windows.MessageBox.Show("Export Berhasil!\nVideo telah disimpan.", "ZeroMix Studio", MessageBoxButton.OK, MessageBoxImage.Information);
                    Process.Start("explorer.exe", $"/select,\"{outputPath}\"");
                }
                else
                {
                    throw new Exception("Output file was not created. Check FFmpeg logs.");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Export Gagal: " + ex.Message, "Engine Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ExportProgressPanel.Visibility = Visibility.Collapsed;
                ExportButton.IsEnabled = true;
                ExportProgressBar.IsIndeterminate = false;
            }
        }

        private BitmapSource? GetVideoThumbnail(string filePath)
        {
            try
            {
                // 1. Try WPF Native (Quick but often fails/black for H264/H265)
                var uri = new Uri(filePath);
                var frame = BitmapFrame.Create(uri, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                if (frame.Thumbnail != null) return frame.Thumbnail;

                // 2. If it fails, we will trigger a background extraction later (for now return null to show placeholder)
                // In a heavy app we'd use FFmpeg here, but to stay "super ringan", 
                // we only use FFmpeg if explicitly needed.
                ExtractThumbnailAsync(filePath);
                
                return null; 
            }
            catch { return null; }
        }

        private async void ExtractThumbnailAsync(string videoPath)
        {
            try
            {
                string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FFMPEG", "ffmpeg.exe");
                if (!File.Exists(ffmpegPath)) return;

                string thumbDir = Path.Combine(Path.GetTempPath(), "ZeroMix", "Thumbs");
                if (!Directory.Exists(thumbDir)) Directory.CreateDirectory(thumbDir);

                string thumbPath = Path.Combine(thumbDir, Guid.NewGuid().ToString() + ".jpg");
                
                // Extract frame at 1 second
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = $"-ss 00:00:01 -i \"{videoPath}\" -frames:v 1 -q:v 5 \"{thumbPath}\" -y",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using var process = Process.Start(psi);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    if (File.Exists(thumbPath))
                    {
                        // Update the Item in the list
                        Dispatcher.Invoke(() => {
                            foreach (VideoClip item in VideoQueueList.Items)
                            {
                                if (item.Path == videoPath)
                                {
                                    var bitmap = new BitmapImage();
                                    bitmap.BeginInit();
                                    bitmap.UriSource = new Uri(thumbPath);
                                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                    bitmap.EndInit();
                                    item.Thumbnail = bitmap;
                                    
                                    // Refresh UI
                                    VideoQueueList.Items.Refresh();
                                    break;
                                }
                            }
                        });
                    }
                }
            }
            catch (Exception ex) { Debug.WriteLine("Thumb Error: " + ex.Message); }
        }
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try {
                if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
                    this.DragMove();
            } catch { }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
