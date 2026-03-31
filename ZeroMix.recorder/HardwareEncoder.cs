using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Concurrent;
using System.Linq;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace ZeroMix.Recorder
{
    /// <summary>
    /// Hardware encoder that writes GPU textures directly to MP4.
    /// Uses FFmpeg in a SMART way - async pipe with proper timing.
    /// NOT blocking stdin like before.
    /// </summary>
    public class HardwareEncoder : IDisposable
    {
        private Process? _ffmpegProcess;
        private string _ffmpegPath;
        private string _outputPath = "";
        private int _width;
        private int _height;
        private int _framerate;
        private string _encoder = "";

        private ID3D11Texture2D? _stagingTexture;
        private ID3D11DeviceContext _context;
        
        private Thread? _encoderThread;
        private ConcurrentQueue<byte[]> _frameQueue = new();
        private bool _isRunning = false;
        private long _framesWritten = 0;
        private bool _encoderDead = false;
        
        // Pre-allocated buffer pool
        private ConcurrentBag<byte[]> _bufferPool = new();

        public bool IsInitialized { get; private set; }
        public long FramesWritten => _framesWritten;
        public bool IsEncoderAlive => !_encoderDead && _ffmpegProcess != null && !_ffmpegProcess.HasExited;

        public HardwareEncoder(string ffmpegPath, ID3D11Device device, ID3D11DeviceContext context, int width, int height, int framerate = 30)
        {
            _ffmpegPath = ffmpegPath;
            _context = context;
            _width = width;
            _height = height;
            _framerate = framerate;
            
            // Detect best encoder
            _encoder = DetectEncoder();
            
            // Create staging texture for GPU -> CPU transfer
            var stagingDesc = new Texture2DDescription
            {
                Width = (uint)width,
                Height = (uint)height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.B8G8R8A8_UNorm,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Staging,
                BindFlags = BindFlags.None,
                CPUAccessFlags = CpuAccessFlags.Read,
                MiscFlags = ResourceOptionFlags.None
            };
            _stagingTexture = device.CreateTexture2D(stagingDesc);
            
            // Pre-allocate buffers
            int bufferSize = width * height * 4;
            for (int i = 0; i < 10; i++)
            {
                _bufferPool.Add(new byte[bufferSize]);
            }

            IsInitialized = true;
        }

        private string DetectEncoder()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = _ffmpegPath,
                    Arguments = "-encoders",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                string output = p?.StandardOutput.ReadToEnd() ?? "";
                
                // List of encoders to try, in priority order
                string[] encodersToTry = { "h264_nvenc", "h264_qsv", "h264_amf", "h264_mf" };
                
                // First filter: Check if encoder is in list
                foreach (var encoder in encodersToTry)
                {
                    if (output.Contains(encoder))
                    {
                        // Second filter: Actually test if encoder works
                        if (TestEncoder(encoder))
                        {
                            Console.WriteLine($"[HardwareEncoder] Selected encoder: {encoder}");
                            return encoder;
                        }
                        else
                        {
                            Console.WriteLine($"[HardwareEncoder] Encoder {encoder} listed but not working, trying next...");
                        }
                    }
                }
                
                Console.WriteLine("[HardwareEncoder] No hardware encoders available, using libx264 (CPU)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HardwareEncoder] Error detecting encoders: {ex.Message}");
            }
            
            return "libx264";
        }

        private bool TestEncoder(string encoderName)
        {
            try
            {
                // Test if encoder can be initialized with a dummy encode command
                var psi = new ProcessStartInfo
                {
                    FileName = _ffmpegPath,
                    // Test with minimal parameters - just check if encoder loads
                    Arguments = $"-f lavfi -i color=c=black:s=320x240:d=0.1 -c:v {encoderName} -f null -",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using var p = Process.Start(psi);
                if (p == null) return false;
                
                // Wait for process to complete with timeout
                bool completed = p.WaitForExit(3000);
                
                if (!completed)
                {
                    p.Kill();
                    return false;
                }
                
                // Exit code 0 means success
                bool success = p.ExitCode == 0;
                
                if (success)
                {
                    Console.WriteLine($"[HardwareEncoder] ✓ Encoder {encoderName} test passed");
                }
                else
                {
                    Console.WriteLine($"[HardwareEncoder] ✗ Encoder {encoderName} test failed (exit code: {p.ExitCode})");
                    string errorOutput = p.StandardError.ReadToEnd();
                    if (!string.IsNullOrEmpty(errorOutput))
                    {
                        Console.WriteLine($"[HardwareEncoder]   Error: {errorOutput.Split('\n').FirstOrDefault()}");
                    }
                }
                
                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HardwareEncoder] Error testing encoder {encoderName}: {ex.Message}");
                return false;
            }
        }

        public void Start(string outputPath, string micDevice = "No Audio", string speakerDevice = "No Audio")
        {
            _outputPath = outputPath;
            
            string encoderArgs = _encoder switch
            {
                "h264_nvenc" => "-c:v h264_nvenc -preset fast -tune hq -rc vbr -cq 23",
                "h264_qsv" => "-c:v h264_qsv -q 23 -preset faster -look_ahead 0",
                "h264_amf" => "-c:v h264_amf -quality speed -rc cqp -qp_i 23 -qp_p 23",
                "h264_mf" => "-c:v h264_mf -rate_control vbr -quality 23",
                _ => "-c:v libx264 -preset ultrafast -crf 23 -threads 4"
            };

            // Audio Input Strategy (Backward Compatible)
            string audioInputs = "";
            int audioChannelCount = 0;
            
            // Try Microphone via dshow (Windows Universal)
            if (micDevice != "No Audio" && !micDevice.Contains("System") && !micDevice.Contains("Default"))
            {
                try
                {
                    audioInputs += $"-f dshow -i audio=\"{micDevice}\" ";
                    audioChannelCount++;
                    Console.WriteLine($"[HardwareEncoder] Using microphone via dshow: {micDevice}");
                }
                catch
                {
                    Console.WriteLine("[HardwareEncoder] WARNING: Microphone dshow input failed, skipping audio.");
                }
            }

            // System Audio: Skip WASAPI completely - too unreliable on old hardware
            // If user needs system audio, they should use other methods
            if (speakerDevice != "No Audio")
            {
                Console.WriteLine("[HardwareEncoder] WARNING: System audio not supported on this version (use external audio mixer).");
            }

            // Sync video/audio
            string mapArgs = "-map 0:v";
            string audioCodecArgs = "";
            if (audioChannelCount > 0)
            {
                for (int i = 0; i < audioChannelCount; i++)
                    mapArgs += $" -map {i + 1}:a";
                
                audioCodecArgs = "-c:a aac -b:a 128k";
            }

            string args = $"-f rawvideo -pixel_format bgra -video_size {_width}x{_height} " +
                          $"-framerate {_framerate} -i - " +
                          $"{audioInputs.Trim()} " +
                          $"{encoderArgs} -pix_fmt yuv420p -r {_framerate} {mapArgs} {audioCodecArgs.Trim()} -y \"{_outputPath}\"";

            Console.WriteLine($"[HardwareEncoder] FINAL COMMAND: {_ffmpegPath} {args}");

            var psi = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            try
            {
                _ffmpegProcess = Process.Start(psi);
                
                if (_ffmpegProcess != null)
                {
                    bool encoderInitialized = false;
                    string encoderErrorMsg = "";
                    
                    // Log FFmpeg errors to Console for terminal debugging
                    _ffmpegProcess.ErrorDataReceived += (s, e) => {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            Console.WriteLine($"[FFMPEG-LOG] {e.Data}");
                            
                            // Detect encoder initialization errors
                            if (e.Data.Contains("Error while opening encoder") || 
                                e.Data.Contains("Cannot load") ||
                                e.Data.Contains("Unknown encoder"))
                            {
                                encoderErrorMsg = e.Data;
                            }
                        }
                    };
                    _ffmpegProcess.OutputDataReceived += (s, e) => {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            Console.WriteLine($"[FFMPEG-LOG] {e.Data}");
                            
                            // Encoder successfully initialized when we see Stream mapping
                            if (e.Data.Contains("Stream mapping"))
                            {
                                encoderInitialized = true;
                                Console.WriteLine($"[HardwareEncoder] ✓ Encoder {_encoder} initialized successfully!");
                            }
                        }
                    };
                    _ffmpegProcess.BeginErrorReadLine();
                    _ffmpegProcess.BeginOutputReadLine();
                    
                    // Give FFmpeg a moment to initialize and check for errors
                    Thread.Sleep(500);
                    
                    if (_ffmpegProcess.HasExited)
                    {
                        Console.WriteLine($"[HardwareEncoder] ✗ FFmpeg exited immediately (exit code: {_ffmpegProcess.ExitCode})");
                        
                        // If selected encoder failed, fallback to libx264
                        if (!encoderInitialized && _encoder != "libx264")
                        {
                            Console.WriteLine($"[HardwareEncoder] WARNING: Encoder {_encoder} failed!");
                            if (!string.IsNullOrEmpty(encoderErrorMsg))
                            {
                                Console.WriteLine($"[HardwareEncoder] Error: {encoderErrorMsg}");
                            }
                            Console.WriteLine("[HardwareEncoder] Falling back to libx264 (CPU encoding)...");
                            
                            // Retry with libx264
                            _encoder = "libx264";
                            Start(outputPath, micDevice, speakerDevice);
                            return;
                        }
                    }
                    else
                    {
                        Console.WriteLine("[HardwareEncoder] FFmpeg process started successfully.");
                    }
                }
                else
                {
                    Console.WriteLine("[HardwareEncoder] ERROR: Failed to start FFmpeg process.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HardwareEncoder] FATAL ERROR starting FFmpeg: {ex.Message}");
            }

            _isRunning = true;
            _encoderThread = new Thread(EncoderLoop) { IsBackground = true, Priority = ThreadPriority.AboveNormal };
            _encoderThread.Start();
        }

        public void QueueFrame(ID3D11Texture2D texture)
        {
            if (!IsInitialized || _stagingTexture == null || !_isRunning || _encoderDead) return;

            // Check if encoder is still alive before queueing
            if (_ffmpegProcess == null || _ffmpegProcess.HasExited)
            {
                Console.WriteLine($"[HardwareEncoder] ERROR: FFmpeg process is dead (Exit code: {_ffmpegProcess?.ExitCode})");
                _encoderDead = true;
                return;
            }

            if (_frameQueue.Count > 15) return; // Limit queue size to prevent memory bloat

            try
            {
                lock (_context)
                {
                    _context.CopyResource(_stagingTexture, texture);
                    var mapped = _context.Map(_stagingTexture, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None);
                    try
                    {
                        if (!_bufferPool.TryTake(out byte[]? buffer))
                        {
                            buffer = new byte[_width * _height * 4];
                        }

                        int lineSize = _width * 4;
                        for (int y = 0; y < _height; y++)
                        {
                            IntPtr src = IntPtr.Add(mapped.DataPointer, y * (int)mapped.RowPitch);
                            Marshal.Copy(src, buffer, y * lineSize, lineSize);
                        }

                        _frameQueue.Enqueue(buffer);
                        
                        // Health check log every 100 frames
                        if (Interlocked.Read(ref _framesWritten) % 100 == 0 && Interlocked.Read(ref _framesWritten) > 0)
                        {
                            Console.WriteLine($"[HardwareEncoder] Info: Written {Interlocked.Read(ref _framesWritten)} frames so far... Queue: {_frameQueue.Count}");
                        }
                    }
                    finally
                    {
                        _context.Unmap(_stagingTexture, 0);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HardwareEncoder] ERROR in QueueFrame: {ex.GetType().Name} - {ex.Message}");
            }
        }

        private void EncoderLoop()
        {
            int consecutiveErrors = 0;
            const int MAX_CONSECUTIVE_ERRORS = 3;

            try
            {
                while (_isRunning || !_frameQueue.IsEmpty)
                {
                    if (_frameQueue.TryDequeue(out byte[]? frame))
                    {
                        try
                        {
                            // Validate FFmpeg process is still alive
                            if (_ffmpegProcess == null || _ffmpegProcess.HasExited)
                            {
                                Console.WriteLine($"[HardwareEncoder] ERROR: FFmpeg process died! Exit code: {_ffmpegProcess?.ExitCode}");
                                _encoderDead = true;
                                break;
                            }

                            var inputStream = _ffmpegProcess.StandardInput.BaseStream;
                            if (!inputStream.CanWrite)
                            {
                                Console.WriteLine("[HardwareEncoder] ERROR: Cannot write to FFmpeg stdin!");
                                _encoderDead = true;
                                break;
                            }

                            inputStream.Write(frame, 0, frame.Length);
                            inputStream.Flush();
                            Interlocked.Increment(ref _framesWritten);
                            _bufferPool.Add(frame);
                            consecutiveErrors = 0; // Reset error counter on success
                        }
                        catch (ObjectDisposedException ex)
                        {
                            Console.WriteLine($"[HardwareEncoder] Stream disposed: {ex.Message}");
                            _encoderDead = true;
                            break;
                        }
                        catch (IOException ex)
                        {
                            consecutiveErrors++;
                            Console.WriteLine($"[HardwareEncoder] EncoderLoop IO Error ({consecutiveErrors}/{MAX_CONSECUTIVE_ERRORS}): {ex.Message}");
                            
                            if (consecutiveErrors >= MAX_CONSECUTIVE_ERRORS)
                            {
                                Console.WriteLine("[HardwareEncoder] Too many IO errors, stopping encoding...");
                                _encoderDead = true;
                                break;
                            }
                            Thread.Sleep(10);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[HardwareEncoder] EncoderLoop ERROR: {ex.GetType().Name} - {ex.Message}");
                            consecutiveErrors++;
                            
                            if (consecutiveErrors >= MAX_CONSECUTIVE_ERRORS)
                            {
                                Console.WriteLine("[HardwareEncoder] Too many errors, stopping encoding...");
                                _encoderDead = true;
                                break;
                            }
                            Thread.Sleep(10);
                        }
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HardwareEncoder] FATAL ERROR in EncoderLoop: {ex.GetType().Name} - {ex.Message}");
                _encoderDead = true;
            }
        }

        public void Stop()
        {
            _isRunning = false;
            Console.WriteLine("[HardwareEncoder] Stopping encoder...");
            
            try
            {
                // Wait for encoder thread to finish processing queue
                if (_encoderThread != null && !_encoderThread.Join(TimeSpan.FromSeconds(5)))
                {
                    Console.WriteLine("[HardwareEncoder] WARNING: Encoder thread didn't exit in time.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HardwareEncoder] Error waiting for encoder thread: {ex.Message}");
            }
            
            try
            {
                if (_ffmpegProcess != null && !_ffmpegProcess.HasExited)
                {
                    try
                    {
                        // Close stdin to signal EOF
                        _ffmpegProcess.StandardInput?.Close();
                        Console.WriteLine("[HardwareEncoder] Sent EOF to FFmpeg stdin.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[HardwareEncoder] Error closing stdin: {ex.Message}");
                    }

                    // Wait for FFmpeg to finalize video
                    if (!_ffmpegProcess.WaitForExit(5000))
                    {
                        Console.WriteLine("[HardwareEncoder] WARNING: FFmpeg didn't exit in time, killing process.");
                        try
                        {
                            _ffmpegProcess.Kill();
                            _ffmpegProcess.WaitForExit(2000);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[HardwareEncoder] Error killing FFmpeg: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[HardwareEncoder] FFmpeg exited gracefully with code: {_ffmpegProcess.ExitCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HardwareEncoder] Error during Stop: {ex.Message}");
            }
        }

        public void Dispose()
        {
            Stop();
            _stagingTexture?.Dispose();
            _ffmpegProcess?.Dispose();
        }
    }
}
