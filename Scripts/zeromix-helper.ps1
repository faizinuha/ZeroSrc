#Requires -Version 5.1
<#
.SYNOPSIS
    ZeroMix Installer & Updater
.DESCRIPTION
    Professional installer. Parallel chunked download (5 connections).
    Safe: uses only built-in .NET HttpClient, no irm/curl.

    One-liner install:
        irm https://raw.githubusercontent.com/faizinuha/ZeroMix/main/ZeroMix.ps1 | iex

    Local run:
        powershell -ExecutionPolicy Bypass -File ZeroMix.ps1
.NOTES
    Author : ZeroMix Team
    GitHub : https://github.com/faizinuha/ZeroMix
#>

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
Add-Type -AssemblyName System.Net.Http

# ── Constants ────────────────────────────────────────────────────────────────
$REPO        = "faizinuha/ZeroMix"
$API_URL     = "https://api.github.com/repos/$REPO/releases/latest"
$RELEASE_URL = "https://github.com/$REPO/releases/latest"
$TEMP_DIR    = $env:TEMP
$LOG_DIR     = Join-Path $env:APPDATA "ZeroMix\logs"
$LOG_FILE    = Join-Path $LOG_DIR "installer.log"
$CHUNKS      = 5   # Jumlah koneksi paralel
$IS_PIPED    = -not $MyInvocation.MyCommand.Path

# ── Logging ──────────────────────────────────────────────────────────────────
function Write-Log {
    param([string]$Level, [string]$Message)
    if (-not (Test-Path $LOG_DIR)) { New-Item -ItemType Directory -Path $LOG_DIR -Force | Out-Null }
    $ts = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Add-Content -Path $LOG_FILE -Value "[$ts] [$Level] $Message" -Encoding UTF8
}

# ── UI ───────────────────────────────────────────────────────────────────────
function c { param([string]$t, [string]$fg = "White", [switch]$n)
    if ($n) { Write-Host $t -ForegroundColor $fg -NoNewline }
    else    { Write-Host $t -ForegroundColor $fg }
}

function divider { param([string]$col = "DarkGray")
    c "  $('─' * 52)" $col
}

function Write-Banner {
    Clear-Host
    Write-Host ""
    c "  ╔══════════════════════════════════════════════════╗" Cyan
    c "  ║                                                  ║" Cyan
    c "  ║    ███████╗███████╗██████╗  ██████╗              ║" Cyan
    c "  ║       ███╔╝██╔════╝██╔══██╗██╔═══██╗            ║" Cyan
    c "  ║      ███╔╝ █████╗  ██████╔╝██║   ██║            ║" Cyan
    c "  ║     ███╔╝  ██╔══╝  ██╔══██╗██║   ██║            ║" Cyan
    c "  ║    ███████╗███████╗██║  ██║╚██████╔╝            ║" Cyan
    c "  ║    ╚══════╝╚══════╝╚═╝  ╚═╝ ╚═════╝             ║" Cyan
    c "  ║                                                  ║" Cyan
    c "  ║         M I X                                    ║" Magenta
    c "  ║    Smart Desktop Suite for Windows               ║" DarkGray
    c "  ║                                                  ║" Cyan
    c "  ╚══════════════════════════════════════════════════╝" Cyan
    Write-Host ""
    if ($IS_PIPED) { c "  ⚡ Running via one-liner install" Green }
    Write-Host ""
}

function Write-ProgressBar {
    param([double]$Pct, [long]$Downloaded, [long]$Total, [double]$SpeedBps)
    $w      = 44
    $filled = [int]($Pct / 100 * $w)
    $empty  = $w - $filled
    $bar    = ('█' * $filled) + ('░' * $empty)
    $pctStr = "$([math]::Round($Pct,1))%".PadLeft(6)
    $dlStr  = if ($Downloaded -gt 1MB) { "$([math]::Round($Downloaded/1MB,1)) MB" }
              else { "$([math]::Round($Downloaded/1KB,0)) KB" }
    $totStr = if ($Total -gt 1MB) { "$([math]::Round($Total/1MB,1)) MB" } else { "$([math]::Round($Total/1KB,0)) KB" }
    $spStr  = if ($SpeedBps -gt 1MB) { "$([math]::Round($SpeedBps/1MB,1)) MB/s" }
              elseif ($SpeedBps -gt 1KB) { "$([math]::Round($SpeedBps/1KB,0)) KB/s" }
              else { "" }
    Write-Host "`r  " -NoNewline
    Write-Host $bar -ForegroundColor Green -NoNewline
    Write-Host "  $pctStr  $dlStr / $totStr  $spStr   " -ForegroundColor DarkGray -NoNewline
}

