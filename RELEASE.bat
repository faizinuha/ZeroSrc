@echo off
REM ZeroMix Release Helper Script - Windows PowerShell Version
REM Quick reference untuk membuat release dengan GitHub Actions

setlocal enabledelayedexpansion

echo.
echo ╔════════════════════════════════════════════════════╗
echo ║          ZeroMix Release Helper v1.0              ║
echo ╚════════════════════════════════════════════════════╝
echo.

REM Check if in git repo
git rev-parse --git-dir >nul 2>&1
if errorlevel 1 (
    echo ❌ Not a git repository!
    echo Please run this script from the ZeroMix repository root
    pause
    exit /b 1
)

REM Check git status
for /f %%i in ('git status -s ^| find /c ""') do set "CHANGES=%%i"

if not "!CHANGES!"=="0" (
    echo ⚠️  Warning: Uncommitted changes found
    echo Please commit all changes before creating a release
    echo.
    git status -s
    echo.
    set /p CONTINUE="Continue anyway? (y/n): "
    if /i not "!CONTINUE!"=="y" (
        exit /b 1
    )
)

REM Get current branch
for /f %%i in ('git rev-parse --abbrev-ref HEAD') do set "BRANCH=%%i"

REM Get latest tag
for /f %%i in ('git describe --tags --abbrev=0 2^>nul') do set "LATEST_TAG=%%i"
if "!LATEST_TAG!"=="" set "LATEST_TAG=No tags yet"

echo Current branch: !BRANCH!
echo Latest tag: !LATEST_TAG!
echo.

REM Get version input
set /p VERSION="Enter version number (e.g., 1.0.0): "

REM Validate version format (simple check for X.Y.Z)
echo !VERSION! | findstr /r "^[0-9][0-9]*\.[0-9][0-9]*\.[0-9][0-9]*" >nul
if errorlevel 1 (
    echo ❌ Invalid version format!
    echo Use semver: 1.0.0, 1.0.1, 2.0.0, etc.
    pause
    exit /b 1
)

set "TAG=v!VERSION!"

REM Check if tag exists
git rev-parse "!TAG!" >nul 2>&1
if not errorlevel 1 (
    echo ❌ Tag !TAG! already exists!
    pause
    exit /b 1
)

REM Get release message
echo.
echo Enter release message (or press Enter for auto-generated^):
set /p RELEASE_MSG="> "

if "!RELEASE_MSG!"=="" (
    set "RELEASE_MSG=Release version !VERSION!"
)

echo.
echo Release Summary:
echo   Version:  !VERSION!
echo   Tag:      !TAG!
echo   Message:  !RELEASE_MSG!
echo   Branch:   !BRANCH!
echo.

set /p CONFIRM="Create release? (y/n): "
if /i not "!CONFIRM!"=="y" (
    echo Cancelled.
    exit /b 1
)

echo.
echo 🏷️  Creating tag...
git tag -a "!TAG!" -m "!RELEASE_MSG!"

if errorlevel 1 (
    echo ❌ Failed to create tag
    pause
    exit /b 1
)

echo ✓ Tag created locally
echo.

echo 📤 Pushing tag to GitHub...
git push origin "!TAG!"

if errorlevel 1 (
    echo ❌ Failed to push tag
    echo To retry, run: git push origin !TAG!
    pause
    exit /b 1
)

echo ✓ Tag pushed
echo.

echo ╔════════════════════════════════════════════════════╗
echo ║          🎉 Release created successfully!         ║
echo ╚════════════════════════════════════════════════════╝
echo.

echo What happens next:
echo 1. GitHub Actions workflows start automatically
echo 2. Project is built and compiled
echo 3. Changelog is generated from commits
echo 4. Release page is created with all details
echo 5. Download links are available
echo.

echo 🔗 Links:
echo   Build progress:
echo   https://github.com/faizinuha/ZeroMix/actions
echo.
echo   Release page:
echo   https://github.com/faizinuha/ZeroMix/releases/tag/!TAG!
echo.

echo 💡 Tips:
echo   * Make sure all commits follow Conventional Commits
echo   * Examples: 'feat(...)', 'fix(...)', 'refactor(...)'
echo   * Changelog is auto-generated from commit messages
echo   * Check GitHub Actions for real-time build progress
echo.

echo 📚 Documentation: see GITHUB_ACTIONS_GUIDE.md and CHANGELOG.md
echo.

pause
