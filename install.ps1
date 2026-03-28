# install.ps1 untuk ZeroMix - Premium Cyber Edition v9.8
# Cara pakai: iwr -useb bit.ly/ZeroMix | iex

# Konfigurasi Repository
$repo = "faizinuha/ZeroMix"
$tagUri = "https://api.github.com/repos/$repo/releases/latest"
$listUri = "https://api.github.com/repos/$repo/releases"

$Host.UI.RawUI.WindowTitle = "ZeroMix Professional Deployment | Connecting to Aurora Cloud..."

# --- UI Helper ---
function Write-Banner {
    Write-Host "`n  ╔═══════════════════════════════════════════════════════════════════╗" -ForegroundColor Gray
    Write-Host "  ║  " -NoNewline -ForegroundColor Gray
    Write-Host "███████╗███████╗██████╗  ██████╗ ███╗   ███╗██╗██╗  ██╗" -NoNewline -ForegroundColor Magenta
    Write-Host "  ║  " -ForegroundColor Gray
    Write-Host "  ║  " -NoNewline -ForegroundColor Gray
    Write-Host "╚══███╔╝██╔════╝██╔══██╗██╔═══██╗████╗ ████║██║╚██╗██╔╝" -NoNewline -ForegroundColor Magenta
    Write-Host "  ║  " -ForegroundColor Gray
    Write-Host "  ║  " -NoNewline -ForegroundColor Gray
    Write-Host "    ███╔╝ █████╗  ██████╔╝██║   ██║██╔████╔██║██║ ╚███╔╝ " -NoNewline -ForegroundColor Cyan
    Write-Host "   ║  " -ForegroundColor Gray
    Write-Host "  ║  " -NoNewline -ForegroundColor Gray
    Write-Host "   ███╔╝  ██╔══╝  ██╔══██╗██║   ██║██║╚██╔╝██║██║ ██╔██╗ " -NoNewline -ForegroundColor Cyan
    Write-Host "   ║  " -ForegroundColor Gray
    Write-Host "  ║  " -NoNewline -ForegroundColor Gray
    Write-Host "   ███████╗███████╗██║  ██║╚██████╔╝██║ ╚═╝ ██║██║██╔╝ ██╗" -NoNewline -ForegroundColor White
    Write-Host "  ║  " -ForegroundColor Gray
    Write-Host "  ║  " -NoNewline -ForegroundColor Gray
    Write-Host "   ╚══════╝╚══════╝╚═╝  ╚═╝ ╚═════╝ ╚═╝     ╚═╝╚═╝╚═╝  ╚═╝" -NoNewline -ForegroundColor White
    Write-Host "  ║  " -ForegroundColor Gray
    Write-Host "  ╚═══════════════════════════════════════════════════════════════════╝" -ForegroundColor Gray
    Write-Host "         - [ AURORA GLASS CORE SYSTEM | BUILT BY FAIZINUHA ] -`n" -ForegroundColor DarkGray
}

function Show-Status($msg, $type = "info") {
    $color = "White"
    $icon = "·"
    switch ($type) {
        "info"    { $color = "Cyan"; $icon = "⚡" }
        "success" { $color = "Green"; $icon = "✔️" }
        "warn"    { $color = "Yellow"; $icon = "⚠️" }
        "error"   { $color = "Red"; $icon = "❌" }
        "process" { $color = "Magenta"; $icon = "🛰️" }
        "cloud"   { $color = "White"; $icon = "📦" }
    }
    Write-Host "  $icon " -NoNewline -ForegroundColor $color
    Write-Host "$msg" -ForegroundColor White
}

# --- Asset Finder Logic ---
function Find-BestAsset($releaseData) {
    if (-not $releaseData -or -not $releaseData.assets) { return $null }
    $asset = $releaseData.assets | Where-Object { $_.name -like "*-Setup-*.exe" } | Select-Object -First 1
    if (-not $asset) {
        $asset = $releaseData.assets | Where-Object { $_.name -like "*.exe" -and $_.name -notlike "*Portable*" } | Select-Object -First 1
    }
    if (-not $asset) {
        $asset = $releaseData.assets | Where-Object { $_.name -like "*Portable*.exe" -or $_.name -like "*.exe" } | Select-Object -First 1
    }
    return $asset
}

# --- Main Script ---
try {
    Clear-Host
    Write-Banner
    Show-Status "INITIALIZING SMART DEPLOYMENT SEQUENCE..." "process"
    
    # 1. Attempt to Get Latest Release
    Show-Status "Pinging Aurora Cloud for the most recent ZeroMix Core..." "cloud"
    $latest = Invoke-RestMethod -Uri $tagUri -ErrorAction SilentlyContinue
    $asset = Find-BestAsset($latest)
    $releaseInfo = $latest

    # 2. Smart Fallback: If latest is empty/fails, check the history list
    if (-not $asset) {
        Show-Status "No release assets found in 'latest'. Initiating Deep History Scan..." "warn"
        $history = Invoke-RestMethod -Uri $listUri -ErrorAction Stop
        foreach ($rel in $history) {
            $asset = Find-BestAsset($rel)
            if ($asset) {
                $releaseInfo = $rel
                Show-Status "Synchronized with repository history. Found version: $($rel.tag_name)." "success"
                break
            }
        }
    }

    if (-not $asset) { throw "CRITICAL: No valid ZeroMix binary (*.exe) detected in entire GitHub Galaxy." }

    $version = $releaseInfo.tag_name
    $downloadUrl = $asset.browser_download_url
    $fileName = $asset.name
    $tempPath = Join-Path $env:TEMP "$fileName"

    Show-Status "Targeting release $version | Asset: $fileName" "info"
    
    # DOWNLOAD START
    Show-Status "Opening secure data port. Transferring binary packets..." "process"
    $ProgressPreference = 'Continue' 
    Invoke-WebRequest -Uri $downloadUrl -OutFile $tempPath -UseBasicParsing

    Show-Status "Data transfer complete. Integrity check: PASSED." "success"
    Show-Status "Integrating ZeroMix with local Windows core..." "info"

    # Execution Mode
    if ($fileName -like "*Portable*") {
        Show-Status "Mode: Portable Execution Block." "process"
        Start-Process -FilePath $tempPath
    } else {
        Show-Status "Mode: Standard Deployment Installation." "process"
        $process = Start-Process -FilePath $tempPath -ArgumentList "/SILENT /SUPPRESSMSGBOXES /NOREBOOT" -Wait -PassThru
    }
    
    Write-Host "`n  ╔═══════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "  ║                                                                   ║" -ForegroundColor Green
    Write-Host "  ║     ZEROMIX v$($version.PadRight(10)) SUCCESSFULLY DEPLOYED!           ║" -ForegroundColor White
    Write-Host "  ║     Enjoy your enhanced desktop experience, Kak.                ║" -ForegroundColor Gray
    Write-Host "  ║                                                                   ║" -ForegroundColor Green
    Write-Host "  ╚═══════════════════════════════════════════════════════════════════╝`n" -ForegroundColor Green
}
catch {
    Show-Status "FATAL SYSTEM FAILURE: $($_.Exception.Message)" "error"
    Write-Host "  >> Check your uplink and try the emergency deployment again.`n" -ForegroundColor Gray
}
finally {
    if ($null -ne $tempPath -and (Test-Path $tempPath)) {
        try { Remove-Item $tempPath -Force -ErrorAction SilentlyContinue } catch {}
    }
}
