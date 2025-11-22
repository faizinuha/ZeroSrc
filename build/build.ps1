# Script untuk mengotomatiskan build installer ZeroMix
# Simplified version dengan JavaScript CLI

$ErrorActionPreference = "Stop"

Write-Host "`n=== ZeroMix Build Script ===" -ForegroundColor Cyan

# --- Step 1: Publish Aplikasi WPF ---
Write-Host "`nLangkah 1: Mem-publish aplikasi ZeroMix..." -ForegroundColor Green
try {
    # Change to project root directory first
    Push-Location ..
    dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -p:PublishTrimmed=true -p:PublishReadyToRun=true --self-contained
    Pop-Location
    Write-Host "✅ Publish aplikasi berhasil" -ForegroundColor Green
} catch {
    Pop-Location
    Write-Host "❌ Publish aplikasi gagal: $_" -ForegroundColor Red
    exit 1
}

# --- Step 2: Kompilasi Installer dengan Inno Setup ---
Write-Host "`nLangkah 2: Mengkompilasi installer dengan Inno Setup..." -ForegroundColor Green

$InnoSetup = "C:\Program Files (x86)\Inno Setup 6\iscc.exe"
if (-not (Test-Path $InnoSetup)) {
    Write-Host "⚠️  Inno Setup tidak ditemukan di: $InnoSetup" -ForegroundColor Yellow
    Write-Host "💡 Download dari: https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
    Write-Host "⚠️  Atau gunakan path berbeda jika terinstall di lokasi lain" -ForegroundColor Yellow
    exit 1
}

$SetupScript = ".\Exe\Setup.iss"
if (-not (Test-Path $SetupScript)) {
    Write-Host "❌ Setup.iss tidak ditemukan di $SetupScript" -ForegroundColor Red
    exit 1
}

try {
    & $InnoSetup $SetupScript
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Kompilasi installer gagal (Exit code: $LASTEXITCODE)" -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ Installer berhasil dikompilasi" -ForegroundColor Green
} catch {
    Write-Host "❌ Kompilasi installer gagal: $_" -ForegroundColor Red
    exit 1
}

# --- Step 2.5: Menandatangani Installer ---
Write-Host "`nLangkah 2.5: Menandatangani installer dengan osslsigncode..." -ForegroundColor Green

# Path ke osslsigncode.exe. Asumsi berada di PATH atau di folder yang sama.
$SignTool = ".\Exe\bin\osslsigncode.exe"
$CertFile = ".\Exe\ZeroMixCert.pfx"
$InstallerFile = ".\Exe\ZeroMix-Setup-v2.1.0.exe"
$TempInstallerFile = ".\Exe\ZeroMix-Setup-v2.1.0-signed.exe"
# $password = "ZeroMixPass"
$CertPassword = "ZeroMixPass"


if (-not (Test-Path $CertFile)) {
    Write-Host "❌ File sertifikat tidak ditemukan di $CertFile" -ForegroundColor Red
    exit 1
}

# Pastikan osslsigncode.exe dapat ditemukan
if (-not (Test-Path $SignTool)) {
    Write-Host "❌ Perintah '$SignTool' tidak ditemukan. Pastikan osslsigncode.exe ada di PATH atau di direktori ini." -ForegroundColor Red
    exit 1
}

try {
    # Gunakan timestamp server untuk memastikan tanda tangan valid bahkan setelah sertifikat kedaluwarsa.
    & $SignTool sign -pkcs12 $CertFile -pass $CertPassword -n "ZeroMix" -i "https://zeromix.pages.dev" -h sha256 -t http://timestamp.digicert.com -in $InstallerFile -out $TempInstallerFile
    
    # Hapus installer asli yang belum ditandatangani
    Remove-Item $InstallerFile -Force
    # Ganti nama file yang sudah ditandatangani menjadi nama file installer asli
    Rename-Item -Path $TempInstallerFile -NewName (Split-Path $InstallerFile -Leaf)

    Write-Host "✅ Installer berhasil ditandatangani" -ForegroundColor Green
} catch {
    Write-Host "❌ Penandatanganan installer gagal: $_" -ForegroundColor Red
    exit 1
}

# --- Step 3: Cek Output ---
Write-Host "`nLangkah 3: Verifikasi output..." -ForegroundColor Green

$InstallerOutput = ".\Exe\ZeroMix-Setup-v2.1.0.exe"
if (Test-Path $InstallerOutput) {
    $Size = (Get-Item $InstallerOutput).Length / 1MB
    Write-Host "✅ Installer berhasil dibuat!" -ForegroundColor Green
    Write-Host "📦 File: $InstallerOutput" -ForegroundColor Cyan
    Write-Host "📊 Ukuran: $([Math]::Round($Size, 2)) MB" -ForegroundColor Cyan
} else {
    Write-Host "❌ Installer output tidak ditemukan" -ForegroundColor Red
    exit 1
}

Write-Host "`n=== Build Selesai ===" -ForegroundColor Green
Write-Host "🎉 Siap untuk distribusi!" -ForegroundColor Green
Write-Host "`n📝 Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Test installer di Windows 10/11" -ForegroundColor Gray
Write-Host "  2. Jalankan: zeromix-cli cek-update" -ForegroundColor Gray
Write-Host "  3. Upload ke GitHub Releases" -ForegroundColor Gray
Write-Host ""
