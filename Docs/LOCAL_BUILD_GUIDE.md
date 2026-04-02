# 🏗️ Local Build, Sign & Release Guide

Panduan lengkap untuk build, sign, dan upload ZeroMix secara lokal menggunakan PowerShell script.

## 📋 Prerequisites

### Required
- ✅ Windows 10/11 x64
- ✅ .NET 9.0 SDK
- ✅ PowerShell 5.0+
- ✅ Inno Setup 6 (untuk build installer)
- ✅ Git

### Optional (untuk signing & upload)
- ✅ osslsigncode (auto-installed via chocolatey)
- ✅ GitHub CLI (`gh`)
- ✅ Certificate: `Exe/ZeroMixCert.pfx`

## 🚀 Quick Start

### 1. Build Only
```powershell
.\build\build-sign-release.ps1 -Version 5.1.1 -SkipSign -SkipUpload
```

### 2. Build + Sign
```powershell
.\build\build-sign-release.ps1 -Version 5.1.1 -SkipUpload
```

### 3. Build + Sign + Upload
```powershell
.\build\build-sign-release.ps1 -Version 5.1.1
```

## 📝 Usage Details

### Basic Usage
```powershell
.\build\build-sign-release.ps1 -Version 5.1.1
```

### With Certificate Password
```powershell
.\build\build-sign-release.ps1 -Version 5.1.1 -CertPassword "your_password"
```

### Skip Signing
```powershell
.\build\build-sign-release.ps1 -Version 5.1.1 -SkipSign
```

### Skip Upload
```powershell
.\build\build-sign-release.ps1 -Version 5.1.1 -SkipUpload
```

### Specify Repository
```powershell
.\build\build-sign-release.ps1 -Version 5.1.1 -Repository "YourOrg/ZeroMix"
```

### All Options
```powershell
.\build\build-sign-release.ps1 `
    -Version 5.1.1 `
    -CertPath "Exe/ZeroMixCert.pfx" `
    -CertPassword "password" `
    -SkipSign:$false `
    -SkipUpload:$false `
    -Repository "YourOrg/ZeroMix"
```

## 🔄 Build Process

Script akan otomatis menjalankan:

```
1. Determine Version
   ├─ From parameter atau git tag
   └─ Validate version format

2. Clean & Restore
   ├─ Remove old builds
   └─ Restore NuGet packages

3. Build Application
   ├─ Compile Release build
   ├─ Self-contained deployment
   └─ Output: publish/win-x64/

4. Download FFMPEG
   ├─ Download dari gyan.dev
   ├─ Extract binary
   └─ Output: Tools/FFMPEG/ffmpeg.exe

5. Build Installer
   ├─ Run Inno Setup
   ├─ Create setup.exe
   └─ Output: Exe/ZeroMix-Setup-v*.exe

6. Create Portable ZIP
   ├─ Compress publish folder
   └─ Output: ZeroMix-v*.Portable.zip

7. Sign Executable (Optional)
   ├─ Install osslsigncode
   ├─ Sign with certificate
   └─ Timestamp signing

8. Prepare Artifacts
   ├─ Copy setup.exe
   └─ Ready for release

9. Upload to GitHub (Optional)
   ├─ Create/update release
   ├─ Upload artifacts
   └─ Generate release notes
```

## 📊 Output Files

### Generated Artifacts
```
ZeroMix-v5.1.1-Setup.exe          (~150-200 MB)
ZeroMix-v5.1.1-Portable.zip       (~150-200 MB)
```

### Intermediate Files
```
publish/win-x64/                  (Compiled app)
Tools/FFMPEG/ffmpeg.exe           (Downloaded)
Exe/ZeroMix-Setup-v5.1.1.exe      (Installer)
```

## 🔐 Code Signing

### Setup Certificate (One-time)

1. **Ensure certificate exists:**
   ```
   Exe/ZeroMixCert.pfx
   ```

2. **Test certificate locally:**
   ```powershell
   $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2(
       "Exe/ZeroMixCert.pfx",
       "your_password"
   )
   Write-Host "Valid: $($cert.Verify())"
   ```

### Signing Process

Script akan:
1. Check osslsigncode installed
2. Prompt for certificate password
3. Sign executable with timestamp
4. Replace original with signed version

