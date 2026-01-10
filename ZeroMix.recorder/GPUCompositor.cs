using System;
using System.Numerics;
using Vortice.Direct3D11;
using Vortice.Direct2D1;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace ZeroMix.Recorder
{
    public class GPUCompositor : IDisposable
    {
        private ID2D1Factory1 _d2dFactory;
        private ID2D1Device _d2dDevice;
        private ID2D1DeviceContext _d2dContext;
        
        private ID3D11Texture2D _outputTexture;
        private ID2D1Bitmap1 _outputBitmap;
        
        private int _width;
        private int _height;

        public ID3D11Texture2D OutputTexture => _outputTexture;
        public bool IsInitialized { get; private set; }

        public GPUCompositor(ID3D11Device device, int width, int height)
        {
            _width = width;
            _height = height;
            Initialize(device);
        }

        private void Initialize(ID3D11Device device)
        {
            try
            {
                _d2dFactory = D2D1.D2D1CreateFactory<ID2D1Factory1>();
                using var dxgiDevice = device.QueryInterface<IDXGIDevice>();
                _d2dDevice = _d2dFactory.CreateDevice(dxgiDevice);
                _d2dContext = _d2dDevice.CreateDeviceContext(DeviceContextOptions.None);
                
                // Set DPI agar 1:1 (Anti-Hitam)
                _d2dContext.SetDpi(96.0f, 96.0f);

                var texDesc = new Texture2DDescription
                {
                    Width = (uint)_width,
                    Height = (uint)_height,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = Format.B8G8R8A8_UNorm,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                    CPUAccessFlags = CpuAccessFlags.None,
                    MiscFlags = ResourceOptionFlags.None
                };
                _outputTexture = device.CreateTexture2D(texDesc);

                using var dxgiSurface = _outputTexture.QueryInterface<IDXGISurface>();
                _outputBitmap = _d2dContext.CreateBitmapFromDxgiSurface(dxgiSurface);

                IsInitialized = true;
            }
            catch { IsInitialized = false; }
        }

        public void Compose(ID3D11Texture2D inputTexture, float camX, float camY, float zoom, float cursorX, float cursorY, bool isClick)
        {
            if (!IsInitialized) return;

            try
            {
                using var dxgiSurfaceIn = inputTexture.QueryInterface<IDXGISurface>();
                using var inputBitmap = _d2dContext.CreateBitmapFromDxgiSurface(dxgiSurfaceIn);

                _d2dContext.Target = _outputBitmap;
                _d2dContext.BeginDraw();
                _d2dContext.Clear(new Color4(0, 0, 0, 1.0f));

                // Transform: Zoom & Pan
                var center = new Vector2(camX, camY);
                var screenCenter = new Vector2(_width / 2f, _height / 2f);
                var transform = Matrix3x2.CreateTranslation(-center.X, -center.Y) *
                               Matrix3x2.CreateScale(zoom, zoom) *
                               Matrix3x2.CreateTranslation(screenCenter.X, screenCenter.Y);

                _d2dContext.Transform = transform;
                
                // Gunakan UnitMode.Pixels biar mapping 1:1
                _d2dContext.UnitMode = UnitMode.Pixels;
                _d2dContext.DrawBitmap(inputBitmap, 1.0f, InterpolationMode.Linear);
                
                _d2dContext.EndDraw();
                _d2dContext.Flush(out _, out _); 
                _d2dContext.Target = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Compose Error: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _outputBitmap?.Dispose();
            _outputTexture?.Dispose();
            _d2dContext?.Dispose();
            _d2dDevice?.Dispose();
            _d2dFactory?.Dispose();
            IsInitialized = false;
        }
    }
}
