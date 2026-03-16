<#
.SYNOPSIS
    Professional Build Script for ZeroMix (PowerShell Edition)
    Acts as a "Makefile" for systems without GNU Make.

.DESCRIPTION
    Orchestrates the build, packaging, and signing process.
    Usage: .\build.ps1 [Targets] [Options]

.PARAMETER Target
    The build target to execute. Defaults to 'All'.
    Values: Clean, Build, Installer, Sign, All

.PARAMETER Version
    The version number to build. Defaults to '2.3.7'.

.EXAMPLE
    .\build.ps1
    Builds everything with default version.

.EXAMPLE
    .\build.ps1 -Target Installer -Version 3.0.0
    Only recompiles the installer with version 3.0.0.
#>

[CmdletBinding()]
param (
    [Parameter(Position=0)]
    [ValidateSet("Clean", "Build", "Installer", "Sign", "All")]
    [string]$Target = "All",

    [Parameter(Position=1)]
    [string]$Version = "4.8.0"
)

$ErrorActionPreference = "Stop"

# --- CONFIGURATION ---
$ProjectRoot = Resolve-Path "."
$BuildDir    = Join-Path $ProjectRoot "build_output"
$PublishDir  = Join-Path $ProjectRoot "publish"
$ExeDir      = Join-Path $ProjectRoot "Exe"
$ToolsDir    = Join-Path $ExeDir "bin"
$ProjectFile = Join-Path $ProjectRoot "ZeroMix.csproj"
$SetupScript = Join-Path $ExeDir "Setup.iss"
$CertFile    = Join-Path $ExeDir "ZeroMixCert.pfx"
$CertPass    = "ZeroMixPass" # Use Env Var in production!

# Tools
$Iscc        = "${env:ProgramFiles(x86)}\Inno Setup 6\iscc.exe"
$SignTool    = Join-Path $ToolsDir "osslsigncode.exe"

# --- HELPERS ---
function Log-Info($Message) { Write-Host "[INFO] $Message" -ForegroundColor Cyan }
function Log-Success($Message) { Write-Host "[SUCCESS] $Message" -ForegroundColor Green }
function Log-Error($Message) { Write-Host "[ERROR] $Message" -ForegroundColor Red }

# --- TASKS ---

function Task-Clean {
    Log-Info "Cleaning build artifacts..."
    if (Test-Path $PublishDir) { Remove-Item $PublishDir -Recurse -Force | Out-Null }
    if (Test-Path $BuildDir) { Remove-Item $BuildDir -Recurse -Force | Out-Null }
    
    $OldInstallers = Get-ChildItem "$ExeDir\ZeroMix-Setup-*.exe" -ErrorAction SilentlyContinue
    foreach ($File in $OldInstallers) { Remove-Item $File.FullName -Force }
}

function Task-Build {
    Log-Info "Publishing Application v$Version..."
    $OutDir = Join-Path $PublishDir "win-x64"
    $Proc = Start-Process "dotnet" -ArgumentList "publish `"$ProjectFile`" -c Release -r win-x64 -p:PublishSingleFile=false -p:PublishReadyToRun=true --self-contained -o `"$OutDir`"" -NoNewWindow -PassThru -Wait
    if ($Proc.ExitCode -ne 0) { throw "Dotnet publish failed." }
}

function Task-Installer {
    Log-Info "Compiling Inno Setup Script..."
    if (-not (Test-Path $Iscc)) { throw "Inno Setup compiler (ISCC) not found at $Iscc" }
    
    # Pass version to Inno Setup
    $Proc = Start-Process $Iscc -ArgumentList "`"/DAppVersion=$Version`" `"$SetupScript`"" -NoNewWindow -PassThru -Wait
    if ($Proc.ExitCode -ne 0) { throw "Inno Setup compilation failed." }
}

function Task-Sign {
    Log-Info "Signing Installer..."
    $Unsigned = Join-Path $ExeDir "ZeroMix-Setup-v$Version.exe"
    $Signed   = Join-Path $ExeDir "ZeroMix-Setup-v$Version-signed.exe"
    
    if (-not (Test-Path $Unsigned)) { throw "Installer not found: $Unsigned" }
    
    if (-not (Test-Path $SignTool)) { throw "Signing tool not found: $SignTool" }
    
    # osslsigncode arguments
    $Args = "sign -pkcs12 `"$CertFile`" -pass `"$CertPass`" -n `"ZeroMix`" -i `"https://zeromix.pages.dev`" -t `"http://timestamp.digicert.com`" -in `"$Unsigned`" -out `"$Signed`""
    
    $Proc = Start-Process $SignTool -ArgumentList $Args -NoNewWindow -PassThru -Wait
    if ($Proc.ExitCode -ne 0) { throw "Signing failed." }
    
    # Replace unsigned with signed
    Remove-Item $Unsigned -Force
    Rename-Item $Signed (Split-Path $Unsigned -Leaf)
    Log-Success "Signature applied successfully."
}

# --- EXECUTION FLOW ---

try {
    Write-Host "=== ZeroMix Build System v$Version ===" -ForegroundColor Magenta

    switch ($Target) {
        "Clean" { Task-Clean }
        "Build" { Task-Build }
        "Installer" { Task-Installer }
        "Sign" { Task-Sign }
        "All" {
            Task-Clean
            Task-Build
            Task-Installer
            Task-Sign
        }
    }
    
    Log-Success "Build task '$Target' completed successfully!"
} catch {
    Log-Error $_
    exit 1
}
