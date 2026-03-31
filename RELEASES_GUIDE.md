# � Release Management Guide

**Quick reference - hanya essentials.**

---

## 🚀 Create Release (2 cara)

### Option 1: Interactive Script (Recommended)
```bash
./RELEASE.sh          # Linux/Mac
# or
RELEASE.bat           # Windows
```

### Option 2: Manual Git
```bash
git tag -a v1.0.0 -m "Release message"
git push origin v1.0.0
```

---

## ⚙️ Automated Workflows

| Workflow | Trigger | Action |
|----------|---------|--------|
| **build-release.yml** | Push tag `v*` | Build + create GitHub Release |
| **update-changelog.yml** | Push tag `v*` | Update CHANGELOG.md |
| **auto-generate-changelog.yml** | Daily 02:00 UTC | Prep next CHANGELOG section |

---

## 📝 Commit Format (Important!)

For CHANGELOG to auto-generate correctly, use **Conventional Commits**:

```bash
feat: add new feature              # → Features section
fix: fix a bug                      # → Bug Fixes section
refactor: improve code             # → Changes section
docs: update README                # → Changes section
```

---

## 📁 Files

```
.github/workflows/
├── build-release.yml               (Build & release)
├── update-changelog.yml            (Update CHANGELOG)
├── auto-generate-changelog.yml     (Daily auto)
└── static.yml                      (Deploy static)

RELEASE.sh                           (Linux/Mac helper)
RELEASE.bat                          (Windows helper)
auto-changelog.sh                    (Manual test script)
```

---

## 🔄 What Happens When You Release

1. **You run:** `./RELEASE.sh` or `git tag -a v1.0.0 -m "msg"`
2. **GitHub Actions:**
   - ✅ `build-release.yml` builds ZeroMix
   - ✅ `update-changelog.yml` updates CHANGELOG.md
   - ✅ GitHub Release page created
3. **Next day:** `auto-generate-changelog.yml` preps next CHANGELOG section

---

## 🔗 Quick Links

- **Actions:** https://github.com/faizinuha/ZeroMix/actions
- **Releases:** https://github.com/faizinuha/ZeroMix/releases
- **CHANGELOG:** [CHANGELOG.md](CHANGELOG.md)

---

## ❓ Troubleshooting

**Release failed?**
1. Check Actions tab for error
2. Fix error in code
3. Re-run RELEASE.sh with same version

**CHANGELOG not updating?**
1. Use correct commit format (see above)
2. Check tag format: `v1.0.0`
3. Verify latest release exists

---

**That's it!** Everything else is automatic. 🎉
