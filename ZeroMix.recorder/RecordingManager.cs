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
        private GlobalMouseHook _mouseHook;

        public RecordingManager(string ffmpegPath)
        {
            _ffmpegPath = ffmpegPath;
            _mouseHook = new GlobalMouseHook();
            
            // Initialize the GPU-first recorder
            _recorder = new ScreenStudioRecorder(ffmpegPath, 30);
        }

        public string GetDuration() => _recorder?.Duration ?? "00:00";

        public void StartRecording(string outputFileName, int framerate = 30)
        {
            if (_recorder == null || _recorder.IsRecording) return;

            string outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), outputFileName);
            _mouseHook.Install();
            _recorder.StartRecording(outputPath);
        }

        public void StopRecording()
        {
            if (_recorder == null || !_recorder.IsRecording) return;
            _recorder.StopRecording();
            _mouseHook.Uninstall();
        }
    }
}
