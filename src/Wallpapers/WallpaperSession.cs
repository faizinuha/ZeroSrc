using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZeroMix.Wallpapers
{
    /// <summary>
    /// Model JSON untuk menyimpan state video wallpaper yang aktif.
    /// File disimpan di AppData/ZeroMix/wallpaper_session.json
    /// </summary>
    public class WallpaperSession
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("wallpaper_path")]
        public string WallpaperPath { get; set; } = "";

        [JsonPropertyName("volume")]
        public double Volume { get; set; } = 0;

        [JsonPropertyName("type")]
        public string Type { get; set; } = "video"; // "video" atau "image"

        [JsonPropertyName("active")]
        public bool Active { get; set; } = false;

        [JsonPropertyName("last_set")]
        public string LastSet { get; set; } = DateTime.Now.ToString("o");

        // ── Path file session ──────────────────────────────────────────────
        private static readonly string SessionPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ZeroMix", "wallpaper_session.json");

        public static void Save(WallpaperSession session)
        {
            try
            {
                string dir = Path.GetDirectoryName(SessionPath)!;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                session.LastSet = DateTime.Now.ToString("o");
                string json = JsonSerializer.Serialize(session, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SessionPath, json);
            }
            catch { }
        }

        public static WallpaperSession? Load()
        {
            try
            {
                if (!File.Exists(SessionPath)) return null;
                string json = File.ReadAllText(SessionPath);
                return JsonSerializer.Deserialize<WallpaperSession>(json);
            }
            catch { return null; }
        }

        public static void Clear()
        {
            try
            {
                if (File.Exists(SessionPath))
                {
                    var s = Load();
                    if (s != null) { s.Active = false; Save(s); }
                }
            }
            catch { }
        }
    }
}
