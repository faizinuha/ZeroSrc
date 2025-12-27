<div align="center">

# 🎯 ZeroMix

<img src="assets/zeromix-high-resolution-logo-transparent.png" alt="ZeroMix Logo" width="200"/>

**Your All-in-One Windows Desktop Companion**  
_Streamline your workflow with powerful system utilities and elegant customization_

[![GitHub Release](https://img.shields.io/github/v/release/faizinuha/ZeroMix?color=00D9FF)](https://github.com/faizinuha/ZeroMix/releases)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.txt)
[![.NET Version](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%2B-0078D4)](https://www.microsoft.com/windows)

[📥 Download](https://github.com/faizinuha/ZeroMix/releases) • [🌐 Website](https://faizinuha.github.io/ZeroMix/) • [📖 Documentation](https://github.com/faizinuha/ZeroMix/wiki) • [💬 Community](https://github.com/faizinuha/ZeroMix/discussions)

</div>

---

## ✨ What is ZeroMix?

**ZeroMix** is a modern, lightweight desktop utility suite designed to enhance your Windows experience. Whether you're monitoring system performance, customizing your taskbar, or managing wallpapers (including videos!), ZeroMix combines essential tools into one elegant interface.

Think of it as your **Swiss Army knife** for Windows productivity—powerful, versatile, and always at your fingertips via the system tray.

---

## 🚀 Key Features

<table>
  <tr>
    <td width="50%">
      
### 📊 **Real-Time System Monitoring**
- Live CPU, RAM, and Disk usage tracking
- Process manager with top resource consumers
- System information dashboard
- Performance graphs and metrics

### 🎨 **Desktop Customization**

- **Transparent Taskbar** - Make your taskbar blend seamlessly
- **Video Wallpapers** - Set MP4/AVI videos as animated wallpapers (FFmpeg-powered)
- **Wallpaper Manager** - Browse and apply wallpapers instantly
- **Clock Widget** - Elegant floating clock for your desktop

### Main Dashboard

<img src="assets/image copy.png" alt="ZeroMix Dashboard" width="700"/>

_Monitor your system, manage processes, and access quick features—all in one place._

</div>

---

## 📋 System Requirements

| Component        | Requirement                                                           |
| ---------------- | --------------------------------------------------------------------- |
| **OS**           | Windows 10 (Build 19041) or newer                                     |
| **RAM**          | 2 GB minimum, 4 GB recommended                                        |
| **Disk Space**   | 150 MB (including FFmpeg)                                             |
| **.NET Runtime** | [.NET 9.0](https://dotnet.microsoft.com/download/dotnet/9.0) or newer |
| **Permissions**  | User-level (no admin required)                                        |
| **Internet**     | Optional (for auto-updates and web search)                            |

> 💡 **Note**: For **video wallpaper** features, FFmpeg is bundled with the installer.

---

## 📥 Installation

### **Download from GitHub Releases**

1. **Visit** the [Releases page](https://github.com/faizinuha/ZeroMix/releases)

2. **Download** the latest installer:

   - `ZeroMix-Setup-v2.2.2.exe` (recommended)

3. **Run** the installer and follow the setup wizard:

   - Choose installation directory
   - Select desktop shortcut option (optional)
   - Choose "Run at startup" if desired

4. **Launch** ZeroMix:

   - From Start Menu: **ZeroMix**
   - Or from Desktop shortcut (if created)
   - Check system tray for the ZeroMix icon

5. **Done!** ZeroMix is now running 🎉

> 💡 **Note**: All releases are hosted on [GitHub Releases](https://github.com/faizinuha/ZeroMix/releases). Always download from official sources to ensure security.

---

## 🎯 Quick Start Guide

### **First Launch**

1. After installation, ZeroMix will appear in your **system tray** (bottom-right corner)
2. **Double-click** the tray icon to open the main dashboard
3. Explore the features from the sidebar menu

### **Common Tasks**

#### 🖼️ **Set a Video Wallpaper**

1. Click **Wallpaper** in the sidebar
2. Click **Browse** and select a video file (MP4, AVI, MOV)
3. Click **Set as Wallpaper**
4. ZeroMix will optimize the video using FFmpeg
5. Use recommended apps like [Lively Wallpaper](https://www.microsoft.com/store/apps/9NTM2QC6QWS7) to apply

#### 📊 **Monitor System Performance**

1. Navigate to **Home** tab
2. Click **Enable Monitoring**
3. View real-time CPU, RAM, and Disk usage

#### 🎨 **Enable Transparent Taskbar**

1. Go to **Home** → **Quick Features**
2. Click **Enable Transparent Taskbar**
3. Click again to toggle back to normal

#### 🧹 **Clear System Cache**

1. Home → **System Tools** section
2. Click **Clear Cache**
3. Confirm and wait for completion

---

## ⌨️ Keyboard Shortcuts

| Shortcut       | Action                    |
| -------------- | ------------------------- |
| `Ctrl + Space` | Open Quick Search Overlay |
| `Ctrl + Q`     | Show/Hide Main Window     |
| `Ctrl + R`     | Refresh Dashboard         |
| `Escape`       | Close Current Dialog      |

> 💡 More shortcuts available in **Settings** → **Hotkeys**

---

## 🔄 Auto-Update

ZeroMix uses a **CLI-based update system** to keep your installation up-to-date.

### **How it works:**

1. The **ZeroMix CLI tool** (`zeromix-cli.bat`) checks for updates from GitHub Releases
2. When a new version is available, you'll be notified
3. Updates can be installed via command line

### **Manual Update Check:**

```bash
# Open Command Prompt or PowerShell
cd "C:\Program Files\ZeroMix\bin"
zeromix-cli.bat update
```

### **Or download manually:**

1. Visit [GitHub Releases](https://github.com/faizinuha/ZeroMix/releases)
2. Download the latest installer
3. Run to update (will preserve your settings)

> 💡 **Tip**: The installer will automatically detect and update your existing installation without losing settings.

---

## ❓ FAQ

<details>
<summary><b>Q: Why is FFmpeg needed for video wallpapers?</b></summary>

**A:** FFmpeg optimizes videos by reducing file size (~60% smaller) and converting to the ideal format for smooth playback without lag. It's automatically included in the installer!

</details>

<details>
<summary><b>Q: Does ZeroMix require administrator rights?</b></summary>

**A:** No! ZeroMix runs with user-level permissions. Admin rights are only needed during installation (to copy files to Program Files).

</details>

<details>
<summary><b>Q: Can I run ZeroMix at Windows startup?</b></summary>

**A:** Yes! During installation, check the **"Run at startup"** option. Or enable it later in **Settings** → **Startup**.

</details>

<details>
<summary><b>Q: How do I uninstall ZeroMix?</b></summary>

**A:**

1. Close ZeroMix (right-click tray icon → **Exit**)
2. Go to **Settings** → **Apps** → **ZeroMix**
3. Click **Uninstall**
4. Or run the uninstaller from the Start Menu
</details>

---

## 🗑️ Manual Uninstall (if needed)

If you encounter issues with the standard uninstaller:

1. **Close ZeroMix**:

   - Right-click the system tray icon
   - Select **Exit**

2. **End Background Tasks** (if still running):

   - Press `Ctrl + Shift + Esc` to open Task Manager
   - Find `ZeroMix.exe`
   - Right-click → **End Task**

3. **Run Uninstaller**:

   - Start Menu → **ZeroMix** → **Uninstall ZeroMix**
   - Or: **Settings** → **Apps** → **ZeroMix** → **Uninstall**

4. **Optional: Clean AppData**:
   - Delete `C:\Users\<YourName>\AppData\Local\ZeroMix` (saves settings)
   - Delete `C:\Users\<YourName>\AppData\Roaming\ZeroMix` (cache data)

---

## 🛠️ Building from Source

### **Prerequisites**

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)
- [Inno Setup](https://jrsoftware.org/isinfo.php) (for creating installer)

### **Build Steps**

```bash
# Clone the repository
git clone https://github.com/faizinuha/ZeroMix.git
cd ZeroMix

# Restore dependencies
dotnet restore

# Build the project
dotnet build -c Release

# Run the application
dotnet run

# Publish standalone executable
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true

# Create installer (requires Inno Setup)
iscc Exe/Setup.iss
```

---

## 🤝 Contributing

We welcome contributions! Here's how you can help:

- 🐛 **Report bugs** via [Issues](https://github.com/faizinuha/ZeroMix/issues)
- 💡 **Suggest features** in [Discussions](https://github.com/faizinuha/ZeroMix/discussions)
- 🔧 **Submit pull requests** for bug fixes or improvements
- 📖 **Improve documentation**
- 🌍 **Translate** to other languages

Please read our [Contributing Guide](CONTRIBUTING.md) before submitting PRs.

Contributors:

- [Faizinuha](https://github.com/faizinuha)

---

## 📜 Changelog

### **v2.2.2** (Latest) - 2025-11-26

- ➕ **NEW**: Video wallpaper support with FFmpeg optimization
- ➕ **NEW**: Improved wallpaper manager with filter options
- 🔧 **FIX**: System monitoring memory leaks
- 🔧 **FIX**: Transparent taskbar on Windows 11
- ⚡ **PERF**: 30% faster startup time
- 📚 **DOCS**: Comprehensive README and user guide

[View Full Changelog](https://github.com/faizinuha/ZeroMix/releases)

---

## 📜 License

This project is licensed under the **MIT License** - see the [LICENSE.txt](LICENSE.txt) file for details.

```
Copyright (c) 2025 ZeroMix Team
```

---

## 💖 Support the Project

If you find ZeroMix helpful, consider supporting its development:

<div align="center">

[![Trakteer](https://img.shields.io/badge/Trakteer-00D9FF?style=for-the-badge&logo=buy-me-a-coffee&logoColor=white)](https://trakteer.id/MyCici)

**Your support helps us:**

- 🚀 Add new features
- 🐛 Fix bugs faster
- 📚 Create better documentation
- 🎨 Improve UI/UX

</div>

---

## 📞 Contact & Links

<div align="center">

[![GitHub](https://img.shields.io/badge/GitHub-faizinuha-181717?style=for-the-badge&logo=github)](https://github.com/faizinuha)
[![Website](https://img.shields.io/badge/Website-ZeroMix-00D9FF?style=for-the-badge&logo=google-chrome&logoColor=white)](https://faizinuha.github.io/ZeroMix/)
[![Issues](https://img.shields.io/github/issues/faizinuha/ZeroMix?style=for-the-badge)](https://github.com/faizinuha/ZeroMix/issues)
[![Discussions](https://img.shields.io/github/discussions/faizinuha/ZeroMix?style=for-the-badge)](https://github.com/faizinuha/ZeroMix/discussions)

</div>

---

<div align="center">

**Made with ❤️ by the ZeroMix Team**

_Empowering Windows users since 2025_

⭐ **Star this repo** if you find it useful!

</div>
