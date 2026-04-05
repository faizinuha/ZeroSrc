[🇺🇸 English](README.md) | 🇮🇩 Indonesia

<div align="center">

<img src="Assets/zeromix-high-resolution-logo-transparent.png" alt="ZeroMix Logo" width="120"/>

# ZeroMix

[![Release](https://img.shields.io/github/v/release/faizinuha/ZeroMix?color=00D9FF&style=flat-square)](https://github.com/faizinuha/ZeroMix/releases)
[![License](https://img.shields.io/badge/license-MIT-blue.svg?style=flat-square)](LICENSE.txt)
[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/badge/NuGet-ZeroMix.PluginSDK-004880?style=flat-square&logo=nuget)](https://www.nuget.org/packages/ZeroMix.PluginSDK)

[Download](https://github.com/faizinuha/ZeroMix/releases) · [Demo](https://www.youtube.com/watch?v=y9yz7ZPh_Bo) · [Dokumentasi](https://github.com/faizinuha/ZeroMix/wiki) · [Plugin Guide](Docs/PLUGIN_GUIDE.md) · [Diskusi](https://github.com/faizinuha/ZeroMix/discussions)

Suite utilitas Windows untuk performa dan estetika — ringan, cepat, dan bisa dikustomisasi.

</div>

---

## Fitur Utama

| Fitur | Deskripsi |
| :--- | :--- |
| **Lua Plugin System** | Buat dan bagikan plugin menggunakan skrip Lua |
| **ZeroRecord** | Rekam layar dengan zoom dinamis mengikuti kursor |
| **Nexus Translate** | Terjemahan real-time dengan perlindungan anti-crash |
| **Fake Sleep Mode** | Layar selalu menyala dengan deteksi idle dan hotkey |
| **Virtual Assistant** | AI dengan karakter Live2D (Frieren, Fern, Huohuo) via Groq API |
| **System Monitor** | Pantau CPU, RAM, dan Disk secara real-time |
| **Ghost Taskbar** | Buat taskbar transparan secara instan |
| **Video Wallpaper** | Wallpaper animasi berbasis video performa tinggi |
| **ZeroShell** | Terminal pro dengan Powerline prompt dan Arch-style fetch |
| **Search Overlay** | Pencarian cepat (`Alt + Space`) untuk aplikasi, file, dan web |
| **Memory Optimizer** | Pembersihan memori otomatis di background |
| **Desktop Widgets** | Jam dan widget lainnya untuk desktop |

---

## Plugin System

ZeroMix menggunakan Lua sebagai bahasa scripting untuk plugin — tidak perlu kompilasi ulang.

**Plugin bawaan:**
- `zeromix.Battery` — Monitor baterai real-time
- `zeromix.Translate` — Layanan terjemahan
- `zeromix.weather` — Integrasi data cuaca

**Cara membuat plugin:**
1. Gunakan Plugin Creator yang sudah ada di dalam ZeroMix
2. Tulis skrip Lua di folder `Plugins/user.*`
3. Plugin langsung aktif tanpa perlu restart

**Untuk developer C#:**
```bash
dotnet add package ZeroMix.PluginSDK
```

📖 [Plugin Guide lengkap →](Docs/PLUGIN_GUIDE.md)

---

## Instalasi

Download dari [GitHub Releases](https://github.com/faizinuha/ZeroMix/releases).

| Metode | Keterangan |
| :--- | :--- |
| **Bootstrap Installer** | Kecil (~10MB), download versi terbaru otomatis |
| **Setup EXE** | Installer offline lengkap |
| **Portable ZIP** | Tidak perlu install, langsung jalankan |

Setelah install, cari ikon ⚡ di system tray. ZeroMix akan minta izin untuk berjalan saat startup pada pertama kali dijalankan.

---

## Keyboard Shortcuts

| Shortcut | Aksi |
| :--- | :--- |
| `Alt + S` | Toggle Fake Sleep Mode |
| `Ctrl + Space` | Buka Search Overlay |
| `Ctrl + Q` | Toggle Dashboard |
| `Ctrl + R` | Refresh System Metrics |
| `Esc` | Tutup overlay aktif |

**Ask AI (di Search Overlay):**
- `Carikan sepatu murah` → otomatis buka pencarian
- `Carikan baju di Tokopedia` → otomatis buka Tokopedia
- `Carikan HP murah di Shopee` → otomatis buka Shopee

---

## Teknologi

- **UI** — WPF + Windows Forms
- **Core** — .NET 9.0 (C#), self-contained
- **Scripting** — MoonSharp (Lua)
- **Graphics** — Vortice (DirectX)
- **Video** — FFmpeg
- **Packaging** — Inno Setup

---

## Kontribusi

- Bug? → [Buka Issue](https://github.com/faizinuha/ZeroMix/issues)
- Ide? → [Mulai Diskusi](https://github.com/faizinuha/ZeroMix/discussions)
- Mau coding? → [Contributing Guide](CONTRIBUTING.md)

---

## Kredit

**Video atmosfer:**
- [Hans](https://pixabay.com/id/users/hans-2/) — Rainy City at Night (Pixabay)
- [Nicky](https://pixabay.com/id/users/nickype-10327513/) — Nature & Garden Ambience (Pixabay)
- [Andreas](https://pixabay.com/id/users/adege-4994132/) — Cinematic Scenery (Pixabay)

**Model Live2D:**
- Frieren & Fern oleh [kyokiStudio](https://kyoki.booth.pm/) — Booth.pm
- Huohuo oleh [bailyovo](https://bailyovo.booth.pm/) — Booth.pm *(fan creation Honkai: Star Rail, hak cipta miHoYo)*

> [!IMPORTANT]
> **Kebijakan Penghapusan (Removal Policy):** Jika Model ini tidak diperbolehkan untuk digunakan dalam aplikasi, mohon segera hubungi kami melalui Email: **Rozakadm@gmail.com**. Kami akan segera menghapus model tersebut untuk menghormati hak pemilik dan memastikan kenyamanan bagi semua pihak. Terima kasih.

> [!NOTE]
> Jika ingin mendownload model, harap gunakan **situs resmi yang telah kami sediakan**. Mohon hargai kerja keras pembuat model. Jangan menggunakan model ini untuk **komersial atau dijual tanpa izin**. Terima kasih atas pengertiannya! 🥰

> [!NOTE]
> Penggunaan model ini sepenuhnya menjadi tanggung jawab pengguna.
> Pengembang aplikasi tidak menyediakan izin komersial apa pun atas model ini dan hanya meneruskan ketentuan dari pembuat asli.

> [!WARNING]
> Pengembang aplikasi tidak bertindak sebagai pemberi lisensi model.
> Penggunaan yang melanggar ketentuan pembuat asli merupakan tanggung jawab pengguna.

---


## Dukung Project

[![Trakteer](https://img.shields.io/badge/Trakteer-Support_Dev-red?style=flat-square)](https://trakteer.id/MyCici)
[![Sociabuzz](https://img.shields.io/badge/Sociabuzz-Support_Dev-EE4B2B?style=flat-square)](https://sociabuzz.com/zuax)

Dibuat dengan ❤️ oleh **Faizinuha** dan komunitas. **ZeroMix © 2025-2026**
