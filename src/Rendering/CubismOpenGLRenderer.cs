using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using ZeroMix.Native;

namespace ZeroMix.Rendering
{
    /// <summary>
    /// Renderer OpenGL untuk Live2D Cubism 4.
    ///
    /// Pola render mengikuti alur Cubism Native Samples:
    ///   csmUpdateModel → baca csmGetDrawableVertexPositions → upload ke VBO →
    ///   draw per drawable sesuai csmGetRenderOrders.
    ///
    /// Mask: dipilih pendekatan RENDER-TO-TEXTURE (FBO), bukan stencil, dengan alasan:
    ///   1. Mask Live2D mendukung beberapa mask per drawable + inverted mask —
    ///      stencil buffer (8-bit) kesulitan menangani akumulasi mask bertumpuk.
    ///   2. FBO mask menghasilkan tepi mask anti-aliased via tekstur, bukan tepi
    ///      kaku stencil.
    ///   3. Ini adalah pendekatan yang sama dengan CubismClippingManager resmi
    ///      Live2D (render mask ke offscreen texture, lalu sample di shader utama).
    /// </summary>
    public sealed class CubismOpenGLRenderer : IDisposable
    {
        private CubismModel _model;
        private bool _disposed;

        // Shader programs
        private int _mainProgram;
        private int _maskProgram;

        // Uniform locations (main)
        private int _uProjection, _uModel, _uTexture, _uMaskTexture, _uHasMask, _uMaskInverted,
            _uMaskSize, _uMultiplyColor, _uScreenColor, _uOpacity;

        // Uniform locations (mask)
        private int _maskUProjection, _maskUModel, _maskUTexture;

        // Mask FBO
        private int _maskFbo = -1;
        private int _maskTexture = -1;
        private int _maskWidth, _maskHeight;

        // FBO yang sedang aktif saat Render() dipanggil (dibind oleh host untuk alpha readback)
        private int _prevFbo;

        /// <summary>FBO tempat renderer menggambar. 0 = default framebuffer window.</summary>
        public int TargetFbo { get; set; }

        // Per-drawable buffers
        private int[] _vaos = Array.Empty<int>();
        private int[] _vboPositions = Array.Empty<int>(); // interleaved pos+uv
        private int[] _eboIndices = Array.Empty<int>();
        private int[] _indexCounts = Array.Empty<int>();
        private int[] _vertexCounts = Array.Empty<int>();
        private int[] _textureIndices = Array.Empty<int>();
        private byte[] _constantFlags = Array.Empty<byte>();
        private int[] _maskCounts = Array.Empty<int>();
        private unsafe int** _masks;

        // Cached per-frame data
        private float _viewportWidth = 1, _viewportHeight = 1;
        private float _modelScale = 1f;
        private float _modelOffsetX, _modelOffsetY;
        private float _fitScale = 1f;

        public CubismModel Model => _model;

        public bool Antialias { get; set; } = true;

        public CubismOpenGLRenderer(CubismModel model)
        {
            _model = model;
        }

        /// <summary>Buat semua resource GL (shader, buffers, tekstur). Harus dipanggil saat context GL current.</summary>
        public bool Initialize()
        {
            try
            {
                _mainProgram = CreateShaderProgram(MainVertexShader, MainFragmentShader);
                if (_mainProgram == 0) return false;
                _maskProgram = CreateShaderProgram(MaskVertexShader, MaskFragmentShader);
                if (_maskProgram == 0) return false;

                _uProjection = GL.GetUniformLocation(_mainProgram, "uProjection");
                _uModel = GL.GetUniformLocation(_mainProgram, "uModel");
                _uTexture = GL.GetUniformLocation(_mainProgram, "uTexture");
                _uMaskTexture = GL.GetUniformLocation(_mainProgram, "uMaskTexture");
                _uHasMask = GL.GetUniformLocation(_mainProgram, "uHasMask");
                _uMaskInverted = GL.GetUniformLocation(_mainProgram, "uMaskInverted");
                _uMaskSize = GL.GetUniformLocation(_mainProgram, "uMaskSize");
                _uMultiplyColor = GL.GetUniformLocation(_mainProgram, "uMultiplyColor");
                _uScreenColor = GL.GetUniformLocation(_mainProgram, "uScreenColor");
                _uOpacity = GL.GetUniformLocation(_mainProgram, "uOpacity");

                _maskUProjection = GL.GetUniformLocation(_maskProgram, "uProjection");
                _maskUModel = GL.GetUniformLocation(_maskProgram, "uModel");
                _maskUTexture = GL.GetUniformLocation(_maskProgram, "uTexture");

                LoadTextures();
                SetupDrawableBuffers();
                CreateMaskFramebuffer(256, 256);

                // Ukur fit scale (window menyesuaikan saat Render pertama kali)
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CubismRenderer] Init error: {ex.Message}");
                return false;
            }
        }

