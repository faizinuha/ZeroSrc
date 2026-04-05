# ZeroMix Auto Updater
# Jalankan: powershell -ExecutionPolicy Bypass -File zeromix-update.ps1

Add-Type -AssemblyName System.Windows.Forms

Write-Host ""
Write-Host " ========================================" -ForegroundColor Cyan
Write-Host " =         Selamat -  Datang!!          =" -ForegroundColor Cyan
Write-Host " ========================================" -ForegroundColor Cyan
Write-Host " ========================================" -ForegroundColor Cyan
Write-Host " =        ZeroMix Auto Updater          =" -ForegroundColor Cyan
Write-Host " ========================================" -ForegroundColor Cyan
Write-Host ""

$repo   = "faizinuha/ZeroMix"
$apiUrl = "https://api.github.com/repos/$repo/releases/latest"

# Step 1: Ambil info release terbaru
try {
    $release     = Invoke-RestMethod -Uri $apiUrl -UseBasicParsing
    $latestTag   = $release.tag_name
    $asset       = $release.assets | Where-Object { $_.name -like "*Setup*.exe" } | Select-Object -First 1
    $downloadUrl = $asset.browser_download_url
    $fileName    = $asset.name
    $fileSize    = $asset.size
} catch {
    Write-Host "[Error] Gagal mengambil info release: $_" -ForegroundColor Red
    pause; exit
}

if (-not $downloadUrl) {
    Write-Host "[Error] Tidak ada file Setup di release $latestTag" -ForegroundColor Red
    pause; exit
}

Write-Host "[Info] Versi terbaru : $latestTag" -ForegroundColor Green
Write-Host "[Info] Ukuran file   : $([math]::Round($fileSize / 1MB, 2)) MB" -ForegroundColor Green
Write-Host ""

# Step 2: Tanya user
$msg    = "Update $latestTag tersedia.`n`nApakah ingin langsung INSTALL sekarang?"
$result = [System.Windows.Forms.MessageBox]::Show(
    $msg, "ZeroMix Update",
    [System.Windows.Forms.MessageBoxButtons]::YesNoCancel,
    [System.Windows.Forms.MessageBoxIcon]::Question
)
if ($result -eq "Cancel") { exit }

$targetPath = if ($result -eq "Yes") {
    Join-Path $env:TEMP $fileName
} else {
    Join-Path ([Environment]::GetFolderPath("UserProfile")) "Downloads\$fileName"
}

