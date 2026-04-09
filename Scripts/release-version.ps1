# ZeroMix Version Release Script
# Otomatis update versi, commit, push, dan trigger release

param(
    [Parameter(Mandatory=$true)]
    [string]$NewVersion,
    
    [string]$CommitMessage = "",
    [switch]$SkipPush = $false,
    [switch]$ForceBuild = $false,
    [switch]$Force = $false   # Skip prompt kalau versi sama, tanpa ForceBuild
)

$ErrorActionPreference = "Stop"

# ============================================================================
# VALIDASI VERSION FORMAT
# ============================================================================

if ($NewVersion -notmatch '^\d+\.\d+\.\d+$') {
    Write-Host "❌ Format versi salah! Gunakan format: X.Y.Z (contoh: 5.2.3)" -ForegroundColor Red
    Write-Host "Contoh: .\release-version.ps1 -NewVersion 5.2.3" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║          ZeroMix Version Release - v$NewVersion" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# ============================================================================
# STEP 1: UPDATE VERSION IN FILES
# ============================================================================

Write-Host "📝 Step 1: Updating version in files..." -ForegroundColor Yellow

if ($ForceBuild) {
    Write-Host "  🔨 ForceBuild mode — skipping version file update" -ForegroundColor Cyan
}

$filesToUpdate = @(
    @{
        Path = "src/MainWindow.xaml.cs"
        Pattern = 'private const string CURRENT_VERSION = "[\d\.]+";'
        Replacement = "private const string CURRENT_VERSION = `"$NewVersion`";"
    },
    @{
        Path = "Exe/Setup.iss"
        Pattern = '#define AppVersion "[\d\.]+"'
        Replacement = "#define AppVersion `"$NewVersion`""
    },
    @{
        Path = "ZeroMix.csproj"
        Pattern = '<Version>[\d\.]+</Version>'
        Replacement = "<Version>$NewVersion</Version>"
    }
)

$updatedFiles = @()

foreach ($file in $filesToUpdate) {
    if ($ForceBuild) { break } # Skip version update on force build
    if (Test-Path $file.Path) {
        $content = Get-Content $file.Path -Raw
        $newContent = $content -replace $file.Pattern, $file.Replacement
        
        if ($content -ne $newContent) {
            Set-Content -Path $file.Path -Value $newContent -NoNewline
            Write-Host "  ✅ Updated: $($file.Path)" -ForegroundColor Green
            $updatedFiles += $file.Path
        } else {
            Write-Host "  ⚠️  No change needed: $($file.Path)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "  ❌ File not found: $($file.Path)" -ForegroundColor Red
    }
}

if ($updatedFiles.Count -eq 0) {
    Write-Host ""
    Write-Host "⚠️  No files were updated. Version might already be $NewVersion" -ForegroundColor Yellow
    if (-not $ForceBuild -and -not $Force) {
        $continue = Read-Host "Continue anyway? (y/n)"
        if ($continue -ne "y") {
            exit 0
        }
    } else {
        Write-Host "  🔨 Continuing..." -ForegroundColor Cyan
    }
}

# ============================================================================
# STEP 2: GIT STATUS CHECK
# ============================================================================

Write-Host ""
Write-Host "📊 Step 2: Checking git status..." -ForegroundColor Yellow

$gitStatus = git status --porcelain
if ($gitStatus) {
    Write-Host ""
    Write-Host "Modified files:" -ForegroundColor Cyan
    git status --short
} else {
    Write-Host "  ℹ️  No changes detected" -ForegroundColor Gray
}

# ============================================================================
# STEP 3: UPDATE CHANGELOG (smart auto-detect dari commit + changed files)
# ============================================================================

Write-Host ""
Write-Host "📝 Step 3: Auto-generating CHANGELOG from commits & changed files..." -ForegroundColor Yellow

# ── Mapping: folder/file → kategori & label ──────────────────────────────
function Get-FileCategory {
    param([string]$FilePath)

    $f = $FilePath.ToLower() -replace '\\', '/'

    # Skip file yang tidak perlu masuk changelog
    if ($f -match '(changelog|\.md$|\.gitignore|\.gitattributes|obj/|bin/|\.cache$|\.user$)') {
        return $null
    }

    # Features — UI, fitur baru, plugin, assistant
    if ($f -match '^src/mainwindow\.xaml$')           { return @{ Cat = "feat"; Label = "Updated main UI layout" } }
    if ($f -match '^src/mainwindow\.xaml\.cs$')        { return @{ Cat = "feat"; Label = "Updated main window logic" } }
    if ($f -match '^src/')                             { return @{ Cat = "feat"; Label = "Updated source: $FilePath" } }
    if ($f -match '^plugins/')                         { return @{ Cat = "feat"; Label = "Updated plugin: $FilePath" } }
    if ($f -match '^virtual_assisten/')                { return @{ Cat = "feat"; Label = "Updated Virtual Assistant" } }
    if ($f -match '^wallpapers/')                      { return @{ Cat = "feat"; Label = "Updated Wallpapers module" } }
    if ($f -match '^widgets/')                         { return @{ Cat = "feat"; Label = "Updated Widgets" } }
    if ($f -match '^studio/')                          { return @{ Cat = "feat"; Label = "Updated Studio module" } }
    if ($f -match '^onboarding/')                      { return @{ Cat = "feat"; Label = "Updated Onboarding flow" } }
    if ($f -match '^search/')                          { return @{ Cat = "feat"; Label = "Updated Search module" } }
    if ($f -match '^assets/')                          { return @{ Cat = "feat"; Label = "Updated assets/resources" } }

    # Bug Fixes — recorder, hotkeys, sleep, transparan
    if ($f -match '^recorder/')                        { return @{ Cat = "fix"; Label = "Fixed Recorder module" } }
    if ($f -match '^hotkeys/')                         { return @{ Cat = "fix"; Label = "Fixed Hotkeys" } }
    if ($f -match '^sleepmode/')                       { return @{ Cat = "fix"; Label = "Fixed Sleep Mode" } }
    if ($f -match '^transparan/')                      { return @{ Cat = "fix"; Label = "Fixed Transparent Taskbar" } }
    if ($f -match '^tools/')                           { return @{ Cat = "fix"; Label = "Fixed Tools: $FilePath" } }

    # Changes — scripts, build, config, exe
    if ($f -match '^scripts/')                         { return @{ Cat = "chore"; Label = "Updated script: $FilePath" } }
    if ($f -match '^\.github/')                        { return @{ Cat = "chore"; Label = "Updated CI/CD workflow" } }
    if ($f -match '\.csproj$|\.sln$')                 { return @{ Cat = "chore"; Label = "Updated project config" } }
    if ($f -match '^exe/')                             { return @{ Cat = "chore"; Label = "Updated installer config" } }
    if ($f -match 'config\.json$|appsettings')        { return @{ Cat = "chore"; Label = "Updated app config" } }

    return $null
}

$changelogPath = "Docs/CHANGELOG.md"
if (Test-Path $changelogPath) {
    try {
        $changelog = Get-Content $changelogPath -Raw
        $date = Get-Date -Format "yyyy-MM-dd"

        # Ambil tag sebelumnya — exclude tag HEAD saat ini
        $prevTag = git describe --tags --abbrev=0 HEAD^ 2>$null
        if ([string]::IsNullOrEmpty($prevTag)) {
            $prevTag = git tag --sort=-version:refname 2>$null | Where-Object { $_ -ne "v$NewVersion" } | Select-Object -First 1
        }
        $commitRange = if ([string]::IsNullOrEmpty($prevTag)) { "HEAD" } else { "$prevTag..HEAD" }
        Write-Host "  📌 Commit range: $commitRange" -ForegroundColor Gray

        # ── Kumpulkan dari commit messages ────────────────────────────────
        $commits = git log $commitRange --pretty=format:"%s" 2>$null | Where-Object { $_ -ne "" }

        $features = @()
        $fixes    = @()
        $changes  = @()

        if ($ForceBuild) {
            # ForceBuild → parse -CommitMessage parameter langsung (bukan git log)
            # karena commit belum dibuat saat step ini jalan
            if (-not [string]::IsNullOrWhiteSpace($CommitMessage)) {
                $msgs = $CommitMessage -split '\|' | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne "" }
                foreach ($msg in $msgs) {
                    $clean = $msg -replace '^(feat|fix|chore|refactor|perf|style|docs|test|ci|build)\s*(\([^)]*\))?\s*:?\s*', ''
                    if ($clean.Length -gt 0) { $clean = $clean.Substring(0,1).ToUpper() + $clean.Substring(1) }
                    if ([string]::IsNullOrWhiteSpace($clean)) { continue }
                    if ($msg -match '^feat')    { $features += "- $clean" }
                    elseif ($msg -match '^fix') { $fixes    += "- $clean" }
                }
            }

            # Juga baca git log commits yang sudah ada sebelumnya
            foreach ($msg in $commits) {
                if ($msg -match '^Merge|^bump version|^v\d|^Docs/CHANGELOG') { continue }
                $clean = $msg -replace '^(feat|fix|chore|refactor|perf|style|docs|test|ci|build)\s*(\([^)]*\))?\s*:?\s*', ''
                if ($clean.Length -gt 0) { $clean = $clean.Substring(0,1).ToUpper() + $clean.Substring(1) }
                if ([string]::IsNullOrWhiteSpace($clean)) { continue }
                if ($msg -match '^feat')    { $features += "- $clean" }
                elseif ($msg -match '^fix') { $fixes    += "- $clean" }
            }
            # File yang berubah → semua masuk Changes
            $changedFiles = git diff --name-only $commitRange 2>$null
            if (-not $changedFiles) { $changedFiles = git diff --name-only HEAD 2>$null }

            foreach ($file in $changedFiles) {
                $f = $file.ToLower() -replace '\\', '/'
                if ($f -match '(changelog|\.md$|\.gitignore|obj/|bin/|\.cache$)') { continue }
                $label = "- Rebuilt: $file"
                if (-not ($changes -contains $label)) { $changes += $label }
            }

            if ($features.Count -eq 0) { $features = @("- No new features") }
            if ($fixes.Count -eq 0)    { $fixes    = @("- No bug fixes") }
            if ($changes.Count -eq 0)  { $changes  = @("- Force rebuild v$NewVersion") }

        } else {
            # Normal build → kategorikan dari commit message + file detect
            foreach ($msg in $commits) {
                if ($msg -match '^Merge|^bump version|^v\d|^Docs/CHANGELOG') { continue }

                $clean = $msg -replace '^(feat|fix|chore|refactor|perf|style|docs|test|ci|build)\s*(\([^)]*\))?\s*:?\s*', ''
                if ($clean.Length -gt 0) { $clean = $clean.Substring(0,1).ToUpper() + $clean.Substring(1) }
                if ([string]::IsNullOrWhiteSpace($clean)) { continue }

                if ($msg -match '^feat')                                               { $features += "- $clean" }
                elseif ($msg -match '^fix')                                            { $fixes    += "- $clean" }
                elseif ($msg -match '^(chore|refactor|perf|style|docs|test|ci|build)') { $changes  += "- $clean" }
                else { $features += "- $clean" }  # tanpa prefix → feat
            }

            # Auto-detect dari file yang berubah sebagai pelengkap
            $changedFiles = git diff --name-only $commitRange 2>$null
            if (-not $changedFiles) { $changedFiles = git diff --name-only HEAD 2>$null }

            $detectedLabels = @{ feat = @(); fix = @(); chore = @() }
            foreach ($file in $changedFiles) {
                $cat = Get-FileCategory -FilePath $file
                if ($cat -ne $null) {
                    $label = "- $($cat.Label)"
                    if (-not $detectedLabels[$cat.Cat].Contains($label)) {
                        $detectedLabels[$cat.Cat] += $label
                    }
                }
            }

            # Merge — commit message prioritas, file-detect sebagai tambahan
            foreach ($l in $detectedLabels["feat"])  { if (-not ($features -contains $l)) { $features += $l } }
            foreach ($l in $detectedLabels["fix"])   { if (-not ($fixes    -contains $l)) { $fixes    += $l } }
            foreach ($l in $detectedLabels["chore"]) { if (-not ($changes  -contains $l)) { $changes  += $l } }

            # Fallback
            if ($features.Count -eq 0) { $features = @("- No new features") }
            if ($fixes.Count -eq 0)    { $fixes    = @("- No bug fixes") }
            if ($changes.Count -eq 0)  { $changes  = @("- Version bump to $NewVersion") }
        }

        $newEntry = @"
## [v$NewVersion] - $date

### ✨ Features
$($features -join "`n")

### 🐛 Bug Fixes
$($fixes -join "`n")

### 🔧 Changes
$($changes -join "`n")

---

"@

        if ($changelog -match '(?s)(# ZeroMix - Changelog.*?---\s*)(.*)') {
            $header = $matches[1]
            $rest   = $matches[2]

            # Cek apakah versi ini sudah ada → skip, jangan timpa manual entry
            $existingPattern = "(?s)## \[v$([regex]::Escape($NewVersion))\].*?(?=\n## \[|\z)"
            if ($changelog -match $existingPattern) {
                Write-Host "  ℹ️  Entry v$NewVersion sudah ada di CHANGELOG — tidak ditimpa (manual entry preserved)" -ForegroundColor Cyan
            } else {
                # Insert baru di atas
                Set-Content -Path $changelogPath -Value ($header + "`n" + $newEntry + $rest) -NoNewline
                Write-Host "  ✅ CHANGELOG generated (new entry v$NewVersion)" -ForegroundColor Green
                Write-Host "  📋 Features: $($features.Count) | Fixes: $($fixes.Count) | Changes: $($changes.Count)" -ForegroundColor Cyan
                Write-Host "  📁 Files scanned: $($changedFiles.Count) changed files" -ForegroundColor Cyan
                $updatedFiles += $changelogPath
            }
        } else {
            Write-Host "  ⚠️  Could not parse CHANGELOG format" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "  ⚠️  CHANGELOG update failed: $($_.Exception.Message)" -ForegroundColor Yellow
    }
} else {
    Write-Host "  ⚠️  CHANGELOG not found at $changelogPath" -ForegroundColor Yellow
}

# ============================================================================
# STEP 4: COMMIT CHANGES
# ============================================================================

Write-Host ""
Write-Host "💾 Step 4: Committing changes..." -ForegroundColor Yellow

if ([string]::IsNullOrEmpty($CommitMessage)) {
    # Jika ForceBuild, pakai prefix fix: agar auto-tag-release trigger build
    if ($ForceBuild) {
        $CommitMessage = "fix: rebuild v$NewVersion [force-build]"
    } else {
        $CommitMessage = "fix: bump version to $NewVersion"
    }
}

try {
    # Tambah semua perubahan yang ada
    git add .
    Write-Host "  ✅ Staged all changes" -ForegroundColor Green

    foreach ($file in $updatedFiles) {
        Write-Host "  ✅ Staged: $file" -ForegroundColor Green
    }
    
    git commit -m $CommitMessage
    Write-Host "  ✅ Committed: $CommitMessage" -ForegroundColor Green
} catch {
    Write-Host "  ⚠️  Commit skipped (no changes or already committed)" -ForegroundColor Yellow
}

# ============================================================================
# STEP 5: CREATE TAG
# ============================================================================

Write-Host ""
Write-Host "🏷️  Step 5: Creating git tag..." -ForegroundColor Yellow

$tagName = "v$NewVersion"

$existingTag = git tag -l $tagName
if ($existingTag) {
    Write-Host "  ♻️  Tag $tagName sudah ada, di-update..." -ForegroundColor Yellow
    git tag -d $tagName 2>$null
    git push origin :refs/tags/$tagName 2>$null
}

git tag -a $tagName -m "Release $NewVersion" -f
Write-Host "  ✅ Created tag: $tagName" -ForegroundColor Green

# ============================================================================
# STEP 6: PUSH TO GITHUB
# ============================================================================

if (-not $SkipPush) {
    Write-Host ""
    Write-Host "🚀 Step 6: Pushing to GitHub..." -ForegroundColor Yellow
    
    $currentBranch = git branch --show-current
    Write-Host "  📍 Current branch: $currentBranch" -ForegroundColor Cyan
    
    Write-Host "  ⏳ Pushing commits..." -ForegroundColor Gray
    git pull --rebase origin $currentBranch
    git push origin $currentBranch
    Write-Host "  ✅ Commits pushed" -ForegroundColor Green
    
    Write-Host "  ⏳ Pushing tag..." -ForegroundColor Gray
    git push origin $tagName
    Write-Host "  ✅ Tag pushed: $tagName" -ForegroundColor Green
    
    Write-Host ""
    Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║                    🎉 RELEASE TRIGGERED!                       ║" -ForegroundColor Green
    Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Green
    Write-Host ""
    Write-Host "✅ Version $NewVersion has been released!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📋 Next steps:" -ForegroundColor Cyan
    Write-Host "  1. GitHub Actions will automatically build the installer" -ForegroundColor White
    Write-Host "  2. The installer will be signed (if certificate is configured)" -ForegroundColor White
    Write-Host "  3. Release will be published at:" -ForegroundColor White
    
    $repoUrl = git config --get remote.origin.url
    $repoUrl = $repoUrl -replace '\.git$', ''
    $repoUrl = $repoUrl -replace '^git@github\.com:', 'https://github.com/'
    
    Write-Host "     $repoUrl/releases/tag/$tagName" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  4. Monitor workflow at:" -ForegroundColor White
    Write-Host "     $repoUrl/actions" -ForegroundColor Yellow
    Write-Host ""
    
} else {
    Write-Host ""
    Write-Host "⏸️  Push skipped (use -SkipPush:`$false to push)" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "To push manually:" -ForegroundColor Cyan
    Write-Host "  git push origin $(git branch --show-current)" -ForegroundColor White
    Write-Host "  git push origin $tagName" -ForegroundColor White
    Write-Host ""
}

# ============================================================================
# SUMMARY
# ============================================================================

Write-Host "📊 Summary:" -ForegroundColor Cyan
Write-Host "  Version: $NewVersion" -ForegroundColor White
Write-Host "  Tag: $tagName" -ForegroundColor White
Write-Host "  ForceBuild: $(if ($ForceBuild) { 'Yes' } else { 'No' })" -ForegroundColor White
Write-Host "  Files updated: $($updatedFiles.Count)" -ForegroundColor White
Write-Host "  Pushed: $(if ($SkipPush) { 'No' } else { 'Yes' })" -ForegroundColor White
Write-Host ""
