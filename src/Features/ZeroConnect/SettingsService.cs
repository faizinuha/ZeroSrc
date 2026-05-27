using System;
using System.IO;
using System.Text.Json;

namespace ZeroMix.Features.ZeroConnect;

public class SettingsModel
{
    public bool ShowFeature1 { get; set; } = true;
    public bool ShowFeature2 { get; set; } = true;
    public bool ShowFeature3 { get; set; } = true;
    public bool ShowFeature4 { get; set; } = true;
    public bool ShowFeature5 { get; set; } = true;
    public bool ShowFeature6 { get; set; } = true;

    public SettingsModel Clone() => JsonSerializer.Deserialize<SettingsModel>(JsonSerializer.Serialize(this))!;
}

public class SettingsService
{
    private static readonly Lazy<SettingsService> _inst = new(() => new SettingsService());
    public static SettingsService Instance => _inst.Value;

    private readonly string _path;
    public SettingsModel Model { get; private set; } = new SettingsModel();

    public event EventHandler? OnChanged;

    private SettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, "ZeroMix");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "settings.json");
        Load();
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_path)) return;
            var json = File.ReadAllText(_path);
            var m = JsonSerializer.Deserialize<SettingsModel>(json);
            if (m != null) Model = m;
        }
        catch { }
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(Model, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_path, json);
            OnChanged?.Invoke(this, EventArgs.Empty);
        }
        catch { }
    }

    public void Update(SettingsModel newModel)
    {
        Model = newModel.Clone();
        Save();
    }
}