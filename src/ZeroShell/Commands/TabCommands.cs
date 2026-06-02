using System;
using System.Threading.Tasks;

namespace ZeroMix.ZeroShell.Commands
{
    /// <summary>
    /// Handler untuk tab management commands: !tab, !close, !settings
    /// </summary>
    public class TabCommands : IShellCommand
    {
        private Action? _addTab;
        private Action? _closeActiveTab;
        private Action? _openSettings;

        public TabCommands(Action addTab, Action closeActiveTab, Action openSettings)
        {
            _addTab = addTab;
            _closeActiveTab = closeActiveTab;
            _openSettings = openSettings;
        }

        public bool CanHandle(string command)
        {
            var low = command.ToLower().Trim();
            return low == "!tab" || low == "!close" || low == "!settings";
        }

        public async Task Execute(string command, TerminalTab activeTab, Action<TerminalTab, string, string> appendToTab)
        {
            var low = command.ToLower().Trim();

            if (low == "!tab")
            {
                _addTab?.Invoke();
                return;
            }

            if (low == "!close")
            {
                _closeActiveTab?.Invoke();
                return;
            }

            if (low == "!settings")
            {
                _openSettings?.Invoke();
                return;
            }

            await Task.CompletedTask;
        }
    }
}
