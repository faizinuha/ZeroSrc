using System;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.Direct3D9;
using Vortice.DXGI;
using System.Windows.Interop;
using System.Windows;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using Vortice.D3DCompiler;
using System.Runtime.InteropServices;
using System.Numerics;

namespace ZeroMix.Virtual_Assisten.Native
{
    public class NativeRenderer : IDisposable
    {
        private ID3D11Device _device;
        private ID3D11DeviceContext _context;
        private ID3D11Texture2D _renderTarget;
        private ID3D11RenderTargetView _rtv;
        private D3DImage _d3dImage;

        // D3D9 Interop for WPF
        private IDirect3D9Ex _d3d9;
        private IDirect3DDevice9Ex _d3d9Device;
        private IDirect3DTexture9 _d3d9Texture;

        // States
        private ID3D11BlendState _blendNormal;
        private ID3D11BlendState _blendAdd;
        private ID3D11BlendState _blendMult;
        private ID3D11RasterizerState _rasterizerState;
        private ID3D11DepthStencilState _depthState;
        private ID3D11SamplerState _samplerState;

        // Shaders
        private ID3D11VertexShader _vertexShader;
        private ID3D11PixelShader _pixelShader;
        private ID3D11InputLayout _inputLayout;
        private ID3D11Buffer _constantBuffer;

        // Buffers
        private ID3D11Buffer _vertexBuffer;
        private ID3D11Buffer _indexBuffer;
        private int _maxVertices = 4096;
        private int _maxIndices = 8192;

        private Dictionary<string, ID3D11ShaderResourceView> _textures = new();

        public D3DImage ImageSource => _d3dImage;

        public NativeRenderer()
        {
            InitializeD3D();
            _d3dImage = new D3DImage();
        }

        private void InitializeD3D()
        {
            // Create DX11 Device
            D3D11.D3D11CreateDevice(null, DriverType.Hardware, DeviceCreationFlags.BgraSupport, 
                new[] { FeatureLevel.Level_11_0, FeatureLevel.Level_10_1 }, out _device, out _context).CheckError();

            // Create DX9 Device for Interop
            D3D9.Direct3DCreate9Ex(out _d3d9).CheckError();
            var presentParams = new Vortice.Direct3D9.PresentParameters
            {
                Windowed = true,
                SwapEffect = Vortice.Direct3D9.SwapEffect.Discard,
                DeviceWindowHandle = IntPtr.Zero,
                PresentationInterval = PresentInterval.Default
            };
            _d3d9Device = _d3d9.CreateDeviceEx(0, DeviceType.Hardware, IntPtr.Zero, CreateFlags.HardwareVertexProcessing | CreateFlags.Multithreaded | CreateFlags.PureDevice, presentParams);
            
            CreateStates();
            LoadShaders();
            UpdateSize(400, 600);
        }

        private void CreateStates()
        {
            var blendDesc = BlendDescription.AlphaBlend;
            _blendNormal = _device.CreateBlendState(blendDesc);

            blendDesc.RenderTarget[0].SourceBlend = Vortice.Direct3D11.Blend.One;
            blendDesc.RenderTarget[0].DestinationBlend = Vortice.Direct3D11.Blend.One;
            _blendAdd = _device.CreateBlendState(blendDesc);

            blendDesc.RenderTarget[0].SourceBlend = Vortice.Direct3D11.Blend.DestinationColor;
            blendDesc.RenderTarget[0].DestinationBlend = Vortice.Direct3D11.Blend.Zero;
            _blendMult = _device.CreateBlendState(blendDesc);

            _rasterizerState = _device.CreateRasterizerState(RasterizerDescription.CullNone);
            _depthState = _device.CreateDepthStencilState(DepthStencilDescription.None);
            _samplerState = _device.CreateSamplerState(SamplerDescription.LinearWrap);
        }

