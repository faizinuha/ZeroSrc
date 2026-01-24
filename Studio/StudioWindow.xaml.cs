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
using System.Windows.Media;
using System.Collections.ObjectModel;
using System.Linq;

namespace ZeroMix.Studio
{
    public partial class StudioWindow : Window
    {
        private string _selectedVideoPath = "";
        private string _selectedMusicPath = "";
        private DispatcherTimer _timer;
        private double _lastVolume = 0.5;
        private bool _isMuted = false;
        private string _activeFilter = "";

        public class VideoClip
        {
            public string Path { get; set; } = "";
            public string FileName { get; set; } = "";
            public string DurationStr { get; set; } = "00:00";
            public TimeSpan TotalDuration { get; set; }
            public TimeSpan StartTime { get; set; } = TimeSpan.Zero;
            public TimeSpan EndTime { get; set; } = TimeSpan.Zero;
            public System.Windows.Media.ImageSource Thumbnail { get; set; }
            public bool FadeIn { get; set; }
            public bool FadeOut { get; set; }
        }

        private string PIXABAY_KEY => ApiKeys.PixabayKey;
        private ObservableCollection<VideoClip> _videoClips = new ObservableCollection<VideoClip>();
        private HttpClient _http = new HttpClient();

        public class PixabayMusicResult
        {
            public string Id { get; set; } = "";
            public string Title { get; set; } = "";
            public string User { get; set; } = "";
            public string DurationStr { get; set; } = "";
            public string PreviewUrl { get; set; } = "";
            public string DownloadUrl { get; set; } = "";
        }

        public StudioWindow()
        {
            InitializeComponent();
            VideoQueueList.ItemsSource = _videoClips;
            TimelineList.ItemsSource = _videoClips;
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(50);
            _timer.Tick += Timer_Tick;
            PixabayPreviewInternal.LoadedBehavior = System.Windows.Controls.MediaState.Manual;
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Close_Click(object sender, RoutedEventArgs e) => Close();
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); }

