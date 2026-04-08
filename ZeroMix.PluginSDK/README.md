# ZeroMix.PluginSDK

[![NuGet](https://img.shields.io/nuget/v/ZeroMix.PluginSDK)](https://www.nuget.org/packages/ZeroMix.PluginSDK)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/faizinuha/ZeroMix/blob/main/LICENSE.txt)
[![.NET](https://img.shields.io/badge/.NET-9.0-purple)](https://dotnet.microsoft.com)

Universal plugin SDK untuk .NET — bisa dipakai untuk membuat plugin di **ZeroMix** maupun **aplikasi .NET lain** yang mengimplementasi `IPluginHost`.

---

## ⚠️ Penting: Kompatibilitas Aplikasi Host

SDK ini berbasis **.NET 9** dan hanya kompatibel dengan aplikasi yang dibangun di atas .NET.

| Aplikasi | Kompatibel? | Catatan |
|----------|-------------|---------|
| **ZeroMix** | ✅ | Native support, langsung jalan |
| Aplikasi WPF (.NET) | ✅ | Implementasi `IPluginHost` sendiri |
| Aplikasi WinForms (.NET) | ✅ | Implementasi `IPluginHost` sendiri |
| Aplikasi Console (.NET) | ✅ | Implementasi `IPluginHost` sendiri |
| **OBS Studio** | ❌ | OBS pakai C/C++ native, bukan .NET |
| **Photoshop** | ❌ | Plugin Photoshop pakai C++ / UXP (JS) |
| **VS Code** | ❌ | Extension VS Code pakai TypeScript/JS |
| Game Engine (Unity) | ⚠️ | Unity pakai .NET tapi sistem plugin berbeda |

> **Singkatnya:** SDK ini cocok untuk aplikasi desktop Windows yang dibangun dengan .NET/C#.

---

## 📦 Install

```bash
dotnet add package ZeroMix.PluginSDK
```

---

## 🚀 Cara Membuat Plugin untuk ZeroMix

### Step 1 — Buat project baru

```bash
dotnet new classlib -n MyZeroMixPlugin -f net9.0-windows
cd MyZeroMixPlugin
dotnet add package ZeroMix.PluginSDK
```

### Step 2 — Implementasi plugin

```csharp
using ZeroMix.PluginSDK;

public class MyPlugin : IZeroMixPlugin
{
    public string Name        => "CPU Monitor Plugin";
    public string Version     => "1.0.0";
    public string Description => "Tampilkan CPU usage di status bar ZeroMix";

    private IPluginHost? _host;

    public void OnLoad(IPluginHost host)
    {
        _host = host;
        host.Log($"Plugin dimuat di {host.HostName} v{host.HostVersion}");

        // Akses system monitor (ZeroMix mendukung ini)
        var monitor = host.GetService<ISystemMonitor>();
        if (monitor != null)
        {
            double cpu = monitor.GetCpuUsage();
            double ram = monitor.GetRamUsage();
            host.Log($"CPU: {cpu}% | RAM: {ram}%");
        }

        // Tampilkan status
        var status = host.GetService<IStatusService>();
        status?.SetStatus("CPU Monitor Plugin aktif!");

        // Operasi UI — selalu pakai Dispatch
        host.Dispatch(() =>
        {
            // Contoh: buat window kustom
            var win = new System.Windows.Window
            {
                Title = "CPU Monitor",
                Width = 300,
                Height = 200
            };
            win.Show();
        });
    }

    public void OnUnload()
    {
        _host?.Log("Plugin dinonaktifkan.");
        _host = null;
    }
}
```

### Step 3 — Build plugin

```bash
dotnet build -c Release
```

### Step 4 — Deploy ke ZeroMix

Copy hasil build ke folder plugins ZeroMix:

```
C:\Program Files\ZeroMix\Tools\Plugins\MyZeroMixPlugin\
    MyZeroMixPlugin.dll
    MyZeroMixPlugin.deps.json
```

### Step 5 — Aktifkan di ZeroMix

Buka ZeroMix → sidebar **Plugins** → plugin kamu muncul otomatis → klik **Enable**.

---

## 🔌 Cara Mengintegrasikan ke Aplikasi .NET Kamu (Jadi Host)

Kalau kamu punya aplikasi .NET sendiri dan ingin mendukung plugin dari SDK ini:

### Step 1 — Install SDK di project kamu

```bash
dotnet add package ZeroMix.PluginSDK
```

### Step 2 — Implementasi `IPluginHost`

```csharp
using ZeroMix.PluginSDK;

// Contoh: aplikasi WPF sederhana sebagai host
public class MyAppHost : IPluginHost
{
    public string HostName    => "MyApp";
    public string HostVersion => "1.0.0";

    // Jalankan di UI thread (WPF)
    public void Dispatch(Action action) =>
        System.Windows.Application.Current.Dispatcher.Invoke(action);

    // Logging
    public void Log(string message) =>
        System.Diagnostics.Debug.WriteLine($"[Plugin] {message}");

    // Expose service yang kamu support
    public T? GetService<T>() where T : class
    {
        if (typeof(T) == typeof(IStatusService))   return new MyStatusService() as T;
        if (typeof(T) == typeof(ISystemMonitor))   return new MySystemMonitor() as T;
        if (typeof(T) == typeof(IStorageService))  return new MyStorageService() as T;
        return null;
    }
}

// Implementasi service status
public class MyStatusService : IStatusService
{
    public void SetStatus(string text) =>
        System.Diagnostics.Debug.WriteLine($"[Status] {text}");

    public void ShowNotification(string title, string message) =>
        System.Windows.MessageBox.Show(message, title);
}
```

### Step 3 — Load dan jalankan plugin

```csharp
var host   = new MyAppHost();
var plugin = new SomePlugin(); // plugin yang implement IZeroMixPlugin
plugin.OnLoad(host);

// Saat app tutup atau plugin di-disable:
plugin.OnUnload();
```

---

## 🌙 Lua Plugin (Khusus ZeroMix)

ZeroMix juga mendukung plugin berbasis **Lua** — tidak perlu compile, cukup buat file `script.lua`.

### Struktur folder Lua plugin

```
Tools/Plugins/user.pub.NamaPlugin/
    script.lua
    config.json   (opsional)
```

### Contoh `script.lua`

```lua
-- Dipanggil saat plugin diaktifkan
function OnLoad()
    CreateUI("My Lua Plugin", 400, 300)
    AddLabel("Halo dari Lua!")
    AddButton("Klik Aku", "OnButtonClick")
    Notify("Plugin", "Lua plugin aktif!")
end

-- Dipanggil setiap detik
function OnUpdate()
    local cpu = ZeroMix:GetCpuUsage()
    Log("CPU: " .. cpu .. "%")
end

-- Callback tombol
function OnButtonClick()
    Notify("Info", "Tombol diklik!")
end
```

### Lua API yang tersedia

| Fungsi | Keterangan |
|--------|------------|
| `CreateUI(title, w, h)` | Buat window plugin |
| `AddLabel(text)` | Tambah teks |
| `AddInput(id, placeholder)` | Tambah input field |
| `AddButton(text, callback)` | Tambah tombol |
| `GetInput(id)` | Ambil nilai input |
| `Notify(title, msg)` | Tampilkan dialog |
| `Log(msg)` | Tulis ke debug log |
| `SaveConfig(key, json)` | Simpan data |
| `LoadConfig(key)` | Baca data |
| `ZeroMix:GetCpuUsage()` | CPU usage |
| `ZeroMix:GetRamUsage()` | RAM usage |

---

## 📋 API Reference (C#)

### `IZeroMixPlugin`

| Member | Keterangan |
|--------|------------|
| `Name` | Nama plugin |
| `Version` | Versi (`x.y.z`) |
| `Description` | Deskripsi singkat |
| `OnLoad(host)` | Dipanggil saat aktif |
| `OnUnload()` | Dipanggil saat nonaktif |

### `IPluginHost`

| Member | Keterangan |
|--------|------------|
| `HostName` | Nama aplikasi host |
| `HostVersion` | Versi host |
| `Dispatch(action)` | Jalankan di UI thread |
| `Log(message)` | Tulis log |
| `GetService<T>()` | Ambil service opsional |

### Services via `GetService<T>()`

| Interface | Method | Keterangan |
|-----------|--------|------------|
| `ISystemMonitor` | `GetCpuUsage()` | CPU 0–100 |
| | `GetRamUsage()` | RAM 0–100 |
| | `GetDiskUsage()` | Disk 0–100 |
| `IStatusService` | `SetStatus(text)` | Status bar |
| | `ShowNotification(title, msg)` | Dialog notifikasi |
| `IStorageService` | `Save(key, json)` | Simpan data |
| | `Load(key)` | Baca data |
| | `PluginDirectory` | Path folder plugin |

---

## 🔗 Links

- [ZeroMix Repository](https://github.com/faizinuha/ZeroMix)
- [NuGet Package](https://www.nuget.org/packages/ZeroMix.PluginSDK)
- [Plugin Guide Lengkap](https://github.com/faizinuha/ZeroMix/blob/main/Docs/PLUGIN_GUIDE.md)
- [Issues / Feedback](https://github.com/faizinuha/ZeroMix/issues)

---

**Maintained by ZeroMix Team** | License: MIT
