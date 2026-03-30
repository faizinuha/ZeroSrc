# ZeroMix - The Ultimate Windows Suite for Performance & Aesthetics

[![Version](https://img.shields.io/badge/version-5.0.0-blue.svg)](https://github.com/faizinuha/ZeroMix/releases)
[![.NET](https://img.shields.io/badge/.NET-9.0-green.svg)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-red.svg)](LICENSE.txt)

ZeroMix adalah aplikasi desktop Windows yang dirancang untuk meningkatkan performa dan estetika sistem Anda. Dengan antarmuka modern berbasis WPF, ZeroMix menawarkan berbagai fitur canggih seperti monitoring sistem real-time, plugin berbasis Lua, perekaman layar GPU-accelerated, asisten virtual AI, wallpaper video, dan banyak lagi.

## ✨ Fitur Utama

### 🔍 **Search Overlay**
- Pencarian cepat untuk aplikasi, file, web, dan kalkulator
- Aktivasi via hotkey (Alt+S atau Ctrl+Space)
- Lazy-loading untuk performa optimal

### 🎮 **Screen Recording (ZeroRecord)**
- Perekaman layar dengan akselerasi GPU (DXGI + DirectX)
- Deteksi game otomatis
- Encoder hardware (H.264/H.265)
- Output tersimpan di `Videos/ZeroRecord/`

### 🔌 **Plugin System (Lua-powered)**
- Ekstensi via plugin berbasis Lua
- Plugin built-in: Battery, Translate, Weather
- Real-time loading dan update
- Buat plugin kustom dengan mudah

### 💤 **Fake Sleep Mode**
- Always-on display dengan deteksi idle
- Aktivasi berdasarkan waktu idle atau baterai rendah
- Overlay window untuk simulasi sleep mode

### 🤖 **Virtual Assistant**
- Asisten AI dengan vision service (integrasi Groq API)
- Dukungan karakter Live2D (Mihoyo, Sou Sou No Frieren)
- Analisis konteks window aktif

### 🎨 **Video Wallpapers**
- Wallpaper video immersif
- Kontrol volume
- Refresh desktop otomatis

### 🖥️ **Ghost Taskbar**
- Taskbar transparan dengan efek blur/acrylic
- Menggunakan Win32 API untuk integrasi sistem

### ⌨️ **Global Hotkeys**
- Shortcut keyboard global untuk berbagai aksi
- Konfigurasi kustom tersimpan di `%AppData%\ZeroMix\`

### 📊 **System Monitoring**
- Monitoring real-time CPU, RAM, dan disk I/O
- Dashboard performa dengan update periodik

### 🐚 **ZeroShell Terminal**
- Emulator terminal dengan Powerline prompt
- Fetch sistem dan ASCII art

### 🕒 **Desktop Widgets**
- Widget clock dan lainnya untuk desktop

## 🚀 Instalasi

### Persyaratan Sistem
- Windows 10 Build 19041+ (64-bit)
- .NET 9.0 Runtime (termasuk dalam installer)

### Metode Instalasi

#### 1. Installer (Direkomendasikan)
Download installer terbaru dari [Releases](https://github.com/faizinuha/ZeroMix/releases):
- **EXE Installer** (Inno Setup) - Setup lengkap dengan shortcut
- **MSI Installer** (WiX) - Untuk deployment enterprise
- **MSIX Package** - Modern Windows package

#### 2. One-Liner CLI
```powershell
iwr -useb bit.ly/ZeroMix | iex
```

#### 3. Portable ZIP
- Download ZIP dari Releases
- Ekstrak dan jalankan `ZeroMix.exe`
- Tidak perlu instalasi, cocok untuk USB

## 📖 Penggunaan

### Memulai
1. Jalankan ZeroMix setelah instalasi
2. Ikuti onboarding untuk setup awal
3. Gunakan hotkey default untuk akses cepat

### Hotkey Utama
- `Alt+S` atau `Ctrl+Space`: Buka search overlay
- `Ctrl+Q`: Toggle main dashboard
- `Ctrl+R`: Refresh metrics sistem
- `Esc`: Tutup overlay aktif

### Plugin Development
1. Buka "Create Plugin" dari menu
2. Buat script Lua dengan template
3. Simpan di folder `Plugins/user.*`
4. Plugin akan dimuat otomatis

### Screen Recording
1. Tekan hotkey recording atau dari menu
2. Pilih area perekaman (opsional)
3. Mulai recording - output otomatis tersimpan

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

## 📁 Struktur Proyek

```
ZeroMix/
├── Hotkeys/           # Sistem hotkey global
├── Plugins/           # Plugin engine dan built-in plugins
├── Search/            # Search overlay
├── SleepMode/         # Fake sleep mode
├── Virtual_Assisten/  # AI assistant
├── Wallpapers/        # Video wallpaper
├── ZeroMix.recorder/  # Screen recording
├── ZeroShell/         # Terminal emulator
├── Widgets/           # Desktop widgets
├── Transparan/        # Taskbar transparency
├── Resource/          # Assets (images, videos)
├── Web/               # Web UI assets
├── build/             # Build scripts
├── Exe/               # Installer config
└── ZeroMix.csproj     # Project file
```

## 🤝 Kontribusi

Kami menerima kontribusi! Silakan:
1. Fork repository
2. Buat branch fitur baru
3. Commit perubahan
4. Push ke branch Anda
5. Buat Pull Request

### Development Guidelines
- Ikuti coding style C# standard
- Tambahkan komentar untuk kode kompleks
- Test plugin di berbagai skenario
- Update dokumentasi jika menambah fitur

## 📄 Lisensi

ZeroMix dilisensikan di bawah [MIT License](LICENSE.txt). Lihat file LICENSE untuk detail lengkap.

## 🆘 Dukungan

- **Issues**: Laporkan bug atau request fitur di [GitHub Issues](https://github.com/faizinuha/ZeroMix/issues)
- **Discussions**: Diskusi umum di [GitHub Discussions](https://github.com/faizinuha/ZeroMix/discussions)
- **Wiki**: Dokumentasi lengkap di [Wiki](https://github.com/faizinuha/ZeroMix/wiki)

## 🙏 Credits

- **MoonSharp**: Lua interpreter untuk .NET
- **Vortice**: DirectX bindings
- **WebView2**: Embedded Chromium
- **Inno Setup**: Installer framework

---

**Dibuat dengan ❤️ untuk komunitas Windows**