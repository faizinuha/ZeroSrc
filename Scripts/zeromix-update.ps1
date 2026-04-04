# ZeroMix Auto Updater
# Jalankan: powershell -ExecutionPolicy Bypass -File zeromix-update.ps1

Add-Type -AssemblyName System.Windows.Forms

Write-Host ""
Write-Host " ========================================" -ForegroundColor Cyan
Write-Host " =        ZeroMix Auto Updater          =" -ForegroundColor Cyan
Write-Host " ========================================" -ForegroundColor Cyan
Write-Host ""

$repo    = "faizinuha/ZeroMix"
$apiUrl  = "https://api.github.com/repos/$repo/releases/latest"

# Step 1: Ambil info release terbaru
try {
    $release     = Invoke-RestMethod -Uri $apiUrl -UseBasicParsing
    $latestTag   = $release.tag_name
    $asset       = $release.assets | Where-Object { $_.name -like "*Setup*.exe" } | Select-Object -First 1
    $downloadUrl = $asset.browser_download_url
    $fileName    = $asset.name
} catch {
    Write-Host "[Error] Gagal mengambil info release: $_" -ForegroundColor Red
    pause
    exit
}

if (-not $downloadUrl) {
    Write-Host "[Error] Tidak ada file Setup di release $latestTag" -ForegroundColor Red
    pause
    exit
}

Write-Host "[Info] Versi terbaru: $latestTag" -ForegroundColor Green
Write-Host ""

# Step 2: Tanya user
$msg    = "Update $latestTag tersedia.`n`nApakah ingin langsung INSTALL sekarang?"
$result = [System.Windows.Forms.MessageBox]::Show(
    $msg,
    "ZeroMix Update",
    [System.Windows.Forms.MessageBoxButtons]::YesNoCancel,
    [System.Windows.Forms.MessageBoxIcon]::Question
)

if ($result -eq "Cancel") { exit }

$targetPath = if ($result -eq "Yes") {
    Join-Path $env:TEMP $fileName
} else {
    Join-Path ([Environment]::GetFolderPath("MyDocuments") | Split-Path) "Downloads\$fileName"
}

# Step 3: Cek file sudah ada
if (Test-Path $targetPath) {
    Write-Host "[Info] File sudah ada di $targetPath" -ForegroundColor Yellow
    if ($result -eq "Yes") {
        Write-Host "[Info] Menjalankan installer..." -ForegroundColor Green
        Start-Process $targetPath
    } else {
        Start-Process explorer.exe "/select,`"$targetPath`""
    }
    exit
}

# Step 4: Download
Write-Host "[Info] Mendownload $fileName..." -ForegroundColor Cyan
try {
    Invoke-WebRequest -Uri $downloadUrl -OutFile $targetPath -UseBasicParsing
    Write-Host "[Success] Download selesai!" -ForegroundColor Green

    if ($result -eq "Yes") {
        Write-Host "[Info] Memulai instalasi..." -ForegroundColor Green
        Start-Process $targetPath
    } else {
        Write-Host "[Info] File disimpan di: $targetPath" -ForegroundColor Green
        Start-Process explorer.exe "/select,`"$targetPath`""
    }
} catch {
    Write-Host "[Error] Gagal download: $_" -ForegroundColor Red
    pause
}
