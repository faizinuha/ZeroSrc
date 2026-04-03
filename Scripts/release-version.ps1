# ZeroMix Version Release Script
# Otomatis update versi, commit, push, dan trigger release

param(
    [Parameter(Mandatory=$true)]
    [string]$NewVersion,
    
    [string]$CommitMessage = "",
    [switch]$SkipPush = $false,
    [switch]$ForceBuild = $false
)

$ErrorActionPreference = "Stop"

# ============================================================================
# VALIDASI VERSION FORMAT
# ============================================================================

if ($NewVersion -notmatch '^\d+\.\d+\.\d+$') {
    Write-Host "❌ Format versi salah! Gunakan format: X.Y.Z (contoh: 5.1.6)" -ForegroundColor Red
    Write-Host "Contoh: .\release-version.ps1 -NewVersion 5.1.6" -ForegroundColor Yellow
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
    if (-not $ForceBuild) {
        $continue = Read-Host "Continue anyway? (y/n)"
        if ($continue -ne "y") {
            exit 0
        }
    } else {
        Write-Host "  🔨 ForceBuild flag set, continuing..." -ForegroundColor Cyan
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
# STEP 3: UPDATE CHANGELOG
# ============================================================================

Write-Host ""
Write-Host "📝 Step 3: Updating CHANGELOG..." -ForegroundColor Yellow

$changelogPath = "Docs/CHANGELOG.md"
if (Test-Path $changelogPath) {
    try {
        $changelog = Get-Content $changelogPath -Raw
        $date = Get-Date -Format "yyyy-MM-dd"
        
        $newEntry = @"
## [v$NewVersion] - $date

### ✨ Features
- To be documented

### 🐛 Bug Fixes
- To be documented

### 🔧 Changes
- Version bump to $NewVersion

---

"@
        
        if ($changelog -match '(?s)(# ZeroMix - Changelog.*?---\s*)(.*)') {
            $header = $matches[1]
            $rest = $matches[2]
            $newChangelog = $header + "`n" + $newEntry + $rest
            Set-Content -Path $changelogPath -Value $newChangelog -NoNewline
            Write-Host "  ✅ CHANGELOG updated" -ForegroundColor Green
            $updatedFiles += $changelogPath
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
    Write-Host "  ⚠️  Tag $tagName already exists!" -ForegroundColor Yellow
    $overwrite = Read-Host "Delete and recreate tag? (y/n)"
    if ($overwrite -eq "y") {
        git tag -d $tagName
        git push origin :refs/tags/$tagName 2>$null
        Write-Host "  ✅ Deleted old tag" -ForegroundColor Green
    } else {
        Write-Host "  ❌ Aborted" -ForegroundColor Red
        exit 1
    }
}

git tag -a $tagName -m "Release $NewVersion"
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