        private void LoadShaders()
        {
            string shaderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Virtual_Assisten/Native/L2DShader.hlsl");
            if (!File.Exists(shaderPath)) return;

            string shaderCode = File.ReadAllText(shaderPath);
            var vsBlob = Compiler.Compile(shaderCode, "VS", "vs_4_0", "L2DShader.hlsl", ShaderFlags.None);
            _vertexShader = _device.CreateVertexShader(vsBlob.Span);

            var inputElements = new[] {
                new InputElementDescription("POSITION", 0, Vortice.DXGI.Format.R32G32_Float, 0, 0),
                new InputElementDescription("TEXCOORD", 0, Vortice.DXGI.Format.R32G32_Float, 8, 0)
            };
            _inputLayout = _device.CreateInputLayout(inputElements, vsBlob.Span);

            var psBlob = Compiler.Compile(shaderCode, "PS", "ps_4_0", "L2DShader.hlsl", ShaderFlags.None);
            _pixelShader = _device.CreatePixelShader(psBlob.Span);

            _constantBuffer = _device.CreateBuffer(new BufferDescription(64, BindFlags.ConstantBuffer));

            // Initial Dynamic Buffers
            _vertexBuffer = _device.CreateBuffer(new BufferDescription((uint)(_maxVertices * 16), BindFlags.VertexBuffer, ResourceUsage.Dynamic, CpuAccessFlags.Write));
            _indexBuffer = _device.CreateBuffer(new BufferDescription((uint)(_maxIndices * 2), BindFlags.IndexBuffer, ResourceUsage.Dynamic, CpuAccessFlags.Write));
        }

        public void UpdateSize(int width, int height)
        {
            if (width <= 0 || height <= 0) return;

            _rtv?.Dispose();
            _renderTarget?.Dispose();
            _d3d9Texture?.Dispose();

            // Create DX11 Shared Texture
            var desc = new Texture2DDescription
            {
                Width = (uint)width,
                Height = (uint)height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Vortice.DXGI.Format.B8G8R8A8_UNorm,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                CPUAccessFlags = CpuAccessFlags.None,
                MiscFlags = ResourceOptionFlags.Shared
            };

            _renderTarget = _device.CreateTexture2D(desc);
            _rtv = _device.CreateRenderTargetView(_renderTarget);

            // Get Shared Handle
            using (var resource = _renderTarget.QueryInterface<IDXGIResource>())
            {
                IntPtr sharedHandle = resource.SharedHandle;
                _d3d9Texture = _d3d9Device.CreateTexture((uint)width, (uint)height, 1, Vortice.Direct3D9.Usage.RenderTarget, Vortice.Direct3D9.Format.A8R8G8B8, Vortice.Direct3D9.Pool.Default, ref sharedHandle);
            }

            UpdateWpfSurface();
        }

        private void UpdateWpfSurface()
        {
            if (_d3d9Texture == null) return;
            using (var surface = _d3d9Texture.GetSurfaceLevel(0))
            {
                _d3dImage.Lock();
                _d3dImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, surface.NativePointer);
                _d3dImage.Unlock();
            }
        }

