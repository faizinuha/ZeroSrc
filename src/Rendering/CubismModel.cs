using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using ZeroMix.Native;
using Newtonsoft.Json.Linq;

namespace ZeroMix.Rendering
{
    /// <summary>
    /// Representasi model Live2D Cubism 4 yang sudah di-load.
    /// Memegang pointer moc & model (aligned memory) + metadata dari .model3.json.
    /// JANGAN dipakai dari thread lain — semua akses harus dari thread render (UI thread WPF).
    /// </summary>
    public sealed class CubismModel : IDisposable
    {
        // Pointer native
        private IntPtr _mocBytes;      // aligned 64 — buffer moc3
        private int _mocBytesSize;
        private IntPtr _moc;           // hasil csmReviveMocInPlace
        private IntPtr _modelBytes;    // aligned 16 — buffer model
        private IntPtr _model;         // hasil csmInitializeModelInPlace

        private bool _disposed;

        // Metadata dari model3.json
        public string ModelDir { get; private set; } = "";
        public string ModelJsonPath { get; private set; } = "";
        public string[] TexturePaths { get; private set; } = Array.Empty<string>();
        public string? PhysicsPath { get; private set; }
        public string? DisplayInfoPath { get; private set; }
        public Dictionary<string, string> Expressions { get; } = new();
        public Dictionary<string, List<string>> Motions { get; } = new(); // key = group, value = file list

        // Canvas info
        public float CanvasWidth { get; private set; }
        public float CanvasHeight { get; private set; }
        public float PixelsPerUnit { get; private set; }

        // Counts
        public int ParameterCount { get; private set; }
        public int PartCount { get; private set; }
        public int DrawableCount { get; private set; }

        // ID lookup (dibangun saat load)
        private Dictionary<string, int> _parameterIndex = new();
        private Dictionary<string, int> _partIndex = new();
        private Dictionary<string, int> _drawableIndex = new();

        public IntPtr ModelPtr => _model;
        public IntPtr MocPtr => _moc;

        public int ParameterCountTotal => ParameterCount;
        public int PartCountTotal => PartCount;
        public int DrawableCountTotal => DrawableCount;

        /// <summary>Texture GL handles (diproduksi oleh renderer, bukan di sini).</summary>
        public List<int> GlTextures { get; } = new();

        private CubismModel() { }

        /// <summary>
        /// Load model dari .model3.json. Path relatif di dalam JSON diselesaikan
        /// relatif terhadap folder model3.json.
        /// </summary>
        public static CubismModel Load(string modelJsonPath)
        {
            if (!File.Exists(modelJsonPath))
                throw new FileNotFoundException($"model3.json tidak ditemukan: {modelJsonPath}");

            string dir = Path.GetDirectoryName(modelJsonPath) ?? "";
            JObject json = JObject.Parse(File.ReadAllText(modelJsonPath));

            var model = new CubismModel
            {
                ModelDir = dir,
                ModelJsonPath = modelJsonPath
            };

            var fileRefs = json["FileReferences"] as JObject ?? new JObject();

            // Moc
            string? mocRel = fileRefs["Moc"]?.ToString();
            if (string.IsNullOrEmpty(mocRel))
                throw new InvalidDataException("model3.json tidak punya FileReferences.Moc");
            string mocFull = Path.Combine(dir, mocRel);
            model.LoadMoc(mocFull);

            // Textures
            var textures = new List<string>();
            if (fileRefs["Textures"] is JArray texArr)
            {
                foreach (var t in texArr)
                {
                    string texRel = t.ToString();
                    textures.Add(Path.Combine(dir, texRel));
                }
            }
            model.TexturePaths = textures.ToArray();

            // Physics / DisplayInfo / Expressions / Motions
            model.PhysicsPath = fileRefs["Physics"] != null
                ? Path.Combine(dir, fileRefs["Physics"]!.ToString())
                : null;
            model.DisplayInfoPath = fileRefs["DisplayInfo"] != null
                ? Path.Combine(dir, fileRefs["DisplayInfo"]!.ToString())
                : null;

            if (fileRefs["Expressions"] is JArray expArr)
            {
                foreach (var e in expArr)
                {
                    string? name = e["Name"]?.ToString();
                    string? file = e["File"]?.ToString();
                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(file))
                        model.Expressions[name] = Path.Combine(dir, file);
                }
            }

            if (fileRefs["Motions"] is JObject motionObj)
            {
                foreach (var kv in motionObj)
                {
                    var list = new List<string>();
                    if (kv.Value is JArray arr)
                    {
                        foreach (var m in arr)
                        {
                            string? file = m["File"]?.ToString();
                            if (!string.IsNullOrEmpty(file))
                                list.Add(Path.Combine(dir, file));
                        }
                    }
                    model.Motions[kv.Key] = list;
                }
            }

