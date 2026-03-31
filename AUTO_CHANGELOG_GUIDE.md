# 🤖 Auto-Generate CHANGELOG - Complete Setup

Workflow otomatis yang membaca **latest GitHub release**, menganalisis commits setelahnya, dan auto-update CHANGELOG.md setiap hari.

## 🎯 Apa yang dilakukan?

```
Latest Release (e.g., v1.0.0)
        ↓
   [24 jam kemudian]
        ↓
Workflow runs (pukul 02:00 UTC / 9:00 WIB)
        ↓
Baca latest release tag dari GitHub
        ↓
Find semua commits sejak release itu
        ↓
Parse commits by type (feat, fix, chore, etc)
        ↓
Generate CHANGELOG section baru
        ↓
Auto-commit ke repository
        ↓
CHANGELOG.md updated ✅
```

---

## ⚙️ Konfigurasi

### 1️⃣ Schedule Time
**File:** `.github/workflows/auto-generate-changelog.yml` (line 10)

```yaml
- cron: '0 2 * * *'  # 02:00 UTC (9:00 WIB) setiap hari
```

**Mengubah waktu:**
- `0 2 * * *` = 02:00 UTC setiap hari ← CURRENT
- `0 9 * * *` = 09:00 UTC (16:00 WIB)
- `0 0 * * 1` = Senin 00:00 UTC (weekly)
- `0 3 * * 1-5` = Weekdays 03:00 UTC

**Cron Reference:** https://crontab.guru/

### 2️⃣ Commit Message Template
**File:** `.github/workflows/auto-generate-changelog.yml` (step: Auto-Commit)

```yaml
git commit -m "docs(changelog): update for $NEXT_VERSION" \
    -m "Auto-generated changelog from commits since last release" \
    -m "Workflow: Auto-Generate CHANGELOG"
```

Customize message sesuai preference:
- Baris 1: Subject line (required for git)
- Baris 2+: Body (optional)

### 3️⃣ Version Bump Strategy
**Current:** Increments PATCH version only
- v1.0.0 → v1.0.1
- v2.3.5 → v2.3.6

**Untuk mengubah:**
Edit di file `.github/workflows/auto-generate-changelog.yml` (step: Determine Next Version)

---

## 🚀 Usage

### ✅ Automatic (Default)
Workflow akan:
- ✨ Trigger secara otomatis setiap hari pukul 02:00 UTC
- 📖 Update CHANGELOG.md
- 💾 Auto-commit perubahan
- ✅ Selesai

**Monitoring:**
1. Pergi ke: https://github.com/faizinuha/ZeroMix/actions
2. Cari: "📝 Auto-Generate CHANGELOG"
3. Lihat hasil run harian

### ▶️ Manual Trigger (Testing)
Jika ingin jalankan sekarang (tidak perlu tunggu 24 jam):

```bash
# Via GitHub Actions GUI
1. Go to: Actions tab
2. Select: "📝 Auto-Generate CHANGELOG"
3. Click: "Run workflow" dropdown
4. Select branch: "ProyekTil"
5. Click: "Run workflow"
```

**Atau via GitHub CLI:**
```bash
gh workflow run auto-generate-changelog.yml
```

### 🖥️ Local Manual Script (Testing)
Untuk test atau jalankan manual di komputer:

```bash
# Make script executable
chmod +x auto-changelog.sh

# Run it
./auto-changelog.sh
```

**Output contoh:**
```
🚀 Starting Auto-Changelog Generation...
🔧 Setting up Git config...
📦 Fetching latest release...
✅ Latest release: v1.0.0
📌 Next version: v1.0.1
📊 Found 15 new commits
📋 Generating changelog...
✅ CHANGELOG.md updated

📄 Preview (first 30 lines):
## [v1.0.1] - 2026-03-31

### ✨ Features
- a1b2c3d: feat: add new recording format
- d4e5f6a: feat: improve performance monitoring

### 🐛 Bug Fixes
- g7h8i9j: fix: crash on recorder start
...

💾 Auto-committing changes...
✅ Changes committed
📤 Ready to push? Run:
   git push origin ProyekTil
```