        private void Fullscreen_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                WindowStyle = WindowStyle.None;
            }
            else
            {
                WindowState = WindowState.Maximized;
                // Keep WindowStyle.None for borderless fullscreen
            }
        }

        private void Mute_Click(object sender, RoutedEventArgs e)
        {
            _isMuted = !_isMuted;
            if (_isMuted)
            {
                _lastVolume = VideoPreview.Volume;
                VideoPreview.Volume = 0;
                VolumeSlider.Value = 0;
                MuteBtn.Content = "🔇";
            }
            else
            {
                VideoPreview.Volume = _lastVolume > 0 ? _lastVolume : 0.5;
                VolumeSlider.Value = VideoPreview.Volume * 100;
                MuteBtn.Content = "🔊";
            }
        }

        private void Timer_Tick(object? sender, EventArgs e) { if (VideoPreview.NaturalDuration.HasTimeSpan) UpdateDurationLabel(); }
        
        private void UpdateDurationLabel() { if (VideoPreview.NaturalDuration.HasTimeSpan) DurationText.Text = $"{VideoPreview.Position:mm\\:ss} / {VideoPreview.NaturalDuration.TimeSpan:mm\\:ss}"; }

        private void ImportVideo_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Video files (*.mp4;*.avi;*.mov;*.mkv)|*.mp4;*.avi;*.mov;*.mkv";
            openFileDialog.Multiselect = true;
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filename in openFileDialog.FileNames)
                {
                    var clip = new VideoClip { Path = filename, FileName = Path.GetFileName(filename), DurationStr = "00:00", StartTime = TimeSpan.Zero, EndTime = TimeSpan.Zero, Thumbnail = GetVideoThumbnail(filename) ?? new BitmapImage() };
                    _videoClips.Add(clip);
                    if (string.IsNullOrEmpty(_selectedVideoPath)) LoadVideoToPreview(filename);
                }
            }
        }

        private void LoadVideoToPreview(string path)
        {
            _selectedVideoPath = path;
            VideoPreview.Source = new System.Uri(_selectedVideoPath);
            VideoPreview.Play();
            VideoPreview.Pause();
            if (_timer.IsEnabled) _timer.Stop();
            _timer.Start();
            VideoPreview.MediaOpened -= OnMediaOpened;
            VideoPreview.MediaOpened += OnMediaOpened;

            // Sync Music Preview
            if (!string.IsNullOrEmpty(_selectedMusicPath) && File.Exists(_selectedMusicPath))
            {
                MusicPreview.Source = new System.Uri(_selectedMusicPath);
                MusicPreview.Play();
                MusicPreview.Pause();
            }
        }

        private void OnMediaOpened(object sender, RoutedEventArgs e)
        {
            if (VideoPreview.NaturalDuration.HasTimeSpan)
            {
                var duration = VideoPreview.NaturalDuration.TimeSpan;
                foreach (VideoClip clip in _videoClips)
                {
                    if (clip.Path == _selectedVideoPath)
                    {
                        clip.TotalDuration = duration;
                        if (clip.EndTime == TimeSpan.Zero) clip.EndTime = duration;
                        clip.DurationStr = duration.ToString("mm\\:ss");
                    }
                }
                // ObservableCollection handles UI updates better than Items.Refresh()
            }
        }

        private void VideoQueueList_SelectionChanged(object sender, SelectionChangedEventArgs e) 
        { 
            if (VideoQueueList.SelectedItem is VideoClip clip) 
            {
                LoadVideoToPreview(clip.Path);
                ShowClipSettings(clip);
            }
        }

        private void ShowClipSettings(VideoClip clip)
        {
            PropertySidebarColumn.Width = new GridLength(200);
            ClipSettingsPanel.Visibility = Visibility.Visible;
            
            // Sync UI
            FadeInCheck.IsChecked = clip.FadeIn;
            FadeOutCheck.IsChecked = clip.FadeOut;
            ClipInfoText.Text = $"File: {clip.FileName}\nDuration: {clip.DurationStr}";
        }

        private void ClipSetting_Changed(object sender, RoutedEventArgs e)
        {
            if (VideoQueueList.SelectedItem is VideoClip clip)
            {
                clip.FadeIn = FadeInCheck.IsChecked ?? false;
                clip.FadeOut = FadeOutCheck.IsChecked ?? false;
            }
        }

        private void RemoveFromQueue_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            var clip = btn?.Tag as VideoClip;
            if (clip != null)
            {
                _videoClips.Remove(clip);
                if (clip.Path == _selectedVideoPath)
                {
                    VideoPreview.Source = null;
                    _selectedVideoPath = "";
                    _timer.Stop();
                }
            }
        }

        private void SelectMusic_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Audio files (*.mp3;*.wav;*.m4a)|*.mp3;*.wav;*.m4a";
            if (openFileDialog.ShowDialog() == true)
            {
                ApplyMusic(openFileDialog.FileName);
            }
        }

        private void ApplyMusic(string path)
        {
            _selectedMusicPath = path;
            string fileName = Path.GetFileName(path);
            MusicInfoLabel.Text = "♫ " + fileName;
            
            // Show in timeline
            MusicTimelineBar.Visibility = Visibility.Visible;
            MusicTimelineLabel.Text = "♫ " + fileName;
            
            // Adjust width to match video track (approximate) or container
            MusicTimelineBar.Width = Math.Max(300, TimelineList.ActualWidth > 0 ? TimelineList.ActualWidth : _videoClips.Count * 110);
            
            // Load into preview player immediately if video is ready
            MusicPreview.Source = new Uri(_selectedMusicPath);
            MusicPreview.Pause(); // Wait for video play
        }

        private void SelectPixabayMusic_Click(object sender, RoutedEventArgs e)
        {
            // Toggle sidebar width dan splitter
            if (SidebarColumn.Width.Value == 0)
            {
                SplitterColumn.Width = new GridLength(5);
                SidebarColumn.Width = new GridLength(300);
                SidebarSplitter.Visibility = Visibility.Visible;
            }
            else
            {
                SplitterColumn.Width = new GridLength(0);
                SidebarColumn.Width = new GridLength(0);
                SidebarSplitter.Visibility = Visibility.Collapsed;
            }
        }

        private async void PixabaySearch_Click(object sender, RoutedEventArgs e) => await PerformPixabaySearch(PixabaySearchBox.Text);
        private async void PixabaySearchBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) { if (e.Key == Key.Enter) await PerformPixabaySearch(PixabaySearchBox.Text); }
        
        private async void PixabayCategory_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            string category = btn?.Tag?.ToString() ?? "";
            if (!string.IsNullOrEmpty(category))
            {
                PixabaySearchBox.Text = category;
                await PerformPixabaySearch(category);
            }
        }

        private async Task PerformPixabaySearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return;

            PixabayResultsList.Items.Clear();
            try
            {
                // Gunakan videos API karena music API butuh akses khusus
                string url = $"https://pixabay.com/api/videos/?key={PIXABAY_KEY}&q={Uri.EscapeDataString(query)}&video_type=film";
                string response = await _http.GetStringAsync(url);
                var data = JObject.Parse(response);
                var hits = data["hits"];

                if (hits != null)
                {
                    foreach (var hit in hits)
                    {
                        // Videos API: ambil video file (tiny/small/medium/large)
                        var videos = hit["videos"];
                        string videoUrl = "";
                        if (videos != null)
                        {
                            videoUrl = (videos["small"]?["url"] ?? videos["tiny"]?["url"] ?? videos["medium"]?["url"] ?? "").ToString();
                        }
                        
                        if (string.IsNullOrEmpty(videoUrl)) continue;
                        
                        var result = new PixabayMusicResult
                        {
                            Id = hit["id"]?.ToString() ?? "",
                            Title = hit["tags"]?.ToString() ?? "Untitled",
                            User = hit["user"]?.ToString() ?? "Unknown",
                            DurationStr = FormatPixabayDuration(hit["duration"]?.ToString()),
                            PreviewUrl = videoUrl,
                            DownloadUrl = videoUrl
                        };

                        PixabayResultsList.Items.Add(result);
                    }
                }
            }
            catch (Exception ex) { System.Windows.MessageBox.Show("Pixabay Error: " + ex.Message); }
        }

        private string FormatPixabayDuration(string? durationSec)
        {
            if (double.TryParse(durationSec, out double sec))
            {
                TimeSpan t = TimeSpan.FromSeconds(sec);
                return string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds);
            }
            return "00:00";
        }

        private void PixabayPreview_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            var music = btn?.Tag as PixabayMusicResult;
            if (music != null && !string.IsNullOrEmpty(music.PreviewUrl))
            {
                if (PixabayPreviewInternal.Source?.ToString() == music.PreviewUrl && PixabayPreviewInternal.NaturalDuration.HasTimeSpan)
                {
                    PixabayPreviewInternal.Stop();
                    PixabayPreviewInternal.Source = null;
                }
                else
                {
                    PixabayPreviewInternal.Source = new Uri(music.PreviewUrl);
                    PixabayPreviewInternal.Play();
                }
            }
        }

        private async void PixabayAdd_Click(object sender, RoutedEventArgs e)
        {
            var selected = PixabayResultsList.SelectedItem as PixabayMusicResult;
            if (selected == null) { System.Windows.MessageBox.Show("Pilih lagu dulu Kak!"); return; }

            PixabayAddBtn.IsEnabled = false;
            PixabayAddBtn.Content = "DOWNLOADING...";

            try
            {
                string musicDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Music");
                if (!Directory.Exists(musicDir)) Directory.CreateDirectory(musicDir);

                string fileName = $"{selected.Id}.mp3";
                string filePath = Path.Combine(musicDir, fileName);

                using (var response = await _http.GetAsync(selected.DownloadUrl))
                {
                    using (var fs = new FileStream(filePath, FileMode.Create))
                    {
                        await response.Content.CopyToAsync(fs);
                    }
                }

                ApplyMusic(filePath);
                System.Windows.MessageBox.Show("Lagu terpasang! Sidebar ditutup.");
                SidebarColumn.Width = new GridLength(0);
            }
            catch (Exception ex) { System.Windows.MessageBox.Show("Download Gagal: " + ex.Message); }
            finally
            {
                PixabayAddBtn.IsEnabled = true;
                PixabayAddBtn.Content = "ADD SELECTED";
            }
        }

        private void VideoPreview_MediaEnded(object sender, RoutedEventArgs e)
        {
            // Auto next logic
            var currentClip = _videoClips.FirstOrDefault(c => c.Path == _selectedVideoPath);
            if (currentClip != null)
            {
                int index = _videoClips.IndexOf(currentClip);
                if (index < _videoClips.Count - 1)
                {
                    var nextClip = _videoClips[index + 1];
                    VideoQueueList.SelectedItem = nextClip;
                    LoadVideoToPreview(nextClip.Path);
                }
            }
        }

        private void TimelineItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var border = sender as FrameworkElement;
            var clip = border?.DataContext as VideoClip;
            if (clip != null)
            {
                VideoQueueList.SelectedItem = clip;
            }
        }

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (PreviewScale != null)
            {
                PreviewScale.ScaleX = e.NewValue;
                PreviewScale.ScaleY = e.NewValue;
            }
        }

        private void MoveClipLeft_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            var clip = btn?.Tag as VideoClip;
            if (clip != null)
            {
                int oldIndex = _videoClips.IndexOf(clip);
                if (oldIndex > 0) _videoClips.Move(oldIndex, oldIndex - 1);
            }
        }

        private void MoveClipRight_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            var clip = btn?.Tag as VideoClip;
            if (clip != null)
            {
                int oldIndex = _videoClips.IndexOf(clip);
                if (oldIndex < _videoClips.Count - 1) _videoClips.Move(oldIndex, oldIndex + 1);
            }
        }

        private void RemoveVideo_ContextMenu(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as System.Windows.Controls.MenuItem;
            var clip = menuItem?.Tag as VideoClip;
            if (clip != null)
            {
                _videoClips.Remove(clip);
                if (clip.Path == _selectedVideoPath)
                {
                    VideoPreview.Source = null;
                    _selectedVideoPath = "";
                    _timer.Stop();
                }
            }
        }

        private void RemoveMusic_ContextMenu(object sender, RoutedEventArgs e)
        {
            _selectedMusicPath = "";
            MusicPreview.Source = null;
            MusicPreview.Stop();
            MusicInfoLabel.Text = "♫ No music";
            MusicTimelineBar.Visibility = Visibility.Collapsed;
        }

        private void JumpStart_Click(object sender, RoutedEventArgs e) 
        { 
            if (VideoPreview.Source != null) VideoPreview.Position = TimeSpan.Zero;
            if (MusicPreview.Source != null) MusicPreview.Position = TimeSpan.Zero;
        }
        
        private void JumpEnd_Click(object sender, RoutedEventArgs e) 
        { 
            if (VideoPreview.NaturalDuration.HasTimeSpan) VideoPreview.Position = VideoPreview.NaturalDuration.TimeSpan;
            // Stop music if video ends
             if (MusicPreview.Source != null) MusicPreview.Stop();
        }
        private void Volume_Changed(object sender, RoutedPropertyChangedEventArgs<double> e) 
        { 
            if (VideoPreview != null && VolumeSlider != null) 
            {
                VideoPreview.Volume = VolumeSlider.Value / 100.0;
                // Sync Music volume (or maybe separate? for now keep ratio constant or simpler SAME volume)
                // MusicPreview.Volume = VideoPreview.Volume * 0.8; // slightly lower bg
                if (VolumeSlider.Value > 0 && _isMuted) { _isMuted = false; MuteBtn.Content = "🔊"; }
            }
        }

        private void TogglePlay_Click(object sender, RoutedEventArgs e)
        {
            if (VideoPreview.Source == null) return;
            if (PlayToggleBtn.Content.ToString() == "▶") 
            { 
                VideoPreview.Play(); 
                if (MusicPreview.Source != null) MusicPreview.Play();
                PlayToggleBtn.Content = "⏸"; 
            }
            else 
            { 
                VideoPreview.Pause(); 
                if (MusicPreview.Source != null) MusicPreview.Pause();
                PlayToggleBtn.Content = "▶"; 
            }
        }

        private void Cut_Click(object sender, RoutedEventArgs e)
        {
            if (VideoQueueList.SelectedItem is VideoClip clip && VideoPreview.Source != null)
            {
                var currentPos = VideoPreview.Position;
                // Only split if within clip range (simplified for now since we use full path)
                if (currentPos > TimeSpan.FromSeconds(1) && currentPos < (clip.TotalDuration - TimeSpan.FromSeconds(1)))
                {
                    int index = _videoClips.IndexOf(clip);
                    
                    // Modify current clip to end at cut
                    var oldEndTime = clip.EndTime;
                    clip.EndTime = currentPos;
                    clip.DurationStr = (clip.EndTime - clip.StartTime).ToString("mm\\:ss");

                    // Create second part
                    var nextPart = new VideoClip
                    {
                        Path = clip.Path,
                        FileName = clip.FileName + " (Part 2)",
                        Thumbnail = clip.Thumbnail,
                        TotalDuration = clip.TotalDuration,
                        StartTime = currentPos,
                        EndTime = oldEndTime,
                        DurationStr = (oldEndTime - currentPos).ToString("mm\\:ss")
                    };
                    
                    _videoClips.Insert(index + 1, nextPart);
                    VideoQueueList.SelectedItem = nextPart;
                }
            }
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            if (VideoQueueList.SelectedItem is VideoClip clip)
            {
                var newClip = new VideoClip
                {
                    Path = clip.Path,
                    FileName = clip.FileName + " (Copy)",
                    Thumbnail = clip.Thumbnail,
                    TotalDuration = clip.TotalDuration,
                    StartTime = clip.StartTime,
                    EndTime = clip.EndTime,
                    DurationStr = clip.DurationStr,
                    FadeIn = clip.FadeIn,
                    FadeOut = clip.FadeOut
                };
                _videoClips.Add(newClip);
            }
        }

        private void RemoveFromIcons_Click(object sender, RoutedEventArgs e)
        {
            if (VideoQueueList.SelectedItem is VideoClip clip)
            {
                _videoClips.Remove(clip);
                if (_videoClips.Count == 0)
                {
                    VideoPreview.Source = null;
                    PropertySidebarColumn.Width = new GridLength(0);
                }
            }
        }

        private void FilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FilterCombo?.SelectedItem is ComboBoxItem item)
            {
                _activeFilter = item.Tag?.ToString() ?? "";
            }
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (VideoQueueList.Items.Count == 0) { System.Windows.MessageBox.Show("Add video first!"); return; }
            
            // Show Export Options Dialog
            var exportDialog = new ExportOptionsDialog();
            exportDialog.Owner = this;
            if (exportDialog.ShowDialog() != true) return;

            string outputPath = exportDialog.OutputPath;
            if (string.IsNullOrEmpty(outputPath)) { System.Windows.MessageBox.Show("Add output path first!"); return; }

            string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FFMPEG", "ffmpeg.exe");
            if (!File.Exists(ffmpegPath)) { System.Windows.MessageBox.Show("FFmpeg not found in: " + ffmpegPath); return; }

            ExportButton.IsEnabled = false;
            ExportProgressPanel.Visibility = Visibility.Visible;
            ExportProgressBar.IsIndeterminate = true;
            ExportStatusText.Text = "Exporting at " + exportDialog.SelectedWidth + "x" + exportDialog.SelectedHeight + "...";

            try
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "ZeroMix");
                if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
                string concatFile = Path.Combine(tempDir, Guid.NewGuid().ToString() + ".txt");

                StringBuilder concatContent = new StringBuilder();
                foreach (VideoClip clip in _videoClips) 
                {
                    // Full path for ffmpeg concat
                    concatContent.AppendLine($"file '{clip.Path.Replace("\\", "/")}'");
                }
                File.WriteAllText(concatFile, concatContent.ToString());

                // Prepare FFmpeg arguments with resolution and fps from dialog
                string args = "";
                
                // Determine FFmpeg preset based on resolution (lower = faster export)
                string ffmpegPreset = "fast"; // default
                if (exportDialog.SelectedHeight <= 480) 
                    ffmpegPreset = "ultrafast"; // SD = paling cepat
                else if (exportDialog.SelectedHeight <= 720) 
                    ffmpegPreset = "fast"; // HD = medium
                else 
                    ffmpegPreset = "slow"; // Full HD = kualitas terbaik, lebih lambat
                
                // Advanced FFmpeg Filter Construction for Individual Fades and Cuts
                StringBuilder inputs = new StringBuilder();
                StringBuilder filterV = new StringBuilder();
                StringBuilder filterA = new StringBuilder();
                
                for (int i = 0; i < _videoClips.Count; i++)
                {
                    var clip = _videoClips[i];
                    double duration = (clip.EndTime - clip.StartTime).TotalSeconds;
                    if (duration <= 0) duration = clip.TotalDuration.TotalSeconds;

                    // SS (Seek) and T (Duration) for Cut support
                    inputs.Append($"-ss {clip.StartTime.TotalSeconds} -t {duration} -i \"{clip.Path}\" ");

                    // Video filter per clip: Scale/Pad -> Optional Fade
                    string vTag = $"[v{i}]";
                    filterV.Append($"[{i}:v]scale={exportDialog.SelectedWidth}:{exportDialog.SelectedHeight}:force_original_aspect_ratio=decrease,pad={exportDialog.SelectedWidth}:{exportDialog.SelectedHeight}:(ow-iw)/2:(oh-ih)/2,setpts=PTS-STARTPTS");
                    
                    if (clip.FadeIn) filterV.Append($",fade=t=in:st=0:d=1");
                    if (clip.FadeOut) filterV.Append($",fade=t=out:st={Math.Max(0, duration - 1)}:d=1");
                    
                    // Final global filters (activeFilter) on each clip segment for simplicity
                    if (_activeFilter == "grayscale") filterV.Append(",format=gray");
                    else if (_activeFilter == "sepia") filterV.Append(",colorchannelmixer=.393:.769:.189:0:.349:.686:.168:0:.272:.534:.131");
                    else if (_activeFilter == "cine") filterV.Append(",curves=preset=lighter,eq=saturation=1.2");

                    filterV.Append($"{vTag};");

                    // Audio filter per clip: Optional Fade
                    string aTag = $"[a{i}]";
                    filterA.Append($"[{i}:a]aselect=enable='between(t,0,{duration})',asetpts=PTS-STARTPTS");
                    if (clip.FadeIn) filterA.Append($",afade=t=in:st=0:d=1");
                    if (clip.FadeOut) filterA.Append($",afade=t=out:st={Math.Max(0, duration - 1)}:d=1");
                    filterA.Append($"{aTag};");
                }

                // Concatenate all clips
                string concatV = "";
                string concatA = "";
                for (int i = 0; i < _videoClips.Count; i++) { concatV += $"[v{i}]"; concatA += $"[a{i}]"; }
                filterV.Append($"{concatV}concat=n={_videoClips.Count}:v=1:a=0[vout];");
                filterA.Append($"{concatA}concat=n={_videoClips.Count}:v=0:a=1[a_orig];");

                string finalArgs = inputs.ToString();
                
                if (!string.IsNullOrEmpty(_selectedMusicPath))
                {
                    // Add background music as final input
                    finalArgs += $"-i \"{_selectedMusicPath}\" ";
                    int musicIndex = _videoClips.Count;
                    
                    // Mix original audio with loopable background music
                    filterA.Append($"[{musicIndex}:a]aloop=loop=-1:size=2e+09[bg];[a_orig][bg]amix=inputs=2:duration=first[aout]");
                    finalArgs += $"-filter_complex \"{filterV}{filterA}\" -map \"[vout]\" -map \"[aout]\" ";
                }
                else
                {
                    finalArgs += $"-filter_complex \"{filterV}{filterA}\" -map \"[vout]\" -map \"[a_orig]\" ";
                }

                finalArgs += $"-r {exportDialog.SelectedFPS} -c:v libx264 -preset {ffmpegPreset} -pix_fmt yuv420p -c:a aac -b:a 192k -shortest -y \"{outputPath}\"";
                args = finalArgs;

                var processInfo = new ProcessStartInfo 
                { 
                    FileName = ffmpegPath, 
                    Arguments = args, 
                    UseShellExecute = false, 
                    RedirectStandardOutput = true, 
                    RedirectStandardError = true, 
                    CreateNoWindow = true 
                };

                await Task.Run(() => 
                {
                    using var process = System.Diagnostics.Process.Start(processInfo);
                    process?.WaitForExit();
                });

                if (File.Exists(concatFile)) File.Delete(concatFile);
                
                System.Windows.MessageBox.Show("Export Finished Successfully!\nSaved to: " + outputPath, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                ExportProgressPanel.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex) 
            { 
                System.Windows.MessageBox.Show("Export Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); 
                ExportProgressPanel.Visibility = Visibility.Collapsed; 
            }
            finally { ExportButton.IsEnabled = true; }
        }

        private System.Windows.Media.ImageSource GetVideoThumbnail(string videoPath)
        {
            try
            {
                string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FFMPEG", "ffmpeg.exe");
                if (!File.Exists(ffmpegPath)) return null;
                string tempThumb = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".jpg");
                var processInfo = new ProcessStartInfo { FileName = ffmpegPath, Arguments = $"-i \"{videoPath}\" -ss 00:00:00.5 -vframes 1 \"{tempThumb}\"", UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
                using var process = System.Diagnostics.Process.Start(processInfo);
                process?.WaitForExit();
                if (File.Exists(tempThumb))
                {
                    BitmapImage bi = new BitmapImage();
                    bi.BeginInit();
                    bi.UriSource = new Uri(tempThumb, UriKind.Absolute);
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.EndInit();
                    bi.Freeze();
                    try { File.Delete(tempThumb); } catch { }
                    return bi;
                }
            }
            catch { }
            return null;
        }
    }
}
