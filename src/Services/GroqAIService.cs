using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ZeroMix.Services
{
    public class GroqAIService
    {
        private readonly HttpClient _client;
        private readonly string _apiKey;

        public GroqAIService()
        {
            _client = new HttpClient();
            _apiKey = ApiKeys.OPENAI_API_KEY; // Groq API key dari ApiKeys.cs
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }

        public async Task<string> AnalyzeVideoForEditing(string videoDescription, double duration)
        {
            var prompt = $@"You are a professional video editor AI. Analyze this video and suggest optimal editing:

Video Description: {videoDescription}
Duration: {duration} seconds

Provide editing suggestions in VALID JSON format (no markdown, no code blocks):
{{
    ""cuts"": [{{""time"": 5.2, ""reason"": ""slow moment""}}],
    ""effects"": [{{""time"": 10.5, ""type"": ""zoom"", ""intensity"": 0.8}}],
    ""volumeChanges"": [{{""time"": 15.0, ""volume"": 0.5, ""reason"": ""dialogue""}}],
    ""transitions"": [{{""time"": 20.0, ""type"": ""fade""}}],
    ""musicSuggestion"": ""upbeat""
}}

IMPORTANT: Return ONLY the JSON object, no explanations, no markdown formatting.";

            var requestBody = new
            {
                model = "llama-3.3-70b-versatile",
                messages = new[]
                {
                    new { role = "system", content = "You are a professional video editing AI assistant. Always respond with valid JSON only." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.7,
                max_tokens = 2000,
                response_format = new { type = "json_object" }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _client.PostAsync("https://api.groq.com/openai/v1/chat/completions", content);
                var responseText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Groq API Error ({response.StatusCode}): {responseText}");
                }

                var result = JsonDocument.Parse(responseText);
                var aiResponse = result.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                // Clean up response if it has markdown code blocks
                var cleanResponse = aiResponse ?? "{}";
                if (cleanResponse.Contains("```json"))
                {
                    cleanResponse = cleanResponse.Replace("```json", "").Replace("```", "").Trim();
                }

                return cleanResponse;
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Network error: {ex.Message}. Check your internet connection.");
            }
            catch (Exception ex)
            {
                throw new Exception($"AI Analysis failed: {ex.Message}");
            }
        }

        public async Task<string> GenerateSubtitles(string audioTranscript, double duration)
        {
            var prompt = $@"Generate SRT subtitle format for this transcript:

Transcript: {audioTranscript}
Video Duration: {duration} seconds

Generate proper SRT format with timestamps. Example:
1
00:00:00,000 --> 00:00:03,500
First subtitle text

2
00:00:03,500 --> 00:00:07,000
Second subtitle text

Make subtitles concise and well-timed.";

            var requestBody = new
            {
                model = "llama-3.3-70b-versatile",
                messages = new[]
                {
                    new { role = "system", content = "You are a subtitle generation expert." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.5,
                max_tokens = 3000
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("https://api.groq.com/openai/v1/chat/completions", content);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Groq API Error: {responseText}");
            }

            var result = JsonDocument.Parse(responseText);
            var srtContent = result.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return srtContent ?? "";
        }
    }

    // Model untuk AI Editing Suggestions
    public class AIEditingSuggestion
    {
        public CutSuggestion[]? Cuts { get; set; }
        public EffectSuggestion[]? Effects { get; set; }
        public VolumeSuggestion[]? VolumeChanges { get; set; }
        public TransitionSuggestion[]? Transitions { get; set; }
        public string? MusicSuggestion { get; set; }
    }

    public class CutSuggestion
    {
        public double Time { get; set; }
        public string? Reason { get; set; }
    }

    public class EffectSuggestion
    {
        public double Time { get; set; }
        public string? Type { get; set; }
        public double Intensity { get; set; }
    }

    public class VolumeSuggestion
    {
        public double Time { get; set; }
        public double Volume { get; set; }
        public string? Reason { get; set; }
    }

    public class TransitionSuggestion
    {
        public double Time { get; set; }
        public string? Type { get; set; }
    }
}
