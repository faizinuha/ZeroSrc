using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ZeroMix.ZeroShell.Commands
{
    /// <summary>
    /// Router untuk dispatch commands ke handler yang sesuai.
    /// Menggantikan 500+ line ProcessCommand god method.
    /// </summary>
    public class ZeroShellCommandRouter
    {
        private readonly List<IShellCommand> _handlers = new();

        public ZeroShellCommandRouter()
        {
        }

        /// <summary>
        /// Register command handler.
        /// </summary>
        public void Register(IShellCommand handler)
        {
            _handlers.Add(handler);
        }

        /// <summary>
        /// Route command ke handler yang appropriate.
        /// Jika tidak ada handler, return false.
        /// </summary>
        public async Task<bool> HandleCommand(string command, TerminalTab activeTab, 
            Action<TerminalTab, string, string> appendToTab)
        {
            // Loop through all registered handlers
            foreach (var handler in _handlers)
            {
                if (handler.CanHandle(command))
                {
                    await handler.Execute(command, activeTab, appendToTab);
                    return true;
                }
            }

            return false; // No handler found
        }
    }
}
