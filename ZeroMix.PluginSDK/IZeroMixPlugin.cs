namespace ZeroMix.PluginSDK;

/// <summary>
/// Interface utama untuk semua plugin.
/// Kompatibel dengan ZeroMix dan aplikasi host lain yang mengimplementasi IPluginHost.
/// </summary>
public interface IZeroMixPlugin
{
    /// <summary>Nama plugin yang tampil di UI host</summary>
    string Name { get; }

    /// <summary>Versi plugin (format: x.y.z)</summary>
    string Version { get; }

    /// <summary>Deskripsi singkat plugin</summary>
    string Description { get; }

    /// <summary>Dipanggil saat plugin diaktifkan oleh host</summary>
    void OnLoad(IPluginHost host);

    /// <summary>Dipanggil saat plugin dinonaktifkan</summary>
    void OnUnload();
}