# ── Version helpers ──────────────────────────────────────────────────────────
function Compare-SemVer {
    param([string]$Latest, [string]$Current)
    try   { return ([Version]($Latest -replace '[^0-9.]','')) -gt ([Version]($Current -replace '[^0-9.]','')) }
    catch { return $Latest -ne $Current }
}

function Get-InstalledVersion {
    # 1. Registry
    # FIX #1: Null-conditional operator ?. tidak didukung di PowerShell 5.1.
    # Sebelum: if ($e?.DisplayVersion)
    # Sesudah: cek $null secara eksplisit agar kompatibel dengan PS 5.1+
    foreach ($hive in @("HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*",
                         "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*")) {
        $e = Get-ItemProperty $hive -ErrorAction SilentlyContinue |
             Where-Object { $_.DisplayName -like "*ZeroMix*" } | Select-Object -First 1
        if ($null -ne $e -and $e.DisplayVersion) { return $e.DisplayVersion }
    }
    # 2. Exe
    $exePaths = @(
        "$env:ProgramFiles\ZeroMix\ZeroMix.exe",
        "$env:LOCALAPPDATA\Programs\ZeroMix\ZeroMix.exe"
    )
    if (-not $IS_PIPED -and $MyInvocation.MyCommand.Path) {
        $exePaths += Join-Path (Split-Path $MyInvocation.MyCommand.Path) "ZeroMix.exe"
    }
    foreach ($p in $exePaths) {
        if (Test-Path $p) {
            try { return [System.Diagnostics.FileVersionInfo]::GetVersionInfo($p).ProductVersion.Split('+')[0] }
            catch {}
        }
    }
    return $null
}

# ── GitHub API ───────────────────────────────────────────────────────────────
function Get-LatestRelease {
    # FIX #2: HttpClient tidak di-dispose jika terjadi exception.
    # Sebelum: $client.Dispose() hanya dipanggil di happy path.
    # Sesudah: pakai try/finally agar selalu di-dispose meski ada error.
    $client = [System.Net.Http.HttpClient]::new()
    try {
        $client.DefaultRequestHeaders.Add("User-Agent", "ZeroMix-Installer/1.0")
        $client.Timeout = [TimeSpan]::FromSeconds(15)
        $resp = $client.GetStringAsync($API_URL).GetAwaiter().GetResult()
        return $resp | ConvertFrom-Json
    } finally {
        $client.Dispose()
    }
}

