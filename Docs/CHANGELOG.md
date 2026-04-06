# ZeroMix - Changelog

All notable changes to this project will be documented in this file.
Format: [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) | [Semantic Versioning](https://semver.org/)

---







## [v5.2.5] - 2026-04-06

### ✨ Features
- Update Readme.md

### 🐛 Bug Fixes
- No bug fixes

### 🔧 Changes
- Version bump to 5.2.5

---
## [v5.2.3] - 2026-04-06

### ✨ Features
- No new features

### 🐛 Bug Fixes
- No bug fixes

### 🔧 Changes
- Force rebuild v5.2.3

---
## [v5.2.3] - 2026-04-06

### ✨ Features
- No new features

### 🐛 Bug Fixes
- No bug fixes

### 🔧 Changes
- Force rebuild v5.2.3

---
## [v5.2.3] - 2026-04-06

### ✨ Features
- No new features

### 🐛 Bug Fixes
- No bug fixes

### 🔧 Changes
- Force rebuild v5.2.3

---
## [v5.2.3] - 2026-04-06

### ✨ Features
- No new features

### 🐛 Bug Fixes
- No bug fixes

### 🔧 Changes
- Force rebuild v5.2.3

---
## [v5.2.3] - 2026-04-06

### ✨ Features
- No new features

### 🐛 Bug Fixes
- No bug fixes

### 🔧 Changes
- Force rebuild v5.2.3

---
## [v5.2.2] - 2026-04-05

### ✨ Features
- Sidebar update badge — dot merah pulsing di icon System Info saat ada update tersedia
- Auto silent update check saat startup (tidak popup, hanya nyalain badge)
- Parallel chunked download di updater script (8 koneksi, IDM-style)

### 🐛 Bug Fixes
- Fix download lambat di zeromix-update.ps1 — ganti Invoke-WebRequest ke HttpWebRequest
- Fix path Downloads yang salah di beberapa sistem
- Fix installer popup "applications using files" — auto kill ZeroMix sebelum install

### 🔧 Changes
- Buffer download naik dari 4KB ke 128KB per chunk
- Progress bar updater sekarang tampilkan speed (KB/s) dan ETA

---

## [v5.2.3] - 2026-04-06

### ✨ Features
- No new features

### 🐛 Bug Fixes
- No bug fixes

### 🔧 Changes
- Force rebuild v5.2.3

---
## [v5.1.9] - 2026-04-03

### ✨ Features
- Game Mode Translator via Clipboard Paste (Ctrl+V), kompatibel dengan DirectInput/RawInput
- CI/CD Sign Fix — perbaikan pipeline signing di GitHub Actions

### 🐛 Bug Fixes
- Fix osslsigncode tidak dikenali setelah choco install pada Windows runner
- Fallback ke path hardcoded C:\ProgramData\chocolatey\bin\ jika PATH belum ter-refresh

---

## [v5.1.5] - 2026-04-02

### ✨ Features
- Ask AI Mode Overlay di Search Box
- Smart Intent Detection — carikan foto, beli barang, shopee
- Text-to-Speech (TTS) untuk Frieren/Fern/HuoHuo
- Lip-Sync Animation sinkron dengan suara

### 🐛 Bug Fixes
- Fix Force Close saat tombol rekam diklik pada beberapa GPU
- Fix Temp Cleanup — pembersihan %temp% lebih stabil

### 🚀 Improvements
- Hardware Encoder Fallback: NVENC/QSV/AMF gagal → otomatis ke CPU
- DXGI Resource Management lebih efisien
- Memory Optimization: cache Live2D dibersihkan lebih agresif
- System Health scan lebih ringan

---

## [v5.1.3] - 2026-03-28

### 🐛 Bug Fixes
- Fix crash recorder pada GPU tertentu saat pertama kali dijalankan
- Fix memory leak pada engine Live2D saat karakter aktif lama
- Fix pembersihan %temp% yang tidak lengkap
- Fix encoder fallback yang menyebabkan hang saat NVENC tidak tersedia

### 🚀 Improvements
- Stabilitas DXGI capture ditingkatkan
- Optimasi minor pada System Health scanner

---

## [v5.1.2] - 2026-03-25

### ✨ Features
- Ask AI Mode Overlay di Search Box
- Smart Intent Detection untuk foto dan marketplace
- Fallback to AI — pertanyaan umum dijawab Frieren
- Text-to-Speech dan Lip-Sync Animation

### 🐛 Bug Fixes
- Fix Force Close saat tombol rekam diklik
- Fix Temp Cleanup lebih stabil

### 🚀 Improvements
- Hardware Encoder Fallback lebih cerdas
- Memory Optimization untuk Live2D
- System Health scanner lebih ringan

---

## [v1.0.0] - Initial Release

### ✨ Features
- Desktop Recorder — full-screen dan window-specific
- GPU-Accelerated Encoding (NVENC, QuickSync, AMF)
- Plugin System dengan Lua support
- Virtual Assistant AI
- Wallpaper Management
- Transparent Taskbar
- Sleep Mode

---

**Maintained by:** ZeroMix Team | **License:** [LICENSE.txt](./LICENSE.txt)
