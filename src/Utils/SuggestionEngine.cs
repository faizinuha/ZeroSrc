using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ZeroMix.Utils
{
    public class SuggestionItem
    {
        public string DisplayText { get; set; } = string.Empty;
        public string CompletionText { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // "Folder", "Command", "File"
        public int Priority { get; set; } = 0; // Higher = prioritize first
    }

    public class SuggestionEngine
    {
        private readonly FolderHistoryManager _folderHistory;
        private readonly List<string> _commandHistory;

        public SuggestionEngine(FolderHistoryManager folderHistory, List<string> commandHistory)
        {
            _folderHistory = folderHistory;
            _commandHistory = commandHistory ?? new List<string>();
        }

        public List<SuggestionItem> GetSuggestions(string input, int maxResults = 8)
        {
            if (string.IsNullOrWhiteSpace(input))
                return new List<SuggestionItem>();

            var suggestions = new List<SuggestionItem>();

            // Parse input to determine context
            var trimmed = input.Trim();

            // Check if it's a cd command or path-like
            if (trimmed.StartsWith("cd ", StringComparison.OrdinalIgnoreCase))
            {
                string pathPrefix = trimmed.Substring(3).Trim();
                suggestions.AddRange(GetPathSuggestions(pathPrefix, maxResults));
            }
            else if (trimmed.Contains("\\") || trimmed.Contains("/"))
            {
                // Path-like input
                suggestions.AddRange(GetPathSuggestions(trimmed, maxResults));
            }
            else if (trimmed.StartsWith("!"))
            {
                // Command suggestions
                suggestions.AddRange(GetCommandSuggestions(trimmed, maxResults));
            }
            else
            {
                // Mixed suggestions: folder history (top), then commands, then files
                suggestions.AddRange(GetFolderHistorySuggestions(trimmed, maxResults / 2));
                if (suggestions.Count < maxResults)
                {
                    suggestions.AddRange(GetCommandSuggestions(trimmed, maxResults - suggestions.Count));
                }
            }

            return suggestions.OrderByDescending(s => s.Priority).Take(maxResults).ToList();
        }

        private List<SuggestionItem> GetFolderHistorySuggestions(string prefix, int maxResults)
        {
            var folders = _folderHistory.GetSuggestedFolders(prefix, maxResults);
            return folders.Select((f, idx) => new SuggestionItem
            {
                DisplayText = f,
                CompletionText = f,
                Category = "Folder",
                Priority = 1000 - idx  // Prioritize top folders
            }).ToList();
        }

        private List<SuggestionItem> GetPathSuggestions(string pathPrefix, int maxResults)
        {
            var suggestions = new List<SuggestionItem>();

            try
            {
                // Extract directory and file prefix
                string directory = Path.GetDirectoryName(pathPrefix) ?? "";
                string filePrefix = Path.GetFileName(pathPrefix);

                if (string.IsNullOrEmpty(directory))
                    directory = Directory.GetCurrentDirectory();

                if (!Directory.Exists(directory))
                    return suggestions;

                // Get matching directories
                var dirs = Directory.GetDirectories(directory)
                    .Where(d => Path.GetFileName(d).StartsWith(filePrefix, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(d => d)
                    .Take(maxResults / 2)
                    .Select((d, idx) => new SuggestionItem
                    {
                        DisplayText = Path.GetFileName(d) + "\\",
                        CompletionText = d + "\\",
                        Category = "Folder",
                        Priority = 500 - idx
                    });

                suggestions.AddRange(dirs);

                // Get matching files
                if (suggestions.Count < maxResults)
                {
                    var files = Directory.GetFiles(directory)
                        .Where(f => Path.GetFileName(f).StartsWith(filePrefix, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(f => f)
                        .Take(maxResults - suggestions.Count)
                        .Select((f, idx) => new SuggestionItem
                        {
                            DisplayText = Path.GetFileName(f),
                            CompletionText = f,
                            Category = "File",
                            Priority = 300 - idx
                        });

                    suggestions.AddRange(files);
                }
            }
            catch { }

            return suggestions;
        }

        private List<SuggestionItem> GetCommandSuggestions(string prefix, int maxResults)
        {
            var suggestions = new List<SuggestionItem>();

            // Built-in commands
            var builtInCommands = new[]
            {
                "!sys", "!task", "!wifi", "!ip", "!battery", "!disk", "!apps",
                "!startup", "!wdm", "!install", "!startmenu", "!restore", "!desktop",
                "!notepad", "!everglass", "!exit", "!help"
            };

            var matching = builtInCommands
                .Where(c => c.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Select((c, idx) => new SuggestionItem
                {
                    DisplayText = c,
                    CompletionText = c,
                    Category = "Command",
                    Priority = 800 - idx
                });

            suggestions.AddRange(matching);

            // Command history
            if (suggestions.Count < maxResults)
            {
                var historyMatches = _commandHistory
                    .Where(c => c.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(maxResults - suggestions.Count)
                    .Select((c, idx) => new SuggestionItem
                    {
                        DisplayText = c,
                        CompletionText = c,
                        Category = "Command",
                        Priority = 700 - idx
                    });

                suggestions.AddRange(historyMatches);
            }

            return suggestions;
        }
    }
}
