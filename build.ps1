# Skrip untuk membuat dan menandatangani installer ZeroMix.

# Hentikan jika ada error
$ErrorActionPreference = "Stop"

Write-Host "Langkah 1: Mengkompilasi installer ZeroMix..." -ForegroundColor Green
& "C:\Program Files (x86)\Inno Setup 6\iscc.exe" "c:\ZeroMix\ZeroMix\Exe\Setup.iss"
Write-Host "Kompilasi Selesai." -ForegroundColor Green

Write-Host ""
Write-Host "Langkah 2: Menandatangani installer..." -ForegroundColor Green

# Tentukan path
$SignTool = "C:\ZeroMix\ZeroMix\Exe\bin\osslsigncode.exe"
$CertFile = "C:\ZeroMix\ZeroMix\Exe\ZeroMixCert.pfx"
$InstallerFile = "C:\ZeroMix\ZeroMix\Exe\ZeroMix-Setup.exe"
$TempInstallerFile = "C:\ZeroMix\ZeroMix\Exe\ZeroMix-Setup-signed.exe"

# Langkah 2a: Tandatangani ke file sementara
Write-Host " - Menandatangani ke file sementara..."
& $SignTool sign -pkcs12 $CertFile -pass "ZeroMixPass" -n "ZeroMix" -i "https://zeromix.pages.dev" -in $InstallerFile -out $TempInstallerFile -t http://timestamp.digicert.com

# Langkah 2b: Hapus installer lama yang belum ditandatangani
Write-Host " - Menghapus installer lama..."
Remove-Item $InstallerFile

# Langkah 2c: Ganti nama installer baru yang sudah ditandatangani
Write-Host " - Mengganti nama installer baru..."
Rename-Item $TempInstallerFile -NewName $InstallerFile

Write-Host "Penandatanganan Selesai." -ForegroundColor Green
Write-Host ""
Write-Host "Build Berhasil! Installer Anda ada di: $InstallerFile" -ForegroundColor Cyan