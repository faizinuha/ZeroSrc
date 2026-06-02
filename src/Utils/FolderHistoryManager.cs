using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZeroMix.Utils
{
    public class FolderHistoryEntry
    {
        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        [JsonPropertyName("visitCount")]
        public int VisitCount { get; set; }

        [JsonPropertyName("lastVisited")]
        public DateTime LastVisited { get; set; }
    }

    public class FolderHistoryManager
    {
        private readonly string _historyFilePath;
        private List<FolderHistoryEntry> _history = new List<FolderHistoryEntry>();
        private readonly object _lockObj = new object();

        public FolderHistoryManager()
        {
            string zeromixDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".zeromix"
            );
            Directory.CreateDirectory(zeromixDir);
            _historyFilePath = Path.Combine(zeromixDir, "folder-history.json");
            LoadHistory();
        }

        private void LoadHistory()
        {
            lock (_lockObj)
            {
                try
                {
                    if (File.Exists(_historyFilePath))
                    {
                        string json = File.ReadAllText(_historyFilePath);
                        _history = JsonSerializer.Deserialize<List<FolderHistoryEntry>>(json) ?? new List<FolderHistoryEntry>();
                    }
                }
                catch
                {
                    _history = new List<FolderHistoryEntry>();
                }
            }
        }

        private void SaveHistory()
        {
            lock (_lockObj)
            {
                try
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    string json = JsonSerializer.Serialize(_history, options);
                    File.WriteAllText(_historyFilePath, json);
                }
                catch { }
            }
        }

        public void RecordFolderVisit(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
                return;

            lock (_lockObj)
            {
                var normalized = Path.GetFullPath(folderPath);
                var existing = _history.FirstOrDefault(h => 
                    string.Equals(h.Path, normalized, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    existing.VisitCount++;
                    existing.LastVisited = DateTime.Now;
                }
                else
                {
                    _history.Add(new FolderHistoryEntry
                    {
                        Path = normalized,
                        VisitCount = 1,
                        LastVisited = DateTime.Now
                    });
                }

                // Keep only top 100 most visited
                _history = _history.OrderByDescending(h => h.VisitCount)
                    .ThenByDescending(h => h.LastVisited)
                    .Take(100)
                    .ToList();

                SaveHistory();
            }
        }

        public List<string> GetTopFolders(int count = 10)
        {
            lock (_lockObj)
            {
                return _history
                    .OrderByDescending(h => h.VisitCount)
                    .ThenByDescending(h => h.LastVisited)
                    .Take(count)
                    .Select(h => h.Path)
                    .ToList();
            }
        }

        public List<string> GetSuggestedFolders(string prefix, int maxResults = 10)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                return GetTopFolders(maxResults);

            lock (_lockObj)
            {
                var normalized = prefix.Replace('/', '\\');
                return _history
                    .Where(h => h.Path.StartsWith(normalized, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(h => h.VisitCount)
                    .ThenByDescending(h => h.LastVisited)
                    .Take(maxResults)
                    .Select(h => h.Path)
                    .ToList();
            }
        }
    }
}
