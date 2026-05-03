#Requires -Version 5.1
<#
.SYNOPSIS
    ZeroMix Installer & Updater
.DESCRIPTION
    Professional installer script for ZeroMix Desktop Suite.
    Safe: uses only built-in .NET WebClient, no irm/curl.
    
    Run via internet (one-liner):
        irm https://raw.githubusercontent.com/faizinuha/ZeroMix/main/ZeroMix.ps1 | iex
    
    Or download and run locally:
        powershell -ExecutionPolicy Bypass -File ZeroMix.ps1
.NOTES
    Author  : ZeroMix Team
    GitHub  : https://github.com/faizinuha/ZeroMix
#>

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

# ── Constants ────────────────────────────────────────────────────────────────
$REPO        = "faizinuha/ZeroMix"
$API_URL     = "https://api.github.com/repos/$REPO/releases/latest"
$RELEASE_URL = "https://github.com/$REPO/releases/latest"
$TEMP_DIR    = $env:TEMP
$LOG_DIR     = Join-Path $env:APPDATA "ZeroMix\logs"
$LOG_FILE    = Join-Path $LOG_DIR "installer.log"

# Detect jika dijalankan via pipe (irm ... | iex) atau sebagai file
$IS_PIPED    = -not $MyInvocation.MyCommand.Path

# ── Colors ───────────────────────────────────────────────────────────────────
$C = @{
    Reset   = "`e[0m"
    Bold    = "`e[1m"
    Dim     = "`e[2m"
    Cyan    = "`e[96m"
    Green   = "`e[92m"
    Yellow  = "`e[93m"
    Red     = "`e[91m"
    Blue    = "`e[94m"
    Magenta = "`e[95m"
    White   = "`e[97m"
    Gray    = "`e[90m"
    BgBlue  = "`e[44m"
    BgCyan  = "`e[46m"
}

# ── Logging ──────────────────────────────────────────────────────────────────
function Write-Log {
    param([string]$Level, [string]$Message)
    if (-not (Test-Path $LOG_DIR)) { New-Item -ItemType Directory -Path $LOG_DIR -Force | Out-Null }
    $ts = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Add-Content -Path $LOG_FILE -Value "[$ts] [$Level] $Message" -Encoding UTF8
}

# ── UI Helpers ───────────────────────────────────────────────────────────────
function Write-Color {
    param([string]$Text, [string]$Color = $C.White, [switch]$NoNewline)
    if ($NoNewline) { Write-Host "$Color$Text$($C.Reset)" -NoNewline }
    else            { Write-Host "$Color$Text$($C.Reset)" }
}

function Write-Divider {
    param([string]$Color = $C.Gray)
    Write-Color "  $('─' * 50)" $Color
}

function Write-Banner {
    Clear-Host
    Write-Host ""
    Write-Color "  ╔══════════════════════════════════════════════════╗" $C.Cyan
    Write-Color "  ║                                                  ║" $C.Cyan
    Write-Color "  ║    ███████╗███████╗██████╗  ██████╗              ║" $C.Cyan
    Write-Color "  ║       ███╔╝██╔════╝██╔══██╗██╔═══██╗            ║" $C.Cyan
    Write-Color "  ║      ███╔╝ █████╗  ██████╔╝██║   ██║            ║" $C.Cyan
    Write-Color "  ║     ███╔╝  ██╔══╝  ██╔══██╗██║   ██║            ║" $C.Cyan
    Write-Color "  ║    ███████╗███████╗██║  ██║╚██████╔╝            ║" $C.Cyan
    Write-Color "  ║    ╚══════╝╚══════╝╚═╝  ╚═╝ ╚═════╝             ║" $C.Cyan
    Write-Color "  ║                                                  ║" $C.Cyan
    Write-Color "  ║         M I X                                    ║" $C.Magenta
    Write-Color "  ║    Smart Desktop Suite for Windows               ║" $C.Gray
    Write-Color "  ║                                                  ║" $C.Cyan
    Write-Color "  ╚══════════════════════════════════════════════════╝" $C.Cyan
    Write-Host ""
    if ($IS_PIPED) {
        Write-Color "  ⚡ Running via one-liner install" $C.Green
    }
    Write-Host ""
}

function Write-Step {
    param([int]$Num, [string]$Text)
    Write-Color "  $($C.Cyan)[$Num]$($C.Reset) $Text" $C.White
}

function Write-Status {
    param([string]$Label, [string]$Value, [string]$ValueColor = $C.Green)
    Write-Host "  $($C.Gray)$Label$($C.Reset)  $ValueColor$Value$($C.Reset)"
}

function Write-Progress-Bar {
    param([double]$Percent, [int]$Width = 40)
    $filled = [int]($Percent / 100 * $Width)
    $empty  = $Width - $filled
    $bar    = "$($C.Green)$('█' * $filled)$($C.Gray)$('░' * $empty)$($C.Reset)"
    $pct    = "$($C.Yellow)$([math]::Round($Percent, 1).ToString('0.0'))%$($C.Reset)"
    Write-Host "`r  $bar $pct   " -NoNewline
}

