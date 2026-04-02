# 🔄 ZeroMix Workflow Flow Diagram

Dokumentasi lengkap alur workflow GitHub Actions untuk build, sign, dan release.

## 📊 Complete Workflow Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    Developer Action                             │
│                                                                 │
│  git tag v5.1.1                                                │
│  git push origin v5.1.1                                        │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│         🚀 build-release.yml TRIGGERED                          │
│                                                                 │
│  Event: push tag matching 'v*'                                 │
│  Runs on: windows-latest                                       │
│  Permissions: contents:write                                   │
└────────────────────────┬────────────────────────────────────────┘
                         │
        ┌────────────────┼────────────────┐
        │                │                │
        ▼                ▼                ▼
   ┌─────────┐    ┌──────────┐    ┌──────────────┐
   │ Checkout│    │ Determine│    │ Setup .NET 9 │
   │  Code   │    │ Version  │    │              │
   └────┬────┘    └────┬─────┘    └──────┬───────┘
        │              │                 │
        └──────────────┼─────────────────┘
                       │
                       ▼
        ┌──────────────────────────────┐
        │  📦 Restore & Publish        │
        │                              │
        │  dotnet restore              │
        │  dotnet publish (Release)    │
        │  Output: publish/win-x64/    │
        └──────────────┬───────────────┘
                       │
                       ▼
        ┌──────────────────────────────┐
        │  🎬 Download FFMPEG          │
        │                              │
        │  Download from gyan.dev      │
        │  Extract ffmpeg.exe          │
        │  Output: Tools/FFMPEG/       │
        └──────────────┬───────────────┘
                       │
                       ▼
        ┌──────────────────────────────┐
        │  🛠️ Build Inno Setup         │
        │                              │
        │  Run ISCC.exe                │
        │  Create setup.exe            │
        │  Output: Exe/ZeroMix-*.exe   │
        └──────────────┬───────────────┘
                       │
                       ▼
        ┌──────────────────────────────┐
        │  🔐 Sign Executable          │
        │                              │
        │  Install osslsigncode        │
        │  Check CERT_BASE64 secret    │
        │  ├─ If exists: Sign exe      │
        │  └─ If not: Skip (OK)        │
        └──────────────┬───────────────┘
                       │
                       ▼
        ┌──────────────────────────────┐
        │  📦 Prepare Artifacts        │
        │                              │
        │  Create Portable ZIP         │
        │  Move Setup EXE              │
        │  Ready for release           │
        └──────────────┬───────────────┘
                       │
                       ▼
        ┌──────────────────────────────┐
        │  🚀 Create GitHub Release    │
        │                              │
        │  Create release tag          │
        │  Upload Setup EXE            │
        │  Upload Portable ZIP         │
        │  Generate release notes      │
        └──────────────┬───────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────────┐
│         ✅ build-release.yml COMPLETED                          │
│                                                                 │
│  Release created on GitHub                                     │
│  Artifacts uploaded                                            │
│  Release notes generated                                       │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│         🔐 sign-release.yml TRIGGERED (Optional)               │
│                                                                 │
│  Event: build-release.yml completed successfully               │
│  Runs on: windows-latest                                       │
│  Permissions: contents:write                                   │
│                                                                 │
│  OR Manual trigger:                                            │
│  GitHub Actions → sign-release.yml → Run workflow              │
└────────────────────────┬────────────────────────────────────────┘
                         │
        ┌────────────────┼────────────────┐
        │                │                │
        ▼                ▼                ▼
   ┌─────────┐    ┌──────────┐    ┌──────────────┐
   │ Checkout│    │ Download │    │ Install      │
   │  Code   │    │ Artifacts│    │ osslsigncode │
   └────┬────┘    └────┬─────┘    └──────┬───────┘
        │              │                 │
        └──────────────┼─────────────────┘
                       │
                       ▼
        ┌──────────────────────────────┐
        │  🔐 Sign Executable          │
        │                              │
        │  Check CERT_BASE64 secret    │
        │  ├─ If exists:               │
        │  │  ├─ Decode certificate    │
        │  │  ├─ Sign with osslsigncode│
        │  │  └─ Replace original      │
        │  └─ If not: Skip (OK)        │
        └──────────────┬───────────────┘
                       │
                       ▼
        ┌──────────────────────────────┐
        │  📤 Upload Signed Artifacts  │
        │                              │
        │  Upload to GitHub Release    │
        │  Replace unsigned version    │
        └──────────────┬───────────────┘
                       │
                       ▼
        ┌──────────────────────────────┐
        │  📝 Release Ready            │
        │                              │
        │  Display release URL         │
        │  Ready for download          │
        └──────────────┬───────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────────┐
