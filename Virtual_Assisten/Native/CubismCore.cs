using System;
using System.Runtime.InteropServices;

namespace ZeroMix.Virtual_Assisten.Native
{
    // Wrapper for Live2DCubismCore.dll
    public static class CubismCore
    {
        private const string DllName = "Virtual_Assisten\\Native\\Live2DCubismCore.dll";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint csmGetVersion();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr csmReviveMocInPlace(IntPtr address, uint size);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr csmMakeModelInPlace(IntPtr mocAddress, IntPtr modelAddress, uint modelSize);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint csmGetSizeofModel(IntPtr mocAddress);

        // -- DRAWING & UPDATING --
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void csmUpdateModel(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int csmGetDrawableCount(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr csmGetDrawableIds(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr csmGetDrawableVertexCounts(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr csmGetDrawableVertexPositions(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr csmGetDrawableVertexUvs(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr csmGetDrawableIndexCounts(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr csmGetDrawableIndices(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr csmGetDrawableOpacities(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr csmGetDrawableTextureIndices(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr csmGetDrawableConstantFlags(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr csmGetDrawableDynamicFlags(IntPtr model);
    }
}