# ── Parallel Chunked Download ────────────────────────────────────────────────
function Invoke-ChunkedDownload {
    param([string]$Url, [string]$Destination, [long]$FileSize)

    $chunkSize  = [math]::Ceiling($FileSize / $CHUNKS)
    $tmpFiles   = @()
    $jobs       = @()
    $sw         = [System.Diagnostics.Stopwatch]::StartNew()

    Write-Host ""
    c "  Downloading with $CHUNKS parallel connections..." DarkGray
    Write-Host ""

    # Buat temp files
    for ($i = 0; $i -lt $CHUNKS; $i++) {
        $tmpFiles += Join-Path $TEMP_DIR "zeromix_chunk_$i.tmp"
    }

    # Cleanup sisa temp file dari run sebelumnya
    foreach ($f in $tmpFiles) {
        if (Test-Path $f) { Remove-Item $f -Force }
    }

    # Launch parallel jobs
    for ($i = 0; $i -lt $CHUNKS; $i++) {
        $start = $i * $chunkSize
        $end   = [math]::Min($start + $chunkSize - 1, $FileSize - 1)
        $tmp   = $tmpFiles[$i]

        $jobs += Start-Job -ScriptBlock {
            param($url, $dest, $start, $end)
            Add-Type -AssemblyName System.Net.Http
            $client = [System.Net.Http.HttpClient]::new()
            try {
                $client.DefaultRequestHeaders.Add("User-Agent", "ZeroMix-Installer/1.0")
                $client.DefaultRequestHeaders.Add("Range", "bytes=$start-$end")
                $resp   = $client.GetAsync($url, [System.Net.Http.HttpCompletionOption]::ResponseHeadersRead).GetAwaiter().GetResult()

                # Validasi status response — Range Request harus 206 Partial Content
                if (-not ($resp.StatusCode -eq 206 -or $resp.StatusCode -eq 200)) {
                    throw "Unexpected HTTP status: $($resp.StatusCode)"
                }

                $stream = $resp.Content.ReadAsStreamAsync().GetAwaiter().GetResult()
                $fs     = [System.IO.File]::Create($dest)
                $buf    = New-Object byte[] 65536
                $read   = 0
                $total  = 0
                while (($read = $stream.Read($buf, 0, $buf.Length)) -gt 0) {
                    $fs.Write($buf, 0, $read)
                    $total += $read
                }
                $fs.Close()
                $stream.Close()
                return $total
            } finally {
                # FIX #3 (dalam job): pastikan HttpClient di-dispose di tiap worker
                $client.Dispose()
            }
        } -ArgumentList $Url, $tmp, $start, $end
    }

    # Monitor progress — FIX #4: gunakan bytes aktual (bukan job count) untuk akurasi progress
    # Sebelum: $pct = ($done / $CHUNKS) * 100  → melompat 0/20/40/60/80/100%
    # Sesudah: $pct dihitung dari total bytes yang sudah ditulis ke temp files
    while ($jobs | Where-Object { $_.State -eq 'Running' }) {
        $bytes = [long]0
        foreach ($f in $tmpFiles) {
            if (Test-Path $f) { $bytes += (Get-Item $f).Length }
        }
        $elapsed = $sw.Elapsed.TotalSeconds
        $speed   = if ($elapsed -gt 0) { $bytes / $elapsed } else { 0 }
        $pct     = if ($FileSize -gt 0) { [math]::Min(($bytes / $FileSize) * 100, 99) } else { 0 }
        Write-ProgressBar -Pct $pct -Downloaded $bytes -Total $FileSize -SpeedBps $speed
        Start-Sleep -Milliseconds 200
    }

    # Tunggu semua selesai & cek error
    $jobs | Wait-Job | Out-Null
    $hasError = $false
    foreach ($job in $jobs) {
        if ($job.State -eq 'Failed') {
            $hasError = $true
            Write-Log "ERROR" "Chunk failed: $($job.ChildJobs[0].JobStateInfo.Reason.Message)"
        }
    }
    $jobs | Remove-Job -Force

    if ($hasError) {
        # Cleanup temp files sebelum throw agar tidak ada sisa file korup
        foreach ($f in $tmpFiles) { if (Test-Path $f) { Remove-Item $f -Force -ErrorAction SilentlyContinue } }
        throw "Satu atau lebih chunk download gagal. Lihat log untuk detail."
    }

    # FIX #5: Validasi semua chunk tersedia sebelum merge.
    # Sebelum: chunk yang hilang di-skip diam-diam → file output korup.
    # Sesudah: throw jika ada chunk yang tidak ada.
    for ($i = 0; $i -lt $CHUNKS; $i++) {
        if (-not (Test-Path $tmpFiles[$i])) {
            foreach ($f in $tmpFiles) { if (Test-Path $f) { Remove-Item $f -Force -ErrorAction SilentlyContinue } }
            throw "Chunk $i tidak ditemukan setelah download selesai."
        }
    }

    # Hitung total & tampilkan progress 100%
    $totalBytes = [long]0
    foreach ($f in $tmpFiles) { $totalBytes += (Get-Item $f).Length }
    $elapsed = $sw.Elapsed.TotalSeconds
    $speed   = if ($elapsed -gt 0) { $totalBytes / $elapsed } else { 0 }
    Write-ProgressBar -Pct 100 -Downloaded $totalBytes -Total $FileSize -SpeedBps $speed
    Write-Host ""

    # FIX #6: Merge chunk menggunakan stream copy, bukan ReadAllBytes.
    # Sebelum: [System.IO.File]::ReadAllBytes($tmp) — membaca seluruh chunk ke RAM.
    #          Untuk file 100 MB dibagi 5, tiap chunk = 20 MB → puncak RAM 20 MB extra per iterasi.
    # Sesudah: stream.CopyTo(fs) — pipeline byte langsung dari disk ke disk, tanpa buffer besar.
    c "  Merging chunks..." DarkGray
    $fs = [System.IO.File]::Create($Destination)
    try {
        foreach ($tmp in $tmpFiles) {
            $srcStream = [System.IO.File]::OpenRead($tmp)
            try {
                $srcStream.CopyTo($fs)
            } finally {
                $srcStream.Close()
            }
            Remove-Item $tmp -Force
        }
    } finally {
        $fs.Close()
    }

    $sw.Stop()
    return @{ Seconds = [math]::Round($sw.Elapsed.TotalSeconds, 1); Speed = $speed }
}

