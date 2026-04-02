using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows;
using System.Drawing;

namespace ZeroMix.Recorder
{
    /// <summary>
    /// Screen Studio Style Recorder.
    /// GPU-first architecture with physics-based camera.
    /// </summary>
    public class ScreenStudioRecorder : IDisposable
    {
        // Core components
        private DXGICapturer? _dxgiCapturer;
        private GDICapturer? _gdiCapturer;
        private GPUCompositor? _compositor;
        private VirtualCamera? _camera;
        private CursorTracker? _cursorTracker;
        private HardwareEncoder? _encoder;

        private string _ffmpegPath;
        private int _framerate;
        private bool _isRecording = false;
        private Thread? _recordingThread;
        private Stopwatch _recordingTimer = new();
        private IntPtr? _captureHandle;
        private System.Windows.Rect? _captureRect;
        public bool IsZoomEnabled { get; set; } = true;

        public bool IsRecording => _isRecording;
        public bool IsInitialized => (_dxgiCapturer?.IsInitialized ?? false) || (_gdiCapturer?.IsInitialized ?? false);
        
        public bool IsUsingGDI => _gdiCapturer != null && _gdiCapturer.IsInitialized;
        public string Duration => _isRecording ? _recordingTimer.Elapsed.ToString(@"mm\:ss") : "00:00";

        public ScreenStudioRecorder(string ffmpegPath, int framerate = 30)
        {
            _ffmpegPath = ffmpegPath;
            _framerate = framerate;

            Console.WriteLine($"[ScreenStudioRecorder] Initializing with FFmpeg: {ffmpegPath}");
            
            // 1. Try DXGI (GPU-based) first
            _dxgiCapturer = new DXGICapturer();
            
            if (_dxgiCapturer.IsInitialized)
            {
                Console.WriteLine("[ScreenStudioRecorder] DXGICapturer initialized successfully.");
                _compositor = new GPUCompositor(_dxgiCapturer.Device, _dxgiCapturer.Width, _dxgiCapturer.Height);
                _camera = new VirtualCamera(_dxgiCapturer.Width, _dxgiCapturer.Height);
                _cursorTracker = new CursorTracker();
            }
            else
            {
                Console.WriteLine("[ScreenStudioRecorder] DXGICapturer failed. Falling back to GDI...");
                
                // 2. Try GDI (CPU-based) if DXGI fails
                // We still need a D3D11 device for the compositor/encoder pipeline
                if (_dxgiCapturer.Device != null)
                {
                    _gdiCapturer = new GDICapturer(_dxgiCapturer.Device, _dxgiCapturer.Context);
                    if (_gdiCapturer.IsInitialized)
                    {
                        Console.WriteLine("[ScreenStudioRecorder] ✓ GDICapturer initialized successfully!");
                        _compositor = new GPUCompositor(_dxgiCapturer.Device, _gdiCapturer.Width, _gdiCapturer.Height);
                        _camera = new VirtualCamera(_gdiCapturer.Width, _gdiCapturer.Height);
                        _cursorTracker = new CursorTracker();
                    }
                    else
                    {
                        Console.WriteLine("[ScreenStudioRecorder] ERROR: GDICapturer failed also.");
                    }
                }
                else
                {
                    Console.WriteLine("[ScreenStudioRecorder] ERROR: No D3D11 Device available for GDI fallback.");
                }
            }
            
            if (IsInitialized)
                Console.WriteLine("[ScreenStudioRecorder] ✓ All systems GO!");
            else
                Console.WriteLine("[ScreenStudioRecorder] CRITICAL: No capture system initialized!");
        }

        public void StartRecording(string outputPath, string micDevice = "No Audio", string speakerDevice = "No Audio", IntPtr? captureHandle = null, System.Windows.Rect? captureRect = null)
        {
            if (_isRecording)
            {
                Console.WriteLine($"[ScreenStudioRecorder] Start blocked: Already recording");
                return;
            }

            _captureHandle = captureHandle;
            _captureRect = captureRect;

            if (!IsInitialized)
            {
                Console.WriteLine($"[ScreenStudioRecorder] ERROR: Not initialized! No capture system available.");
                throw new InvalidOperationException("Screen capture system not initialized. This might be due to GPU driver issues.");
            }

            try
            {
                int width = _dxgiCapturer?.Width ?? _gdiCapturer?.Width ?? 1920;
                int height = _dxgiCapturer?.Height ?? _gdiCapturer?.Height ?? 1080;
                
                // Verify we have valid device for encoder
                var device = _dxgiCapturer?.Device;
                var context = _dxgiCapturer?.Context;
                
                if (device == null || context == null)
                {
                    Console.WriteLine($"[ScreenStudioRecorder] ERROR: No D3D11 device available for encoding!");
                    throw new InvalidOperationException("Failed to get D3D11 device context for encoding.");
                }

                Console.WriteLine($"[ScreenStudioRecorder] Launching Encoder: {width}x{height} -> {outputPath}");
                Console.WriteLine($"[ScreenStudioRecorder] Using {(IsUsingGDI ? "GDI" : "DXGI")} capture mode");
                
                _encoder = new HardwareEncoder(_ffmpegPath, device, context, width, height, _framerate);
                _encoder.Start(outputPath, micDevice, speakerDevice);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ScreenStudioRecorder] CRITICAL ERROR in StartRecording: {ex.Message}");
                throw;
            }

            _isRecording = true;
            _recordingTimer.Restart();
            _camera?.Reset();

