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
