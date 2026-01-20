using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Concurrent;
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
        private string _outputPath;
        private int _width;
        private int _height;
        private int _framerate;
        private string _encoder;

        private ID3D11Texture2D? _stagingTexture;
        private ID3D11DeviceContext _context;
        
        private Thread? _encoderThread;
        private ConcurrentQueue<byte[]> _frameQueue = new();
        private bool _isRunning = false;
        private long _framesWritten = 0;
        
        // Pre-allocated buffer pool
        private ConcurrentBag<byte[]> _bufferPool = new();

        public bool IsInitialized { get; private set; }
        public long FramesWritten => _framesWritten;

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
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                string output = p?.StandardOutput.ReadToEnd() ?? "";
                
                // Priority: NVIDIA -> Intel -> AMD -> CPU
                if (output.Contains("h264_nvenc")) return "h264_nvenc";
                if (output.Contains("h264_qsv")) return "h264_qsv";
                if (output.Contains("h264_amf")) return "h264_amf";
            }
            catch { }
            
            return "libx264";
        }

        public void Start(string outputPath, string micDevice = "No Audio", string speakerDevice = "No Audio")
        {
            _outputPath = outputPath;
            
            string encoderArgs = _encoder switch
            {
                "h264_nvenc" => "-c:v h264_nvenc -preset p4 -tune hq -rc vbr -cq 23",
                "h264_qsv" => "-c:v h264_qsv -global_quality 23 -preset fast",
                "h264_amf" => "-c:v h264_amf -quality speed -rc cqp -qp_i 23 -qp_p 23",
                _ => "-c:v libx264 -preset ultrafast -crf 23 -threads 0"
            };

            // Audio Inputs
            string audioInputs = "";
            int audioChannelCount = 0;
            
            if (micDevice != "No Audio" && !micDevice.Contains("System") && !micDevice.Contains("Default"))
            {
                audioInputs += $"-f dshow -i audio=\"{micDevice}\" ";
                audioChannelCount++;
            }

            if (speakerDevice != "No Audio")
            {
                // Use wasapi loopback for system audio
                audioInputs += "-f wasapi -i default "; 
                audioChannelCount++;
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
                          $"{audioInputs} " +
                          $"{encoderArgs} -pix_fmt yuv420p -r {_framerate} {mapArgs} {audioCodecArgs} -y \"{_outputPath}\"";

            Console.WriteLine($"[HardwareEncoder] Starting FFmpeg with args: {args}");

            var psi = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardError = true // Capture errors
            };

            try
            {
                _ffmpegProcess = Process.Start(psi);
                
                if (_ffmpegProcess != null)
                {
                    // Log FFmpeg errors to Console for terminal debugging
                    _ffmpegProcess.ErrorDataReceived += (s, e) => {
                        if (!string.IsNullOrEmpty(e.Data))
                            Console.WriteLine($"[FFMPEG-LOG] {e.Data}");
                    };
                    _ffmpegProcess.BeginErrorReadLine();
                    Console.WriteLine("[HardwareEncoder] FFmpeg process started successfully.");
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
            if (!IsInitialized || _stagingTexture == null || !_isRunning) return;

            if (_frameQueue.Count > 10) return;

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
                            Console.WriteLine($"[HardwareEncoder] Info: Written {Interlocked.Read(ref _framesWritten)} frames so far...");
                        }
                    }
                    finally
                    {
                        _context.Unmap(_stagingTexture, 0);
                    }
                }
            }
            catch { }
        }

        private void EncoderLoop()
        {
            while (_isRunning || !_frameQueue.IsEmpty)
            {
                if (_frameQueue.TryDequeue(out byte[]? frame))
                {
                    try
                    {
                        _ffmpegProcess?.StandardInput.BaseStream.Write(frame, 0, frame.Length);
                        _ffmpegProcess?.StandardInput.BaseStream.Flush();
                        Interlocked.Increment(ref _framesWritten);
                        _bufferPool.Add(frame);
                    }
                    catch { break; }
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _encoderThread?.Join(TimeSpan.FromSeconds(5));
            try
            {
                _ffmpegProcess?.StandardInput.Close();
                _ffmpegProcess?.WaitForExit(3000);
            }
            catch { }
        }

        public void Dispose()
        {
            Stop();
            _stagingTexture?.Dispose();
            _ffmpegProcess?.Dispose();
        }
    }
}
