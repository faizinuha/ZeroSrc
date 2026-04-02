## [v5.1.2] - 2026-04-02

### ✨ Features
- 76cfb8b: feat: Perubahan Utama:
- 7ee1fa2: feat: implement search overlay with suggestion system and add automated release tagging workflow

### 🐛 Bug Fixes
- aa5632e: fix: Memperbaiki Ci/Cd
- a5b4723: fix: Menambah fitur perekaman area
- e5ef89c: fix: memory leak in onboarding and VA, optimize eye tracking, and update build trigger for chore

### 🔧 Changes
- aeffb49: chore: bump version to 5.2.2 [skip ci]
- 8f97cea: chore: bump version to 5.2.1 [skip ci]

### 📊 Commit Summary
- Total commits: **8**
- Date range: 5.1.1 to HEAD

## [v5.0.3] - 2026-04-01

### ✨ Features
- af35367: feat: implement comprehensive code signing for ZeroMix
- 099ce02: feat: implement industry-standard update process and fix VirtualAssistant issues
- 010869c: feat: optimize WebView2 performance in VirtualAssistant

### 🐛 Bug Fixes
- 7b8edee: fix: robust osslsigncode download and graceful signing fallback
- eab15b3: fix: update osslsigncode download URL and add fallback
- b88133e: fix: prevent recording hangs and crashes in installed applications
- dd9fbee: fix: include zeromix-update.bat in installer and build
- 6272d82: fix: include Virtual_Assisten assets in build and installer

### 🔧 Changes

### 📊 Commit Summary
- Total commits: **10**
- Date range: v5.0.2 to HEAD

## [v5.0.3] - 2026-03-31

### ✨ Features

### 🐛 Bug Fixes

### 🔧 Changes

### 📊 Commit Summary
- Total commits: **1**
- Range: v5.0.3..HEAD
- Generated: Tue Mar 31 11:37:27 UTC 2026

# ZeroMix - Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### ✨ Features (In Development)
- [ ] To be determined

### 🐛 Bug Fixes (In Development)
- [ ] To be determined

### 🚀 Improvements (In Development)
- [ ] To be determined

---

## [1.0.0] - Initial Release

### ✨ Features
- **Desktop Recorder** - Full-screen and window-specific recording
- **GPU-Accelerated Encoding** - Hardware encoding support (NVIDIA NVENC, Intel QuickSync, AMD VCE)
- **Screen Capture** - DXGI-based GPU capture with GDI fallback
- **Cursor Tracking** - Native cursor drawing during recording
- **Audio Support** - Microphone and system audio capture
- **Hotkeys** - Customizable hotkeys for recording control
- **Plugin System** - Extensible plugin architecture with Lua support
- **Virtual Assistant** - AI-powered virtual assistant
- **Wallpaper Support** - Custom wallpaper management
- **Customizable UI** - Themes and layout customization
- **Sleep Mode** - Idle detection and sleep mode
- **Transparent Taskbar** - Custom Windows functionality

### 🐛 Bug Fixes
- Fixed crash when FFmpeg process dies unexpectedly
- Fixed memory leaks in frame processing
- Fixed null reference exceptions in compositor
- Fixed audio device enumeration errors
- Fixed window capture coordinate handling

### 🚀 Improvements
- Optimized frame processing pipeline
- Improved error logging and diagnostics  
- Enhanced resource cleanup on shutdown
- Better performance with high-resolution displays
- Improved GPU fallback handling
- Better exception handling throughout application

### 📚 Documentation
- Added comprehensive README with quick start guide
- Added CHANGELOG following Keep a Changelog format
- Added GitHub Actions automation documentation
- Added contribution guidelines
- Added security policy

---

## Release Process

Each release follows this process:

1. **Code Changes** → Push commits following [Conventional Commits](https://www.conventionalcommits.org/)
2. **Version Bump** → Create annotated git tag: `git tag -a v1.2.0 -m "Release message"`
3. **Push Tag** → `git push origin v1.2.0`
4. **Automation** → GitHub Actions automatically:
   - Builds the project
   - Generates changelog from commits
   - Creates GitHub Release with release notes
   - Updates this CHANGELOG.md
5. **Published** → Release available on GitHub Releases page

---

## Version Naming

ZeroMix uses [Semantic Versioning](https://semver.org/):

- **MAJOR** (1.0.0 → 2.0.0) - Breaking changes
- **MINOR** (1.0.0 → 1.1.0) - New features (backward compatible)
- **PATCH** (1.0.0 → 1.0.1) - Bug fixes (backward compatible)

Examples:
- `v1.0.0` - Initial release
- `v1.1.0` - New features added
- `v1.1.1` - Bug fixes
- `v2.0.0` - Major breaking changes

---

## Commit Message Format

Using [Conventional Commits](https://www.conventionalcommits.org/) helps generate changelogs automatically.

### Format:
```
<type>(<scope>): <subject>
<blank line>
<body>
<blank line>
<footer>
```

### Types:
- **feat** - New feature → Listed in ✨ Features
- **fix** - Bug fix → Listed in 🐛 Bug Fixes  
- **refactor** - Code refactoring → Listed in 🚀 Improvements
- **perf** - Performance improvement → Listed in 🚀 Improvements
- **docs** - Documentation → Listed in 📚 Documentation
- **test** - Test additions (not in changelog)
- **chore** - Build/config changes (not in changelog)
- **ci** - CI/CD changes (not in changelog)

### Examples:

```bash
# Feature
git commit -m "feat(recorder): add batch recording support"

# Bug fix  
git commit -m "fix(encoder): handle FFmpeg crash gracefully"

# Improvement
git commit -m "perf(capture): optimize frame dequeuing"

# Documentation
git commit -m "docs(README): add troubleshooting section"
```

---

## Key Features by Version

### v1.0.0
- ✅ Core screen recording functionality
- ✅ GPU acceleration support
- ✅ Plugin system
- ✅ Virtual assistant

### Future (v1.1+)
- 🔄 Real-time effects and filters
- 🔄 Multi-monitor recording
- 🔄 Cloud storage integration
- 🔄 Streaming support
- 🔄 Advanced audio mixing

---

## Support

- 🐛 [Report Issues](https://github.com/faizinuha/ZeroMix/issues)
- 💬 [Discussions](https://github.com/faizinuha/ZeroMix/discussions)
- 📖 [Wiki & Documentation](https://github.com/faizinuha/ZeroMix/wiki)

---

## Contributing

We welcome contributions! Please see [CONTRIBUTING.md](./CONTRIBUTING.md) for:
- Code of conduct
- Development setup
- Pull request guidelines
- Commit message standards

---

**Last Updated:** March 31, 2026  
**Maintained by:** ZeroMix Team  
**License:** See [LICENSE.txt](./LICENSE.txt)
