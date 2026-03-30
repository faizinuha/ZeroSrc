<div align="center">

<img src="assets/zeromix-high-resolution-logo-transparent.png" alt="ZeroMix Logo" width="160"/>

# 🎯 ZeroMix

**The Ultimate Windows Suite for Performance & Aesthetics**

[![GitHub Release](https://img.shields.io/github/v/release/faizinuha/ZeroMix?color=00D9FF&style=for-the-badge)](https://github.com/faizinuha/ZeroMix/releases)
[![License](https://img.shields.io/badge/license-MIT-blue.svg?style=for-the-badge)](LICENSE.txt)
[![.NET Version](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![Support](https://img.shields.io/badge/SUPPORT-TRAKTEER-red?style=for-the-badge&logo=trakteer)](https://trakteer.id/MyCici)

[📥 Download Now](https://github.com/faizinuha/ZeroMix/releases) • [🎥 Video Demo](https://www.youtube.com/watch?v=y9yz7ZPh_Bo) • [📖 Documentation](https://github.com/faizinuha/ZeroMix/wiki) • [💬 Community](https://github.com/faizinuha/ZeroMix/discussions)

---

**ZeroMix** is a high-performance, lightweight utility suite designed to transform your Windows experience. From real-time system monitoring to immersive video wallpapers and a **Lua-powered plugin system**, ZeroMix brings pro-level tools into a beautiful, modern interface.

[Features](#-core-features) • [Installation](#-quick-installation) • [Shortcuts](#-keyboard-shortcuts) • [Plugins](#-lua-plugin-system) • [Contributing](#-community--contribution)

</div>

## 🚀 Core Features

ZeroMix is packed with features that keep your system fast and your desktop stunning.

| Feature                  | Description                                                             |
| :----------------------- | :---------------------------------------------------------------------- |
| **🧩 Lua Plugin system** | Create and share your own plugins using simple Lua scripts!             |
| **🎥 ZeroRecord**        | **(HOT)** Screen recording with dynamic zoom based on cursor movement.  |
| **🌍 Nexus Translate**   | **(NEW) v2.0** Magic real-time translation with Anti-Crash protection.  |
| **🌙 Fake Sleep Mode**   | **(NEW)** Always-On Display with multi-trigger, idle detection & hotkey.|
| **🤖 Virtual Assistant** | AI Vision service with Live2D characters (Mihoyo, Frieren) via Groq API. |
| **📊 System Monitor**    | Real-time tracking of CPU, RAM, and Disk directly from your dashboard.  |
| **🎨 Ghost Taskbar**     | Instantly make your taskbar transparent for a clean, professional look. |
| **🎬 Video Wallpaper**   | Immersive animated wallpapers with high-performance video engine.       |
| **💻 ZeroShell v4.8**    | **(STABLE)** Pro Terminal with Powerline prompt & Arch-style fetch.     |
| **🔍 Search Overlay**    | High-performance search bar (`Alt + Space`) for apps, files, and web.   |
| **🧹 Memory Optimizer**  | Smart background GC collection and working set trimming.                |
| **🕒 Desktop Widgets**   | Clock and other widgets for your desktop.                               |
| **🔧 Plugin Creator**    | Built-in tool to create and manage Lua plugins.                         |

---

## 🧩 Lua Plugin System

ZeroMix features a powerful Lua-powered plugin ecosystem that allows you to extend functionality without recompiling the app.

### Built-in Plugins
- **zeromix.Battery**: Real-time battery monitoring
- **zeromix.Translate**: Translation services
- **zeromix.weather**: Weather data integration
- **user.pub.SimpleGui**: GUI template for custom plugins

### Creating Plugins
1. Use the built-in Plugin Creator tool
2. Write Lua scripts in `Plugins/user.*` folders
3. Plugins load automatically with real-time updates
4. Access C# APIs via ZeroMixLuaApi bridge

### Plugin Features
- Real-time execution (1-second tick)
- Dynamic window management
- FileSystemWatcher for hot-reloading
- MoonSharp Lua interpreter integration

---

## 🎥 Zerorecord & Preview

<div style="left: 0; margin-bottom: 20px; text-align: center">
      <video src="https://github.com/user-attachments/assets/0a5c1928-32b8-4b32-a391-aefdb5b3d2f5" width="1000"  autoplay muted loop></video>
    </div>

---

## 🌍 Nexus Translator Core v2.0
The world's most stable real-time translator for Windows, now integrated natively.

- **✨ Pure Magic Mode**: Translates automatically as you type (no hotkeys needed).
- **🛡️ Anti-Collision Engine**: Intelligent keyboard hook prevents text corruption during injection.
- **🚀 Triple-API Backend**: Seamless failover between MyMemory and Google Translate.
- **💎 Glassmorphism UI**: Beautiful, premium interface with a live terminal status log.
- **⌨️ Unicode Bypass**: Works in almost any application (Notepad, Chrome, Discord, etc).


---

## 📥 Quick Installation

Upgrade your Windows experience with your preferred installation method:

### 1. ⚡ One-Liner (CLI Method)
The fastest way to install ZeroMix. Open **PowerShell** and run:
```powershell
iwr -useb bit.ly/ZeroMix | iex
```

### 2. 📦 Standard Installers
Recommended for most users. Grab them from the [Official Releases](https://github.com/faizinuha/ZeroMix/releases):
- **Standard EXE**: Full setup with Inno Setup.
- **MSI Installer**: Enterprise-ready installer (built with WiX v4).
- **MSIX Package**: Modern Windows 10/11 app format with clean uninstalls.

### 3. 🚀 Portable Version
No installation required. Just download the **ZIP** file, extract, and run `ZeroMix.exe`. Perfectly suited for USB drives or restricted environments.

---

### Getting Started
1. **Launch**: Find the ⚡ icon in your system tray or search for "ZeroMix" in the Start Menu.
2. **Startup**: ZeroMix will prompt to enable "Run on Startup" during the first launch.
3. **No Admin?**: ZeroMix is designed to run with user-level permissions. Admin is only needed during initial `.exe` installation for Start Menu shortcuts.

---


## ⌨️ Keyboard Shortcuts

Workflow is everything. Master ZeroMix with these shortcuts:

- **`Alt + S`** : Toggle Fake Sleep Mode (Always-On Display)
- **`Ctrl + Space`** : Open Search Overlay (Apps, Web, Files)
- **`Ctrl + Q`** : Toggle Main Dashboard
- **`Ctrl + R`** : Force Refresh System Metrics
- **`Esc`** : Close Active Overlay/Window

---

## 🛠️ Built With

High-performance technologies for a smooth experience.

- **Frontend**: WPF (Windows Presentation Foundation) + Windows Forms hybrid
- **Core**: .NET 9.0 (C#) with self-contained publishing
- **Scripting**: MoonSharp (Lua interpreter for plugins)
- **Graphics**: Vortice (DirectX bindings for GPU acceleration)
- **Web Integration**: Microsoft.Web.WebView2 (Embedded Chromium)
- **Video Processing**: FFmpeg integration for recording and wallpapers
- **JSON Handling**: Newtonsoft.Json for configuration
- **System APIs**: Native Windows API (User32.dll, Win32 hooks)
- **Optimization**: Active GC trimming and memory management
- **Packaging**: Inno Setup, WiX v4, MSIX for installers

---

## 🛠️ Build dari Source

### Persyaratan Build
- .NET 9.0 SDK
- Visual Studio 2022 atau VS Code dengan C# extension
- Inno Setup (untuk installer)

### Langkah Build
1. Clone repository:
   ```bash
   git clone https://github.com/faizinuha/ZeroMix.git
   cd ZeroMix
   ```

2. Build project:
   ```bash
   dotnet build ZeroMix.sln
   ```

3. Publish untuk distribusi:
   ```bash
   dotnet publish ZeroMix.csproj -c Release -r win-x64 --self-contained
   ```

4. Build installer (opsional):
   ```powershell
   .\build\build.ps1 -Target Installer
   ```

### Struktur Proyek
```
ZeroMix/
├── Hotkeys/           # Sistem hotkey global
├── Plugins/           # Plugin engine dan built-in plugins
├── Search/            # Search overlay
├── SleepMode/         # Fake sleep mode
├── Virtual_Assisten/  # AI assistant dengan Live2D
├── Wallpapers/        # Video wallpaper
├── ZeroMix.recorder/  # Screen recording GPU-accelerated
├── ZeroShell/         # Terminal emulator
├── Widgets/           # Desktop widgets (clock, dll.)
├── Transparan/        # Taskbar transparency
├── Resource/          # Assets (images, videos)
├── Web/               # Web UI assets
├── build/             # Build scripts PowerShell
├── Exe/               # Installer config (Inno Setup)
└── ZeroMix.csproj     # Project file .NET 9.0
```

---

## 🤝 Community & Contribution

ZeroMix is an open-source project, and we love our contributors!

- **Found a bug?** Open an [Issue](https://github.com/faizinuha/ZeroMix/issues).
- **Have an idea?** Start a [Discussion](https://github.com/faizinuha/ZeroMix/discussions).
- **Want to code?** Check our [Contributing Guide](CONTRIBUTING.md).


---

## 🎬 Credits & Acknowledgments

The atmospheric experiences in ZeroMix are powered by beautiful visuals from the creative community:

- **Rainy City at Night** by [Hans](https://pixabay.com/id/users/hans-2/) dari [Pixabay](https://pixabay.com/)
- **Nature & Garden Ambience** by [Nicky ❤️🌿🐞🌿❤️](https://pixabay.com/id/users/nickype-10327513/) dari [Pixabay](https://pixabay.com/)
- **Cinematic Scenery 1** by [Andreas](https://pixabay.com/id/users/adege-4994132/?content=257240) dari [Pixabay](https://pixabay.com/)
- **Cinematic Scenery 2** by [Andreas](https://pixabay.com/id/users/adege-4994132/?content=230317) dari [Pixabay](https://pixabay.com/)

### 🎭 Live2D Models

- **Frieren & Fern Model** by [kyokiStudio](https://kyoki.booth.pm/) on Booth.pm

- **Huohuo Model** by [bailyovo](https://bailyovo.booth.pm/) on Booth.pm
  - _Precautions_: The copyright belongs to miHoYo. This model is for Honkai: Star Rail fan creation only. Not for political use or profit-oriented live streaming. Secondary distribution is prohibited. Creators are not responsible for violations.

---

> [!IMPORTANT]
> **Kebijakan Penghapusan (Removal Policy):** Jika Model ini tidak diperbolehkan untuk digunakan dalam aplikasi, mohon segera hubungi kami melalui Email: **Rozakadm@gmail.com**. Kami akan segera menghapus model tersebut untuk menghormati hak pemilik dan memastikan kenyamanan bagi semua pihak. Terima kasih.

> [!NOTE]
> Jika ingin mendownload model, harap gunakan **situs resmi yang telah kami sediakan**. Mohon hargai kerja keras pembuat model. Jangan menggunakan model ini untuk **komersial atau dijual tanpa izin**. Terima kasih atas pengertiannya! 🥰

> [!NOTE]
> Penggunaan model ini sepenuhnya menjadi tanggung jawab pengguna.
> Pengembang aplikasi tidak menyediakan izin komersial apa pun atas model ini dan hanya meneruskan ketentuan dari pembuat asli.

> [!WARNING]
> Pengembang aplikasi tidak bertindak sebagai pemberi lisensi model.
> Penggunaan yang melanggar ketentuan pembuat asli merupakan tanggung jawab pengguna.

---

---

## 💖 Support the Project

ZeroMix is free and open source. If you find it useful, please consider starring the repository or supporting development via Trakteer.

<div align="center">

[![Trakteer Support](https://img.shields.io/badge/Trakteer-Support_The_Dev-EE4B2B?style=for-the-badge&logoColor=white)](https://trakteer.id/MyCici)

</div>

---

<div align="center">
  Made with ❤️ by <b>Faizinuha</b> and the community. <br/>
  <b>ZeroMix © 2025-2026</b>
</div>