function Show-Spinner {
    param([string]$Message, [scriptblock]$Action)
    $frames = @('⠋','⠙','⠹','⠸','⠼','⠴','⠦','⠧','⠇','⠏')
    $job    = Start-Job -ScriptBlock $Action
    $i      = 0
    while ($job.State -eq 'Running') {
        $frame = $frames[$i % $frames.Count]
        Write-Host "`r  $($C.Cyan)$frame$($C.Reset) $Message   " -NoNewline
        Start-Sleep -Milliseconds 80
        $i++
    }
    Write-Host "`r  $($C.Green)✓$($C.Reset) $Message   "
    $result = Receive-Job $job
    Remove-Job $job
    return $result
}

# ── Version Compare ──────────────────────────────────────────────────────────
function Compare-SemVer {
    param([string]$Latest, [string]$Current)
    try   { return ([Version]($Latest -replace '[^0-9.]','')) -gt ([Version]($Current -replace '[^0-9.]','')) }
    catch { return $Latest -ne $Current }
}

# ── Get Installed Version ────────────────────────────────────────────────────
function Get-InstalledVersion {
    # Cek registry dulu (lebih reliable setelah install)
    $regPaths = @(
        "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*",
        "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*"
    )
    foreach ($reg in $regPaths) {
        $entry = Get-ItemProperty $reg -ErrorAction SilentlyContinue |
                 Where-Object { $_.DisplayName -like "*ZeroMix*" } |
                 Select-Object -First 1
        if ($entry -and $entry.DisplayVersion) { return $entry.DisplayVersion }
    }

    # Fallback: cari exe
    $paths = @(
        "$env:ProgramFiles\ZeroMix\ZeroMix.exe",
        "$env:LOCALAPPDATA\Programs\ZeroMix\ZeroMix.exe"
    )
    # Jika dijalankan sebagai file (bukan pipe), cek folder script juga
    if (-not $IS_PIPED -and $MyInvocation.MyCommand.Path) {
        $paths += Join-Path (Split-Path $MyInvocation.MyCommand.Path) "ZeroMix.exe"
    }
    foreach ($p in $paths) {
        if (Test-Path $p) {
            try { return [System.Diagnostics.FileVersionInfo]::GetVersionInfo($p).ProductVersion.Split('+')[0] }
            catch {}
        }
    }
    return $null
}

# ── Fetch Release Info ───────────────────────────────────────────────────────
function Get-LatestRelease {
    $wc = New-Object System.Net.WebClient
    $wc.Headers.Add("User-Agent", "ZeroMix-Installer/1.0")
    $wc.Headers.Add("Accept", "application/vnd.github.v3+json")
    $json = $wc.DownloadString($API_URL)
    return $json | ConvertFrom-Json
}

# ── Download with Progress ───────────────────────────────────────────────────
function Invoke-Download {
    param([string]$Url, [string]$Destination, [long]$FileSize)

    $wc = New-Object System.Net.WebClient
    $wc.Headers.Add("User-Agent", "ZeroMix-Installer/1.0")

    $downloaded = 0
    $sw         = [System.Diagnostics.Stopwatch]::StartNew()
    $lastBytes  = 0

    $wc.add_DownloadProgressChanged({
        param($s, $e)
        $script:downloaded = $e.BytesReceived
        $total = if ($e.TotalBytesToReceive -gt 0) { $e.TotalBytesToReceive } else { $FileSize }
        $pct   = if ($total -gt 0) { $e.BytesReceived / $total * 100 } else { 0 }

        if ($sw.ElapsedMilliseconds -gt 300) {
            $speed = ($e.BytesReceived - $script:lastBytes) / $sw.Elapsed.TotalSeconds
            $script:lastBytes = $e.BytesReceived
            $sw.Restart()
            $speedStr = if ($speed -gt 1MB) { "$([math]::Round($speed/1MB,1)) MB/s" }
                        elseif ($speed -gt 1KB) { "$([math]::Round($speed/1KB,0)) KB/s" }
                        else { "$([math]::Round($speed,0)) B/s" }
            $dlStr = "$([math]::Round($e.BytesReceived/1MB,1)) MB"
            Write-Progress-Bar -Percent $pct
            Write-Host "  $($C.Gray)$dlStr  $speedStr$($C.Reset)   " -NoNewline
        }
    })

    $task = $wc.DownloadFileTaskAsync($Url, $Destination)
    while (-not $task.IsCompleted) { Start-Sleep -Milliseconds 100 }

    Write-Host ""
    if ($task.IsFaulted) { throw $task.Exception.InnerException }
}

# ── Main ─────────────────────────────────────────────────────────────────────
Write-Banner
Write-Log "INFO" "ZeroMix Installer started"

# Detect installed version
$installedVer = Get-InstalledVersion
if ($installedVer) {
    Write-Status "Installed  :" "v$installedVer" $C.Yellow
} else {
    Write-Status "Installed  :" "Not found" $C.Gray
}

Write-Color "  Fetching latest release from GitHub..." $C.Gray
Write-Host ""

