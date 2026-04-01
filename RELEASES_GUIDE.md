# 📖 Release Management Guide

**Complete automated release pipeline with Conventional Commits & signed installer.**

---

## 🚀 Create Release (Super Simple!)

```bash
# Create annotated tag
git tag -a v1.0.0 -m "Release message"

# Push to GitHub - everything runs automatically!
git push origin v1.0.0
```

**That's it!** ✅ No more manual steps needed.

---

## 📝 Commit Format (Important!)

For CHANGELOG to auto-generate from your commits, use **Conventional Commits**:

### ✅ Good Examples
```bash
# Features
git commit -m "feat: add new recording format"
git commit -m "feat(recorder): support 4K resolution"

# Bug Fixes
git commit -m "fix: crash on large file save"
git commit -m "fix(encoder): audio sync issue"

# Improvements
git commit -m "refactor: simplify encoder logic"
git commit -m "perf: improve performance 20%"
git commit -m "docs: update installation guide"
```

### ❌ Avoid These
```bash
git commit -m "update"           # Too generic
git commit -m "fix bug"          # Not categorized
git commit -m "changes"          # No description
git commit -m "WIP: stuff"       # Work in progress
```

**Why?** The workflow parses these messages to auto-generate CHANGELOG sections.

---

## 📥 Release Contains (Automatic!)

After push tag, your Release page has:

### 1. **Changelog Summary**
- Automatically generated from your commits
- Categorized by type (Features, Fixes, Improvements)
- Always up-to-date

### 2. **Portable EXE**
```
ZeroMix-v1.0.0.exe
```
- Ready to run immediately  
- No installation needed
- Good for testing

### 3. **EXE Installer** (Inno Setup)
```
ZeroMix-v1.0.0-Setup.exe
```
- Professional installer (Inno Setup)
- Signed with code certificate
- Adds to Start Menu
- Add/Remove Programs support
- Easy uninstall
- Checks .NET 9 & WebView2 dependencies

---

## 🔄 What Happens (Behind the Scenes)

When you push a tag:

```
1. GitHub Actions detects tag
   └─ Workflow: build-release.yml starts

2. Build Application
   ├─ Checkout code
   ├─ Setup .NET 9.0
   ├─ Build for Release
   ├─ Publish executable
   └─ Download FFMPEG

3. Code Signing (Pre-Installer)
   ├─ Download osslsigncode
   ├─ Sign main ZeroMix.exe
   └─ Uses CERT_PASSWORD secret

4. Generate Installer
   ├─ Create EXE installer using Inno Setup
   ├─ Bundles the SIGNED exe inside
   ├─ Version passed via /DAppVersion=
   └─ Sign the installer itself

5. Generate Changelog
   ├─ Parse your commits
   ├─ Categorize (feat/fix/refactor)
   └─ Create summary

6. Create GitHub Release
   ├─ Release page created
   ├─ Summary added
   ├─ Both EXE and Installer attached
   └─ Ready for download!
```

---

## 📊 Release Page Example

```
🎉 ZeroMix v1.0.0

What's Changed

✨ Features
- feat: add new recording format  
- feat(ui): improve dark mode

🐛 Bug Fixes
- fix: crash on large recordings
- fix(encoder): audio quality issue

🚀 Improvements
- refactor: simplify code structure
- perf: increase speed 15%

📥 Downloads
- ZeroMix-v1.0.0.exe (Portable)
- ZeroMix-v1.0.0-Setup.exe (Installer)

🖥️ System Requirements
- Windows 10/11 (64-bit)
- .NET 9.0 Desktop Runtime
- WebView2 Runtime
- DirectX 12 compatible GPU
- 2GB RAM minimum
```

---

## 🔗 Quick Links

- **GitHub Actions:** https://github.com/faizinuha/ZeroMix/actions
- **Releases Page:** https://github.com/faizinuha/ZeroMix/releases
- **CHANGELOG:** [CHANGELOG.md](CHANGELOG.md)

---

## 💡 Best Practices

✅ **DO:**
- Use Conventional Commits format
- Write clear commit messages
- Tag releases consistently (v1.0.0)
- Review CHANGELOG after creation

❌ **DON'T:**
- Use generic commit messages  
- Forget to follow commit format
- Tag without incrementing version
- Push commits without message

---

## ❓ Help

**Release failed?**
1. Check GitHub Actions tab for error
2. Look at specific step logs
3. Common: .NET build warnings (usually safe)

**CHANGELOG empty?**
1. Verify commits use Conventional Commits
2. Check tag format: `v1.0.0` (not `1.0.0`)
3. Ensure commits between last tag and now

**Installer didn't build?**
1. Check Inno Setup step in Actions log
2. Portable EXE still available as fallback
3. Verify Setup.iss paths are correct

**Code signing skipped?**
1. Set `CERT_PASSWORD` secret in repo Settings → Secrets → Actions
2. Check if `ZeroMixCert.pfx` exists in `Exe/` folder
3. osslsigncode download may have failed — check logs

---

**Everything is automatic.** Just commit, push tag, and done! 🚀
