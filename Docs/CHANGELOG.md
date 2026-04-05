# ZeroMix - Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---













## [v5.2.2] - 2026-04-05

### ✨ Features
- Something

### 🐛 Bug Fixes
- No bug fixes

### 🔧 Changes
- Version bump to 5.2.2

---
## [v5.2.1] - 2026-04-04

### ✨ Features
- No new features

### 🐛 Bug Fixes
- No bug fixes

### 🔧 Changes
- Version bump to 5.2.1

---
## [v5.2.1] - 2026-04-04

### ✨ Features
- Publish SDK to nuget.org
- Add PluginSDK and refactor IZeroMixHost

### 🐛 Bug Fixes
- Fix Zeromix.recorder | Virtual_assisten Img | Upcooming : Bitrate control ΓÇö slider untuk pilih kualitas (720p/1080p/4K)

### 🔧 Changes
- Update
- update
- update readme
- Update FIle docs

---
## [v5.2.1] - 2026-04-04

### ✨ Features
- Publish SDK to nuget.org
- Add PluginSDK and refactor IZeroMixHost

### 🐛 Bug Fixes
- No bug fixes

### 🔧 Changes
- Update
- update
- update readme
- Update FIle docs

---
## [v5.2.0] - 2026-04-03

### ✨ Features
- To be documented

### 🐛 Bug Fixes
- To be documented

### 🔧 Changes
- Version bump to 5.2.0

---
## [v5.1.9] - 2026-04-03

### ✨ Features
- To be documented

### 🐛 Bug Fixes
- To be documented

### 🔧 Changes
- Version bump to 5.1.9

---
## [v5.1.9] - 2026-04-03

### ✨ Features
- To be documented

### 🐛 Bug Fixes
- To be documented

### 🔧 Changes
- Version bump to 5.1.9

---
## [v5.1.9] - 2026-04-03

### ✨ Features
- To be documented

### 🐛 Bug Fixes
- To be documented

### 🔧 Changes
- Version bump to 5.1.9

---
## [v5.1.9] - 2026-04-03

### ✨ Features
- To be documented

### 🐛 Bug Fixes
- To be documented

### 🔧 Changes
- Version bump to 5.1.9

---
## [v5.1.9] - 2026-04-03

### ✨ Features
- To be documented

### 🐛 Bug Fixes
- To be documented

### 🔧 Changes
- Version bump to 5.1.9

---
## [v5.1.9] - 2026-04-03

### ✨ Features
- To be documented

### 🐛 Bug Fixes
- To be documented

### 🔧 Changes
- Version bump to 5.1.9

---
## [v5.1.9] - 2026-04-03

### ✨ Features
- To be documented

### 🐛 Bug Fixes
- To be documented

### 🔧 Changes
- Version bump to 5.1.9

---
## [Unreleased] - v5.2.0 Upcoming

### ✨ Planned Features
- Multi-Monitor recording support
- Streaming mode (OBS-compatible output)
- Advanced Audio Mixer (per-source volume control)
- Plugin Marketplace browser in-app
- Cloud Sync Settings antar perangkat

---

## [v5.1.9] - 2026-04-03

### ✨ Features
- **Game Mode Translator**: Terjemahan via Clipboard Paste (Ctrl+V), kompatibel dengan DirectInput/RawInput game chat.
- **CI/CD Sign Fix**: Perbaikan pipeline signing di GitHub Actions — osslsigncode PATH refresh antar step.

