using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Windows;
using System.Threading;

namespace ZeroMix.Recorder
{
    public class RecordingManager
    {
        private Process? _ffmpegProcess;
        private string _ffmpegPath;
        private string? _outputPath;
        private bool _isRecording = false;
        private GlobalMouseHook _mouseHook;
        private Stopwatch _recordingTimer = new Stopwatch();

        private double _zoom = 1.0;
        private double _targetZoom = 1.0;
        private float _camX = 0, _camY = 0;
        private float _velX = 0, _velY = 0;

        private int _screenWidth;
        private int _screenHeight;
        private string _bestEncoder = "libx264";

        [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int vKey);

        public RecordingManager(string ffmpegPath)
        {
            _ffmpegPath = ffmpegPath;
            _mouseHook = new GlobalMouseHook();
            _mouseHook.MouseWheelScrolled += (zoomIn) => {
                if (zoomIn) _targetZoom = Math.Min(_targetZoom + 0.1, 1.5);
                else _targetZoom = Math.Max(_targetZoom - 0.1, 1.0);
            };
            _screenWidth = (int)SystemParameters.PrimaryScreenWidth;
            _screenHeight = (int)SystemParameters.PrimaryScreenHeight;
            _camX = _screenWidth / 2;
            _camY = _screenHeight / 2;
            _ = DetectBestEncoderAsync();
        }

        private async System.Threading.Tasks.Task DetectBestEncoderAsync()
        {
            await System.Threading.Tasks.Task.Run(() => {
                if (CheckCodec("h264_qsv")) _bestEncoder = "h264_qsv";
                else if (CheckCodec("h264_nvenc")) _bestEncoder = "h264_nvenc";
            });
        }

        private bool CheckCodec(string name)
        {
            try {
                var psi = new ProcessStartInfo {
                    FileName = _ffmpegPath, Arguments = "-encoders",
                    RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                return p?.StandardOutput.ReadToEnd().Contains(name) ?? false;
            } catch { return false; }
        }

        public string GetDuration() => _isRecording ? _recordingTimer.Elapsed.ToString(@"mm\:ss") : "00:00";

        public void StartRecording(string outputFileName, int framerate = 24) // Turunkan ke 24 biar i3 stabil
        {
            if (_isRecording) return;
            _outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), outputFileName);

            string encoderArgs = _bestEncoder switch {
                "h264_qsv" => "-c:v h264_qsv -global_quality 25 -preset fast -b:v 5000k",
                "h264_nvenc" => "-c:v h264_nvenc -preset fast -cq 23",
                _ => "-c:v libx264 -preset ultrafast -crf 23 -tune zerolatency"
            };

            // Tambahkan flag -re dan sync audio/video
            string args = $"-f rawvideo -pixel_format bgr0 -video_size {_screenWidth}x{_screenHeight} -framerate {framerate} -i - " +
                          $"{encoderArgs} -pix_fmt yuv420p -movflags +faststart -y \"{_outputPath}\"";

            try {
                _ffmpegProcess = Process.Start(new ProcessStartInfo {
                    FileName = _ffmpegPath, Arguments = args,
                    UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true
                });
                if (_ffmpegProcess != null) {
                    _isRecording = true;
                    _recordingTimer.Restart();
                    _mouseHook.Install();
                    new Thread(() => CaptureLoop(framerate)) { IsBackground = true, Priority = ThreadPriority.Highest }.Start();
                }
            } catch { }
        }

