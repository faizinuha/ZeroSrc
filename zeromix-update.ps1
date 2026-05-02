# ZeroMix Auto-Updater Script
# Jalankan script ini untuk download dan install versi terbaru ZeroMix

$REPO = "faizinuha/ZeroMix"
$API_URL = "https://api.github.com/repos/$REPO/releases/latest"
$CURRENT_VERSION = "6.9.6"

Write-Host ""
Write-Host "  ╔══════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "  ║       ZeroMix Auto-Updater           ║" -ForegroundColor Cyan
Write-Host "  ╚══════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Versi saat ini: v$CURRENT_VERSION" -ForegroundColor Yellow
Write-Host "  Mengecek update dari GitHub..." -ForegroundColor Gray
Write-Host ""

try {
    # Cek versi terbaru
    $headers = @{ "User-Agent" = "ZeroMix-Updater" }
    $release = Invoke-RestMethod -Uri $API_URL -Headers $headers -TimeoutSec 10
    
    $latestTag = $release.tag_name
    $latestVer = $latestTag.TrimStart('v')
    $releaseUrl = $release.html_url
    
    Write-Host "  Versi terbaru: $latestTag" -ForegroundColor Green
    Write-Host ""
    
    # Bandingkan versi
    $current = [Version]$CURRENT_VERSION
    $latest  = [Version]$latestVer
    
    if ($latest -le $current) {
        Write-Host "  ✅ Kamu sudah menggunakan versi terbaru!" -ForegroundColor Green
        Write-Host ""
        Write-Host "  Tekan Enter untuk keluar..." -ForegroundColor Gray
        Read-Host
        exit 0
    }
    
    Write-Host "  🆕 Update tersedia: $latestTag" -ForegroundColor Cyan
    Write-Host ""
    
    # Cari Setup*.exe di assets
    $setupAsset = $release.assets | Where-Object { $_.name -like "*Setup*" -and $_.name -like "*.exe" } | Select-Object -First 1
    
    if (-not $setupAsset) {
        Write-Host "  ⚠️  File installer tidak ditemukan di release." -ForegroundColor Yellow
        Write-Host "  Buka halaman release secara manual:" -ForegroundColor Gray
        Write-Host "  $releaseUrl" -ForegroundColor Cyan
        Write-Host ""
        Start-Process $releaseUrl
        Write-Host "  Tekan Enter untuk keluar..." -ForegroundColor Gray
        Read-Host
        exit 0
    }
    
    $downloadUrl  = $setupAsset.browser_download_url
    $fileName     = $setupAsset.name
    $fileSizeMB   = [math]::Round($setupAsset.size / 1MB, 1)
    $downloadPath = Join-Path $env:TEMP $fileName
    
    Write-Host "  📦 File    : $fileName ($fileSizeMB MB)" -ForegroundColor White
    Write-Host "  📥 URL     : $downloadUrl" -ForegroundColor Gray
    Write-Host ""
    
    $confirm = Read-Host "  Download dan install sekarang? (Y/N)"
    if ($confirm -notmatch "^[Yy]") {
        Write-Host ""
        Write-Host "  Dibatalkan. Buka halaman release untuk download manual:" -ForegroundColor Yellow
        Write-Host "  $releaseUrl" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "  Tekan Enter untuk keluar..." -ForegroundColor Gray
        Read-Host
        exit 0
    }
    
    Write-Host ""
    Write-Host "  ⬇️  Mendownload $fileName..." -ForegroundColor Cyan
    
    # Download dengan progress
    $webClient = New-Object System.Net.WebClient
    $webClient.Headers.Add("User-Agent", "ZeroMix-Updater")
    
    $startTime = Get-Date
    $webClient.DownloadFile($downloadUrl, $downloadPath)
    $elapsed = ((Get-Date) - $startTime).TotalSeconds
    
    Write-Host "  ✅ Download selesai! ($([math]::Round($elapsed, 1)) detik)" -ForegroundColor Green
    Write-Host ""
    Write-Host "  🚀 Menjalankan installer..." -ForegroundColor Cyan
    Write-Host "  ZeroMix akan ditutup otomatis saat install." -ForegroundColor Gray
    Write-Host ""
    
    # Jalankan installer
    Start-Process -FilePath $downloadPath -ArgumentList "/closeapplications /restartapplications" -Wait
    
    Write-Host "  ✅ Update selesai!" -ForegroundColor Green
    Write-Host ""
    Write-Host "  Tekan Enter untuk keluar..." -ForegroundColor Gray
    Read-Host

} catch {
    Write-Host "  ❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "  Coba download manual di:" -ForegroundColor Yellow
    Write-Host "  https://github.com/$REPO/releases/latest" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  Tekan Enter untuk keluar..." -ForegroundColor Gray
    Read-Host
    exit 1
}