### 🐛 Bug Fixes
- Fix `osslsigncode` tidak dikenali di step berikutnya setelah `choco install` pada Windows runner.
- Fallback ke path hardcoded `C:\ProgramData\chocolatey\bin\` jika PATH belum ter-refresh.

---

## [v5.1.5] - 2026-04-02

### ✨ Features
- **Ask AI Mode Overlay**: Tombol "Ask AI" di Search Box untuk bertanya langsung dari tampilan pencarian.
- **Smart Intent Detection**: `carikan foto [nama]` → pencarian gambar; `beli barang` / `shopee` → marketplace.
- **Fallback to AI**: Pertanyaan umum dijawab otomatis oleh AI Frieren.
- **Text-to-Speech (TTS)**: Frieren/Fern/HuoHuo bisa bicara langsung menjawab pertanyaan.
- **Lip-Sync Animation**: Gerakan mulut karakter sinkron dengan suara.

### 🐛 Bug Fixes
- Fix Force Close saat tombol rekam diklik pada beberapa tipe GPU.
- Fix Temp Cleanup — pembersihan `%temp%` lebih stabil.

### 🚀 Improvements
- Hardware Encoder Fallback: NVENC/QSV/AMF gagal → otomatis ke CPU tanpa hang.
- DXGI Resource Management: Screen capture tidak membebani driver video Windows.
- Memory Optimization: Cache Live2D dibersihkan lebih agresif, mencegah memory leak.
- System Health scan lebih ringan, tidak menyebabkan UI lag.

---

## [v5.1.3] - 2026-03-28

### 🐛 Bug Fixes
- Fix crash recorder pada GPU tertentu saat pertama kali dijalankan.
- Fix memory leak pada engine Live2D saat karakter aktif dalam waktu lama.
- Fix pembersihan `%temp%` yang tidak lengkap pada beberapa konfigurasi sistem.
- Fix encoder fallback yang menyebabkan aplikasi hang saat NVENC tidak tersedia.

### 🚀 Improvements
- Stabilitas DXGI capture ditingkatkan.
- Optimasi minor pada System Health scanner.

---

## [v5.1.2] - 2026-03-25

### ✨ Features
- **Ask AI Mode Overlay**: Menambahkan tombol "Ask AI" di Search Box untuk bertanya langsung dari tampilan pencarian.
- **Smart Intent Detection**: `carikan foto [nama]` otomatis membuka pencarian gambar; `beli barang` / `shopee` langsung mengarahkan ke marketplace favorit.
- **Fallback to AI**: Pertanyaan umum otomatis dijawab oleh AI Frieren dengan gaya bicaranya yang khas.
- **Text-to-Speech (TTS)**: Frieren/Fern/HuoHuo kini bisa bicara langsung menjawab pertanyaan.
- **Lip-Sync Animation**: Gerakan mulut karakter sinkron dengan suara yang dihasilkan.

### 🐛 Bug Fixes
- **Fix Force Close**: Memperbaiki crash saat tombol rekam diklik pada beberapa tipe GPU.
- **Fix Temp Cleanup**: Pembersihan folder `%temp%` kini lebih stabil dan mencakup lebih banyak folder sampah sistem.

### 🚀 Improvements
- **Hardware Encoder Fallback**: Deteksi encoder GPU (NVENC/QSV/AMF) lebih cerdas; otomatis beralih ke CPU jika gagal tanpa hang.
- **DXGI Resource Management**: Screen capture tidak lagi membebani driver video Windows.
- **Memory Optimization**: Pembersihan cache Live2D lebih agresif untuk mencegah memory leak saat karakter aktif lama.
- **Optimasi System Health**: Proses scanning sistem lebih ringan, tidak menyebabkan UI lag.
- **Version Stamp**: Update internal ke versi v5.1.2.

---

## [1.0.0] - Initial Release

### ✨ Features
- **Desktop Recorder** - Full-screen and window-specific recording
- **GPU-Accelerated Encoding** - Hardware encoding support (NVIDIA NVENC, Intel QuickSync, AMD VCE)
- **Screen Capture** - DXGI-based GPU capture with GDI fallback
- **Cursor Tracking** - Native cursor drawing during recording
- **Audio Support** - Microphone and system audio capture
- **Hotkeys** - Customizable hotkeys for recording control
- **Plugin System** - Extensible plugin architecture with Lua support
- **Virtual Assistant** - AI-powered virtual assistant
- **Wallpaper Support** - Custom wallpaper management
- **Customizable UI** - Themes and layout customization
- **Sleep Mode** - Idle detection and sleep mode
- **Transparent Taskbar** - Custom Windows functionality

### 🐛 Bug Fixes
- Fixed crash when FFmpeg process dies unexpectedly
- Fixed memory leaks in frame processing
- Fixed null reference exceptions in compositor
- Fixed audio device enumeration errors
- Fixed window capture coordinate handling

### 🚀 Improvements
- Optimized frame processing pipeline
- Improved error logging and diagnostics
- Enhanced resource cleanup on shutdown
- Better performance with high-resolution displays
- Improved GPU fallback handling
- Better exception handling throughout application

### 📚 Documentation
- Added comprehensive README with quick start guide
- Added CHANGELOG following Keep a Changelog format
- Added GitHub Actions automation documentation
- Added contribution guidelines
- Added security policy

---

## Release Process

Each release follows this process:

1. **Code Changes** → Push commits following [Conventional Commits](https://www.conventionalcommits.org/)
2. **Version Bump** → Create annotated git tag: `git tag -a v5.1.9 -m "Release message"`
3. **Push Tag** → `git push origin v5.1.9`
4. **Automation** → GitHub Actions automatically:
   - Builds the project
   - Generates changelog from commits
   - Creates GitHub Release with release notes
   - Updates this CHANGELOG.md
5. **Published** → Release available on [GitHub Releases](https://github.com/faizinuha/ZeroMix/releases)

---

## Support

- 🐛 [Report Issues](https://github.com/faizinuha/ZeroMix/issues)
- 💬 [Discussions](https://github.com/faizinuha/ZeroMix/discussions)
- 📖 [Wiki & Documentation](https://github.com/faizinuha/ZeroMix/wiki)

---

**Last Updated:** April 3, 2026
**Maintained by:** ZeroMix Team
**License:** See [LICENSE.txt](./LICENSE.txt)
