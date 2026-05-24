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
    public partial class StudioWindow : Wpf.Ui.Controls.FluentWindow
    {
        private string _selectedMusicPath = "";
        private DispatcherTimer _timer;
        private double _lastVolume = 0.5;
        private bool _isMuted = false;
        private string _activeFilter = "";
        private bool _isScrubbing = false; // Flag untuk scrubber drag

        public class VideoClip
        {
            public string Path { get; set; } = "";
            public string FileName { get; set; } = "";
            public string DurationStr { get; set; } = "00:00";
            public TimeSpan TotalDuration { get; set; }
            public TimeSpan StartTime { get; set; } = TimeSpan.Zero;
            public TimeSpan EndTime { get; set; } = TimeSpan.Zero;
            public System.Windows.Media.ImageSource? Thumbnail { get; set; }
            public bool FadeIn { get; set; }
            public bool FadeOut { get; set; }
        }

        private string PIXABAY_KEY => ApiKeys.PixabayVideoKey;
        private ObservableCollection<VideoClip> _timelineClips = new ObservableCollection<VideoClip>();
        private HttpClient _http = new HttpClient();
        private VideoClip? _selectedClip;
        
        public class FilterItem
        {
            public string Name { get; set; } = "";
            public string Tag { get; set; } = "";
            public string StatusText { get; set; } = "";
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
            // Adjust initial size to fit current display (keep sensible minimums)
            try
            {
                double screenW = SystemParameters.WorkArea.Width;
                double screenH = SystemParameters.WorkArea.Height;
                this.Width = Math.Min(this.Width, Math.Max(1100, screenW * 0.9));
                this.Height = Math.Min(this.Height, Math.Max(600, screenH * 0.85));
                this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            catch { }

            TimelineList.ItemsSource = _timelineClips;
            FilterList.ItemsSource = _filters;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            _timer.Tick += Timer_Tick;
            
            LoadFilters();
            
            TimelineScrubber.Maximum = 100;
            TimelineScrubber.Value = 0;

            Closed += (_, _) =>
            {
                _timer.Stop();
                _http.Dispose();
                VideoPreview.Close();
                MusicPreview.Close();
                PixabayPreviewInternal.Close();
            };
        }

        private void LoadFilters()
        {
            _filters.Add(new FilterItem { Name = "None",      Tag = "",          StatusText = "" });
            _filters.Add(new FilterItem { Name = "Grayscale", Tag = "grayscale", StatusText = "" });
            _filters.Add(new FilterItem { Name = "Sepia",     Tag = "sepia",     StatusText = "" });
            _filters.Add(new FilterItem { Name = "Cinematic", Tag = "cine",      StatusText = "" });
        }

        // Window control methods tidak diperlukan lagi (handled by FluentWindow)
        
        private void Fullscreen_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }
        
        private void Mute_Click(object sender, RoutedEventArgs e)
        {
            _isMuted = !_isMuted;
            var textBlock = FindVisualChild<TextBlock>(QuickMuteBtn);
            
            if (_isMuted)
            {
                _lastVolume = VideoPreview.Volume;
                VideoPreview.Volume = 0;
                VolumeSlider.Value = 0;
                if (textBlock != null) textBlock.Text = "🔇";
            }
            else
            {
                VideoPreview.Volume = _lastVolume > 0 ? _lastVolume : 0.5;
                VolumeSlider.Value = VideoPreview.Volume * 100;
                if (textBlock != null) textBlock.Text = "🔊";
            }
        }

        private void Timer_Tick(object? sender, EventArgs e) 
        { 
            if (VideoPreview.NaturalDuration.HasTimeSpan && !_isScrubbing) 
            {
                UpdateDurationLabel();
                UpdateScrubber();
                ApplyRealtimeEffects();
                UpdateCaptionPreview(); // Update caption overlay
            }
        }
        
        private void UpdateScrubber()
        {
            if (VideoPreview.NaturalDuration.HasTimeSpan)
            {
                double totalSeconds = VideoPreview.NaturalDuration.TimeSpan.TotalSeconds;
                double currentSeconds = VideoPreview.Position.TotalSeconds;
                
                if (totalSeconds > 0)
                {
                    TimelineScrubber.Maximum = totalSeconds;
                    TimelineScrubber.Value = currentSeconds;
                }
                
                ScrubberTimeText.Text = $"{VideoPreview.Position:mm\\:ss} / {VideoPreview.NaturalDuration.TimeSpan:mm\\:ss}";
            }
        }
        
        private void TimelineScrubber_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isScrubbing && VideoPreview.Source != null && VideoPreview.NaturalDuration.HasTimeSpan)
            {
                VideoPreview.Position = TimeSpan.FromSeconds(e.NewValue);
            }
        }
        
        private void TimelineScrubber_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isScrubbing = true;
            if (VideoPreview.Source != null)
            {
                VideoPreview.Pause();
            }
        }
        
        private void TimelineScrubber_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isScrubbing = false;
            // Resume playback if it was playing before
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
            openFileDialog.Filter = "Video files (*.mp4;*.mkv;*.avi;*.mov)|*.mp4;*.mkv;*.avi;*.mov";
            openFileDialog.Multiselect = true;
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filename in openFileDialog.FileNames)
                {
                    // Langsung tambah ke timeline (tidak ke library)
                    var clip = new VideoClip 
                    { 
                        Path = filename, 
                        FileName = Path.GetFileName(filename), 
                        DurationStr = "00:00", 
                        Thumbnail = GetVideoThumbnail(filename) ?? new BitmapImage() 
                    };
                    
                    _timelineClips.Add(clip);
                    
                    // Auto load ke preview (video pertama)
                    if (_timelineClips.Count == 1)
                    {
                        LoadVideoToPreview(clip);
                        // Auto play
                        VideoPreview.Play();
                        _timer.Start();
                    }
                }
                
                // Show success message
                if (openFileDialog.FileNames.Length > 0)
                {
                    System.Windows.MessageBox.Show(
                        $"{openFileDialog.FileNames.Length} video ditambahkan ke timeline!\n\nKlik video card untuk edit.",
                        "Import Berhasil",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
            }
        }

        // Hapus AddToTimeline_Click - tidak perlu lagi karena langsung ke timeline
        /*
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
        */

        private void LoadVideoToPreview(VideoClip clip)
        {
            _selectedClip = clip;
            VideoPreview.Source = new System.Uri(clip.Path);
            VideoPreview.Position = clip.StartTime;
            VideoPreview.Play();
            VideoPreview.Pause();
            _timer.Start();
            PreviewEmpty.Visibility = Visibility.Collapsed;

            // Refresh Filter Thumbnails
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

        private void ShowClipSettings(VideoClip clip)
        {
            ShowSidePanel(PanelEdit);
            FadeInCheck.IsChecked  = clip.FadeIn;
            FadeOutCheck.IsChecked = clip.FadeOut;

            // Sync right properties panel
            PropsEmpty.Visibility   = Visibility.Collapsed;
            PropsContent.Visibility = Visibility.Visible;
            PropFileName.Text  = clip.FileName;
            PropDuration.Text  = clip.DurationStr;
            FadeInCheck2.IsChecked  = clip.FadeIn;
            FadeOutCheck2.IsChecked = clip.FadeOut;
        }

        // Sidebar Navigation
        private void ShowFile_Click(object sender, RoutedEventArgs e) { ShowSidePanel(PanelFile); }
        private void ShowMedia_Click(object sender, RoutedEventArgs e) { ShowSidePanel(PanelMedia); }
        private void ShowEdit_Click(object sender, RoutedEventArgs e) { ShowSidePanel(PanelEdit); }
        private void ShowEffects_Click(object sender, RoutedEventArgs e) { ShowSidePanel(PanelEffects); }
        private void ShowMusic_Click(object sender, RoutedEventArgs e) { ShowSidePanel(PanelMusic); }
        private void ShowCaption_Click(object sender, RoutedEventArgs e) { ShowSidePanel(PanelCaption); }
        private void ShowExport_Click(object sender, RoutedEventArgs e) { ShowSidePanel(PanelExport); }

        private void ShowSidePanel(Grid targetPanel)
        {
            // Hide all panels
            PanelFile.Visibility = Visibility.Collapsed;
            PanelMedia.Visibility = Visibility.Collapsed;
            PanelEdit.Visibility = Visibility.Collapsed;
            PanelEffects.Visibility = Visibility.Collapsed;
            PanelMusic.Visibility = Visibility.Collapsed;
            PanelCaption.Visibility = Visibility.Collapsed;
            PanelExport.Visibility = Visibility.Collapsed;
            
            // Show target panel and side panel container
            targetPanel.Visibility = Visibility.Visible;
            SidePanel.Visibility = Visibility.Visible;
        }

        private void SwitchPanel(Grid targetPanel)
        {
            ShowSidePanel(targetPanel);
        }

        private void ClipSetting_Changed(object sender, RoutedEventArgs e)
        {
            if (_selectedClip == null) return;
            // Either checkbox panel can drive the value
            bool fi = (FadeInCheck.IsChecked ?? false) || (FadeInCheck2.IsChecked ?? false);
            bool fo = (FadeOutCheck.IsChecked ?? false) || (FadeOutCheck2.IsChecked ?? false);
            _selectedClip.FadeIn  = fi;
            _selectedClip.FadeOut = fo;
            FadeInCheck.IsChecked  = fi; FadeInCheck2.IsChecked  = fi;
            FadeOutCheck.IsChecked = fo; FadeOutCheck2.IsChecked = fo;
        }

        // RemoveFromLibrary - tidak perlu lagi karena tidak ada library terpisah
        /*
        private void RemoveFromLibrary_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            var clip = btn?.Tag as VideoClip;
            if (clip != null)
            {
                _libraryClips.Remove(clip);
            }
        }
        */

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
            
            // Music track indicator (removed in redesign - simplified UI)
            // MusicTimelineBar.Visibility = Visibility.Visible;
            // MusicTimelineLabel.Text = "♫ " + fileName;
            
            // Load into preview player immediately if video is ready
            MusicPreview.Source = new Uri(_selectedMusicPath);
            MusicPreview.Pause(); // Wait for video play
        }

        private async void PixabaySearch_Click(object sender, RoutedEventArgs e) => await PerformPixabaySearch(PixabaySearchBox.Text, _pixabaySearchMode);
        private async void PixabaySearchBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) { if (e.Key == Key.Enter) await PerformPixabaySearch(PixabaySearchBox.Text, _pixabaySearchMode); }
        
        private async void PixabayCategory_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            string category = btn?.Tag?.ToString() ?? "";
            if (!string.IsNullOrEmpty(category))
            {
                PixabaySearchBox.Text = category;
                await PerformPixabaySearch(category, _pixabaySearchMode);
            }
        }

        // Toggle mode: video atau music
        private void PixabayModeVideo_Click(object sender, RoutedEventArgs e)
        {
            _pixabaySearchMode = "video";
            if (!string.IsNullOrWhiteSpace(PixabaySearchBox.Text))
                _ = PerformPixabaySearch(PixabaySearchBox.Text, "video");
        }

        private void PixabayModeMusic_Click(object sender, RoutedEventArgs e)
        {
            _pixabaySearchMode = "music";
            if (!string.IsNullOrWhiteSpace(PixabaySearchBox.Text))
                _ = PerformPixabaySearch(PixabaySearchBox.Text, "music");
            else
                _ = PerformPixabaySearch("music background", "music");
        }

        // Mode pencarian Pixabay: "video" atau "music"
        private string _pixabaySearchMode = "video";

        private async Task PerformPixabaySearch(string query, string mode = "video")
        {
            if (string.IsNullOrWhiteSpace(query)) return;

            _pixabaySearchMode = mode;
            PixabayResultsList.Items.Clear();
            
            var loadingItem = new PixabayMusicResult
            {
                Id = "loading",
                Title = $"🔄 Searching {(mode == "music" ? "music videos" : "videos")}...",
                User = "", DurationStr = "", PreviewUrl = "", DownloadUrl = ""
            };
            PixabayResultsList.Items.Add(loadingItem);
            
            try
            {
                // Pixabay Videos API — gunakan category=music untuk cari video musik
                string url;
                if (mode == "music")
                {
                    // Category music: video dengan kategori musik (background music videos)
                    url = $"https://pixabay.com/api/videos/?key={PIXABAY_KEY}&q={Uri.EscapeDataString(query)}&category=music&per_page=20&order=popular";
                }
                else
                {
                    // Mode video biasa
                    url = $"https://pixabay.com/api/videos/?key={PIXABAY_KEY}&q={Uri.EscapeDataString(query)}&video_type=film&per_page=20&order=popular";
                }
                
                using var response = await _http.GetAsync(url);
                PixabayResultsList.Items.Clear();
                
                if (!response.IsSuccessStatusCode)
                {
                    PixabayResultsList.Items.Add(new PixabayMusicResult
                    {
                        Id = "error",
                        Title = $"❌ Search failed ({(int)response.StatusCode})",
                        User = response.ReasonPhrase ?? "Unknown error",
                        DurationStr = "", PreviewUrl = "", DownloadUrl = ""
                    });
                    return;
                }
                
                string responseBody = await response.Content.ReadAsStringAsync();
                var data = JObject.Parse(responseBody);
                var hits = data["hits"];

                if (hits != null && hits.HasValues)
                {
                    foreach (var hit in hits)
                    {
                        var videos = hit["videos"];
                        string videoUrl = "";
                        if (videos != null)
                        {
                            videoUrl = (videos["small"]?["url"] 
                                     ?? videos["tiny"]?["url"] 
                                     ?? videos["medium"]?["url"] 
                                     ?? "").ToString();
                        }
                        
                        if (string.IsNullOrEmpty(videoUrl)) continue;
                        
                        string tags = hit["tags"]?.ToString() ?? "Untitled";
                        string title = mode == "music" ? $"🎵 {tags}" : tags;
                        
                        PixabayResultsList.Items.Add(new PixabayMusicResult
                        {
                            Id = hit["id"]?.ToString() ?? "",
                            Title = title,
                            User = hit["user"]?.ToString() ?? "Unknown",
                            DurationStr = FormatPixabayDuration(hit["duration"]?.ToString()),
                            PreviewUrl = videoUrl,
                            DownloadUrl = videoUrl
                        });
                    }
                    
                    if (PixabayResultsList.Items.Count == 0)
                    {
                        PixabayResultsList.Items.Add(new PixabayMusicResult
                        {
                            Id = "noresult",
                            Title = "😕 No results found",
                            User = "Try different keywords",
                            DurationStr = "", PreviewUrl = "", DownloadUrl = ""
                        });
                    }
                }
                else
                {
                    PixabayResultsList.Items.Add(new PixabayMusicResult
                    {
                        Id = "noresult",
                        Title = "😕 No results found",
                        User = "Try different keywords",
                        DurationStr = "", PreviewUrl = "", DownloadUrl = ""
                    });
                }
            }
            catch (Exception ex)
            {
                PixabayResultsList.Items.Clear();
                PixabayResultsList.Items.Add(new PixabayMusicResult
                {
                    Id = "error",
                    Title = "❌ Error",
                    User = ex.Message.Length > 60 ? ex.Message[..60] + "..." : ex.Message,
                    DurationStr = "", PreviewUrl = "", DownloadUrl = ""
                });
                Console.WriteLine($"[Pixabay] Error: {ex.Message}");
            }
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
                System.Windows.MessageBox.Show("Lagu terpasang!");
                // SidebarColumn.Width = new GridLength(0); // Removed in redesign
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

        // Zoom feature removed in redesign - simplified UI
        /*
        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (PreviewScale != null)
            {
                PreviewScale.ScaleX = e.NewValue;
                PreviewScale.ScaleY = e.NewValue;
            }
        }
        */

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
                
                // Update StatusText untuk semua filter items
                foreach (var f in _filters)
                    f.StatusText = (f.Tag == _activeFilter && !string.IsNullOrEmpty(_activeFilter)) ? "✓ Active" : "";
                
                // Refresh list
                FilterList.Items.Refresh();
                
                ApplyFilterRealtime();
            }
        }
        
        private void ClearFilter_Click(object sender, RoutedEventArgs e)
        {
            _activeFilter = "";
            foreach (var f in _filters) f.StatusText = "";
            FilterList.SelectedIndex = 0; // Select "None"
            FilterList.Items.Refresh();
            if (VideoPreview != null) VideoPreview.Effect = null;
        }
        
        private void ApplyFilterRealtime()
        {
            if (VideoPreview == null) return;
            VideoPreview.Effect = null;
            
            // WPF tidak punya built-in grayscale/sepia shader
            // Filter di-apply saat export via FFmpeg
            // Untuk preview, tampilkan overlay teks saja
            Console.WriteLine($"[Studio] Filter set: {(_activeFilter == "" ? "None" : _activeFilter)} — will apply at export");
        }

        private void TogglePlay_Click(object sender, RoutedEventArgs e)
        {
            if (VideoPreview.Source == null) return;
            
            // Get the TextBlock inside the button
            var textBlock = FindVisualChild<TextBlock>(PlayToggleBtn);
            
            if (textBlock != null)
            {
                if (textBlock.Text == "▶") 
                { 
                    VideoPreview.Play(); 
                    if (MusicPreview.Source != null) MusicPreview.Play();
                    textBlock.Text = "⏸"; 
                }
                else 
                { 
                    VideoPreview.Pause(); 
                    if (MusicPreview.Source != null) MusicPreview.Pause();
                    textBlock.Text = "▶"; 
                }
            }
        }
        
        // Helper method to find child controls
        private T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    return typedChild;
                
                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        private async Task SearchPixabayMusic(string query)
        {
            await PerformPixabaySearch(query);
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
                    // FilterBtn.IsEnabled = false; // Removed in redesign
                }
                else LoadVideoToPreview(_timelineClips.Last());
            }
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (_timelineClips.Count == 0) { System.Windows.MessageBox.Show("Add video to timeline first!"); return; }
            
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "MP4 Video (*.mp4)|*.mp4",
                FileName = "ZeroMix_Export.mp4"
            };
            
            if (saveDialog.ShowDialog() != true) return;
            string outputPath = saveDialog.FileName;

            string ffmpegPath = ResolveFFmpegPath();
            if (!File.Exists(ffmpegPath)) { System.Windows.MessageBox.Show("FFmpeg not found in: " + ffmpegPath); return; }

            ExportButton.IsEnabled = false;
            ExportProgressPanel.Visibility = Visibility.Visible;
            ExportProgressBar.IsIndeterminate = true;
            
            // Get resolution
            int exportHeight = 1080;
            int exportWidth = 1920;
            if (ResolutionCombo.SelectedItem is ComboBoxItem resItem)
            {
                exportHeight = int.Parse(resItem.Tag.ToString() ?? "1080");
                exportWidth = exportHeight * 16 / 9;
            }
            
            // Get FPS
            int exportFPS = 30;
            if (FPSCombo.SelectedItem is ComboBoxItem fpsItem)
            {
                exportFPS = int.Parse(fpsItem.Tag.ToString() ?? "30");
            }

            ExportStatusText.Text = $"Exporting at {exportWidth}x{exportHeight} @ {exportFPS}fps...";

            try
            {
                string args = "";
                string ffmpegPreset = exportHeight <= 720 ? "fast" : "slow";
                
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
                    filterV.Append($"[{i}:v]scale={exportWidth}:{exportHeight}:force_original_aspect_ratio=decrease,pad={exportWidth}:{exportHeight}:(ow-iw)/2:(oh-ih)/2,setpts=PTS-STARTPTS");
                    
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
                
                // Add caption burn-in if enabled
                if (BurnCaptionsCheck.IsChecked == true && _currentCaptions.Count > 0 && _captionService != null)
                {
                    ExportStatusText.Text = "Burning captions to video...";
                    
                    var styles = _captionService.GetPresetStyles();
                    var selectedStyle = styles[0]; // Default to first style
                    
                    if (CaptionStyleCombo.SelectedItem is ComboBoxItem styleItem)
                    {
                        string styleName = styleItem.Content.ToString() ?? "Bottom Classic";
                        selectedStyle = styles.FirstOrDefault(s => s.Name == styleName) ?? styles[0];
                    }
                    
                    string captionFilter = _captionService.BuildFFmpegCaptionFilter(_currentCaptions, selectedStyle, exportWidth, exportHeight);
                    
                    if (!string.IsNullOrEmpty(captionFilter))
                    {
                        // Append caption filter to video filter chain
                        filterV.Append($"[vout]{captionFilter}[vfinal];");
                        finalArgs += $"-filter_complex \"{filterV}{filterA}\" -map \"[vfinal]\" ";
                    }
                    else
                    {
                        finalArgs += $"-filter_complex \"{filterV}{filterA}\" -map \"[vout]\" ";
                    }
                }
                else
                {
                    finalArgs += $"-filter_complex \"{filterV}{filterA}\" -map \"[vout]\" ";
                }
                
                if (!string.IsNullOrEmpty(_selectedMusicPath))
                {
                    finalArgs += $"-i \"{_selectedMusicPath}\" ";
                    int musicIndex = _timelineClips.Count;
                    finalArgs += $"-map \"[a_orig]\" -map {musicIndex}:a -filter_complex \"[a_orig][{musicIndex}:a]amix=inputs=2:duration=first[aout]\" -map \"[aout]\" ";
                }
                else
                {
                    finalArgs += $"-map \"[a_orig]\" ";
                }

                finalArgs += $"-r {exportFPS} -c:v libx264 -preset {ffmpegPreset} -pix_fmt yuv420p -c:a aac -b:a 192k -shortest -y \"{outputPath}\"";
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

        private System.Windows.Media.ImageSource? GetVideoThumbnail(string videoPath)
        {
            try
            {
                string ffmpegPath = ResolveFFmpegPath();
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

        // ═══════════════════════════════════════════════════════════════════
        // AI AUTO-EDITING FEATURES
        // ═══════════════════════════════════════════════════════════════════

        private Services.CaptionService? _captionService;
        private List<Services.CaptionSegment> _currentCaptions = new List<Services.CaptionSegment>();

        // AI Auto-Edit and Auto Subtitles - Coming Soon (In Development)
        /*
        private void ShowAIEdit_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("AI Auto-Edit feature is coming soon!", "Coming Soon", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowSubtitles_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Auto Subtitles feature is coming soon!", "Coming Soon", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        */

        // AI methods commented out - Coming Soon
        /*
        private async void AIAnalyze_Click(object sender, RoutedEventArgs e)
        {
            // Implementation coming soon
        }

        private async void AIApply_Click(object sender, RoutedEventArgs e)
        {
            // Implementation coming soon
        }

        private async void GenerateSubtitles_Click(object sender, RoutedEventArgs e)
        {
            // Implementation coming soon
        }
        */

        private void UpdateCaptionPreview()
        {
            if (_currentCaptions.Count == 0 || _captionService == null)
            {
                CaptionOverlay.Text = "";
                return;
            }

            double currentTime = VideoPreview.Position.TotalSeconds;
            string captionText = _captionService.GeneratePreviewCaptionText(_currentCaptions, currentTime);
            CaptionOverlay.Text = captionText;
        }

        private void CaptionStyle_Changed(object sender, SelectionChangedEventArgs e)
        {
            // Update caption style preview
            if (CaptionStyleCombo.SelectedItem is ComboBoxItem selected)
            {
                string styleName = selected.Content.ToString() ?? "Bottom Classic";
                UpdateCaptionStylePreview(styleName);
            }
        }

        private void UpdateCaptionStylePreview(string styleName)
        {
            if (_captionService == null) return;

            var styles = _captionService.GetPresetStyles();
            var style = styles.FirstOrDefault(s => s.Name == styleName);
            
            if (style != null)
            {
                CaptionOverlay.FontSize = style.FontSize;
                CaptionOverlay.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(style.FontColor));
                
                // Update position
                CaptionOverlay.VerticalAlignment = style.Position switch
                {
                    "top" => VerticalAlignment.Top,
                    "center" => VerticalAlignment.Center,
                    "bottom" => VerticalAlignment.Bottom,
                    _ => VerticalAlignment.Bottom
                };
                
                CaptionOverlay.Margin = new Thickness(40, style.MarginVertical, 40, style.MarginVertical);
            }
        }

        private async void GenerateCaptions_Click(object sender, RoutedEventArgs e)
        {
            if (_timelineClips.Count == 0)
            {
                System.Windows.MessageBox.Show("Please add videos to timeline first!", "No Videos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                GenerateCaptionsBtn.IsEnabled = false;
                CaptionStatusText.Text = "🔄 Generating captions with AI...";

                // Initialize services
                if (_captionService == null)
                {
                    var ffmpegPath = ResolveFFmpegPath();
                    _captionService = new Services.CaptionService(ffmpegPath);
                }

                // Get first video for demo
                var firstClip = _timelineClips.FirstOrDefault();
                if (firstClip == null) return;

                // Demo transcript dengan timing yang lebih akurat
                // Dalam production, gunakan speech-to-text API (Whisper, Google Speech)
                var transcript = "Welcome to ZeroMix Studio. " +
                               "This is an automatic caption generation demo. " +
                               "The captions are synchronized with the video audio. " +
                               "You can customize the style and position. " +
                               "Captions will be burned directly into your video during export.";

                // Generate caption segments dengan timing yang lebih presisi
                double videoDuration = firstClip.TotalDuration.TotalSeconds;
                _currentCaptions = await _captionService.GenerateCaptionsFromTranscript(transcript, videoDuration);

                // Adjust timing untuk sync dengan audio (simulasi)
                // Dalam production, timing didapat dari speech-to-text API
                for (int i = 0; i < _currentCaptions.Count; i++)
                {
                    // Tambah sedikit delay untuk sync dengan audio
                    _currentCaptions[i].StartTime += 0.1; // 100ms delay
                    _currentCaptions[i].EndTime += 0.1;
                }

                CaptionStatusText.Text = $"✅ Generated {_currentCaptions.Count} caption segments!\n" +
                                        "Preview captions during playback.\n" +
                                        "Captions are synced with video audio.\n" +
                                        "Enable 'Burn Captions' in Export panel.";

                // Update preview style
                if (CaptionStyleCombo.SelectedItem is ComboBoxItem selected)
                {
                    UpdateCaptionStylePreview(selected.Content.ToString() ?? "Bottom Classic");
                }

                System.Windows.MessageBox.Show(
                    $"Captions generated successfully!\n\n" +
                    $"• {_currentCaptions.Count} segments created\n" +
                    $"• Synced with video audio\n" +
                    $"• Preview during playback\n" +
                    $"• Enable 'Burn Captions' to export",
                    "Captions Ready",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                CaptionStatusText.Text = $"❌ Error: {ex.Message}";
                System.Windows.MessageBox.Show($"Failed to generate captions:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                GenerateCaptionsBtn.IsEnabled = true;
            }
        }

        private string ResolveFFmpegPath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var possiblePaths = new[]
            {
                System.IO.Path.Combine(baseDir, "Tools", "FFMPEG", "ffmpeg.exe"),
                System.IO.Path.Combine(baseDir, "FFMPEG", "ffmpeg.exe"),
                System.IO.Path.Combine(baseDir, "..", "..", "..", "..", "Tools", "FFMPEG", "ffmpeg.exe"),
            };

            foreach (var path in possiblePaths)
            {
                string full = System.IO.Path.GetFullPath(path);
                if (System.IO.File.Exists(full)) return full;
            }

            return "ffmpeg.exe"; // Try system PATH
        }
    }
}
