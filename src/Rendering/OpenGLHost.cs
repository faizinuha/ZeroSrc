using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using OpenTK.Graphics.OpenGL4;
using ZeroMix.Native;

namespace ZeroMix.Rendering
{
    /// <summary>
    /// HwndHost yang menampilkan model Live2D (Cubism 4) yang di-render dengan OpenGL
    /// di dalam window WPF transparan (AllowsTransparency="True").
    ///
    /// ── Catatan teknis penting (verified, bukan asumsi) ──
    /// OpenGL TIDAK bisa mempresentasikan per-pixel alpha langsung ke layered window
    /// (WS_EX_LAYERED): MSDN/Khronos menyatakan hardware rendering tidak di-composite
    /// dengan efek transparency layered window. Karena itu:
    ///   1. GL context dibuat di window tersembunyi (non-layered) — murni untuk rendering.
    ///   2. Scene di-render ke MSAA FBO, di-resolve ke FBO readback.
    ///   3. glReadPixels (400x500x4 ≈ 800KB/frame — kecil untuk widget sekecil ini),
    ///      dikonversi ke premultiplied BGRA, lalu UpdateLayeredWindow ke child window
    ///      ber-style WS_EX_LAYERED supaya alpha benar-benar tembus ke desktop/WPF di bawahnya.
    /// Ini satu-satunya cara mendapat true per-pixel alpha dari GL; biaya readback jauh
    /// lebih kecil daripada proses Chromium (msedgewebview2.exe) yang dihapus.
    ///
    /// Alur frame (UI thread, DispatcherTimer ~60fps):
    ///   motion → eye tracking → lip-sync → physics → csmUpdateModel → render → readback.
    /// </summary>
    public sealed class OpenGLHost : HwndHost, IDisposable
    {
        // ── Public API ────────────────────────────────────────────────────

        /// <summary>Model berhasil di-load DAN frame pertama sudah ter-render.</summary>
        public event Action? ModelLoaded;

        /// <summary>Klik pada area host (posisi relatif host, DIP).</summary>
        public event Action<System.Windows.Point>? Clicked;

        /// <summary>Delta pergerakan mouse saat drag (dipakai window untuk pindah).</summary>
        public event Action<int, int>? DragDelta;

        /// <summary>True jika MSAA diaktifkan (di-set SEBELUM LoadModel).</summary>
        public bool Antialias { get; set; } = true;

        /// <summary>Pause render loop (hemat CPU/GPU saat window tidak aktif).</summary>
        public bool IsPaused { get; set; }

        /// <summary>True saat karakter sedang berbicara (lip-sync aktif).</summary>
        public bool IsSpeaking { get; set; }

        public bool IsModelLoaded { get; private set; }
        public bool FirstFrameRendered { get; private set; }
        public CubismModel? Model => _model;

        // ── Native / GL state ─────────────────────────────────────────────

        private IntPtr _glWindow;          // hidden window untuk GL context (non-layered)
        private IntPtr _childWindow;       // visible layered child (target UpdateLayeredWindow)
        private WglContext? _glContext;
        private bool _bindingsLoaded;

        private int _msaaFbo = -1, _msaaColor = -1;
        private int _resolveFbo = -1, _resolveColor = -1;
        private int _fboWidth, _fboHeight, _samples = 4;

        private CubismModel? _model;
        private CubismOpenGLRenderer? _renderer;
        private CubismPhysics? _physics;
        private CubismMotion? _motion;
        private bool _motionLoop;
        private float _motionTime;
        private long _lastTickMs;

        // Eye tracking (lerp per frame, sama seperti JS lama)
        private float _targetEyeX, _targetEyeY, _currentEyeX, _currentEyeY;

        // Lip-sync
        private float _mouthTarget, _mouthCurrent;
        private long _lastMouthUpdateMs;

        private DispatcherTimer? _renderTimer;
        private readonly object _lock = new();

        private bool _disposed;

