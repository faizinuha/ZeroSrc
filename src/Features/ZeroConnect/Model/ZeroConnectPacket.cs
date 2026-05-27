using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZeroMix.Features.ZeroConnect;

public enum PacketType
{
   ClipboardText = 0,
   ClipboardImage = 1,
   FileTransfer = 2,
   Ping = 3,
   Pong = 4,
   ChunkTransfer = 5,
   Auth = 6
}

public class ZeroConnectPacket
{
   [JsonPropertyName("type")]
   public PacketType Type { get; set; }
   
    [JsonPropertyName("payload")]
    public string? Payload { get; set; }       // Text / Base64 image / JSON metadata

    [JsonPropertyName("filename")]
    public string? FileName { get; set; }

    [JsonPropertyName("meta")]
    public string? Meta { get; set; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public string Serialize() => JsonSerializer.Serialize(this);

    public static ZeroConnectPacket? Deserialize(string json)
        => JsonSerializer.Deserialize<ZeroConnectPacket>(json);
}