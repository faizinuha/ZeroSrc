# ZeroMix - Changelog

All notable changes to this project will be documented in this file.
Format: [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) | [Semantic Versioning](https://semver.org/)

---

## [v5.3.1] - 2026-04-08

### ✨ Features
- Menambahkan Beberapa komponenen dan perbaikan , ZeroMixSell , SleepMode , Perubahan Tampilan , perbaiki Bugs , Optimize Virtual ,
- Menambahkan Beberapa komponenen dan perbaikan , ZeroMixSell , SleepMode , Perubahan Tampilan , perbaiki Bugs , Optimize Virtual ,
- Menambahkan Beberapa komponenen dan perbaikan , ZeroMixSell , SleepMode , Perubahan Tampilan , perbaiki Bugs , Optimize Virtual ,

### 🐛 Bug Fixes
- No bug fixes

### 🔧 Changes
- Rebuilt: Assets/Data/Video/Vs (5).mp4
- Rebuilt: Exe/Setup.iss
- Rebuilt: ZeroMix.csproj
- Rebuilt: src/MainWindow.xaml
- Rebuilt: src/MainWindow.xaml.cs
- Rebuilt: src/SleepMode/SleepOverlayWindow.xaml
- Rebuilt: src/SleepMode/SleepOverlayWindow.xaml.cs
- Rebuilt: src/SleepMode/SleepSettingsModel.cs
- Rebuilt: src/SleepMode/SleepSettingsWindow.xaml
- Rebuilt: src/SleepMode/SleepSettingsWindow.xaml.cs
- Rebuilt: src/Virtual_Assisten/VirtualAssistantWindow.xaml
- Rebuilt: src/Virtual_Assisten/VirtualAssistantWindow.xaml.cs
- Rebuilt: src/Wallpapers/VideoWallpaperWindow.xaml.cs
- Rebuilt: src/Wallpapers/WallpapersView.xaml.cs
- Rebuilt: src/ZeroMix.recorder/WindowPickerWindow.xaml.cs
- Rebuilt: src/ZeroShell/ZeroShellWindow.xaml
- Rebuilt: src/ZeroShell/ZeroShellWindow.xaml.cs

---
## [5.3.0] - 2026-04-07

### ✨ Features
- Sleep Mode: tambah opsi musik opsional (MP3/WAV/FLAC/OGG/M4A) — bisa dipakai bareng background apapun
- Sleep Mode: custom background (gambar/video) langsung tampil tanpa delay
- Sleep Mode: volume slider untuk musik

### 🐛 Bug Fixes
- Fix Search Overlay kotak-kotak setelah penggunaan ke-2 (root cause: window di-reuse, sekarang selalu buat instance baru)
- Fix SleepMode custom background tidak tersimpan saat settings dibuka ulang
- Fix ZeroMix.recorder video output hitam (tambah -vsync cfr flag ke FFmpeg)
- Fix video wallpaper tidak bisa diputar — error handling diperbaiki dengan pesan yang jelas
- Fix uninstaller tidak hapus registry context menu dan startup shortcut

### 🔧 Changes
- Virtual Assistant: hapus tombol mic dari UI overlay (mic tetap bisa diakses dari halaman list model)
- Virtual Assistant: eye tracking interval 200ms → 300ms, vision timer 30s → 60s (hemat CPU/RAM)
- Virtual Assistant: semua permission request di-deny kecuali yang dibutuhkan
- Wallpapers: hapus kode optimasi video background yang tidak perlu (langsung play tanpa ffmpeg pre-process)
- Uninstaller: auto kill ZeroMix.exe, hapus registry, hapus startup shortcut, hapus LocalAppData
- Sleep Mode: BitmapImage di-Freeze() setelah load → tidak makan RAM berulang
- Sleep Mode: DispatcherTimer pakai Background priority

---



-- End Version 5.2.5 -> 5.3.0 -- 



## [v5.2.5] - 2026-04-07

### ✨ Features
- Sleep Mode: tambah opsi musik opsional (MP3/WAV/FLAC/OGG/M4A) — bisa dipakai bareng background apapun
- Sleep Mode: custom background (gambar/video) langsung tampil tanpa delay
- Sleep Mode: volume slider untuk musik

### 🐛 Bug Fixes
- Fix Search Overlay kotak-kotak setelah penggunaan ke-2 (root cause: window di-reuse, sekarang selalu buat instance baru)
- Fix SleepMode custom background tidak tersimpan saat settings dibuka ulang
- Fix ZeroMix.recorder video output hitam (tambah -vsync cfr flag ke FFmpeg)
- Fix video wallpaper tidak bisa diputar — error handling diperbaiki dengan pesan yang jelas
- Fix uninstaller tidak hapus registry context menu dan startup shortcut

### 🔧 Changes
- Virtual Assistant: hapus tombol mic dari UI overlay (mic tetap bisa diakses dari halaman list model)
- Virtual Assistant: eye tracking interval 200ms → 300ms, vision timer 30s → 60s (hemat CPU/RAM)
- Virtual Assistant: semua permission request di-deny kecuali yang dibutuhkan
- Virtual Assistant: tambah AreBrowserAcceleratorKeysEnabled=false dan IsSwipeNavigationEnabled=false
- Wallpapers: hapus kode optimasi video background yang tidak perlu (langsung play tanpa ffmpeg pre-process)
- Uninstaller: auto kill ZeroMix.exe, hapus registry, hapus startup shortcut, hapus LocalAppData
- Sleep Mode: BitmapImage di-Freeze() setelah load → tidak makan RAM berulang
- Sleep Mode: DispatcherTimer pakai Background priority

---
## [v5.2.3] - 2026-04-06

### ✨ Features
- AOD Style Picker — 5 style Always-On Display
- Analog Clock AOD dengan anti burn-in
- Sidebar Update Badge
- Auto Silent Update Check

### 🐛 Bug Fixes
- Fix double instance
- Fix Search Overlay lambat
- Fix Sleep Mode overlay crash saat laptop wake up

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
