namespace ZeroMix.PluginSDK;

/// <summary>
/// Service untuk monitoring sistem (CPU, RAM, Disk).
/// Host yang mendukung ini akan return instance via IPluginHost.GetService&lt;ISystemMonitor&gt;()
/// </summary>
public interface ISystemMonitor
{
    double GetCpuUsage();
    double GetRamUsage();
    double GetDiskUsage();
}

/// <summary>
/// Service untuk menampilkan notifikasi/status di UI host.
/// </summary>
public interface IStatusService
{

    void SetStatus(string text);
    void ShowNotification(string title, string message);
}

/// <summary>
/// Service untuk akses file dan storage plugin.
/// </summary>
public interface IStorageService
{
    void Save(string key, string json);
    string Load(string key);
    string PluginDirectory { get; }
}
