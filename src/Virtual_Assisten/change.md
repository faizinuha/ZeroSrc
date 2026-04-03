# Changelog Project ZeroMix

---

## 🚀 v5.2.0 — Upcoming

### ✨ Planned Features
- **Multi-Monitor Support**: Rekam atau capture layar dari monitor lebih dari satu sekaligus.
- **Streaming Mode**: Dukungan output langsung ke platform streaming (OBS-compatible).
- **Advanced Audio Mixer**: Kontrol volume per-source (mic, system, game) secara terpisah.
- **Plugin Marketplace**: Browser plugin langsung dari dalam aplikasi.
- **Cloud Sync Settings**: Sinkronisasi konfigurasi antar perangkat via cloud.

---

## ✅ v5.1.7 — Published

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
