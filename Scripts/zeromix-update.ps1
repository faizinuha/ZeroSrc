#Requires -Version 5.1
# ==============================
# ZeroMix Updater Script
# ==============================

$ErrorActionPreference = "Stop"

# ── Config ─────────────────────────────────────────────────────────────────
$AppName      = "ZeroMix"
$Repo         = "faizinuha/ZeroMix"
$ApiUrl       = "https://api.github.com/repos/$Repo/releases/latest"
$InstallDir   = Split-Path -Parent $MyInvocation.MyCommand.Path
$TempDir      = $env:TEMP
$LogDir       = Join-Path $InstallDir "logs"
$LogFile      = Join-Path $LogDir "updater.log"

# Baca versi saat ini dari assembly
$ExePath      = Join-Path $InstallDir "ZeroMix.exe"
$CurrentVer   = if (Test-Path $ExePath) {
    try { [System.Diagnostics.FileVersionInfo]::GetVersionInfo($ExePath).ProductVersion.Split('+')[0] }
    catch { "0.0.0" }
} else { "0.0.0" }

# ── Helpers ─────────────────────────────────────────────────────────────────
function Write-Log {
    param([string]$Level, [string]$Message)
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $line = "[$timestamp] [$Level] $Message"
    Add-Content -Path $LogFile -Value $line -Encoding UTF8
    switch ($Level) {
        "INFO"    { Write-Host "  $Message" -ForegroundColor White }
        "SUCCESS" { Write-Host "  ✅ $Message" -ForegroundColor Green }
        "WARN"    { Write-Host "  ⚠️  $Message" -ForegroundColor Yellow }
        "ERROR"   { Write-Host "  ❌ $Message" -ForegroundColor Red }
    }
}

function Compare-Versions {
    param([string]$Latest, [string]$Current)
    try {
        return ([Version]$Latest) -gt ([Version]$Current)
    } catch {
        return $Latest -ne $Current
    }
}

# ── Init ────────────────────────────────────────────────────────────────────
if (-not (Test-Path $LogDir)) { New-Item -ItemType Directory -Path $LogDir -Force | Out-Null }

Clear-Host
Write-Host ""
Write-Host "  ╔══════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "  ║        ZeroMix Auto-Updater          ║" -ForegroundColor Cyan
Write-Host "  ╚══════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Versi saat ini : v$CurrentVer" -ForegroundColor Yellow
Write-Host "  Mengecek update..." -ForegroundColor Gray
Write-Host ""

Write-Log "INFO" "Starting updater. Current version: $CurrentVer"

# ── Cek GitHub API ──────────────────────────────────────────────────────────
try {
    $headers  = @{ "User-Agent" = "ZeroMix-Updater/$CurrentVer" }
    $release  = Invoke-RestMethod -Uri $ApiUrl -Headers $headers -TimeoutSec 15
    $latestTag = $release.tag_name
    $latestVer = $latestTag.TrimStart('v')
    $releaseUrl = $release.html_url
    $releaseBody = $release.body

    Write-Host "  Versi terbaru  : $latestTag" -ForegroundColor Cyan
    Write-Log "INFO" "Latest version: $latestTag"

    if (-not (Compare-Versions -Latest $latestVer -Current $CurrentVer)) {
        Write-Host ""
        Write-Log "SUCCESS" "Already up to date."
        Write-Host "  Kamu sudah menggunakan versi terbaru!" -ForegroundColor Green
        Write-Host ""
        Write-Host "  Tekan Enter untuk keluar..." -ForegroundColor Gray
        Read-Host
        exit 0
    }

    # Tampilkan highlights release notes
    if ($releaseBody) {
        $lines = ($releaseBody -split "`n" | Select-Object -First 8) -join "`n"
        Write-Host ""
        Write-Host "  ── What's New ──────────────────────────" -ForegroundColor DarkGray
        $lines -split "`n" | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
        Write-Host "  ────────────────────────────────────────" -ForegroundColor DarkGray
        Write-Host ""
    }

} catch {
    Write-Log "ERROR" "Failed to check update: $_"
    Write-Host ""
    Write-Host "  Gagal cek update. Periksa koneksi internet." -ForegroundColor Red
    Write-Host "  Download manual: https://github.com/$Repo/releases" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  Tekan Enter untuk keluar..." -ForegroundColor Gray
    Read-Host
    exit 1
}

