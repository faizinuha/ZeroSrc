# Changelog Project ZeroMix

---

## 🚀 v5.2.3 — Latest

### ✨ Features
- **AOD Style Picker**: Sleep Mode kini punya 5 style Always-On Display — Minimal Clock, Neon Glow, Analog, Date Focus, Blank.
- **Analog Clock AOD**: Jarum jam real-time dengan anti burn-in (posisi geser otomatis tiap 10 detik).
- **Sidebar Update Badge**: Dot merah pulsing di icon System Info saat ada update tersedia.
- **Auto Silent Update Check**: Cek update otomatis saat startup tanpa popup.

### 🐛 Bug Fixes
- Fix double instance — klik icon desktop saat app sudah jalan di tray tidak lagi buka instance baru.
- Fix Search Overlay (Ctrl+Space) lambat — overlay sekarang di-preload saat startup, tidak init ulang tiap kali dipanggil.
- Fix "Show Terminal" masih muncul di tray menu.
- Fix installer popup "applications using files" — ZeroMix di-close otomatis sebelum update.

### 🔧 Changes
- Sleep Mode settings: section Visuals diganti jadi AOD style card picker.
- Brightness slider sekarang tampilkan persentase langsung.

---

## ✅ v5.2.2 — Published

### ✨ Features
- **Parallel Chunked Download**: Updater script pakai 8 koneksi paralel (IDM-style) untuk download lebih cepat.
- **Progress Bar Updater**: Tampilkan speed (KB/s) dan ETA saat download update.

### 🐛 Bug Fixes
- Fix download lambat di zeromix-update.ps1 — ganti Invoke-WebRequest ke HttpWebRequest dengan buffer 128KB.
- Fix path Downloads yang salah di beberapa sistem.
- Fix installer popup minta close app sebelum update.

### 🔧 Changes
- README ditambah versi Indonesia (README.id.md).
- CHANGELOG sekarang auto-generate dari git commits + file yang berubah via release script.

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
- **Game Mode Translator**: Mode terjemahan khusus game via Clipboard Paste (Ctrl+V), kompatibel dengan DirectInput/RawInput game chat.
- **CI/CD Sign Fix**: Perbaikan pipeline signing executable di GitHub Actions (osslsigncode PATH refresh).

### 🐛 Bug Fixes
- Fix `osslsigncode` tidak dikenali di step berikutnya setelah `choco install` pada Windows runner.
- Fallback ke path hardcoded `C:\ProgramData\chocolatey\bin\` jika PATH belum ter-refresh.

---

## ✅ v5.1.5 — Published

### ✨ Features
- **Ask AI Mode Overlay**: Tombol "Ask AI" di Search Box untuk bertanya langsung dari tampilan pencarian.
- **Smart Intent Detection**:
  - `carikan foto [nama]` → Otomatis membuka hasil pencarian gambar.
  - `beli barang` / `shopee` → Langsung mengarahkan ke marketplace favorit.
- **Fallback to AI**: Pertanyaan umum dijawab otomatis oleh AI Frieren.
- **Text-to-Speech (TTS)**: Frieren/Fern/HuoHuo bisa bicara langsung menjawab pertanyaan.
- **Lip-Sync Animation**: Gerakan mulut karakter sinkron dengan suara.

### 🐛 Bug Fixes
- Fix Force Close saat tombol rekam diklik pada beberapa tipe GPU.
- Fix Temp Cleanup — pembersihan `%temp%` lebih stabil.

### 🚀 Improvements
- Hardware Encoder Fallback: NVENC/QSV/AMF gagal → otomatis ke CPU tanpa hang.
- DXGI Resource Management lebih efisien.
- Memory Optimization: cache Live2D dibersihkan lebih agresif.
- System Health scan lebih ringan.

---

## 🔧 v5.1.3 — Fix Release

### 🐛 Bug Fixes
- Fix crash recorder pada GPU tertentu saat pertama kali dijalankan.
- Fix memory leak pada engine Live2D saat karakter aktif lama.
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


### ✨ Planned Features
- **Multi-Monitor Support**: Rekam atau capture layar dari monitor lebih dari satu sekaligus.
- **Streaming Mode**: Dukungan output langsung ke platform streaming (OBS-compatible).
- **Advanced Audio Mixer**: Kontrol volume per-source (mic, system, game) secara terpisah.
- **Plugin Marketplace**: Browser plugin langsung dari dalam aplikasi.
- **Cloud Sync Settings**: Sinkronisasi konfigurasi antar perangkat via cloud.

---

## ✅ v5.1.9 — Published

### ✨ Features
- **Game Mode Translator**: Mode terjemahan khusus game via Clipboard Paste (Ctrl+V), kompatibel dengan DirectInput/RawInput game chat.
- **CI/CD Sign Fix**: Perbaikan pipeline signing executable di GitHub Actions (osslsigncode PATH refresh).

### 🐛 Bug Fixes
- Fix `osslsigncode` tidak dikenali di step berikutnya setelah `choco install` pada Windows runner.
- Fallback ke path hardcoded `C:\ProgramData\chocolatey\bin\` jika PATH belum ter-refresh.

---

## ✅ v5.1.5 — Published

### ✨ Features
- **Ask AI Mode Overlay**: Tombol "Ask AI" di Search Box untuk bertanya langsung dari tampilan pencarian.
- **Smart Intent Detection**:
  - `carikan foto [nama]` → Otomatis membuka hasil pencarian gambar.
  - `beli barang` / `shopee` → Langsung mengarahkan ke marketplace favorit (Shopee/Tokopedia).
- **Fallback to AI**: Pertanyaan umum dijawab otomatis oleh AI Frieren dengan gaya bicaranya yang khas.
- **Text-to-Speech (TTS)**: Frieren/Fern/HuoHuo kini bisa bicara langsung menjawab pertanyaan.
- **Lip-Sync Animation**: Gerakan mulut karakter sinkron dengan suara yang dihasilkan.

### 🐛 Bug Fixes
- Fix Force Close saat tombol rekam diklik pada beberapa tipe GPU.
- Fix Temp Cleanup — pembersihan `%temp%` lebih stabil dan mencakup lebih banyak folder sampah.

### 🚀 Improvements
- Hardware Encoder Fallback: NVENC/QSV/AMF gagal → otomatis ke CPU tanpa hang.
- DXGI Resource Management: Screen capture tidak membebani driver video Windows.
- Memory Optimization: Cache Live2D dibersihkan lebih agresif, mencegah memory leak.
- System Health scan lebih ringan, tidak menyebabkan UI lag.

---

## 🔧 v5.1.3 — Fix Release

### 🐛 Bug Fixes
- Fix crash recorder pada GPU tertentu saat pertama kali dijalankan.
- Fix memory leak pada engine Live2D saat karakter aktif dalam waktu lama.
- Fix pembersihan `%temp%` yang tidak lengkap pada beberapa konfigurasi sistem.
- Fix encoder fallback yang menyebabkan aplikasi hang saat NVENC tidak tersedia.

### 🚀 Improvements
- Stabilitas DXGI capture ditingkatkan.
- Optimasi minor pada System Health scanner.

---

## ✅ v5.1.2 — Standard Industrial

### ✨ Features
- AI Control & Search Assistant (dasar).
- Virtual Assistant upgrade awal (TTS & Lip-Sync prototype).
- Recorder Engine stability improvements.
- Dashboard & System Health optimization.

---

*ZeroMix - Smart Desktop Launcher*
