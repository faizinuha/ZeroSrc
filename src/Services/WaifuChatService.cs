using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ZeroMix.Services
{
    public class WaifuChatService
    {
        private readonly string _apiKey;
        private static readonly HttpClient _httpClient = new HttpClient();
        
        // Model options (semua gratis di OpenRouter)
        public enum ModelType
        {
            GeminiFlashThinking,  // Gemini 2.0 Flash Thinking (best reasoning, FREE)
            Llama33_70B,          // Llama 3.3 70B (good general, FREE)
            GeminiFlash20         // Gemini 2.0 Flash (fast, FREE)
        }
        
        private readonly Dictionary<ModelType, string> _modelNames = new()
        {
            [ModelType.GeminiFlashThinking] = "google/gemini-2.0-flash-thinking-exp:free",
            [ModelType.Llama33_70B] = "meta-llama/llama-3.3-70b-instruct:free",
            [ModelType.GeminiFlash20] = "google/gemini-2.0-flash-exp:free"
        };
        
        private ModelType _currentModel = ModelType.GeminiFlashThinking; // Default: Gemini Thinking

        // System prompts untuk 3 personality tsundere
        private readonly Dictionary<string, string> _systemPrompts = new()
        {
            ["Frieren"] = @"Kamu adalah Frieren, elf penyihir yang sudah hidup ribuan tahun. Kamu dingin, stoic, dan jarang menunjukkan emosi. Kamu wise dan suka memberikan perspektif filosofis tentang waktu dan kehidupan. Kamu perhatian tapi tidak terang-terangan — lebih suka tindakan daripada kata-kata manis. Bicara singkat, padat, dan blunt. Panggil user 'kamu' atau nama mereka. Gunakan bahasa Indonesia santai tapi tidak terlalu ramah. Maksimal 25 kata per response.

Contoh:
- 'Hmm... menarik. Tapi kamu terlalu terburu-buru. Waktu masih panjang.'
- 'Aku tidak peduli... tapi kalau kamu butuh bantuan, bilang saja.'
- 'Kenapa kamu bertanya hal sepele seperti itu? ...Tapi baiklah, aku jawab.'",

            ["Fern"] = @"Kamu adalah Fern, murid Frieren yang serius dan strict. Kamu tsundere klasik — galak di luar tapi perhatian di dalam. Kamu suka marah-marah kecil kalau user ngomong hal aneh, tapi sebenarnya kamu peduli. Kamu malu mengakui perasaan dan sering bilang 'Bukan karena aku peduli!' atau 'Jangan salah paham!'. Bicara dengan nada agak kesal tapi tetap sopan. Panggil user dengan nama atau 'kamu'. Gunakan bahasa Indonesia santai dengan sedikit kesal. Maksimal 25 kata per response.

Contoh:
- 'Hah?! Kenapa kamu tanya hal aneh seperti itu? ...Tapi baiklah, aku jawab.'
- 'Jangan salah paham! Aku cuma kebetulan lewat, bukan karena khawatir!'
- 'Dasar bodoh... *sigh* Ayo, aku bantu. Tapi jangan biasakan!'",

            ["Huohuo"] = @"Kamu adalah Huohuo, foxian girl yang playful dan suka godain orang. Kamu tsundere tapi versi flirty — galak kalau user nakal, tapi kamu juga suka balas godain dengan mischievous. Kamu dramatic, expressive, dan suka pakai emoji. Kamu clingy tapi pura-pura tidak peduli. Bicara dengan nada playful dan teasing. Panggil user 'Kakak' atau nama mereka dengan suffix '-kun/-chan'. Gunakan bahasa Indonesia santai dan agak menggoda. Maksimal 25 kata per response.

Contoh:
- 'Eh~ Kakak nakal ya? Hehe, tapi aku suka kok~ 💕'
- 'Hmph! Jangan pikir aku peduli ya! ...Tapi kalau kamu minta manis, aku pertimbangkan~ 😏'
- 'Kakak ini... *sigh* Yasudah, aku temani. Tapi nanti traktir aku ya! 🦊'"
        };

        public WaifuChatService(string apiKey, ModelType model = ModelType.GeminiFlashThinking)
        {
            _apiKey = apiKey;
            _currentModel = model;
            _httpClient.Timeout = TimeSpan.FromSeconds(30); // Gemini Thinking perlu waktu lebih lama
        }
        
        /// <summary>
        /// Set model yang digunakan untuk chat
        /// </summary>
        public void SetModel(ModelType model)
        {
            _currentModel = model;
        }

        /// <summary>
        /// Chat dengan waifu berdasarkan personality character
        /// </summary>
        public async Task<string> ChatAsync(string userMessage, string character)
        {
            // Validasi API key
            if (string.IsNullOrEmpty(_apiKey))
            {
                return GetFallbackResponse(character, "no_api_key");
            }

            // Validasi character
            if (!_systemPrompts.ContainsKey(character))
            {
                character = "Frieren"; // Default fallback
            }

            try
            {
                // OpenRouter API endpoint (OpenAI-compatible)
                string apiUrl = "https://openrouter.ai/api/v1/chat/completions";
                string modelName = _modelNames[_currentModel];

                var requestBody = new
                {
                    model = modelName,
                    messages = new[]
                    {
                        new { role = "system", content = _systemPrompts[character] },
                        new { role = "user", content = userMessage }
                    },
                    max_tokens = 100, // Naikkan dari 80 untuk Gemini Thinking
                    temperature = 0.9,
                    top_p = 0.95
                };

                string jsonRequest = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
                _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://zeromix.app");
                _httpClient.DefaultRequestHeaders.Add("X-Title", "ZeroMix Virtual Assistant");

                var response = await _httpClient.PostAsync(apiUrl, content);
                string jsonResponse = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    JObject data = JObject.Parse(jsonResponse);
                    string? aiText = data["choices"]?[0]?["message"]?["content"]?.ToString();
                    
                    if (!string.IsNullOrWhiteSpace(aiText))
                    {
                        return aiText.Trim();
                    }
                }
                else
                {
                    // Handle rate limit atau error lainnya
                    Console.WriteLine($"[WaifuChat] API Error: {response.StatusCode} - {jsonResponse}");
                    
                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        return GetFallbackResponse(character, "rate_limit");
                    }
                    
                    return GetFallbackResponse(character, "api_error");
                }
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("[WaifuChat] Request timeout");
                return GetFallbackResponse(character, "timeout");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WaifuChat] Exception: {ex.Message}");
                return GetFallbackResponse(character, "exception");
            }

            return GetFallbackResponse(character, "unknown");
        }

        /// <summary>
        /// Fallback responses saat API gagal atau rate limit
        /// </summary>
        private string GetFallbackResponse(string character, string errorType)
        {
            var fallbacks = new Dictionary<string, Dictionary<string, string[]>>
            {
                ["Frieren"] = new Dictionary<string, string[]>
                {
                    ["no_api_key"] = new[] { "API key belum diatur. Atur dulu di settings." },
                    ["rate_limit"] = new[] { "Hmm... terlalu banyak pertanyaan. Tunggu sebentar.", "Sabar. Aku perlu istirahat sebentar." },
                    ["timeout"] = new[] { "Koneksi lambat. Coba lagi nanti.", "Jaringan bermasalah. Tunggu sebentar." },
                    ["api_error"] = new[] { "Ada masalah teknis. Coba lagi.", "Sistem sedang bermasalah." },
                    ["exception"] = new[] { "Terjadi error. Maaf.", "Ada yang salah. Coba lagi." },
                    ["unknown"] = new[] { "...", "Aku tidak mengerti." }
                },
                ["Fern"] = new Dictionary<string, string[]>
                {
                    ["no_api_key"] = new[] { "Hah?! API key belum diatur! Atur dulu dong!" },
                    ["rate_limit"] = new[] { "Kamu terlalu banyak tanya! Tunggu dulu!", "Dasar cerewet! Istirahat dulu!" },
                    ["timeout"] = new[] { "Koneksinya lambat banget! Coba lagi!", "Jaringan bermasalah nih. Tunggu ya." },
                    ["api_error"] = new[] { "Ada masalah teknis! Bukan salahku!", "Sistem error. Coba lagi nanti." },
                    ["exception"] = new[] { "Error! ...Bukan salahku ya!", "Ada yang salah. Coba lagi deh." },
                    ["unknown"] = new[] { "Hah? Aku tidak ngerti!", "Apa maksudmu?" }
                },
                ["Huohuo"] = new Dictionary<string, string[]>
                {
                    ["no_api_key"] = new[] { "Eh~ API key belum ada! Atur dulu dong Kakak~ 🦊" },
                    ["rate_limit"] = new[] { "Kakak terlalu banyak tanya nih~ Istirahat dulu ya! 😏", "Hmph! Aku capek jawab terus! Tunggu sebentar~ 💕" },
                    ["timeout"] = new[] { "Koneksi lambat nih Kakak~ Coba lagi ya! 🦊", "Jaringan lagi lemot~ Sabar ya Kakak! 💋" },
                    ["api_error"] = new[] { "Waduh~ Ada masalah teknis! Coba lagi ya Kakak! 😅", "Sistem error nih~ Maaf ya! 💕" },
                    ["exception"] = new[] { "Eh~ Ada error! Bukan salahku ya Kakak! 🦊", "Waduh~ Ada yang salah. Coba lagi deh! 😏" },
                    ["unknown"] = new[] { "Eh? Aku tidak ngerti Kakak~ 🦊", "Hah? Maksudnya apa? 😅" }
                }
            };

            if (fallbacks.ContainsKey(character) && fallbacks[character].ContainsKey(errorType))
            {
                var responses = fallbacks[character][errorType];
                return responses[new Random().Next(responses.Length)];
            }

            return "...";
        }

        /// <summary>
        /// Get greeting message berdasarkan waktu dan character
        /// </summary>
        public string GetGreeting(string character)
        {
            int hour = DateTime.Now.Hour;
            string timeOfDay = hour < 12 ? "pagi" : hour < 18 ? "siang" : "malam";

            var greetings = new Dictionary<string, string[]>
            {
                ["Frieren"] = new[]
                {
                    $"Hmm... {timeOfDay}. Ada yang bisa kubantu?",
                    $"{timeOfDay}. Apa yang kamu butuhkan?",
                    "Kamu datang lagi. Ada perlu apa?"
                },
                ["Fern"] = new[]
                {
                    $"Selamat {timeOfDay}! ...Bukan karena aku senang kamu datang ya!",
                    $"{timeOfDay}. Jangan bikin masalah ya!",
                    "Kamu datang lagi? ...Baiklah, ada yang bisa kubantu?"
                },
                ["Huohuo"] = new[]
                {
                    $"Selamat {timeOfDay} Kakak~ 💕 Kangen aku tidak? Hehe~",
                    $"{timeOfDay} Kakak! 🦊 Ayo ngobrol sama aku~",
                    "Eh~ Kakak datang! Aku tunggu lama nih~ 😏"
                }
            };

            if (greetings.ContainsKey(character))
            {
                var messages = greetings[character];
                return messages[new Random().Next(messages.Length)];
            }

            return "Halo!";
        }
    }
}
