# 🚀 Quick Release Guide

## Cara Release Versi Baru (Super Mudah!)

### 1️⃣ Satu Command Saja

```bash
.\Scripts\release-version.bat 5.1.6
```

**Done!** Otomatis:
- ✅ Update versi di semua file
- ✅ Commit & push ke GitHub
- ✅ Create tag `v5.1.6`
- ✅ Trigger build & release workflow

---

### 2️⃣ Cek Hasil

Buka: `https://github.com/[user]/[repo]/releases`

Tunggu 5-10 menit, release baru akan muncul dengan:
- `ZeroMix-v5.1.6-Setup.exe` (Installer)
- `ZeroMix-v5.1.6-Portable.zip` (Portable)

---

### 3️⃣ Monitor Build

Buka: `https://github.com/[user]/[repo]/actions`

Lihat workflow "🚀 Build & Release ZeroMix" sedang berjalan.

---

## 🔄 Version History

Setiap versi punya release sendiri:

```
v5.1.6 ← Latest
v5.1.4
v5.1.3
v5.1.2
```

Tidak numpuk! Setiap tag = 1 release baru.

---

## 🛠️ Commands Lain

```powershell
# Preview saja (tidak push)
.\Scripts\release-version.ps1 -NewVersion 5.1.6 -SkipPush

# Custom commit message
.\Scripts\release-version.ps1 -NewVersion 5.1.6 -CommitMessage "feat: new features"

# Build lokal dulu (test)
.\build\build-sign-release.ps1 -Version 5.1.6 -SkipUpload
```

---

## 📚 Dokumentasi Lengkap

- **Complete Guide:** [Docs/RELEASES_GUIDE.md](Docs/RELEASES_GUIDE.md)
- **Troubleshooting:** [Docs/RELEASE_TROUBLESHOOTING.md](Docs/RELEASE_TROUBLESHOOTING.md)
- **Scripts Documentation:** [Scripts/README.md](Scripts/README.md)
- **Build Guide:** [build/README.md](build/README.md)

---

**That's it! Simple kan? 😎**
