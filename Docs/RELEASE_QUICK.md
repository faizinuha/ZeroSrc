# 🚀 Quick Release Guide

## Cara Release Versi Baru
5.2.7  →  5.3.0  (tambah fitur, patch reset)
5.3.0  →  5.3.1  (bug fix)
5.3.1  →  5.3.2  (bug fix lagi)
5.3.2  →  5.4.0  (tambah fitur lagi, patch reset lagi)

5.3.2  →  6.0.0  (breaking change, minor & patch reset)

### Satu Command

```powershell

.\Scripts\release-version.ps1 -NewVersion 5.1.9 -CommitMessage "feat: new bootstrap ZeroMix.Installer"

.\Scripts\release-version.ps1 -NewVersion 5.2.0 -CommitMessage "fix: fix startup freeze, move plugin init to background"

```

Script ini otomatis:
1. Update versi di `MainWindow.xaml.cs`, `Setup.iss`, `ZeroMix.csproj`
2. Update `CHANGELOG.md`
3. `git add .` → `git commit` → buat tag `v5.1.9`
4. `git push` + `git push --tags`
5. GitHub Actions otomatis jalan → build → release

---

## Apa yang Di-build GitHub Actions?

Setelah tag di-push, workflow akan menghasilkan:

| File | Keterangan |
|------|------------|
| `ZeroMix-v5.1.9-Installer.exe` | ⭐ Bootstrap kecil (~10MB) — kasih ini ke user |
| `ZeroMix-v5.1.9-Setup.exe` | Offline installer lengkap |
| `ZeroMix-v5.1.9-Portable.zip` | Portable tanpa install |
| `ZeroMix-Setup.zip` | Di-download otomatis oleh bootstrap |

Bootstrap (`ZeroMix-Installer.exe`) akan otomatis download `ZeroMix-Setup.zip`
dari GitHub Releases saat user klik install.

---

## Monitor Build

Buka: `https://github.com/faizinuha/ZeroMix/actions`

Tunggu ~10 menit. Kalau hijau = sukses.

---

## Commands Lain

```powershell
# Preview saja, tidak push ke GitHub
.\Scripts\release-version.ps1 -NewVersion 5.1.9 -SkipPush

# Rebuild versi yang sama (misal ada bug di workflow)
.\Scripts\release-version.ps1 -NewVersion 5.1.9 -ForceBuild
```

---

## Catatan Penting

- Push biasa (`git push` tanpa tag) **tidak** trigger build
- Hanya `git push --tags` yang trigger GitHub Actions
- Script `release-version.ps1` sudah handle semua itu otomatis
- `ZeroMix.Installer` di-build ulang setiap release — URL download otomatis terupdate
