using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZeroMix.Services
{
    public class CaptionStyle
    {
        public string Name { get; set; } = "";
        public string FontName { get; set; } = "Arial";
        public int FontSize { get; set; } = 24;
        public string FontColor { get; set; } = "white";
        public string OutlineColor { get; set; } = "black";
        public int OutlineWidth { get; set; } = 2;
        public bool HasShadow { get; set; } = true;
        public bool HasBackground { get; set; } = false;
        public string BackgroundColor { get; set; } = "black@0.5";
        public string Position { get; set; } = "bottom"; // bottom, center, top
        public int MarginVertical { get; set; } = 50;
    }

    public class CaptionSegment
    {
        public double StartTime { get; set; }
        public double EndTime { get; set; }
        public string Text { get; set; } = "";
    }

    public class CaptionService
    {
        private readonly string _ffmpegPath;

        public CaptionService(string ffmpegPath)
        {
            _ffmpegPath = ffmpegPath;
        }

        public List<CaptionStyle> GetPresetStyles()
        {
            return new List<CaptionStyle>
            {
                new CaptionStyle
                {
                    Name = "Bottom Classic",
                    FontName = "Arial",
                    FontSize = 24,
                    FontColor = "white",
                    OutlineColor = "black",
                    OutlineWidth = 2,
                    HasShadow = true,
                    HasBackground = false,
                    Position = "bottom",
                    MarginVertical = 50
                },
                new CaptionStyle
                {
                    Name = "Center Modern",
                    FontName = "Segoe UI",
                    FontSize = 28,
                    FontColor = "white",
                    OutlineColor = "black",
                    OutlineWidth = 3,
                    HasShadow = false,
                    HasBackground = true,
                    BackgroundColor = "black@0.6",
                    Position = "center",
                    MarginVertical = 0
                },
                new CaptionStyle
                {
                    Name = "Minimal",
                    FontName = "Consolas",
                    FontSize = 20,
                    FontColor = "white",
                    OutlineColor = "black",
                    OutlineWidth = 1,
                    HasShadow = false,
                    HasBackground = false,
                    Position = "bottom",
                    MarginVertical = 30
                },
                new CaptionStyle
                {
                    Name = "Bold",
                    FontName = "Impact",
                    FontSize = 32,
                    FontColor = "yellow",
                    OutlineColor = "black",
                    OutlineWidth = 4,
                    HasShadow = true,
                    HasBackground = false,
                    Position = "top",
                    MarginVertical = 50
                }
            };
        }

        public async Task<List<CaptionSegment>> GenerateCaptionsFromTranscript(string transcript, double videoDuration)
        {
            var segments = new List<CaptionSegment>();
            
            // Split transcript into sentences
            var sentences = transcript.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(s => s.Trim())
                                     .Where(s => !string.IsNullOrEmpty(s))
                                     .ToList();

            if (sentences.Count == 0) return segments;

            // Distribute sentences evenly across video duration
            double timePerSegment = videoDuration / sentences.Count;
            
            for (int i = 0; i < sentences.Count; i++)
            {
                segments.Add(new CaptionSegment
                {
                    StartTime = i * timePerSegment,
                    EndTime = (i + 1) * timePerSegment,
                    Text = sentences[i]
                });
            }

            return segments;
        }

        public string GenerateSRTContent(List<CaptionSegment> segments)
        {
            var sb = new StringBuilder();
            
            for (int i = 0; i < segments.Count; i++)
            {
                sb.AppendLine((i + 1).ToString());
                sb.AppendLine($"{FormatSRTTime(segments[i].StartTime)} --> {FormatSRTTime(segments[i].EndTime)}");
                sb.AppendLine(segments[i].Text);
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private string FormatSRTTime(double seconds)
        {
            var ts = TimeSpan.FromSeconds(seconds);
            return $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2},{ts.Milliseconds:D3}";
        }

        public string BuildFFmpegCaptionFilter(List<CaptionSegment> segments, CaptionStyle style, int videoWidth, int videoHeight)
        {
            if (segments == null || segments.Count == 0) return "";

            var filters = new List<string>();

            // Calculate position
            string yPosition = style.Position switch
            {
                "top" => $"y={style.MarginVertical}",
                "center" => "y=(h-text_h)/2",
                "bottom" => $"y=h-text_h-{style.MarginVertical}",
                _ => $"y=h-text_h-{style.MarginVertical}"
            };

            foreach (var segment in segments)
            {
                // Escape special characters in text
                string escapedText = EscapeFFmpegText(segment.Text);

                // Build drawtext filter for this segment
                var filterParts = new List<string>
                {
                    $"text='{escapedText}'",
                    $"fontfile=/Windows/Fonts/{GetFontFile(style.FontName)}",
                    $"fontsize={style.FontSize}",
                    $"fontcolor={style.FontColor}",
                    $"borderw={style.OutlineWidth}",
                    $"bordercolor={style.OutlineColor}",
                    "x=(w-text_w)/2", // Center horizontally
                    yPosition,
                    $"enable='between(t,{segment.StartTime},{segment.EndTime})'",
                    "line_spacing=5"
                };

                // Add shadow if enabled
                if (style.HasShadow)
                {
                    filterParts.Add("shadowx=2");
                    filterParts.Add("shadowy=2");
                    filterParts.Add("shadowcolor=black@0.5");
                }

                // Add background box if enabled
                if (style.HasBackground)
                {
                    filterParts.Add("box=1");
                    filterParts.Add($"boxcolor={style.BackgroundColor}");
                    filterParts.Add("boxborderw=10");
                }

                filters.Add($"drawtext={string.Join(":", filterParts)}");
            }

            return string.Join(",", filters);
        }

        private string GetFontFile(string fontName)
        {
            return fontName switch
            {
                "Arial" => "arial.ttf",
                "Segoe UI" => "segoeui.ttf",
                "Consolas" => "consola.ttf",
                "Impact" => "impact.ttf",
                "Times New Roman" => "times.ttf",
                "Courier New" => "cour.ttf",
                _ => "arial.ttf"
            };
        }

        private string EscapeFFmpegText(string text)
        {
            // Escape special characters for FFmpeg drawtext
            return text.Replace("\\", "\\\\")
                      .Replace("'", "\\'")
                      .Replace(":", "\\:")
                      .Replace("%", "\\%")
                      .Replace("\n", "\\n");
        }

        public async Task<string> BurnCaptionsToVideo(
            string inputVideoPath,
            string outputVideoPath,
            List<CaptionSegment> segments,
            CaptionStyle style,
            int videoWidth,
            int videoHeight,
            Action<string>? progressCallback = null)
        {
            try
            {
                // Build caption filter
                string captionFilter = BuildFFmpegCaptionFilter(segments, style, videoWidth, videoHeight);

                if (string.IsNullOrEmpty(captionFilter))
                {
                    throw new Exception("No captions to burn");
                }

                // Build FFmpeg command
                var args = $"-i \"{inputVideoPath}\" " +
                          $"-vf \"{captionFilter}\" " +
                          $"-c:v libx264 -preset fast -crf 23 " +
                          $"-c:a copy " +
                          $"-y \"{outputVideoPath}\"";

                progressCallback?.Invoke("Starting caption burn-in...");

                // Execute FFmpeg
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _ffmpegPath,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                await Task.Run(() =>
                {
                    using var process = System.Diagnostics.Process.Start(processInfo);
                    if (process != null)
                    {
                        // Read stderr for progress (FFmpeg outputs to stderr)
                        process.ErrorDataReceived += (sender, e) =>
                        {
                            if (!string.IsNullOrEmpty(e.Data))
                            {
                                progressCallback?.Invoke(e.Data);
                            }
                        };
                        process.BeginErrorReadLine();
                        process.WaitForExit();

                        if (process.ExitCode != 0)
                        {
                            throw new Exception($"FFmpeg exited with code {process.ExitCode}");
                        }
                    }
                });

                progressCallback?.Invoke("Caption burn-in completed!");
                return outputVideoPath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to burn captions: {ex.Message}", ex);
            }
        }

        public string GeneratePreviewCaptionText(List<CaptionSegment> segments, double currentTime)
        {
            var activeSegment = segments.FirstOrDefault(s => currentTime >= s.StartTime && currentTime <= s.EndTime);
            return activeSegment?.Text ?? "";
        }
    }
}
