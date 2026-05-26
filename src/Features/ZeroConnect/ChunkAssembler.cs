using System.Collections.Concurrent;
using System.Text.Json;
using System.Linq;

namespace ZeroMix.Features.ZeroConnect;

public class ChunkMeta
{
    public string FileId { get; set; } = "";
    public int ChunkIndex { get; set; }
    public int TotalChunks { get; set; }
    public bool IsImage { get; set; }
}

public class ChunkAssembler
{
    // fileId → list of chunks (indexed)
    private readonly ConcurrentDictionary<string, byte[]?[]> _buffers = new();

    /// <summary>
    /// Returns completed file bytes when all chunks arrive, null otherwise.
    /// </summary>
    public byte[]? AddChunk(string filename, string metaJson, string base64Payload)
    {
        var meta = JsonSerializer.Deserialize<ChunkMeta>(metaJson)!;
        var chunk = Convert.FromBase64String(base64Payload);

        var chunks = _buffers.GetOrAdd(meta.FileId, _ => new byte[]?[meta.TotalChunks]);
        chunks[meta.ChunkIndex] = chunk;

        // Cek apakah semua chunk sudah tiba
        if (chunks.All(c => c != null))
        {
            _buffers.TryRemove(meta.FileId, out _);
            // Gabungkan semua chunk
            var total = chunks.Sum(c => c!.Length);
            var result = new byte[total];
            int offset = 0;
            foreach (var c in chunks)
            {
                Buffer.BlockCopy(c!, 0, result, offset, c!.Length);
                offset += c.Length;
            }
            return result;
        }

        return null; // belum selesai
    }
}
