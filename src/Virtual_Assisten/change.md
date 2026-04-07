# Changelog Project ZeroMix

---

## 🚀 v5.3.1 — Latest

### ✨ Features
- **Sleep Mode Settings Panel**: Settings dipindah ke panel navigasi dalam MainWindow — konsep seperti Settings Windows 11.
- **Sleep Mode Music**: Opsi musik opsional (MP3/WAV/FLAC/OGG/M4A) dengan volume slider.
- **Sleep Mode No Delay**: Custom background (gambar/video) langsung tampil tanpa delay.

### 🐛 Bug Fixes
- Fix ParserError di ZeroShell prompt — ganti `-Command` ke `-EncodedCommand` (Base64).
- Fix ZeroShell crash saat klik gear — `TerminalSettingsWindow` dihapus, kembali ke `SettingsOverlay` inline.
- Fix Terminal Customizer tidak bisa scroll — tambah `ScrollViewer` di konten settings.
- Fix tombol BROWSE Terminal Wallpaper tidak bisa diklik — tambah `IsHitTestVisible="False"` pada TextBlock.
- Fix Search Overlay kotak-kotak setelah penggunaan ke-2.
- Fix SleepMode custom background tidak tersimpan.
- Fix ZeroMix.recorder video output hitam.
- Fix video wallpaper tidak bisa diputar.
- Fix uninstaller tidak hapus registry dan startup shortcut.
- Fix window list recorder tidak lengkap — explorer tidak di-skip, icon fetch dipisah.

### 🔧 Changes
- Hapus `SleepSettingsWindow.xaml/.cs`, diganti panel `SleepContent` di MainWindow.
- Hapus `TerminalSettingsWindow.xaml/.cs`, kembali pakai `SettingsOverlay` inline di ZeroShellWindow.
- Virtual Assistant: hapus tombol mic dari UI overlay.
- Virtual Assistant: eye tracking 200ms → 300ms, vision timer 30s → 60s.
- Wallpapers: video langsung play tanpa pre-process ffmpeg.
- Uninstaller: auto kill, hapus registry, startup shortcut, LocalAppData.
- Window Picker: ukuran minimum 100px → 50px, window minimize tidak di-skip.

---

## 🚀 v5.2.9 — Upcoming

### ✨ Planned Features
- **Multi-Monitor Support**: Rekam atau capture layar dari monitor lebih dari satu sekaligus.
- **Streaming Mode**: Dukungan output langsung ke platform streaming (OBS-compatible).
- **Advanced Audio Mixer**: Kontrol volume per-source (mic, system, game) secara terpisah.
- **Plugin Marketplace**: Browser plugin langsung dari dalam aplikasi.
- **Cloud Sync Settings**: Sinkronisasi konfigurasi antar perangkat via cloud.

---

## ✅ v5.2.5 — Latest

### ✨ Features
- **Sleep Mode Music**: Opsi musik opsional (MP3/WAV/FLAC/OGG/M4A) — bisa dipakai bareng background apapun, ada volume slider.
- **Sleep Mode No Delay**: Custom background (gambar/video) langsung tampil tanpa delay.

### 🐛 Bug Fixes
- Fix Search Overlay kotak-kotak setelah penggunaan ke-2 (root cause: window di-reuse, sekarang selalu buat instance baru).
- Fix SleepMode custom background tidak tersimpan saat settings dibuka ulang.
- Fix ZeroMix.recorder video output hitam (tambah -vsync cfr flag ke FFmpeg).
- Fix video wallpaper tidak bisa diputar — error handling diperbaiki.
- Fix uninstaller tidak hapus registry context menu dan startup shortcut.

### 🔧 Changes
- Virtual Assistant: hapus tombol mic dari UI overlay (tetap bisa diakses dari halaman list model).
- Virtual Assistant: eye tracking 200ms → 300ms, vision timer 30s → 60s (hemat CPU/RAM ~10-15MB).
- Wallpapers: hapus pre-process ffmpeg yang tidak perlu, video langsung play.
- Uninstaller: auto kill ZeroMix.exe, hapus registry, startup shortcut, LocalAppData.
- Sleep Mode: BitmapImage di-Freeze() setelah load, DispatcherTimer pakai Background priority.

---

## ✅ v5.2.3 — Latest

