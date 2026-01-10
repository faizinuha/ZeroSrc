using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Direct2D1;
using System.Numerics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;

namespace ZeroMix.Recorder
{
    public class DXGIManager : IDisposable
    {
        private ID3D11Device? _device;
        private ID3D11DeviceContext? _deviceContext;
        private IDXGIOutputDuplication? _deskDupl;
        
        private ID2D1Factory1? _d2dFactory;
        private ID2D1Device? _d2dDevice;
        private ID2D1DeviceContext? _d2dContext;
        
        private ID3D11Texture2D? _renderTargetTexture;
        private ID3D11Texture2D? _stagingTexture;
        private ID2D1Bitmap1? _d2dBitmapOut;
        
        private int _width;
        private int _height;
        private bool _isInitialized = false;

        public bool IsInitialized => _isInitialized;

        public DXGIManager()
        {
            Initialize();
        }

        private void Initialize()
        {
            try
            {
                D3D11.D3D11CreateDevice(null, DriverType.Hardware, DeviceCreationFlags.BgraSupport, null, out _device, out _deviceContext).CheckError();
                if (_device == null || _deviceContext == null) {
                    Console.WriteLine("[ZeroRecord] Failed to create D3D11 device");
                    return;
                }

                using (var dxgiDevice = _device.QueryInterface<IDXGIDevice>())
                using (var adapter = dxgiDevice.GetParent<IDXGIAdapter>())
                {
                    adapter.EnumOutputs(0, out var output).CheckError();
                    using (var output1 = output.QueryInterface<IDXGIOutput1>())
                    {
                        _deskDupl = output1.DuplicateOutput(_device);
                    }
                    output.Dispose();
                }

                _width = (int)SystemParameters.PrimaryScreenWidth;
                _height = (int)SystemParameters.PrimaryScreenHeight;

                _d2dFactory = D2D1.D2D1CreateFactory<ID2D1Factory1>();
                using (var dxgiDevice = _device.QueryInterface<IDXGIDevice>())
                {
                    _d2dDevice = _d2dFactory.CreateDevice(dxgiDevice);
                    _d2dContext = _d2dDevice.CreateDeviceContext(DeviceContextOptions.None);
                }

                var rtDesc = new Texture2DDescription
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
                _renderTargetTexture = _device.CreateTexture2D(rtDesc);

                var stagingDesc = new Texture2DDescription
                {
                    Width = (uint)_width,
                    Height = (uint)_height,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = Format.B8G8R8A8_UNorm,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Staging,
                    BindFlags = BindFlags.None,
                    CPUAccessFlags = CpuAccessFlags.Read,
                    MiscFlags = ResourceOptionFlags.None
                };
                _stagingTexture = _device.CreateTexture2D(stagingDesc);

                using (var dxgiSurfaceOut = _renderTargetTexture.QueryInterface<IDXGISurface>())
                {
                    _d2dBitmapOut = _d2dContext.CreateBitmapFromDxgiSurface(dxgiSurfaceOut);
                }

                _isInitialized = true;
                Console.WriteLine($"[ZeroRecord] DXGI/D2D Initialized: {_width}x{_height}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ZeroRecord] DXGI Init Error: {ex.Message}");
                Dispose();
            }
        }

        public bool CaptureFrame(Bitmap targetBmp, float scale = 1.0f, float offsetX = 0, float offsetY = 0)
        {
            if (!_isInitialized || _deskDupl == null || _deviceContext == null || _stagingTexture == null || _d2dContext == null || _renderTargetTexture == null || _d2dBitmapOut == null) 
                return false;

            try
            {
                var acquireResult = _deskDupl.AcquireNextFrame(0, out var frameInfo, out var desktopResource);
                
                if (acquireResult.Success)
                {
                    using (var desktopTexture = desktopResource.QueryInterface<ID3D11Texture2D>())
                    {
                        using (var dxgiSurfaceIn = desktopTexture.QueryInterface<IDXGISurface>())
                        using (var d2dBitmapIn = _d2dContext.CreateBitmapFromDxgiSurface(dxgiSurfaceIn))
                        {
                            _d2dContext.Target = _d2dBitmapOut;
                            _d2dContext.BeginDraw();
                            _d2dContext.Clear(new Vortice.Mathematics.Color4(0, 0, 0, 1));

                            var center = new Vector2(_width / 2f, _height / 2f);
                            var transform = Matrix3x2.CreateScale(scale, scale, center) * 
                                            Matrix3x2.CreateTranslation(offsetX, offsetY);
                            
                            _d2dContext.Transform = transform;
                            _d2dContext.DrawBitmap(d2dBitmapIn, 1.0f, Vortice.Direct2D1.InterpolationMode.Linear);
                            
                            _d2dContext.EndDraw();
                            _d2dContext.Target = null;
                        }
                        _deviceContext.CopyResource(_stagingTexture, _renderTargetTexture);
                    }
                    _deskDupl.ReleaseFrame();
                }
                else if (acquireResult.Code != (int)Vortice.DXGI.ResultCode.WaitTimeout)
                {
                    Console.WriteLine($"[ZeroRecord] DXGI Error: {acquireResult.Code:X}");
                    _isInitialized = false;
                    return false;
                }

                // Copy from Staging to existing Bitmap
                var mappedResource = _deviceContext.Map(_stagingTexture, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None);
                
                var bounds = new Rectangle(0, 0, _width, _height);
                BitmapData? bmpData = null;
                try
                {
                    bmpData = targetBmp.LockBits(bounds, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

                    int lineSize = _width * 4;
                    for (int y = 0; y < _height; y++)
                    {
                        IntPtr sourcePtr = IntPtr.Add(mappedResource.DataPointer, y * (int)mappedResource.RowPitch);
                        IntPtr destPtr = IntPtr.Add(bmpData.Scan0, y * bmpData.Stride);
                        NativeMemoryCopy(destPtr, sourcePtr, (uint)lineSize);
                    }
                }
                finally
                {
                    if (bmpData != null) targetBmp.UnlockBits(bmpData);
                    _deviceContext.Unmap(_stagingTexture, 0);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ZeroRecord] Capture Exception: {ex.Message}");
                return false;
            }
        }

        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        private static unsafe extern void NativeMemoryCopy(IntPtr dest, IntPtr src, uint count);

        public void Dispose()
        {
            _d2dBitmapOut?.Dispose();
            _d2dContext?.Dispose();
            _d2dDevice?.Dispose();
            _d2dFactory?.Dispose();
            _renderTargetTexture?.Dispose();
            _stagingTexture?.Dispose();
            _deskDupl?.Dispose();
            _deviceContext?.Dispose();
            _device?.Dispose();
            _isInitialized = false;
        }
    }
}