# Fetch release
try {
    $release    = Get-LatestRelease
    $latestTag  = $release.tag_name
    $latestVer  = $latestTag.TrimStart('v')
    $releaseUrl = $release.html_url
    Write-Log "INFO" "Latest: $latestTag"
} catch {
    Write-Log "ERROR" "Failed to fetch release: $_"
    Write-Color "  ✗ Gagal mengambil info release. Periksa koneksi internet." $C.Red
    Write-Host ""
    Write-Color "  Download manual: $RELEASE_URL" $C.Cyan
    Write-Host ""
    Read-Host "  Tekan Enter untuk keluar"
    exit 1
}

Write-Status "Latest     :" "$latestTag" $C.Green
Write-Host ""

# Check if update needed
if ($installedVer -and -not (Compare-SemVer -Latest $latestVer -Current $installedVer)) {
    Write-Divider $C.Green
    Write-Color "  ✓ Kamu sudah menggunakan versi terbaru!" $C.Green
    Write-Divider $C.Green
    Write-Host ""
    Read-Host "  Tekan Enter untuk keluar"
    exit 0
}

# Show release notes
if ($release.body) {
    Write-Divider
    Write-Color "  📋 What's New in $latestTag" $C.Cyan
    Write-Divider
    $release.body -split "`n" | Select-Object -First 10 | ForEach-Object {
        $line = $_.Trim()
        if ($line -match "^###") { Write-Color "  $line" $C.Yellow }
        elseif ($line -match "^\-\s+\*\*") { Write-Color "  $line" $C.White }
        elseif ($line -ne "") { Write-Color "  $line" $C.Gray }
    }
    Write-Divider
    Write-Host ""
}

# Find installer asset
$asset = $release.assets | Where-Object {
    $_.name -like "*Setup*" -and $_.name -like "*.exe"
} | Select-Object -First 1

if (-not $asset) {
    Write-Color "  ⚠  Installer tidak ditemukan di release ini." $C.Yellow
    Write-Color "  Buka: $releaseUrl" $C.Cyan
    Start-Process $releaseUrl
    Read-Host "  Tekan Enter untuk keluar"
    exit 0
}

$dlUrl      = $asset.browser_download_url
$fileName   = $asset.name
$fileSizeMB = [math]::Round($asset.size / 1MB, 1)
$dlPath     = Join-Path $TEMP_DIR $fileName

Write-Status "File       :" "$fileName" $C.White
Write-Status "Size       :" "$fileSizeMB MB" $C.White
Write-Host ""

# Confirm
$action = if ($installedVer) { "Update" } else { "Install" }
Write-Color "  $action ZeroMix $latestTag sekarang?" $C.White
Write-Host ""
Write-Color "  [Y] Ya, lanjutkan   [N] Tidak, batalkan" $C.Gray
Write-Host ""
$confirm = Read-Host "  Pilihan"

if ($confirm -notmatch "^[Yy]") {
    Write-Log "INFO" "User cancelled."
    Write-Host ""
    Write-Color "  Dibatalkan. Sampai jumpa! 👋" $C.Yellow
    Write-Host ""
    exit 0
}

# Download
Write-Host ""
Write-Divider
Write-Color "  ⬇  Mendownload $fileName..." $C.Cyan
Write-Divider
Write-Host ""

try {
    if (Test-Path $dlPath) { Remove-Item $dlPath -Force }
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    Invoke-Download -Url $dlUrl -Destination $dlPath -FileSize $asset.size
    $sw.Stop()
    $elapsed = [math]::Round($sw.Elapsed.TotalSeconds, 1)
    Write-Log "INFO" "Downloaded in ${elapsed}s: $dlPath"
    Write-Host ""
    Write-Color "  ✓ Download selesai! ($elapsed detik)" $C.Green
} catch {
    Write-Log "ERROR" "Download failed: $_"
    Write-Host ""
    Write-Color "  ✗ Download gagal: $_" $C.Red
    Write-Color "  Download manual: $dlUrl" $C.Cyan
    Write-Host ""
    Read-Host "  Tekan Enter untuk keluar"
    exit 1
}

# Install
Write-Host ""
Write-Divider
Write-Color "  🚀 Menjalankan installer..." $C.Cyan
Write-Divider
Write-Host ""
Write-Color "  ZeroMix akan ditutup otomatis saat proses install." $C.Gray
Write-Host ""

try {
    Start-Process -FilePath $dlPath -ArgumentList "/closeapplications /restartapplications" -Wait
    Write-Log "SUCCESS" "$action completed."
    Write-Host ""
    Write-Divider $C.Green
    Write-Color "  ✓ $action berhasil! ZeroMix $latestTag siap digunakan." $C.Green
    Write-Divider $C.Green
} catch {
    Write-Log "ERROR" "Installer failed: $_"
    Write-Color "  ✗ Installer gagal: $_" $C.Red
    Write-Color "  Jalankan manual: $dlPath" $C.Yellow
}

Write-Host ""
Read-Host "  Tekan Enter untuk keluar"
