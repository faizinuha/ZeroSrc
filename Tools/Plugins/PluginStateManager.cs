using System;
using System.IO;
using System.Text.Json;

namespace ZeroMix.Plugins
{
    /// <summary>
    /// Simpan dan restore state toggle semua plugin ke AppData/ZeroMix/plugin_state.json
    /// </summary>
    public static class PluginStateManager
    {
        private static readonly string StatePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ZeroMix", "plugin_state.json");

        public static PluginState Load()
        {
            try
            {
                if (!File.Exists(StatePath)) return new PluginState();
                var json = File.ReadAllText(StatePath);
                return JsonSerializer.Deserialize<PluginState>(json) ?? new PluginState();
            }
            catch { return new PluginState(); }
        }

        public static void Save(PluginState state)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(StatePath)!);
                File.WriteAllText(StatePath,
                    JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }
    }

    public class PluginState
    {
        public bool BatteryAssistant  { get; set; } = false;
        public bool ChargerNotif      { get; set; } = false;
        public bool TranslateEngine   { get; set; } = false;
        public bool TranslateKeyboard { get; set; } = false;
        public bool TranslateGameMode { get; set; } = false;
        public bool TranslateBubble   { get; set; } = false;
    }
}
