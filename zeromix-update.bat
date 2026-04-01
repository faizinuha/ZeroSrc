@echo off
REM ZeroMix Auto Updater Script
REM This script downloads the latest installer and runs it silently

echo ========================================
echo ZeroMix Auto Updater
echo ========================================
echo.

set "REPO_OWNER=faizinuha"
set "REPO_NAME=ZeroMix"
set "API_URL=https://api.github.com/repos/%REPO_OWNER%/%REPO_NAME%/releases/latest"
set "DOWNLOAD_DIR=%TEMP%\ZeroMix_Update"
set "USER_AGENT=ZeroMix-Updater/1.0"

echo [1/4] Checking for updates...
echo.

REM Create download directory
if not exist "%DOWNLOAD_DIR%" mkdir "%DOWNLOAD_DIR%"

REM Get latest release info using curl (assuming it's available)
curl -s -H "User-Agent: %USER_AGENT%" "%API_URL%" > "%DOWNLOAD_DIR%\release.json"

REM Parse JSON for download URL (simple parsing)
for /f "tokens=*" %%i in ('findstr /c:"browser_download_url" "%DOWNLOAD_DIR%\release.json"') do (
    set "line=%%i"
    goto :parse_url
)

:parse_url
REM Extract URL from JSON (basic parsing)
set "download_url=%line:*"browser_download_url": "=%"
set "download_url=%download_url:"=%"
set "download_url=%download_url:",=%"

echo [2/4] Downloading latest installer...
echo URL: %download_url%
echo.

REM Download the installer
curl -L -o "%DOWNLOAD_DIR%\ZeroMix_Installer.exe" "%download_url%"

if not exist "%DOWNLOAD_DIR%\ZeroMix_Installer.exe" (
    echo ERROR: Failed to download installer!
    pause
    exit /b 1
)

echo [3/4] Download complete. File size:
dir "%DOWNLOAD_DIR%\ZeroMix_Installer.exe" | findstr "ZeroMix_Installer.exe"

echo.
echo [4/4] Installing update...
echo.

REM Run installer silently
start "" "%DOWNLOAD_DIR%\ZeroMix_Installer.exe" /S

echo Update installation started!
echo ZeroMix will restart automatically after installation.
echo.

REM Wait a bit then clean up
timeout /t 5 /nobreak > nul
rmdir /s /q "%DOWNLOAD_DIR%"

echo Cleanup complete.
pause