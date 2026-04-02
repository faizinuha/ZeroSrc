# ZeroMix Complete Build, Sign & Release Pipeline
# Automated: Build → Sign → Upload to GitHub

param(
    [string]$Version = "",
    [string]$CertPath = "Exe/ZeroMixCert.pfx",
    [string]$CertPassword = "",
    [switch]$SkipSign = $false,
    [switch]$SkipUpload = $false,
    [string]$Repository = ""
)

$ErrorActionPreference = "Stop"

# ============================================================================
# CONFIGURATION
# ============================================================================

$AppName = "ZeroMix"
$PublishDir = "publish/win-x64"
$SetupScript = "Exe/Setup.iss"
$FFMPEGUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip"
$TimestampServer = "http://timestamp.comodoca.com/authenticode"

# ============================================================================
# FUNCTIONS
# ============================================================================

function Write-Section {
    param([string]$Title)
    Write-Host ""
    Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║ $($Title.PadRight(62)) ║" -ForegroundColor Cyan
    Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""
}

function Write-Step {
    param([string]$Message)
    Write-Host "▶ $Message" -ForegroundColor Yellow
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-Error-Custom {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor Red
}

# ============================================================================
# STEP 1: DETERMINE VERSION
# ============================================================================

Write-Section "STEP 1: Determine Version"

if ([string]::IsNullOrEmpty($Version)) {
    # Try to get from git tag
    $gitTag = git describe --tags --abbrev=0 2>$null
    if ($gitTag) {
        $Version = $gitTag -replace '^v', ''
        Write-Step "Version from git tag: $Version"
    } else {
        Write-Error-Custom "Version not specified and no git tag found"
        Write-Host "Usage: .\build-sign-release.ps1 -Version 5.1.1"
        exit 1
    }
}

Write-Success "Version: $Version"

# ============================================================================
# STEP 2: CLEAN & RESTORE
# ============================================================================

Write-Section "STEP 2: Clean & Restore"

Write-Step "Cleaning old builds..."
if (Test-Path $PublishDir) {
    Remove-Item -Recurse -Force $PublishDir
}
if (Test-Path "dist") {
    Remove-Item -Recurse -Force "dist"
}
Write-Success "Cleaned"

Write-Step "Restoring dependencies..."
dotnet restore ZeroMix.csproj
Write-Success "Dependencies restored"

# ============================================================================
# STEP 3: BUILD APPLICATION
# ============================================================================

Write-Section "STEP 3: Build Application"

Write-Step "Publishing application (Release, win-x64)..."
dotnet publish ZeroMix.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -o $PublishDir `
    -p:Version=$Version

if (!(Test-Path "$PublishDir/ZeroMix.exe")) {
    Write-Error-Custom "Build failed: ZeroMix.exe not found"
    exit 1
}

Write-Success "Application built: $PublishDir/ZeroMix.exe"

# ============================================================================
# STEP 4: DOWNLOAD FFMPEG
# ============================================================================

Write-Section "STEP 4: Download FFMPEG"

$FFMPEGDir = "Tools/FFMPEG"
if (!(Test-Path $FFMPEGDir)) {
    New-Item -ItemType Directory -Force -Path $FFMPEGDir | Out-Null
}

Write-Step "Downloading FFMPEG..."
$FFMPEGZip = "ffmpeg.zip"
Invoke-WebRequest -Uri $FFMPEGUrl -OutFile $FFMPEGZip -ErrorAction Stop

Write-Step "Extracting FFMPEG..."
Expand-Archive $FFMPEGZip -DestinationPath "ffmpeg_temp" -Force
$binDir = Get-ChildItem -Path "ffmpeg_temp" -Filter "bin" -Recurse | Select-Object -First 1
Copy-Item "$($binDir.FullName)\ffmpeg.exe" -Destination "$FFMPEGDir/ffmpeg.exe" -Force

Write-Step "Cleaning up..."
Remove-Item "ffmpeg_temp" -Recurse -Force
Remove-Item $FFMPEGZip -Force

Write-Success "FFMPEG ready: $FFMPEGDir/ffmpeg.exe"

# ============================================================================
# STEP 5: BUILD INSTALLER
# ============================================================================

Write-Section "STEP 5: Build Installer with Inno Setup"

Write-Step "Checking Inno Setup..."
$InnoSetup = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if (!(Test-Path $InnoSetup)) {
    Write-Error-Custom "Inno Setup not found at: $InnoSetup"
    Write-Host "Install from: https://jrsoftware.org/isdl.php"
    exit 1
}

Write-Step "Building installer..."
& $InnoSetup "/DAppVersion=$Version" $SetupScript

$SetupExe = "Exe/ZeroMix-Setup-v$Version.exe"
if (!(Test-Path $SetupExe)) {
    Write-Error-Custom "Installer build failed: $SetupExe not found"
    exit 1
}

Write-Success "Installer built: $SetupExe"

# ============================================================================
# STEP 6: CREATE PORTABLE ZIP
# ============================================================================

Write-Section "STEP 6: Create Portable ZIP"

Write-Step "Creating portable package..."
$PortableZip = "ZeroMix-v$Version-Portable.zip"
Compress-Archive -Path "$PublishDir/*" -DestinationPath $PortableZip -Force

Write-Success "Portable ZIP created: $PortableZip"

# ============================================================================
# STEP 7: SIGN EXECUTABLE (OPTIONAL)
# ============================================================================

if (-not $SkipSign) {
    Write-Section "STEP 7: Sign Executable"
    
    if (!(Test-Path $CertPath)) {
        Write-Error-Custom "Certificate not found: $CertPath"
        Write-Host "Skipping signing..."
    } else {
        Write-Step "Checking osslsigncode..."
        if (!(Get-Command osslsigncode -ErrorAction SilentlyContinue)) {
            Write-Step "Installing osslsigncode via chocolatey..."
            choco install osslsigncode -y
        }
        
        if ([string]::IsNullOrEmpty($CertPassword)) {
            Write-Host "🔑 Enter certificate password (will not be displayed):" -ForegroundColor Yellow
            $SecurePassword = Read-Host -AsSecureString
            $CertPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto(
                [System.Runtime.InteropServices.Marshal]::SecureStringToCoTaskMemUnicode($SecurePassword)
            )
        }
        
        Write-Step "Signing installer..."
        $SignedExe = "ZeroMix-v$Version-Setup.exe"
        
        osslsigncode sign `
            -pkcs12 $CertPath `
            -pass $CertPassword `
            -n "$AppName" `
            -i "https://zeromix.vercel.app" `
            -t $TimestampServer `
            -in $SetupExe `
            -out $SignedExe
        
        if (!(Test-Path $SignedExe)) {
            Write-Error-Custom "Signing failed"
            exit 1
        }
        
        Write-Success "Executable signed: $SignedExe"
        
        # Replace original with signed version
        Remove-Item $SetupExe -Force
        Rename-Item $SignedExe $SetupExe
        Write-Success "Replaced original with signed version"
    }
} else {
    Write-Section "STEP 7: Sign Executable (SKIPPED)"
    Write-Host "Use -SkipSign:$false to enable signing"
}

