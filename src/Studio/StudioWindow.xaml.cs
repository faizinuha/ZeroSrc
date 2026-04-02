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
        private ObservableCollection<VideoClip> _libraryClips = new ObservableCollection<VideoClip>();
        private ObservableCollection<VideoClip> _timelineClips = new ObservableCollection<VideoClip>();
        private HttpClient _http = new HttpClient();
        private VideoClip? _selectedClip;
        
        public class FilterItem
        {
            public string Name { get; set; } = "";
            public string Tag { get; set; } = "";
            public ImageSource? PreviewImage { get; set; }
        }
        private ObservableCollection<FilterItem> _filters = new ObservableCollection<FilterItem>();

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
            LibraryList.ItemsSource = _libraryClips;
            TimelineList.ItemsSource = _timelineClips;
            FilterList.ItemsSource = _filters;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            _timer.Tick += Timer_Tick;
            
            LoadFilters();
        }

        private void LoadFilters()
        {
            _filters.Add(new FilterItem { Name = "None", Tag = "" });
            _filters.Add(new FilterItem { Name = "Grayscale", Tag = "grayscale" });
            _filters.Add(new FilterItem { Name = "Sepia", Tag = "sepia" });
            _filters.Add(new FilterItem { Name = "Cinematic", Tag = "cine" });
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
                WindowStyle = WindowStyle.None; 
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
                QuickMuteBtn.Content = "🔇";
            }
            else
            {
                VideoPreview.Volume = _lastVolume > 0 ? _lastVolume : 0.5;
                VolumeSlider.Value = VideoPreview.Volume * 100;
                QuickMuteBtn.Content = "🔊";
            }
        }

        private void Timer_Tick(object? sender, EventArgs e) 
        { 
            if (VideoPreview.NaturalDuration.HasTimeSpan) 
            {
                UpdateDurationLabel(); 
                ApplyRealtimeEffects();
            }
        }
        
        private void ApplyRealtimeEffects()
        {
            if (_selectedClip == null) { VideoPreview.Opacity = 1; return; }
            
            double pos = VideoPreview.Position.TotalSeconds;
            double start = _selectedClip.StartTime.TotalSeconds;
            double dur = (_selectedClip.EndTime - _selectedClip.StartTime).TotalSeconds;
            if (dur <= 0) dur = _selectedClip.TotalDuration.TotalSeconds;
            
            double opacity = 1.0;
            
            // Real-time Fade In
            if (_selectedClip.FadeIn && pos < 1.0) 
            {
                opacity = Math.Min(1.0, pos);
            }
            // Real-time Fade Out
            else if (_selectedClip.FadeOut && pos > (dur - 1.0))
            {
                opacity = Math.Max(0.0, dur - pos);
            }
            
            VideoPreview.Opacity = opacity;
        }
        
        private void UpdateDurationLabel() { if (VideoPreview.NaturalDuration.HasTimeSpan) DurationText.Text = $"{VideoPreview.Position:mm\\:ss} / {VideoPreview.NaturalDuration.TimeSpan:mm\\:ss}"; }

        private void ImportVideo_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Video files (*.mp4;*.mkv)|*.mp4;*.mkv";
            openFileDialog.Multiselect = true;
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filename in openFileDialog.FileNames)
                {
                    var clip = new VideoClip { Path = filename, FileName = Path.GetFileName(filename), DurationStr = "00:00", Thumbnail = GetVideoThumbnail(filename) ?? new BitmapImage() };
                    _libraryClips.Add(clip);
                }
            }
        }

        private void AddToTimeline_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            var clip = btn?.Tag as VideoClip;
            if (clip != null)
            {
                var newClip = new VideoClip { 
                    Path = clip.Path, FileName = clip.FileName, Thumbnail = clip.Thumbnail, 
                    TotalDuration = clip.TotalDuration, StartTime = clip.StartTime, 
                    EndTime = clip.EndTime, DurationStr = clip.DurationStr 
                };
                _timelineClips.Add(newClip);
                LoadVideoToPreview(newClip);
            }
        }

        private void LoadVideoToPreview(VideoClip clip)
        {
            _selectedClip = clip;
            VideoPreview.Source = new System.Uri(clip.Path);
            VideoPreview.Position = clip.StartTime;
            VideoPreview.Play();
            VideoPreview.Pause();
            _timer.Start();

            // Refresh Filter Thumbnails & Enable Button
            FilterBtn.IsEnabled = true;
            foreach(var filter in _filters) filter.PreviewImage = clip.Thumbnail;
            FilterList.Items.Refresh();
        }

        private void OnMediaOpened(object sender, RoutedEventArgs e)
        {
            if (VideoPreview.NaturalDuration.HasTimeSpan && _selectedClip != null)
            {
                var duration = VideoPreview.NaturalDuration.TimeSpan;
                _selectedClip.TotalDuration = duration;
                if (_selectedClip.EndTime == TimeSpan.Zero) _selectedClip.EndTime = duration;
                _selectedClip.DurationStr = duration.ToString("mm\\:ss");
                TimelineList.Items.Refresh();
            }
        }

        private void TimelineItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border?.DataContext is VideoClip clip)
            {
                LoadVideoToPreview(clip);
                ShowClipSettings(clip);
            }
        }

        private void ShowFilters_Click(object sender, RoutedEventArgs e) { SwitchPanel(PanelFilters); }
        private void ShowPixabay_Click(object sender, RoutedEventArgs e) { SwitchPanel(PanelPixabay); }

        private void SwitchPanel(Grid targetPanel)
        {
            PanelSettings.Visibility = Visibility.Collapsed;
            PanelFilters.Visibility = Visibility.Collapsed;
            PanelPixabay.Visibility = Visibility.Collapsed;
            
            targetPanel.Visibility = Visibility.Visible;
            SidebarColumn.Width = new GridLength(300);
        }

        private void ShowClipSettings(VideoClip clip)
        {
            SwitchPanel(PanelSettings);
            FadeInCheck.IsChecked = clip.FadeIn;
            FadeOutCheck.IsChecked = clip.FadeOut;
        }

        private void ClipSetting_Changed(object sender, RoutedEventArgs e)
        {
            if (_selectedClip != null)
            {
                _selectedClip.FadeIn = FadeInCheck.IsChecked ?? false;
                _selectedClip.FadeOut = FadeOutCheck.IsChecked ?? false;
            }
        }

        private void RemoveFromLibrary_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            var clip = btn?.Tag as VideoClip;
            if (clip != null)
            {
                _libraryClips.Remove(clip);
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
            
            // Show in timeline
            MusicTimelineBar.Visibility = Visibility.Visible;
            MusicTimelineLabel.Text = "♫ " + fileName;
            
            // Adjust width to match video track
            MusicTimelineBar.Width = Math.Max(300, TimelineList.ActualWidth > 0 ? TimelineList.ActualWidth : _timelineClips.Count * 170);
            
            // Load into preview player immediately if video is ready
            MusicPreview.Source = new Uri(_selectedMusicPath);
            MusicPreview.Pause(); // Wait for video play
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
                string url = $"https://pixabay.com/api/videos/?key={PIXABAY_KEY}&q={Uri.EscapeDataString(query)}&video_type=film";
                string response = await _http.GetStringAsync(url);
                var data = JObject.Parse(response);
                var hits = data["hits"];

                if (hits != null)
                {
                    foreach (var hit in hits)
                    {
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
        }

        private void VideoPreview_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (_selectedClip != null)
            {
                int index = _timelineClips.IndexOf(_selectedClip);
                if (index < _timelineClips.Count - 1)
                {
                    var nextClip = _timelineClips[index + 1];
                    LoadVideoToPreview(nextClip);
                }
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
                int oldIndex = _timelineClips.IndexOf(clip);
                if (oldIndex > 0) _timelineClips.Move(oldIndex, oldIndex - 1);
            }
        }

        private void MoveClipRight_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            var clip = btn?.Tag as VideoClip;
            if (clip != null)
            {
                int oldIndex = _timelineClips.IndexOf(clip);
                if (oldIndex < _timelineClips.Count - 1) _timelineClips.Move(oldIndex, oldIndex + 1);
            }
        }

        private void JumpStart_Click(object sender, RoutedEventArgs e) 
        { 
            if (VideoPreview.Source != null) VideoPreview.Position = TimeSpan.Zero;
            if (MusicPreview.Source != null) MusicPreview.Position = TimeSpan.Zero;
        }
        
        private void JumpEnd_Click(object sender, RoutedEventArgs e) 
        { 
            if (VideoPreview.NaturalDuration.HasTimeSpan) VideoPreview.Position = VideoPreview.NaturalDuration.TimeSpan;
             if (MusicPreview.Source != null) MusicPreview.Stop();
        }
        private void Volume_Changed(object sender, RoutedPropertyChangedEventArgs<double> e) 
        { 
            if (VideoPreview != null && VolumeSlider != null) 
            {
                VideoPreview.Volume = VolumeSlider.Value / 100.0;
                MusicPreview.Volume = VideoPreview.Volume * 0.8;
            }
        }

        private void FilterList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FilterList.SelectedItem is FilterItem filter)
            {
                _activeFilter = filter.Tag;
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
            if (_selectedClip != null && VideoPreview.Source != null)
            {
                var currentPos = VideoPreview.Position;
                var clip = _selectedClip;

                if (currentPos > TimeSpan.FromSeconds(0.5) && currentPos < (clip.EndTime - TimeSpan.FromSeconds(0.5)))
                {
                    int index = _timelineClips.IndexOf(clip);
                    
                    var oldEndTime = clip.EndTime;
                    clip.EndTime = currentPos;
                    clip.DurationStr = (clip.EndTime - clip.StartTime).ToString("mm\\:ss");

                    var nextPart = new VideoClip
                    {
                        Path = clip.Path,
                        FileName = clip.FileName + " (Part 2)",
                        Thumbnail = clip.Thumbnail,
                        TotalDuration = clip.TotalDuration,
                        StartTime = currentPos,
                        EndTime = oldEndTime,
                        DurationStr = (oldEndTime - currentPos).ToString("mm\\:ss"),
                        FadeIn = false, 
                        FadeOut = clip.FadeOut
                    };
                    
                    _timelineClips.Insert(index + 1, nextPart);
                    TimelineList.Items.Refresh();
                }
            }
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedClip != null)
            {
                var clip = _selectedClip;
                var newClip = new VideoClip {
                    Path = clip.Path, FileName = clip.FileName, Thumbnail = clip.Thumbnail,
                    TotalDuration = clip.TotalDuration, StartTime = clip.StartTime,
                    EndTime = clip.EndTime, DurationStr = clip.DurationStr,
                    FadeIn = clip.FadeIn, FadeOut = clip.FadeOut
                };
                _timelineClips.Add(newClip);
            }
        }

        private void RemoveFromIcons_Click(object sender, RoutedEventArgs e)
        {
            var target = _selectedClip;
            // Handle if click from ContextMenu
            if (sender is MenuItem mi && mi.Tag is VideoClip clip) target = clip;
            else if (sender is System.Windows.Controls.Button btn && btn.Tag is VideoClip clip2) target = clip2;

            if (target != null)
            {
                _timelineClips.Remove(target);
                if (_timelineClips.Count == 0) 
                {
                    VideoPreview.Source = null;
                    FilterBtn.IsEnabled = false;
                }
                else LoadVideoToPreview(_timelineClips.Last());
            }
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (_timelineClips.Count == 0) { System.Windows.MessageBox.Show("Add video to timeline first!"); return; }
            
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
                string args = "";
                string ffmpegPreset = "fast";
                if (exportDialog.SelectedHeight <= 480) ffmpegPreset = "ultrafast";
                else if (exportDialog.SelectedHeight <= 720) ffmpegPreset = "fast";
                else ffmpegPreset = "slow";
                
                StringBuilder inputs = new StringBuilder();
                StringBuilder filterV = new StringBuilder();
                StringBuilder filterA = new StringBuilder();
                
                for (int i = 0; i < _timelineClips.Count; i++)
                {
                    var clip = _timelineClips[i];
                    double duration = (clip.EndTime - clip.StartTime).TotalSeconds;
                    if (duration <= 0) duration = clip.TotalDuration.TotalSeconds;

                    inputs.Append($"-ss {clip.StartTime.TotalSeconds} -t {duration} -i \"{clip.Path}\" ");

                    string vTag = $"[v{i}]";
                    filterV.Append($"[{i}:v]scale={exportDialog.SelectedWidth}:{exportDialog.SelectedHeight}:force_original_aspect_ratio=decrease,pad={exportDialog.SelectedWidth}:{exportDialog.SelectedHeight}:(ow-iw)/2:(oh-ih)/2,setpts=PTS-STARTPTS");
                    
                    if (clip.FadeIn) filterV.Append($",fade=t=in:st=0:d=1");
                    if (clip.FadeOut) filterV.Append($",fade=t=out:st={Math.Max(0, duration - 1)}:d=1");
                    
                    if (_activeFilter == "grayscale") filterV.Append(",format=gray");
                    else if (_activeFilter == "sepia") filterV.Append(",colorchannelmixer=.393:.769:.189:0:.349:.686:.168:0:.272:.534:.131");
                    else if (_activeFilter == "cine") filterV.Append(",curves=preset=lighter,eq=saturation=1.2");

                    filterV.Append($"{vTag};");

                    string aTag = $"[a{i}]";
                    filterA.Append($"[{i}:a]aselect=enable='between(t,0,{duration})',asetpts=PTS-STARTPTS");
                    if (clip.FadeIn) filterA.Append($",afade=t=in:st=0:d=1");
                    if (clip.FadeOut) filterA.Append($",afade=t=out:st={Math.Max(0, duration - 1)}:d=1");
                    filterA.Append($"{aTag};");
                }

                string concatV = "";
                string concatA = "";
                for (int i = 0; i < _timelineClips.Count; i++) { concatV += $"[v{i}]"; concatA += $"[a{i}]"; }
                filterV.Append($"{concatV}concat=n={_timelineClips.Count}:v=1:a=0[vout];");
                filterA.Append($"{concatA}concat=n={_timelineClips.Count}:v=0:a=1[a_orig];");

                string finalArgs = inputs.ToString();
                
                if (!string.IsNullOrEmpty(_selectedMusicPath))
                {
                    finalArgs += $"-i \"{_selectedMusicPath}\" ";
                    int musicIndex = _timelineClips.Count;
                    filterA.Append($"[{musicIndex}:a]aloop=loop=-1:size=2e+09[bg];[a_orig][bg]amix=inputs=2:duration=first[aout]");
                    finalArgs += $"-filter_complex \"{filterV}{filterA}\" -map \"[vout]\" -map \"[aout]\" ";
                }
                else
                {
                    finalArgs += $"-filter_complex \"{filterV}{filterA}\" -map \"[vout]\" -map \"[a_orig]\" ";
                }

                finalArgs += $"-r {exportDialog.SelectedFPS} -c:v libx264 -preset {ffmpegPreset} -pix_fmt yuv420p -c:a aac -b:a 192k -shortest -y \"{outputPath}\"";
                args = finalArgs;

                var processInfo = new ProcessStartInfo { FileName = ffmpegPath, Arguments = args, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
                await Task.Run(() => { using var process = System.Diagnostics.Process.Start(processInfo); process?.WaitForExit(); });

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
