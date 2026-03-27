# install.ps1 untuk ZeroMix
# Script ini digunakan untuk instalasi cepat via terminal (One-Liner Install)
# Cara pakai: iwr -useb bit.ly/download-zeromix | iex

# Konfigurasi Repository
$repo = "faizinuha/ZeroMix"
$tagUri = "https://api.github.com/repos/$repo/releases/latest"

Clear-Host
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "        ZeroMix CLI Installer             " -ForegroundColor Bold
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Mencari versi terbaru di GitHub..." -ForegroundColor Yellow

try {
    # 1. Mengambil informasi release terbaru dari GitHub API
    $latest = Invoke-RestMethod -Uri $tagUri -ErrorAction Stop
    $version = $latest.tag_name
    
    # Cari file asset yang merupakan installer .exe (biasanya keluaran Inno Setup)
    $asset = $latest.assets | Where-Object { $_.name -like "*-Setup-*.exe" -or ($_.name -like "*.exe" -and $_.name -notlike "*Portable*") } | Select-Object -First 1
    
    if (-not $asset) {
        throw "Tidak menemukan file installer (.exe) yang valid di release GitHub."
    }

    $downloadUrl = $asset.browser_download_url
    $fileName = $asset.name
    $tempPath = Join-Path $env:TEMP "$fileName"

    Write-Host "Ditemukan versi: $version ($fileName)" -ForegroundColor Green
    Write-Host "Sedang mengunduh dari: GitHub Assets..." -ForegroundColor Yellow
    
    # 2. Proses Download (Gunakan basic parsing agar ringan di PowerShell lama)
    $progressPreference = 'SilentlyContinue' # Sembunyikan progress bar agar lebih bersih di terminal
    Invoke-WebRequest -Uri $downloadUrl -OutFile $tempPath -UseBasicParsing

    Write-Host "Download selesai! Sedang menjalankan instalasi..." -ForegroundColor Green
    Write-Host "(Ini mungkin memerlukan izin Administrator)" -ForegroundColor Yellow

    # 3. Menjalankan installer secara Silent (Asumsi menggunakan Inno Setup atau sejenisnya)
    # /VERYSILENT /SUPPRESSMSGBOXES: Argumen standar Inno Setup
    # /S: Argumen standar untuk NSIS / installer lain
    $process = Start-Process -FilePath $tempPath -ArgumentList "/VERYSILENT /SUPPRESSMSGBOXES /NOREBOOT" -Wait -PassThru
    
    if ($process.ExitCode -eq 0) {
        Write-Host "==========================================" -ForegroundColor Green
        Write-Host "  ZeroMix v$version Berhasil Terpasang!    " -ForegroundColor Bold
        Write-Host "==========================================" -ForegroundColor Green
        Write-Host "Silahkan periksa Start Menu Kakak."
    } else {
        Write-Host "Instalasi selesai dengan kode keluar: $($process.ExitCode)" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "------------------------------------------" -ForegroundColor Red
    Write-Host "ERROR: Gagal memasang ZeroMix." -ForegroundColor Red
    Write-Host "$($_.Exception.Message)" -ForegroundColor Red
    Write-Host "------------------------------------------" -ForegroundColor Red
}
finally {
    # 4. Cleanup: Hapus file installer sementara untuk menjaga kebersihan sistem
    if (Test-Path $tempPath) {
        try { Remove-Item $tempPath -Force -ErrorAction SilentlyContinue } catch {}
    }
}
