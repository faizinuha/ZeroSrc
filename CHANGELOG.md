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
