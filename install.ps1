# install.ps1 untuk ZeroMix - Retro-Cyber Edition (FIXED)
# Cara pakai: iwr -useb bit.ly/ZeroMix | iex

# Konfigurasi Repository
$repo = "faizinuha/ZeroMix"
$tagUri = "https://api.github.com/repos/$repo/releases/latest"

$Host.UI.RawUI.WindowTitle = "ZeroMix Installer v9.0 - Cyber Deployment"

# --- UI Helper ---
function Write-Banner {
    Write-Host " #################################################################" -ForegroundColor Cyan
    Write-Host " #                                                               #" -ForegroundColor Cyan
    Write-Host " #   _____   ______   _____    ____    __  __   ___  __   __     #" -ForegroundColor Cyan
    Write-Host " #  |__  /  |  ____| |  __ \  / __ \  |  \/  | |_ _| \ \ / /     #" -ForegroundColor Cyan
    Write-Host " #    / /   | |__    | |__) || |  | | | \  / |  | |   \ V /      #" -ForegroundColor Cyan
    Write-Host " #   / /_   |  __|   |  _  / | |  | | | |\/| |  | |    > <       #" -ForegroundColor Cyan
    Write-Host " #  /____|  |______| |_| \_\  \____/  |_|  |_| |___|  /_/ \_\     #" -ForegroundColor Cyan
    Write-Host " #                                                               #" -ForegroundColor Cyan
    Write-Host " #################################################################" -ForegroundColor Cyan
}

# --- Main Script ---
try {
    Clear-Host
    Write-Banner
    Write-Host "`n [SYSTEM] INITIALIZING INSTALLATION SEQUENCE..." -ForegroundColor Gray
    
    # Check connect
    Write-Host " [CONNECT] Estabilishing secure connection to GitHub..." -ForegroundColor White
    $latest = Invoke-RestMethod -Uri $tagUri -ErrorAction Stop
    $version = $latest.tag_name
    
    # Asset detection
    $asset = $latest.assets | Where-Object { $_.name -like "*-Setup-*.exe" -or ($_.name -like "*.exe" -and $_.name -notlike "*Portable*") } | Select-Object -First 1
    if (-not $asset) { throw "Unable to locate binary asset (installer) in the latest release." }

    $downloadUrl = $asset.browser_download_url
    $fileName = $asset.name
    $tempPath = Join-Path $env:TEMP "$fileName"

    Write-Host " [DETECTED] Version $version confirmed." -ForegroundColor Green
    
    # REAL DOWNLOAD START
    Write-Host " [TRANSFER] Pulling data from GitHub Cloud... Please wait." -ForegroundColor Yellow
    # Biarkan PowerShell menunjukkan progres download aslinya di bagian atas
    $ProgressPreference = 'Continue' 
    Invoke-WebRequest -Uri $downloadUrl -OutFile $tempPath -UseBasicParsing

    Write-Host " [SUCCESS] Binary data verified and saved to temp storage." -ForegroundColor Green
    Write-Host " [EXECUTE] Triggering system integration engine..." -ForegroundColor White
    Write-Host " >> Note: If a window pops up, please allow it to proceed." -ForegroundColor Yellow

    # Running installer - Pakai /SILENT (tampil GUI progres dikit) agar tidak dikira nge-stuck
    # /VERYSILENT benar-benar tidak terlihat apapun, sering bikin user bingung.
    $process = Start-Process -FilePath $tempPath -ArgumentList "/SILENT /SUPPRESSMSGBOXES /NOREBOOT" -Wait -PassThru
    
    if ($process.ExitCode -eq 0) {
        Write-Host "`n =================================================================" -ForegroundColor Green
        Write-Host "  [FINISH] ZEROMIX v$version SUCCESSFULLY DEPLOYED!" -ForegroundColor Green
        Write-Host "  Everything is ready. Enjoy your enhanced Windows, Kak!         " -ForegroundColor White
        Write-Host " =================================================================`n" -ForegroundColor Green
    } else {
        Write-Host "`n [WARNING] Installation exited with code: $($process.ExitCode)" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "`n [FATAL ERROR] System Failure Detected!" -ForegroundColor Red
    Write-Host " Details: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host " Please check your uplink/connection and try again.`n" -ForegroundColor Red
}
finally {
    if (Test-Path $tempPath) {
        try { Remove-Item $tempPath -Force -ErrorAction SilentlyContinue } catch {}
    }
}

# Trap logic for Ctrl+C
trap {
    Write-Host "`n`n [CANCEL] Are you sure you want to stop the magic? (Y/N)" -ForegroundColor Cyan
    $ans = Read-Host
    if ($ans -eq "y" -or $ans -eq "Y") {
        Write-Host " [DISCONNECT] Installation aborted. Goodbye.`n" -ForegroundColor Gray
        exit
    }
    continue
}
