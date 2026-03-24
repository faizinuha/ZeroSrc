using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ZeroMix.Virtual_Assisten
{
    public class AiVisionService
    {
        private readonly string _apiKey;
        private static readonly HttpClient _httpClient = new HttpClient();

        // WinAPI to get active window title
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        public AiVisionService(string apiKey)
        {
            _apiKey = apiKey;
        }

        public string GetActiveWindowTitle()
        {
            try
            {
                const int nChars = 256;
                StringBuilder buff = new StringBuilder(nChars);
                IntPtr handle = GetForegroundWindow();

                if (GetWindowText(handle, buff, nChars) > 0)
                {
                    return buff.ToString();
                }
            }
            catch { }
            return "Desktop / Layar Utama";
        }

        public async Task<string> AnalyzeAppsAsync(string windowTitle, string characterName)
        {
            // Pastikan pakai API Key Groq yang dimulai dari "gsk_..."
            if (string.IsNullOrEmpty(_apiKey) || !_apiKey.StartsWith("gsk_"))
            {
                return "Key Groq belum dipasang atau salah nih, Um!";
            }

            try
            {
                // Groq Endpoint (Kompatibel dengan OpenAI format)
                string apiUrl = "https://api.groq.com/openai/v1/chat/completions";

                var requestBody = new
                {
                    model = "llama-3.3-70b-versatile", 
                    messages = new[]
                    {
                        new { role = "system", content = $"Anda adalah {characterName} dari anime. Gunakan gaya bicara karakter tersebut (misal Frieren yang tenang/datar, atau Huohuo yang pemalu). Gunakan bahasa Indonesia santai, panggil user 'Kakak'. Jangan terlalu formal, jangan pakai 'bro'. Berikan komentar pendek yang lucu atau perhatian. Maksimal 15 kata." },
                        new { role = "user", content = $"[Konteks Jendela: {windowTitle}] Berikan satu komentar pendek yang sesuai kepribadianmu!" }
                    },
                    max_tokens = 60,
                    temperature = 0.8,
                    top_p = 0.9
                };

                string jsonRequest = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

                var response = await _httpClient.PostAsync(apiUrl, content);
                string jsonResponse = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    JObject data = JObject.Parse(jsonResponse);
                    string? aiText = data["choices"]?[0]?["message"]?["content"]?.ToString();
                    return aiText?.Trim() ?? "Asik banget kelihatannya!";
                }
                
                return "Waduh, otak Groq-ku lagi nge-hang sebentar... (Cek kuota/key)";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AiVision] Groq Error: {ex.Message}");
                return "Maaf Um, ada kendala teknis sama asistennya.";
            }
        }
        public async Task<string> AskAiAsync(string userMessage, string characterName)
        {
            if (string.IsNullOrEmpty(_apiKey) || !_apiKey.StartsWith("gsk_")) return "Key Groq belum dipasang, Kak!";

            try
            {
                string apiUrl = "https://api.groq.com/openai/v1/chat/completions";
                var requestBody = new
                {
                    model = "llama-3.3-70b-versatile",
                    messages = new[]
                    {
                        new { role = "system", content = $"Anda adalah {characterName} dari anime. Bicara seperti karakter tersebut. Singkat, padat, ramah. Gunakan bahasa Indonesia santai. Panggil user 'Kakak'. Maksimal 20 kata." },
                        new { role = "user", content = userMessage }
                    },
                    max_tokens = 80
                };

                string jsonRequest = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

                var response = await _httpClient.PostAsync(apiUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    JObject data = JObject.Parse(await response.Content.ReadAsStringAsync());
                    return data["choices"]?[0]?["message"]?["content"]?.ToString()?.Trim() ?? "...";
                }
                return "Maaf Kak, aku lagi sedikit pusing.";
            }
            catch { return "Gagal ngobrol sama AI."; }
        }
    }
}
