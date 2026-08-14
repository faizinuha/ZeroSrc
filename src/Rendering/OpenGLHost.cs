using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
    /// Window VA memakai AllowsTransparency="True" (layered window WPF). Dokumentasi WPF
    /// menyatakan HWND child (HwndHost) TIDAK pernah di-render di dalam window layered
    /// ("HwndHost descendant controls cannot be displayed in WPF windows whose
    /// AllowsTransparency property is true") — inilah alasan model dulu tidak muncul
    /// walau render loop berjalan. Karena itu:
    ///   1. GL context dibuat di window tersembunyi — murni untuk rendering.
    ///   2. Scene di-render ke MSAA FBO, di-resolve ke FBO readback.
    ///   3. glReadPixels (400x500x4 ≈ 800KB/frame), dikonversi ke premultiplied BGRA,
    ///      lalu disajikan lewat WriteableBitmap (Surface) di pohon visual WPF — WPF sendiri
    ///      yang meng-composite per-pixel alpha-nya ke layered window dengan benar.
    ///   4. HwndHost child window tetap ada hanya untuk input (drag & klik).
    ///
    /// Alur frame (UI thread, DispatcherTimer ~60fps):
    ///   motion → eye tracking → lip-sync → physics → csmUpdateModel → render → readback → present.
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

        /// <summary>Bitmap WPF berisi hasil render GL (per-pixel alpha), disajikan oleh window.</summary>
        public static readonly DependencyProperty SurfaceProperty =
            DependencyProperty.Register(nameof(Surface), typeof(WriteableBitmap), typeof(OpenGLHost), new PropertyMetadata(null));
        public WriteableBitmap? Surface
        {
            get => (WriteableBitmap?)GetValue(SurfaceProperty);
            set => SetValue(SurfaceProperty, value);
        }

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

        private byte[] _readBuffer = Array.Empty<byte>();  // RGBA hasil glReadPixels (bottom-up)
        private byte[] _pixelBuffer = Array.Empty<byte>(); // Pbgra32 premultiplied (top-down, untuk WriteableBitmap)

        private bool _disposed;

        public OpenGLHost()
        {
            // Auto-detect antialias seperti JS lama (non-aktif di low-end: <= 4 core)
            Antialias = Environment.ProcessorCount > 4;
        }

        // ── HwndHost lifecycle ────────────────────────────────────────────

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            // 1) Child window plain — hanya untuk input (drag & klik). Visual model disajikan
            //    lewat WriteableBitmap (Surface), karena HWND child tidak di-composite di dalam
            //    window WPF AllowsTransparency=True (keterbatasan WPF yang terdokumentasi).
            _childWindow = CreateWindowEx(
                0,
                "static", "",
                WS_CHILD | WS_VISIBLE | WS_CLIPSIBLINGS,
                0, 0, 1, 1,
                hwndParent.Handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            if (_childWindow == IntPtr.Zero)
            {
                int err = Marshal.GetLastWin32Error();
                Console.WriteLine($"[OpenGLHost] CRITICAL: CreateWindowEx child GAGAL (error {err}: {new System.ComponentModel.Win32Exception(err).Message}).");
            }

            // 2) Window tersembunyi khusus GL context (non-layered, bebas quirk layered window)
            _glWindow = CreateWindowEx(
                0,
                "static", "",
                WS_OVERLAPPED,
                0, 0, 64, 64,
                IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            if (_glWindow == IntPtr.Zero)
            {
                int err = Marshal.GetLastWin32Error();
                Console.WriteLine($"[OpenGLHost] CreateWindowEx GL window gagal (error {err}).");
            }

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
            EnsureFramebuffers();
            EnsureSurfaceBitmap(w, h);
        }

        private void EnsureSurfaceBitmap(int w, int h)
        {
            int size = w * h * 4;
            if (_readBuffer.Length != size) _readBuffer = new byte[size];
            if (_pixelBuffer.Length != size) _pixelBuffer = new byte[size];

            var cur = Surface;
            if (cur != null && cur.PixelWidth == w && cur.PixelHeight == h) return;
            Surface = new WriteableBitmap(w, h, 96, 96, PixelFormats.Pbgra32, null);
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
                OpenTK.Graphics.OpenGL4.PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
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

        /// <summary>Hasil persiapan model di background thread (parse + decode tekstur).</summary>
        private sealed class PreparedModel
        {
            public CubismModel Model = null!;
            public CubismPhysics? Physics;
            public List<CubismOpenGLRenderer.DecodedTexture> Textures = new();
        }

        /// <summary>
        /// Load model TANPA membekukan UI thread: bagian berat (parse moc3/model3.json,
        /// decode tekstur 8192x8192) dijalankan di background thread, lalu GL init (cepat)
        /// dikembalikan ke UI thread. Dipanggil dari UI thread.
        /// </summary>
        public async Task LoadModelAsync(string modelJsonPath)
        {
            if (_disposed) return;

            PreparedModel? prep = null;
            try
            {
                prep = await Task.Run(() => PrepareModelLoad(modelJsonPath));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OpenGLHost] Prepare model error: {ex.Message}");
                ShowModelError(ex.Message);
                return;
            }
            if (prep == null) return;

            // Lanjutan di sini kembali ke UI thread (SynchronizationContext WPF).
            if (_disposed)
            {
                prep.Model.Dispose();
                return;
            }

            lock (_lock)
            {
                try
                {
                    UnloadModel();

                    _model = prep.Model;
                    _model.UpdateModel();

                    _physics = prep.Physics;
                    _physics?.AttachModel(_model);

                    if (!_bindingsLoaded) { Console.WriteLine("[OpenGLHost] GL bindings belum siap"); return; }
                    EnsureFramebuffers();

                    _renderer = new CubismOpenGLRenderer(_model) { Antialias = Antialias };
                    if (!_renderer.Initialize(prep.Textures))
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
                    prep.Model.Dispose();
                }
            }
        }

        /// <summary>Bagian berat (CPU/native, TANPA GL) — aman dijalankan di background thread.</summary>
        private static PreparedModel PrepareModelLoad(string modelJsonPath)
        {
            var model = CubismModel.Load(modelJsonPath);
            model.UpdateModel();

            var physics = model.PhysicsPath != null ? CubismPhysics.Load(model.PhysicsPath) : null;

            var textures = new List<CubismOpenGLRenderer.DecodedTexture>();
            foreach (string path in model.TexturePaths)
            {
                var t = CubismOpenGLRenderer.DecodeTextureFromFile(path);
                if (t != null) textures.Add(t);
                else Console.WriteLine($"[OpenGLHost] Texture gagal didecode: {path}");
            }

            return new PreparedModel { Model = model, Physics = physics, Textures = textures };
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

                // Readback + present via WriteableBitmap WPF (per-pixel alpha)
                PresentSurface();

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

        // ── Presentasi ke WPF ─────────────────────────────────────────────

        private void PresentSurface()
        {
            var bmp = Surface;
            int w = _fboWidth, h = _fboHeight;
            if (bmp == null || w <= 0 || h <= 0 || _readBuffer.Length < w * h * 4) return;

            unsafe
            {
                fixed (byte* src = _readBuffer)
                {
                    GL.ReadPixels(0, 0, w, h, OpenTK.Graphics.OpenGL4.PixelFormat.Rgba, PixelType.UnsignedByte, (IntPtr)src);
                }
            }

            // glReadPixels bottom-up; WriteableBitmap top-down → flip baris, lalu premultiply RGBA→Pbgra.
            int stride = w * 4;
            for (int row = 0; row < h; row++)
            {
                int srcRow = h - 1 - row;
                Array.Copy(_readBuffer, srcRow * stride, _pixelBuffer, row * stride, stride);
            }
            for (int i = 0; i < w * h; i++)
            {
                int si = i * 4;
                byte r = _pixelBuffer[si + 0], g = _pixelBuffer[si + 1], b = _pixelBuffer[si + 2], a = _pixelBuffer[si + 3];
                _pixelBuffer[si + 0] = (byte)(b * a / 255);
                _pixelBuffer[si + 1] = (byte)(g * a / 255);
                _pixelBuffer[si + 2] = (byte)(r * a / 255);
                _pixelBuffer[si + 3] = a;
            }

            bmp.WritePixels(new Int32Rect(0, 0, w, h), _pixelBuffer, stride, 0);
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
            Surface = null;
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
        private const int WM_SIZE = 0x0005;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_MOUSEACTIVATE = 0x0021;
        private const int MA_NOACTIVATE = 3;

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateWindowEx(uint dwExStyle, string lpClassName, string lpWindowName,
            uint dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu,
            IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll")]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);
    }
}