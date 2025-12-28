# ==============================================================================
# ZeroMix Build & Installer Automation Script
# Clean, Professional, Maintainable
# ==============================================================================

$ErrorActionPreference = "Stop"

# ------------------------------------------------------------------------------
# CONFIG
# ------------------------------------------------------------------------------
$ProjectRoot      = Resolve-Path ".."
$ExeDir           = ".\Exe"
$SetupScript      = "$ExeDir\Setup.iss"
$InnoSetup        = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

$AppName          = "ZeroMix"
$Version          = "2.3.7"

$InstallerName    = "$AppName-Setup-v$Version.exe"
$InstallerPath    = "$ExeDir\$InstallerName"
$SignedInstaller  = "$ExeDir\$AppName-Setup-v$Version-signed.exe"

# Signing
$SignTool         = "$ExeDir\bin\osslsigncode.exe"
$CertFile         = "$ExeDir\ZeroMixCert.pfx"
$CertPassword     = "ZeroMixPass"
$TimestampServer  = "http://timestamp.digicert.com"

# ------------------------------------------------------------------------------
# HELPER FUNCTIONS
# ------------------------------------------------------------------------------
function Info($msg)  { Write-Host $msg -ForegroundColor Cyan }
function Ok($msg)    { Write-Host "✅ $msg" -ForegroundColor Green }
function Warn($msg)  { Write-Host "⚠️  $msg" -ForegroundColor Yellow }
function Fail($msg)  { Write-Host "❌ $msg" -ForegroundColor Red; exit 1 }

# ------------------------------------------------------------------------------
# START
# ------------------------------------------------------------------------------
Info "`n=== ZeroMix Build Script ==="

# ------------------------------------------------------------------------------
# STEP 1: Publish App
# ------------------------------------------------------------------------------
Info "`n[1/4] Publishing ZeroMix application..."

try {
    Push-Location $ProjectRoot

    dotnet publish `
        -c Release `
        -r win-x64 `
        -p:PublishSingleFile=true `
        -p:PublishTrimmed=true `
        -p:PublishReadyToRun=true `
        --self-contained

    Pop-Location
    Ok "Publish aplikasi berhasil"
}
catch {
    Pop-Location
    Fail "Publish aplikasi gagal: $_"
}

# ------------------------------------------------------------------------------
# STEP 2: Compile Installer
# ------------------------------------------------------------------------------
Info "`n[2/4] Compiling installer with Inno Setup..."

if (-not (Test-Path $InnoSetup)) {
    Fail "Inno Setup tidak ditemukan di: $InnoSetup"
}

if (-not (Test-Path $SetupScript)) {
    Fail "Setup.iss tidak ditemukan di: $SetupScript"
}

& $InnoSetup $SetupScript
if ($LASTEXITCODE -ne 0) {
    Fail "Kompilasi installer gagal (Exit code: $LASTEXITCODE)"
}

Ok "Installer berhasil dikompilasi"

# ------------------------------------------------------------------------------
# STEP 3: Code Signing
# ------------------------------------------------------------------------------
Info "`n[3/4] Signing installer..."

if (-not (Test-Path $SignTool)) {
    Fail "osslsigncode tidak ditemukan di: $SignTool"
}

if (-not (Test-Path $CertFile)) {
    Fail "Sertifikat tidak ditemukan di: $CertFile"
}

if (-not (Test-Path $InstallerPath)) {
    Fail "Installer tidak ditemukan di: $InstallerPath"
}

& $SignTool sign `
    -pkcs12 $CertFile `
    -pass $CertPassword `
    -n $AppName `
    -i "https://zeromix.vercel.app" `
    -t $TimestampServer `
    -in  $InstallerPath `
    -out $SignedInstaller

if ($LASTEXITCODE -ne 0) {
    Fail "Penandatanganan installer gagal"
}

Remove-Item $InstallerPath -Force
Rename-Item $SignedInstaller $InstallerName

Ok "Installer berhasil ditandatangani"

# ------------------------------------------------------------------------------
# STEP 4: Verify Output
# ------------------------------------------------------------------------------
Info "`n[4/4] Verifying output..."

if (-not (Test-Path $InstallerPath)) {
    Fail "Installer output tidak ditemukan"
}

$SizeMB = [Math]::Round((Get-Item $InstallerPath).Length / 1MB, 2)

Ok "Build selesai!"
Info "📦 File  : $InstallerPath"
Info "📊 Ukuran: $SizeMB MB"

# ------------------------------------------------------------------------------
# DONE
# ------------------------------------------------------------------------------
Info "`n=== BUILD COMPLETE ==="
Write-Host "🎉 ZeroMix siap didistribusikan!" -ForegroundColor Green
Write-Host "📝 Next Steps:" -ForegroundColor Yellow
Write-Host "  • Test installer di Windows 10 / 11" -ForegroundColor Gray
Write-Host "  • Upload ke GitHub Releases" -ForegroundColor Gray
Write-Host ""
