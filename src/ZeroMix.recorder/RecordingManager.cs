using System;
using System.Diagnostics;
using System.IO;

namespace ZeroMix.Recorder
{
    /// <summary>
    /// Recording Manager - Wrapper for ScreenStudioRecorder.
    /// Maintains backward compatibility with existing UI.
    /// </summary>
    public class RecordingManager
    {
        private ScreenStudioRecorder? _recorder;
        private string _ffmpegPath;

        public bool IsInitialized => _recorder?.IsInitialized ?? false;
        public string FFmpegPath => _ffmpegPath;
        public ScreenStudioRecorder? Recorder => _recorder;

        public RecordingManager(string ffmpegPath)
        {
            _ffmpegPath = ffmpegPath;
            
            // Validate FFmpeg path exists
            if (!File.Exists(ffmpegPath))
            {
                Console.WriteLine($"[RecordingManager] ERROR: FFmpeg not found at: {ffmpegPath}");
                throw new FileNotFoundException($"FFmpeg executable not found at: {ffmpegPath}");
            }
            
            Console.WriteLine($"[RecordingManager] FFmpeg found: {ffmpegPath}");
            
            // Initialize the GPU-first recorder
            _recorder = new ScreenStudioRecorder(ffmpegPath, 30);
        }

        public string GetDuration() => _recorder?.Duration ?? "00:00";

        public bool IsPaused => _recorder?.IsPaused ?? false;

        public void Pause()  => _recorder?.Pause();
        public void Resume() => _recorder?.Resume();

        public void StartRecording(string outputFileName, int framerate = 30, string micDevice = "No Audio",
                                   string speakerDevice = "No Audio", IntPtr? captureHandle = null,
                                   System.Windows.Rect? captureRect = null,
                                   string format = "mp4", int bitrate = 8000)
        {
            if (_recorder == null || _recorder.IsRecording)
            {
                Console.WriteLine($"[RecordingManager] Recording blocked: Recorder null={_recorder == null} or already recording={_recorder?.IsRecording ?? false}");
                return;
            }

            try
            {
                string myVideos = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
                string zeroRecordDir = Path.Combine(myVideos, "ZeroRecord");
                
                if (!Directory.Exists(zeroRecordDir))
                {
                    Directory.CreateDirectory(zeroRecordDir);
                }

                string outputPath = Path.Combine(zeroRecordDir, outputFileName);
                Console.WriteLine($"[RecordingManager] Starting recording to: {outputPath}");

                _recorder.StartRecording(outputPath, micDevice, speakerDevice, captureHandle, captureRect, format, bitrate);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"[RecordingManager] INVALID STATE ERROR: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RecordingManager] ERROR starting recording: {ex.GetType().Name} - {ex.Message}");
                throw;
            }
        }

        public void StopRecording()
        {
            if (_recorder == null || !_recorder.IsRecording) return;
            
            try
            {
                _recorder.StopRecording();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RecordingManager] ERROR stopping recording: {ex.GetType().Name} - {ex.Message}");
            }
        }
    }
}
