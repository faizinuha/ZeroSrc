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
    [string]$Version = "5.0.3"
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

# Certificate password from environment variable (NEVER hardcode!)
$CertPass = $env:ZEROMIX_CERT_PASS
if (-not $CertPass) {
    Log-Info "WARNING: ZEROMIX_CERT_PASS env var not set. Code signing will be skipped."
    Log-Info "Set it with: `$env:ZEROMIX_CERT_PASS = 'YourPassword'"
}

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
    # Note: PublishSingleFile and SelfContained are controlled by ZeroMix.csproj
    # Do NOT override them here to avoid conflicts with CI builds
    $Proc = Start-Process "dotnet" -ArgumentList "publish `"$ProjectFile`" -c Release -r win-x64 -p:PublishReadyToRun=true -p:Version=$Version -o `"$OutDir`"" -NoNewWindow -PassThru -Wait
    if ($Proc.ExitCode -ne 0) { throw "Dotnet publish failed." }
    
    # Copy additional assets (using $ProjectRoot for CWD-independence)
    Log-Info "Copying additional assets..."
    
    $VaDir = Join-Path $ProjectRoot "Virtual_Assisten"
    if (Test-Path $VaDir) {
        Copy-Item $VaDir "$OutDir\Virtual_Assisten" -Recurse -Force
        Log-Info "Virtual_Assisten copied"
    }
    
    $ResDir = Join-Path $ProjectRoot "Resource"
    if (Test-Path $ResDir) {
        Copy-Item $ResDir "$OutDir\Resource" -Recurse -Force
        Log-Info "Resource folder copied"
    }
    
    $PlugDir = Join-Path $ProjectRoot "Plugins"
    if (Test-Path $PlugDir) {
        Copy-Item $PlugDir "$OutDir\Plugins" -Recurse -Force
        Log-Info "Plugins folder copied"
    }
}

function Task-Installer {
    Log-Info "Compiling Inno Setup Script..."
    if (-not (Test-Path $Iscc)) { throw "Inno Setup compiler (ISCC) not found at $Iscc" }
    
    # Pass version to Inno Setup
    $Proc = Start-Process $Iscc -ArgumentList "`"/DAppVersion=$Version`" `"$SetupScript`"" -NoNewWindow -PassThru -Wait
    if ($Proc.ExitCode -ne 0) { throw "Inno Setup compilation failed." }
}

function Task-Sign {
    Log-Info "Signing Application and Installer..."
    
    # Sign the main executable first
    $ExePath = Join-Path $PublishDir "win-x64\ZeroMix.exe"
    if (Test-Path $ExePath) {
        Log-Info "Signing main executable..."
        Sign-File $ExePath
    }
    
    # Sign the installer
    $Unsigned = Join-Path $ExeDir "ZeroMix-Setup-v$Version.exe"
    $Signed   = Join-Path $ExeDir "ZeroMix-Setup-v$Version-signed.exe"
    
    if (-not (Test-Path $Unsigned)) { 
        Log-Error "Installer not found: $Unsigned"
        return
    }
    
    Log-Info "Signing installer..."
    Sign-File $Unsigned
    
    # Rename if signed successfully
    if (Test-Path $Signed) {
        Remove-Item $Unsigned -Force
        Rename-Item $Signed (Split-Path $Unsigned -Leaf)
        Log-Success "Installer signature applied successfully."
    }
}

function Sign-File {
    param([string]$FilePath)
    
    if (-not $CertPass) {
        Log-Error "ZEROMIX_CERT_PASS not set. Skipping signature for $(Split-Path $FilePath -Leaf)."
        return
    }

    if (-not (Test-Path $SignTool)) { 
        Log-Error "Signing tool not found at $SignTool. Skipping signature for $FilePath."
        return
    }

    if (-not (Test-Path $CertFile)) {
        Log-Error "Certificate PFX file not found. Skipping signature for $FilePath."
        return
    }

    try {
        # Validasi Cert
        $CertType = [System.Security.Cryptography.X509Certificates.X509Certificate2]::GetCertContentType($CertFile)
        if ($CertType -ne "Pkcs12") {
            Log-Error "Sertifikat ($CertFile) bukan file PFX/PKCS12 yang valid (Tipe: $CertType)."
            return
        }
        
        # osslsigncode arguments
        $SignedPath = $FilePath -replace '\.exe$', '-signed.exe'
        $Args = "sign -pkcs12 `"$CertFile`" -pass `"$CertPass`" -n `"ZeroMix`" -i `"https://zeromix.pages.dev`" -t `"http://timestamp.digicert.com`" -in `"$FilePath`" -out `"$SignedPath`""
        
        $Proc = Start-Process $SignTool -ArgumentList $Args -NoNewWindow -PassThru -Wait
        if ($Proc.ExitCode -eq 0) {
            # Ganti file asli dengan yang sudah di-sign
            Remove-Item $FilePath -Force
            Rename-Item $SignedPath $FilePath
            Log-Success "Signature applied to $(Split-Path $FilePath -Leaf)"
        } else {
            Log-Error "Signing failed for $(Split-Path $FilePath -Leaf) (Exit Code: $($Proc.ExitCode))"
        }
    } catch {
        Log-Error "Failed to sign $(Split-Path $FilePath -Leaf): $($_.Exception.Message)"
    }
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
            Task-Sign       # Sign main exe BEFORE Inno Setup bundles it
            Task-Installer  # Inno Setup now bundles the signed exe
            Task-Sign       # Sign the installer itself
        }
    }
    
    Log-Success "Build task '$Target' completed successfully!"
} catch {
    Log-Error $_
    exit 1
}
