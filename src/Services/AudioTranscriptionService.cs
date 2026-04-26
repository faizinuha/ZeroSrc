using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace ZeroMix.Services
{
    public class AudioTranscriptionService
    {
        private readonly string _ffmpegPath;

        public AudioTranscriptionService(string ffmpegPath)
        {
            _ffmpegPath = ffmpegPath;
        }

        /// <summary>
        /// Extract audio dari video untuk transcription
        /// </summary>
        public async Task<string> ExtractAudioFromVideo(string videoPath, string outputAudioPath)
        {
            var args = $"-i \"{videoPath}\" -vn -acodec pcm_s16le -ar 16000 -ac 1 \"{outputAudioPath}\"";
            
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _ffmpegPath,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new Exception($"FFmpeg audio extraction failed: {error}");
            }

            return outputAudioPath;
        }

        /// <summary>
        /// Analyze audio untuk detect silence (untuk auto-cut)
        /// </summary>
        public async Task<SilenceDetection[]> DetectSilence(string audioPath, double silenceThreshold = -30, double minDuration = 0.5)
        {
            var args = $"-i \"{audioPath}\" -af silencedetect=noise={silenceThreshold}dB:d={minDuration} -f null -";
            
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _ffmpegPath,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            // Parse silence detection output
            var silences = new System.Collections.Generic.List<SilenceDetection>();
            var lines = output.Split('\n');
            
            double? silenceStart = null;
            foreach (var line in lines)
            {
                if (line.Contains("silence_start:"))
                {
                    var parts = line.Split(':');
                    if (parts.Length > 1 && double.TryParse(parts[1].Trim(), out double start))
                    {
                        silenceStart = start;
                    }
                }
                else if (line.Contains("silence_end:") && silenceStart.HasValue)
                {
                    var parts = line.Split('|');
                    if (parts.Length > 0)
                    {
                        var endPart = parts[0].Split(':');
                        if (endPart.Length > 1 && double.TryParse(endPart[1].Trim(), out double end))
                        {
                            silences.Add(new SilenceDetection
                            {
                                Start = silenceStart.Value,
                                End = end,
                                Duration = end - silenceStart.Value
                            });
                            silenceStart = null;
                        }
                    }
                }
            }

            return silences.ToArray();
        }

        /// <summary>
        /// Detect audio peaks (untuk sync dengan effects)
        /// </summary>
        public async Task<AudioPeak[]> DetectAudioPeaks(string audioPath, double threshold = -10)
        {
            // Simplified peak detection - bisa diperluas dengan librosa atau audio analysis library
            var peaks = new System.Collections.Generic.List<AudioPeak>();
            
            // Placeholder: Dalam implementasi real, gunakan audio analysis library
            // Untuk sekarang, return sample data
            peaks.Add(new AudioPeak { Time = 5.0, Amplitude = 0.8 });
            peaks.Add(new AudioPeak { Time = 12.5, Amplitude = 0.9 });
            peaks.Add(new AudioPeak { Time = 20.0, Amplitude = 0.85 });

            return await Task.FromResult(peaks.ToArray());
        }
    }

    public class SilenceDetection
    {
        public double Start { get; set; }
        public double End { get; set; }
        public double Duration { get; set; }
    }

    public class AudioPeak
    {
        public double Time { get; set; }
        public double Amplitude { get; set; }
    }
}
