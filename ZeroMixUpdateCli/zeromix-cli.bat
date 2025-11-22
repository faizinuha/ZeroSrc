@echo off
REM ZeroMix CLI Wrapper
REM Lokasi: %APPDATA%\ZeroMix\bin\zeromix-cli.bat

setlocal enabledelayedexpansion

REM Cari Node.js
where node >nul 2>nul
if %errorlevel% neq 0 (
    echo ❌ Error: Node.js tidak ditemukan di PATH
    echo 💡 Install Node.js dari: https://nodejs.org
    exit /b 1
)

REM Cari CLI script
set CLI_PATH=%~dp0zeromix-cli
if not exist "%CLI_PATH%" (
    set CLI_PATH=%APPDATA%\ZeroMix\bin\zeromix-cli
)
if not exist "%CLI_PATH%" (
    echo ❌ Error: zeromix-cli tidak ditemukan
    exit /b 1
)

REM Jalankan Node.js
node "%CLI_PATH%" %*
