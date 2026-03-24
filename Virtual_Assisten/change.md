# ZeroMix Release Notes v5.1.0

## 🌟 Major Highlights: Aurora Glass UI Redesign
Transformasi total estetika aplikasi ke arah yang lebih premium, modern, dan futuristik.

### [UI/UX Overhaul]
- **Glassmorphism Design Total**: Lapisan antarmuka sekarang menggunakan tema **Aurora Glass** (Semi-transparent dark with soft white glass edges).
- **Collapsible Sidebar (☰)**: Penambahan menu samping yang bisa dikecilkan menggunakan animasi halus untuk area kerja yang lebih luas.
- **Brand Refresh**: Penghapusan logo besar di bagian atas untuk tampilan yang lebih bersih (*Clean Titles*).
- **Standard Modern Controls**: Redesain tombol kontrol jendela (Close/Min/Max) yang menyatu dengan tema kaca namun tetap fungsional standar.

### [Virtual Assistant Integration 2.0]
- **Pre-Launch Settings Dashboard**: Penambahan kontrol **Interaction Language** dan **Mic Selection** langsung di kartu asisten sebelum diaktifkan.
- **Auto-Sync Engine**: Pengaturan bahasa dan mikrofon dari dashboard utama sekarang otomatis tersinkronisasi ke karakter asisten (Frieren, Fern, Huohuo) saat *activation*.
- **Smart Activation**: Mic secara otomatis menyala (jika diaktifkan di dashboard) begitu model 3D asisten selesai loading.

### [🌙 NEW: Fake Sleep Mode / Always-On Display]
Fitur baru yang meniru Always-On Display pada smartphone. Menampilkan overlay fullscreen bertema gelap dengan animasi ringan saat PC tidak digunakan.

#### Fitur Lengkap:
- **Multi-Trigger System (bisa kombinasi)**:
  - ✅ **Manual**: Langsung aktifkan overlay dari tombol "Start Sleep Mode" di settings.
  - ✅ **Idle Detection**: Otomatis aktif saat tidak ada input selama waktu yang ditentukan (default: 60 detik). Menggunakan Win32 API `GetLastInputInfo` untuk akurasi tinggi.
  - ✅ **Global Shortcut**: Aktifkan kapan saja dengan hotkey `Alt + S` (bisa dikustomisasi). Bekerja bahkan saat aplikasi di-minimize ke system tray.
  - 💡 **Bisa pilih lebih dari satu** — misalnya Idle + Shortcut aktif bersamaan.

- **Konfigurasi Exit (Cara Keluar dari Overlay)**:
  - ✅ Exit saat **Mouse bergerak** (opsional, bisa dimatikan)
  - ✅ Exit saat **Mouse diklik** (opsional)
  - ✅ Exit saat **Keyboard ditekan** (opsional)
  - 🛡️ **Input Protection 500ms**: Mencegah overlay langsung tertutup saat baru muncul karena event input sisa.

- **Kustomisasi Visual**:
  - ✅ **Jam Digital**: Tampilkan/sembunyikan jam dengan animasi pulsing anti-burn-in.
  - ✅ **Pixel Character**: Karakter dekoratif kecil dengan animasi lompat.
  - ✅ **Neo-Glow Effect**: Efek cahaya neon ringan (opsional).
  - ✅ **Brightness Slider**: Kontrol kecerahan overlay dari 20% hingga 100%.
  - 🎨 **Anti Burn-in**: Elemen visual bergerak perlahan secara acak setiap 10 detik.

- **Fitur Advanced**:
  - ✅ **Hide Notifications**: Sembunyikan notifikasi saat sleep mode aktif.
  - ✅ **Ultra-Low CPU Mode**: Matikan semua animasi untuk penghematan daya ekstrem (hanya update jam per detik).
  - ✅ **Auto-Disable on Low Battery**: Otomatis nonaktifkan sleep mode jika baterai laptop di bawah 20% dan tidak sedang dicharge.

- **UI Settings Window**:
  - 🖥️ Jendela pengaturan modern dengan desain **Glassmorphism** yang konsisten dengan tema aplikasi.
  - Semua opsi di-*persist* dan bisa diubah kapan saja.
  - Tombol dashboard dipersempit menjadi satu: **"Sleep Mode Settings"** (menggantikan tombol Test + Stop yang lama).

---
*"Build with heart for the community. ZeroMix 5.1 is more than an update, it's a new standard."* - **Antigravity AI Assistant**