# ============================================================================
# STEP 8: PREPARE ARTIFACTS
# ============================================================================

Write-Section "STEP 8: Prepare Artifacts"

Write-Step "Moving setup executable..."
$FinalSetupExe = "ZeroMix-v$Version-Setup.exe"
if (Test-Path $SetupExe) {
    Copy-Item $SetupExe $FinalSetupExe -Force
    Write-Success "Setup: $FinalSetupExe"
}

Write-Host ""
Write-Host "📦 Artifacts Ready:" -ForegroundColor Cyan
Write-Host "  1. $FinalSetupExe ($([math]::Round((Get-Item $FinalSetupExe).Length/1MB, 1)) MB)"
Write-Host "  2. $PortableZip ($([math]::Round((Get-Item $PortableZip).Length/1MB, 1)) MB)"

# ============================================================================
# STEP 9: UPLOAD TO GITHUB (OPTIONAL)
# ============================================================================

if (-not $SkipUpload) {
    Write-Section "STEP 9: Upload to GitHub Release"
    
    Write-Step "Checking GitHub CLI..."
    if (!(Get-Command gh -ErrorAction SilentlyContinue)) {
        Write-Error-Custom "GitHub CLI not found"
        Write-Host "Install from: https://cli.github.com/"
        Write-Host "Skipping upload..."
    } else {
        if ([string]::IsNullOrEmpty($Repository)) {
            Write-Step "Detecting repository..."
            $Repository = gh repo view --json nameWithOwner -q '.nameWithOwner'
        }
        
        Write-Host "Repository: $Repository" -ForegroundColor Cyan
        
        $Tag = "v$Version"
        Write-Step "Checking if release exists: $Tag"
        
        $ReleaseExists = gh release view $Tag --repo $Repository 2>$null
        
        if ($ReleaseExists) {
            Write-Step "Release exists, uploading artifacts..."
            gh release upload $Tag $FinalSetupExe $PortableZip --repo $Repository --clobber
        } else {
            Write-Step "Creating new release: $Tag"
            gh release create $Tag $FinalSetupExe $PortableZip `
                --repo $Repository `
                --title "$AppName $Tag" `
                --notes "🎉 **$AppName $Tag** is here!

### 📥 Downloads
- **Setup Installer:** \`$FinalSetupExe\` (Recommended)
- **Portable Version:** \`$PortableZip\`

### 🖥️ Requirements
- Windows 10/11 x64
- .NET 9.0 Desktop Runtime

Check [CHANGELOG.md](https://github.com/$Repository/blob/main/CHANGELOG.md) for details."
        }
        
        Write-Success "Upload complete!"
        Write-Host "📥 Release: https://github.com/$Repository/releases/tag/$Tag" -ForegroundColor Green
    }
} else {
    Write-Section "STEP 9: Upload to GitHub (SKIPPED)"
    Write-Host "Use -SkipUpload:$false to enable upload"
}

# ============================================================================
# SUMMARY
# ============================================================================

Write-Section "BUILD COMPLETE ✅"

Write-Host "📊 Summary:" -ForegroundColor Cyan
Write-Host "  Version: $Version"
Write-Host "  Setup: $FinalSetupExe"
Write-Host "  Portable: $PortableZip"
Write-Host "  Signed: $(if ($SkipSign) { 'No' } else { 'Yes' })"
Write-Host "  Uploaded: $(if ($SkipUpload) { 'No' } else { 'Yes' })"
Write-Host ""
Write-Host "🎉 Ready for release!" -ForegroundColor Green