        private readonly LayeredSurface _surface = new();

        public OpenGLHost()
        {
            // Auto-detect antialias seperti JS lama (non-aktif di low-end: <= 4 core)
            Antialias = Environment.ProcessorCount > 4;
        }

        // ── HwndHost lifecycle ────────────────────────────────────────────

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            // 1) Child window visible + layered (target UpdateLayeredWindow)
            _childWindow = CreateWindowEx(
                WS_EX_LAYERED,
                "static", "",
                WS_CHILD | WS_VISIBLE | WS_CLIPSIBLINGS,
                0, 0, 1, 1,
                hwndParent.Handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

            // 2) Window tersembunyi khusus GL context (non-layered, bebas quirk layered window)
            _glWindow = CreateWindowEx(
                0,
                "static", "",
                WS_OVERLAPPED,
                0, 0, 64, 64,
                IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

            InitOpenGL();

            return new HandleRef(this, _childWindow);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            StopRenderLoop();
            DisposeOpenGL();
            if (_childWindow != IntPtr.Zero) { DestroyWindow(_childWindow); _childWindow = IntPtr.Zero; }
            if (_glWindow != IntPtr.Zero) { DestroyWindow(_glWindow); _glWindow = IntPtr.Zero; }
        }

        protected override IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_SIZE:
                    int w = lParam.ToInt32() & 0xFFFF;
                    int h = (lParam.ToInt32() >> 16) & 0xFFFF;
                    if (w > 0 && h > 0 && (w != _fboWidth || h != _fboHeight))
                    {
                        UpdateSurfaceSize(w, h);
                        _renderer?.SetViewport(w, h);
                    }
                    handled = true;
                    return IntPtr.Zero;

                case WM_LBUTTONDOWN:
                    _mouseDown = true;
                    _mouseMoved = false;
                    _lastMouseScreen = GetCursorPos();
                    handled = true;
                    return IntPtr.Zero;

                case WM_LBUTTONUP:
                    if (_mouseDown && !_mouseMoved)
                    {
                        int x = lParam.ToInt32() & 0xFFFF;
                        int y = (lParam.ToInt32() >> 16) & 0xFFFF;
                        double dipX = x / DpiScale;
                        double dipY = y / DpiScale;
                        Clicked?.Invoke(new System.Windows.Point(dipX, dipY));
                    }
                    _mouseDown = false;
                    handled = true;
                    return IntPtr.Zero;

                case WM_MOUSEMOVE:
                    if (_mouseDown)
                    {
                        var cur = GetCursorPos();
                        int dx = cur.X - _lastMouseScreen.X;
                        int dy = cur.Y - _lastMouseScreen.Y;
                        if (Math.Abs(dx) > 2 || Math.Abs(dy) > 2)
                        {
                            _mouseMoved = true;
                            DragDelta?.Invoke(dx, dy);
                            _lastMouseScreen = cur;
                        }
                    }
                    handled = true;
                    return IntPtr.Zero;

                case WM_MOUSEACTIVATE:
                    // Jangan ambil fokus dari elemen WPF (mis. TextBox chat)
                    handled = true;
                    return new IntPtr(MA_NOACTIVATE);
            }

            return base.WndProc(hwnd, msg, wParam, lParam, ref handled);
        }

        protected override void OnRenderSizeChanged(System.Windows.SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            int w = Math.Max(1, (int)Math.Ceiling(ActualWidth * DpiScale));
            int h = Math.Max(1, (int)Math.Ceiling(ActualHeight * DpiScale));
            UpdateSurfaceSize(w, h);
            _renderer?.SetViewport(w, h);
        }

        private void UpdateSurfaceSize(int w, int h)
        {
            if (w <= 0 || h <= 0) return;
            if (_fboWidth == w && _fboHeight == h) return;
            _fboWidth = w;
            _fboHeight = h;
            _surface.EnsureSize(w, h);
            EnsureFramebuffers();
        }

