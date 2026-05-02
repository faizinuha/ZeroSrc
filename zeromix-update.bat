@echo off
title ZeroMix Updater
echo.
echo  Membuka ZeroMix Updater...
echo.

:: Cari PowerShell
where powershell >nul 2>&1
if %errorlevel% == 0 (
    powershell.exe -NoExit -ExecutionPolicy Bypass -File "%~dp0zeromix-update.ps1"
) else (
    echo  PowerShell tidak ditemukan!
    echo  Download manual di: https://github.com/faizinuha/ZeroMix/releases
    pause
)
