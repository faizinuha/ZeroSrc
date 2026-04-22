using System.Collections.Generic;

namespace ZeroMix.ZeroShell
{
    public enum WdmStyle
    {
        None,
        AcrylicDark,
        AcrylicLight,
        BlurOnly,
        GlassClear,
        FullTransparent,
        FloatingMacOS   // transparent + DWM border glow
    }

    public class WdmEntry
    {
        public WdmStyle Style { get; set; } = WdmStyle.None;
        public int Alpha { get; set; } = 0xDD;
        public string ColorHex { get; set; } = "#000000";
        public bool AutoApply { get; set; } = true;
    }

    public class WdmState
    {
        public Dictionary<string, WdmEntry> Entries { get; set; } = new();
    }

    public static class WdmCategories
    {
        // ClassMap: key = category, value = window class names to match
        public static readonly Dictionary<string, string[]> ClassMap = new()
        {
            ["Taskbar"]      = new[] { "Shell_TrayWnd", "Shell_SecondaryTrayWnd" },
            // Notification & StartMenu use process-aware apply (see ProcessMap)
            ["Notification"] = new[] { "Windows.UI.Core.CoreWindow" },
            ["Explorer"]     = new[] { "CabinetWClass", "ExplorerWClass" },
            ["StartMenu"]    = new[] { "Windows.UI.Core.CoreWindow" },
            ["Desktop"]      = new[] { "Progman", "WorkerW" },
        };

        // ProcessMap: which process owns each category (for process-filtered apply)
        public static readonly Dictionary<string, string> ProcessMap = new()
        {
            ["Taskbar"]      = "explorer",
            ["Notification"] = "ShellExperienceHost",
            ["Explorer"]     = "explorer",
            ["StartMenu"]    = "StartMenuExperienceHost",
            ["Desktop"]      = "explorer",
        };

        public static readonly Dictionary<string, string> DisplayNames = new()
        {
            ["Taskbar"]      = "Taskbar",
            ["Notification"] = "Notification Panel",
            ["Explorer"]     = "File Explorer",
            ["StartMenu"]    = "Start Menu",
            ["Desktop"]      = "Desktop",
            ["Layouts"]      = "Layouts",
        };
    }

    public static class WdmPresets
    {
        public static WdmState MacOSDock => new WdmState
        {
            Entries = new()
            {
                ["Shell_TrayWnd"]          = new() { Style = WdmStyle.FloatingMacOS,  Alpha = 0x00, ColorHex = "#000000", AutoApply = true },
                ["Shell_SecondaryTrayWnd"] = new() { Style = WdmStyle.FloatingMacOS,  Alpha = 0x00, ColorHex = "#000000", AutoApply = true },
                ["CabinetWClass"]          = new() { Style = WdmStyle.AcrylicDark,    Alpha = 0xBB, ColorHex = "#000000", AutoApply = true },
                ["ExplorerWClass"]         = new() { Style = WdmStyle.AcrylicDark,    Alpha = 0xBB, ColorHex = "#000000", AutoApply = true },
                // Win10 Notification via ShellExperienceHost
                ["__Notification__"]       = new() { Style = WdmStyle.GlassClear,     Alpha = 0x44, ColorHex = "#000000", AutoApply = true },
                // Win10 Start Menu via StartMenuExperienceHost
                ["__StartMenu__"]          = new() { Style = WdmStyle.AcrylicDark,    Alpha = 0x88, ColorHex = "#000000", AutoApply = true },
            }
        };

        public static WdmState MinimalDark => new WdmState
        {
            Entries = new()
            {
                ["Shell_TrayWnd"]          = new() { Style = WdmStyle.FullTransparent, Alpha = 0x00, ColorHex = "#000000", AutoApply = true },
                ["Shell_SecondaryTrayWnd"] = new() { Style = WdmStyle.FullTransparent, Alpha = 0x00, ColorHex = "#000000", AutoApply = true },
                ["CabinetWClass"]          = new() { Style = WdmStyle.AcrylicDark,     Alpha = 0xCC, ColorHex = "#000000", AutoApply = true },
                ["__Notification__"]       = new() { Style = WdmStyle.AcrylicDark,     Alpha = 0xCC, ColorHex = "#000000", AutoApply = true },
            }
        };

        public static WdmState Cyberpunk => new WdmState
        {
            Entries = new()
            {
                ["Shell_TrayWnd"]          = new() { Style = WdmStyle.AcrylicDark,  Alpha = 0xCC, ColorHex = "#1A0825", AutoApply = true },
                ["Shell_SecondaryTrayWnd"] = new() { Style = WdmStyle.AcrylicDark,  Alpha = 0xCC, ColorHex = "#1A0825", AutoApply = true },
                ["CabinetWClass"]          = new() { Style = WdmStyle.AcrylicDark,  Alpha = 0xBB, ColorHex = "#100520", AutoApply = true },
                ["__Notification__"]       = new() { Style = WdmStyle.AcrylicLight, Alpha = 0x88, ColorHex = "#1A0825", AutoApply = true },
                ["__StartMenu__"]          = new() { Style = WdmStyle.AcrylicDark,  Alpha = 0xAA, ColorHex = "#1A0825", AutoApply = true },
            }
        };

        public static WdmState ClassicWindows => new WdmState
        {
            Entries = new()
            {
                ["Shell_TrayWnd"]          = new() { Style = WdmStyle.AcrylicDark,  Alpha = 0xDD, ColorHex = "#000000", AutoApply = true },
                ["Shell_SecondaryTrayWnd"] = new() { Style = WdmStyle.AcrylicDark,  Alpha = 0xDD, ColorHex = "#000000", AutoApply = true },
                ["CabinetWClass"]          = new() { Style = WdmStyle.AcrylicLight, Alpha = 0x99, ColorHex = "#1A1A2E", AutoApply = true },
                ["__Notification__"]       = new() { Style = WdmStyle.AcrylicDark,  Alpha = 0xDD, ColorHex = "#000000", AutoApply = true },
            }
        };
    }
}