        private double DpiScale
        {
            get
            {
                var source = PresentationSource.FromVisual(this);
                return source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
            }
        }

        // ── OpenGL init ───────────────────────────────────────────────────

        private void InitOpenGL()
        {
            try
            {
                _glContext = new WglContext(_glWindow);
                if (!_glContext.Create(32, 8, 0, 0))
                {
                    Console.WriteLine("[OpenGLHost] WglContext.Create gagal");
                    return;
                }
                if (!_glContext.MakeCurrent())
                {
                    Console.WriteLine("[OpenGLHost] MakeCurrent gagal");
                    return;
                }

                GL.LoadBindings(new OpenTkBindingsContext(_glContext));
                _bindingsLoaded = true;

                string version = GL.GetString(StringName.Version) ?? "?";
                Console.WriteLine($"[OpenGLHost] OpenGL context OK: {version}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OpenGLHost] InitOpenGL error: {ex.Message}");
            }
        }

        private void EnsureFramebuffers()
        {
            int w = _fboWidth, h = _fboHeight;
            if (w <= 0 || h <= 0) return;
            if (!_bindingsLoaded) return;

            // MSAA 4x jika aktif; 1 sample (= non-MSAA) jika tidak — nilai 0 invalid untuk TexImage2DMultisample
            _samples = Antialias ? 4 : 1;

            // MSAA color
            if (_msaaFbo != -1) { GL.DeleteFramebuffer(_msaaFbo); GL.DeleteTexture(_msaaColor); }
            _msaaFbo = GL.GenFramebuffer();
            _msaaColor = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2DMultisample, _msaaColor);
            GL.TexImage2DMultisample(TextureTargetMultisample.Texture2DMultisample,
                _samples, PixelInternalFormat.Rgba8, w, h, true);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _msaaFbo);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2DMultisample, _msaaColor, 0);

            // Resolve/readback color
            if (_resolveFbo != -1) { GL.DeleteFramebuffer(_resolveFbo); GL.DeleteTexture(_resolveColor); }
            _resolveFbo = GL.GenFramebuffer();
            _resolveColor = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _resolveColor);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, w, h, 0,
                PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Nearest);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _resolveFbo);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D, _resolveColor, 0);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.BindTexture(TextureTarget.Texture2DMultisample, 0);
        }

        // ── Public model API ──────────────────────────────────────────────

        public void LoadModel(string modelJsonPath)
        {
            lock (_lock)
            {
                try
                {
                    UnloadModel();

                    _model = CubismModel.Load(modelJsonPath);
                    _model.UpdateModel();

                    _physics = _model.PhysicsPath != null ? CubismPhysics.Load(_model.PhysicsPath) : null;
                    _physics?.AttachModel(_model);

                    if (!_bindingsLoaded) { Console.WriteLine("[OpenGLHost] GL bindings belum siap"); return; }
                    EnsureFramebuffers();

                    _renderer = new CubismOpenGLRenderer(_model) { Antialias = Antialias };
                    if (!_renderer.Initialize())
                    {
                        Console.WriteLine("[OpenGLHost] Renderer.Initialize gagal");
                        return;
                    }
                    _renderer.SetViewport(_fboWidth, _fboHeight);

                    // Idle: motion pertama grup "" (loop) + ekspresi acak (sama dengan JS lama)
                    PlayIdleMotion();
                    ApplyRandomExpression();

                    IsModelLoaded = true;
                    FirstFrameRendered = false;
                    _lastTickMs = Environment.TickCount64;

                    StartRenderLoop();
                    ModelLoaded?.Invoke();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[OpenGLHost] LoadModel error: {ex.Message}");
                    ShowModelError(ex.Message);
                }
            }
        }

        public void UnloadModel()
        {
            lock (_lock)
            {
                StopRenderLoop();
                _motion = null;
                _physics = null;
                if (_renderer != null) { _renderer.Dispose(); _renderer = null; }
                if (_model != null) { _model.Dispose(); _model = null; }
                IsModelLoaded = false;
                FirstFrameRendered = false;
            }
        }

        private void ShowModelError(string message)
        {
            Console.WriteLine($"[OpenGLHost] Model error: {message}");
        }

        /// <summary>Ganti target mata (dari -1..1). Di-lerp per frame di dalam host.</summary>
        public void SetEyeTarget(float x, float y)
        {
            _targetEyeX = Math.Clamp(x, -1f, 1f);
            _targetEyeY = Math.Clamp(y, -1f, 1f);
        }

        /// <summary>Putar motion acak (dipakai saat karakter diklik).</summary>
        public void PlayTapMotion()
        {
            if (_model == null) return;
            var motions = GetMotionList();
            if (motions.Count == 0) return;

            int idx = _random.Next(motions.Count);
            // Hindari motion yang sama dengan idle jika ada alternatif
            if (motions.Count > 1 && _motionIsIdle && idx == _currentMotionIndex)
                idx = (idx + 1) % motions.Count;

            _currentMotionIndex = idx;
            var m = CubismMotion.Load(motions[idx]);
            if (m == null) return;
            _motion = m;
            _motionTime = 0f;
            _motionLoop = false;
            _motionIsIdle = false;
        }

        /// <summary>Mulai idle motion (loop).</summary>
        public void PlayIdleMotion()
        {
            if (_model == null) return;
            var motions = GetMotionList();
            if (motions.Count == 0) return;

            int idx = 0;
            var m = CubismMotion.Load(motions[idx]);
            if (m == null) return;
            _currentMotionIndex = idx;
            _motion = m;
            _motionTime = 0f;
            _motionLoop = true;
            _motionIsIdle = true;
        }

        private System.Collections.Generic.List<string> GetMotionList()
        {
            var list = new System.Collections.Generic.List<string>();
            if (_model == null) return list;
            foreach (var kv in _model.Motions)
                list.AddRange(kv.Value);
            return list;
        }

        private void ApplyRandomExpression()
        {
            if (_model == null || _model.Expressions.Count == 0) return;
            var keys = new System.Collections.Generic.List<string>(_model.Expressions.Keys);
            string key = keys[_random.Next(keys.Count)];
            try
            {
                var exp = CubismExpression.Load(_model.Expressions[key]);
                exp?.Apply(_model, 1f);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OpenGLHost] Expression error: {ex.Message}");
            }
        }

        // ── Render loop ───────────────────────────────────────────────────

        private void StartRenderLoop()
        {
            if (_renderTimer != null) return;
            _renderTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            _renderTimer.Tick += (s, e) => RenderFrame();
            _renderTimer.Start();
        }

        private void StopRenderLoop()
        {
            if (_renderTimer != null)
            {
                _renderTimer.Stop();
                _renderTimer.Tick -= (s, e) => RenderFrame();
                _renderTimer = null;
            }
        }

        private void RenderFrame()
        {
            if (_disposed || IsPaused) return;
            if (_model == null || _renderer == null || !_bindingsLoaded) return;
            if (_fboWidth <= 0 || _fboHeight <= 0) return;

            try
            {
                long now = Environment.TickCount64;
                float dt = (float)(now - _lastTickMs) / 1000f;
                _lastTickMs = now;
                if (dt <= 0f) dt = 1f / 60f;
                if (dt > 0.05f) dt = 0.05f;

                UpdateMotion(dt);
                UpdateEye();
                UpdateLipSync(now);
                _physics?.Evaluate(dt, now / 1000f);

                _model.UpdateModel();

                // Render ke MSAA FBO (alpha dipertahankan; clear transparan penuh)
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, _msaaFbo);
                GL.Viewport(0, 0, _fboWidth, _fboHeight);
                GL.ClearColor(0f, 0f, 0f, 0f);
                GL.Clear(ClearBufferMask.ColorBufferBit);
                _renderer.Render();

                // Resolve MSAA → readback
                GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _msaaFbo);
                GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _resolveFbo);
                GL.BlitFramebuffer(0, 0, _fboWidth, _fboHeight, 0, 0, _fboWidth, _fboHeight,
                    ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, _resolveFbo);

                // Readback + present via layered window (per-pixel alpha)
                _surface.UpdateFromFramebuffer(_childWindow, _fboWidth, _fboHeight);

                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

                if (!FirstFrameRendered)
                {
                    FirstFrameRendered = true;
                    Console.WriteLine("[OpenGLHost] Frame pertama sukses dirender.");
                }
            }
            catch (Exception ex)
            {
                // Jangan spam console tiap frame — log sekali lalu pause
                Console.WriteLine($"[OpenGLHost] RenderFrame error: {ex.Message}");
                IsPaused = true;
            }
        }

        // ── Per-frame updates (meniru perilaku JS lama) ───────────────────

        private void UpdateMotion(float dt)
        {
            if (_motion == null || _model == null) return;

            _motionTime += dt;
            float t = _motionTime;
            if (t >= _motion.Duration)
            {
                if (_motionLoop) { _motionTime = 0f; t = 0f; }
                else { _motion = null; _motionTime = 0f; _motionIsIdle = false; return; }
            }

            foreach (var curve in _motion.Curves)
            {
                float v = _motion.Evaluate(curve, t);
                float w = curve.FadeInTime > 0f ? Math.Min(1f, t / curve.FadeInTime) : 1f;
                v = v * w;

                if (curve.Target == "Parameter")
                {
                    int idx = _model.GetParameterIndex(curve.Id);
                    if (idx >= 0) _model.SetParameterValue(idx, v);
                }
                else if (curve.Target == "PartOpacity")
                {
                    int idx = _model.GetPartIndex(curve.Id);
                    if (idx >= 0) unsafe { _model.GetPartOpacitiesPtr()[idx] = v; }
                }
            }
        }

        private void UpdateEye()
        {
            if (_model == null) return;
            _currentEyeX += (_targetEyeX - _currentEyeX) * 0.15f;
            _currentEyeY += (_targetEyeY - _currentEyeY) * 0.15f;

            SetParamIfExists("ParamEyeBallX", _currentEyeX);
            SetParamIfExists("ParamEyeBallY", _currentEyeY);
            SetParamIfExists("ParamAngleX", _currentEyeX * 15f);
            SetParamIfExists("ParamAngleY", _currentEyeY * 10f);
            SetParamIfExists("ParamBodyAngleX", _currentEyeX * 5f);
            SetParamIfExists("ParamEyeBallX01", _currentEyeX);
            SetParamIfExists("ParamEyeBallY01", _currentEyeY);
            SetParamIfExists("ParamEyeX", _currentEyeX);
            SetParamIfExists("ParamEyeY", _currentEyeY);
        }

        private void UpdateLipSync(long now)
        {
            if (_model == null) return;

            if (IsSpeaking)
            {
                // Update target acak tiap 180ms (sama seperti JS lama)
                if (now - _lastMouthUpdateMs >= 180)
                {
                    _lastMouthUpdateMs = now;
                    _mouthTarget = (float)_random.NextDouble() * 0.8f;
                }
            }
            else
            {
                _mouthTarget = 0f;
            }

            _mouthCurrent += (_mouthTarget - _mouthCurrent) * 0.25f;
            if (_mouthCurrent < 0.001f && _mouthTarget == 0f) _mouthCurrent = 0f;
            SetParamIfExists("ParamMouthOpenY", _mouthCurrent);
        }

        private void SetParamIfExists(string id, float value)
        {
            if (_model == null) return;
            int idx = _model.GetParameterIndex(id);
            if (idx >= 0) _model.SetParameterValue(idx, value);
        }

        // ── Mouse helpers ─────────────────────────────────────────────────

        private bool _mouseDown, _mouseMoved;
        private (int X, int Y) _lastMouseScreen;
        private readonly Random _random = new();
        private int _currentMotionIndex;
        private bool _motionIsIdle;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X, Y; }

        private static (int X, int Y) GetCursorPos()
        {
            POINT p;
            GetCursorPos(out p);
            return (p.X, p.Y);
        }

        // ── Dispose ───────────────────────────────────────────────────────

        public new void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            UnloadModel();
            DisposeOpenGL();
            _surface.Dispose();
        }

        private void DisposeOpenGL()
        {
            try
            {
                StopRenderLoop();
                if (_msaaFbo != -1) { GL.DeleteFramebuffer(_msaaFbo); _msaaFbo = -1; }
                if (_msaaColor != -1) { GL.DeleteTexture(_msaaColor); _msaaColor = -1; }
                if (_resolveFbo != -1) { GL.DeleteFramebuffer(_resolveFbo); _resolveFbo = -1; }
                if (_resolveColor != -1) { GL.DeleteTexture(_resolveColor); _resolveColor = -1; }
                _glContext?.Dispose();
                _glContext = null;
            }
            catch { }
        }

        // ── Win32 constants & P/Invoke ────────────────────────────────────

        private const uint WS_CHILD = 0x40000000;
        private const uint WS_VISIBLE = 0x10000000;
        private const uint WS_CLIPSIBLINGS = 0x04000000;
        private const uint WS_OVERLAPPED = 0x00000000;
        private const uint WS_EX_LAYERED = 0x00080000;
        private const int WM_SIZE = 0x0005;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_MOUSEACTIVATE = 0x0021;
        private const int MA_NOACTIVATE = 3;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateWindowEx(uint dwExStyle, string lpClassName, string lpWindowName,
            uint dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu,
            IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll")]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);
    }

    /// <summary>
    /// Permukaan layered window (WS_EX_LAYERED) berisi bitmap premultiplied ARGB
    /// yang di-update tiap frame dari hasil readback framebuffer OpenGL.
    /// </summary>
    internal sealed class LayeredSurface : IDisposable
    {
        private IntPtr _hdcSrc = IntPtr.Zero;   // memory DC
        private IntPtr _hBitmap = IntPtr.Zero;  // DIB section
        private IntPtr _bits = IntPtr.Zero;     // pointer ke pixel DIB
        private int _width, _height;
        private byte[] _pixelBuffer = Array.Empty<byte>();
        private bool _disposed;

        public void EnsureSize(int w, int h)
        {
            if (w == _width && h == _height && _hBitmap != IntPtr.Zero) return;

            ReleaseDib();

            _width = w;
            _height = h;
            _pixelBuffer = new byte[w * h * 4];

            _hdcSrc = CreateCompatibleDC(IntPtr.Zero);
            if (_hdcSrc == IntPtr.Zero) return;

            var bmi = new BITMAPINFO();
            bmi.bmiHeader.biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>();
            bmi.bmiHeader.biWidth = w;
            bmi.bmiHeader.biHeight = h;   // bottom-up: cocok dengan urutan baris glReadPixels
            bmi.bmiHeader.biPlanes = 1;
            bmi.bmiHeader.biBitCount = 32;
            bmi.bmiHeader.biCompression = 0; // BI_RGB

            _hBitmap = CreateDIBSection(_hdcSrc, ref bmi, DIB_RGB_COLORS, out _bits, IntPtr.Zero, 0);
            if (_hBitmap == IntPtr.Zero) return;

            SelectObject(_hdcSrc, _hBitmap);
        }

        private void ReleaseDib()
        {
            if (_hBitmap != IntPtr.Zero) { DeleteObject(_hBitmap); _hBitmap = IntPtr.Zero; }
            if (_hdcSrc != IntPtr.Zero) { DeleteDC(_hdcSrc); _hdcSrc = IntPtr.Zero; }
            _bits = IntPtr.Zero;
        }

        /// <summary>
        /// Baca framebuffer (harus dalam keadaan bind ke _resolveFbo, GL context current)
        /// lalu tampilkan via UpdateLayeredWindow dengan per-pixel alpha.
        /// </summary>
        public unsafe void UpdateFromFramebuffer(IntPtr hwnd, int w, int h)
        {
            if (hwnd == IntPtr.Zero || _hBitmap == IntPtr.Zero || _bits == IntPtr.Zero) return;
            if (w != _width || h != _height) EnsureSize(w, h);
            if (_hBitmap == IntPtr.Zero) return;

            // glReadPixels: RGBA, urutan baris bottom-up (baris pertama = bawah)
            fixed (byte* dst = _pixelBuffer)
            {
                GL.ReadPixels(0, 0, w, h, OpenTK.Graphics.OpenGL4.PixelFormat.Rgba, PixelType.UnsignedByte, (IntPtr)dst);

                // Konversi RGBA → premultiplied BGRA (UpdateLayeredWindow butuh AC_SRC_ALPHA premultiplied)
                byte* src = dst;
                byte* outBits = (byte*)_bits.ToPointer();
                int count = w * h;
                for (int i = 0; i < count; i++)
                {
                    int si = i * 4;
                    byte r = src[si + 0], g = src[si + 1], b = src[si + 2], a = src[si + 3];
                    if (a == 0)
                    {
                        outBits[si + 0] = 0; outBits[si + 1] = 0; outBits[si + 2] = 0; outBits[si + 3] = 0;
                    }
                    else if (a == 255)
                    {
                        outBits[si + 0] = b; outBits[si + 1] = g; outBits[si + 2] = r; outBits[si + 3] = 255;
                    }
                    else
                    {
                        outBits[si + 0] = (byte)(b * a / 255);
                        outBits[si + 1] = (byte)(g * a / 255);
                        outBits[si + 2] = (byte)(r * a / 255);
                        outBits[si + 3] = a;
                    }
                }
            }

            POINT pptSrc = new POINT();
            POINT pptDst = new POINT();
            SIZE size = new SIZE { cx = w, cy = h };
            GetWindowRect(hwnd, out var rect);
            pptDst.X = rect.left;
            pptDst.Y = rect.top;

            BLENDFUNCTION blend = new BLENDFUNCTION
            {
                BlendOp = 0,           // AC_SRC_OVER
                BlendFlags = 0,
                SourceConstantAlpha = 255,
                AlphaFormat = 1        // AC_SRC_ALPHA
            };

            IntPtr hdcDst = GetDC(hwnd);
            if (hdcDst != IntPtr.Zero)
            {
                UpdateLayeredWindow(hwnd, hdcDst, ref pptDst, ref size, _hdcSrc, ref pptSrc, 0, ref blend, ULW_ALPHA);
                ReleaseDC(hwnd, hdcDst);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            ReleaseDib();
        }

        private const uint DIB_RGB_COLORS = 0;
        private const uint ULW_ALPHA = 0x00000002;

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFOHEADER
        {
            public uint biSize;
            public int biWidth;
            public int biHeight;
            public ushort biPlanes;
            public ushort biBitCount;
            public uint biCompression;
            public uint biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public uint biClrUsed;
            public uint biClrImportant;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFO
        {
            public BITMAPINFOHEADER bmiHeader;
            public uint bmiColors;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X, Y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct SIZE { public int cx, cy; }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int left, top, right, bottom; }

        [StructLayout(LayoutKind.Sequential)]
        private struct BLENDFUNCTION
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateDIBSection(IntPtr hdc, ref BITMAPINFO pbmi, uint usage,
            out IntPtr ppvBits, IntPtr hSection, uint offset);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hObject);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("user32.dll")]
        private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst,
            ref SIZE psize, IntPtr hdcSrc, ref POINT pptSrc, uint crKey, ref BLENDFUNCTION pblend, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);
    }
}
