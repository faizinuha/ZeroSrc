<div align="center">

<img src="Assets/zeromix-high-resolution-logo-transparent.png" alt="ZeroMix Logo" width="160"/>

# 🎯 ZeroMix

[🇺🇸 English](README.md) | [🇮🇩 Indonesia](README.id.md)

**The Ultimate Windows Suite for Performance & Aesthetics**

[![GitHub Release](https://img.shields.io/github/v/release/faizinuha/ZeroMix?color=00D9FF&style=for-the-badge)](https://github.com/faizinuha/ZeroMix/releases)
[![License](https://img.shields.io/badge/license-MIT-blue.svg?style=for-the-badge)](LICENSE.txt)
[![.NET Version](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/badge/NuGet-ZeroMix.PluginSDK-004880?style=for-the-badge&logo=nuget)](https://www.nuget.org/packages/ZeroMix.PluginSDK)
[![Support](https://img.shields.io/badge/SUPPORT-TRAKTEER-red?style=for-the-badge)](https://trakteer.id/MyCici)

[📥 Download](https://github.com/faizinuha/ZeroMix/releases) • [🎥 Demo](https://www.youtube.com/watch?v=y9yz7ZPh_Bo) • [📖 Docs](https://github.com/faizinuha/ZeroMix/wiki) • [🧩 Plugin Guide](Docs/PLUGIN_GUIDE.md) • [💬 Community](https://github.com/faizinuha/ZeroMix/discussions)

---

**ZeroMix** is a high-performance, lightweight utility suite designed to transform your Windows experience. From real-time system monitoring to immersive video wallpapers and a **Lua-powered plugin system**, ZeroMix brings pro-level tools into a beautiful, modern interface.

</div>

---

## 🚀 Core Features

| Feature | Description |
| :--- | :--- |
| **🧩 Lua Plugin System** | Create and share plugins using simple Lua scripts |
| **🎥 ZeroRecord** | **(HOT)** Screen recording with dynamic zoom based on cursor movement |
| **🌍 Nexus Translate** | **(v2.0)** Real-time translation with Anti-Crash protection |
| **🌙 Fake Sleep Mode** | Always-On Display with multi-trigger, idle detection & hotkey |
| **🤖 Virtual Assistant** | AI Vision with Live2D characters (Mihoyo, Frieren) via Groq API |
| **📊 System Monitor** | Real-time CPU, RAM, and Disk tracking from your dashboard |
| **🎨 Ghost Taskbar** | Instantly make your taskbar transparent |
| **🎬 Video Wallpaper** | Immersive animated wallpapers with high-performance video engine |
| **💻 ZeroShell** | **(STABLE)** Pro Terminal with Powerline prompt & Arch-style fetch |
| **🔍 Search Overlay** | High-performance search bar (`Alt + Space`) for apps, files, and web |
| **🧹 Memory Optimizer** | Smart background GC collection and working set trimming |
| **🕒 Desktop Widgets** | Clock and other widgets for your desktop |
| **🔧 Plugin Creator** | Built-in tool to create and manage Lua plugins |

---

## 🧩 Lua Plugin System

ZeroMix features a powerful Lua-powered plugin ecosystem — extend functionality without recompiling.

### Built-in Plugins
- **zeromix.Battery** — Real-time battery monitoring
- **zeromix.Translate** — Translation services
- **zeromix.weather** — Weather data integration

### Creating Plugins
1. Use the built-in Plugin Creator tool in ZeroMix
2. Write Lua scripts in `Plugins/user.*` folders
3. Plugins load automatically — no restart needed
4. Full API access via `ZeroMixLuaApi` bridge

📖 **[Full Plugin Guide →](Docs/PLUGIN_GUIDE.md)**

### Plugin SDK (for C# developers)
```bash
dotnet add package ZeroMix.PluginSDK
```

---

## 🎥 ZeroRecord Preview

<div align="center">
  <video src="https://github.com/user-attachments/assets/0a5c1928-32b8-4b32-a391-aefdb5b3d2f5" width="1000" autoplay muted loop></video>
</div>

---

## 🌍 Nexus Translator Core v2.0

- **✨ Pure Magic Mode** — Translates automatically as you type
- **🛡️ Anti-Collision Engine** — Prevents text corruption during injection
- **🚀 Triple-API Backend** — Failover between MyMemory and Google Translate
- **💎 Glassmorphism UI** — Premium interface with live terminal status log
- **⌨️ Unicode Bypass** — Works in Notepad, Chrome, Discord, and more

---

## 📥 Quick Installation

| Method | Description |
| :--- | :--- |
| **⭐ Bootstrap Installer** | Small (~10MB), downloads latest version automatically |
| **📦 Setup EXE** | Full offline installer |
| **🚀 Portable ZIP** | No install needed, just extract and run |

Download from **[GitHub Releases →](https://github.com/faizinuha/ZeroMix/releases)**

### Getting Started
1. Find the ⚡ icon in your system tray after install
2. ZeroMix will prompt to enable "Run on Startup" on first launch
3. Admin is only needed during initial installation for Start Menu shortcuts

---

## ⌨️ Keyboard Shortcuts

| Shortcut | Action |
| :--- | :--- |
| `Alt + S` | Toggle Fake Sleep Mode |
| `Ctrl + Space` | Open Search Overlay |
| `Ctrl + Q` | Toggle Main Dashboard |
| `Ctrl + R` | Force Refresh System Metrics |
| `Esc` | Close Active Overlay/Window |

```
| Key | Ask AI |
| Carikan saya sepatu yang murah -> otomatis buka 
| Carikan saya bajuu yang murah di tokopedia -> otomatis buka Tokopedia
| Carikan saya Handphone yang murah di Shoppe -> otomatis buka Shoppe

```
---

## 🛠️ Built With

- **Frontend** — WPF + Windows Forms hybrid
- **Core** — .NET 9.0 (C#), self-contained
- **Scripting** — MoonSharp (Lua interpreter)
- **Graphics** — Vortice (DirectX bindings)
- **Web** — Microsoft.Web.WebView2
- **Video** — FFmpeg
- **Packaging** — Inno Setup

---

## 🤝 Community & Contribution

- **Found a bug?** → [Open an Issue](https://github.com/faizinuha/ZeroMix/issues)
- **Have an idea?** → [Start a Discussion](https://github.com/faizinuha/ZeroMix/discussions)
- **Want to code?** → [Contributing Guide](CONTRIBUTING.md)

---

## 🎬 Credits & Acknowledgments

The atmospheric experiences in ZeroMix are powered by beautiful visuals from the creative community:

- **Rainy City at Night** by [Hans](https://pixabay.com/id/users/hans-2/) from [Pixabay](https://pixabay.com/)
- **Nature & Garden Ambience** by [Nicky ❤️🌿🐞🌿❤️](https://pixabay.com/id/users/nickype-10327513/) from [Pixabay](https://pixabay.com/)
- **Cinematic Scenery 1** by [Andreas](https://pixabay.com/id/users/adege-4994132/?content=257240) from [Pixabay](https://pixabay.com/)
- **Cinematic Scenery 2** by [Andreas](https://pixabay.com/id/users/adege-4994132/?content=230317) from [Pixabay](https://pixabay.com/)

### 🎭 Live2D Models

- **Frieren & Fern Model** by [kyokiStudio](https://kyoki.booth.pm/) on Booth.pm
- **Huohuo Model** by [bailyovo](https://bailyovo.booth.pm/) on Booth.pm
  - _Precautions_: The copyright belongs to miHoYo. This model is for Honkai: Star Rail fan creation only. Not for political use or profit-oriented live streaming. Secondary distribution is prohibited. Creators are not responsible for violations.

---

> [!IMPORTANT]
> **Removal Policy:** If this model is not permitted for use in the application, please contact us immediately via email: **Rozakadm@gmail.com**. We will immediately remove the model to respect the owner's rights and ensure the comfort of all parties. Thank you.

> [!NOTE]
> If you wish to download the model, please use the **official website we have provided**. Please respect the hard work of the model creator. Do not use this model for **commercial purposes or sell it without permission**. Thank you for your understanding! 🥰

> [!NOTE]
> Use of this model is entirely the user's responsibility.
> The application developer does not provide any commercial license for this model and only passes on the terms of the original creator.

> [!WARNING]
> The application developer does not act as the model's licensor.
> Any use that violates the original creator's terms is the user's responsibility.

---

## 💖 Support the Project

<div align="center">

[![sociabuzz Support](https://img.shields.io/badge/sociabuzz-Support_The_Dev-EE4B2B?style=for-the-badge&logoColor=white)](https://sociabuzz.com/zuax)

## Star History

<a href="https://www.star-history.com/?repos=faizinuha%2FZeroMix&type=date&legend=top-left">
 <picture>
   <source media="(prefers-color-scheme: dark)" srcset="https://api.star-history.com/chart?repos=faizinuha/ZeroMix&type=date&theme=dark&legend=top-left" />
   <source media="(prefers-color-scheme: light)" srcset="https://api.star-history.com/chart?repos=faizinuha/ZeroMix&type=date&legend=top-left" />
   <img alt="Star History Chart" src="https://api.star-history.com/chart?repos=faizinuha/ZeroMix&type=date&legend=top-left" />
 </picture>
</a>

Made with ❤️ by **Faizinuha** and the community.
**ZeroMix © 2026-2026**

</div>
