# 🚀 ZeroMix Release Management Guide

**Complete reference for creating releases and managing CHANGELOG automatically.**

---

## 📋 Quick Start (5 Minutes)

### Creating a New Release

#### Option 1: Interactive Script (Recommended)
```bash
# Linux/Mac
./RELEASE.sh

# Windows
RELEASE.bat
```

Then follow the prompts. Script will:
1. ✅ Check git status
2. ✅ Ask for version (e.g., 5.0.1)
3. ✅ Create tag and push to GitHub
4. ✅ GitHub Actions builds automatically

#### Option 2: Manual Git
```bash
git tag -a v5.0.1 -m "Release message"
git push origin v5.0.1
```

#### Option 3: GitHub Web UI
1. Go to Releases tab
2. Click "Create new release"
3. Set tag and fill details
4. Publish

### What Happens Next ✅
- 🏗️ **build-release.yml** runs - builds ZeroMix
- 📋 **update-changelog.yml** runs - updates CHANGELOG.md
- 📦 Creates GitHub Release page
- Every day: **auto-generate-changelog.yml** prepares next CHANGELOG section

---

## 📁 File Structure

```
.github/workflows/              ← Automated workflows
├── build-release.yml          ← Builds & creates release
├── update-changelog.yml        ← Updates CHANGELOG.md
├── auto-generate-changelog.yml ← Daily auto-changelog
└── static.yml                  ← Deploys static content

scripts/                        ← Manual helpers
├── RELEASE.sh                  ← Interactive release (Linux/Mac)
├── RELEASE.bat                 ← Interactive release (Windows)
└── auto-changelog.sh           ← Manual changelog generator

docs/
├── CHANGELOG.md                ← Version history
└── RELEASES_GUIDE.md           ← This file
```

---

## 🔄 Release Workflows

### Workflow 1: Build & Release (`build-release.yml`)
**Triggered:** When tag pushed (v*)

**Steps:**
1. Checkout code
2. Setup .NET 9.0
3. Build ZeroMix
4. Publish Release build
5. Generate changelog from commits
6. Create GitHub Release
7. Upload artifacts

**Output:** 
- GitHub Release page with changelog
- Build artifacts (if present)

---

### Workflow 2: Update CHANGELOG (`update-changelog.yml`)
**Triggered:** When tag pushed (v*)

**Steps:**
1. Parse commits since last release
2. Categorize by type (features, fixes, improvements)
3. Update CHANGELOG.md
4. Auto-commit & push

**Output:**
- Updated CHANGELOG.md in repository

---

### Workflow 3: Auto-Generate CHANGELOG (`auto-generate-changelog.yml`)
**Triggered:** Daily at 02:00 UTC (09:00 WIB)

**Steps:**
1. Read latest GitHub release
2. Find commits since that release
3. Parse commits by type
4. Generate next version section
5. Update CHANGELOG.md
6. Auto-commit & push

**Output:**
- CHANGELOG.md updated daily with new commits

---

## 📝 Commit Message Convention

For CHANGELOG to work correctly, use **Conventional Commits**:

```bash
# Features
git commit -m "feat: add new recording codec"
git commit -m "feat(recorder): support 4K resolution"

# Bug fixes
git commit -m "fix: crash on large file recording"
git commit -m "fix(encoder): audio sync issue"

# Improvements
git commit -m "refactor: simplify encoder logic"
git commit -m "perf: improve performance monitoring"
git commit -m "docs: update README"

# Other (not included in CHANGELOG)
git commit -m "ci: update workflow"
git commit -m "test: add unit tests"
```

**Format:** `type(scope): description`
- `type`: feat, fix, refactor, perf, docs, style, chore, test, ci
- `scope`: optional, what it affects
- `description`: clear, concise message

**Important:** Commits not starting with recognized type may be skipped!

---

## 📊 CHANGELOG Format

Generated automatically with structure:

```markdown
## [v5.0.1] - 2026-03-31

### ✨ Features
- a1b2c3d: feat: add new recording format
- d4e5f6a: feat: improve performance monitoring

### 🐛 Bug Fixes
- g7h8i9j: fix: crash on recorder start
- k1l2m3n: fix: audio sync issue

### 🚀 Improvements
- o4p5q6r: refactor: simplify encoder logic
- s7t8u9v: docs: update README

### 📊 Commit Summary
- Total commits: 15
- Range: v5.0.0..HEAD
```

---

## 🎯 Complete Release Workflow

```
1. DEVELOP
   └─ git commit -m "feat: new feature"
   └─ git commit -m "fix: bug fix"

2. PREPARE RELEASE
   └─ ./RELEASE.sh  (or RELEASE.bat)
   └─ Enter version: 5.0.1
   └─ Confirm

3. GITHUB ACTIONS RUNS
   ├─ build-release.yml
   │  ├─ Build ZeroMix
   │  └─ Create GitHub Release
   └─ update-changelog.yml
      └─ Update CHANGELOG.md

4. NEXT DAY (02:00 UTC)
   └─ auto-generate-changelog.yml runs
      └─ Prepares CHANGELOG for next version

5. DONE ✅
   └─ Release available at GitHub
   └─ CHANGELOG updated
   └─ Users can download
```

