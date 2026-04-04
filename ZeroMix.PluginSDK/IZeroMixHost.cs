namespace ZeroMix.PluginSDK;

/// <summary>
/// Interface yang menghubungkan plugin dengan ZeroMix host application.
/// </summary>
public interface IZeroMixHost
{
    /// <summary>Jalankan action di UI thread</summary>
    void Dispatch(Action action);

    /// <summary>Tampilkan teks di status bar ZeroMix</summary>
    void SetStatus(string text);

    /// <summary>Ambil nilai CPU usage saat ini (0-100)</summary>
    double GetCpuUsage();

    /// <summary>Ambil nilai RAM usage saat ini (0-100)</summary>
    double GetRamUsage();
}
