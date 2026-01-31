using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZeroMix.Plugins.Spotify
{
    public class SpotifyBookmark
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("artist")]
        public string Artist { get; set; } = "";

        [JsonPropertyName("coverUrl")]
        public string CoverUrl { get; set; } = "";

        [JsonPropertyName("addedAt")]
        public DateTime AddedAt { get; set; } = DateTime.Now;

        [JsonPropertyName("source")]
        public string Source { get; set; } = "local"; // "local" or "spotify"
    }

    public class BookmarkManager
    {
        private readonly string _bookmarksPath;
        private List<SpotifyBookmark> _bookmarks = new();

        public event Action<SpotifyBookmark>? OnBookmarkAdded;
        public event Action<string>? OnBookmarkRemoved;
        public event Action? OnBookmarksChanged;

        public BookmarkManager()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string spotifyDataDir = Path.Combine(appData, "ZeroMix", "Plugins", "Spotify");
            if (!Directory.Exists(spotifyDataDir)) Directory.CreateDirectory(spotifyDataDir);

            _bookmarksPath = Path.Combine(spotifyDataDir, "Bookmark.json");
            LoadBookmarks();
        }

        public void LoadBookmarks()
        {
            try
            {
                if (File.Exists(_bookmarksPath))
                {
                    string json = File.ReadAllText(_bookmarksPath);
                    using (JsonDocument doc = JsonDocument.Parse(json))
                    {
                        if (doc.RootElement.TryGetProperty("bookmarks", out var bookmarksArray))
                        {
                            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                            var bookmarksJson = JsonSerializer.Serialize(bookmarksArray, new JsonSerializerOptions { WriteIndented = true });
                            _bookmarks = JsonSerializer.Deserialize<List<SpotifyBookmark>>(bookmarksJson, options) ?? new();
                        }
                    }
                }
                else
                {
                    _bookmarks = new();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[BookmarkManager] Load Error: " + ex.Message);
                _bookmarks = new();
            }
        }

        public void SaveBookmarks()
        {
            try
            {
                var data = new { bookmarks = _bookmarks };
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_bookmarksPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[BookmarkManager] Save Error: " + ex.Message);
            }
        }

        public void AddBookmark(SpotifyBookmark bookmark)
        {
            if (_bookmarks.Any(b => b.Id == bookmark.Id)) return; // Sudah ada

            bookmark.AddedAt = DateTime.Now;
            _bookmarks.Add(bookmark);
            SaveBookmarks();
            OnBookmarkAdded?.Invoke(bookmark);
            OnBookmarksChanged?.Invoke();
        }

        public void RemoveBookmark(string trackId)
        {
            var bookmark = _bookmarks.FirstOrDefault(b => b.Id == trackId);
            if (bookmark != null)
            {
                _bookmarks.Remove(bookmark);
                SaveBookmarks();
                OnBookmarkRemoved?.Invoke(trackId);
                OnBookmarksChanged?.Invoke();
            }
        }

        public List<SpotifyBookmark> GetAllBookmarks()
        {
            return new List<SpotifyBookmark>(_bookmarks.OrderByDescending(b => b.AddedAt));
        }

        public bool IsBookmarked(string trackId)
        {
            return _bookmarks.Any(b => b.Id == trackId);
        }

        public void ClearBookmarks()
        {
            _bookmarks.Clear();
            SaveBookmarks();
            OnBookmarksChanged?.Invoke();
        }

        public int GetBookmarkCount()
        {
            return _bookmarks.Count;
        }
    }
}
