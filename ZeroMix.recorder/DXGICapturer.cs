using System;
using System.Diagnostics;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using System.Windows;
using System.Runtime.InteropServices;

namespace ZeroMix.Recorder
{
    /// <summary>
    /// GPU-based screen capture using DXGI Desktop Duplication.
    /// Outputs ID3D11Texture2D directly - NO Bitmap, NO GDI.
    /// </summary>
    public class DXGICapturer : IDisposable
    {
        private ID3D11Device? _device;
        private ID3D11DeviceContext? _context;
        private IDXGIOutputDuplication? _deskDupl;
        
        private ID3D11Texture2D? _lastFrame;
        
        public int Width { get; private set; }
        public int Height { get; private set; }
        public ID3D11Device? Device => _device;
        public ID3D11DeviceContext? Context => _context;
        public bool IsInitialized { get; private set; }

        public DXGICapturer()
        {
            if (!TryCreateDevice(DriverType.Hardware))
            {
                Console.WriteLine("[DXGICapturer] Hardware device creation failed, falling back to WARP...");
                if (!TryCreateDevice(DriverType.Warp))
                {
                    Console.WriteLine("[DXGICapturer] FATAL: WARP device creation also failed.");
                    IsInitialized = false;
                    return;
                }
            }

            Initialize();
        }

        private bool TryCreateDevice(DriverType driverType)
        {
            try
            {
                D3D11.D3D11CreateDevice(
                    null,
                    driverType,
                    DeviceCreationFlags.BgraSupport | DeviceCreationFlags.VideoSupport,
                    null,
                    out _device,
                    out _context
                ).CheckError();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DXGICapturer] Failed to create {driverType} device: {ex.Message}");
                return false;
            }
        }

        private void Initialize()
        {
            try
            {
                Console.WriteLine("[DXGICapturer] Initializing DXGI Output Duplication...");
                
                // Use static factory creation instead of device parent
                using var dxgiFactory = DXGI.CreateDXGIFactory1<IDXGIFactory1>();

                if (dxgiFactory == null)
                {
                    Console.WriteLine("[DXGICapturer] ERROR: Failed to create DXGI factory.");
                    IsInitialized = false;
                    return;
                }

                // Iterasi semua adapter (GPU)
                for (uint adapterIndex = 0; dxgiFactory.EnumAdapters(adapterIndex, out var adapter).Success; adapterIndex++)
                {
                    try
                    {
                        var adapterDesc = adapter.Description;
                        Console.WriteLine($"[DXGICapturer] Checking Adapter {adapterIndex}: {adapterDesc.Description}");

                        // Iterasi semua output (Monitor) pada adapter ini
                        for (uint outputIndex = 0; adapter.EnumOutputs(outputIndex, out var output).Success; outputIndex++)
                        {
                            try
                            {
                                using var output1 = output.QueryInterface<IDXGIOutput1>();
                                var desc = output.Description;
                                Width = desc.DesktopCoordinates.Right - desc.DesktopCoordinates.Left;
                                Height = desc.DesktopCoordinates.Bottom - desc.DesktopCoordinates.Top;

                                Console.WriteLine($"[DXGICapturer] Trying Output {outputIndex}: {Width}x{Height}");

                                // Coba duplikasi - titik krusial kegagalan biasanya di sini
                                try
                                {
                                    _deskDupl = output1.DuplicateOutput(_device);
                                    Console.WriteLine($"[DXGICapturer] ✓ Success on Adapter {adapterIndex}, Output {outputIndex}!");
                                    
                                    SetupStagingTexture();
                                    IsInitialized = true;
                                    output.Dispose();
                                    adapter.Dispose();
                                    return; // Berhasil!
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[DXGICapturer] DuplicateOutput failed: {ex.Message}");
                                    // Try next output
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[DXGICapturer] Output enumeration error: {ex.Message}");
                            }
                            finally 
                            { 
                                try { output.Dispose(); } catch { }
                            }
                        }
                    }
                    catch (Exception ex) 
                    { 
                        Console.WriteLine($"[DXGICapturer] Adapter enumeration error: {ex.Message}");
                    }
                    finally 
                    { 
                        try { adapter.Dispose(); } catch { }
                    }
                }

                Console.WriteLine("[DXGICapturer] ERROR: No duplicatable output found on any adapter.");
                Console.WriteLine("[DXGICapturer] This might be an old GPU or driver issue - GDI fallback will be used.");
                IsInitialized = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DXGICapturer] FATAL ERROR during initialization: {ex.Message}");
                IsInitialized = false;
            }
        }

        private void SetupStagingTexture()
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
            _lastFrame = _device.CreateTexture2D(texDesc);
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
