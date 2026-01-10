using System;
using System.Diagnostics;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using System.Windows;

namespace ZeroMix.Recorder
{
    /// <summary>
    /// GPU-based screen capture using DXGI Desktop Duplication.
    /// Outputs ID3D11Texture2D directly - NO Bitmap, NO GDI.
    /// </summary>
    public class DXGICapturer : IDisposable
    {
        private ID3D11Device _device;
        private ID3D11DeviceContext _context;
        private IDXGIOutputDuplication? _deskDupl;
        
        private ID3D11Texture2D? _lastFrame;
        
        public int Width { get; private set; }
        public int Height { get; private set; }
        public ID3D11Device Device => _device;
        public ID3D11DeviceContext Context => _context;
        public bool IsInitialized { get; private set; }

        public DXGICapturer()
        {
            D3D11.D3D11CreateDevice(
                null, 
                DriverType.Hardware, 
                DeviceCreationFlags.BgraSupport | DeviceCreationFlags.VideoSupport, 
                null, 
                out _device!, 
                out _context!
            ).CheckError();

            Initialize();
        }

        private void Initialize()
        {
            try
            {
                using var dxgiDevice = _device.QueryInterface<IDXGIDevice>();
                using var adapter = dxgiDevice.GetParent<IDXGIAdapter>();
                
                adapter.EnumOutputs(0, out var output).CheckError();
                using var output1 = output.QueryInterface<IDXGIOutput1>();
                
                var desc = output.Description;
                Width = desc.DesktopCoordinates.Right - desc.DesktopCoordinates.Left;
                Height = desc.DesktopCoordinates.Bottom - desc.DesktopCoordinates.Top;
                
                _deskDupl = output1.DuplicateOutput(_device);
                output.Dispose();

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
                _lastFrame = _device.CreateTexture2D(texDesc);

                IsInitialized = true;
            }
            catch { IsInitialized = false; }
        }

        public ID3D11Texture2D? CaptureFrame()
        {
            if (!IsInitialized || _deskDupl == null) return null;

            try
            {
                var result = _deskDupl.AcquireNextFrame(0, out var frameInfo, out var resource);
                
                if (result.Success && resource != null)
                {
                    using var desktopTexture = resource.QueryInterface<ID3D11Texture2D>();
                    _context.CopyResource(_lastFrame!, desktopTexture);
                    
                    _deskDupl.ReleaseFrame();
                    resource.Dispose();
                }
                else if (result.Code != (int)Vortice.DXGI.ResultCode.WaitTimeout)
                {
                    // Jangan return null langsung, coba pakai frame terakhir dulu
                    return _lastFrame;
                }

                return _lastFrame;
            }
            catch { return _lastFrame; }
        }

        public void Dispose()
        {
            _lastFrame?.Dispose();
            _deskDupl?.Dispose();
            _context?.Dispose();
            _device?.Dispose();
            IsInitialized = false;
        }
    }
}