# ── Fallback: single connection ──────────────────────────────────────────────
function Invoke-SingleDownload {
    param([string]$Url, [string]$Destination, [long]$FileSize)

    $client = [System.Net.Http.HttpClient]::new()
    $client.DefaultRequestHeaders.Add("User-Agent", "ZeroMix-Installer/1.0")
    $client.Timeout = [TimeSpan]::FromMinutes(10)

    $resp   = $client.GetAsync($Url, [System.Net.Http.HttpCompletionOption]::ResponseHeadersRead).GetAwaiter().GetResult()
    $stream = $resp.Content.ReadAsStreamAsync().GetAwaiter().GetResult()
    $fs     = [System.IO.File]::Create($Destination)
    $buf    = New-Object byte[] 65536
    $read   = 0
    $total  = [long]0
    $sw     = [System.Diagnostics.Stopwatch]::StartNew()
    $lastT  = [long]0
    $lastB  = [long]0

    Write-Host ""
    try {
        while (($read = $stream.Read($buf, 0, $buf.Length)) -gt 0) {
            $fs.Write($buf, 0, $read)
            $total += $read
            if (($sw.ElapsedMilliseconds - $lastT) -gt 300) {
                $interval = $sw.ElapsedMilliseconds - $lastT
                $speed    = if ($interval -gt 0) { ($total - $lastB) / $interval * 1000 } else { 0 }
                $lastT    = $sw.ElapsedMilliseconds
                $lastB    = $total
                $pct      = if ($FileSize -gt 0) { $total / $FileSize * 100 } else { 0 }
                Write-ProgressBar -Pct $pct -Downloaded $total -Total $FileSize -SpeedBps $speed
            }
        }
    } finally {
        $fs.Close()
        $stream.Close()
        $client.Dispose()
    }

    $elapsed = $sw.Elapsed.TotalSeconds
    $finalSpeed = if ($elapsed -gt 0) { $total / $elapsed } else { 0 }
    Write-ProgressBar -Pct 100 -Downloaded $total -Total $FileSize -SpeedBps $finalSpeed
    Write-Host ""
    $sw.Stop()
    return @{ Seconds = [math]::Round($sw.Elapsed.TotalSeconds, 1); Speed = $finalSpeed }
}

# ── File Integrity Validation ─────────────────────────────────────────────────
function Test-DownloadIntegrity {
    param([string]$FilePath, [long]$ExpectedSize)
    if (-not (Test-Path $FilePath)) { throw "File tidak ditemukan setelah download: $FilePath" }
    $actualSize = (Get-Item $FilePath).Length
    if ($actualSize -ne $ExpectedSize) {
        throw "Ukuran file tidak sesuai: ekspektasi $ExpectedSize bytes, aktual $actualSize bytes"
    }
}

# ════════════════════════════════════════════════════════════════════════════
# MAIN
# ════════════════════════════════════════════════════════════════════════════
Write-Banner
Write-Log "INFO" "ZeroMix Installer started (piped=$IS_PIPED)"

$installedVer = Get-InstalledVersion
if ($installedVer) {
    c "  Installed  :  v$installedVer" Yellow
} else {
    c "  Installed  :  Not found" DarkGray
}
c "  Fetching latest release..." DarkGray
Write-Host ""

# Fetch release
try {
    $release    = Get-LatestRelease
    $latestTag  = $release.tag_name
    $latestVer  = $latestTag.TrimStart('v')
    $releaseUrl = $release.html_url
    Write-Log "INFO" "Latest: $latestTag"
} catch {
    Write-Log "ERROR" "Fetch failed: $_"
    c "  ✗ Gagal mengambil info release. Periksa koneksi." Red
    Write-Host ""
    c "  Download manual: $RELEASE_URL" Cyan
    Write-Host ""
    Read-Host "  Tekan Enter untuk keluar"
    exit 1
}

c "  Latest     :  $latestTag" Green
Write-Host ""

# Up to date?
if ($installedVer -and -not (Compare-SemVer -Latest $latestVer -Current $installedVer)) {
    divider Green
    c "  ✓ Kamu sudah menggunakan versi terbaru!" Green
    divider Green
    Write-Host ""
    Read-Host "  Tekan Enter untuk keluar"
    exit 0
}

