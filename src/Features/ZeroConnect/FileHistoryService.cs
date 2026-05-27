using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ZeroMix.Features.ZeroConnect;

public class FileHistoryEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string FileName { get; set; } = "";
    public string? Path { get; set; }
    public string Direction { get; set; } = "received"; // "sent" or "received"
    public long SizeBytes { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

public class FileHistoryService
{
    private readonly string _storePath;
    private readonly List<FileHistoryEntry> _entries = new();

    public event EventHandler? OnChanged;

    public FileHistoryService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = System.IO.Path.Combine(appData, "ZeroMix", "ZeroConnect");
        Directory.CreateDirectory(dir);
        _storePath = System.IO.Path.Combine(dir, "history.json");

        Load();
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_storePath)) return;
            var json = File.ReadAllText(_storePath);
            var items = JsonSerializer.Deserialize<List<FileHistoryEntry>>(json);
            if (items != null)
            {
                _entries.Clear();
                _entries.AddRange(items);
            }
        }
        catch { }
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_entries);
            File.WriteAllText(_storePath, json);
        }
        catch { }
    }

    public Task AddEntryAsync(FileHistoryEntry e)
    {
        _entries.Add(e);
        Save();
        OnChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<FileHistoryEntry>> GetTodayEntriesAsync()
    {
        var today = DateTime.Today;
        var res = _entries.Where(x => x.Timestamp.Date == today).OrderByDescending(x => x.Timestamp).AsEnumerable();
        return Task.FromResult(res);
    }
}