# Step 3: Cek file sudah ada & lengkap
if (Test-Path $targetPath) {
    if ((Get-Item $targetPath).Length -eq $fileSize) {
        Write-Host "[Info] File sudah ada dan lengkap." -ForegroundColor Yellow
        if ($result -eq "Yes") { Start-Process $targetPath }
        else { Start-Process explorer.exe "/select,`"$targetPath`"" }
        exit
    }
    Remove-Item $targetPath -Force
}

# ─────────────────────────────────────────────────────────────
# Step 4: Chunked Parallel Download (IDM-style, 8 koneksi)
# ─────────────────────────────────────────────────────────────
$CHUNKS     = 8          # jumlah koneksi paralel
$chunkSize  = [math]::Ceiling($fileSize / $CHUNKS)
$tempDir    = Join-Path $env:TEMP "zeromix_chunks"
New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

Write-Host "[Info] Mendownload $fileName dengan $CHUNKS koneksi paralel..." -ForegroundColor Cyan
Write-Host ""

# Scriptblock yang dijalankan tiap job
$chunkScript = {
    param($Url, $OutPath, $Start, $End)
    try {
        $req = [System.Net.HttpWebRequest]::Create($Url)
        $req.AddRange("bytes", $Start, $End)
        $req.Timeout          = 1800000
        $req.ReadWriteTimeout = 1800000
        $req.UserAgent        = "ZeroMix-Updater"

        $resp   = $req.GetResponse()
        $stream = $resp.GetResponseStream()
        $fs     = [System.IO.FileStream]::new($OutPath, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write)
        $buf    = New-Object byte[] 131072   # 128 KB buffer per chunk
        while ($true) {
            $r = $stream.Read($buf, 0, $buf.Length)
            if ($r -le 0) { break }
            $fs.Write($buf, 0, $r)
        }
        $fs.Flush(); $fs.Dispose()
        $stream.Dispose(); $resp.Dispose()
        return $true
    } catch {
        return $false
    }
}

# Cek apakah server support Range requests
$testReq = [System.Net.HttpWebRequest]::Create($downloadUrl)
$testReq.Method  = "HEAD"
$testReq.Timeout = 10000
$supportsRange   = $false
try {
    $testResp      = $testReq.GetResponse()
    $supportsRange = $testResp.Headers["Accept-Ranges"] -eq "bytes"
    $testResp.Dispose()
} catch {}

$jobs      = @()
$chunkFiles = @()

if ($supportsRange) {
    # Parallel chunked download
    for ($i = 0; $i -lt $CHUNKS; $i++) {
        $start     = $i * $chunkSize
        $end       = [math]::Min($start + $chunkSize - 1, $fileSize - 1)
        $chunkPath = Join-Path $tempDir "chunk_$i.tmp"
        $chunkFiles += $chunkPath
        $jobs += Start-Job -ScriptBlock $chunkScript -ArgumentList $downloadUrl, $chunkPath, $start, $end
    }
} else {
    # Fallback: single connection kalau server tidak support Range
    Write-Host "[Info] Server tidak support multi-koneksi, pakai single connection..." -ForegroundColor Yellow
    $chunkPath  = Join-Path $tempDir "chunk_0.tmp"
    $chunkFiles += $chunkPath
    $jobs += Start-Job -ScriptBlock $chunkScript -ArgumentList $downloadUrl, $chunkPath, 0, ($fileSize - 1)
}

# Progress monitor — cek ukuran chunk files secara berkala
$sw       = [System.Diagnostics.Stopwatch]::StartNew()
$lastSize = 0

while ($jobs | Where-Object { $_.State -eq "Running" }) {
    Start-Sleep -Milliseconds 400

    $totalDone = 0
    foreach ($cf in $chunkFiles) {
        if (Test-Path $cf) { $totalDone += (Get-Item $cf).Length }
    }

    $now      = $sw.ElapsedMilliseconds
    $speed    = ($totalDone - $lastSize) / [math]::Max($now / 1000, 0.001)
    # recalc speed per interval
    $speed    = ($totalDone - $lastSize) / 0.4
    $lastSize = $totalDone

    $dlMB    = [math]::Round($totalDone / 1MB, 1)
    $totalMB = [math]::Round($fileSize  / 1MB, 1)
    $speedKB = [math]::Round($speed / 1KB, 0)
    $pct     = if ($fileSize -gt 0) { [math]::Min([math]::Round(($totalDone / $fileSize) * 100), 99) } else { 0 }
    $bar     = "#" * [math]::Floor($pct / 2)
    $gap     = " " * (50 - $bar.Length)
    $eta     = if ($speed -gt 0) { [math]::Round(($fileSize - $totalDone) / $speed) } else { "?" }

    Write-Host "`r  [$bar$gap] $pct%  $dlMB/$totalMB MB  $speedKB KB/s  ETA: ${eta}s   " -NoNewline -ForegroundColor Cyan
}

# Tunggu semua job selesai
$results = $jobs | Wait-Job | Receive-Job
$jobs    | Remove-Job

# Cek ada job yang gagal
$failed = $false
foreach ($r in $results) { if ($r -eq $false) { $failed = $true } }

Write-Host "`r  [##################################################] 100%  $([math]::Round($fileSize/1MB,1))/$([math]::Round($fileSize/1MB,1)) MB  Done!          " -ForegroundColor Green
Write-Host ""

if ($failed) {
    Write-Host "[Error] Satu atau lebih chunk gagal didownload." -ForegroundColor Red
    Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    pause; exit
}

# Gabungkan semua chunk jadi satu file
Write-Host "[Info] Menggabungkan file..." -ForegroundColor Cyan
try {
    $outStream = [System.IO.FileStream]::new($targetPath, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write)
    foreach ($cf in $chunkFiles) {
        $data = [System.IO.File]::ReadAllBytes($cf)
        $outStream.Write($data, 0, $data.Length)
    }
    $outStream.Flush()
    $outStream.Dispose()
} catch {
    Write-Host "[Error] Gagal menggabungkan file: $_" -ForegroundColor Red
    Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    pause; exit
}

Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue

# Verifikasi ukuran
$actualSize = (Get-Item $targetPath).Length
if ($actualSize -ne $fileSize) {
    Write-Host "[Warning] Ukuran file tidak sesuai ($actualSize vs $fileSize), mungkin ada masalah." -ForegroundColor Yellow
}

Write-Host "[Success] Download selesai!" -ForegroundColor Green
Write-Host ""

if ($result -eq "Yes") {
    Write-Host "[Info] Menutup ZeroMix dan memulai instalasi..." -ForegroundColor Green
    # Tutup ZeroMix dulu biar installer tidak komplain
    Get-Process -Name "ZeroMix" -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 1
    # /closeapplications = auto close app, /restartapplications = buka lagi setelah install
    Start-Process $targetPath -ArgumentList "/closeapplications /restartapplications"
} else {
    Write-Host "[Info] File disimpan di: $targetPath" -ForegroundColor Green
    Start-Process explorer.exe "/select,`"$targetPath`""
}