# Release notes
if ($release.body) {
    divider
    c "  📋 What's New in $latestTag" Cyan
    divider
    $release.body -split "`n" | Select-Object -First 10 | ForEach-Object {
        $line = $_.Trim()
        if     ($line -match "^###")       { c "  $line" Yellow }
        elseif ($line -match "^\-\s+\*\*") { c "  $line" White }
        elseif ($line -ne "")              { c "  $line" DarkGray }
    }
    divider
    Write-Host ""
}

# Find asset
$asset = $release.assets | Where-Object {
    $_.name -like "*Setup*" -and $_.name -like "*.exe"
} | Select-Object -First 1

if (-not $asset) {
    c "  ⚠  Installer tidak ditemukan. Buka: $releaseUrl" Yellow
    Start-Process $releaseUrl
    Read-Host "  Tekan Enter untuk keluar"
    exit 0
}

$dlUrl      = $asset.browser_download_url
$fileName   = $asset.name
$fileSizeMB = [math]::Round($asset.size / 1MB, 1)
$dlPath     = Join-Path $TEMP_DIR $fileName

c "  File       :  $fileName" White
c "  Size       :  $fileSizeMB MB" White
c "  Connections:  $CHUNKS parallel chunks" Cyan
Write-Host ""

# Confirm
$action  = if ($installedVer) { "Update" } else { "Install" }
c "  $action ZeroMix $latestTag sekarang? [Y/N]" White
Write-Host ""
$confirm = Read-Host "  Pilihan"
if ($confirm -notmatch "^[Yy]") {
    Write-Log "INFO" "Cancelled."
    c "  Dibatalkan. Sampai jumpa! 👋" Yellow
    Write-Host ""
    exit 0
}

# Download
Write-Host ""
divider
c "  ⬇  Downloading $fileName..." Cyan
divider

try {
    if (Test-Path $dlPath) { Remove-Item $dlPath -Force }

    # Cek apakah server support Range requests
    $testClient = [System.Net.Http.HttpClient]::new()
    try {
        $testClient.DefaultRequestHeaders.Add("User-Agent", "ZeroMix-Installer/1.0")
        $testResp = $testClient.SendAsync(
            [System.Net.Http.HttpRequestMessage]::new("HEAD", $dlUrl)
        ).GetAwaiter().GetResult()
        $supportsRange = $testResp.Headers.AcceptRanges -contains "bytes"
    } finally {
        $testClient.Dispose()
    }

    if ($supportsRange -and $asset.size -gt 5MB) {
        $result = Invoke-ChunkedDownload -Url $dlUrl -Destination $dlPath -FileSize $asset.size
        c "  ✓ Download selesai! ($($result.Seconds)s, $CHUNKS connections)" Green
    } else {
        c "  Server tidak support range — single connection..." DarkGray
        $result = Invoke-SingleDownload -Url $dlUrl -Destination $dlPath -FileSize $asset.size
        c "  ✓ Download selesai! ($($result.Seconds)s)" Green
    }

    # Validasi integritas file setelah download
    Test-DownloadIntegrity -FilePath $dlPath -ExpectedSize $asset.size
    c "  ✓ Integritas file OK ($fileSizeMB MB)" Green

    Write-Log "INFO" "Downloaded: $dlPath ($($result.Seconds)s)"
} catch {
    Write-Log "ERROR" "Download failed: $_"
    c "  ✗ Download gagal: $_" Red
    c "  Download manual: $dlUrl" Cyan
    Write-Host ""
    Read-Host "  Tekan Enter untuk keluar"
    exit 1
}

# Install
Write-Host ""
divider
c "  🚀 Menjalankan installer..." Cyan
divider
Write-Host ""
c "  ZeroMix akan ditutup otomatis saat install." DarkGray
Write-Host ""

try {
    Start-Process -FilePath $dlPath -ArgumentList "/closeapplications /restartapplications" -Wait
    Write-Log "SUCCESS" "$action completed: $latestTag"
    Write-Host ""
    divider Green
    c "  ✓ $action berhasil! ZeroMix $latestTag siap digunakan." Green
    divider Green
} catch {
    Write-Log "ERROR" "Installer failed: $_"
    c "  ✗ Installer gagal: $_" Red
    c "  Jalankan manual: $dlPath" Yellow
}

Write-Host ""
Read-Host "  Tekan Enter untuk keluar"