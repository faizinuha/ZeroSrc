# ZeroMix Release Script
# Usage: .\Scripts\release-version.ps1 -Version 5.6.0
# Usage: .\Scripts\release-version.ps1 -Version 5.6.0 -DryRun

param(
    [Parameter(Mandatory=$true)]
    [string]$Version,

    [switch]$DryRun,      # Preview saja, tidak commit/push
    [switch]$ForceBuild   # Re-release tag yang sama (hapus tag lama, push ulang)
)

$ErrorActionPreference = "Stop"

# ── Validasi format versi ──────────────────────────────────────────────────
if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    Write-Error "Format versi salah. Gunakan: X.Y.Z (contoh: 5.6.0)"
    exit 1
}

$Tag = "v$Version"

Write-Host ""
Write-Host "  ZeroMix Release — $Tag" -ForegroundColor Cyan
if ($DryRun)    { Write-Host "  [DRY RUN — tidak ada yang di-commit/push]" -ForegroundColor Yellow }
if ($ForceBuild){ Write-Host "  [FORCE BUILD — tag lama akan dihapus dan di-push ulang]" -ForegroundColor Magenta }
Write-Host ""

# ── Step 1: Update versi di file ───────────────────────────────────────────
Write-Host "1. Updating version in files..." -ForegroundColor Yellow

$files = @(
    @{
        Path    = "src/MainWindow.xaml.cs"
        Pattern = 'private const string CURRENT_VERSION = "[\d\.]+";'
        Replace = "private const string CURRENT_VERSION = `"$Version`";"
    },
    @{
        Path    = "Exe/Setup.iss"
        Pattern = '#define AppVersion "[\d\.]+"'
        Replace = "#define AppVersion `"$Version`""
    },
    @{
        Path    = "ZeroMix.csproj"
        Pattern = '<Version>[\d\.]+</Version>'
        Replace = "<Version>$Version</Version>"
    }
)

foreach ($f in $files) {
    if (-not (Test-Path $f.Path)) {
        Write-Host "   skip (not found): $($f.Path)" -ForegroundColor Gray
        continue
    }
    $content    = Get-Content $f.Path -Raw
    $newContent = $content -replace $f.Pattern, $f.Replace

    if ($content -eq $newContent) {
        Write-Host "   unchanged: $($f.Path)" -ForegroundColor Gray
    } else {
        if (-not $DryRun) { Set-Content $f.Path $newContent -NoNewline }
        Write-Host "   updated:   $($f.Path)" -ForegroundColor Green
    }
}

# ── Step 2: Pastikan CHANGELOG sudah ada entry untuk versi ini ────────────
Write-Host ""
Write-Host "2. Checking CHANGELOG..." -ForegroundColor Yellow

$changelogPath = "Docs/CHANGELOG.md"
if (Test-Path $changelogPath) {
    $cl = Get-Content $changelogPath -Raw
    if ($cl -match "## \[v?$([regex]::Escape($Version))\]") {
        Write-Host "   found: v$Version entry exists" -ForegroundColor Green
    } else {
        Write-Host "   WARNING: No entry for v$Version found in CHANGELOG" -ForegroundColor Red
        Write-Host "   Add the entry manually before releasing." -ForegroundColor Yellow
        if (-not $DryRun) {
            $confirm = Read-Host "   Continue without CHANGELOG entry? (y/N)"
            if ($confirm -ne "y") { exit 0 }
        }
    }
} else {
    Write-Host "   skip (CHANGELOG not found)" -ForegroundColor Gray
}

# ── Step 3: Commit ─────────────────────────────────────────────────────────
Write-Host ""
Write-Host "3. Committing..." -ForegroundColor Yellow

if (-not $DryRun) {
    git add .
    $status = git status --porcelain
    if ($status) {
        git commit -m "chore: release $Tag"
        Write-Host "   committed: chore: release $Tag" -ForegroundColor Green
    } else {
        Write-Host "   nothing to commit" -ForegroundColor Gray
    }
} else {
    Write-Host "   [dry run] would commit: chore: release $Tag" -ForegroundColor Gray
}

# ── Step 4: Tag ────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "4. Tagging..." -ForegroundColor Yellow

if (-not $DryRun) {
    $existing = git tag -l $Tag
    if ($existing) {
        if ($ForceBuild) {
            Write-Host "   [ForceBuild] deleting local tag $Tag..." -ForegroundColor Magenta
            git tag -d $Tag | Out-Null
            Write-Host "   [ForceBuild] deleting remote tag $Tag..." -ForegroundColor Magenta
            git push origin ":refs/tags/$Tag" 2>$null
        } else {
            Write-Host "   tag $Tag already exists, deleting..." -ForegroundColor Yellow
            git tag -d $Tag | Out-Null
        }
    }
    git tag -a $Tag -m "Release $Version"
    Write-Host "   created: $Tag" -ForegroundColor Green
} else {
    Write-Host "   [dry run] would create tag: $Tag" -ForegroundColor Gray
}

# ── Step 5: Push ───────────────────────────────────────────────────────────
Write-Host ""
Write-Host "5. Pushing..." -ForegroundColor Yellow

if (-not $DryRun) {
    $branch = git branch --show-current
    git pull --rebase origin $branch
    git push origin $branch
    if ($ForceBuild) {
        git push origin $Tag --force
        Write-Host "   force pushed tag: $Tag" -ForegroundColor Magenta
    } else {
        git push origin $Tag
        Write-Host "   pushed tag:    $Tag" -ForegroundColor Green
    }
    Write-Host "   pushed branch: $branch" -ForegroundColor Green
} else {
    Write-Host "   [dry run] would push branch + tag $Tag" -ForegroundColor Gray
}

# ── Done ───────────────────────────────────────────────────────────────────
Write-Host ""
if ($DryRun) {
    Write-Host "  Dry run complete. Run without -DryRun to release." -ForegroundColor Yellow
} else {
    $repoUrl = (git config --get remote.origin.url) -replace '\.git$','' -replace '^git@github\.com:','https://github.com/'
    Write-Host "  Released $Tag" -ForegroundColor Green
    Write-Host "  $repoUrl/releases/tag/$Tag" -ForegroundColor Cyan
    Write-Host "  $repoUrl/actions" -ForegroundColor Cyan
}
Write-Host ""
