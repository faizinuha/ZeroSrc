# 📜 ZeroMix Scripts

Kumpulan script automation untuk development dan release ZeroMix.

---

## 🚀 Release Scripts

### `release-version.ps1` / `release-version.bat`

**Fungsi:** Otomatis update versi, commit, tag, dan push ke GitHub untuk trigger release.

**Usage:**

```bash
# Cara termudah (Windows)
.\Scripts\release-version.bat 5.1.5

# Atau dengan PowerShell
.\Scripts\release-version.ps1 -NewVersion 5.1.5

# Dengan custom commit message
.\Scripts\release-version.ps1 -NewVersion 5.1.5 -CommitMessage "feat: new features"

# Preview saja (tidak push)
.\Scripts\release-version.ps1 -NewVersion 5.1.5 -SkipPush
```

**Apa yang dilakukan:**
1. ✅ Update versi di `src/MainWindow.xaml.cs`
2. ✅ Update versi di `Exe/Setup.iss`
3. ✅ Update versi di `ZeroMix.csproj`
4. ✅ Update CHANGELOG.md dengan entry baru
5. ✅ Git commit semua perubahan
6. ✅ Create git tag `v5.1.5`
7. ✅ Push commit & tag ke GitHub
8. ✅ Trigger GitHub Actions workflow

**Output:**
- Tag baru di GitHub: `v5.1.5`
- GitHub Release otomatis dengan installer & portable ZIP
- Workflow monitoring link

---

## 🔐 Certificate Scripts

### `setup-github-secrets.ps1`

**Fungsi:** Setup certificate untuk code signing di GitHub Actions.

**Usage:**

```powershell
.\Scripts\setup-github-secrets.ps1
```

**Apa yang dilakukan:**
1. Encode `Exe/ZeroMixCert.pfx` ke base64
2. Save ke `Exe/ZeromixCert_base64.txt`
3. Provide instructions untuk add GitHub Secrets

**Requirements:**
- Certificate file: `Exe/ZeroMixCert.pfx`
- GitHub CLI (optional, untuk auto-upload)

---

## 🔄 Update Scripts

### `zeromix-update.bat`

**Fungsi:** Auto-update ZeroMix dari GitHub Releases.

**Usage:**

```bash
# Dijalankan otomatis oleh aplikasi
zeromix-update.bat
```

**Apa yang dilakukan:**
1. Check versi terbaru dari GitHub API
2. Download installer baru
3. Close aplikasi
4. Install update
5. Restart aplikasi

---

## 📊 Script Overview

| Script | Fungsi | Kapan Digunakan |
|--------|--------|-----------------|
| `release-version.ps1` | Release versi baru | Setiap mau release |
| `setup-github-secrets.ps1` | Setup certificate | Sekali saja (initial setup) |
| `zeromix-update.bat` | Auto-update | Otomatis oleh aplikasi |

---

## 🎯 Common Workflows

### Release Versi Baru

```bash
# 1. Test build lokal
.\build\build-sign-release.ps1 -Version 5.1.5 -SkipUpload

# 2. Jika OK, release ke GitHub
.\Scripts\release-version.bat 5.1.5

# 3. Monitor workflow
# Buka: https://github.com/[user]/[repo]/actions
```

### Setup Certificate (Pertama Kali)

```bash
# 1. Pastikan certificate ada
dir Exe\ZeroMixCert.pfx

# 2. Run setup script
.\Scripts\setup-github-secrets.ps1

# 3. Add secrets ke GitHub
# Settings → Secrets → Actions → New repository secret
# - CERT_BASE64 = isi dari Exe/ZeromixCert_base64.txt
# - CERT_PASSWORD = password certificate
```

### Hotfix Release

```bash
# Langsung release tanpa banyak testing
.\Scripts\release-version.ps1 -NewVersion 5.1.6 -CommitMessage "hotfix: critical bug"
```

---

## 🛠️ Development

### Menambah Script Baru

1. Buat file `.ps1` di folder `Scripts/`
2. Tambahkan dokumentasi di README ini
3. Buat wrapper `.bat` jika perlu (untuk kemudahan)

### Testing Script

```powershell
# Test dengan -WhatIf (dry run)
.\Scripts\release-version.ps1 -NewVersion 5.1.5 -SkipPush -WhatIf

# Test dengan -Verbose
.\Scripts\release-version.ps1 -NewVersion 5.1.5 -Verbose
```

---

## 📚 Dokumentasi Lengkap

- [RELEASES_GUIDE.md](../Docs/RELEASES_GUIDE.md) - Panduan lengkap release
- [RELEASE_QUICK.md](../RELEASE_QUICK.md) - Quick reference
- [RELEASE_TROUBLESHOOTING.md](../Docs/RELEASE_TROUBLESHOOTING.md) - Troubleshooting

---

## 🚨 Troubleshooting

### Script Tidak Jalan

```powershell
# Set execution policy
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Git Command Gagal

```bash
# Pastikan git configured
git config --global user.name "Your Name"
git config --global user.email "your@email.com"
```

### Tag Sudah Ada

```bash
# Hapus tag lokal & remote
git tag -d v5.1.5
git push origin :refs/tags/v5.1.5

# Buat ulang
.\Scripts\release-version.bat 5.1.5
```

---

**Happy Scripting! 🎉**
