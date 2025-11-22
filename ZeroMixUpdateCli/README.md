# ZeroMix Update Checker CLI

CLI tool untuk memeriksa update aplikasi ZeroMix dari GitHub Releases.

## 📋 Fitur

✅ Cek update Full Release dan Pre-Release
✅ Tampilkan changelog dari GitHub
✅ Download link langsung
✅ Tanya user sebelum download
✅ Link ke GitHub dan website jika tidak ada update
✅ Fully Bahasa Indonesia

## 🚀 Penggunaan

### Dari Terminal (Setelah Install)

```bash
# Cek update
zeromix cek update

# Atau menggunakan path lengkap
C:\Program Files\ZeroMix\bin\zeromix-update.exe cek update
```

### Output Contoh

**Jika ada update:**
```
🔍 Memeriksa pembaruan...

📦 Versi saat ini: 2.0.0

📡 Mengambil data dari GitHub...

──────────────────────────────────────────────
✅ ADA UPDATE TERSEDIA!
Versi Terbaru: v2.1.0
Versi Saat Ini: 2.0.0
Tipe: Full Release
Judul: ZeroMix v2.1.0 - Professional Release

📝 Changelog:
- Enhanced installer
- Video wallpaper support
- Improved performance
...

📥 Download:
  • ZeroMix-Setup-v2.1.0.exe
    https://github.com/faizinuha/ZeroMix/releases/download/v2.1.0/...

──────────────────────────────────────────────

❓ Apakah Anda ingin membuka halaman download? (Y/N)
> y

🌐 Membuka: https://github.com/faizinuha/ZeroMix/releases/tag/v2.1.0
✅ Browser sudah dibuka.
```

**Jika tidak ada update:**
```
🔍 Memeriksa pembaruan...

📦 Versi saat ini: 2.1.0

📡 Mengambil data dari GitHub...

──────────────────────────────────────────────
✅ APLIKASI SUDAH TERBARU
Versi: 2.1.0
Status: Anda menggunakan versi terbaru dari ZeroMix

──────────────────────────────────────────────

📍 Kunjungi untuk informasi lebih lanjut:
  🌐 GitHub Releases: https://github.com/faizinuha/ZeroMix/releases
  🌐 Website: https://zeromix.vercel.app
```

## 🔧 Instalasi

1. **Otomatis (Saat Install ZeroMix)**
   - CLI akan di-install ke `C:\Program Files\ZeroMix\bin\`
   - PATH akan di-update otomatis
   - Bisa langsung dipanggil dari terminal

2. **Manual**
   - Copy `zeromix-update.exe` ke `{ZeroMix Install Dir}\bin\`
   - Copy `zeromix.bat` ke directory yang ada di PATH
   - Atau add `{ZeroMix Install Dir}\bin\` ke PATH

## 📝 Release Channels

### Full Release (`zeromix`)
- Stable version
- Latest stable build
- Tag format: `v2.1.0`, `v2.0.0`, dll

### Pre-Release (`zeromix-latest`)
- Beta/Preview version
- Marked as pre-release di GitHub
- Testing purposes

## 🌐 GitHub Integration

Menggunakan GitHub API v3:
- `https://api.github.com/repos/faizinuha/ZeroMix/releases`
- No authentication required (public repo)
- Rate limit: 60 requests/hour per IP

## 💾 Version Detection

Versi saat ini dicek dari:
1. Registry: `HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ZeroMix`
2. Fallback: File version info dari `ZeroMix.exe`
3. Default: "Unknown"

## 🛠️ Development

### Build CLI

```bash
cd ZeroMixUpdateCli
dotnet publish -c Release -r win-x64 --self-contained
```

Output: `bin/Release/net8.0/win-x64/publish/zeromix-update.exe`

### Dependencies

- .NET 8.0 Runtime (self-contained included)
- No external NuGet packages (built-in System.Net.Http)
- Windows 10+ untuk path environment variable

## ⚙️ Configuration

Tidak ada file konfigurasi. CLI menggunakan hardcoded:
- GitHub API: `https://api.github.com/repos/faizinuha/ZeroMix/releases`
- Release names: `zeromix` (full), `zeromix-latest` (pre)

## 🐛 Troubleshooting

**"Command not found: zeromix"**
- Pastikan ZeroMix sudah di-install
- Buka terminal baru setelah install (refresh PATH)
- Manual add `C:\Program Files\ZeroMix\bin` ke PATH

**"Gagal terhubung ke GitHub"**
- Check internet connection
- GitHub server mungkin down
- Coba lagi nanti

**"Versi Unknown"**
- Aplikasi tidak terinstall di tempat default
- Registry entry tidak ada
- Edit registry: `HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ZeroMix` → `DisplayVersion`

## 📞 Support

- GitHub Issues: https://github.com/faizinuha/ZeroMix/issues
- Email: [contact email]
- Website: https://zeromix.vercel.app

---

**Version**: 1.0.0  
**Updated**: 2025-11-22  
**License**: MIT
