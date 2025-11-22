@echo off
REM ZeroMix Update Checker Wrapper
REM This script allows calling: zeromix cek update

setlocal enabledelayedexpansion

REM Get the directory of this script
set SCRIPT_DIR=%~dp0

REM Get ZeroMix installation directory
for /f "tokens=2*" %%a in ('reg query "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ZeroMix" /v InstallLocation 2^>nul ^| findstr InstallLocation') do set INSTALL_DIR=%%b

if not defined INSTALL_DIR (
    set INSTALL_DIR=%PROGRAMFILES%\ZeroMix
)

REM Check if CLI executable exists
if exist "%INSTALL_DIR%\bin\zeromix-update.exe" (
    "%INSTALL_DIR%\bin\zeromix-update.exe" %*
) else if exist "%SCRIPT_DIR%zeromix-update.exe" (
    "%SCRIPT_DIR%zeromix-update.exe" %*
) else (
    echo.
    echo ZeroMix Update Checker tidak ditemukan.
    echo Pastikan ZeroMix sudah diinstal dengan benar.
    echo.
    pause
    exit /b 1
)

endlocal