        private void LoadTextures()
        {
            foreach (string path in _model.TexturePaths)
            {
                int tex = LoadTextureFromFile(path);
                if (tex != 0)
                    _model.GlTextures.Add(tex);
                else
                    Console.WriteLine($"[CubismRenderer] Texture gagal dimuat: {path}");
            }
            if (_model.GlTextures.Count == 0)
                Console.WriteLine("[CubismRenderer] WARNING: tidak ada tekstur yang berhasil dimuat!");
        }

        private static int LoadTextureFromFile(string path)
        {
            if (!File.Exists(path)) return 0;
            try
            {
                using var bmp = new Bitmap(path);
                int width = bmp.Width;
                int height = bmp.Height;

                // Konversi ke RGBA (baris pertama = top, seperti UV Cubism)
                var data = new byte[width * height * 4];
                var rect = new Rectangle(0, 0, width, height);
                var bmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                try
                {
                    int stride = bmpData.Stride;
                    unsafe
                    {
                        byte* src = (byte*)bmpData.Scan0.ToPointer();
                        for (int y = 0; y < height; y++)
                        {
                            byte* row = src + y * stride;
                            int dstRow = y * width * 4;
                            for (int x = 0; x < width; x++)
                            {
                                int si = x * 4;
                                // BGRA → RGBA
                                data[dstRow + x * 4 + 0] = row[si + 2];
                                data[dstRow + x * 4 + 1] = row[si + 1];
                                data[dstRow + x * 4 + 2] = row[si + 0];
                                data[dstRow + x * 4 + 3] = row[si + 3];
                            }
                        }
                    }
                }
                finally
                {
                    bmp.UnlockBits(bmpData);
                }

                int tex = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, tex);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, width, height, 0,
                    OpenTK.Graphics.OpenGL4.PixelFormat.Rgba, PixelType.UnsignedByte, data);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)All.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)All.ClampToEdge);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                return tex;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CubismRenderer] Texture load error {path}: {ex.Message}");
                return 0;
            }
        }

        private unsafe void SetupDrawableBuffers()
        {
            int n = _model.DrawableCountTotal;
            _vaos = new int[n];
            _vboPositions = new int[n];
            _eboIndices = new int[n];
            _indexCounts = new int[n];
            _vertexCounts = new int[n];
            _textureIndices = new int[n];
            _constantFlags = _model.ReadDrawableConstantFlags();
            _maskCounts = _model.ReadDrawableMaskCounts();
            _masks = _model.GetDrawableMasksPtr();

            int[] vcounts = _model.ReadDrawableVertexCounts();
            int[] icounts = _model.ReadDrawableIndexCounts();
            _textureIndices = _model.ReadDrawableTextureIndices();

            for (int i = 0; i < n; i++)
            {
                _vertexCounts[i] = vcounts[i];
                _indexCounts[i] = icounts[i];

                _vaos[i] = GL.GenVertexArray();
                _vboPositions[i] = GL.GenBuffer();
                _eboIndices[i] = GL.GenBuffer();

                GL.BindVertexArray(_vaos[i]);

                // Interleaved: pos.xy, uv.xy
                GL.BindBuffer(BufferTarget.ArrayBuffer, _vboPositions[i]);
                GL.BufferData(BufferTarget.ArrayBuffer, vcounts[i] * 4 * sizeof(float), IntPtr.Zero,
                    BufferUsageHint.DynamicDraw);
                GL.EnableVertexAttribArray(0);
                GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
                GL.EnableVertexAttribArray(1);
                GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));

                // Indices (static)
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, _eboIndices[i]);
                if (icounts[i] > 0)
                {
                    var idx = CubismCoreNative.ReadUshortArray(GetDrawableIndices(i), icounts[i]);
                    fixed (ushort* p = idx)
                        GL.BufferData(BufferTarget.ElementArrayBuffer, icounts[i] * sizeof(ushort), (IntPtr)p, BufferUsageHint.StaticDraw);
                }

                GL.BindVertexArray(0);
            }
        }

        private unsafe IntPtr GetDrawableIndices(int drawableIndex)
        {
            var p = (IntPtr*)_model.GetDrawableIndicesPtr();
            return p[drawableIndex];
        }

        private void CreateMaskFramebuffer(int width, int height)
        {
            if (_maskFbo != -1) DeleteMaskFramebuffer();
            _maskWidth = width;
            _maskHeight = height;

            _maskTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _maskTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, width, height, 0,
                OpenTK.Graphics.OpenGL4.PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)All.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)All.ClampToEdge);

            _maskFbo = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _maskFbo);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D, _maskTexture, 0);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        private void DeleteMaskFramebuffer()
        {
            if (_maskTexture != -1) GL.DeleteTexture(_maskTexture);
            if (_maskFbo != -1) GL.DeleteFramebuffer(_maskFbo);
            _maskTexture = -1;
            _maskFbo = -1;
        }

        /// <summary>
        /// Hitung transform model agar muat di viewport (anchor bawah-tengah, seperti JS lama).
        /// </summary>
        private void ComputeModelTransform()
        {
            float cw = _model.CanvasWidth > 0 ? _model.CanvasWidth : 1f;
            float ch = _model.CanvasHeight > 0 ? _model.CanvasHeight : 1f;

            float scaleX = _viewportWidth / cw;
            float scaleY = _viewportHeight / ch;
            _fitScale = Math.Min(scaleX, scaleY) * 0.92f;

            _modelScale = _fitScale;

            // Model space: (0,0) = kiri-atas canvas. Anchor di tengah horizontal,
            // bottom di bawah viewport (dengan padding kecil).
            _modelOffsetX = (_viewportWidth - cw * _modelScale) * 0.5f;
            _modelOffsetY = (_viewportHeight - ch * _modelScale) * 0.93f;
        }

        /// <summary>Set viewport size (dipanggil tiap frame dari OpenGLHost).</summary>
        public void SetViewport(int width, int height)
        {
            if (width <= 0 || height <= 0) return;
            _viewportWidth = width;
            _viewportHeight = height;
            ComputeModelTransform();

            // Resize mask FBO mengikuti viewport (bisa juga tetap kecil; di sini ikuti layar)
            if (_maskFbo == -1 || _maskWidth != width || _maskHeight != height)
                CreateMaskFramebuffer(width, height);
        }

        /// <summary>Render satu frame. Harus dipanggil saat context GL current.</summary>
        public unsafe void Render()
        {
            if (_model.ModelPtr == IntPtr.Zero || _model.DrawableCountTotal == 0) return;
            if (_model.GlTextures.Count == 0) return;

            GL.GetInteger(GetPName.FramebufferBinding, out _prevFbo);

            int vw = (int)_viewportWidth;
            int vh = (int)_viewportHeight;
            GL.Viewport(0, 0, vw, vh);
            GL.ClearColor(0f, 0f, 0f, 0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.StencilBufferBit);
            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            GL.Disable(EnableCap.CullFace);

            // Projection: kanvas space (0..cw, 0..ch, Y ke bawah) → NDC
            float cw = _model.CanvasWidth > 0 ? _model.CanvasWidth : 1f;
            float ch = _model.CanvasHeight > 0 ? _model.CanvasHeight : 1f;
            var projection = CreateOrtho(0f, cw, ch, 0f, -1f, 1f);
            var modelMat = CreateTranslation(_modelOffsetX, _modelOffsetY, 0f) *
                           CreateScale(_modelScale, _modelScale, 1f);

            int[] renderOrders = _model.ReadRenderOrders();
            var positions = (IntPtr*)_model.GetDrawableVertexPositionsPtr();
            var uvs = (IntPtr*)_model.GetDrawableVertexUvsPtr();

            GL.UseProgram(_mainProgram);
            GL.UniformMatrix4(_uProjection, false, ref projection);
            GL.UniformMatrix4(_uModel, false, ref modelMat);
            GL.Uniform1(_uTexture, 0);
            GL.Uniform1(_uMaskTexture, 1);

            // Sort drawable index by render order (ascending = back to front)
            var drawIndices = new int[_model.DrawableCountTotal];
            for (int i = 0; i < drawIndices.Length; i++) drawIndices[i] = i;
            Array.Sort(drawIndices, (a, b) => renderOrders[a].CompareTo(renderOrders[b]));

            for (int oi = 0; oi < drawIndices.Length; oi++)
            {
                int d = drawIndices[oi];
                if (_indexCounts[d] <= 0) continue;

                // Update vertex buffer (posisi berubah tiap frame setelah deformasi)
                int vc = _vertexCounts[d];
                var interleaved = new float[vc * 4];
                var posPtr = positions[d];
                var uvPtr = uvs[d];
                var pos = (CubismCoreNative.csmVector2*)posPtr;
                var uv = (CubismCoreNative.csmVector2*)uvPtr;
                for (int v = 0; v < vc; v++)
                {
                    interleaved[v * 4 + 0] = pos[v].X;
                    interleaved[v * 4 + 1] = pos[v].Y;
                    interleaved[v * 4 + 2] = uv[v].X;
                    interleaved[v * 4 + 3] = uv[v].Y;
                }
                GL.BindVertexArray(_vaos[d]);
                GL.BindBuffer(BufferTarget.ArrayBuffer, _vboPositions[d]);
                fixed (float* p = interleaved)
                    GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, interleaved.Length * sizeof(float), (IntPtr)p);

                // Mask handling (render-to-texture)
                bool hasMask = _maskCounts[d] > 0 && _masks != null;
                if (hasMask)
                {
                    RenderMask(_maskCounts[d], _masks[d], positions, uvs, projection, modelMat);
                }

                // Bind texture
                int texIdx = _textureIndices[d];
                if (texIdx >= 0 && texIdx < _model.GlTextures.Count)
                {
                    GL.ActiveTexture(TextureUnit.Texture0);
                    GL.BindTexture(TextureTarget.Texture2D, _model.GlTextures[texIdx]);
                }
                else
                {
                    GL.BindTexture(TextureTarget.Texture2D, 0);
                }
                if (hasMask)
                {
                    GL.ActiveTexture(TextureUnit.Texture1);
                    GL.BindTexture(TextureTarget.Texture2D, _maskTexture);
                }

                GL.Uniform1(_uHasMask, hasMask ? 1 : 0);
                int inverted = (_constantFlags[d] & CubismCoreNative.csmIsInvertedMask) != 0 ? 1 : 0;
                GL.Uniform1(_uMaskInverted, inverted);
                GL.Uniform2(_uMaskSize, _maskWidth, _maskHeight);

                var mult = _model.ReadDrawableMultiplyColors();
                var screen = _model.ReadDrawableScreenColors();
                GL.Uniform4(_uMultiplyColor, mult[d].X, mult[d].Y, mult[d].Z, mult[d].W);
                GL.Uniform4(_uScreenColor, screen[d].X, screen[d].Y, screen[d].Z, screen[d].W);

                float opacity = GetDrawableOpacity(d);
                GL.Uniform1(_uOpacity, opacity);

                // Blend mode
                byte flags = _constantFlags[d];
                if ((flags & CubismCoreNative.csmBlendAdditive) != 0)
                {
                    GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
                }
                else if ((flags & CubismCoreNative.csmBlendMultiplicative) != 0)
                {
                    GL.BlendFunc(BlendingFactor.DstColor, BlendingFactor.OneMinusSrcAlpha);
                }
                else
                {
                    GL.BlendFuncSeparate(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha,
                        BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);
                }

                GL.DrawElements(PrimitiveType.Triangles, _indexCounts[d], DrawElementsType.UnsignedShort, 0);
                GL.BindVertexArray(0);
            }

            GL.UseProgram(0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.ActiveTexture(TextureUnit.Texture0);
        }

        private unsafe void RenderMask(int maskCount, int* maskIndices, IntPtr* positions, IntPtr* uvs,
            OpenTK.Mathematics.Matrix4 projection, OpenTK.Mathematics.Matrix4 modelMat)
        {
            // Render mask drawables ke FBO mask
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _maskFbo);
            GL.Viewport(0, 0, _maskWidth, _maskHeight);
            GL.ClearColor(0f, 0f, 0f, 0f);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.One, BlendingFactor.One); // additive ke mask buffer

            GL.UseProgram(_maskProgram);
            GL.UniformMatrix4(_maskUProjection, false, ref projection);
            GL.UniformMatrix4(_maskUModel, false, ref modelMat);
            GL.Uniform1(_maskUTexture, 0);

            for (int m = 0; m < maskCount; m++)
            {
                int md = maskIndices[m];
                if (md < 0 || md >= _model.DrawableCountTotal || _indexCounts[md] <= 0) continue;

                int vc = _vertexCounts[md];
                var interleaved = new float[vc * 4];
                var pos = (CubismCoreNative.csmVector2*)positions[md];
                var uv = (CubismCoreNative.csmVector2*)uvs[md];
                for (int v = 0; v < vc; v++)
                {
                    interleaved[v * 4 + 0] = pos[v].X;
                    interleaved[v * 4 + 1] = pos[v].Y;
                    interleaved[v * 4 + 2] = uv[v].X;
                    interleaved[v * 4 + 3] = uv[v].Y;
                }

                GL.BindVertexArray(_vaos[md]);
                GL.BindBuffer(BufferTarget.ArrayBuffer, _vboPositions[md]);
                fixed (float* p = interleaved)
                    GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, interleaved.Length * sizeof(float), (IntPtr)p);

                int texIdx = _textureIndices[md];
                if (texIdx >= 0 && texIdx < _model.GlTextures.Count)
                {
                    GL.ActiveTexture(TextureUnit.Texture0);
                    GL.BindTexture(TextureTarget.Texture2D, _model.GlTextures[texIdx]);
                }
                else
                {
                    GL.BindTexture(TextureTarget.Texture2D, 0);
                }

                GL.DrawElements(PrimitiveType.Triangles, _indexCounts[md], DrawElementsType.UnsignedShort, 0);
            }

            GL.BindVertexArray(0);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _prevFbo);
            GL.Viewport(0, 0, (int)_viewportWidth, (int)_viewportHeight);
            GL.UseProgram(_mainProgram);
        }

        private float GetDrawableOpacity(int drawableIndex)
        {
            // Pakai dynamic flag visibility & opacity
            var opacities = _model.ReadDrawableOpacities();
            if (drawableIndex < 0 || drawableIndex >= opacities.Length) return 1f;
            var dynFlags = _model.ReadDrawableDynamicFlags();
            bool visible = (dynFlags[drawableIndex] & CubismCoreNative.csmIsVisible) != 0;
            return visible ? Math.Clamp(opacities[drawableIndex], 0f, 1f) : 0f;
        }

        // ── Shader sources ────────────────────────────────────────────────

        private const string MainVertexShader = @"