            // Index parameter/part/drawable
            model.BuildIndices();

            return model;
        }

        private void LoadMoc(string mocPath)
        {
            byte[] data = File.ReadAllBytes(mocPath);
            _mocBytesSize = data.Length;

            _mocBytes = CubismCoreNative.AlignedAlloc(data.Length, CubismCoreNative.csmAlignofMoc);
            Marshal.Copy(data, 0, _mocBytes, data.Length);

            int consistent = CubismCoreNative.csmHasMocConsistency(_mocBytes, (uint)data.Length);
            if (consistent == 0)
                throw new InvalidDataException($"moc3 tidak konsisten: {mocPath}");

            _moc = CubismCoreNative.csmReviveMocInPlace(_mocBytes, (uint)data.Length);
            if (_moc == IntPtr.Zero)
                throw new InvalidDataException($"csmReviveMocInPlace gagal: {mocPath}");

            uint modelSize = CubismCoreNative.csmGetSizeofModel(_moc);
            if (modelSize == 0)
                throw new InvalidDataException("csmGetSizeofModel return 0");

            _modelBytes = CubismCoreNative.AlignedAlloc((int)modelSize, CubismCoreNative.csmAlignofModel);
            _model = CubismCoreNative.csmInitializeModelInPlace(_moc, _modelBytes, modelSize);
            if (_model == IntPtr.Zero)
                throw new InvalidDataException("csmInitializeModelInPlace return 0");

            // Canvas info
            CubismCoreNative.csmReadCanvasInfo(_model, out var size, out var origin, out float ppu);
            CanvasWidth = size.X;
            CanvasHeight = size.Y;
            PixelsPerUnit = ppu;
        }

        private void BuildIndices()
        {
            ParameterCount = CubismCoreNative.csmGetParameterCount(_model);
            PartCount = CubismCoreNative.csmGetPartCount(_model);
            DrawableCount = CubismCoreNative.csmGetDrawableCount(_model);

            var paramIds = CubismCoreNative.ReadStringArray(CubismCoreNative.csmGetParameterIds(_model), ParameterCount);
            for (int i = 0; i < paramIds.Length; i++)
                _parameterIndex[paramIds[i]] = i;

            var partIds = CubismCoreNative.ReadStringArray(CubismCoreNative.csmGetPartIds(_model), PartCount);
            for (int i = 0; i < partIds.Length; i++)
                _partIndex[partIds[i]] = i;

            var drawIds = CubismCoreNative.ReadStringArray(CubismCoreNative.csmGetDrawableIds(_model), DrawableCount);
            for (int i = 0; i < drawIds.Length; i++)
                _drawableIndex[drawIds[i]] = i;
        }

        // ── Parameter access ──────────────────────────────────────────────

        public int GetParameterIndex(string id)
            => _parameterIndex.TryGetValue(id, out int i) ? i : -1;

        public int GetPartIndex(string id)
            => _partIndex.TryGetValue(id, out int i) ? i : -1;

        public int GetDrawableIndex(string id)
            => _drawableIndex.TryGetValue(id, out int i) ? i : -1;

        public float GetParameterValue(int index)
        {
            if (index < 0 || index >= ParameterCount) return 0f;
            IntPtr values = CubismCoreNative.csmGetParameterValues(_model);
            unsafe
            {
                return ((float*)values)[index];
            }
        }

        public void SetParameterValue(int index, float value)
        {
            if (index < 0 || index >= ParameterCount) return;
            IntPtr values = CubismCoreNative.csmGetParameterValues(_model);
            unsafe
            {
                ((float*)values)[index] = value;
            }
        }

        public float GetParameterDefaultValue(int index)
        {
            if (index < 0 || index >= ParameterCount) return 0f;
            IntPtr defs = CubismCoreNative.csmGetParameterDefaultValues(_model);
            unsafe { return ((float*)defs)[index]; }
        }

        public float GetParameterMinValue(int index)
        {
            if (index < 0 || index >= ParameterCount) return 0f;
            IntPtr mins = CubismCoreNative.csmGetParameterMinimumValues(_model);
            unsafe { return ((float*)mins)[index]; }
        }

        public float GetParameterMaxValue(int index)
        {
            if (index < 0 || index >= ParameterCount) return 0f;
            IntPtr maxs = CubismCoreNative.csmGetParameterMaximumValues(_model);
            unsafe { return ((float*)maxs)[index]; }
        }

        // ── Part / Drawable access ────────────────────────────────────────

        public unsafe float* GetPartOpacitiesPtr()
            => (float*)CubismCoreNative.csmGetPartOpacities(_model);

