using System;
using System.IO;
using System.Runtime.InteropServices;
using ZeroMix.Virtual_Assisten.Native;

namespace ZeroMix.Virtual_Assisten.Native
{
    public unsafe class Live2DModelNative : IDisposable
    {
        private IntPtr _mocBuffer;
        private IntPtr _moc;
        private IntPtr _modelBuffer;
        private IntPtr _model;

        public IntPtr Model => _model;

        public Live2DModelNative(string mocPath)
        {
            LoadMoc(mocPath);
        }

        private void LoadMoc(string path)
        {
            byte[] fileBytes = File.ReadAllBytes(path);
            uint size = (uint)fileBytes.Length;

            // Aligned Allocation for MOC
            _mocBuffer = (IntPtr)NativeMemory.AlignedAlloc((nuint)size, 64);
            Marshal.Copy(fileBytes, 0, _mocBuffer, (int)size);

            _moc = CubismCore.csmReviveMocInPlace(_mocBuffer, size);
            
            if (_moc == IntPtr.Zero) throw new Exception("Failed to revive MOC");

            // Allocate Model
            uint modelSize = CubismCore.csmGetSizeofModel(_moc);
            _modelBuffer = (IntPtr)NativeMemory.AlignedAlloc((nuint)modelSize, 64);
            _model = CubismCore.csmMakeModelInPlace(_moc, _modelBuffer, modelSize);
        }

        public void Update()
        {
            if (_model != IntPtr.Zero)
            {
                CubismCore.csmUpdateModel(_model);
            }
        }

        public void Dispose()
        {
            if (_modelBuffer != IntPtr.Zero) NativeMemory.AlignedFree((void*)_modelBuffer);
            if (_mocBuffer != IntPtr.Zero) NativeMemory.AlignedFree((void*)_mocBuffer);
        }
    }
}
