using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace ZeroMix.ZeroShell.Commands
{
    /// <summary>
    /// Handler untuk visual commands: !font, !layout, !alias, !unalias
    /// </summary>
    public class VisualCommands : IShellCommand
    {
        private Action<TerminalTab>? _showSelectionMenu;
        private Dictionary<string, string>? _aliases;
        private Action? _saveAliases;
        private string[] _fontNames = new[] { "Consolas", "JetBrains Mono", "Fira Code", "Cascadia Mono", "Courier New" };
        private string[] _layoutNames = new[] { "Neofetch", "Full Terminal", "Compact", "Retro Green", "Cyberpunk Neon", "Pixel Retro", "Glass Minimalist", "Tiled (Dynamic)" };
        private Action<int>? _setCurrentFont;
        private Action<int>? _setCurrentLayout;
        private Action? _applyFont;
        private Action? _applyLayout;

        public VisualCommands(
            Action<TerminalTab> showSelectionMenu,
            Dictionary<string, string> aliases,
            Action saveAliases,
            Action<int> setCurrentFont,
            Action<int> setCurrentLayout,
            Action applyFont,
            Action applyLayout)
        {
            _showSelectionMenu = showSelectionMenu;
            _aliases = aliases;
            _saveAliases = saveAliases;
            _setCurrentFont = setCurrentFont;
            _setCurrentLayout = setCurrentLayout;
            _applyFont = applyFont;
            _applyLayout = applyLayout;
        }

        public bool CanHandle(string command)
        {
            var low = command.ToLower().Trim();
            return low == "!font" || low.StartsWith("!font ") ||
                   low == "!layout" || low.StartsWith("!layout ") ||
                   low == "!alias" || low.StartsWith("!alias ") ||
                   low.StartsWith("!unalias ");
        }

        public async Task Execute(string command, TerminalTab activeTab, Action<TerminalTab, string, string> appendToTab)
        {
            var low = command.ToLower().Trim();

            // ALIAS
            if (low == "!alias") {
                appendToTab(activeTab, "\n  💡 TIPS ALIAS:\n", "#FFFFDA6B");
                appendToTab(activeTab, "  Pake alias buat cepetin buka apapun. Contoh:\n", "#CCCCCC");
                appendToTab(activeTab, "  !alias gh=start https://github.com/faizinuha\n", "#FF6BDDFF");
                appendToTab(activeTab, "  (Nanti tinggal ketik 'gh' buat buka GitHub Kakak)\n\n", "#888888");
                
                if (_aliases != null && _aliases.Count > 0) {
                    appendToTab(activeTab, "  📝 ALIAS AKTIF:\n", "#FFFFDA6B");
                    foreach (var kv in _aliases) appendToTab(activeTab, $"    {kv.Key}  →  {kv.Value}\n", "#FFCC6BFF");
                    appendToTab(activeTab, "\n", "#888888");
                }
                return;
            }

            if (low.StartsWith("!alias ") && command.Contains('=')) {
                string rest = command.Substring(7);
                int eq = rest.IndexOf('=');
                if (eq > 0) {
                    string key = rest.Substring(0, eq).Trim();
                    string val = rest.Substring(eq + 1).Trim();
                    if (_aliases != null) {
                        _aliases[key] = val;
                        _saveAliases?.Invoke();
                        appendToTab(activeTab, $"\n  ✅ Alias tersimpan: {key} → {val}\n\n", "#FF27C93F");
                    }
                }
                return;
            }

            if (low.StartsWith("!unalias ")) {
                string key = command.Substring(9).Trim();
                if (_aliases != null && _aliases.Remove(key)) {
                    _saveAliases?.Invoke();
                    appendToTab(activeTab, $"\n  🗑 Alias '{key}' dihapus.\n\n", "#FFFF9F43");
                }
                else {
                    appendToTab(activeTab, $"\n  ❌ Alias '{key}' tidak ditemukan.\n\n", "#FFFF6B6B");
                }
                return;
            }

            // FONT
            if (low == "!font") {
                _showSelectionMenu?.Invoke(activeTab);
                return;
            }

            if (low.StartsWith("!font ") && int.TryParse(low.Substring(6), out int fi) && fi >= 1 && fi <= _fontNames.Length) {
                _setCurrentFont?.Invoke(fi - 1);
                _applyFont?.Invoke();
                appendToTab(activeTab, $"\n  🔤 Font baru: {_fontNames[fi - 1]}\n\n", "#FFCC6BFF");
                return;
            }

            // LAYOUT
            if (low == "!layout") {
                _showSelectionMenu?.Invoke(activeTab);
                return;
            }

            if (low.StartsWith("!layout ") && int.TryParse(low.Substring(8), out int li) && li >= 1 && li <= _layoutNames.Length) {
                _setCurrentLayout?.Invoke(li - 1);
                _applyLayout?.Invoke();
                appendToTab(activeTab, $"\n  🎨 Layout baru: {_layoutNames[li - 1]}\n\n", "#FFCC6BFF");
                return;
            }

            await Task.CompletedTask;
        }
    }
}
