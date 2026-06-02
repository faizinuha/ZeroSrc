using System;
using System.Threading.Tasks;

namespace ZeroMix.ZeroShell.Commands
{
    /// <summary>
    /// Interface untuk semua shell commands.
    /// Setiap command group implement interface ini untuk handle berbagai kategori command.
    /// </summary>
    public interface IShellCommand
    {
        /// <summary>
        /// Check apakah command handler ini bisa handle command yang diberikan.
        /// </summary>
        bool CanHandle(string command);

        /// <summary>
        /// Execute command secara async dengan access ke active tab.
        /// </summary>
        Task Execute(string command, TerminalTab activeTab, Action<TerminalTab, string, string> appendToTab);
    }
}