### Timestamp Servers
- Primary: `http://timestamp.comodoca.com/authenticode`
- Fallback: `http://timestamp.sectigo.com`
- Alternative: `http://timestamp.globalsign.com/tsa/r6advanced1`

## 📤 GitHub Upload

### Prerequisites
```powershell
# Install GitHub CLI
choco install gh -y

# Login to GitHub
gh auth login
```

### Upload Process

Script akan:
1. Detect repository automatically
2. Check if release exists
3. Create or update release
4. Upload artifacts
5. Generate release notes

### Manual Upload
```powershell
# If automatic upload fails
gh release create v5.1.1 `
    ZeroMix-v5.1.1-Setup.exe `
    ZeroMix-v5.1.1-Portable.zip `
    --title "ZeroMix v5.1.1" `
    --notes "Release notes here"
```

## 🐛 Troubleshooting

### Build Failed
```
Error: Build failed: ZeroMix.exe not found
```
**Solution:**
- Verify .NET 9 SDK installed: `dotnet --version`
- Check project file: `ZeroMix.csproj`
- Clean and retry: `dotnet clean`

### Inno Setup Not Found
```
Error: Inno Setup not found at: C:\Program Files (x86)\Inno Setup 6\ISCC.exe
```
**Solution:**
- Install Inno Setup: https://jrsoftware.org/isdl.php
- Or update path in script

### FFMPEG Download Failed
```
Error: Invoke-WebRequest: The remote server returned an error
```
**Solution:**
- Check internet connection
- Verify URL: https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip
- Try manual download

### Signing Failed
```
Error: osslsigncode: command not found
```
**Solution:**
- Install chocolatey: https://chocolatey.org/install
- Run: `choco install osslsigncode -y`

### Certificate Password Wrong
```
Error: Unable to read certificate
```
**Solution:**
- Verify password is correct
- Test locally: `$cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2(...)`
- Check for special characters

### GitHub Upload Failed
```
Error: gh: command not found
```
**Solution:**
- Install GitHub CLI: `choco install gh -y`
- Login: `gh auth login`
- Or use `-SkipUpload` flag

## 📝 Examples

### Example 1: Build Only (No Sign/Upload)
```powershell
.\build\build-sign-release.ps1 -Version 5.1.1 -SkipSign -SkipUpload
```
**Output:**
- `ZeroMix-v5.1.1-Setup.exe` (unsigned)
- `ZeroMix-v5.1.1-Portable.zip`

### Example 2: Build + Sign (No Upload)
```powershell
.\build\build-sign-release.ps1 -Version 5.1.1 -SkipUpload
```
**Output:**
- `ZeroMix-v5.1.1-Setup.exe` (signed)
- `ZeroMix-v5.1.1-Portable.zip`

### Example 3: Full Pipeline
```powershell
.\build\build-sign-release.ps1 -Version 5.1.1
```
**Output:**
- Artifacts created
- Executable signed
- Release created on GitHub
- Artifacts uploaded

### Example 4: With Explicit Password
```powershell
.\build\build-sign-release.ps1 `
    -Version 5.1.1 `
    -CertPassword "MySecurePassword123!"
```

## ✅ Verification

### Check Build Output
```powershell
# Verify artifacts exist
Test-Path "ZeroMix-v5.1.1-Setup.exe"
Test-Path "ZeroMix-v5.1.1-Portable.zip"

# Check file sizes
(Get-Item "ZeroMix-v5.1.1-Setup.exe").Length / 1MB
```

### Verify Signing
```powershell
# Check if executable is signed
Get-AuthenticodeSignature "ZeroMix-v5.1.1-Setup.exe"

# Should show: Status = Valid
```

### Verify GitHub Release
```powershell
# List releases
gh release list

# Download specific release
gh release download v5.1.1
```

## 🎯 Next Steps

1. ✅ Install prerequisites
2. ✅ Setup certificate (if signing)
3. ✅ Run build script
4. ✅ Test artifacts
5. ✅ Verify release on GitHub

## 📚 Related Documentation

- [CERTIFICATE_SETUP.md](./CERTIFICATE_SETUP.md) - Certificate setup
- [RELEASE_WORKFLOW.md](./RELEASE_WORKFLOW.md) - GitHub Actions workflow
- [RELEASE_QUICK_START.md](../RELEASE_QUICK_START.md) - Quick reference

---

**Last Updated:** April 2, 2026  
**Version:** 5.1.1  
**Status:** Ready for use
