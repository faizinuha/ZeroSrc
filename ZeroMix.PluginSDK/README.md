# ZeroMix.PluginSDK

SDK for building plugins for [ZeroMix](https://github.com/faizinuha/ZeroMix) — Smart Desktop Launcher & System Utilities.

## Install

```bash
dotnet add package ZeroMix.PluginSDK
```

## Usage

```csharp
using ZeroMix.PluginSDK;

public class MyPlugin : IZeroMixPlugin
{
    public string Name => "My Plugin";
    public string Version => "1.0.0";
    public string Description => "Does something cool";

    public void OnLoad(IZeroMixHost host)
    {
        host.SetStatus("My Plugin loaded!");
        host.Dispatch(() => {
            // UI operations here
        });
    }

    public void OnUnload() { }
}
```

## Available APIs via `IZeroMixHost`

| Method | Description |
|--------|-------------|
| `Dispatch(action)` | Run action on UI thread |
| `SetStatus(text)` | Set ZeroMix status bar text |
| `GetCpuUsage()` | Get current CPU usage (0-100) |
| `GetRamUsage()` | Get current RAM usage (0-100) |

## Lua Plugins

ZeroMix also supports Lua plugins. See [PLUGIN_GUIDE.md](https://github.com/faizinuha/ZeroMix/blob/main/Docs/PLUGIN_GUIDE.md) for details.
