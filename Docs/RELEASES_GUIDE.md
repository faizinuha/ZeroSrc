# 🚀 ZeroMix Release Guide

Panduan lengkap untuk merilis versi baru ZeroMix ke GitHub Releases.

## 📋 Quick Start
git -C . add ZeroMix.Installer/MainWindow.xaml ZeroMix.Installer/MainWindow.xaml.cs ZeroMix.Installer/ZeroMix.Installer.csproj 2>&1

git -C . commit -m "feat(installer): redesign UI with Load.gif animation and improved layout

git -C . push origin HEAD 2>&1

### Cara Paling Mudah (Otomatis)

```

💡 Ringkasan Cepat
Jenis Perubahan	Versi (Contoh)	Tipe Commit
Perbaikan Bug	5.2.3 → 5.2.2	fix:
Fitur Baru	5.2.3 → 5.3.0	feat:
Perubahan Besar (Rusak)	5.2.3 → 6.0.0	feat!: atau fix!:

# Rilis biasa
.\Scripts\release-version.ps1 -NewVersion 5.2.3 -CommitMessage "Update version 5.2.3"

# Kalau ada fitur spesifik yang mau keliatan di changelog
.\Scripts\release-version.ps1 -NewVersion 5.2.3 -CommitMessage "feat: tambah settings page dan tweaks"

# Fix tanpa naik versi
.\Scripts\release-version.ps1 -NewVersion 5.2.3 -CommitMessage "fix: crash saat buka plugin" -ForceBuild

Contoh Alur Penomoran:
5.3.0-alpha.1 (Coba fitur baru)
5.3.0-beta.1 (Tes ke komunitas)
5.3.0-rc.1 (Persiapan rilis)
5.3.0 (Rilis Resmi )
```

```bash
# Dari root project
.\Scripts\release-version.bat 5.2.3
```

Atau dengan PowerShell:

```powershell
.\Scripts\release-version.ps1 -NewVersion 5.2.3
```

**Itu saja!** Script akan otomatis:
1. ✅ Update versi di semua file
2. ✅ Commit perubahan
3. ✅ Create tag `v5.2.3`
4. ✅ Push ke GitHub
5. ✅ Trigger workflow build & release

---

## 🔄 Workflow Lengkap

### 1. Update Versi & Release

```powershell
# Release versi baru
.\Scripts\release-version.ps1 -NewVersion 5.2.3

# Dengan custom commit message
.\Scripts\release-version.ps1 -NewVersion 5.2.3 -CommitMessage "feat: add new features"

# Preview saja (tidak push)
.\Scripts\release-version.ps1 -NewVersion 5.2.3 -SkipPush
```

Kamu:
  .\Scripts\release-version.ps1 -NewVersion 5.2.3
          │
          ▼
  Script push tag v5.2.3 ke GitHub
          │
          ▼
GitHub Actions (workflow):
  .github/workflows/build-release.yml
          │
          ├─ Build aplikasi
          ├─ Download FFMPEG
          ├─ Build installer (Inno Setup)
          ├─ Sign executable (osslsigncode)
          ├─ Create portable ZIP
          │
          ▼
  Upload ke GitHub Releases ← INI YANG UPLOAD
          │
          ▼
  https://github.com/faizinuha/ZeroMix/releases
  ✅ Release v5.2.3 muncul dengan:
     - ZeroMix-v5.2.3-Setup.exe
     - ZeroMix-v5.2.3-Portable.zip

### 2. Apa yang Terjadi di GitHub?

Setelah push tag, GitHub Actions otomatis:

```
📦 Build Workflow (build-release.yml)
├─ ⚙️  Setup .NET 9
├─ 📦 Restore & Publish
├─ 🎬 Download FFMPEG
├─ 🛠️  Build Inno Setup Installer
├─ 🔐 Sign dengan osslsigncode (jika cert tersedia)
├─ 📦 Create Portable ZIP
└─ 🚀 Upload ke GitHub Releases
```

### 3. Hasil Release

GitHub Releases akan berisi:
- `ZeroMix-v5.2.3-Setup.exe` (Installer, signed)
- `ZeroMix-v5.2.3-Portable.zip` (Portable version)

---

## 📁 File yang Di-Update Otomatis

Script `release-version.ps1` akan update versi di:

| File | Pattern |
|------|---------|
| `src/MainWindow.xaml.cs` | `CURRENT_VERSION = "5.2.3"` |
| `Exe/Setup.iss` | `#define AppVersion "5.2.3"` |
| `ZeroMix.csproj` | `<Version>5.2.3</Version>` |

---

## 🏷️ Git Tag Management

### Lihat Semua Tag

