# Test Script untuk Verifikasi FFmpeg Integration
# Test ini akan memastikan aplikasi bisa detect FFmpeg dengan benar

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Testing FFmpeg Integration" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Test 1: Cek apakah folder FFMPEG ada
Write-Host "[1/4] Checking FFMPEG folder..." -ForegroundColor Yellow
$ffmpegFolder = "C:\ZeroMix\ZeroMix\FFMPEG"

if (Test-Path $ffmpegFolder) {
    Write-Host "✓ FFMPEG folder found: $ffmpegFolder" -ForegroundColor Green
    
    # List files
    $files = Get-ChildItem $ffmpegFolder -File
    Write-Host "  Files:" -ForegroundColor Cyan
    foreach ($file in $files) {
        $sizeMB = [math]::Round($file.Length / 1MB, 2)
        Write-Host "    - $($file.Name) ($sizeMB MB)" -ForegroundColor Gray
    }
} else {
    Write-Host "✗ FFMPEG folder not found!" -ForegroundColor Red
    exit 1
}

# Test 2: Cek apakah ffmpeg.exe bisa dijalankan
Write-Host ""
Write-Host "[2/4] Testing ffmpeg.exe..." -ForegroundColor Yellow

$ffmpegExe = Join-Path $ffmpegFolder "ffmpeg.exe"
if (Test-Path $ffmpegExe) {
    Write-Host "✓ ffmpeg.exe found" -ForegroundColor Green
    
    try {
        $version = & $ffmpegExe -version 2>&1 | Select-Object -First 1
        Write-Host "  Version: $version" -ForegroundColor Cyan
        Write-Host "✓ ffmpeg.exe is working!" -ForegroundColor Green
    } catch {
        Write-Host "✗ ffmpeg.exe error: $_" -ForegroundColor Red
    }
} else {
    Write-Host "✗ ffmpeg.exe not found!" -ForegroundColor Red
}

# Test 3: Cek kode C# FindFFmpeg()
Write-Host ""
Write-Host "[3/4] Checking C# code..." -ForegroundColor Yellow

$csFile = "C:\ZeroMix\ZeroMix\Wallpapers.xaml.cs"
if (Test-Path $csFile) {
    $content = Get-Content $csFile -Raw
    
    # Cek apakah ada fungsi FindFFmpeg
    if ($content -match "private string\? FindFFmpeg\(\)") {
        Write-Host "✓ FindFFmpeg() method found" -ForegroundColor Green
    }
    
    # Cek apakah ada path ke FFMPEG folder
    if ($content -match 'Path\.Combine\(baseDir, "FFMPEG", "ffmpeg\.exe"\)') {
        Write-Host "✓ FFMPEG path is correctly configured" -ForegroundColor Green
        Write-Host "  Path: AppDomain.CurrentDomain.BaseDirectory + FFMPEG\ffmpeg.exe" -ForegroundColor Cyan
    } elseif ($content -match "FFMPEG") {
        Write-Host "⚠ FFMPEG mentioned but path might be incorrect" -ForegroundColor Yellow
    } else {
        Write-Host "✗ FFMPEG path not found in code!" -ForegroundColor Red
    }
} else {
    Write-Host "✗ Wallpapers.xaml.cs not found!" -ForegroundColor Red
}

# Test 4: Cek Setup.iss
Write-Host ""
Write-Host "[4/4] Checking Setup.iss..." -ForegroundColor Yellow

$setupFile = "C:\ZeroMix\ZeroMix\Exe\Setup.iss"
if (Test-Path $setupFile) {
    $setupContent = Get-Content $setupFile -Raw
    
    # Cek apakah FFmpeg di-include di installer
    if ($setupContent -match 'Source:.*FFMPEG.*ffmpeg\.exe') {
        Write-Host "✓ FFmpeg is included in installer" -ForegroundColor Green
        
        # Extract lines
        $ffmpegLines = ($setupContent -split "`n") | Where-Object { $_ -match "FFMPEG" }
        Write-Host "  Installer will copy:" -ForegroundColor Cyan
        foreach ($line in $ffmpegLines) {
            if ($line -match 'Source:.*"(.+?)".*DestDir:.*"(.+?)"') {
                Write-Host "    $($Matches[1]) → $($Matches[2])" -ForegroundColor Gray
            }
        }
    } else {
        Write-Host "✗ FFmpeg not included in Setup.iss!" -ForegroundColor Red
    }
} else {
    Write-Host "✗ Setup.iss not found!" -ForegroundColor Red
}

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Test Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

$allGood = $true

# Check all critical points
$checks = @(
    @{ Name = "FFMPEG folder exists"; Pass = (Test-Path "C:\ZeroMix\ZeroMix\FFMPEG\ffmpeg.exe") },
    @{ Name = "C# code configured"; Pass = ((Get-Content $csFile -Raw) -match 'FFMPEG') },  
    @{ Name = "Installer configured"; Pass = ((Get-Content $setupFile -Raw) -match 'FFMPEG') }
)

foreach ($check in $checks) {
    if ($check.Pass) {
        Write-Host "✓ $($check.Name)" -ForegroundColor Green
    } else {
        Write-Host "✗ $($check.Name)" -ForegroundColor Red
        $allGood = $false
    }
}

Write-Host ""
if ($allGood) {
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  ✓ ALL TESTS PASSED!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "FFmpeg integration is ready! 🎉" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "1. Build the application (dotnet build or Visual Studio)" -ForegroundColor White
    Write-Host "2. Test the Wallpaper feature with a video file" -ForegroundColor White
    Write-Host "3. Create installer (compile Setup.iss with Inno Setup)" -ForegroundColor White
} else {
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "  ✗ SOME TESTS FAILED" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please fix the issues above before proceeding." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Press Enter to exit..." -ForegroundColor Gray
Read-Host
