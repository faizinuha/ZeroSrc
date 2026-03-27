# install.ps1 untuk ZeroMix - Retro-Cyber Edition
# Cara pakai: iwr -useb bit.ly/ZeroMix | iex

# Konfigurasi Repository
$repo = "faizinuha/ZeroMix"
$tagUri = "https://api.github.com/repos/$repo/releases/latest"

$Host.UI.RawUI.WindowTitle = "ZeroMix Installer v9.0"

# --- UI Helper Functions ---
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

function Show-RetroProgress {
    param([string]$message, [int]$duration = 10)
    Write-Host "`n >> $message" -ForegroundColor Yellow -NoNewline
    for ($i = 0; $i -lt $duration; $i++) {
        Write-Host "#" -ForegroundColor Green -NoNewline
        Start-Sleep -Milliseconds 100
    }
    Write-Host " [OK]" -ForegroundColor Green
}

# --- Main Script ---
try {
    Clear-Host
    Write-Banner
    Write-Host "`n [SYSTEM] INITIALIZING INSTALLATION SEQUENCE..." -ForegroundColor Gray
    Start-Sleep -Milliseconds 500
    
    Write-Host " [CONNECT] Estabilishing secure connection to GitHub..." -ForegroundColor White
    $latest = Invoke-RestMethod -Uri $tagUri -ErrorAction Stop
    $version = $latest.tag_name
    
    # Mencari installer valid
    $asset = $latest.assets | Where-Object { $_.name -like "*-Setup-*.exe" -or ($_.name -like "*.exe" -and $_.name -notlike "*Portable*") } | Select-Object -First 1
    
    if (-not $asset) { throw "Unable to locate binary asset in the latest release." }

    $downloadUrl = $asset.browser_download_url
    $fileName = $asset.name
    $tempPath = Join-Path $env:TEMP "$fileName"

    Write-Host " [DETECTED] Version $version confirmed." -ForegroundColor Green
    Show-RetroProgress "Downloading $fileName " 20
    
    $progressPreference = 'SilentlyContinue'
    Invoke-WebRequest -Uri $downloadUrl -OutFile $tempPath -UseBasicParsing

    Write-Host "`n [READY] Download complete." -ForegroundColor White
    Write-Host " [EXEC] Starting system integration..." -ForegroundColor White
    Write-Host " (Please allow Administrator privileges if requested)" -ForegroundColor Yellow

    # Running installer
    $process = Start-Process -FilePath $tempPath -ArgumentList "/VERYSILENT /SUPPRESSMSGBOXES /NOREBOOT" -Wait -PassThru
    
    if ($process.ExitCode -eq 0) {
        Write-Host "`n =================================================================" -ForegroundColor Green
        Write-Host "  [SUCCESS] ZEROMIX v$version HAS BEEN FULLY INTEGRATED." -ForegroundColor Green
        Write-Host "  Your system is now enhanced. Enjoy the experience, Kak!       " -ForegroundColor White
        Write-Host " =================================================================`n" -ForegroundColor Green
    } else {
        Write-Host "`n [WARNING] Installation exited with code: $($process.ExitCode)" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "`n [FATAL ERROR] System Failure Detected!" -ForegroundColor Red
    Write-Host " Details: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host " Please check your uplink and try again.`n" -ForegroundColor Red
}
finally {
    if (Test-Path $tempPath) {
        Remove-Item $tempPath -Force -ErrorAction SilentlyContinue
    }
}

# --- Interrupt Handling ---
# Trick to catch Ctrl+C: just a prompt at the end if aborted.
# Note: PSCore and WinPS handle Trap differently, keeping it simple.
trap {
    Write-Host "`n`n [CANCEL] Are you sure you want to stop the magic? (Y/N)" -ForegroundColor Cyan
    $ans = Read-Host
    if ($ans -eq "y" -or $ans -eq "Y") {
        Write-Host " Installation aborted. Cyber-Uplink Disconnected.`n" -ForegroundColor Gray
        exit
    }
    continue
}