            _recordingThread = new Thread(RecordingLoop) { IsBackground = true, Priority = ThreadPriority.Highest };
            _recordingThread.Start();
            Debug.WriteLine("[ScreenStudioRecorder] ✓ Recording started!");
        }

        private void RecordingLoop()
        {
            double ticksPerFrame = Stopwatch.Frequency / (double)_framerate;
            var masterClock = Stopwatch.StartNew();
            long frameIndex = 0;
            int consecutiveErrors = 0;
            const int maxConsecutiveErrors = 10;

            try
            {
                while (_isRecording)
                {
                    try
                    {
                        long currentTicks = masterClock.ElapsedTicks;
                        long expectedFrame = (long)(currentTicks / ticksPerFrame);

                        // Tunggu sampai waktu frame berikutnya
                        while (frameIndex > expectedFrame && _isRecording)
                        {
                            Thread.Sleep(1);
                            currentTicks = masterClock.ElapsedTicks;
                            expectedFrame = (long)(currentTicks / ticksPerFrame);
                        }

                        if (!_isRecording) break;

                        // Check if encoder is still alive
                        if (_encoder == null || !_encoder.IsEncoderAlive)
                        {
                            Console.WriteLine("[ScreenStudioRecorder] ERROR: Encoder died! Stopping recording...");
                            _isRecording = false;
                            break;
                        }

                        _cursorTracker?.Update();
                        if (_camera != null && _cursorTracker != null)
                        {
                            _camera.Update(_cursorTracker, IsZoomEnabled);
                        }

                        Vortice.Direct3D11.ID3D11Texture2D? rawFrame = null;
                        if (_dxgiCapturer != null && _dxgiCapturer.IsInitialized)
                            rawFrame = _dxgiCapturer.CaptureFrame();
                        else if (_gdiCapturer != null && _gdiCapturer.IsInitialized)
                            rawFrame = _gdiCapturer.CaptureFrame();

                        if (rawFrame == null)
                        {
                            frameIndex++;
                            consecutiveErrors++;
                            if (consecutiveErrors >= maxConsecutiveErrors)
                            {
                                Console.WriteLine($"[ScreenStudioRecorder] Too many consecutive frame capture failures ({consecutiveErrors}), stopping recording.");
                                _isRecording = false;
                                break;
                            }
                            continue;
                        }

                        consecutiveErrors = 0; // Reset error count on success

                        if (_compositor != null && _camera != null && _cursorTracker != null)
                        {
                            System.Drawing.RectangleF? crop = null;
                            if (_captureRect.HasValue)
                            {
                                var r = _captureRect.Value;
                                crop = new System.Drawing.RectangleF((float)r.Left, (float)r.Top, (float)r.Width, (float)r.Height);
                            }
                            else if (_captureHandle.HasValue && _captureHandle.Value != IntPtr.Zero)
                            {
                                if (GetWindowRect(_captureHandle.Value, out RECT_WIN rect))
                                {
                                    crop = new System.Drawing.RectangleF(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
                                }
                            }

                            _compositor.Compose(rawFrame, _camera.X, _camera.Y, _camera.Zoom, _cursorTracker.X, _cursorTracker.Y, _cursorTracker.IsLeftClick, crop);
                            _encoder?.QueueFrame(_compositor.OutputTexture);
                        }

                        if (frameIndex % 100 == 0 && frameIndex > 0)
                        {
                            Console.WriteLine($"[ScreenStudioRecorder] Loop: Processed {frameIndex} frames...");
                        }

                        frameIndex++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ScreenStudioRecorder] ERROR in recording loop iteration: {ex.GetType().Name} - {ex.Message}");
                        consecutiveErrors++;
                        if (consecutiveErrors >= maxConsecutiveErrors)
                        {
                            Console.WriteLine($"[ScreenStudioRecorder] Too many consecutive errors ({consecutiveErrors}), stopping recording.");
                            _isRecording = false;
                            break;
                        }
                        // Continue processing instead of crashing
                        Thread.Sleep(10);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ScreenStudioRecorder] FATAL ERROR in RecordingLoop: {ex.GetType().Name} - {ex.Message}");
                _isRecording = false;
            }
        }

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT_WIN lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT_WIN
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public void StopRecording()
        {
            if (!_isRecording) return;
            
            try
            {
                _isRecording = false;
                _recordingTimer.Stop();
                
                Console.WriteLine($"[ScreenStudioRecorder] Stopping recording...");
                
                // Give recording thread time to gracefully exit
                try
                {
                    if (_recordingThread != null && !_recordingThread.Join(TimeSpan.FromSeconds(5)))
                    {
                        Console.WriteLine("[ScreenStudioRecorder] WARNING: Recording thread didn't exit in time.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ScreenStudioRecorder] Error waiting for recording thread: {ex.Message}");
                }
                
                // Stop and dispose encoder safely
                try
                {
                    if (_encoder != null)
                    {
                        Console.WriteLine("[ScreenStudioRecorder] Stopping encoder...");
                        _encoder.Stop();
                        _encoder.Dispose();
                        Console.WriteLine($"[ScreenStudioRecorder] Encoder stopped. Frames written: {_encoder.FramesWritten}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ScreenStudioRecorder] Error disposing encoder: {ex.Message}");
                }
                finally
                {
                    _encoder = null;
                }
                
                Console.WriteLine("[ScreenStudioRecorder] Recording fully stopped.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ScreenStudioRecorder] ERROR during StopRecording: {ex.GetType().Name} - {ex.Message}");
            }
        }

        public void Dispose()
        {
            StopRecording();
            _compositor?.Dispose();
            _dxgiCapturer?.Dispose();
            _gdiCapturer?.Dispose();
        }
    }
}
