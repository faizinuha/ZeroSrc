<div align="center">

<img src="Assets/zeromix-high-resolution-logo-transparent.png" alt="ZeroMix Logo" width="160"/>

# ZeroMix

[🇺🇸 English](README.md) | [🇮🇩 Indonesia](README.id.md)

**A modern Windows utility suite for power users who care about performance and aesthetics.**

[![GitHub Release](https://img.shields.io/github/v/release/faizinuha/ZeroMix?color=00D9FF&style=for-the-badge)](https://github.com/faizinuha/ZeroMix/releases)
[![License](https://img.shields.io/badge/license-MIT-blue.svg?style=for-the-badge)](LICENSE.txt)
[![.NET Version](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/badge/NuGet-ZeroMix.PluginSDK-004880?style=for-the-badge&logo=nuget)](https://www.nuget.org/packages/ZeroMix.PluginSDK)
[![Support](https://img.shields.io/badge/SUPPORT-TRAKTEER-red?style=for-the-badge)](https://trakteer.id/MyCici)

[📥 Download](https://github.com/faizinuha/ZeroMix/releases) · [🎥 Demo](https://www.youtube.com/watch?v=y9yz7ZPh_Bo) · [📖 Docs](https://github.com/faizinuha/ZeroMix/wiki) · [🧩 Plugin Guide](Docs/PLUGIN_GUIDE.md) · [💬 Community](https://github.com/faizinuha/ZeroMix/discussions)

</div>

---

ZeroMix is a self-contained Windows desktop enhancement suite built on .NET 9 and WPF. It combines system monitoring, AI-powered tools, Live2D virtual assistants, screen recording, and a Lua plugin system into a single lightweight application.

---

## Features

| Feature | Description |
| :--- | :--- |
| **🧩 Lua Plugin System** | Extend ZeroMix with custom Lua scripts — no recompile needed |
| **🎥 ZeroRecord** | Screen recorder with hardware-accelerated encoding (NVENC / QSV / AMF) |
| **🌍 Nexus Translate** | Real-time translation with clipboard integration and game mode support |
| **🐱 Cat Gatekeeper** | Screen time enforcer — a cat takes over your screen when it's time to rest |
| **🌙 Sleep Mode** | Always-On Display with idle detection, hotkeys, and custom backgrounds |
| **🤖 Virtual Assistant** | Live2D AI companions (Frieren, Fern, Huohuo) powered by OpenRouter API |
| **📊 System Monitor** | Real-time CPU, RAM, and disk metrics on your dashboard |
| **🎨 Ghost Taskbar** | Transparent taskbar with acrylic and Mica effects |
| **🎬 Video Wallpaper** | Animated desktop wallpapers with FFmpeg-powered playback |
| **💻 ZeroShell** | Integrated terminal with Powerline prompt, tab support, and shell commands |
| **🔍 Search Overlay** | Fast app and file launcher (`Alt + Space`) with AI query support |
| **🕒 Desktop Widgets** | Clock, weather, and system stats embedded directly on your desktop |
| **🔧 Plugin Creator** | Built-in tool to scaffold and manage Lua plugins |

---

## Plugin System

ZeroMix ships with a Lua-powered plugin ecosystem. Plugins run in an isolated sandbox and communicate with the host via a typed API bridge.

### Built-in Plugins

| Plugin | Description |
| :--- | :--- |
| `zeromix.Battery` | Battery level monitoring with custom notifications |
| `zeromix.Translate` | Translation engine with keyboard and clipboard hooks |
| `zeromix.weather` | Real-time weather via Open-Meteo API |
| `zeromix.CatGatekeeper` | Screen time tracker with fullscreen cat overlay |

### Creating Plugins

1. Open the Plugin Creator from the ZeroMix dashboard
2. Write your logic in Lua inside a `Plugins/user.*` folder
3. Plugins hot-reload automatically — no restart required
4. Access system APIs via the `ZeroMixLuaApi` bridge

📖 **[Full Plugin Guide →](Docs/PLUGIN_GUIDE.md)**

### C# Plugin SDK

```bash
dotnet add package ZeroMix.PluginSDK
```

---

## Demo

**ZeroRecord — Screen Recording**
<div align="center">
  <video src="https://github.com/faizinuha/ZeroMix/blob/ProyekTil/src/Web/assets/ZeroRecord_20260122_133325.mp4?raw=true" width="1000" autoplay muted loop controls></video>
</div>

---

**ZeroShell — Integrated Terminal**
<div align="center">
  <video src="https://github.com/faizinuha/ZeroMix/blob/ProyekTil/src/Web/assets/ZeroRecord_20260411_211121.mp4?raw=true" width="1000" autoplay muted loop controls></video>
</div>

---

**Select Area Recording**
<div align="center">
  <video src="https://github.com/faizinuha/ZeroMix/blob/ProyekTil/src/Web/assets/ZeroRecord_20260411_211722.mp4?raw=true" width="1000" autoplay muted loop controls></video>
</div>

---

## Nexus Translator

- **Pure Magic Mode** — Translates as you type, injected directly into the active window
- **Anti-Collision Engine** — Prevents text corruption during injection
- **Triple-API Backend** — Automatic failover between MyMemory and Google Translate
- **Game Mode** — Clipboard-based translation compatible with DirectInput games
- **Unicode Bypass** — Works in Notepad, Chrome, Discord, VS Code, and more

---

## Installation

| Method | Description |
| :--- | :--- |
| **⭐ Bootstrap Installer** | ~10 MB download, fetches the latest release automatically |
| **📦 Setup EXE** | Full offline installer with all dependencies |
| **🚀 Portable ZIP** | Extract and run — no installation required |

Download from **[GitHub Releases →](https://github.com/faizinuha/ZeroMix/releases)**

### Getting Started

1. Run the installer and follow the setup wizard
2. ZeroMix starts minimized to the system tray (⚡ icon)
3. Open the dashboard with `Ctrl + Q` or click the tray icon
4. Admin rights are only required during initial install for Start Menu shortcuts

---

## Keyboard Shortcuts

| Shortcut | Action |
| :--- | :--- |
| `Ctrl + Q` | Toggle main dashboard |
| `Alt + Space` | Open Search Overlay |
| `Alt + S` | Toggle Sleep Mode |
| `Ctrl + R` | Force refresh system metrics |
| `Esc` | Close active overlay or window |

**Ask AI via Search Overlay:**

| Query | Action |
| :--- | :--- |
| `Carikan saya sepatu murah` | Opens a search for cheap shoes |
| `Beli baju di Tokopedia` | Opens Tokopedia with the query |
| `Handphone murah di Shopee` | Opens Shopee with the query |

---

## Tech Stack

| Layer | Technology |
| :--- | :--- |
| UI Framework | WPF (.NET 9) + WPF-UI (Fluent Design) |
| Scripting | MoonSharp (Lua 5.2 interpreter) |
| Graphics | Vortice.Windows (DirectX 11/12 bindings) |
| Web Rendering | Microsoft.Web.WebView2 |
| Video Processing | FFmpeg |
| AI Backend | OpenRouter API (Gemini 2.0 Flash Thinking) |
| Packaging | Inno Setup |
| Distribution | GitHub Releases + Bootstrap Installer |

---

## Contributing

- **Found a bug?** → [Open an Issue](https://github.com/faizinuha/ZeroMix/issues)
- **Have a feature idea?** → [Start a Discussion](https://github.com/faizinuha/ZeroMix/discussions)
- **Want to contribute code?** → [Contributing Guide](CONTRIBUTING.md)

---

## Credits & Acknowledgments

### Atmospheric Video Assets

Background videos used in ZeroMix are sourced from Pixabay under their free license:

- **Rainy City at Night** by [Hans](https://pixabay.com/id/users/hans-2/) — [Pixabay](https://pixabay.com/)
- **Nature & Garden Ambience** by [Nicky ❤️🌿🐞🌿❤️](https://pixabay.com/id/users/nickype-10327513/) — [Pixabay](https://pixabay.com/)
- **Cinematic Scenery 1** by [Andreas](https://pixabay.com/id/users/adege-4994132/?content=257240) — [Pixabay](https://pixabay.com/)
- **Cinematic Scenery 2** by [Andreas](https://pixabay.com/id/users/adege-4994132/?content=230317) — [Pixabay](https://pixabay.com/)

### 🐱 Cat Gatekeeper

- **Original cat animation videos** by [@konekone2026 (ZOKUZOKU)](https://x.com/konekone2026) — Cat Gatekeeper Chrome Extension
- **Extraction & WPF adaptation** by Zaki

### 🎭 Live2D Models

- **Frieren & Fern** by [kyokiStudio](https://kyoki.booth.pm/) on Booth.pm
- **Huohuo** by [bailyovo](https://bailyovo.booth.pm/) on Booth.pm
  - Copyright belongs to miHoYo. This model is for Honkai: Star Rail fan creation only. Not for commercial use, political content, or profit-oriented streaming. Secondary distribution is prohibited.

---

> [!IMPORTANT]
> **Removal Policy:** If any model or asset is not permitted for use in this application, contact us at **Rozakadm@gmail.com** and we will remove it promptly.

> [!NOTE]
> Download models only from the official sources linked above. Do not use these models for commercial purposes or redistribute them without permission.

> [!NOTE]
> Use of included models is entirely the user's responsibility. The application developer does not provide any commercial license for third-party models.

> [!WARNING]
> The application developer does not act as licensor for any included third-party models. Violations of the original creator's terms are the user's responsibility.

---

## Support

<div align="center">

[![Sociabuzz](https://img.shields.io/badge/sociabuzz-Support_The_Dev-EE4B2B?style=for-the-badge&logoColor=white)](https://sociabuzz.com/zuax)

## Star History

<a href="https://www.star-history.com/?repos=faizinuha%2FZeroMix&type=date&legend=top-left">
 <picture>
   <source media="(prefers-color-scheme: dark)" srcset="https://api.star-history.com/chart?repos=faizinuha/ZeroMix&type=date&theme=dark&legend=top-left" />
   <source media="(prefers-color-scheme: light)" srcset="https://api.star-history.com/chart?repos=faizinuha/ZeroMix&type=date&legend=top-left" />
   <img alt="Star History Chart" src="https://api.star-history.com/chart?repos=faizinuha/ZeroMix&type=date&legend=top-left" />
 </picture>
</a>

Made with ❤️ by **Faizinuha** and the community. **ZeroMix © 2026**
</div>
