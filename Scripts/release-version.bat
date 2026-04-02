@echo off
REM ZeroMix Version Release - Batch Wrapper
REM Usage: release-version.bat 5.1.6

if "%~1"=="" (
    echo.
    echo Usage: release-version.bat [VERSION]
    echo Example: release-version.bat 5.1.6
    echo.
    pause
    exit /b 1
)

powershell -ExecutionPolicy Bypass -File "%~dp0release-version.ps1" -NewVersion %1 -SkipPush:$false
pause