# ── Cari asset installer ────────────────────────────────────────────────────
$asset = $release.assets | Where-Object {
    $_.name -like "*Setup*" -and $_.name -like "*.exe"
} | Select-Object -First 1

if (-not $asset) {
    Write-Log "WARN" "No installer asset found in release."
    Write-Host "  Installer tidak ditemukan. Buka halaman release:" -ForegroundColor Yellow
    Write-Host "  $releaseUrl" -ForegroundColor Cyan
    Start-Process $releaseUrl
    Write-Host ""
    Write-Host "  Tekan Enter untuk keluar..." -ForegroundColor Gray
    Read-Host
    exit 0
}

$downloadUrl  = $asset.browser_download_url
$fileName     = $asset.name
$fileSizeMB   = [math]::Round($asset.size / 1MB, 1)
$downloadPath = Join-Path $TempDir $fileName

Write-Host "  Update tersedia: $latestTag" -ForegroundColor Green
Write-Host ""
Write-Host "  File    : $fileName ($fileSizeMB MB)" -ForegroundColor White
Write-Host ""

$confirm = Read-Host "  Download dan install sekarang? (Y/N)"
if ($confirm -notmatch "^[Yy]") {
    Write-Log "INFO" "User cancelled update."
    Write-Host ""
    Write-Host "  Dibatalkan. Buka halaman release untuk download manual:" -ForegroundColor Yellow
    Write-Host "  $releaseUrl" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  Tekan Enter untuk keluar..." -ForegroundColor Gray
    Read-Host
    exit 0
}

# ── Download ────────────────────────────────────────────────────────────────
Write-Host ""
Write-Log "INFO" "Downloading $fileName..."
Write-Host "  ⬇️  Mendownload $fileName..." -ForegroundColor Cyan

try {
    # Hapus file lama jika ada
    if (Test-Path $downloadPath) { Remove-Item $downloadPath -Force }

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $webClient = New-Object System.Net.WebClient
    $webClient.Headers.Add("User-Agent", "ZeroMix-Updater/$CurrentVer")
    $webClient.DownloadFile($downloadUrl, $downloadPath)
    $stopwatch.Stop()

    $elapsed = [math]::Round($stopwatch.Elapsed.TotalSeconds, 1)
    Write-Log "SUCCESS" "Download completed in ${elapsed}s"
    Write-Host "  Download selesai! ($elapsed detik)" -ForegroundColor Green

} catch {
    Write-Log "ERROR" "Download failed: $_"
    Write-Host ""
    Write-Host "  Download gagal: $_" -ForegroundColor Red
    Write-Host "  Download manual: $downloadUrl" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  Tekan Enter untuk keluar..." -ForegroundColor Gray
    Read-Host
    exit 1
}

# ── Install ─────────────────────────────────────────────────────────────────
Write-Host ""
Write-Log "INFO" "Launching installer: $downloadPath"
Write-Host "  🚀 Menjalankan installer..." -ForegroundColor Cyan
Write-Host "  ZeroMix akan ditutup otomatis saat install." -ForegroundColor Gray
Write-Host ""

try {
    Start-Process -FilePath $downloadPath -ArgumentList "/closeapplications /restartapplications" -Wait
    Write-Log "SUCCESS" "Installer completed."
    Write-Host "  ✅ Update selesai!" -ForegroundColor Green
} catch {
    Write-Log "ERROR" "Installer failed: $_"
    Write-Host "  Gagal menjalankan installer: $_" -ForegroundColor Red
    Write-Host "  Coba jalankan manual: $downloadPath" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "  Tekan Enter untuk keluar..." -ForegroundColor Gray
Read-Host
