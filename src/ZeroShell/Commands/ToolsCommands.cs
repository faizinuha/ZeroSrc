using System;
using System.Threading.Tasks;
using System.Windows;

namespace ZeroMix.ZeroShell.Commands
{
    /// <summary>
    /// Handler untuk tools commands: !install, !wdm, !startmenu, !restore, !desktop, !notepad, !everglass, !exit, !tasks
    /// </summary>
    public class ToolsCommands : IShellCommand
    {
        private Action<TerminalTab>? _showSelectionMenu;
        private Action? _closeWindow;
        private Action<WdmWindow, TerminalTab>? _showWdmWindow;
        private Action<TerminalTab>? _launchDesktopWidget;
        private Action<TerminalTab>? _toggleStartMenuInterceptor;
        private Action<TerminalTab>? _restoreAllStyles;
        private Action<TerminalTab>? _applyEverglass;
        private Action<string>? _runProfessionalTasks;
        private string? _autoRunCommand;

        public ToolsCommands(
            Action<TerminalTab> showSelectionMenu,
            Action closeWindow,
            Action<WdmWindow, TerminalTab> showWdmWindow,
            Action<TerminalTab> launchDesktopWidget,
            Action<TerminalTab> toggleStartMenuInterceptor,
            Action<TerminalTab> restoreAllStyles,
            Action<TerminalTab> applyEverglass,
            Action<string> runProfessionalTasks,
            string autoRunCommand)
        {
            _showSelectionMenu = showSelectionMenu;
            _closeWindow = closeWindow;
            _showWdmWindow = showWdmWindow;
            _launchDesktopWidget = launchDesktopWidget;
            _toggleStartMenuInterceptor = toggleStartMenuInterceptor;
            _restoreAllStyles = restoreAllStyles;
            _applyEverglass = applyEverglass;
            _runProfessionalTasks = runProfessionalTasks;
            _autoRunCommand = autoRunCommand;
        }

        public bool CanHandle(string command)
        {
            var low = command.ToLower().Trim();
            return low == "!install" || low == "!wdm" || low == "!startmenu" || 
                   low == "!restore" || low == "!desktop" || low == "!notepad" || 
                   low == "!everglass" || low == "!exit" || low == "!tasks";
        }

        public async Task Execute(string command, TerminalTab activeTab, Action<TerminalTab, string, string> appendToTab)
        {
            var low = command.ToLower().Trim();

            if (low == "!install") {
                _showSelectionMenu?.Invoke(activeTab);
                return;
            }

            if (low == "!wdm") {
                System.Windows.Application.Current?.Dispatcher.Invoke(() => {
                    var wdmWindow = new WdmWindow();
                    _showWdmWindow?.Invoke(wdmWindow, activeTab);
                });
                appendToTab(activeTab, "\n  ⚡ WDM Window opened.\n\n", "#00D4FF");
                return;
            }

            if (low == "!startmenu") {
                System.Windows.Application.Current?.Dispatcher.Invoke(() => {
                    _toggleStartMenuInterceptor?.Invoke(activeTab);
                });
                return;
            }

            if (low == "!restore") {
                System.Windows.Application.Current?.Dispatcher.Invoke(() => {
                    _restoreAllStyles?.Invoke(activeTab);
                });
                appendToTab(activeTab, "\n  ✅ All styles restored to Windows default. System is back to normal.\n\n", "#4EC94E");
                return;
            }

            if (low == "!desktop") {
                System.Windows.Application.Current?.Dispatcher.Invoke(() => {
                    _launchDesktopWidget?.Invoke(activeTab);
                });
                return;
            }

            if (low == "!notepad") {
                System.Diagnostics.Process.Start("notepad.exe");
                appendToTab(activeTab, "\n  📝 Notepad diluncurkan.\n\n", "#FF00D4FF");
                return;
            }

            if (low == "!everglass") {
                System.Windows.Application.Current?.Dispatcher.Invoke(() => {
                    _applyEverglass?.Invoke(activeTab);
                });
                appendToTab(activeTab, "\n  💎 Glass applied to Explorer and Taskbar.\n\n", "#FF00D4FF");
                return;
            }

            if (low == "!exit") {
                System.Windows.Application.Current?.Dispatcher.Invoke(() => {
                    _closeWindow?.Invoke();
                });
                return;
            }

            if (low == "!tasks") {
                // Jika sudah ada AutoRunCommand berarti kita di window "Task", langsung jalankan
                if (!string.IsNullOrEmpty(_autoRunCommand)) {
                    _runProfessionalTasks?.Invoke("tasks");
                    return;
                }
                
                // Jika tidak, buka window baru khusus task
                var taskWin = new ZeroShellWindow();
                taskWin.AutoRunCommand = "!tasks";
                taskWin.Show();
                appendToTab(activeTab, "\n  🚀 Membuka Terminal Task ...\n\n", "#FF6BDDFF");
                return;
            }

            await Task.CompletedTask;
        }
    }
}
