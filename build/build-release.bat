@echo off
REM ZeroMix Build, Sign & Release Wrapper
REM Usage: build-release.bat [version] [options]

setlocal enabledelayedexpansion

if "%1"=="" (
    echo.
    echo ZeroMix Build, Sign ^& Release
    echo ==============================
    echo.
    echo Usage: build-release.bat [version] [options]
    echo.
    echo Examples:
    echo   build-release.bat 5.1.1                    (Full pipeline)
    echo   build-release.bat 5.1.1 -SkipSign          (Build only)
    echo   build-release.bat 5.1.1 -SkipUpload        (Build + Sign)
    echo.
    echo Options:
    echo   -SkipSign                Skip code signing
    echo   -SkipUpload              Skip GitHub upload
    echo   -CertPassword "pass"     Specify certificate password
    echo.
    exit /b 1
)

set VERSION=%1
shift

echo.
echo Starting ZeroMix Build Pipeline v%VERSION%
echo.

REM Run PowerShell script
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
    "& '.\build\build-sign-release.ps1' -Version '%VERSION%' %*"

if errorlevel 1 (
    echo.
    echo Build failed!
    exit /b 1
)

echo.
echo Build complete!
echo.
