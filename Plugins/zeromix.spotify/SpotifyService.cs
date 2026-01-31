using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text.Json;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Net;

namespace ZeroMix.Plugins.Spotify
{
    public class SpotifyService : IDisposable
    {
        private readonly HttpClient _http;
        private string _accessToken = "";
        private HttpListener? _currentListener;
        
        // PUBLIC DEMO CLIENT (Menggunakan API Keys dari Kakak)
        private static string CLIENT_ID => ApiKeys.SpotifyClientId;
        private static string REDIRECT_URI => ApiKeys.SpotifyRedirectUri;
        
        public bool IsConnected => !string.IsNullOrEmpty(_accessToken);

        public event Action<string, string, string>? OnTrackChanged; // Title, Artist, CoverUrl
        public event Action<string, string>? OnUserProfileUpdated; // UserName, ProfileImageUrl

        public SpotifyService()
        {
            _http = new HttpClient();
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public void SetToken(string token)
        {
            _accessToken = token;
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        }

        public async Task<string> InitiateLogin(System.Threading.CancellationToken ct = default)
        {
            if (_currentListener != null) { try { _currentListener.Abort(); } catch {} }
            
            _currentListener = new HttpListener();
            string basePrefix = REDIRECT_URI.Replace("/callback", "/");
            if (!basePrefix.EndsWith("/")) basePrefix += "/";
            
            _currentListener.Prefixes.Add(basePrefix);
            _currentListener.Start();

            string scope = "user-read-private user-read-currently-playing user-modify-playback-state user-library-read user-library-modify";
            // Tambahkan show_dialog=true agar pengguna bisa memilih akun jika memiliki beberapa akun terkait
            string url = $"https://accounts.spotify.com/authorize?client_id={CLIENT_ID}&response_type=token&redirect_uri={WebUtility.UrlEncode(REDIRECT_URI)}&scope={WebUtility.UrlEncode(scope)}&show_dialog=true";
            
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

            string token = "";
            var startTime = DateTime.Now;

            try
            {
                while (!ct.IsCancellationRequested && (DateTime.Now - startTime).TotalSeconds < 300)
                {
                    // Gunakan Task.WhenAny agar bisa dicancel
                    var contextTask = _currentListener.GetContextAsync();
                    var completedTask = await Task.WhenAny(contextTask, Task.Delay(-1, ct));

                    if (completedTask != contextTask) break; // Cancelled

                    var context = await contextTask;
                    var req = context.Request;
                    var res = context.Response;
                    string path = req.Url!.AbsolutePath.ToLower();
                    string rawUrl = req.Url.ToString();

                    // 1. Cek apakah ada access_token di Query String
                    var queryParams = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                    string foundToken = queryParams["access_token"];
                    
                    if (!string.IsNullOrEmpty(foundToken))
                    {
                        token = foundToken;
                        byte[] b = System.Text.Encoding.UTF8.GetBytes("<html><body style='background:#121212;color:#1DB954;font-family:sans-serif;text-align:center;padding-top:100px;'><div style='padding:40px;border:2px solid #1DB954;display:inline-block;border-radius:20px;'><h1 style='margin-bottom:0;'>Connected to ZeroMix!</h1><p style='color:#888;'>You can safely close this window now.</p></div></body></html>");
                        res.OutputStream.Write(b, 0, b.Length);
                        res.Close();
                        break;
                    }

                    // 2. Jika path adalah callback awal, sajikan halaman JS extractor
                    if (path.Contains("/callback"))
                    {
                        string html = @"<html><body style='background:#121212;color:white;font-family:sans-serif;text-align:center;padding-top:100px;'>
                                    <div style='background:#1DB954;display:inline-block;padding:20px;border-radius:10px;'>
                                        <h5 style='margin:0'>ZeroMix Sync</h5>
                                        <p id='msg'>Redirecting to secure token sink...</p>
                                    </div>
                                    <script>
                                        // Dukung fragment (#access_token=...) dan query (?access_token=...)
                                        var hash = window.location.hash.substring(1);
                                        var search = window.location.search.substring(1);
                                        var params = hash || search;
                                        if(params) {
                                            // Kirim selalu ke /callback/token agar tidak menumpuk /token/token/... pada pathname
                                            var target = window.location.origin + '/callback/token?' + params;
                                            // Jika kita sudah berada di /callback/token, jangan redirect lagi
                                            if (!window.location.pathname.endsWith('/token')) {
                                                window.location.href = target;
                                            } else {
                                                // Jika sudah di /token, tampilkan pesan dan let server menangani query
                                                document.getElementById('msg').innerText = 'Processing token... You can close this window.';
                                            }
                                        } else {
                                            document.getElementById('msg').innerText = 'Waiting for token... If the browser did not return a token, close and retry login.';
                                        }
                                    </script>
                                </body></html>";
                        byte[] b = System.Text.Encoding.UTF8.GetBytes(html);
                        res.OutputStream.Write(b, 0, b.Length);
                        res.Close();
                    }
                    else
                    {
                        // Fallback buat request lain (favicon dll) agar browser tidak bengong 400
                        res.StatusCode = 200;
                        res.Close();
                    }
                }
            }
            catch (Exception ex) { Debug.WriteLine("[Spotify Auth] " + ex.Message); }
            finally 
            { 
                if (_currentListener != null) { try { _currentListener.Stop(); } catch {} }
                _currentListener = null; 
            }

            return token;
        }

        public void Dispose()
        {
            if (_currentListener != null) { try { _currentListener.Abort(); } catch {} }
        }

        public async Task<bool> CheckPlayerState()
        {
            if (!IsConnected) return false;
            try
            {
                var json = await _http.GetStringAsync("https://api.spotify.com/v1/me/player/currently-playing");
                if (string.IsNullOrEmpty(json)) return false;

                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
                    if (root.TryGetProperty("item", out var item) && item.ValueKind != JsonValueKind.Null)
                    {
                        string name = item.GetProperty("name").GetString() ?? "Unknown";
                        string artist = item.GetProperty("artists")[0].GetProperty("name").GetString() ?? "Unknown";
                        string cover = item.GetProperty("album").GetProperty("images")[0].GetProperty("url").GetString() ?? "";
                        
                        OnTrackChanged?.Invoke(name, artist, cover);
                        return true;
                    }
                }
            }
            catch (Exception ex) 
            {
                Debug.WriteLine("[Spotify] Sync Error: " + ex.Message);
            }
            return false;
        }

