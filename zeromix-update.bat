@echo off
setlocal

set DOWNLOAD_URL=https://github.com/faizinuha/ZeroMix/releases/latest/download/ZeroMix-Setup.exe
set OUTPUT=%TEMP%\zeromix-setup.exe

echo Downloading update...
powershell -Command "Invoke-WebRequest %DOWNLOAD_URL% -OutFile %OUTPUT%"

if exist %OUTPUT% (
    echo Update downloaded.
    start "" %OUTPUT%
) else (
    echo Failed to download update.
)

endlocal