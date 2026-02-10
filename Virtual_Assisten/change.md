# 🚀 ZeroMix v2.7.0-Beta - The "Companion & Security" Update

Update besar kali ini membawa teman baru ke desktop Kakak, sekaligus memperkuat keamanan internal aplikasi agar tetap ringan namun sulit ditembus.

### ✨ Fitur Baru (New Features)

- **🎭 Virtual Assistant 2.0 Integration**:
  - **New Model: HuoHuo (Honkai Star Rail)**: Karakter baru telah ditambahkan ke dalam koleksi.
  - **Model Frieren & Fern**: Karakter ikonik sekarang siap menemani Kakak di pojok layar.
  - **Auto-Motion System**: Karakter kini lebih "hidup" dengan animasi otomatis setiap 5 detik.
  - **Interactive Chat**: Klik pada asisten untuk memunculkan bubble chat berisi dialog unik.
  - **Global Eye Tracking (Fixed)**: Mata asisten akan mengikuti kursor mouse Kakak dengan logika arah yang lebih alami (Up is Up!).
- **🎴 Advanced Assistant Dashboard**:
  - **Master Toggle Switch**: Saklar utama untuk menghidupkan/mematikan asisten secara total guna menghemat RAM.
  - **Compact Pro Cards**: Desain kartu karakter yang lebih mungil, rapi, dan responsif.
  - **Smart State Persistence**: Mengingat karakter terakhir yang Kakak gunakan.

### 🛡️ Security & Performance (Under the Hood)

- **🔒 Zero-Theft Asset Protection**: Semua model Live2D kini dienkripsi di dalam file `.exe` (Embedded Resource). Tidak ada lagi folder aset yang bisa di-copy orang lain.
- **🚀 Resource Interceptor**: Menggunakan sistem _Virtual Resource Serving_ di memory untuk memuat aset tanpa meninggalkan jejak file fisik di harddisk.
- **📦 Compressed Single-File**: Aplikasi kini dibungkus dalam satu file `.exe` yang sudah dikompresi (Self-Contained), tanpa perlu install .NET runtime tambahan.
- **🧹 Pro Uninstaller**: Proses uninstall kini lebih bersih, cepat (Silent Process Kill), dan minim pop-up yang mengganggu.

### 🎨 UI & UX Improvements

- **🛠️ Navigation Overhaul**: Menghapus tab redundan untuk tampilan sidebar yang lebih minimalis.
- **📦 Unified Credits**: Bagian Donasi (Trakteer) dan Credit Asset sekarang terpusat di halaman About.
- **🧼 Smooth Transitions**: Efek _Fade-in_ saat memuat model Live2D untuk menghilangkan kedipan kotak hitam.

### 🔧 Bug Fixes & Stability

- **Fixed**: Error build `NETSDK1175` terkait fitur Trimming pada Windows Forms.
- **Fixed**: Kesalahan nama filter pada WebView2 API yang menyebabkan aplikasi crash.
- **Fixed**: Masalah rendering pada komponen `WallpapersView` saat berpindah tab secara cepat.
- **Improved**: Optimalisasi memori saat asisten dimatikan (Clear from Task Manager).

---

### 📥 Cara Update:

1. Unduh installer `ZeroMix-v2.7.0-beta-Setup.exe`.
2. Jalankan installer (installer akan otomatis mendeteksi dan memperbarui versi lama).
3. Buka menu **Assistant** dan aktifkan saklar **ONLINE** untuk memulai!

---

**Note dari Pengembang:**
Update ini adalah langkah besar menuju ZeroMix yang lebih personal. Jika Kakak menyukai update ini, dukung kami terus melalui [Trakteer](https://trakteer.id/MyCici). Terima kasih, Kak! 🥰
