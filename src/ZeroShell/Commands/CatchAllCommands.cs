using System;
using System.IO;
using System.Threading.Tasks;

namespace ZeroMix.ZeroShell.Commands
{
    /// <summary>
    /// Catch-all handler yang menangani command yang tidak tertangani oleh handler lain.
    /// - Alias expansion (re-routed ke router via callback)
    /// - cd path tracking &amp; folder history
    /// - PsSession fallthrough untuk shell commands
    /// Didaftarkan TERAKHIR di router chain.
    /// </summary>
    public class CatchAllCommands : IShellCommand
    {
        private readonly Func<string, string> _aliasExpander;
        private readonly Action<string> _onCdFolder;
        private readonly Action<TerminalTab, string> _sendToSession;
        private readonly Func<TerminalTab, string> _getCurrentDir;
        private readonly Action<TerminalTab, string> _updateLocalDir;
        private readonly Func<string> _getPromptColor;

        public CatchAllCommands(
            Func<string, string> aliasExpander,
            Action<string> onCdFolder,
            Action<TerminalTab, string> sendToSession,
            Func<TerminalTab, string> getCurrentDir,
            Action<TerminalTab, string> updateLocalDir,
            Func<string> getPromptColor)
        {
            _aliasExpander = aliasExpander;
            _onCdFolder = onCdFolder;
            _sendToSession = sendToSession;
            _getCurrentDir = getCurrentDir;
            _updateLocalDir = updateLocalDir;
        }

        public bool CanHandle(string command)
        {
            // CatchAll: handles EVERYTHING that wasn't caught by previous handlers
            return true;
        }

        public async Task Execute(string command, TerminalTab activeTab, Action<TerminalTab, string, string> appendToTab)
        {
            // 1. Alias expansion
            string expanded = _aliasExpander(command);
            string low = expanded.ToLower().Trim();

            // 2. Folder history tracking for cd
            if (low.StartsWith("cd ") && low.Length > 3)
            {
                string folderPath = expanded.Substring(3).Trim().Trim('"');
                try
                {
                    string fullPath = Path.GetFullPath(folderPath);
                    if (Directory.Exists(fullPath))
                        _onCdFolder(fullPath);
                }
                catch { }
            }

            // 3. Local cd tracking (for prompt update — PsSession PWD tracking is primary)
            if (low == "cd" || low == "cd ~")
            {
                _updateLocalDir(activeTab, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            }
            else if (low.StartsWith("cd "))
            {
                string newPath = expanded.Substring(3).Trim().Trim('"');
                try
                {
                    string current = _getCurrentDir(activeTab);
                    string combined = Path.IsPathRooted(newPath)
                        ? newPath
                        : Path.GetFullPath(Path.Combine(current, newPath));
                    if (Directory.Exists(combined))
                        _updateLocalDir(activeTab, combined);
                }
                catch { }
            }
            else if (low == "cd.." || low == "cd ..")
            {
                var parent = Directory.GetParent(_getCurrentDir(activeTab));
                if (parent != null)
                    _updateLocalDir(activeTab, parent.FullName);
            }

            // 4. Echo command with theme-aware prompt color + send to PsSession
            string promptColor = _getPromptColor?.Invoke() ?? "#FF00D4FF";
            appendToTab(activeTab, $"  ❯ {command}\n", promptColor);
            _sendToSession(activeTab, expanded);

            await Task.CompletedTask;
        }
    }
}
