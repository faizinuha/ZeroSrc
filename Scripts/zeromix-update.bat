@echo off
setlocal ENABLEDELAYEDEXPANSION

:: ==============================
:: ZeroMix Updater Launcher
:: ==============================

set SCRIPT_NAME=zeromix-update.ps1
set LOG_DIR=%~dp0logs
set LOG_FILE=%LOG_DIR%\updater.log

:: Buat folder logs kalau belum ada
if not exist "%LOG_DIR%" mkdir "%LOG_DIR%"

echo [INFO] Starting ZeroMix Updater... >> "%LOG_FILE%"

:: Cek PowerShell tersedia
where powershell >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] PowerShell not found! >> "%LOG_FILE%"
    echo.
    echo  PowerShell tidak ditemukan di sistem ini.
    echo  Download manual: https://github.com/faizinuha/ZeroMix/releases
    echo.
    pause
    exit /b 1
)

:: Cek script ada
if not exist "%~dp0%SCRIPT_NAME%" (
    echo [ERROR] Script not found: %SCRIPT_NAME% >> "%LOG_FILE%"
    echo.
    echo  File %SCRIPT_NAME% tidak ditemukan.
    echo  Download manual: https://github.com/faizinuha/ZeroMix/releases
    echo.
    pause
    exit /b 1
)

:: Jalankan PowerShell script
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0%SCRIPT_NAME%"

if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Update failed with code %ERRORLEVEL% >> "%LOG_FILE%"
    echo.
    echo  Update gagal. Cek log di: %LOG_FILE%
    echo.
    pause
    exit /b 1
)

echo [SUCCESS] Update completed! >> "%LOG_FILE%"
exit /b 0
