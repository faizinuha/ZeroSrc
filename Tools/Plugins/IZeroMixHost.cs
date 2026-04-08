using ZeroMix.PluginSDK;

namespace ZeroMix.Plugins;

/// <summary>
/// ZeroMix-specific host interface.
/// Extend IPluginHost dengan fitur khusus ZeroMix.
/// MainWindow mengimplementasikan interface ini.
/// </summary>
public interface IZeroMixHost : IPluginHost, ISystemMonitor, IStatusService
{
    // IPluginHost  : HostName, HostVersion, Dispatch, Log, GetService<T>
    // ISystemMonitor : GetCpuUsage, GetRamUsage, GetDiskUsage
    // IStatusService : SetStatus, ShowNotification
}
