$ErrorActionPreference = "Stop"

Write-Host "Starting build process..." -ForegroundColor Cyan

# Clean
if (Test-Path "dist") {
    Remove-Item -Recurse -Force dist
}

# Install deps
Write-Host "Restoring dependencies..."
dotnet restore ZeroMix.csproj

# Build app
Write-Host "Building application..."
dotnet publish ZeroMix.csproj -c Release -r win-x64 --self-contained true -o "dist/"

# Output check
if (!(Test-Path "dist/ZeroMix.exe")) {
    Write-Error "Build failed: dist folder not found"
    exit 1
}

Write-Host "Build success!" -ForegroundColor Green
