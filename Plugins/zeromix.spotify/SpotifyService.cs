using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text.Json;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Net;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using System.Threading;

namespace ZeroMix.Plugins.Spotify
{
    public class SpotifyService : IDisposable
    {
        private readonly HttpClient _http;
        private string _accessToken = "";
        
        // Menggunakan API Keys dari file terpusat ApiKeys.cs
        private readonly string GOOGLE_CLIENT_ID = ZeroMix.ApiKeys.GOOGLE_CLIENT_ID;
        private readonly string GOOGLE_CLIENT_SECRET = ZeroMix.ApiKeys.GOOGLE_CLIENT_SECRET;
        
        public bool IsConnected => !string.IsNullOrEmpty(_accessToken);

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

        public async Task<string> InitiateLogin(CancellationToken ct = default)
        {
            try
            {
                var clientSecrets = new ClientSecrets
                {
                    ClientId = GOOGLE_CLIENT_ID,
                    ClientSecret = GOOGLE_CLIENT_SECRET
                };

                var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = clientSecrets,
                    Scopes = new[] { "https://www.googleapis.com/auth/userinfo.profile" }
                });

                var redirectUri = "http://127.0.0.1:8000/callback";
                var authorizationUrl = flow.CreateAuthorizationCodeRequest(redirectUri).Build();

                Process.Start(new ProcessStartInfo(authorizationUrl.AbsoluteUri) { UseShellExecute = true });

                var httpListener = new HttpListener();
                httpListener.Prefixes.Add(redirectUri + "/");
                httpListener.Start();

                var context = await httpListener.GetContextAsync();
                var code = context.Request.QueryString.Get("code");

                var tokenResponse = await flow.ExchangeCodeForTokenAsync("user", code, redirectUri, ct);

                httpListener.Stop();

                return tokenResponse.AccessToken;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Google Auth] " + ex.Message);
                return "";
            }
        }

        public void Dispose()
        {
            // Nothing to dispose
        }

        public async Task<(string userName, string profileImageUrl)> GetUserProfile()
        {
            if (!IsConnected) return ("Google Guest", "");

            try
            {
                var json = await _http.GetStringAsync("https://www.googleapis.com/oauth2/v1/userinfo?alt=json");
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
                    string userName = root.TryGetProperty("name", out var name) ? name.GetString() ?? "Google User" : "Google User";
                    string profileImageUrl = root.TryGetProperty("picture", out var picture) ? picture.GetString() ?? "" : "";
                    return (userName, profileImageUrl);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Google] Profile Error: " + ex.Message);
                return ("Google User", "");
            }
        }
    }
}