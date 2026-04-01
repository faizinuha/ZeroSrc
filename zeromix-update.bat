@echo off
setlocal enabledelayedexpansion

:: Header
echo.
echo  ========================================
echo  =        ZeroMix Auto Updater          =
echo  ========================================
echo.

set REPO=faizinuha/ZeroMix
set API_URL=https://api.github.com/repos/%REPO%/releases/latest

:: Step 1: Check latest release via PowerShell
for /f "tokens=*" %%i in ('powershell -Command "$r = Invoke-RestMethod -Uri %API_URL%; $r.tag_name"') do set LATEST_TAG=%%i
for /f "tokens=*" %%i in ('powershell -Command "$r = Invoke-RestMethod -Uri %API_URL%; $a = $r.assets | Where-Object { $_.name -like '*Setup*.exe' } | Select-Object -First 1; $a.browser_download_url"') do set DOWNLOAD_URL=%%i
for /f "tokens=*" %%i in ('powershell -Command "$r = Invoke-RestMethod -Uri %API_URL%; $a = $r.assets | Where-Object { $_.name -like '*Setup*.exe' } | Select-Object -First 1; $a.name"') do set FILE_NAME=%%i

if "%DOWNLOAD_URL%"=="" (
    echo [Error] Tidak dapat menemukan URL download untuk %LATEST_TAG%.
    pause
    exit /b
)

echo [Info] Versi terbaru ditemukan: %LATEST_TAG%
echo.

:: Alert: Install now?
set UI_MSG="Update %LATEST_TAG% tersedia. Apakah Kak ingin langsung INSTALL?"
powershell -Command "[Reflection.Assembly]::LoadWithPartialName('System.Windows.Forms'); $res = [System.Windows.Forms.MessageBox]::Show(%UI_MSG%, 'ZeroMix Update', 'YesNoCancel', 'Question'); exit $res.value__"
set CHOICE=%ERRORLEVEL%

:: Result: 6 = Yes, 7 = No, 2 = Cancel
if %CHOICE%==2 exit /b

set DOWNLOAD_DIR=%USERPROFILE%\Downloads
if %CHOICE%==6 set TARGET_PATH=%TEMP%\%FILE_NAME%
if %CHOICE%==7 set TARGET_PATH=%DOWNLOAD_DIR%\%FILE_NAME%

:: Check if already exists in target
if exist "%TARGET_PATH%" (
    echo [Info] File %FILE_NAME% sudah ada di %TARGET_PATH%.
    if %CHOICE%==6 (
        echo [Info] Menjalankan installer yang sudah ada...
        start "" "%TARGET_PATH%"
        exit /b
    ) else (
        echo [Info] Kakak bisa membukanya di folder Downloads.
        explorer /select,"%TARGET_PATH%"
        exit /b
    )
)

:: Download
echo [Info] Mendownload %FILE_NAME% ke %TARGET_PATH%...
powershell -Command "Invoke-WebRequest -Uri '%DOWNLOAD_URL%' -OutFile '%TARGET_PATH%'"

if %ERRORLEVEL%==0 (
    echo.
    echo [Success] Download selesai!
    if %CHOICE%==6 (
        echo [Info] Memulai instalasi...
        start "" "%TARGET_PATH%"
    ) else (
        echo [Info] File disimpan di: %TARGET_PATH%
        explorer /select,"%TARGET_PATH%"
    )
) else (
    echo.
    echo [Error] Gagal mendownload update.
    pause
)

endlocal