# 🏗️ ZeroMix Build Scripts

Automated build, sign, and release pipeline untuk ZeroMix.

## 📁 Files

### `build.ps1`
Manual build script (legacy). Hanya compile aplikasi.

**Usage:**
```powershell
.\build.ps1
```

### `build-sign-release.ps1`
**Recommended** - Complete automated pipeline: Build → Sign → Upload

**Features:**
- ✅ Compile aplikasi
- ✅ Download FFMPEG
- ✅ Build installer dengan Inno Setup
- ✅ Create portable ZIP
- ✅ Sign executable (optional)
- ✅ Upload ke GitHub (optional)

**Usage:**
```powershell
.\build-sign-release.ps1 -Version 5.1.1
```

### `build-release.bat`
Wrapper untuk PowerShell script (Windows batch).

**Usage:**
```batch
build-release.bat 5.1.1
```

## 🚀 Quick Start

### Option 1: Using PowerShell (Recommended)
```powershell
cd build
.\build-sign-release.ps1 -Version 5.1.1
```

### Option 2: Using Batch File
```batch
cd build
build-release.bat 5.1.1
```

### Option 3: Build Only (No Sign/Upload)
```powershell
.\build-sign-release.ps1 -Version 5.1.1 -SkipSign -SkipUpload
```

## 📊 Pipeline Steps

```
1. Determine Version
2. Clean & Restore
3. Build Application
4. Download FFMPEG
5. Build Installer
6. Create Portable ZIP
7. Sign Executable (optional)
8. Prepare Artifacts
9. Upload to GitHub (optional)
```

## 📋 Prerequisites

### Required
- Windows 10/11 x64
- .NET 9.0 SDK
- PowerShell 5.0+
- Inno Setup 6
- Git

### Optional
- osslsigncode (auto-installed)
- GitHub CLI (`gh`)
- Certificate: `Exe/ZeroMixCert.pfx`

## 🔐 Code Signing

### Setup (One-time)
1. Ensure certificate exists: `Exe/ZeroMixCert.pfx`
2. Test certificate locally
3. Run script with certificate password

### Automatic Signing
```powershell
.\build-sign-release.ps1 -Version 5.1.1 -CertPassword "your_password"
```

### Skip Signing
```powershell
.\build-sign-release.ps1 -Version 5.1.1 -SkipSign
```

## 📤 GitHub Upload

### Prerequisites
```powershell
# Install GitHub CLI
choco install gh -y

# Login
gh auth login
```

### Automatic Upload
```powershell
.\build-sign-release.ps1 -Version 5.1.1
```

### Skip Upload
```powershell
.\build-sign-release.ps1 -Version 5.1.1 -SkipUpload
```

## 📝 Examples

### Build Only
```powershell
.\build-sign-release.ps1 -Version 5.1.1 -SkipSign -SkipUpload
```

### Build + Sign
```powershell
.\build-sign-release.ps1 -Version 5.1.1 -SkipUpload
```

### Full Pipeline
```powershell
.\build-sign-release.ps1 -Version 5.1.1
```

### With Password
```powershell
.\build-sign-release.ps1 -Version 5.1.1 -CertPassword "MyPassword"
```

## 📊 Output

### Artifacts
```
ZeroMix-v5.1.1-Setup.exe          (~150-200 MB)
ZeroMix-v5.1.1-Portable.zip       (~150-200 MB)
```

### Intermediate
```
publish/win-x64/                  (Compiled app)
Tools/FFMPEG/ffmpeg.exe           (Downloaded)
Exe/ZeroMix-Setup-v5.1.1.exe      (Installer)
```

## 🐛 Troubleshooting

### Build Failed
- Check .NET 9 SDK: `dotnet --version`
- Clean project: `dotnet clean`

### Inno Setup Not Found
- Install from: https://jrsoftware.org/isdl.php

### FFMPEG Download Failed
- Check internet connection
- Verify URL is accessible

### Signing Failed
- Install osslsigncode: `choco install osslsigncode -y`
- Verify certificate password

### Upload Failed
- Install GitHub CLI: `choco install gh -y`
- Login: `gh auth login`

## 📚 Documentation

- [LOCAL_BUILD_GUIDE.md](../Docs/LOCAL_BUILD_GUIDE.md) - Detailed guide
- [CERTIFICATE_SETUP.md](../Docs/CERTIFICATE_SETUP.md) - Certificate setup
- [RELEASE_WORKFLOW.md](../Docs/RELEASE_WORKFLOW.md) - GitHub Actions workflow

## 🎯 Workflow Comparison

| Feature | build.ps1 | build-sign-release.ps1 |
|---------|-----------|------------------------|
| Compile | ✅ | ✅ |
| FFMPEG | ❌ | ✅ |
| Installer | ❌ | ✅ |
| Portable ZIP | ❌ | ✅ |
| Sign | ❌ | ✅ |
| Upload | ❌ | ✅ |
| Progress | Basic | Detailed |

---

**Last Updated:** April 2, 2026  
**Version:** 5.1.1