---

## ⚙️ Configuration

### Change Release Time
**File:** `.github/workflows/auto-generate-changelog.yml` (line 10)

```yaml
schedule:
  - cron: '0 2 * * *'  # 02:00 UTC (09:00 WIB)
```

**Common times:**
- `0 2 * * *` = 02:00 UTC (09:00 WIB) ← Current
- `0 9 * * *` = 09:00 UTC (16:00 WIB)
- `0 3 * * 1-5` = 03:00 UTC weekdays

**Generator:** https://crontab.guru

### Version Bump Strategy
**Current:** Auto-increments PATCH version
- v1.0.0 → v1.0.1

**To change:** Edit `.github/workflows/auto-generate-changelog.yml` step "Determine Next Version"

---

## 🧪 Testing & Manual Runs

### Test Local Changelog Generation
```bash
./auto-changelog.sh
```

**Output:**
```
🚀 Starting Auto-Changelog Generation...
📦 Latest release: v5.0.0
📌 Next version: v5.0.1
📊 Found 5 new commits
✅ CHANGELOG.md updated
💾 Auto-committing changes...
✅ Changes committed
📤 Ready to push? Run:
   git push origin ProyekTil
```

### Manual Trigger Workflow
```bash
# Via GitHub CLI
gh workflow run auto-generate-changelog.yml

# Or click in GitHub Actions UI:
# Actions → Auto-Generate CHANGELOG → Run workflow
```

### Monitor Release Progress
```bash
# GitHub Web
https://github.com/faizinuha/ZeroMix/actions

# GitHub CLI
gh run view [run-id]
gh run list --workflow build-release.yml
```

---

## 🚨 Troubleshooting

### Issue: Release workflow fails

**Check:**
1. Go to Actions tab
2. Click failed workflow
3. Expand failed step for error message
4. Common causes:
   - .NET build error (check compiler warnings)
   - Git config issue
   - Permissions problem

**Solution:**
- Fix error in code
- Commit fix: `git commit -m "fix: build error"`
- Retag and push: Re-run RELEASE.sh with same version

### Issue: CHANGELOG not updating

**Check:**
1. Verify commits use Conventional Commits format
2. Check tag format: `v1.0.0` (not `1.0.0`)
3. Ensure latest release exists

**Solution:**
```bash
# Check commits
git log --oneline -10

# Check releases
gh release list

# Run manual script to test
./auto-changelog.sh
```

### Issue: Too many files in CHANGELOG

**Normal!** CHANGELOG grows over time. To archive:
```bash
# Create CHANGELOG.archive
cp CHANGELOG.md CHANGELOG.archive

# Keep only recent in CHANGELOG.md
# Edit manually or create new
```

---

## 📈 Monitoring

### Daily Checks (Optional)
- Go to Actions tab
- See auto-generate-changelog.yml runs
- Check CHANGELOG.md for updates

### Before Creating Release
- Make sure environment is clean: `git status`
- Update version thoughtfully
- Write clear commit messages

### After Creating Release
- Check build completed: Actions tab
- Verify release page: GitHub Release
- Download and test if needed

---

## 💡 Best Practices

✅ **DO:**
- Use semantic versioning (1.0.0, not 1.0, not 1)
- Follow Conventional Commits format
- Write clear, descriptive commit messages
- Tag releases consistently (v1.0.0)
- Review CHANGELOG before major releases

❌ **DON'T:**
- Push commits without clear messages
- Use vague commit subjects ("fix stuff", "update")
- Skip git config (name/email)
- Forget to push tags (`git push --tags`)
- Break semantic versioning

---

## 🔗 Quick Links

| Resource | Link |
|----------|------|
| GitHub Repository | https://github.com/faizinuha/ZeroMix |
| Releases Page | https://github.com/faizinuha/ZeroMix/releases |
| Actions Tab | https://github.com/faizinuha/ZeroMix/actions |
| Issues | https://github.com/faizinuha/ZeroMix/issues |
| CHANGELOG | [CHANGELOG.md](CHANGELOG.md) |

---

## 📚 Files Reference

| File | Purpose | Edit? |
|------|---------|-------|
| `.github/workflows/build-release.yml` | Release build | Only if customizing |
| `.github/workflows/update-changelog.yml` | CHANGELOG update | Only if customizing |
| `.github/workflows/auto-generate-changelog.yml` | Daily auto | Edit cron time if needed |
| `.github/workflows/static.yml` | Static deploy | Don't edit |
| `RELEASE.sh` | Release helper | Don't edit |
| `RELEASE.bat` | Release helper (Win) | Don't edit |
| `auto-changelog.sh` | Manual changelog | Don't edit |
| `CHANGELOG.md` | Changelog file | Auto-updated, optional manual edit |
| `RELEASES_GUIDE.md` | This file | Reference only |

---

## 🎉 You're Ready!

Everything is set up and ready to use. To create your first release:

```bash
./RELEASE.sh
# Follow prompts and you're done!
```

**That's it!** GitHub Actions handles the rest automatically. 🚀

---

**Last Updated:** March 31, 2026  
**Status:** ✅ Production Ready  
**Maintenance:** Minimal - mostly automatic
