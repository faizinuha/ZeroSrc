using System;
using System.Threading.Tasks;

namespace ZeroMix.ZeroShell.Commands
{
    /// <summary>
    /// Handler untuk core shell commands: cls, clear, !help, ?, !clock
    /// </summary>
    public class CoreCommands : IShellCommand
    {
        private Action<TerminalTab>? _printHeader;
        private Action<string, string, string>? _toggleClock;

        public CoreCommands(Action<TerminalTab> printHeader, Action<string, string, string> toggleClock)
        {
            _printHeader = printHeader;
            _toggleClock = toggleClock;
        }

        public bool CanHandle(string command)
        {
            var low = command.ToLower().Trim();
            return low == "cls" || low == "clear" || low == "!help" || low == "?" || low == "!clock";
        }

        public async Task Execute(string command, TerminalTab activeTab, Action<TerminalTab, string, string> appendToTab)
        {
            var low = command.ToLower().Trim();

            if (low == "cls" || low == "clear")
            {
                if (activeTab.Output != null)
                {
                    activeTab.Output.Inlines.Clear();
                    _printHeader?.Invoke(activeTab);
                }
                return;
            }

            if (low == "!help" || low == "?")
            {
                appendToTab(activeTab, "\n", "#CCCCCC");
                appendToTab(activeTab, "  [ ZERO MIX SHELL HELP ]\n\n", "#00D4FF");
                
                appendToTab(activeTab, "  ✨ Pilih aksi atau ketik perintah:\n\n", "#FFFFDA6B");
                
                appendToTab(activeTab, "  [ 💻 SISTEM ]\n", "#FFFFDA6B");
                appendToTab(activeTab, "  !task      Real-time System Monitor 📊\n", "#FF27C93F");
                appendToTab(activeTab, "  !sys       Info Detail Sistem\n", "#FF27C93F");
                appendToTab(activeTab, "  !settings  Buka Panel Pengaturan ⚙️\n", "#FF27C93F");
                appendToTab(activeTab, "  cls        Bersihkan Terminal\n", "#FF27C93F");
                appendToTab(activeTab, "  !wifi      Lihat Password WiFi\n", "#FF27C93F");
                appendToTab(activeTab, "  !ip        Lihat Alamat IP\n", "#FF27C93F");
                appendToTab(activeTab, "  !battery   Status Baterai\n", "#FF27C93F");
                appendToTab(activeTab, "  !disk      Info Disk\n", "#FF27C93F");
                appendToTab(activeTab, "  !apps      List Aplikasi\n", "#FF27C93F");
                appendToTab(activeTab, "  !startup   List Startup Items\n", "#FF27C93F");
                
                appendToTab(activeTab, "\n  [ 🎨 VISUAL ]\n", "#FFFFDA6B");
                appendToTab(activeTab, "  !font      Ganti Font (Interaktif)\n", "#FFCC6BFF");
                appendToTab(activeTab, "  !layout    Ganti Layout (Interaktif)\n", "#FFCC6BFF");
                appendToTab(activeTab, "  !alias     Custom Command Alias\n", "#FFCC6BFF");
                appendToTab(activeTab, "  !unalias   Hapus Alias\n", "#FFCC6BFF");

                appendToTab(activeTab, "\n  [ 🛠 TOOLS ]\n", "#FFFFDA6B");
                appendToTab(activeTab, "  !install   Install Framework (React/Laravel)\n", "#FFFF9F43");

                appendToTab(activeTab, "\n  [ 📑 TABS ]\n", "#FFFFDA6B");
                appendToTab(activeTab, "  !tab       Buka Tab Baru\n", "#FFFF9F43");
                appendToTab(activeTab, "  !close     Tutup Tab Aktif\n", "#FFFF9F43");
                appendToTab(activeTab, "  !exit      Keluar Terminal\n", "#FFFF6B6B");

                appendToTab(activeTab, "\n  [ 🌌 ZERO SHELL CORE ]\n", "#FFFFDA6B");
                appendToTab(activeTab, "  !wdm      Open WDM Window\n", "#FF6BDDFF");
                
                appendToTab(activeTab, "\n  💬 Tips: Gunakan Tanda Panah ↑ ↓ buat milih font/layout.\n\n", "#888888");
                return;
            }

            if (low == "!clock")
            {
                _toggleClock?.Invoke("toggle", "", "");
                appendToTab(activeTab, $"\n  🕒 Clock Tile toggled.\n\n", "#FFCC6BFF");
                return;
            }

            await Task.CompletedTask;
        }
    }
}