---

## 📋 CHANGELOG Format

**Otomatis di-generate dengan struktur:**

```markdown
## [v1.0.1] - 2026-03-31

### ✨ Features
- a1b2c3d: feat: add new recording format
- d4e5f6a: feat: improve performance monitoring

### 🐛 Bug Fixes
- g7h8i9j: fix: crash on recorder start
- k1l2m3n: fix: audio sync issue

### 🔧 Changes
- o4p5q6r: refactor: simplify encoder logic
- s7t8u9v: docs: update README

### 📊 Commit Summary
- Total commits: 15
- Date range: v1.0.0 to HEAD
```

---

## 🔄 How It Works

### Step by Step

#### 1. Checkout Repository
```bash
git clone dengan fetch-depth: 0
# Fetch semua history untuk akses ke semua tags
```

#### 2. Get Latest Release
```bash
gh release view --json tagName
# Ambil latest release tag dari GitHub
```

#### 3. Generate Next Version
```
Current: v1.0.0
↓
Parse: Major=1, Minor=0, Patch=0
↓
Increment Patch: Patch + 1 = 1
↓
Result: v1.0.1
```

#### 4. Find New Commits
```bash
git log v1.0.0..HEAD
# Ambil semua commits antara release terakhir dan HEAD
```

#### 5. Parse by Type
```bash
# Commits dimulai dengan "feat:" -> Features section
# Commits dimulai dengan "fix:" -> Bug Fixes section
# Commits dimulai dengan "chore:", "refactor:", dll -> Changes section
```

#### 6. Generate Changelog
```markdown
Combine semua sections:
- Header dengan version dan date
- Features list
- Bug fixes list
- Changes list
- Summary statistics
- Append existing CHANGELOG
```

#### 7. Auto-Commit
```bash
git add CHANGELOG.md
git commit -m "docs(changelog): update for v1.0.1" ...
git push
```

---

## ✅ Commit Message Convention

Workflow membaca commits dengan format **Conventional Commits**:

```
feat: description        → Features section
fix: description         → Bug Fixes section  
chore: description       → Changes section
refactor: description    → Changes section
docs: description        → Changes section
style: description       → Changes section
perf: description        → Changes section
test: description        → Changes section
ci: description          → Tidak di-include
```

**Contoh:**
```bash
git commit -m "feat: add new recording codec"          # ✅ Features
git commit -m "fix: crash on large file recording"     # ✅ Bug Fixes
git commit -m "refactor: simplify encoder logic"       # ✅ Changes
git commit -m "docs: update recorder documentation"    # ✅ Changes
git commit -m "ci: update build workflow"              # ⏭️ Skipped
```

---

## 🐛 Troubleshooting

### ❌ Workflow tidak berjalan otomatis

**Penyebab & Solusi:**

| Masalah | Penyebab | Solusi |
|---------|---------|--------|
| Workflow belum dimulai | Schedule belum tiba | Tunggu atau trigger manual |
| Error di step Git config | Git user belum diset | Auto-set dalam workflow |
| CHANGELOG tidak update | Tidak ada commit baru | Normal jika tidak ada commit |
| Commit gagal | Branch protection rules | GitHub Actions permissions |

**Cek workflow status:**
```bash
# Via GitHub CLI
gh workflow view auto-generate-changelog.yml

# Via GitHub Web
Actions → Auto-Generate CHANGELOG → See runs
```

### ❌ No new commits detected

**Jika CHANGELOG tidak update padahal ada commits:**

1. **Check commit format:**
   ```bash
   # Lihat last 5 commits
   git log --oneline -5
   
   # Harus start dengan: feat: fix: chore: dll
   ```

