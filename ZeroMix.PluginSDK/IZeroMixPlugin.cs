namespace ZeroMix.PluginSDK;

/// <summary>
/// Interface utama untuk plugin ZeroMix berbasis C#.
/// Implement interface ini untuk membuat plugin native.
/// </summary>
public interface IZeroMixPlugin
{
    /// <summary>Nama plugin yang tampil di UI ZeroMix</summary>
    string Name { get; }

    /// <summary>Versi plugin</summary>
    string Version { get; }

    /// <summary>Deskripsi singkat plugin</summary>
    string Description { get; }

    /// <summary>Dipanggil saat plugin diaktifkan</summary>
    void OnLoad(IZeroMixHost host);

    /// <summary>Dipanggil saat plugin dinonaktifkan</summary>
    void OnUnload();
}
