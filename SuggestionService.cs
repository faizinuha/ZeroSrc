// suggestion service to fetch search suggestions from Google
// SuggestionService.cs
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace ZeroSrc
{
    public class SuggestionService
    {
        private readonly HttpClient _http = new HttpClient();

        public async Task<ObservableCollection<string>> GetSuggestionsAsync(string query)
        {
            var firefox = $"https://suggestqueries.google.com/complete/search?client=firefox&q={query}";
            var chrome = $"https://suggestqueries.google.com/complete/search?client=googlecgrinex&q={query}";
            var brave = $"https://suggestqueries.google.com/complete/search?client=googlecgrinex&q={query}";
            var response = await _http.GetStringAsync(url);

            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;

            var suggestions = new ObservableCollection<string>();
            foreach (var item in root[1].EnumerateArray())
            {
                suggestions.Add(item.GetString()!);
            }

            return suggestions;
        }
    }
}
