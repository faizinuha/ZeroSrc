using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace ZeroMix.Recorder
{
    /// <summary>
    /// Screen Studio Style Recorder.
    /// GPU-first architecture with physics-based camera.
    /// </summary>
    public class ScreenStudioRecorder : IDisposable
    {
        // Core components
        private DXGICapturer? _capturer;
        private GPUCompositor? _compositor;
        private VirtualCamera? _camera;
        private CursorTracker? _cursorTracker;
        private HardwareEncoder? _encoder;

        private string _ffmpegPath;
        private int _framerate;
        private bool _isRecording = false;
        private Thread? _recordingThread;
        private Stopwatch _recordingTimer = new();

        public bool IsRecording => _isRecording;
        public string Duration => _isRecording ? _recordingTimer.Elapsed.ToString(@"mm\:ss") : "00:00";

        public ScreenStudioRecorder(string ffmpegPath, int framerate = 30)
        {
            _ffmpegPath = ffmpegPath;
            _framerate = framerate;

            // Initialize capture system
            _capturer = new DXGICapturer();
            
            if (_capturer.IsInitialized)
            {
                _compositor = new GPUCompositor(_capturer.Device, _capturer.Width, _capturer.Height);
                _camera = new VirtualCamera(_capturer.Width, _capturer.Height);
                _cursorTracker = new CursorTracker();
            }
        }

        public void StartRecording(string outputPath)
        {
            if (_isRecording || _capturer == null || !_capturer.IsInitialized) return;

            _encoder = new HardwareEncoder(_ffmpegPath, _capturer.Device, _capturer.Context, _capturer.Width, _capturer.Height, _framerate);
            _encoder.Start(outputPath);

            _isRecording = true;
            _recordingTimer.Restart();
            _camera?.Reset();

            _recordingThread = new Thread(RecordingLoop) { IsBackground = true, Priority = ThreadPriority.Highest };
            _recordingThread.Start();
        }

        private void RecordingLoop()
        {
            double ticksPerFrame = Stopwatch.Frequency / (double)_framerate;
            var masterClock = Stopwatch.StartNew();
            long frameIndex = 0;

            while (_isRecording)
            {
                long currentTicks = masterClock.ElapsedTicks;
                long expectedFrames = (long)(currentTicks / ticksPerFrame);

                if (frameIndex > expectedFrames)
                {
                    double waitMs = (ticksPerFrame * frameIndex - currentTicks) * 1000.0 / Stopwatch.Frequency;
                    if (waitMs > 1) Thread.Sleep((int)waitMs);
                    continue;
                }

                _cursorTracker?.Update();
                if (_camera != null && _cursorTracker != null)
                {
                    _camera.Update(_cursorTracker);
                }

                var rawFrame = _capturer?.CaptureFrame();
                if (rawFrame == null)
                {
                    frameIndex++;
                    continue;
                }

                if (_compositor != null && _camera != null && _cursorTracker != null)
                {
                    _compositor.Compose(rawFrame, _camera.X, _camera.Y, _camera.Zoom, _cursorTracker.X, _cursorTracker.Y, _cursorTracker.IsLeftClick);
                    _encoder?.QueueFrame(_compositor.OutputTexture);
                }

                frameIndex++;

                if (expectedFrames - frameIndex > _framerate * 2)
                {
                    masterClock.Restart();
                    frameIndex = 0;
                }
            }
        }

        public void StopRecording()
        {
            if (!_isRecording) return;
            _isRecording = false;
            _recordingTimer.Stop();
            _recordingThread?.Join(TimeSpan.FromSeconds(2));
            _encoder?.Stop();
            _encoder?.Dispose();
            _encoder = null;
        }

        public void Dispose()
        {
            StopRecording();
            _compositor?.Dispose();
            _capturer?.Dispose();
        }
    }
}
