@echo off
REM ══════════════════════════════════════════════════════════════════
REM  ZeroMix Auto Updater v2.0
REM  Dipanggil dari MainWindow > About > Check Update > Yes
REM  Menggunakan PowerShell untuk JSON parsing yang robust
REM ══════════════════════════════════════════════════════════════════

title ZeroMix Updater
color 0B

echo.
echo  ╔══════════════════════════════════════════════════════════╗
echo  ║           ZeroMix Auto Updater v2.0                     ║
echo  ╠══════════════════════════════════════════════════════════╣
echo  ║  Repository : github.com/faizinuha/ZeroMix              ║
echo  ║  Method     : GitHub Releases API (HTTPS)               ║
echo  ╚══════════════════════════════════════════════════════════╝
echo.

set "REPO=faizinuha/ZeroMix"
set "API_URL=https://api.github.com/repos/%REPO%/releases/latest"
set "DOWNLOAD_DIR=%TEMP%\ZeroMix_Update"
set "PS_SCRIPT=%DOWNLOAD_DIR%\update_worker.ps1"

REM ── Step 1: Prepare ──
echo  [1/5] Preparing workspace...

if not exist "%DOWNLOAD_DIR%" mkdir "%DOWNLOAD_DIR%"

REM ── Step 2: Fetch release info using PowerShell for proper JSON parsing ──
echo  [2/5] Fetching latest release from GitHub...
echo.

REM Create a PowerShell worker script for robust JSON parsing & download
(
echo $ErrorActionPreference = 'Stop'
echo $ProgressPreference = 'Continue'
echo.
echo try {
echo     # Fetch release data
echo     $headers = @{ 'User-Agent' = 'ZeroMix-Updater/2.0' }
echo     $release = Invoke-RestMethod -Uri '%API_URL%' -Headers $headers -TimeoutSec 30
echo.
echo     $version = $release.tag_name
echo     Write-Host "  Version found: $version" -ForegroundColor Cyan
echo.
echo     # Find best installer asset (Setup ^> Standard EXE ^> Portable)
echo     $asset = $release.assets ^| Where-Object { $_.name -like '*Setup*.exe' } ^| Select-Object -First 1
echo     if (-not $asset) {
echo         $asset = $release.assets ^| Where-Object { $_.name -like '*.exe' -and $_.name -notlike '*Portable*' } ^| Select-Object -First 1
echo     }
echo     if (-not $asset) {
echo         $asset = $release.assets ^| Where-Object { $_.name -like '*.exe' } ^| Select-Object -First 1
echo     }
echo.
echo     if (-not $asset) {
echo         Write-Host '  ERROR: No installer found in release!' -ForegroundColor Red
echo         exit 1
echo     }
echo.
echo     $downloadUrl = $asset.browser_download_url
echo     $fileName = $asset.name
echo     $outPath = Join-Path '%DOWNLOAD_DIR%' $fileName
echo.
echo     Write-Host "  File: $fileName" -ForegroundColor White
echo     Write-Host "  URL:  $downloadUrl" -ForegroundColor DarkGray
echo     Write-Host "  Size: $([math]::Round($asset.size / 1MB, 2)) MB" -ForegroundColor Yellow
echo     Write-Host ""
echo.
echo     # Download with progress
echo     Write-Host '  [3/5] Downloading installer...' -ForegroundColor Cyan
echo     Invoke-WebRequest -Uri $downloadUrl -OutFile $outPath -UseBasicParsing
echo.
echo     if (-not (Test-Path $outPath)) {
echo         Write-Host '  ERROR: Download failed!' -ForegroundColor Red
echo         exit 1
echo     }
echo.
echo     $actualSize = (Get-Item $outPath).Length
echo     Write-Host "  Download complete: $([math]::Round($actualSize / 1MB, 2)) MB" -ForegroundColor Green
echo     Write-Host ""
echo.
echo     # SHA256 hash for transparency
echo     Write-Host '  [4/5] Computing SHA256 hash...' -ForegroundColor Cyan
echo     $hash = (Get-FileHash $outPath -Algorithm SHA256).Hash
echo     Write-Host "  SHA256: $hash" -ForegroundColor DarkGreen
echo     Write-Host ""
echo.
echo     # Run installer (Inno Setup uses /SILENT, NOT /S)
echo     Write-Host '  [5/5] Launching installer...' -ForegroundColor Cyan
echo     Write-Host '  The installer window will appear shortly.' -ForegroundColor White
echo     Write-Host ""
echo.
echo     if ($fileName -like '*Portable*') {
echo         Start-Process -FilePath $outPath
echo     } else {
echo         # Standard mode - user controls the installer UI
echo         $proc = Start-Process -FilePath $outPath -Wait -PassThru
echo         if ($proc.ExitCode -eq 0) {
echo             Write-Host '  Installation completed successfully!' -ForegroundColor Green
echo         } else {
echo             Write-Host "  Installer exited with code: $($proc.ExitCode)" -ForegroundColor Yellow
echo         }
echo     }
echo.
echo     Write-Host ""
echo     Write-Host '  ╔════════════════════════════════════════════╗' -ForegroundColor Green
echo     Write-Host '  ║  ZeroMix update complete!                  ║' -ForegroundColor Green
echo     Write-Host '  ║  Please restart ZeroMix to apply changes.  ║' -ForegroundColor Green
echo     Write-Host '  ╚════════════════════════════════════════════╝' -ForegroundColor Green
echo.
echo } catch {
echo     Write-Host ""
echo     Write-Host "  ERROR: $($_.Exception.Message)" -ForegroundColor Red
echo     Write-Host "  Try downloading manually from:" -ForegroundColor Yellow
echo     Write-Host "  https://github.com/%REPO%/releases/latest" -ForegroundColor Cyan
echo     exit 1
echo }
) > "%PS_SCRIPT%"

REM Execute the PowerShell worker
powershell -NoProfile -ExecutionPolicy Bypass -File "%PS_SCRIPT%"

REM ── Cleanup ──
echo.
echo  Cleaning up temporary files...
if exist "%PS_SCRIPT%" del /Q "%PS_SCRIPT%" 2>nul

echo.
echo  Press any key to close this window...
pause >nul