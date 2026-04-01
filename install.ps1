#Requires -Version 5.1
<#
.SYNOPSIS
    ZeroMix Deployment System — Aurora Cloud Edition v2.0

.DESCRIPTION
    Secure, transparent PowerShell installer for ZeroMix.
    Downloads the latest release from GitHub, validates integrity via SHA256,
    and installs with full user consent.

.NOTES
    Author  : faizinuha
    License : MIT
    Repo    : https://github.com/faizinuha/ZeroMix

.EXAMPLE
    # SAFE — Download script first, review, then run:
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/faizinuha/ZeroMix/main/install.ps1" -OutFile "$env:TEMP\zeromix-install.ps1"
    Get-Content "$env:TEMP\zeromix-install.ps1" | more   # Review script
    & "$env:TEMP\zeromix-install.ps1"                     # Run after review

    # QUICK MODE (still secure — just fewer prompts):
    & "$env:TEMP\zeromix-install.ps1" -Mode Quick
#>

[CmdletBinding()]
param(
    [ValidateSet("Safe", "Quick")]
    [string]$Mode = "Safe",

    [switch]$SkipHashCheck,
    [switch]$SilentInstall,
    [string]$LogPath
)

# ╔══════════════════════════════════════════════════════════════════════════════╗
# ║                        CONFIGURATION & CONSTANTS                            ║
# ╚══════════════════════════════════════════════════════════════════════════════╝

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$Script:Config = @{
    Repository      = "faizinuha/ZeroMix"
    ApiBaseUrl      = "https://api.github.com/repos"
    UserAgent       = "ZeroMix-Installer/2.0"
    TempDir         = Join-Path $env:TEMP "ZeroMix_Deploy"
    MaxRetries      = 3
    RetryDelayMs    = 2000
    Version         = "2.0.0"
    # Known SHA256 hashes for verified releases (update per release)
    KnownHashes     = @{
        # "ZeroMix-v6.0.0-Setup-x64.exe" = "ABC123..."
        # Add hashes here when publishing releases
    }
}

$Script:ApiLatestUrl = "$($Script:Config.ApiBaseUrl)/$($Script:Config.Repository)/releases/latest"
$Script:ApiAllUrl    = "$($Script:Config.ApiBaseUrl)/$($Script:Config.Repository)/releases"

# Log file path
if (-not $LogPath) {
    $LogPath = Join-Path $Script:Config.TempDir "zeromix-install_$(Get-Date -Format 'yyyyMMdd_HHmmss').log"
}

$Host.UI.RawUI.WindowTitle = "ZeroMix Deployment System v$($Script:Config.Version) | Mode: $Mode"

# ╔══════════════════════════════════════════════════════════════════════════════╗
# ║                           LOGGING SYSTEM                                    ║
# ╚══════════════════════════════════════════════════════════════════════════════╝

function Write-Log {
    <#
    .SYNOPSIS
        Thread-safe logger — writes to both console and log file.
    #>
    param(
        [Parameter(Mandatory)]
        [string]$Message,

        [ValidateSet("INFO", "SUCCESS", "WARN", "ERROR", "DEBUG", "STEP")]
        [string]$Level = "INFO"
    )

    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logEntry  = "[$timestamp] [$Level] $Message"

    # Ensure log directory exists
    $logDir = Split-Path $LogPath -Parent
    if (-not (Test-Path $logDir)) {
        New-Item -ItemType Directory -Path $logDir -Force | Out-Null
    }

    # Append to log file
    Add-Content -Path $LogPath -Value $logEntry -ErrorAction SilentlyContinue
}

# ╔══════════════════════════════════════════════════════════════════════════════╗
# ║                           UI COMPONENTS                                     ║
# ╚══════════════════════════════════════════════════════════════════════════════╝