#version 330 core
layout(location = 0) in vec2 aPosition;
layout(location = 1) in vec2 aTexCoord;
uniform mat4 uProjection;
uniform mat4 uModel;
out vec2 vTexCoord;
void main() {
    gl_Position = uProjection * uModel * vec4(aPosition, 0.0, 1.0);
    vTexCoord = aTexCoord;
}";

        private const string MainFragmentShader = @"
#version 330 core
in vec2 vTexCoord;
out vec4 FragColor;
uniform sampler2D uTexture;
uniform sampler2D uMaskTexture;
uniform int uHasMask;
uniform int uMaskInverted;
uniform vec2 uMaskSize;
uniform vec4 uMultiplyColor;
uniform vec4 uScreenColor;
uniform float uOpacity;
void main() {
    vec4 tex = texture(uTexture, vTexCoord);
    vec4 color = tex;
    // Multiply & screen color (Cubism 4.x)
    color.rgb *= uMultiplyColor.rgb;
    color.rgb = color.rgb + uScreenColor.rgb * (1.0 - color.rgb);

    float alpha = color.a;

    // Mask sampling (render-to-texture): mask di-render di FBO dengan orientasi GL sama,
    // jadi koordinat gl_FragCoord langsung dipakai (tanpa flip).
    if (uHasMask == 1) {
        vec2 maskUv = gl_FragCoord.xy / uMaskSize;
        float maskA = texture(uMaskTexture, maskUv).a;
        if (uMaskInverted == 1) {
            alpha *= (1.0 - maskA);
        } else {
            alpha *= maskA;
        }
    }

    color.a = alpha * uOpacity;
    FragColor = color;
}";

        private const string MaskVertexShader = @"
