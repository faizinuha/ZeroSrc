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
            
            // Initialize the GPU-first recorder
            _recorder = new ScreenStudioRecorder(ffmpegPath, 30);
        }

        public string GetDuration() => _recorder?.Duration ?? "00:00";

        public void StartRecording(string outputFileName, int framerate = 30, string micDevice = "No Audio", string speakerDevice = "No Audio", IntPtr? captureHandle = null, System.Windows.Rect? captureRect = null)
        {
            if (_recorder == null || _recorder.IsRecording) return;

            string myVideos = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
            string zeroRecordDir = Path.Combine(myVideos, "ZeroRecord");
            
            if (!Directory.Exists(zeroRecordDir))
            {
                Directory.CreateDirectory(zeroRecordDir);
            }

            string outputPath = Path.Combine(zeroRecordDir, outputFileName);
            Console.WriteLine($"[RecordingManager] Starting recording to: {outputPath}");

            _recorder.StartRecording(outputPath, micDevice, speakerDevice, captureHandle, captureRect);
        }

        public void StopRecording()
        {
            if (_recorder == null || !_recorder.IsRecording) return;
            _recorder.StopRecording();
        }
    }
}