│         ✅ sign-release.yml COMPLETED                           │
│                                                                 │
│  Executable signed (if certificate available)                  │
│  Artifacts uploaded to GitHub Release                          │
│  Release ready for download                                    │
└─────────────────────────────────────────────────────────────────┘
```

## 🔄 Workflow Triggers

### build-release.yml
```
Trigger 1: Push tag matching 'v*'
  git tag v5.1.1
  git push origin v5.1.1
  → Workflow starts automatically

Trigger 2: Manual dispatch
  GitHub Actions → build-release.yml → Run workflow
  → Enter version: 5.1.1
  → Workflow starts
```

### sign-release.yml
```
Trigger 1: build-release.yml completed successfully
  → Workflow starts automatically
  → Downloads latest release artifacts
  → Signs and uploads

Trigger 2: Manual dispatch
  GitHub Actions → sign-release.yml → Run workflow
  → Enter release tag: v5.1.1
  → Workflow starts
```

## 📋 Step Details

### build-release.yml Steps

#### 1. Checkout Code
```
Action: actions/checkout@v4
Purpose: Clone repository
Output: Source code ready
```

#### 2. Determine Version
```
Logic:
  if workflow_dispatch:
    version = input parameter
  else:
    version = git tag name (v5.1.1 → 5.1.1)
Output: ${{ steps.version.outputs.version }}
```

#### 3. Setup .NET
```
Action: actions/setup-dotnet@v4
Version: 9.0.x
Purpose: Install .NET 9 SDK
```

#### 4. Restore & Publish
```
Commands:
  dotnet restore ZeroMix.csproj
  dotnet publish -c Release -r win-x64 --self-contained
Output: publish/win-x64/ZeroMix.exe
```

#### 5. Download FFMPEG
```
Process:
  1. Create Tools/FFMPEG directory
  2. Download ffmpeg-release-essentials.zip
  3. Extract binary
  4. Copy ffmpeg.exe to Tools/FFMPEG/
  5. Cleanup temp files
Output: Tools/FFMPEG/ffmpeg.exe
```

#### 6. Build Inno Setup Installer
```
Action: Minionguyjpro/Inno-Setup-Action@v1.2.7
Input: Exe/Setup.iss
Parameter: /DAppVersion=5.1.1
Output: Exe/ZeroMix-Setup-v5.1.1.exe
```

#### 7. Sign Executable
```
Process:
  1. Check if CERT_BASE64 secret exists
  2. If yes:
     a. Install osslsigncode
     b. Decode certificate from base64
     c. Sign executable with osslsigncode
     d. Replace original with signed version
  3. If no:
     a. Skip signing (not error)
     b. Continue to next step
Output: Signed or unsigned executable
```

#### 8. Prepare Release Artifacts
```
Process:
  1. Create Portable ZIP from publish/win-x64/
  2. Move Setup EXE to root directory
  3. Rename to final names
Output:
  - ZeroMix-v5.1.1-Setup.exe
  - ZeroMix-v5.1.1-Portable.zip
```

#### 9. Create GitHub Release
```
Action: softprops/action-gh-release@v2
Process:
  1. Create release with tag
  2. Upload Setup EXE
  3. Upload Portable ZIP
  4. Generate release notes
