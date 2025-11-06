# Script untuk mengotomatiskan build installer ZeroMix

# --- Langkah 0: Publish Aplikasi ---
Write-Host "Langkah 0: Mem-publish aplikasi ZeroMix..."
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -p:PublishTrimmed=true --self-contained true -o "..\publish\win-x64"

# --- Konfigurasi ---
$ErrorActionPreference = "Stop"

Write-Host "Langkah 1: Mengkompilasi installer ZeroMix..." -ForegroundColor Green
& "C:\Program Files (x86)\Inno Setup 6\iscc.exe" "c:\ZeroMix\ZeroMix\Exe\Setup.iss"
Write-Host "Kompilasi selesai." -ForegroundColor Green

Write-Host "Langkah 2: Menandatangani installer..." -ForegroundColor Green
$SignTool = "C:\ZeroMix\ZeroMix\Exe\bin\osslsigncode.exe"
$CertFile = "C:\ZeroMix\ZeroMix\Exe\ZeroMixCert.pfx"
$InstallerFile = "C:\ZeroMix\ZeroMix\Exe\ZeroMix-Setup.exe"
$TempInstallerFile = "C:\ZeroMix\ZeroMix\Exe\ZeroMix-Setup-signed.exe"

& $SignTool sign -pkcs12 $CertFile -pass "ZeroMixPass" -n "ZeroMix" -i "https://zeromix.pages.dev" -in $InstallerFile -out $TempInstallerFile -t http://timestamp.digicert.com

if ($LASTEXITCODE -eq 0) {
    Write-Host "🎉 Penandatanganan berhasil!"
    Remove-Item $InstallerFile
    Rename-Item $TempInstallerFile -NewName $InstallerFile
} else {
    Write-Host "❌ Penandatanganan gagal!" -ForegroundColor Red
}

Write-Host "Build berhasil! Installer Anda ada di: $InstallerFile" -ForegroundColor Cyan