### ✨ Features
- **AOD Style Picker**: Sleep Mode kini punya 5 style Always-On Display — Minimal Clock, Neon Glow, Analog, Date Focus, Blank.
- **Analog Clock AOD**: Jarum jam real-time dengan anti burn-in (posisi geser otomatis tiap 10 detik).
- **Sidebar Update Badge**: Dot merah pulsing di icon System Info saat ada update tersedia.
- **Auto Silent Update Check**: Cek update otomatis saat startup tanpa popup.

### 🐛 Bug Fixes
- Fix double instance — klik icon desktop saat app sudah jalan di tray tidak lagi buka instance baru.
- Fix Search Overlay (Ctrl+Space) lambat — overlay sekarang di-preload saat startup.
- Fix "Show Terminal" masih muncul di tray menu.
- Fix installer popup "applications using files" — ZeroMix di-close otomatis sebelum update.
- Fix Sleep Mode overlay crash saat laptop wake up dari sleep/hibernate (PowerModeChanged event).

### 🔧 Changes
- Sleep Mode settings: section Visuals diganti jadi AOD style card picker.
- Brightness slider sekarang tampilkan persentase langsung.
- CHANGELOG auto-generate dari git commits + file yang berubah via release script.
- Release script: ForceBuild baca `-CommitMessage` parameter langsung untuk feat/fix.

---

## ✅ v5.2.2 — Published

### ✨ Features
- **Parallel Chunked Download**: Updater script pakai 8 koneksi paralel (IDM-style).
- **Progress Bar Updater**: Tampilkan speed (KB/s) dan ETA saat download update.

### 🐛 Bug Fixes
- Fix download lambat di zeromix-update.ps1 — ganti Invoke-WebRequest ke HttpWebRequest dengan buffer 128KB.
- Fix path Downloads yang salah di beberapa sistem.
- Fix installer popup minta close app sebelum update.

### 🔧 Changes
- README ditambah versi Indonesia (README.id.md).

---

## ✅ v5.2.1 — Published

### ✨ Features
- Publish SDK ke NuGet.org (`ZeroMix.PluginSDK`).
- Add PluginSDK dan refactor `IZeroMixHost`.

### 🐛 Bug Fixes
- Fix ZeroMix.Recorder crash pada beberapa konfigurasi GPU.
- Fix Virtual Assistant thumbnail tidak muncul.
- Fix Bitrate control slider untuk pilih kualitas recording.

---

## 🚀 v5.2.0 — Upcoming

### ✨ Planned Features
- **Multi-Monitor Support**: Rekam atau capture layar dari monitor lebih dari satu sekaligus.
- **Streaming Mode**: Dukungan output langsung ke platform streaming (OBS-compatible).
- **Advanced Audio Mixer**: Kontrol volume per-source (mic, system, game) secara terpisah.
- **Plugin Marketplace**: Browser plugin langsung dari dalam aplikasi.
- **Cloud Sync Settings**: Sinkronisasi konfigurasi antar perangkat via cloud.

---

## ✅ v5.1.9 — Published

### ✨ Features
- **Game Mode Translator**: Mode terjemahan khusus game via Clipboard Paste (Ctrl+V).
- **CI/CD Sign Fix**: Perbaikan pipeline signing executable di GitHub Actions.

### 🐛 Bug Fixes
- Fix `osslsigncode` tidak dikenali setelah `choco install` pada Windows runner.
- Fallback ke path hardcoded `C:\ProgramData\chocolatey\bin\`.

---

## ✅ v5.1.5 — Published

### ✨ Features
- **Ask AI Mode Overlay**: Tombol "Ask AI" di Search Box.
- **Smart Intent Detection**: carikan foto, beli barang, shopee → otomatis buka.
- **Text-to-Speech (TTS)**: Frieren/Fern/HuoHuo bisa bicara.
- **Lip-Sync Animation**: Gerakan mulut sinkron dengan suara.

### 🐛 Bug Fixes
- Fix Force Close saat tombol rekam diklik pada beberapa GPU.
- Fix Temp Cleanup lebih stabil.

---

## 🔧 v5.1.3 — Fix Release

### 🐛 Bug Fixes
- Fix crash recorder pada GPU tertentu.
- Fix memory leak pada engine Live2D.
- Fix pembersihan `%temp%` yang tidak lengkap.
- Fix encoder fallback yang menyebabkan hang saat NVENC tidak tersedia.

---

## ✅ v5.1.2 — Standard Industrial

### ✨ Features
- AI Control & Search Assistant (dasar).
- Virtual Assistant upgrade awal (TTS & Lip-Sync prototype).
- Recorder Engine stability improvements.
- Dashboard & System Health optimization.

---

*ZeroMix - Smart Desktop Launcher*