2. **Check branch:**
   ```bash
   # Workflow hanya jalan di ProyekTil
   git branch
   git push origin ProyekTil
   ```

3. **Check release exists:**
   ```bash
   # Minimal harus ada 1 release
   gh release list
   ```

### ❌ Error: "GITHUB_TOKEN permission denied"

**Solusi:**
1. Go to: Repository Settings → Actions → General
2. Find: "Workflow permissions"
3. Select: "Read and write permissions"
4. Check: "Allow GitHub Actions to create and approve PRs"
5. Save

---

## 📊 Monitoring

### Real-time Monitoring
```bash
# Watch workflow runs (requires GitHub CLI)
gh run watch --repo faizinuha/ZeroMix
```

### Via GitHub Web
1. Go to: https://github.com/faizinuha/ZeroMix/actions
2. Filter: "Auto-Generate CHANGELOG"
3. Click run untuk melihat logs

### Check last run
```bash
gh run list --repo faizinuha/ZeroMix --workflow auto-generate-changelog.yml --limit 5
```

---

## 🧪 Testing

### Test 1: Manual Trigger
1. Go to Actions tab
2. Select "Auto-Generate CHANGELOG"
3. Click "Run workflow" → Run it
4. Cek CHANGELOG.md updated

### Test 2: Local Script
```bash
./auto-changelog.sh
git diff CHANGELOG.md
```

### Test 3: Schedule Timing
1. Set cron to 5 minutes from now
2. Wait untuk workflow trigger
3. Cek Actions tab untuk confirmation

---

## 🔒 Permissions Required

Workflow membutuhkan:

```yaml
permissions:
  contents: write          # Update CHANGELOG.md, push commits
  pull-requests: read      # Read PR info (optional)
```

**Di-setup otomatis** dalam `.github/workflows/auto-generate-changelog.yml`

---

## 📝 Associated Files

```
.github/
└── workflows/
    └── auto-generate-changelog.yml     # Main workflow ← HERE
    
auto-changelog.sh                        # Local manual script ← OPTIONAL
CHANGELOG.md                             # Output file (di-update)
```

---

## 🚀 Quick Start Commands

```bash
# 1. Check if workflow exists
ls -la .github/workflows/auto-generate-changelog.yml

# 2. Make local script executable (optional)
chmod +x auto-changelog.sh

# 3. Test local script
./auto-changelog.sh

# 4. Trigger workflow manually via CLI
gh workflow run auto-generate-changelog.yml

# 5. Monitor workflow
gh run list --repo faizinuha/ZeroMix --workflow auto-generate-changelog.yml

# 6. Check CHANGELOG was updated
git log --oneline -3 | grep changelog
```

---

## 💡 Tips & Best Practices

1. **Keep commit messages clear**
   - ✅ `feat: add noise reduction`
   - ❌ `update stuff`

2. **Organize by feature branches**
   - Merge feature branches ke ProyekTil
   - Workflow akan detect commits otomatis

3. **Review CHANGELOG regularly**
   - Jika ada kesalahan, edit manual CHANGELOG.md
   - Git history akan tercatat

4. **Test before production**
   - Gunakan manual trigger dulu
   - Verifikasi output sebelum automating

5. **Use semantic versioning**
   - Workflow auto-increments patch
   - Adjust if need major/minor bump

---

## 📞 Support

**Jika ada error:**
1. Check `.github/workflows/auto-generate-changelog.yml` syntax
2. Verify branch protection rules allow bot commits
3. Use `gh workflow view` untuk debug
4. Check Actions tab real-time logs

**Untuk customization:**
- Edit `.github/workflows/auto-generate-changelog.yml`
- Change timing, message format, version strategy
- Test with manual trigger first

---

**Status:** ✅ Ready to Use  
**Schedule:** Daily 02:00 UTC (9:00 WIB)  
**Last Updated:** March 31, 2026
