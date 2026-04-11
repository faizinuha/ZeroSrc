# ZeroMix - Changelog

All notable changes to this project will be documented in this file.
Format: [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) | [Semantic Versioning](https://semver.org/)

---

## [v5.5.0] - 2026-04-11

### ✨ Features

- **Charger Notif Plugin** (`ChargerBatterynotif.core`): Plugin baru khusus event charger — notifikasi + Lottie animation + suara saat charger dicolok, dicabut, dan baterai penuh 100%.
- **Lottie Animation Support**: Integrasi `LottieSharp` NuGet untuk animasi JSON di notifikasi charger. Fallback otomatis ke PNG maskot dari `zeromix.Battery/Maskot/` jika Lottie tidak tersedia.
- **Custom Sound per Event**: User bisa pilih file `.wav` sendiri via tombol Browse untuk tiap event (Charging, Unplug, Full). Built-in WAV bawaan tersedia sebagai default. Reset ke bawaan dengan tombol ✕.
- **Sound Preview**: Tombol ▶ untuk preview suara langsung dari settings panel tanpa perlu trigger event sungguhan.
- **Config Persistence**: Pilihan pesan dan sound disimpan ke `%AppData%\ZeroMix\charger_notif_config.json`, tidak hilang saat restart.
- **Bahasa Korea (ko-KR)**: Tambah file locale `ko-KR.xaml` — ZeroMix kini mendukung 5 bahasa: English, Indonesia, 日本語, 中文, 한국어.
- **Language Switching Tanpa Reload**: Ganti bahasa langsung apply ke UI tanpa restart aplikasi. Sidebar nav labels diupdate secara programatik via `ApplyLanguageToStaticElements`.
- **Language Selector Fix**: `InitializeLanguageSelector` sekarang baca dari AppData terlebih dahulu, suppress `SelectionChanged` saat init agar tidak trigger dua kali.
- **Virtual Assistant — Fast Model Loading**: Guard `isLoadingModel` mencegah concurrent load yang menyebabkan not responding. Pause 80ms setelah destroy model lama agar GC bersih sebelum load berikutnya.
- **Virtual Assistant — Idle Animations**: `playBodyMotion()` baru yang memanggil `model.motion("", idx)` sesuai model — Frieren (2 motions), Fern (1), Huohuo (7). Body motion cycle setiap 8–15 detik, expression cycle setiap 5–10 detik. Model langsung play idle 300ms setelah loaded.
- **Video Wallpaper Optimization**: Video besar di-encode ulang di background via FFmpeg (1280×720, CRF 28, preset veryfast). Playback langsung mulai dengan file asli, swap seamless ke file teroptimasi setelah selesai. Cache disimpan di `%AppData%\ZeroMix\WallpaperCache\`.

### 🐛 Bug Fixes

- Fix `LanguageComboBox_SelectionChanged` menampilkan MessageBox error yang tidak perlu saat save language.
- Fix language preference disimpan ke BaseDirectory (read-only) — sekarang ke AppData.
- Fix Virtual Assistant model tidak load saat tombol ACTIVATE diklik — `SetCharacter` dipanggil sebelum WebView navigation selesai. Sekarang tunggu `NavigationCompleted` event.
- Fix idle animation tidak berjalan di Virtual Assistant — `startAutoMotion()` sekarang dipanggil setelah model loaded, bukan saat init.
- Fix `--disable-gpu-vsync` dan `--disable-plugins` di WebView args yang menyebabkan Live2D rendering rusak pada beberapa GPU.
- Fix video wallpaper berat tanpa optimasi — sekarang optimize async di background tanpa block UI.
- Fix `Loader cat.json` duplikat root JSON object — file dipotong ke 6374 baris yang valid.

### 🔧 Changes

- `ChargerBatterynotif.core` scope dipersempit: hanya handle Charging, Unplugged, Full. Low & Critical tetap di `zeromix.Battery`.
- Built-in WAV sounds untuk Charger Notif: `charging.wav` (chime naik E5→G5), `unplug.wav` (chime turun), `full.wav` (triple chime C5→E5→G5), `low.wav` (440Hz), `critical.wav` (double beep 300Hz).
- `zeromix.Battery/Maskot/` PNG direferensi langsung dari `ChargerBatterynotif.core` — tidak duplikat file.
- Virtual Assistant WebView: hapus `--disable-gpu-vsync` dan `--disable-plugins` dari browser args, tambah `--js-flags=--max-old-space-size=128`.
- Virtual Assistant `powerPreference` diubah ke `"low-power"` untuk spek rendah, resolusi di-cap `Math.min(devicePixelRatio, 1.5)`.
- `live2d-viewer.html` di-rewrite: loading guard, GC pause, auto-motion loop, body motion per karakter.
- `ZeroMix.csproj`: tambah `LottieSharp` NuGet, copy `*.json` dan `*.wav` dari `Tools\Plugins\**` ke output.
- Language ComboBox di MainWindow.xaml: tambah opsi `中文` dan `한국어`.

---

## [v5.4.0] - 2026-04-09

### ✨ Features
- **WDM Start Menu Glass**: Efek dark acrylic blur pada Start Menu (Windows 10 & 11).
- **WDM Notification Panel Glass**: Efek dark acrylic blur pada Action Center / Notification Panel.
- **WDM Persistence**: State WDM disimpan ke `wdm.json`, di-restore otomatis saat ZeroShell dibuka kembali.
- **Wallpaper Session**: State video wallpaper disimpan ke `wallpaper_session.json`, auto-restore saat app dibuka.

### 🐛 Bug Fixes
- Fix File Explorer glass — hapus font injection yang menyebabkan font bertolak belakang.
- Fix Start Menu glass hanya apply ke parent — sekarang apply ke semua child windows.
- Fix Notification Panel tidak berubah — tambah `ControlCenterWindow` class untuk Windows 11.
- Fix "Failed to save language preference" — `language.ini` dipindah ke `%AppData%\ZeroMix\`.
- Fix `Mutex.ReleaseMutex()` crash saat shutdown.
- Fix Plugins & About view overflow ke kanan saat window dikecilkan.
- Fix CI/CD: `v5.4.0` not valid version string — strip prefix `v` sebelum `-p:Version`.
- Fix Inno Setup output filename tidak match tag — `#define AppVersion` di-override via `/D`.

### 🔧 Changes
- `ApplyBlur` pakai `ACCENT_ENABLE_ACRYLICBLURBEHIND` dengan alpha tinggi agar warna hitam dominan.
- WDM pulse timer cover semua 4 elemen (Explorer, Taskbar, Start Menu, Notif), interval 2 detik.
- WDM section dihapus dari gear settings — hanya via `!wdm`.
- `!wdm` menu diperluas dari 5 opsi menjadi 7 opsi.
- Setup.iss: exclude video wallpaper & duplicate logo dari installer.
- Plugins & Recorder view: `UniformGrid` → `WrapPanel` untuk responsive layout.
- Release script: tidak timpa entry CHANGELOG yang sudah ditulis manual.
- GitHub Release notes diambil langsung dari `Docs/CHANGELOG.md`.

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