        private void CaptureLoop(int framerate)
        {
            double frameDurationMs = 1000.0 / framerate;
            Stopwatch sw = Stopwatch.StartNew();

            // PRE-ALLOCATE BITMAPS (Cegah Laptop Panas)
            using var fullBmp = new Bitmap(_screenWidth, _screenHeight, PixelFormat.Format32bppRgb);
            using var gFull = Graphics.FromImage(fullBmp);
            using var studioFrame = new Bitmap(_screenWidth, _screenHeight, PixelFormat.Format32bppRgb);
            using var g = Graphics.FromImage(studioFrame);
            
            // Pro Background (Simulasi Gradasi dengan Brush tetap)
            var bgBrush = new LinearGradientBrush(new Rectangle(0, 0, _screenWidth, _screenHeight), 
                Color.FromArgb(30, 32, 38), Color.FromArgb(15, 16, 20), 45f);

            long startTime = sw.ElapsedMilliseconds;
            int framesSent = 0;

            try {
                while (_isRecording) 
                {
                    // SYNC TOTAL: Pastikan frame dikirim sesuai target waktu frame ke-N
                    long targetTime = startTime + (long)(framesSent * frameDurationMs);
                    long now = sw.ElapsedMilliseconds;

                    if (now < targetTime) {
                        Thread.Sleep((int)(targetTime - now));
                    }

                    // 1. Logic Mouse & Zoom
                    POINT p; GetCursorPos(out p);
                    bool isLBtnDown = (GetAsyncKeyState(0x01) & 0x8000) != 0;

                    if (isLBtnDown) _targetZoom = Math.Max(_targetZoom, 1.3);
                    else if (_targetZoom > 1.0) _targetZoom = Math.Max(1.0, _targetZoom - 0.01);

                    // PHYSICS (Slightly Dampened for Stability)
                    _velX = (_velX + (p.X - _camX) * 0.15f) * 0.6f;
                    _velY = (_velY + (p.Y - _camY) * 0.15f) * 0.6f;
                    _camX += _velX;
                    _camY += _velY;
                    _zoom += (_targetZoom - _zoom) * 0.1;

                    // 2. CAPTURE & RENDER
                    gFull.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size(_screenWidth, _screenHeight));

                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.Bilinear; // Pakai Bilinear biar lebih ringan dari Bicubic
                    g.FillRectangle(bgBrush, 0, 0, _screenWidth, _screenHeight);

                    int m = (int)(25 / _zoom); 
                    Rectangle dest = new Rectangle(m, m, _screenWidth - (m*2), _screenHeight - (m*2));
                    int zW = (int)(_screenWidth / _zoom);
                    int zH = (int)(_screenHeight / _zoom);
                    int zX = (int)Math.Clamp(_camX - (zW / 2), 0, _screenWidth - zW);
                    int zY = (int)Math.Clamp(_camY - (zH / 2), 0, _screenHeight - zH);

                    using (GraphicsPath path = GetRoundedRect(dest, 10)) {
                        g.SetClip(path);
                        g.DrawImage(fullBmp, dest, new Rectangle(zX, zY, zW, zH), GraphicsUnit.Pixel);
                        g.ResetClip();
                        using (var pBorder = new Pen(Color.FromArgb(40, 255, 255, 255), 1)) g.DrawPath(pBorder, path);
                    }

                    // Native Cursor Look
                    float curX = (float)((p.X - zX) * dest.Width / zW) + dest.X;
                    float curY = (float)((p.Y - zY) * dest.Height / zH) + dest.Y;
                    g.FillEllipse(Brushes.White, curX - 4, curY - 4, 8, 8);
                    g.DrawEllipse(Pens.Black, curX - 4, curY - 4, 8, 8);

                    SendToFfmpeg(studioFrame);
                    framesSent++;
                }
            } catch { }
        }

        private GraphicsPath GetRoundedRect(Rectangle rect, int r)
        {
            GraphicsPath path = new GraphicsPath();
            int d = r * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void SendToFfmpeg(Bitmap bmp)
        {
            if (_ffmpegProcess == null || _ffmpegProcess.HasExited) return;
            try {
                BitmapData d = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);
                byte[] b = new byte[d.Stride * d.Height];
                Marshal.Copy(d.Scan0, b, 0, b.Length);
                _ffmpegProcess.StandardInput.BaseStream.Write(b, 0, b.Length);
                _ffmpegProcess.StandardInput.BaseStream.Flush();
                bmp.UnlockBits(d);
            } catch { }
        }

        public void StopRecording()
        {
            _isRecording = false;
            Thread.Sleep(500);
            if (_ffmpegProcess != null) {
                try { _ffmpegProcess.StandardInput.Close(); _ffmpegProcess.WaitForExit(3000); } catch { }
            }
            _mouseHook.Uninstall();
        }

        [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X; public int Y; }
        [DllImport("user32.dll")] public static extern bool GetCursorPos(out POINT lpPoint);
    }
}
