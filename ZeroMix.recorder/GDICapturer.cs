using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Vortice;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace ZeroMix.Recorder
{
    /// <summary>
    /// Ultimate fallback capturer using GDI+ (BitBlt).
    /// Works on any system but uses CPU.
    /// </summary>
    public class GDICapturer : IDisposable
    {
        private ID3D11Device _device;
        private ID3D11DeviceContext _context;
        private ID3D11Texture2D? _texture;
        
        public int Width { get; private set; }
        public int Height { get; private set; }
        public bool IsInitialized { get; private set; }

        public GDICapturer(ID3D11Device device, ID3D11DeviceContext context)
        {
            _device = device;
            _context = context;
            
            var screen = Screen.PrimaryScreen;
            Width = screen?.Bounds.Width ?? 1920;
            Height = screen?.Bounds.Height ?? 1080;
            
            try
            {
                var texDesc = new Texture2DDescription
                {
                    Width = (uint)Width,
                    Height = (uint)Height,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = Format.B8G8R8A8_UNorm,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
                    CPUAccessFlags = CpuAccessFlags.None,
                    MiscFlags = ResourceOptionFlags.None
                };
                _texture = _device.CreateTexture2D(texDesc);
                IsInitialized = true;
            }
            catch { IsInitialized = false; }
        }

        public ID3D11Texture2D? CaptureFrame()
        {
            if (!IsInitialized || _texture == null) return null;

            try
            {
                using (Bitmap bmp = new Bitmap(Width, Height, PixelFormat.Format32bppArgb))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.CopyFromScreen(0, 0, 0, 0, bmp.Size);
                    }

                    var data = bmp.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                    try
                    {
                        _context.UpdateSubresource(new DataBox(data.Scan0, data.Stride, 0), _texture, 0);
                    }
                    finally
                    {
                        bmp.UnlockBits(data);
                    }
                }
                return _texture;
            }
            catch { return _texture; }
        }

        public void Dispose()
        {
            _texture?.Dispose();
        }
    }
}