        public void Render(Live2DModelNative model)
        {
            if (_context == null || _rtv == null) return;
            
            _context.ClearRenderTargetView(_rtv, new Vortice.Mathematics.Color4(0, 0, 0, 0));
            _context.OMSetRenderTargets(_rtv);

            if (model != null && model.Model != IntPtr.Zero)
            {
                _context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
                _context.IASetInputLayout(_inputLayout);
                _context.VSSetShader(_vertexShader);
                _context.PSSetShader(_pixelShader);
                _context.PSSetSampler(0, _samplerState);
                _context.RSSetState(_rasterizerState);
                _context.OMSetDepthStencilState(_depthState);
                _context.OMSetBlendState(_blendNormal);

                model.Update();

                int drawableCount = CubismCore.csmGetDrawableCount(model.Model);
                IntPtr[] vertexPositions = GetPointerArray(CubismCore.csmGetDrawableVertexPositions(model.Model), drawableCount);
                IntPtr[] vertexUvs = GetPointerArray(CubismCore.csmGetDrawableVertexUvs(model.Model), drawableCount);
                IntPtr[] indices = GetPointerArray(CubismCore.csmGetDrawableIndices(model.Model), drawableCount);
                int[] vCounts = GetIntArray(CubismCore.csmGetDrawableVertexCounts(model.Model), drawableCount);
                int[] iCounts = GetIntArray(CubismCore.csmGetDrawableIndexCounts(model.Model), drawableCount);

                for (int i = 0; i < drawableCount; i++)
                {
                    int vCount = vCounts[i];
                    int iCount = iCounts[i];
                    if (vCount == 0 || iCount == 0) continue;

                    EnsureBufferCapacity(vCount, iCount);

                    var box = _context.Map(_vertexBuffer, 0, MapMode.WriteDiscard, Vortice.Direct3D11.MapFlags.None);
                    unsafe {
                        float* dst = (float*)box.DataPointer;
                        float* srcPos = (float*)vertexPositions[i];
                        float* srcUv = (float*)vertexUvs[i];
                        for(int v=0; v < vCount; v++) {
                            dst[v*4+0] = srcPos[v*2+0]; dst[v*4+1] = srcPos[v*2+1];
                            dst[v*4+2] = srcUv[v*2+0];  dst[v*4+3] = srcUv[v*2+1];
                        }
                    }
                    _context.Unmap(_vertexBuffer, 0);

                    var iBox = _context.Map(_indexBuffer, 0, MapMode.WriteDiscard, Vortice.Direct3D11.MapFlags.None);
                    unsafe { Buffer.MemoryCopy((void*)indices[i], (void*)iBox.DataPointer, iCount * 2, iCount * 2); }
                    _context.Unmap(_indexBuffer, 0);

                    _context.IASetVertexBuffer(0, _vertexBuffer, 16);
                    _context.IASetIndexBuffer(_indexBuffer, Vortice.DXGI.Format.R16_UInt, 0);
                    _context.DrawIndexed((uint)iCount, 0, 0);
                }
            }
            
            _context.Flush();
            _d3dImage.Lock();
            _d3dImage.AddDirtyRect(new Int32Rect(0, 0, _d3dImage.PixelWidth, _d3dImage.PixelHeight));
            _d3dImage.Unlock();
        }

        private void EnsureBufferCapacity(int vertexCount, int indexCount)
        {
            if (vertexCount > _maxVertices) {
                _vertexBuffer?.Dispose();
                _maxVertices = vertexCount + 1024;
                _vertexBuffer = _device.CreateBuffer(new BufferDescription((uint)(_maxVertices * 16), BindFlags.VertexBuffer, ResourceUsage.Dynamic, CpuAccessFlags.Write));
            }
            if (indexCount > _maxIndices) {
                _indexBuffer?.Dispose();
                _maxIndices = indexCount + 1024;
                _indexBuffer = _device.CreateBuffer(new BufferDescription((uint)(_maxIndices * 2), BindFlags.IndexBuffer, ResourceUsage.Dynamic, CpuAccessFlags.Write));
            }
        }

        private IntPtr[] GetPointerArray(IntPtr ptr, int count)
        {
            IntPtr[] arr = new IntPtr[count];
            Marshal.Copy(ptr, arr, 0, count);
            return arr;
        }

        private int[] GetIntArray(IntPtr ptr, int count)
        {
            int[] arr = new int[count];
            Marshal.Copy(ptr, arr, 0, count);
            return arr;
        }

        public void Dispose()
        {
            _vertexBuffer?.Dispose();
            _indexBuffer?.Dispose();
            _constantBuffer?.Dispose();
            _vertexShader?.Dispose();
            _pixelShader?.Dispose();
            _inputLayout?.Dispose();
            _rtv?.Dispose();
            _renderTarget?.Dispose();
            _d3d9Texture?.Dispose();
            _d3d9Device?.Dispose();
            _d3d9?.Dispose();
            _context?.Dispose();
            _device?.Dispose();
        }
    }
}
