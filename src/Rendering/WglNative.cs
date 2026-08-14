using System;
using System.Runtime.InteropServices;

namespace ZeroMix.Rendering
{
    /// <summary>
    /// Helper WGL (Windows GL) untuk membuat OpenGL context di atas child HWND.
    /// OpenTK 4.x tidak menyediakan context creation WGL murni (hanya GLFW),
    /// jadi di sini dibuat manual + load bindings via OpenTK GL.LoadBindings.
    /// </summary>
    public sealed class WglContext : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern int ChoosePixelFormat(IntPtr hdc, ref PixelFormatDescriptor ppfd);

        [DllImport("gdi32.dll")]
        private static extern bool SetPixelFormat(IntPtr hdc, int iPixelFormat, ref PixelFormatDescriptor ppfd);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SwapBuffers(IntPtr hdc);

        [DllImport("opengl32.dll")]
        private static extern IntPtr wglCreateContext(IntPtr hdc);

        [DllImport("opengl32.dll")]
        private static extern bool wglMakeCurrent(IntPtr hdc, IntPtr hglrc);

        [DllImport("opengl32.dll")]
        private static extern bool wglDeleteContext(IntPtr hglrc);

        [DllImport("opengl32.dll")]
        private static extern IntPtr wglGetProcAddress([MarshalAs(UnmanagedType.LPStr)] string lpszProc);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [StructLayout(LayoutKind.Sequential)]
        private struct PixelFormatDescriptor
        {
            public ushort nSize;
            public ushort nVersion;
            public uint dwFlags;
            public byte iPixelType;
            public byte cColorBits;
            public byte cRedBits;
            public byte cRedShift;
            public byte cGreenBits;
            public byte cGreenShift;
            public byte cBlueBits;
            public byte cBlueShift;
            public byte cAlphaBits;
            public byte cAlphaShift;
            public byte cAccumBits;
            public byte cAccumRedBits;
            public byte cAccumGreenBits;
            public byte cAccumBlueBits;
            public byte cAccumAlphaBits;
            public byte cDepthBits;
            public byte cStencilBits;
            public byte cAuxBuffers;
            public byte iLayerType;
            public byte bReserved;
            public uint dwLayerMask;
            public uint dwVisibleMask;
            public uint dwDamageMask;
        }

        private const byte PFD_DRAW_TO_WINDOW = 0x04;
        private const byte PFD_SUPPORT_OPENGL = 0x20;
        private const byte PFD_DOUBLEBUFFER = 0x01;
        private const byte PFD_TYPE_RGBA = 0;

        private IntPtr _hWnd;
        private IntPtr _hdc;
        private IntPtr _hglrc;
        private bool _disposed;

        public IntPtr Handle => _hglrc;
        public bool IsCurrent { get; private set; }

        public WglContext(IntPtr hWnd)
        {
            _hWnd = hWnd;
        }

        /// <summary>
        /// Buat context di atas window handle. Memilih pixel format RGBA32 + double buffer.
        /// </summary>
        public bool Create(int colorBits = 32, int alphaBits = 8, int depthBits = 24, int stencilBits = 8)
        {
            _hdc = GetDC(_hWnd);
            if (_hdc == IntPtr.Zero) return false;

            var pfd = new PixelFormatDescriptor
            {
                nSize = (ushort)Marshal.SizeOf<PixelFormatDescriptor>(),
                nVersion = 1,
                dwFlags = (uint)(PFD_DRAW_TO_WINDOW | PFD_SUPPORT_OPENGL | PFD_DOUBLEBUFFER),
                iPixelType = PFD_TYPE_RGBA,
                cColorBits = (byte)colorBits,
                cAlphaBits = (byte)alphaBits,
                cDepthBits = (byte)depthBits,
                cStencilBits = (byte)stencilBits,
                iLayerType = 0
            };

            int pf = ChoosePixelFormat(_hdc, ref pfd);
            if (pf == 0) { ReleaseDC(_hWnd, _hdc); _hdc = IntPtr.Zero; return false; }
            if (!SetPixelFormat(_hdc, pf, ref pfd)) { ReleaseDC(_hWnd, _hdc); _hdc = IntPtr.Zero; return false; }

            _hglrc = wglCreateContext(_hdc);
            if (_hglrc == IntPtr.Zero) { ReleaseDC(_hWnd, _hdc); _hdc = IntPtr.Zero; return false; }

            return true;
        }

        public bool MakeCurrent()
        {
            bool ok = wglMakeCurrent(_hdc, _hglrc);
            IsCurrent = ok;
            return ok;
        }

        public void SwapBuffers()
        {
            if (_hdc != IntPtr.Zero) SwapBuffers(_hdc);
        }

        /// <summary>
        /// Implementasi IBindingsContext untuk OpenTK — resolve fungsi GL dari context saat ini.
        /// </summary>
        public IntPtr GetProcAddress(string procName)
        {
            IntPtr p = wglGetProcAddress(procName);
            if (p == IntPtr.Zero)
            {
                // Beberapa fungsi dasar (mis. glGetString) harus di-resolve dari opengl32.dll
                IntPtr opengl32 = LoadLibrary("opengl32.dll");
                if (opengl32 != IntPtr.Zero)
                    p = GetProcAddress(opengl32, procName);
            }
            return p;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_hglrc != IntPtr.Zero)
            {
                wglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
                wglDeleteContext(_hglrc);
                _hglrc = IntPtr.Zero;
            }
            if (_hdc != IntPtr.Zero)
            {
                ReleaseDC(_hWnd, _hdc);
                _hdc = IntPtr.Zero;
            }
            IsCurrent = false;
        }
    }

    /// <summary>Adaptor IBindingsContext untuk OpenTK LoadBindings.</summary>
    public sealed class OpenTkBindingsContext : OpenTK.IBindingsContext
    {
        private readonly WglContext _ctx;
        public OpenTkBindingsContext(WglContext ctx) => _ctx = ctx;

        public IntPtr GetProcAddress(string procName) => _ctx.GetProcAddress(procName);
    }
}
