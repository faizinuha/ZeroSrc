# Build ZeroMix-Updater.exe
Write-Host "Building ZeroMix-Updater..." -ForegroundColor Cyan

$updaterProject = "Tools/Updater/ZeroMix.Updater.csproj"
$outputDir = "bin/Debug/net9.0-windows/win-x64/Tools/Updater"

# Build updater
dotnet publish $updaterProject `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $outputDir

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Updater built successfully!" -ForegroundColor Green
    Write-Host "  Output: $outputDir/ZeroMix-Updater.exe" -ForegroundColor Gray
} else {
    Write-Host "✗ Updater build failed!" -ForegroundColor Red
    exit 1
}