#version 330 core
layout(location = 0) in vec2 aPosition;
layout(location = 1) in vec2 aTexCoord;
uniform mat4 uProjection;
uniform mat4 uModel;
out vec2 vTexCoord;
void main() {
    gl_Position = uProjection * uModel * vec4(aPosition, 0.0, 1.0);
    vTexCoord = aTexCoord;
}";

        private const string MaskFragmentShader = @"
#version 330 core
in vec2 vTexCoord;
out vec4 FragColor;
uniform sampler2D uTexture;
void main() {
    vec4 tex = texture(uTexture, vTexCoord);
    // Output hanya alpha (warna 0) — diakumulasi secara additive di mask buffer
    FragColor = vec4(0.0, 0.0, 0.0, tex.a);
}";

        private static int CreateShaderProgram(string vsSrc, string fsSrc)
        {
            int vs = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vs, vsSrc);
            GL.CompileShader(vs);
            GL.GetShader(vs, ShaderParameter.CompileStatus, out int vsOk);
            if (vsOk == 0)
            {
                Console.WriteLine($"[Shader] VS error: {GL.GetShaderInfoLog(vs)}");
                GL.DeleteShader(vs);
                return 0;
            }

            int fs = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fs, fsSrc);
            GL.CompileShader(fs);
            GL.GetShader(fs, ShaderParameter.CompileStatus, out int fsOk);
            if (fsOk == 0)
            {
                Console.WriteLine($"[Shader] FS error: {GL.GetShaderInfoLog(fs)}");
                GL.DeleteShader(vs);
                GL.DeleteShader(fs);
                return 0;
            }

            int prog = GL.CreateProgram();
            GL.AttachShader(prog, vs);
            GL.AttachShader(prog, fs);
            GL.LinkProgram(prog);
            GL.GetProgram(prog, GetProgramParameterName.LinkStatus, out int linkOk);
            if (linkOk == 0)
            {
                Console.WriteLine($"[Shader] Link error: {GL.GetProgramInfoLog(prog)}");
            }
            GL.DetachShader(prog, vs);
            GL.DetachShader(prog, fs);
            GL.DeleteShader(vs);
            GL.DeleteShader(fs);
            return prog;
        }

        // ── Matrix helpers (tanpa full OpenTK.Mathematics agar sederhana & kompatibel) ──

        private static OpenTK.Mathematics.Matrix4 CreateScale(float sx, float sy, float sz)
            => OpenTK.Mathematics.Matrix4.CreateScale(sx, sy, sz);

        private static OpenTK.Mathematics.Matrix4 CreateTranslation(float x, float y, float z)
            => OpenTK.Mathematics.Matrix4.CreateTranslation(x, y, z);

        private static OpenTK.Mathematics.Matrix4 CreateOrtho(float left, float right, float bottom, float top, float zNear, float zFar)
            => OpenTK.Mathematics.Matrix4.CreateOrthographicOffCenter(left, right, bottom, top, zNear, zFar);

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                DeleteMaskFramebuffer();
                if (_mainProgram != 0) GL.DeleteProgram(_mainProgram);
                if (_maskProgram != 0) GL.DeleteProgram(_maskProgram);

                for (int i = 0; i < _vaos.Length; i++)
                {
                    if (_vaos[i] != 0) GL.DeleteVertexArray(_vaos[i]);
                    if (_vboPositions[i] != 0) GL.DeleteBuffer(_vboPositions[i]);
                    if (_eboIndices[i] != 0) GL.DeleteBuffer(_eboIndices[i]);
                }

                foreach (int t in _model.GlTextures)
                    if (t != 0) GL.DeleteTexture(t);
                _model.GlTextures.Clear();
            }
            catch { }
        }
    }
}
