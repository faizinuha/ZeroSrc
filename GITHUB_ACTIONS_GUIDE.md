# 🚀 ZeroMix GitHub Actions - Release Automation Guide

## Overview

ZeroMix menggunakan **GitHub Actions** untuk automasi build, release, dan changelog generation. Anda tidak perlu lagi membuat release notes secara manual!

## Available Workflows

### 1. 🚀 Build & Release (`build-release.yml`)
**Trigger:** Push tag dengan format `v*` (e.g., `v1.0.0`) atau manual dispatch

**Apa yang dilakukan:**
- ✅ Build project dengan .NET 9.0
- ✅ Generate changelog dari git commits
- ✅ Create GitHub Release dengan release notes
- ✅ Upload executable files
- ✅ Automatic versioning dari tag

---

### 2. 📄 Generate Release Notes (`release-notes.yml`)
**Trigger:** Push tag atau manual via Actions tab

**Apa yang dilakukan:**
- ✅ Parse git commits untuk changelog
- ✅ Categorize changes (Features, Bug Fixes, Improvements)
- ✅ Generate formatted release notes
- ✅ Create/update GitHub release

---

### 3. 📝 Update CHANGELOG (`update-changelog.yml`)
**Trigger:** Push tag (auto-triggered setelah release)

**Apa yang dilakukan:**
- ✅ Parse git commits
- ✅ Update CHANGELOG.md file
- ✅ Commit dan push changes otomatis
- ✅ Maintain changelog history

---

## How to Create a Release

### Method 1: Using Git Tags (Recommended)

```bash
# 1. Create annotated tag
git tag -a v1.2.0 -m "Release version 1.2.0"

# 2. Push tag to GitHub
git push origin v1.2.0

# 3. Watch the magic!
# Go to: https://github.com/faizinuha/ZeroMix/actions
```

### Method 2: Using Interactive Script

```bash
# Linux/Mac
./RELEASE.sh

# Windows
RELEASE.bat
```

### Method 3: GitHub Web Interface

1. Go to [Releases](https://github.com/faizinuha/ZeroMix/releases)
2. Click "Create a new release"
3. Set tag name: `v1.2.0`
4. Click "Publish release"

---

## Commit Message Conventions

Gunakan **Conventional Commits** untuk optimal changelog generation:

### Types:
- **feat:** ✨ New feature → Listed in Features
- **fix:** 🐛 Bug fix → Listed in Bug Fixes
- **refactor:** 🔄 Code refactoring → Listed in Improvements
- **perf:** ⚡ Performance → Listed in Improvements
- **docs:** 📖 Documentation
- **test:** 🧪 Tests
- **chore:** 🔧 Build/config

### Examples:

```bash
git commit -m "feat(recorder): add audio capture support"
git commit -m "fix(encoder): handle FFmpeg process death"
git commit -m "perf(capture): optimize frame queue"
```

---

## Workflow Monitoring

1. Go to [Actions Tab](https://github.com/faizinuha/ZeroMix/actions)
2. Watch live logs as workflows run
3. Release complete when ✅ appears

---

## Troubleshooting

### Release Failed
- Check build errors in Actions logs
- Ensure all changes are committed
- Use correct tag format: `v1.0.0`

### Changelog Not Generated
- Verify commits follow Conventional Commits format
- Check previous tag exists: `git tag -l 'v*'`

### Release Notes Look Wrong
- Edit workflow at `.github/workflows/release-notes.yml`
- Adjust commit search patterns if needed

---

## Best Practices

### ✅ DO's
- ✅ Use semantic versioning (1.0.0)
- ✅ Write descriptive commit messages
- ✅ Follow Conventional Commits format
- ✅ Commit all changes before tagging

### ❌ DON'Ts
- ❌ Manually edit release notes
- ❌ Use non-standard tag formats
- ❌ Mix commit conventions

---

## Useful Git Commands

```bash
# List all tags
git tag

# Show tag info
git show v1.0.0

# Delete local tag  
git tag -d v1.0.0

# Delete remote tag
git push origin --delete v1.0.0

# See commits since last tag
git log v1.0.0.. --oneline
```

---

**For detailed setup info, see:** `GITHUB_ACTIONS_SETUP_SUMMARY.md`
