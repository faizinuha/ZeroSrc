# Contributing to ZeroMix

First off, thank you for taking the time to contribute! ZeroMix is an open-source project, and contributions from the community are what make it a great tool for everyone.

Here is a set of guidelines and instructions to help you contribute effectively.

---

## Table of Contents
1. [Code of Conduct](#code-of-conduct)
2. [How Can I Contribute?](#how-can-i-contribute)
   - [Reporting Bugs](#reporting-bugs)
   - [Suggesting Features](#suggesting-features)
   - [Developing Plugins (Lua)](#developing-plugins-lua)
   - [Contributing Code (C# / WPF)](#contributing-code-c-wpf)
3. [Local Development Setup](#local-development-setup)
4. [Coding Standards](#coding-standards)
5. [Commit Message Guidelines](#commit-message-guidelines)
6. [Pull Request Process](#pull-request-process)

---

## Code of Conduct

By participating in this project, you agree to abide by our Code of Conduct. Please read [Docs/code-of-conduct.md](Docs/code-of-conduct.md) before interacting with the repository or other contributors.

---

## How Can I Contribute?

### Reporting Bugs
If you find a bug, please check the [existing Issues](https://github.com/faizinuha/ZeroMix/issues) first to see if it has already been reported. If not, open a new issue and include:
- A clear, descriptive title.
- Steps to reproduce the bug.
- Expected vs. actual behavior.
- Screenshots, video recordings, or error logs if applicable.
- Your Windows version.

### Suggesting Features
Have an idea to make ZeroMix better? Feel free to open a thread in the [GitHub Discussions](https://github.com/faizinuha/ZeroMix/discussions) page under the "Ideas" category, or submit a feature request issue. Please describe the feature, why it is useful, and how it should work.

### Developing Plugins (Lua)
ZeroMix allows extending its functionalities via Lua plugins.
- Refer to the [ZeroMix Plugin Development Guide](Docs/PLUGIN_GUIDE.md) for APIs, folder structure, and examples.
- Test your plugin locally by placing it in `C:\Program Files\ZeroMix\Plugins\` or in your development build directory under `Tools/Plugins/`.
- If you'd like your plugin to be built-in or featured, open a Pull Request adding it to the `Tools/Plugins/` directory.

### Contributing Code (C# / WPF)
If you want to contribute C# code, fix bugs, or add features:
1. Fork the repository and create a new branch from `main`.
2. Follow the [Local Development Setup](#local-development-setup) instructions below.
3. Make your changes and verify them.
4. Submit a Pull Request.

---

## Local Development Setup

### Prerequisites
To build and run ZeroMix from source, you will need:
- **Windows 10 / 11** (ZeroMix uses WPF and Windows-specific APIs)
- **.NET 9.0 SDK** or later
- **Visual Studio 2022** (with the *.NET Desktop Development* workload) or **VS Code** (with C# Dev Kit extension)
- **Inno Setup** (only if you want to build the installer locally)

### Steps
1. **Clone the repository:**
   ```bash
   git clone https://github.com/faizinuha/ZeroMix.git
   cd ZeroMix
   ```
2. **Restore dependencies:**
   ```bash
   dotnet restore
   ```
3. **Build the solution:**
   ```bash
   dotnet build
   ```
4. **Run the application:**
   ```bash
   dotnet run --project ZeroMix.csproj
   ```

---

## Coding Standards

### C# Guidelines
- Follow standard C# naming conventions:
  - **PascalCase** for classes, methods, properties, and events (e.g., `MainWindow`, `GetCpuUsage`).
  - **camelCase** for local variables and method parameters (e.g., `baseDir`, `installedVer`).
  - **_camelCase** (with leading underscore) for private member fields (e.g., `_httpClient`).
- Always enable and respect C# nullable reference types (`#nullable enable`).
- Use asynchronous programming (`async`/`await`) for any I/O, network requests, or long-running tasks to keep the UI responsive.
- Wrap disposable resources (like `HttpClient`, streams, etc.) in `using` statements or `try-finally` blocks.

### WPF / XAML Guidelines
- Keep your XAML files neat and properly indented.
- Rely on shared styles in `src/Styles/` or `WPF-UI` theme dictionaries rather than hardcoding colors and margins on individual controls.
- Ensure all interactive elements have unique, descriptive `x:Name` or `Uid` attributes for testing and identification.

---

## Commit Message Guidelines

We follow the **Conventional Commits** specification for commit messages. This helps us generate clean changelogs automatically.

Format: `<type>(<scope>): <description>`

Common types:
- `feat`: A new feature
- `fix`: A bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting, missing semi-colons, etc.; no production code changes)
- `refactor`: A code change that neither fixes a bug nor adds a feature
- `perf`: A code change that improves performance
- `test`: Adding missing tests or correcting existing tests
- `chore`: Changes to the build process, auxiliary tools, or libraries (e.g., package updates)

Example:
```text
feat(ui): add modern glassmorphism effect to search overlay
fix(updater): resolve memory leak in HttpClient parallel downloader
```

---

## Pull Request Process

1. **Keep it focused:** Try to make your PR address a single issue or feature.
2. **Verify changes:** Ensure your changes build without warnings or errors.
3. **Update documentation:** If you added a feature, configuration, or API, update the appropriate markdown documentation in `Docs/` or `Readme.md`.
4. **Link Issues:** Reference any related issues in the PR description (e.g., `Closes #123`).
5. **Request review:** Once submitted, wait for a repository maintainer to review and approve your PR.

Thank you for helping make ZeroMix awesome! 🚀