```bash
git tag
```

### Hapus Tag (Lokal & Remote)

```bash
# Hapus lokal
git tag -d v5.1.4

# Hapus remote
git push origin :refs/tags/v5.1.4
```

### Re-release Versi yang Sama

Jika ada kesalahan dan mau re-release versi yang sama:

```powershell
# Script akan tanya apakah mau overwrite tag
.\Scripts\release-version.ps1 -NewVersion 5.2.3
# Jawab 'y' untuk delete & recreate tag
```

---

## 🔐 Certificate Setup (Opsional)

Untuk signing otomatis, setup certificate sekali saja:

```powershell
.\Scripts\setup-github-secrets.ps1
```

Atau manual:
1. Encode certificate ke base64:
   ```powershell
   $cert = [Convert]::ToBase64String([IO.File]::ReadAllBytes("Exe/ZeroMixCert.pfx"))
   $cert | Out-File "cert_base64.txt"
   ```

2. Add GitHub Secrets:
   - `CERT_BASE64` = isi dari `cert_base64.txt`
   - `CERT_PASSWORD` = password certificate

---

## 📊 Version History Example

```
v5.2.3 (Latest)
├─ ZeroMix-v5.2.3-Setup.exe
└─ ZeroMix-v5.2.3-Portable.zip

v5.1.4
├─ ZeroMix-v5.1.4-Setup.exe
└─ ZeroMix-v5.1.4-Portable.zip

v5.1.3
├─ ZeroMix-v5.1.3-Setup.exe
└─ ZeroMix-v5.1.3-Portable.zip
```

Setiap versi punya release sendiri, tidak numpuk!

---

## 🛠️ Manual Release (Tanpa Script)

Jika mau manual:

### 1. Update Versi Manual

Edit file-file ini:
- `src/MainWindow.xaml.cs` → `CURRENT_VERSION`
- `Exe/Setup.iss` → `AppVersion`
- `ZeroMix.csproj` → `<Version>`

### 2. Commit & Tag

```bash
git add .
git commit -m "chore: bump version to 5.2.3"
git tag -a v5.2.3 -m "Release 5.2.3"
```

### 3. Push

```bash
git push origin main
git push origin v5.2.3
```

---

## 🚨 Troubleshooting

### Tag Sudah Ada

```
❌ Tag v5.2.3 already exists!
```

**Solusi:**
```bash
# Hapus tag lokal & remote
git tag -d v5.2.3
git push origin :refs/tags/v5.2.3

# Buat ulang
.\Scripts\release-version.ps1 -NewVersion 5.2.3
```

### Workflow Gagal

1. Cek workflow di: `https://github.com/[user]/[repo]/actions`
2. Lihat log error
3. Common issues:
   - FFMPEG download timeout → Re-run workflow
   - Inno Setup error → Cek `Exe/Setup.iss` syntax
   - Certificate error → Cek GitHub Secrets

### Build Lokal Dulu

Test build sebelum release:

```powershell
# Build lokal tanpa upload
.\build\build-sign-release.ps1 -Version 5.2.3 -SkipUpload
```

---

## 📝 Best Practices

### 1. Semantic Versioning

```
MAJOR.MINOR.PATCH
  5  . 1  . 5

MAJOR: Breaking changes
MINOR: New features (backward compatible)
PATCH: Bug fixes
```

### 2. Release Checklist

- [ ] Test aplikasi lokal
- [ ] Update CHANGELOG.md
- [ ] Run `release-version.ps1`
- [ ] Monitor GitHub Actions
- [ ] Test installer dari release
- [ ] Announce di Discord/Social Media

### 3. Hotfix Release

Untuk bug critical:

```powershell
# Langsung dari main branch
.\Scripts\release-version.ps1 -NewVersion 5.2.3 -CommitMessage "hotfix: critical bug fix"
```

---

## 🎯 Examples

### Release Minor Version

```powershell
# 5.1.4 → 5.2.3
.\Scripts\release-version.ps1 -NewVersion 5.2.3
```

### Release Major Version

```powershell
# 5.2.3 → 6.0.0
.\Scripts\release-version.ps1 -NewVersion 6.0.0 -CommitMessage "feat: major update with breaking changes"
```

### Beta Release

```powershell
# Untuk beta, gunakan tag manual
git tag -a v5.2.0-beta.1 -m "Beta release"
git push origin v5.2.0-beta.1
```

---

## 📞 Support

Jika ada masalah:
1. Cek [WORKFLOW_TROUBLESHOOTING.md](WORKFLOW_TROUBLESHOOTING.md)
2. Lihat GitHub Actions logs
3. Open issue di repository

---

**Happy Releasing! 🚀**
