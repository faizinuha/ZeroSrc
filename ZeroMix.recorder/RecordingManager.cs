using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Timers;

namespace ZeroMix.Recorder
{
    public class RecordingManager
    {
        private Process? _ffmpegProcess;
        private System.Timers.Timer _mouseTracker;
        private string _ffmpegPath;
        private string? _outputPath;
        private bool _isRecording = false;
        private GlobalMouseHook _mouseHook;

        // Settings for Zoom
        private int _zoomWidth = 1280;
        private int _zoomHeight = 720;
        private double _currentZoomLevel = 1.0;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        public RecordingManager(string ffmpegPath)
        {
            _ffmpegPath = ffmpegPath;
            _mouseHook = new GlobalMouseHook();
            _mouseHook.MouseWheelScrolled += (zoomIn) => AdjustZoom(zoomIn);
            
            // Mouse tracker update every 100ms
            _mouseTracker = new System.Timers.Timer(100);
            _mouseTracker.Elapsed += UpdateMousePosition;
        }

        public void StartRecording(string outputFileName, int framerate = 30)
        {
            if (_isRecording) return;
            _mouseHook.Install();

            _outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), outputFileName);
            
            // FFMPEG Command for Screen Capture with dynamic framerate
            string args = $"-f gdigrab -framerate {framerate} -i desktop -vcodec libx264 -preset ultrafast -crf 18 \"{_outputPath}\"";

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true // To stop it gracefully by sending 'q'
            };

            _ffmpegProcess = Process.Start(psi);
            _isRecording = true;
            _mouseTracker.Start();
            
            Debug.WriteLine($"[ZeroRecord] Recording started: {_outputPath}");
        }

        public void StopRecording()
        {
            if (!_isRecording || _ffmpegProcess == null) return;

            try
            {
                // Send 'q' to ffmpeg to stop and save file properly
                _ffmpegProcess.StandardInput.WriteLine("q");
                _ffmpegProcess.WaitForExit(5000);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ZeroRecord] Error stopping: {ex.Message}");
                _ffmpegProcess.Kill();
            }
            finally
            {
                _isRecording = false;
                _mouseTracker.Stop();
                _mouseHook.Uninstall();
                Debug.WriteLine("[ZeroRecord] Recording stopped.");
            }
        }

        private void UpdateMousePosition(object? sender, ElapsedEventArgs e)
        {
            POINT p;
            if (GetCursorPos(out p))
            {
                // Logic for Follow Mouse will be implemented here
                // We will send updated crop parameters to FFMPEG filter if needed
            }
        }

        public void AdjustZoom(bool zoomIn)
        {
            if (zoomIn) _currentZoomLevel += 0.1;
            else _currentZoomLevel -= 0.1;

            if (_currentZoomLevel < 1.0) _currentZoomLevel = 1.0;
            if (_currentZoomLevel > 4.0) _currentZoomLevel = 4.0;
        }
    }
}