        /// <summary>
        /// Menyembunyikan part (dan drawable yang tergabung) berdasarkan substring id.
        /// Dipakai untuk menyembunyikan watermark/kredit part seperti di JS lama.
        /// </summary>
        public unsafe void HidePartsContaining(params string[] substrings)
        {
            var partIds = CubismCoreNative.ReadStringArray(CubismCoreNative.csmGetPartIds(_model), PartCount);
            float* opacities = GetPartOpacitiesPtr();
            for (int i = 0; i < partIds.Length; i++)
            {
                string lower = partIds[i].ToLowerInvariant();
                foreach (var s in substrings)
                {
                    if (lower.Contains(s.ToLowerInvariant()))
                    {
                        opacities[i] = 0f;
                        break;
                    }
                }
            }
        }

        public unsafe void* GetDrawableVertexPositionsPtr()
            => (void*)CubismCoreNative.csmGetDrawableVertexPositions(_model);

        public unsafe void* GetDrawableVertexUvsPtr()
            => (void*)CubismCoreNative.csmGetDrawableVertexUvs(_model);

        public unsafe void* GetDrawableIndicesPtr()
            => (void*)CubismCoreNative.csmGetDrawableIndices(_model);

        public int[] ReadDrawableVertexCounts()
            => CubismCoreNative.ReadIntArray(CubismCoreNative.csmGetDrawableVertexCounts(_model), DrawableCount);

        public int[] ReadDrawableIndexCounts()
            => CubismCoreNative.ReadIntArray(CubismCoreNative.csmGetDrawableIndexCounts(_model), DrawableCount);

        public int[] ReadDrawableTextureIndices()
            => CubismCoreNative.ReadIntArray(CubismCoreNative.csmGetDrawableTextureIndices(_model), DrawableCount);

        public float[] ReadDrawableOpacities()
            => CubismCoreNative.ReadFloatArray(CubismCoreNative.csmGetDrawableOpacities(_model), DrawableCount);

        public byte[] ReadDrawableConstantFlags()
        {
            var raw = CubismCoreNative.ReadIntArray(
                CubismCoreNative.csmGetDrawableConstantFlags(_model), DrawableCount);
            var result = new byte[raw.Length];
            for (int i = 0; i < raw.Length; i++) result[i] = (byte)raw[i];
            return result;
        }

        public byte[] ReadDrawableDynamicFlags()
        {
            var raw = CubismCoreNative.ReadIntArray(
                CubismCoreNative.csmGetDrawableDynamicFlags(_model), DrawableCount);
            var result = new byte[raw.Length];
            for (int i = 0; i < raw.Length; i++) result[i] = (byte)raw[i];
            return result;
        }

        public int[] ReadDrawableMaskCounts()
            => CubismCoreNative.ReadIntArray(CubismCoreNative.csmGetDrawableMaskCounts(_model), DrawableCount);

        /// <summary>Pointer per-drawable ke array mask indices (const int**).</summary>
        public unsafe int** GetDrawableMasksPtr()
            => (int**)CubismCoreNative.csmGetDrawableMasks(_model);

        public int[] ReadRenderOrders()
            => CubismCoreNative.ReadIntArray(CubismCoreNative.csmGetRenderOrders(_model), DrawableCount);

        public CubismCoreNative.csmVector4[] ReadDrawableMultiplyColors()
        {
            int n = DrawableCount;
            IntPtr ptr = CubismCoreNative.csmGetDrawableMultiplyColors(_model);
            var result = new CubismCoreNative.csmVector4[n];
            if (ptr == IntPtr.Zero) return result;
            unsafe
            {
                var src = (CubismCoreNative.csmVector4*)ptr;
                for (int i = 0; i < n; i++) result[i] = src[i];
            }
            return result;
        }

        public CubismCoreNative.csmVector4[] ReadDrawableScreenColors()
        {
            int n = DrawableCount;
            IntPtr ptr = CubismCoreNative.csmGetDrawableScreenColors(_model);
            var result = new CubismCoreNative.csmVector4[n];
            if (ptr == IntPtr.Zero) return result;
            unsafe
            {
                var src = (CubismCoreNative.csmVector4*)ptr;
                for (int i = 0; i < n; i++) result[i] = src[i];
            }
            return result;
        }

        /// <summary>Panggil tiap frame SEBELUM baca vertex positions (setelah parameter di-set).</summary>
        public void UpdateModel()
        {
            CubismCoreNative.csmUpdateModel(_model);
        }

        public void ResetDrawableDynamicFlags()
        {
            CubismCoreNative.csmResetDrawableDynamicFlags(_model);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_modelBytes != IntPtr.Zero) { CubismCoreNative.AlignedFree(_modelBytes); _modelBytes = IntPtr.Zero; }
            if (_mocBytes != IntPtr.Zero) { CubismCoreNative.AlignedFree(_mocBytes); _mocBytes = IntPtr.Zero; }
            _model = IntPtr.Zero;
            _moc = IntPtr.Zero;
        }
    }
}