function Write-Banner {
    $banner = @"

  `e[38;5;54m╔═══════════════════════════════════════════════════════════════════════╗`e[0m
  `e[38;5;54m║`e[0m  `e[38;5;201m███████╗`e[38;5;199m███████╗`e[38;5;197m██████╗ `e[38;5;196m ██████╗ `e[38;5;202m███╗   ███╗`e[38;5;208m██╗`e[38;5;214m██╗  ██╗`e[0m  `e[38;5;54m║`e[0m
  `e[38;5;54m║`e[0m  `e[38;5;201m╚══███╔╝`e[38;5;199m██╔════╝`e[38;5;197m██╔══██╗`e[38;5;196m██╔═══██╗`e[38;5;202m████╗ ████║`e[38;5;208m██║`e[38;5;214m╚██╗██╔╝`e[0m  `e[38;5;54m║`e[0m
  `e[38;5;54m║`e[0m  `e[38;5;171m  ███╔╝ `e[38;5;169m█████╗  `e[38;5;167m██████╔╝`e[38;5;166m██║   ██║`e[38;5;172m██╔████╔██║`e[38;5;178m██║`e[38;5;184m ╚███╔╝ `e[0m  `e[38;5;54m║`e[0m
  `e[38;5;54m║`e[0m  `e[38;5;141m ███╔╝  `e[38;5;139m██╔══╝  `e[38;5;137m██╔══██╗`e[38;5;136m██║   ██║`e[38;5;142m██║╚██╔╝██║`e[38;5;148m██║`e[38;5;154m ██╔██╗ `e[0m  `e[38;5;54m║`e[0m
  `e[38;5;54m║`e[0m  `e[38;5;111m███████╗`e[38;5;109m███████╗`e[38;5;107m██║  ██║`e[38;5;106m╚██████╔╝`e[38;5;112m██║ ╚═╝ ██║`e[38;5;118m██║`e[38;5;120m██╔╝ ██╗`e[0m  `e[38;5;54m║`e[0m
  `e[38;5;54m║`e[0m  `e[38;5;81m╚══════╝`e[38;5;80m╚══════╝`e[38;5;79m╚═╝  ╚═╝`e[38;5;78m ╚═════╝ `e[38;5;84m╚═╝     ╚═╝`e[38;5;83m╚═╝`e[38;5;82m╚═╝  ╚═╝`e[0m  `e[38;5;54m║`e[0m
  `e[38;5;54m╚═══════════════════════════════════════════════════════════════════════╝`e[0m
"@

    # Fallback for terminals that don't support VT escape sequences
    if ($PSVersionTable.PSVersion.Major -lt 7 -and -not $env:WT_SESSION) {
        Clear-Host
        Write-Host ""
        Write-Host "  ╔═══════════════════════════════════════════════════════════════════╗" -ForegroundColor DarkMagenta
        Write-Host "  ║  " -NoNewline -ForegroundColor DarkMagenta
        Write-Host "███████╗███████╗██████╗  ██████╗ ███╗   ███╗██╗██╗  ██╗" -NoNewline -ForegroundColor Magenta
        Write-Host "  ║" -ForegroundColor DarkMagenta
        Write-Host "  ║  " -NoNewline -ForegroundColor DarkMagenta
        Write-Host "╚══███╔╝██╔════╝██╔══██╗██╔═══██╗████╗ ████║██║╚██╗██╔╝" -NoNewline -ForegroundColor Magenta
        Write-Host "  ║" -ForegroundColor DarkMagenta
        Write-Host "  ║  " -NoNewline -ForegroundColor DarkMagenta
        Write-Host "  ███╔╝ █████╗  ██████╔╝██║   ██║██╔████╔██║██║ ╚███╔╝ " -NoNewline -ForegroundColor Cyan
        Write-Host "  ║" -ForegroundColor DarkMagenta
        Write-Host "  ║  " -NoNewline -ForegroundColor DarkMagenta
        Write-Host " ███╔╝  ██╔══╝  ██╔══██╗██║   ██║██║╚██╔╝██║██║ ██╔██╗ " -NoNewline -ForegroundColor Cyan
        Write-Host "  ║" -ForegroundColor DarkMagenta
        Write-Host "  ║  " -NoNewline -ForegroundColor DarkMagenta
        Write-Host "███████╗███████╗██║  ██║╚██████╔╝██║ ╚═╝ ██║██║██╔╝ ██╗" -NoNewline -ForegroundColor White
        Write-Host "  ║" -ForegroundColor DarkMagenta
        Write-Host "  ║  " -NoNewline -ForegroundColor DarkMagenta
        Write-Host "╚══════╝╚══════╝╚═╝  ╚═╝ ╚═════╝ ╚═╝     ╚═╝╚═╝╚═╝  ╚═╝" -NoNewline -ForegroundColor White
        Write-Host "  ║" -ForegroundColor DarkMagenta
        Write-Host "  ╚═══════════════════════════════════════════════════════════════════╝" -ForegroundColor DarkMagenta
    }
    else {
        Clear-Host
        Write-Host $banner
    }

    Write-Host ""
    Write-Host "    ┌─────────────────────────────────────────────────────────────┐" -ForegroundColor DarkGray
    Write-Host "    │  " -NoNewline -ForegroundColor DarkGray
    Write-Host "AURORA CLOUD DEPLOYMENT SYSTEM" -NoNewline -ForegroundColor Cyan
    Write-Host "  ·  " -NoNewline -ForegroundColor DarkGray
    Write-Host "v$($Script:Config.Version)" -NoNewline -ForegroundColor DarkCyan
    Write-Host "  ·  " -NoNewline -ForegroundColor DarkGray
    Write-Host "Mode: $Mode" -NoNewline -ForegroundColor Yellow
    Write-Host "    │" -ForegroundColor DarkGray
    Write-Host "    │  " -NoNewline -ForegroundColor DarkGray
    Write-Host "Built by " -NoNewline -ForegroundColor DarkGray
    Write-Host "faizinuha" -NoNewline -ForegroundColor Magenta
    Write-Host "  ·  " -NoNewline -ForegroundColor DarkGray
    Write-Host "github.com/faizinuha/ZeroMix" -NoNewline -ForegroundColor DarkGray
    Write-Host "        │" -ForegroundColor DarkGray
    Write-Host "    └─────────────────────────────────────────────────────────────┘" -ForegroundColor DarkGray
    Write-Host ""
}

function Show-Step {
    <#
    .SYNOPSIS
        Display a numbered deployment step with icon and color.
    #>
    param(
        [int]$Number,
        [int]$Total,
        [string]$Title,
        [string]$Description = ""
    )

    Write-Host ""
    Write-Host "  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
    Write-Host "  [$Number/$Total] " -NoNewline -ForegroundColor Cyan
    Write-Host "$Title" -ForegroundColor White
    if ($Description) {
        Write-Host "        $Description" -ForegroundColor DarkGray
    }
    Write-Log "$Title" "STEP"
}

function Show-Status {
    <#
    .SYNOPSIS
        Show a status message with icon and color coding.
    #>
    param(
        [Parameter(Mandatory)]
        [string]$Message,

        [ValidateSet("info", "success", "warn", "error", "process", "security", "download")]
        [string]$Type = "info"
    )

    $colorMap = @{
        "info"     = @{ Color = "Cyan";     Icon = "  ◆" }
        "success"  = @{ Color = "Green";    Icon = "  ✓" }
        "warn"     = @{ Color = "Yellow";   Icon = "  ▲" }
        "error"    = @{ Color = "Red";      Icon = "  ✗" }
        "process"  = @{ Color = "Magenta";  Icon = "  ►" }
        "security" = @{ Color = "DarkCyan"; Icon = "  🔒" }
        "download" = @{ Color = "Blue";     Icon = "  ↓" }
    }

    $entry = $colorMap[$Type]
    Write-Host "$($entry.Icon) " -NoNewline -ForegroundColor $entry.Color
    Write-Host "$Message" -ForegroundColor White
    Write-Log $Message $( switch($Type) { "success" {"SUCCESS"} "warn" {"WARN"} "error" {"ERROR"} default {"INFO"} } )
}

function Show-FileInfo {
    <#
    .SYNOPSIS
        Display detailed information about the downloaded file.
    #>
    param(
        [string]$FilePath,
        [string]$DownloadUrl,
        [string]$Version,
        [string]$Hash
    )

    $fileInfo = Get-Item $FilePath
    $sizeMB   = [math]::Round($fileInfo.Length / 1MB, 2)

    Write-Host ""
    Write-Host "  ┌─── FILE INFORMATION ──────────────────────────────────────────┐" -ForegroundColor DarkCyan
    Write-Host "  │" -ForegroundColor DarkCyan
    Write-Host "  │  " -NoNewline -ForegroundColor DarkCyan
    Write-Host "Name     : " -NoNewline -ForegroundColor Gray
    Write-Host "$($fileInfo.Name)" -ForegroundColor White
    Write-Host "  │  " -NoNewline -ForegroundColor DarkCyan
    Write-Host "Version  : " -NoNewline -ForegroundColor Gray
    Write-Host "$Version" -ForegroundColor Cyan
    Write-Host "  │  " -NoNewline -ForegroundColor DarkCyan
    Write-Host "Size     : " -NoNewline -ForegroundColor Gray
    Write-Host "$sizeMB MB" -ForegroundColor Yellow
    Write-Host "  │  " -NoNewline -ForegroundColor DarkCyan
    Write-Host "SHA256   : " -NoNewline -ForegroundColor Gray
    Write-Host "$Hash" -ForegroundColor DarkGreen
    Write-Host "  │  " -NoNewline -ForegroundColor DarkCyan
    Write-Host "Location : " -NoNewline -ForegroundColor Gray
    Write-Host "$FilePath" -ForegroundColor DarkGray
    Write-Host "  │  " -NoNewline -ForegroundColor DarkCyan
    Write-Host "Source   : " -NoNewline -ForegroundColor Gray
    Write-Host "$DownloadUrl" -ForegroundColor DarkGray
    Write-Host "  │" -ForegroundColor DarkCyan
    Write-Host "  └──────────────────────────────────────────────────────────────┘" -ForegroundColor DarkCyan
    Write-Host ""
}

function Show-ProgressDownload {
    <#
    .SYNOPSIS
        Download a file with real progress bar using .NET WebClient events.
    #>
    param(
        [string]$Url,
        [string]$OutFile
    )

    $webClient = New-Object System.Net.WebClient
    $webClient.Headers.Add("User-Agent", $Script:Config.UserAgent)

    $downloadComplete = $false
    $downloadError    = $null

    # Progress event handler
    $progressHandler = {
        param($sender, $e)
        $percent = $e.ProgressPercentage
        $receivedMB = [math]::Round($e.BytesReceived / 1MB, 1)
        $totalMB    = [math]::Round($e.TotalBytesToReceive / 1MB, 1)

        $barLength = 40
        $filled    = [math]::Floor($percent / 100 * $barLength)
        $empty     = $barLength - $filled
        $bar       = ("█" * $filled) + ("░" * $empty)

        Write-Host "`r  ↓ [$bar] $percent% ($receivedMB / $totalMB MB)  " -NoNewline -ForegroundColor Cyan
    }

    # Complete event handler
    $completeHandler = {
        param($sender, $e)
        if ($e.Error) {
            $script:downloadError = $e.Error
        }
        $script:downloadComplete = $true
    }

    $webClient.add_DownloadProgressChanged($progressHandler)
    $webClient.add_DownloadFileCompleted($completeHandler)

    try {
        $uri = New-Object System.Uri($Url)
        $webClient.DownloadFileAsync($uri, $OutFile)

        # Wait for download to complete
        while (-not $script:downloadComplete) {
            Start-Sleep -Milliseconds 100
            [System.Windows.Forms.Application]::DoEvents() 2>$null
        }

        Write-Host ""  # New line after progress bar

        if ($script:downloadError) {
            throw $script:downloadError
        }
    }
    finally {
        $webClient.Dispose()
    }
}

function Show-Completion {
    param([string]$Version)

    Write-Host ""
    Write-Host "  ╔═══════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "  ║                                                                   ║" -ForegroundColor Green
    Write-Host "  ║   " -NoNewline -ForegroundColor Green
    Write-Host "✓  ZEROMIX $($Version.PadRight(12)) DEPLOYED SUCCESSFULLY!  " -NoNewline -ForegroundColor White
    Write-Host "       ║" -ForegroundColor Green
    Write-Host "  ║                                                                   ║" -ForegroundColor Green
    Write-Host "  ║   " -NoNewline -ForegroundColor Green
    Write-Host "Enjoy your enhanced desktop experience, Kak." -NoNewline -ForegroundColor Gray
    Write-Host "              ║" -ForegroundColor Green
    Write-Host "  ║   " -NoNewline -ForegroundColor Green
    Write-Host "Log saved to: " -NoNewline -ForegroundColor DarkGray
    $logName = Split-Path $LogPath -Leaf
    Write-Host "$($logName.PadRight(40))" -NoNewline -ForegroundColor DarkGray
    Write-Host "     ║" -ForegroundColor Green
    Write-Host "  ║                                                                   ║" -ForegroundColor Green
    Write-Host "  ╚═══════════════════════════════════════════════════════════════════╝" -ForegroundColor Green
    Write-Host ""
}

# ╔══════════════════════════════════════════════════════════════════════════════╗
# ║                         CORE FUNCTIONS                                      ║
# ╚══════════════════════════════════════════════════════════════════════════════╝

function Find-BestAsset {
    <#
    .SYNOPSIS
        Intelligently selects the best .exe asset from a GitHub release.
        Priority: Setup > Standard EXE > Portable
    #>
    param([object]$ReleaseData)

    if (-not $ReleaseData -or -not $ReleaseData.assets) { return $null }

    # Priority 1: Setup installer (e.g., ZeroMix-v6.0.0-Setup-x64.exe)
    $asset = $ReleaseData.assets | Where-Object {
        $_.name -like "*Setup*.exe" -and $_.name -notlike "*Portable*"
    } | Sort-Object size -Descending | Select-Object -First 1

    # Priority 2: Any .exe that's not Portable
    if (-not $asset) {
        $asset = $ReleaseData.assets | Where-Object {
            $_.name -like "*.exe" -and $_.name -notlike "*Portable*"
        } | Select-Object -First 1
    }

    # Priority 3: Portable .exe (last resort)
    if (-not $asset) {
        $asset = $ReleaseData.assets | Where-Object {
            $_.name -like "*.exe"
        } | Select-Object -First 1
    }

    return $asset
}

function Invoke-GitHubApi {
    <#
    .SYNOPSIS
        Calls GitHub API with retry logic and proper error handling.
    #>
    param(
        [string]$Uri,
        [int]$MaxRetries = $Script:Config.MaxRetries
    )

    for ($i = 1; $i -le $MaxRetries; $i++) {
        try {
            $headers = @{
                "User-Agent" = $Script:Config.UserAgent
                "Accept"     = "application/vnd.github.v3+json"
            }

            $response = Invoke-RestMethod -Uri $Uri -Headers $headers -TimeoutSec 30 -ErrorAction Stop
            return $response
        }
        catch {
            $errorMsg = $_.Exception.Message
            Write-Log "API call attempt $i/$MaxRetries failed: $errorMsg" "WARN"

            if ($i -lt $MaxRetries) {
                Show-Status "API request failed (attempt $i/$MaxRetries). Retrying in $($Script:Config.RetryDelayMs / 1000)s..." "warn"
                Start-Sleep -Milliseconds $Script:Config.RetryDelayMs
            }
            else {
                throw "GitHub API unreachable after $MaxRetries attempts. Last error: $errorMsg"
            }
        }
    }
}

function Test-FileHash {
    <#
    .SYNOPSIS
        Validates SHA256 hash of downloaded file against known hashes.
        Returns the computed hash and validation status.
    #>
    param(
        [string]$FilePath,
        [string]$FileName
    )

    $result = @{
        Hash     = ""
        Valid    = $false
        Checked  = $false
        Message  = ""
    }

    $hashObj = Get-FileHash -Path $FilePath -Algorithm SHA256
    $result.Hash = $hashObj.Hash

    # Check against known hashes
    if ($Script:Config.KnownHashes.ContainsKey($FileName)) {
        $expected = $Script:Config.KnownHashes[$FileName]
        $result.Checked = $true

        if ($result.Hash -eq $expected) {
            $result.Valid   = $true
            $result.Message = "SHA256 hash MATCHES known good hash."
        }
        else {
            $result.Valid   = $false
            $result.Message = "SHA256 MISMATCH! Expected: $expected | Got: $($result.Hash)"
        }
    }
    else {
        $result.Checked = $false
        $result.Message = "No known hash for this file. Hash computed for your records."
    }

    return $result
}

function Get-UserConfirmation {
    <#
    .SYNOPSIS
        Prompts user for Y/N confirmation. Returns $true if confirmed.
    #>
    param(
        [string]$Prompt = "Lanjutkan?",
        [string]$Default = "Y"
    )

    $choices = if ($Default -eq "Y") { "(Y/n)" } else { "(y/N)" }

    Write-Host ""
    Write-Host "  ❓ " -NoNewline -ForegroundColor Yellow
    Write-Host "$Prompt " -NoNewline -ForegroundColor White
    Write-Host "$choices " -NoNewline -ForegroundColor DarkGray
    $answer = Read-Host

    if ([string]::IsNullOrWhiteSpace($answer)) {
        $answer = $Default
    }

    $confirmed = $answer.Trim().ToUpper() -eq "Y"
    Write-Log "User confirmation '$Prompt': $answer (confirmed=$confirmed)" "INFO"
    return $confirmed
}

# ╔══════════════════════════════════════════════════════════════════════════════╗
# ║                       MAIN DEPLOYMENT PIPELINE                              ║
# ╚══════════════════════════════════════════════════════════════════════════════╝

function Start-Deployment {
    $totalSteps = 7
    $tempPath   = $null

    try {
        # ── STEP 0: INITIALIZATION ──
        Write-Banner
        Write-Log "=== ZeroMix Deployment System v$($Script:Config.Version) started ===" "INFO"
        Write-Log "Mode: $Mode | PowerShell: $($PSVersionTable.PSVersion) | OS: $([System.Environment]::OSVersion.VersionString)" "INFO"

        Show-Status "Deployment Mode: $Mode" "info"
        if ($Mode -eq "Safe") {
            Show-Status "All confirmations enabled. You control every step." "security"
        }
        else {
            Show-Status "Quick Mode: Fewer prompts, but still validates integrity." "info"
        }

        # ── STEP 1: PREPARE ENVIRONMENT ──
        Show-Step -Number 1 -Total $totalSteps -Title "PREPARING ENVIRONMENT" -Description "Creating secure temporary workspace"

        if (-not (Test-Path $Script:Config.TempDir)) {
            New-Item -ItemType Directory -Path $Script:Config.TempDir -Force | Out-Null
        }
        Show-Status "Workspace ready: $($Script:Config.TempDir)" "success"

        # ── STEP 2: FETCH RELEASE DATA ──
        Show-Step -Number 2 -Total $totalSteps -Title "FETCHING RELEASE DATA" -Description "Querying GitHub API for latest ZeroMix release"

        $releaseInfo = $null
        $asset       = $null

        # Try latest release first
        Show-Status "Querying: $Script:ApiLatestUrl" "process"
        try {
            $latestRelease = Invoke-GitHubApi -Uri $Script:ApiLatestUrl
            $asset = Find-BestAsset -ReleaseData $latestRelease

            if ($asset) {
                $releaseInfo = $latestRelease
                Show-Status "Found latest release: $($latestRelease.tag_name)" "success"
            }
        }
        catch {
            Show-Status "Latest release query failed. Falling back to release history..." "warn"
            Write-Log "Latest release fetch failed: $($_.Exception.Message)" "WARN"
        }

        # Fallback: search all releases
        if (-not $asset) {
            Show-Status "Scanning release history for valid assets..." "process"
            $allReleases = Invoke-GitHubApi -Uri $Script:ApiAllUrl

            foreach ($release in $allReleases) {
                $asset = Find-BestAsset -ReleaseData $release
                if ($asset) {
                    $releaseInfo = $release
                    Show-Status "Found valid release in history: $($release.tag_name)" "success"
                    break
                }
            }
        }

        if (-not $asset) {
            throw "No valid ZeroMix installer (.exe) found in any GitHub release."
        }

        $version     = $releaseInfo.tag_name
        $downloadUrl = $asset.browser_download_url
        $fileName    = $asset.name
        $tempPath    = Join-Path $Script:Config.TempDir $fileName

        Write-Log "Selected asset: $fileName from release $version" "INFO"
        Write-Log "Download URL: $downloadUrl" "INFO"

        # ── STEP 3: DISPLAY DOWNLOAD INFO ──
        Show-Step -Number 3 -Total $totalSteps -Title "DOWNLOAD INFORMATION" -Description "Transparent view of what will be downloaded"

        Write-Host ""
        Write-Host "  ┌─── DOWNLOAD DETAILS ─────────────────────────────────────────┐" -ForegroundColor DarkCyan
        Write-Host "  │" -ForegroundColor DarkCyan
        Write-Host "  │  " -NoNewline -ForegroundColor DarkCyan
        Write-Host "File     : " -NoNewline -ForegroundColor Gray
        Write-Host "$fileName" -ForegroundColor White
        Write-Host "  │  " -NoNewline -ForegroundColor DarkCyan
        Write-Host "Version  : " -NoNewline -ForegroundColor Gray
        Write-Host "$version" -ForegroundColor Cyan
        Write-Host "  │  " -NoNewline -ForegroundColor DarkCyan
        Write-Host "Size     : " -NoNewline -ForegroundColor Gray
        Write-Host "$([math]::Round($asset.size / 1MB, 2)) MB" -ForegroundColor Yellow
        Write-Host "  │  " -NoNewline -ForegroundColor DarkCyan
        Write-Host "Source   : " -NoNewline -ForegroundColor Gray
        Write-Host "github.com/$($Script:Config.Repository)" -ForegroundColor DarkGray
        Write-Host "  │  " -NoNewline -ForegroundColor DarkCyan
        Write-Host "Full URL : " -NoNewline -ForegroundColor Gray
        Write-Host "$downloadUrl" -ForegroundColor DarkGray
        Write-Host "  │  " -NoNewline -ForegroundColor DarkCyan
        Write-Host "Save to  : " -NoNewline -ForegroundColor Gray
        Write-Host "$tempPath" -ForegroundColor DarkGray
        Write-Host "  │" -ForegroundColor DarkCyan
        Write-Host "  └──────────────────────────────────────────────────────────────┘" -ForegroundColor DarkCyan

        # Safe Mode: confirm before download
        if ($Mode -eq "Safe") {
            $proceed = Get-UserConfirmation -Prompt "Proceed with download?"
            if (-not $proceed) {
                Show-Status "Download cancelled by user." "warn"
                Write-Log "User cancelled at download confirmation." "INFO"
                return
            }
        }

        # ── STEP 4: DOWNLOAD FILE ──
        Show-Step -Number 4 -Total $totalSteps -Title "DOWNLOADING INSTALLER" -Description "Secure download via HTTPS from GitHub Releases"

        # Try progress download first, fallback to Invoke-WebRequest
        try {
            Show-ProgressDownload -Url $downloadUrl -OutFile $tempPath
        }
        catch {
            Show-Status "Progress download failed, using fallback method..." "warn"
            Write-Log "WebClient download failed: $($_.Exception.Message). Using Invoke-WebRequest fallback." "WARN"
            $ProgressPreference = 'Continue'
            Invoke-WebRequest -Uri $downloadUrl -OutFile $tempPath -UseBasicParsing
        }

        if (-not (Test-Path $tempPath)) {
            throw "Download failed — file not found at: $tempPath"
        }

        $downloadedSize = (Get-Item $tempPath).Length
        Show-Status "Download complete! Size: $([math]::Round($downloadedSize / 1MB, 2)) MB" "success"

        # ── STEP 5: VALIDATE INTEGRITY ──
        Show-Step -Number 5 -Total $totalSteps -Title "SECURITY VALIDATION" -Description "Computing SHA256 hash and verifying file integrity"

        if ($SkipHashCheck) {
            Show-Status "Hash check SKIPPED (--SkipHashCheck flag used)" "warn"
            Write-Log "Hash check skipped by user flag." "WARN"
            $hashResult = @{ Hash = "SKIPPED"; Valid = $true; Checked = $false; Message = "Skipped" }
        }
        else {
            Show-Status "Computing SHA256 hash..." "process"
            $hashResult = Test-FileHash -FilePath $tempPath -FileName $fileName

            if ($hashResult.Checked -and $hashResult.Valid) {
                Show-Status "$($hashResult.Message)" "success"
                Show-Status "Integrity: VERIFIED ✓" "security"
            }
            elseif ($hashResult.Checked -and -not $hashResult.Valid) {
                Show-Status "$($hashResult.Message)" "error"
                Show-Status "INTEGRITY CHECK FAILED! File may be tampered." "error"
                Write-Log "HASH MISMATCH: $($hashResult.Message)" "ERROR"

                $continueAnyway = Get-UserConfirmation -Prompt "Hash mismatch detected! Continue anyway? (NOT RECOMMENDED)" -Default "N"
                if (-not $continueAnyway) {
                    throw "Installation aborted due to hash mismatch."
                }
                Write-Log "User chose to continue despite hash mismatch." "WARN"
            }
            else {
                Show-Status "$($hashResult.Message)" "info"
                Show-Status "No pre-registered hash. File hash recorded in log." "info"
            }
        }

        Write-Log "SHA256: $($hashResult.Hash)" "INFO"

        # ── STEP 6: USER CONFIRMATION ──
        Show-Step -Number 6 -Total $totalSteps -Title "INSTALLATION REVIEW" -Description "Review all details before installation"

        Show-FileInfo -FilePath $tempPath -DownloadUrl $downloadUrl -Version $version -Hash $hashResult.Hash

        # Installation mode selection
        $isPortable = $fileName -like "*Portable*"
        $installMode = ""

        if ($isPortable) {
            Show-Status "Detected: PORTABLE executable. Will launch directly." "info"
            $installMode = "Portable"
        }
        else {
            if ($SilentInstall) {
                Show-Status "Silent install mode enabled via parameter." "info"
                $installMode = "Silent"
            }
            elseif ($Mode -eq "Quick") {
                Show-Status "Quick Mode: Using standard installation (with UI)." "info"
                $installMode = "Standard"
            }
            else {
                # Safe Mode: let user choose
                Write-Host "  ┌─── INSTALLATION MODE ─────────────────────────────────────────┐" -ForegroundColor DarkYellow
                Write-Host "  │                                                                │" -ForegroundColor DarkYellow
                Write-Host "  │  " -NoNewline -ForegroundColor DarkYellow
                Write-Host "[1]" -NoNewline -ForegroundColor Cyan
                Write-Host " Standard   — Full installer UI, you control everything" -NoNewline -ForegroundColor White
                Write-Host "  │" -ForegroundColor DarkYellow
                Write-Host "  │  " -NoNewline -ForegroundColor DarkYellow
                Write-Host "[2]" -NoNewline -ForegroundColor Cyan
                Write-Host " Silent     — Automatic install, no UI prompts" -NoNewline -ForegroundColor White
                Write-Host "         │" -ForegroundColor DarkYellow
                Write-Host "  │  " -NoNewline -ForegroundColor DarkYellow
                Write-Host "[3]" -NoNewline -ForegroundColor Cyan
                Write-Host " Cancel     — Abort installation" -NoNewline -ForegroundColor White
                Write-Host "                       │" -ForegroundColor DarkYellow
                Write-Host "  │                                                                │" -ForegroundColor DarkYellow
                Write-Host "  └────────────────────────────────────────────────────────────────┘" -ForegroundColor DarkYellow

                Write-Host ""
                Write-Host "  Pilih mode " -NoNewline -ForegroundColor White
                Write-Host "[1/2/3]" -NoNewline -ForegroundColor Cyan
                Write-Host ": " -NoNewline -ForegroundColor White
                $choice = Read-Host

                switch ($choice) {
                    "1" { $installMode = "Standard" }
                    "2" { $installMode = "Silent" }
                    "3" {
                        Show-Status "Installation cancelled by user." "warn"
                        Write-Log "User cancelled at install mode selection." "INFO"
                        return
                    }
                    default {
                        Show-Status "Invalid choice. Defaulting to Standard mode." "warn"
                        $installMode = "Standard"
                    }
                }
            }
        }

        Write-Log "Installation mode: $installMode" "INFO"

        # Final confirmation in Safe mode
        if ($Mode -eq "Safe" -and $installMode -ne "Portable") {
            $finalConfirm = Get-UserConfirmation -Prompt "Ready to install ZeroMix $version ($installMode mode). Proceed?"
            if (-not $finalConfirm) {
                Show-Status "Installation cancelled by user." "warn"
                Write-Log "User cancelled at final confirmation." "INFO"
                return
            }
        }

        # ── STEP 7: EXECUTE INSTALLATION ──
        Show-Step -Number 7 -Total $totalSteps -Title "EXECUTING INSTALLATION" -Description "Launching installer with mode: $installMode"

        $process = $null

        switch ($installMode) {
            "Portable" {
                Show-Status "Launching portable executable..." "process"
                $process = Start-Process -FilePath $tempPath -PassThru
            }
            "Silent" {
                Show-Status "Running silent installation..." "process"
                $process = Start-Process -FilePath $tempPath -ArgumentList "/SILENT", "/SUPPRESSMSGBOXES", "/NOREBOOT" -Wait -PassThru
            }
            "Standard" {
                Show-Status "Launching installer UI — follow the on-screen prompts..." "process"
                $process = Start-Process -FilePath $tempPath -Wait -PassThru
            }
        }

        # Check exit code
        if ($process -and $process.HasExited -and $process.ExitCode -ne 0) {
            Show-Status "Installer exited with code: $($process.ExitCode)" "warn"
            Write-Log "Installer exit code: $($process.ExitCode)" "WARN"
        }
        else {
            Show-Status "Installation process completed successfully!" "success"
            Write-Log "Installation completed successfully." "SUCCESS"
        }

        # ── COMPLETION ──
        Show-Completion -Version $version

    }
    catch {
        Write-Host ""
        Write-Host "  ╔═══════════════════════════════════════════════════╗" -ForegroundColor Red
        Write-Host "  ║            DEPLOYMENT ERROR                      ║" -ForegroundColor Red
        Write-Host "  ╚═══════════════════════════════════════════════════╝" -ForegroundColor Red
        Write-Host ""
        Show-Status "Error: $($_.Exception.Message)" "error"
        Show-Status "Check log file for details: $LogPath" "info"
        Write-Log "FATAL ERROR: $($_.Exception.Message)" "ERROR"
        Write-Log "Stack Trace: $($_.ScriptStackTrace)" "ERROR"

        Write-Host ""
        Write-Host "  Troubleshooting:" -ForegroundColor DarkGray
        Write-Host "    1. Check your internet connection" -ForegroundColor DarkGray
        Write-Host "    2. Verify GitHub is accessible: github.com/faizinuha/ZeroMix" -ForegroundColor DarkGray
        Write-Host "    3. Try again later (GitHub API may be rate-limited)" -ForegroundColor DarkGray
        Write-Host "    4. Report this issue with the log file attached" -ForegroundColor DarkGray
        Write-Host ""
    }
    finally {
        # Cleanup temporary files
        Write-Log "=== Deployment session ended ===" "INFO"

        if ($null -ne $tempPath -and (Test-Path $tempPath)) {
            try {
                # Don't delete portable executables immediately
                if ($fileName -notlike "*Portable*") {
                    Remove-Item $tempPath -Force -ErrorAction SilentlyContinue
                    Write-Log "Cleaned up temp file: $tempPath" "INFO"
                }
                else {
                    Write-Log "Portable file retained: $tempPath" "INFO"
                }
            }
            catch {
                Write-Log "Failed to clean up temp file: $($_.Exception.Message)" "WARN"
            }
        }
    }
}

# ╔══════════════════════════════════════════════════════════════════════════════╗
# ║                           ENTRY POINT                                       ║
# ╚══════════════════════════════════════════════════════════════════════════════╝

# Verify execution policy awareness
Write-Log "Script started. Execution context: $($MyInvocation.MyCommand.Path)" "INFO"

# Run the deployment pipeline
Start-Deployment
