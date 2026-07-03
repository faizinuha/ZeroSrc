using System;
using System.Threading.Tasks;
using ZeroMix.ZeroShell;
using ZeroMix.ZeroShell.Commands;
using zeromix.SnapLayout.Models;

namespace zeromix.SnapLayout
{
    /// <summary>
    /// ZeroShell command handler untuk Snap Layout.
    /// Commands: !snap (toggle), !snap 2col / !snap 2x2 (set preset),
    /// !snap status (tampilkan status).
    /// </summary>
    public class SnapCommands : IShellCommand
    {
        private readonly SnapLayoutService _service;
        private readonly Func<string> _getStatusMessage;

        /// <param name="service">Service instance dari SnapLayoutPlugin.</param>
        /// <param name="getStatusMessage">Callback untuk dapatkan status message tambahan.</param>
        public SnapCommands(SnapLayoutService service, Func<string>? getStatusMessage = null)
        {
            _service = service;
            _getStatusMessage = getStatusMessage ?? (() => "");
        }

        public bool CanHandle(string command)
        {
            var low = command.ToLower().Trim();
            return low.StartsWith("!snap");
        }

        public async Task Execute(string command, TerminalTab activeTab,
            Action<TerminalTab, string, string> appendToTab)
        {
            var parts = command.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var action = parts.Length > 1 ? parts[1].ToLower() : "toggle";

            switch (action)
            {
                case "toggle":
                case "on":
                case "enable":
                    if (!_service.IsActive)
                    {
                        // Service start — butuh Window reference
                        // Dari shell, kita hanya bisa toggle jika service sudah active
                        appendToTab(activeTab, "Snap Layout", "[!snap] Service belum active. Aktifkan dari plugin panel.\n");
                    }
                    else
                    {
                        appendToTab(activeTab, "Snap Layout", "[!snap] Snap Layout sudah active.\n");
                    }
                    break;

                case "off":
                case "disable":
                    if (_service.IsActive)
                    {
                        _service.Stop();
                        appendToTab(activeTab, "Snap Layout", "[!snap] Snap Layout dimatikan.\n");
                    }
                    else
                    {
                        appendToTab(activeTab, "Snap Layout", "[!snap] Snap Layout sudah nonaktif.\n");
                    }
                    break;

                case "status":
                    var presetName = _service.ActivePreset.ToString();
                    string status = _service.IsActive ? "🟢 Active" : "🔴 Inactive";
                    string extra = _getStatusMessage();
                    appendToTab(activeTab, "Snap Layout",
                        $"[!snap] Status: {status}\n" +
                        $"        Layout: {presetName}\n" +
                        $"        Drag: {(_service.DragModeEnabled ? "✅" : "❌")}\n" +
                        $"        Keybind: {(_service.KeybindMode ? "✅" : "❌")}\n" +
                        extra + "\n");
                    break;

                default:
                    // Coba parse sebagai preset
                    var preset = SnapLayoutHelper.ParsePreset(action);
                    if (preset != _service.ActivePreset || parts.Length <= 2)
                    {
                        _service.ActivePreset = preset;
                        _service.DragPreset = preset;
                        appendToTab(activeTab, "Snap Layout",
                            $"[!snap] Layout diubah ke: {preset}\n");
                    }
                    else
                    {
                        appendToTab(activeTab, "Snap Layout",
                            $"[!snap] Layout sudah: {preset}\n");
                    }
                    break;
            }

            await Task.CompletedTask;
        }
    }
}
