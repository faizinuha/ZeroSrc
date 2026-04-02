# Changelog Project ZeroMix - v5.1.2 (Standard Industrial)

## ✨ AI Control & Search Assistant
- **Ask AI Mode Overlay**: Menambahkan tombol "Ask AI" di Search Box. Kini Kakak bisa bertanya apa saja ke asisten langsung dari tampilan pencarian.
- **Smart Intent Detection**: 
  - `carikan foto [nama]` -> Otomatis membuka hasil pencarian gambar.
  - `beli barang` / `shopee` -> Langsung mengarahkan ke marketplace favorit (Shopee/Tokopedia).
- **Fallback to AI**: Pertanyaan umum akan otomatis dijawab oleh AI Frieren dengan gaya bicaranya yang khas.

## 🎙️ Virtual Assistant Upgrade
- **Text-to-Speech (TTS)**: Asisten tidak lagi membisu! Sekarang Frieren/Fern/HuoHuo bisa bicara langsung menjawab pertanyaan Kakak.
- **Lip-Sync Animation**: Gerakan mulut karakter kini sinkron (lip-sync) dengan suara yang dihasilkan agar terasa lebih hidup.
- **Memory Optimization**: Memperkenalkan pembersihan cache yang lebih agresif pada engine Live2D untuk mencegah lonjakan RAM (fix memory leak) saat karakter aktif dalam waktu lama.

## 📹 Recorder Engine Stability
- **Fix Force Close**: Memperbaiki issue crash saat tombol rekam diklik pada beberapa tipe GPU.
- **Hardware Encoder Fallback**: Sistem sekarang lebih cerdas dalam mendeteksi encoder GPU (NVENC/QSV/AMF). Jika gagal, sistem otomatis beralih ke CPU tanpa membuat aplikasi hang.
- **DXGI Resource Management**: Memastikan pengambilan gambar layar (Screen Capture) tidak membebani driver video Windows.

## 🛠️ Dashboard & System Health
- **Optimasi System Health**: Proses scanning sistem jauh lebih ringan dan tidak menyebabkan UI lag.
- **Fix Temp Cleanup**: Pembersihan folder `%temp%` kini lebih stabil dan mencakup lebih banyak folder sampah sistem.
- **Version Stamp**: Update internal sistem ke versi **v5.1.2**.

---
*ZeroMix - Smart Desktop Launcher v5.1.2*