        public async Task PlayPause()
        {
            // Note: This endpoint requires Premium
            // Logic: Check state -> if playing then pause, else play
        }

        public async Task Next()
        {
             if (!IsConnected) return;
             await _http.PostAsync("https://api.spotify.com/v1/me/player/next", null);
        }

        public async Task Previous()
        {
             if (!IsConnected) return;
             await _http.PostAsync("https://api.spotify.com/v1/me/player/previous", null);
        }
        
        // --- SEARCH ---
        public async Task<List<SpotifyTrack>> Search(string query)
        {
            var results = new List<SpotifyTrack>();
            if (!IsConnected) return results;

            try
            {
                string encodedQuery = WebUtility.UrlEncode(query);
                string url = $"https://api.spotify.com/v1/search?q={encodedQuery}&type=track&limit=5";
                
                var json = await _http.GetStringAsync(url);
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var tracks = doc.RootElement.GetProperty("tracks").GetProperty("items");
                    foreach (var track in tracks.EnumerateArray())
                    {
                        results.Add(new SpotifyTrack
                        {
                            Id = track.GetProperty("id").GetString() ?? "",
                            Name = track.GetProperty("name").GetString() ?? "",
                            Artist = track.GetProperty("artists")[0].GetProperty("name").GetString() ?? "",
                            CoverUrl = track.GetProperty("album").GetProperty("images")[2].GetProperty("url").GetString() ?? "", // Small image
                            PreviewUrl = track.TryGetProperty("preview_url", out var pv) ? pv.GetString() ?? "" : ""
                        });
                    }
                }
            }
            catch {}
            return results;
        }

        // --- PROFILE ---
        public async Task<(string userName, string profileImageUrl)> GetUserProfile()
        {
            if (!IsConnected) return ("Spotify Guest", "");

            try
            {
                var json = await _http.GetStringAsync("https://api.spotify.com/v1/me");
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
                    string userName = root.TryGetProperty("display_name", out var name) ? name.GetString() ?? "Spotify User" : "Spotify User";
                    string profileImageUrl = "";

                    if (root.TryGetProperty("images", out var images) && images.GetArrayLength() > 0)
                    {
                        profileImageUrl = images[0].GetProperty("url").GetString() ?? "";
                    }

                    return (userName, profileImageUrl);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Spotify] Profile Error: " + ex.Message);
                return ("Spotify User", "");
            }
        }

        // --- USER SAVED TRACKS (for authenticated sync) ---
        public async Task<List<SpotifyTrack>> GetUserSavedTracks(int limit = 10)
        {
            var results = new List<SpotifyTrack>();
            if (!IsConnected) return results;

            try
            {
                string url = $"https://api.spotify.com/v1/me/tracks?limit={limit}";
                var json = await _http.GetStringAsync(url);
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var items = doc.RootElement.GetProperty("items");
                    foreach (var item in items.EnumerateArray())
                    {
                        if (item.TryGetProperty("track", out var track))
                        {
                            results.Add(new SpotifyTrack
                            {
                                Id = track.GetProperty("id").GetString() ?? "",
                                Name = track.GetProperty("name").GetString() ?? "",
                                Artist = track.GetProperty("artists")[0].GetProperty("name").GetString() ?? "",
                                CoverUrl = track.GetProperty("album").GetProperty("images")[2].GetProperty("url").GetString() ?? "",
                                PreviewUrl = track.TryGetProperty("preview_url", out var pv) ? pv.GetString() ?? "" : ""
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Spotify] Saved Tracks Error: " + ex.Message);
            }
            return results;
        }

        // --- SAVE TRACK (add to Spotify Liked Songs) ---
        public async Task<bool> SaveTrack(string trackId)
        {
            if (!IsConnected) return false;

            try
            {
                var content = new StringContent("");
                var response = await _http.PutAsync($"https://api.spotify.com/v1/me/tracks?ids={trackId}", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Spotify] Save Track Error: " + ex.Message);
                return false;
            }
        }

        // --- REMOVE TRACK (remove from Spotify Liked Songs) ---
        public async Task<bool> RemoveTrack(string trackId)
        {
            if (!IsConnected) return false;

            try
            {
                var response = await _http.DeleteAsync($"https://api.spotify.com/v1/me/tracks?ids={trackId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Spotify] Remove Track Error: " + ex.Message);
                return false;
            }
        }
    }

    public class SpotifyTrack
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Artist { get; set; } = "";
        public string CoverUrl { get; set; } = "";
        public string PreviewUrl { get; set; } = "";
    }
}
