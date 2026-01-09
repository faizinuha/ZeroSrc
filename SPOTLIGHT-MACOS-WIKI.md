# Welcome to the ZeroMix Wiki

ZeroMix is a high-performance, productivity-centric utility suite designed to refine the Windows desktop experience. This wiki serves as the central hub for documentation, technical specifications, and user guides.

---

## Core Suite Features

ZeroMix provides a comprehensive set of tools designed for performance monitoring, desktop customization, and workflow optimization:

- **Lua Plugin System**: A powerful, approachable scripting engine that allows users to extend ZeroMix functionality using the Lua language.
- **System Architecture Monitoring**: Real-time telemetry for CPU utilization, RAM consumption, and Disk throughput via an integrated dashboard.
- **Ghost Taskbar**: A minimalist aesthetic utility that enables taskbar transparency and blur effects for a modern desktop look.
- **Video Wallpaper Engine**: A high-performance media engine capable of rendering immersive animated backgrounds with minimal resource overhead.
- **Advanced Search Overlay**: The primary interface for rapid navigation, file discovery, and web integration.
- **Smart Memory Optimizer**: Automated background processes for garbage collection and working set trimming to ensure peak system responsiveness.
- **Battery Guard**: A personalized notification system for power management, featuring interactive mascot-driven alerts.
- **Dynamic Weather Sync**: Synchronize your desktop atmosphere with real-time weather data and specialized wallpaper transitions.
- **Custom Global Shortcuts**: User-definable hotkeys to launch specific applications or system paths instantly.
- **Aesthetic Clock Widget**: A minimalist, high-visibility desktop clock designed to enhance workspace aesthetics.
- **Integrated Tray & Core Management**: Centralized control via the system tray for rapid access to performance tools and suite configurations.
- **Automated Update System**: Built-in release tracking to ensure the application suite remains current with the latest features and security patches.

## Spotlight Search Experience

The centerpiece of ZeroMix is the **Search Overlay**, a high-fidelity implementation of the macOS Spotlight experience tailored for Windows. It provides a unified portal for local applications, file indexing, web search, and system commands.

### Core Capabilities

- **Global Access**: Instantly accessible via the `Alt + Space` hotkey (with configurable fallbacks to `Ctrl + Space`).
- **Intelligent Indexing**: Real-time matching for installed applications, frequently accessed files, and system directories.
- **Web Integration**: Direct connection to the Google Suggestion API for quick web queries and predictive search.
- **System Utility**: Integrated support for mathematical calculations, terminal command execution (CMD/PowerShell), and system states (Shutdown, Reboot, Sleep).

### Technical Implementation

ZeroMix leverages modern .NET 9 and native Windows APIs to ensure a seamless, non-intrusive integration:

- **Aero/Acrylic Composition**: Utilizing native P/Invoke calls to `SetWindowCompositionAttribute` for premium transparency and backdrop blur effects.
- **High-Performance UI**: Built on WPF with hardware-accelerated rendering and optimized storyboard animations for 60fps fluidity.
- **Resource Efficiency**: Background memory optimization and lazy-loading of icons using `SHGetFileInfo` to maintain a low system footprint.

### Navigation and Usage

1.  **Invoke**: Press `Alt + Space` (Default).
2.  **Search**: Begin typing any application name, file path, or search query.
3.  **Select**: Use the **Arrow Keys** to navigate through grouped results.
4.  **Execute**: Press **Enter** to launch the selected item, or **Esc** to dismiss the overlay.

---

_For further information on the Lua plugin system or Video Wallpapers, please refer to their respective sections in the sidebar._

---

**ZeroMix Project Team**
[GitHub Repository](https://github.com/faizinuha/ZeroMix) | [Official Releases](https://github.com/faizinuha/ZeroMix/releases)