Output: GitHub Release with artifacts
```

### sign-release.yml Steps

#### 1. Checkout Code
```
Purpose: Get repository context
```

#### 2. Download Release Artifacts
```
Process:
  1. Determine release tag
  2. Download *.exe files
  3. Save to ./release-artifacts/
Output: Downloaded executable
```

#### 3. Install osslsigncode
```
Command: choco install osslsigncode -y
Purpose: Install signing tool
```

#### 4. Sign Executable
```
Process:
  1. Check CERT_BASE64 secret
  2. If exists:
     a. Decode certificate
     b. Sign executable
     c. Replace original
  3. If not:
     a. Skip (not error)
Output: Signed executable
```

#### 5. Upload Signed Artifacts
```
Process:
  1. Upload signed executable to release
  2. Use --clobber to replace unsigned version
Output: Updated GitHub Release
```

#### 6. Release Ready
```
Purpose: Display completion message
Output: Release URL
```

## 🔐 Certificate Handling

### If Certificate Configured
```
CERT_BASE64 secret exists
    ↓
Decode from base64
    ↓
Create temporary cert.pfx
    ↓
Sign executable with osslsigncode
    ↓
Replace original
    ↓
Delete temporary cert.pfx
    ↓
Result: Signed executable
```

### If Certificate Not Configured
```
CERT_BASE64 secret not found
    ↓
Skip signing (not error)
    ↓
Continue workflow
    ↓
Result: Unsigned executable (OK)
```

## 📊 Artifact Flow

```
Source Code
    ↓
Compile (dotnet publish)
    ↓
publish/win-x64/
    ├─ ZeroMix.exe
    ├─ Dependencies
    └─ Resources
    ↓
├─ Create Portable ZIP
│  └─ ZeroMix-v5.1.1-Portable.zip
│
└─ Build Installer
   ├─ Download FFMPEG
   ├─ Run Inno Setup
   ├─ Sign (optional)
   └─ ZeroMix-v5.1.1-Setup.exe
    ↓
GitHub Release
    ├─ ZeroMix-v5.1.1-Setup.exe
    └─ ZeroMix-v5.1.1-Portable.zip
```

## ✅ Success Criteria

### build-release.yml Success
- ✅ Code checked out
- ✅ Version determined
- ✅ .NET 9 installed
- ✅ Application published
- ✅ FFMPEG downloaded
- ✅ Installer built
- ✅ Artifacts prepared
- ✅ GitHub Release created

### sign-release.yml Success
- ✅ Artifacts downloaded
- ✅ osslsigncode installed
- ✅ Executable signed (if cert available)
- ✅ Signed version uploaded
- ✅ Release ready

## ❌ Failure Handling

### If build-release.yml Fails
```
Workflow stops at failure point
    ↓
GitHub Actions shows error
    ↓
Check logs for details
    ↓
Fix issue
    ↓
Retry: Push tag again or manual trigger
```

### If sign-release.yml Fails
```
Workflow stops at failure point
    ↓
GitHub Actions shows error
    ↓
Check logs for details
    ↓
Fix issue (usually certificate related)
    ↓
Retry: Manual trigger sign-release.yml
```

## 🎯 Typical Execution Times

```
build-release.yml:
  - Checkout: ~10 seconds
  - Setup .NET: ~30 seconds
  - Restore: ~1 minute
  - Publish: ~2 minutes
  - Download FFMPEG: ~2 minutes
  - Build Installer: ~2 minutes
  - Sign: ~1 minute
  - Prepare Artifacts: ~1 minute
  - Create Release: ~30 seconds
  ─────────────────────────
  Total: ~10 minutes

sign-release.yml:
  - Checkout: ~10 seconds
  - Download Artifacts: ~1 minute
  - Install osslsigncode: ~30 seconds
  - Sign: ~1 minute
  - Upload: ~1 minute
  ─────────────────────────
  Total: ~4 minutes
```

---

**Last Updated:** April 2, 2026  
**Version:** 5.1.1
