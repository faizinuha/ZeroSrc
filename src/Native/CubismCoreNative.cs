using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ZeroMix.Native
{
    /// <summary>
    /// P/Invoke bindings untuk Live2D Cubism Core native (Live2DCubismCore.dll).
    /// Semua signature mengikuti persis src/Native/Live2DCubismCore.h.
    ///
    /// Catatan calling convention: header menentukan `__stdcall` ketika
    /// CSM_CORE_WIN32_DLL didefinisikan (build Windows resmi), jadi di sini
    /// dipakai CallingConvention.StdCall.
    /// </summary>
    public static class CubismCoreNative
    {
        public const string DllName = "Live2DCubismCore.dll";

        // Alignment constraints dari header (enum csmAlignofMoc / csmAlignofModel)
        public const int csmAlignofMoc = 64;
        public const int csmAlignofModel = 16;

        // Bit masks drawable constant flags (dari header)
        public const int csmBlendAdditive = 1 << 0;
        public const int csmBlendMultiplicative = 1 << 1;
        public const int csmIsDoubleSided = 1 << 2;
        public const int csmIsInvertedMask = 1 << 3;

        // Bit masks drawable dynamic flags (dari header)
        public const int csmIsVisible = 1 << 0;
        public const int csmVisibilityDidChange = 1 << 1;
        public const int csmOpacityDidChange = 1 << 2;
        public const int csmDrawOrderDidChange = 1 << 3;
        public const int csmRenderOrderDidChange = 1 << 4;
        public const int csmVertexPositionsDidChange = 1 << 5;
        public const int csmBlendColorDidChange = 1 << 6;

        [StructLayout(LayoutKind.Sequential)]
        public struct csmVector2
        {
            public float X;
            public float Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct csmVector4
        {
            public float X;
            public float Y;
            public float Z;
            public float W;
        }

        /// <summary>
        /// Log handler callback. Header: void (*csmLogFunction)(const char* message);
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void csmLogFunction([MarshalAs(UnmanagedType.LPStr)] string message);

        // ── VERSION ───────────────────────────────────────────────────────────

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern uint csmGetVersion();

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern uint csmGetLatestMocVersion();

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern uint csmGetMocVersion(IntPtr address, uint size);

        // ── CONSISTENCY ───────────────────────────────────────────────────────

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern int csmHasMocConsistency(IntPtr address, uint size);

        // ── LOGGING ───────────────────────────────────────────────────────────

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr csmGetLogFunction();

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern void csmSetLogFunction(csmLogFunction handler);

        // ── MOC ───────────────────────────────────────────────────────────────

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr csmReviveMocInPlace(IntPtr address, uint size);

        // ── MODEL ─────────────────────────────────────────────────────────────

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern uint csmGetSizeofModel(IntPtr moc);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr csmInitializeModelInPlace(IntPtr moc, IntPtr address, uint size);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern void csmUpdateModel(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr csmGetRenderOrders(IntPtr model);

        // ── CANVAS ────────────────────────────────────────────────────────────

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern void csmReadCanvasInfo(
            IntPtr model,
            out csmVector2 outSizeInPixels,
            out csmVector2 outOriginInPixels,
            out float outPixelsPerUnit);

        // ── PARAMETERS ────────────────────────────────────────────────────────

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern int csmGetParameterCount(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr csmGetParameterIds(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr csmGetParameterTypes(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr csmGetParameterMinimumValues(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr csmGetParameterMaximumValues(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr csmGetParameterDefaultValues(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr csmGetParameterValues(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr csmGetParameterRepeats(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr csmGetParameterKeyCounts(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr csmGetParameterKeyValues(IntPtr model);

        // ── PARTS ─────────────────────────────────────────────────────────────

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern int csmGetPartCount(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr csmGetPartIds(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr csmGetPartOpacities(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr csmGetPartParentPartIndices(IntPtr model);

        // ── DRAWABLES ─────────────────────────────────────────────────────────

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern int csmGetDrawableCount(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr csmGetDrawableIds(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr csmGetDrawableConstantFlags(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr csmGetDrawableDynamicFlags(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr csmGetDrawableBlendModes(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr csmGetDrawableTextureIndices(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr csmGetDrawableDrawOrders(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr csmGetDrawableOpacities(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr csmGetDrawableMaskCounts(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr csmGetDrawableMasks(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr csmGetDrawableVertexCounts(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr csmGetDrawableVertexPositions(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr csmGetDrawableVertexUvs(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr csmGetDrawableIndexCounts(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr csmGetDrawableIndices(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr csmGetDrawableMultiplyColors(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr csmGetDrawableScreenColors(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr csmGetDrawableParentPartIndices(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern void csmResetDrawableDynamicFlags(IntPtr model);

        // ── OFFSCREENS ────────────────────────────────────────────────────────

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern int csmGetOffscreenCount(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr csmGetOffscreenBlendModes(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr csmGetOffscreenOpacities(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr csmGetOffscreenOwnerIndices(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr csmGetOffscreenMultiplyColors(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr csmGetOffscreenScreenColors(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr csmGetOffscreenMaskCounts(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr csmGetOffscreenMasks(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr csmGetOffscreenConstantFlags(IntPtr model);

        // ── HELPERS (managed) ─────────────────────────────────────────────────

        /// <summary>
        /// Mengalokasikan memory yang di-aligned (untuk moc/model).
        /// Memakai NativeMemory.AlignedAlloc (tersedia di .NET 7+; proyek ini net9.0).
        /// </summary>
        public static unsafe IntPtr AlignedAlloc(int size, int alignment)
        {
            return (IntPtr)NativeMemory.AlignedAlloc((nuint)size, (nuint)alignment);
        }

        public static void AlignedFree(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero) return;
            unsafe { NativeMemory.AlignedFree((void*)ptr); }
        }

        /// <summary>
        /// Membaca array pointer (const char**) menjadi string array.
        /// </summary>
        public static unsafe string[] ReadStringArray(IntPtr ptr, int count)
        {
            if (ptr == IntPtr.Zero || count <= 0) return Array.Empty<string>();
            var result = new string[count];
            IntPtr* p = (IntPtr*)ptr;
            for (int i = 0; i < count; i++)
            {
                result[i] = Marshal.PtrToStringAnsi(p[i]) ?? string.Empty;
            }
            return result;
        }

        /// <summary>
        /// Membaca array float dari pointer.
        /// </summary>
        public static unsafe float[] ReadFloatArray(IntPtr ptr, int count)
        {
            if (ptr == IntPtr.Zero || count <= 0) return Array.Empty<float>();
            var result = new float[count];
            fixed (float* dst = result)
            {
                Buffer.MemoryCopy((void*)ptr, dst, count * sizeof(float), count * sizeof(float));
            }
            return result;
        }

        /// <summary>
        /// Membaca array int dari pointer.
        /// </summary>
        public static unsafe int[] ReadIntArray(IntPtr ptr, int count)
        {
            if (ptr == IntPtr.Zero || count <= 0) return Array.Empty<int>();
            var result = new int[count];
            fixed (int* dst = result)
            {
                Buffer.MemoryCopy((void*)ptr, dst, count * sizeof(int), count * sizeof(int));
            }
            return result;
        }

        /// <summary>
        /// Membaca array ushort dari pointer.
        /// </summary>
        public static unsafe ushort[] ReadUshortArray(IntPtr ptr, int count)
        {
            if (ptr == IntPtr.Zero || count <= 0) return Array.Empty<ushort>();
            var result = new ushort[count];
            fixed (ushort* dst = result)
            {
                Buffer.MemoryCopy((void*)ptr, dst, count * sizeof(ushort), count * sizeof(ushort));
            }
            return result;
        }

        /// <summary>
        /// Membaca array pointer (misal const int**, const float**) dari pointer.
        /// </summary>
        public static unsafe IntPtr[] ReadPointerArray(IntPtr ptr, int count)
        {
            if (ptr == IntPtr.Zero || count <= 0) return Array.Empty<IntPtr>();
            var result = new IntPtr[count];
            IntPtr* p = (IntPtr*)ptr;
            for (int i = 0; i < count; i++) result[i] = p[i];
            return result;
        }

        /// <summary>
        /// SelfTest: memanggil csmGetVersion() dan csmGetLatestMocVersion()
        /// untuk memverifikasi DLL native ter-load dengan benar.
        /// Dipanggil dari console harness saat verifikasi.
        /// </summary>
        public static void SelfTest()
        {
            Console.WriteLine("[CubismCoreNative.SelfTest] mulai...");
            uint version = csmGetVersion();
            Console.WriteLine($"[CubismCoreNative.SelfTest] csmGetVersion() = 0x{version:X8}");

            uint latestMoc = csmGetLatestMocVersion();
            Console.WriteLine($"[CubismCoreNative.SelfTest] csmGetLatestMocVersion() = {latestMoc}");

            if (version == 0)
            {
                Console.WriteLine("[CubismCoreNative.SelfTest] GAGAL: version == 0, DLL mungkin tidak ter-load atau calling convention salah.");
            }
            else
            {
                // Format versi: 0xMMmmppbb (Major, Minor, Patch, Build)
                int major = (int)(version >> 24) & 0xFF;
                int minor = (int)(version >> 16) & 0xFF;
                int patch = (int)(version >> 8) & 0xFF;
                int build = (int)(version) & 0xFF;
                Console.WriteLine($"[CubismCoreNative.SelfTest] Versi Cubism Core: {major}.{minor}.{patch}.{build}");
                Console.WriteLine("[CubismCoreNative.SelfTest] OK — DLL native ter-load dengan benar.");
            }
        }
    }
}
