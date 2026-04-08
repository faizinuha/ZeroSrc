namespace ZeroMix.PluginSDK;

/// <summary>
/// Generic host interface — bisa diimplementasi oleh aplikasi apapun.
/// Gunakan GetService&lt;T&gt;() untuk mengakses fitur spesifik host.
/// </summary>
public interface IPluginHost
{
    /// <summary>Nama aplikasi host (contoh: "ZeroMix", "OBS", dll)</summary>
    string HostName { get; }

    /// <summary>Versi aplikasi host</summary>
    string HostVersion { get; }

    /// <summary>Jalankan action di UI thread</summary>
    void Dispatch(Action action);

    /// <summary>Tulis log dari plugin</summary>
    void Log(string message);

    /// <summary>
    /// Ambil service spesifik dari host.
    /// Return null jika host tidak mendukung service tersebut.
    /// </summary>
    T? GetService<T>() where T : class;
}
