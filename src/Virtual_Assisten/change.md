# Changelog Project ZeroMix

---

## 🚀 v7.2.0 — Latest

### 🖥️ Migrasi Rendering: WebView2 → Native OpenGL + Cubism Core
- **WebView2 dihapus total dari flow Virtual Assistant** — render karakter langsung di proses native pakai OpenTK (OpenGL) + Live2D Cubism Core (P/Invoke `Live2DCubismCore.dll`).
- **Tanpa jembatan JS**: ganti model, eye tracking, speak, mic → method C# langsung.
- **Per-pixel alpha** via `WS_EX_LAYERED` + `UpdateLayeredWindow` (FBO readback) — tetap transparan ke desktop.
- **Anti white/black flash**: window tersembunyi (`Opacity=0`) sampai frame pertama OpenGL sukses, lalu fade-in 300ms.
- **TTS/STT native** (`System.Speech`) menggantikan Web Speech API.
- **Drag window** dari area karakter (fitur baru).
- Lip-sync (`ParamMouthOpenY`), idle motion, expression, physics tetap berjalan.

---

## 🚀 v7.5.1

### 🗑️ Removed
- **EdgeDim (Privacy Filter) dihapus total** — Fitur tidak stabil & tidak bisa digunakan sesuai rencana awal.
- `src/EdgeDim/` — seluruh folder dihapus
- `MainWindow.xaml` — tombol EdgeDim dihapus
- `MainWindow.xaml.cs` — semua field, method, hotkey EdgeDim dihapus
- `SettingsService.cs` — properti EdgeDimShortcut dihapus
- `SettingsWindow.xaml/.cs` — UI shortcut capture EdgeDim dihapus

---

## 🚀 v7.5.0

### ⊞ Snap Layout — FancyZones-Style Window Snapping
- **Fitur baru**: Dual-trigger window snapping — drag-to-zone dan keybind overlay (Ctrl+Win+Z).
- **6 Layout Preset**: TwoColumns, ThreeColumns, TwoPlusOne, OnePlusTwo, TwoByTwo, TopBottom.
- **Drag-to-Zone**: WinEvent hook deteksi window drag → zone indicators → lepas mouse di zona untuk snap.
- **Keybind Overlay**: Fullscreen dengan fake transparency → pilih layout → klik zona individual.
- **Per-zone Clicking**: Zone preview di layar bisa diklik langsung.
- **Hotkey Configurable**: Ctrl+Win+Z default, fallback Ctrl+Alt+Z.
- **Multi-Monitor**: Overlay di monitor yang tepat.
- **ZeroShell Command**: `!snap status`, `!snap 2col`, `!snap off`.

### 🔧 Perbaikan Build
- **CS0579 (Duplicate Assembly Attributes)**: Tambah `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` ke `ZeroMix.PluginSDK.csproj` — mencegah konflik auto-generated attributes dari multi-target project.

### 🔧 Changes
- `ZeroMix.PluginSDK.csproj` — fix CS0579 duplicate assembly attributes
- `Tools/Plugins/zeromix.SnapLayout/` — plugin baru (11 files): SnapZone.cs, SnapLayout.cs, SnapLayoutService.cs, SnapOverlayWindow.xaml/.cs, SnapZoneWindow.xaml/.cs, SnapLayoutPlugin.cs, SnapLayoutUI.xaml/.cs, SnapCommands.cs

---

## 🚀 v7.4.1

### 🔧 ZeroShell — Settings Disederhanakan
- **Hapus SHELL section**: `ShellTypeCombo` (pwsh/legacy/CMD/WSL) dihapus dari settings — tidak perlu pilih shell.
- **Hapus THEME section**: `ThemePicker` cards + 5 tema (Default, Compact, Retro Green, Glass, macOS) dihapus — **cuma 1 tema Default** biar simpel.
- **Font pilihan macOS/terminal**: Font combo tidak lagi nampilin semua system fonts — cuma 6 font berkualitas: *Menlo, SF Mono, Cascadia Mono, JetBrains Mono, Fira Code, Consolas*.
- **Dead code cleanup**: Hapus `ThemeCardData`, `_sessionStartTime`, `_tabOriginal`, 6 utility methods (`RunWifiScan`, `RunNetworkInfo`, `RunBatteryInfo`, `RunDiskInfo`, `RunAppsList`, `RunStartupList`), dan `AutocompleteViewModel.cs`.

### 🐛 ZeroShell — Auto-Suggestion Fix
- **Fix suggestions nutupin input**: `SuggestionsListBox` dipindah dari dalam input bar (ke-clip height 44px) ke **floating overlay** di atas input bar.
- **Fix suggestions transparan**: Background `#E8` → `#FF` (solid, tidak tembus terminal).
- **Fix text tidak clear saat Enter**: `Text=""` dan `e.Handled=true` di-set **sebelum** `await` — text langsung hilang saat Enter ditekan.

### ✨ Search — Google Lens Improvement
- **Auto-upload ke temp hosting**: Upload gambar ke 0x0.st / tmp.ninja sebelum buka Google Lens.
- **Validasi ukuran file**: Max 10MB.
- **Fallback clipboard**: Jika upload gagal, path file di-copy ke clipboard.

### 🔧 GroqAIService — Character AI Prompts
- **CharacterPrompts dictionary**: Prompt personality untuk Frieren, Fern, Huohuo.
- **ChatAsync baru**: Method dengan persona support untuk character-based chat.

### 🐛 Bug Fixes
- Fix `App.xaml.cs` — `HotkeyCoreInstance.Show()` dipanggil agar message pump aktif untuk WM_HOTKEY.

---

## 🚀 v7.3.0

### ✨ Features
- **AI Companions — Inline Chat Panel**: Panel chat (TextBox + Send button) yang toggle via tombol 💬. Chat history 10 pesan terakhir dikirim sebagai context ke AI (WaifuChatService/OpenRouter).
- **AI Companions — Character Switching**: Ganti character Live2D (Frieren/Fern/Huohuo) + personality AI langsung dari context menu. Greeting dinamis per character.
- **WaifuChatService — History Context**: ChatAsync() support parameter history opsional + method BuildMessages() untuk compose array messages dengan history.
- **GroqAIService — ChatAsync**: Method baru untuk character-based chat dengan 3 personality prompt + history context.

### 🔧 Changes
- **AI Companions**: GroqAIService di-rollback dari AI Companions — tetap khusus SearchOverlay. Provider toggle (OpenRouter/Groq) dihapus dari context menu.
- **AI Companions**: AiVisionService tetap auto observe layar tiap 60 detik via Groq.
- **Live2D WebView2**: Hapus SRI integrity hash palsu dari CDN script pixi.js.

### 🐛 Bug Fixes
- Fix Live2D WebView2 crash — SRI integrity hash palsu di pixi.js menyebabkan WebView2 reject script.

---

## 🚀 v5.5.0

### ✨ Features
- **Charger Notif Plugin** (`ChargerBatterynotif.core`): Plugin baru khusus event charger — notifikasi animasi Lottie + suara saat charger dicolok, dicabut, dan baterai penuh.
- **Lottie Animation**: Integrasi `LottieSharp` untuk animasi JSON di notifikasi. Fallback ke PNG maskot `zeromix.Battery/Maskot/` jika Lottie tidak ada.
- **Custom Sound per Event**: User pilih `.wav` sendiri via Browse per event (Charging/Unplug/Full). Built-in WAV bawaan. Tombol ▶ preview, ✕ reset.
- **Bahasa Korea (ko-KR)**: Locale baru — total 5 bahasa: EN, ID, JP, ZH, KR.
- **Language Switch Tanpa Reload**: Ganti bahasa langsung apply ke UI, sidebar nav labels diupdate programatik.
- **Virtual Assistant — Fast Model Loading**: Guard concurrent load, pause GC 80ms antar model, tidak lagi not responding saat klik ACTIVATE.
- **Virtual Assistant — Idle Animations Berjalan**: `playBodyMotion()` baru panggil `model.motion("", idx)` — Frieren 2 motions, Fern 1, Huohuo 7. Expression cycle 5–10 detik, body motion 8–15 detik. Langsung play 300ms setelah loaded.
- **Video Wallpaper Optimization**: FFmpeg encode background async (1280×720, CRF 28, veryfast). Play langsung dengan file asli, swap seamless ke file teroptimasi. Cache di `%AppData%\ZeroMix\WallpaperCache\`.

### 🐛 Bug Fixes
- Fix Virtual Assistant model tidak load — `SetCharacter` dipanggil sebelum WebView navigation selesai.
- Fix idle animation tidak berjalan — `startAutoMotion()` sekarang dipanggil setelah model loaded.
- Fix `--disable-gpu-vsync` dan `--disable-plugins` merusak Live2D rendering pada beberapa GPU.
- Fix language selector trigger dua kali saat init.
- Fix `Loader cat.json` duplikat root JSON object.
- Fix video wallpaper berat tanpa optimasi.

### 🔧 Changes
- `ChargerBatterynotif.core` hanya handle Charging, Unplugged, Full. Low/Critical tetap di `zeromix.Battery`.
- `live2d-viewer.html` di-rewrite: loading guard, GC pause, auto-motion loop, body motion per karakter.
- Virtual Assistant WebView: `powerPreference: "low-power"`, resolusi cap 1.5x, hapus args yang merusak GPU.
- Language ComboBox: tambah 中文 dan 한국어.

---

## 🚀 v5.4.0 — Previous

### ✨ Features
- **WDM Start Menu Glass**: Efek dark acrylic blur pada Start Menu (Windows 10 & 11).
- **WDM Notification Panel Glass**: Efek dark acrylic blur pada Action Center / Notification Panel.
- **WDM Persistence**: State WDM disimpan ke `wdm.json`, di-restore otomatis saat ZeroShell dibuka kembali.
- **Wallpaper Session**: State video wallpaper disimpan ke `wallpaper_session.json`, auto-restore saat app dibuka.

### 🐛 Bug Fixes
- Fix File Explorer glass — hapus font injection yang menyebabkan font bertolak belakang.
- Fix build error `CS1513` di `ShellHelper.cs` — lambda `EnumWindows` tidak tertutup.
- Fix build error `CS1028` di `ZeroShellWindow.xaml.cs` — `#endregion` ganda.
- Fix Start Menu glass hanya apply ke parent, sekarang apply ke semua child windows.
- Fix Notification Panel tidak berubah — tambah `ControlCenterWindow` class untuk Windows 11.
- Fix "Failed to save language preference" — `language.ini` dipindah ke `%AppData%\ZeroMix\`.
- Fix `Mutex.ReleaseMutex()` crash saat shutdown — track `_mutexOwned` agar hanya release 
- Fix app langsung exit saat `dotnet run` — karena proses lama masih jalan (single instance guard).

### 🔧 Changes
- `ApplyBlur` direfactor — tiap elemen punya intensitas blur berbeda (Taskbar `0x99`, Start Menu/Notif `0x66`, Explorer `0x44`).
- `ApplyCrystalBlur` dihapus, semua pakai satu fungsi `ApplyBlur`.
- `ACCENT_ENABLE_ACRYLICBLURBEHIND` → `ACCENT_ENABLE_BLURBEHIND` untuk hasil blur netral tidak ikut warna wallpaper.
- WDM pulse timer diperluas cover semua 4 elemen, interval 2 detik.
- WDM section dihapus dari gear settings — hanya bisa diakses via `!wdm`.
- `!wdm` menu diperluas dari 5 opsi menjadi 7 opsi.
- `ApplyBlurToChildren` baru — apply blur ke semua child window agar bagian dalam ikut berubah.
- Setup.iss: exclude video wallpaper, kurangi image wallpaper, hapus duplicate logo dari installer.
- `language.ini` dibaca dari AppData dulu, fallback ke BaseDirectory.
- Plugins & About view: fix responsive layout — tidak overflow ke kanan saat window dikecilkan.

---

## 🚀 v5.3.1 — Previous -> Lates

### ✨ Features
- **Sleep Mode Settings Panel**: Settings dipindah ke panel navigasi dalam MainWindow — konsep seperti Settings Windows 11.
- **Sleep Mode Music**: Opsi musik opsional (MP3/WAV/FLAC/OGG/M4A) dengan volume slider.
- **Sleep Mode No Delay**: Custom background (gambar/video) langsung tampil tanpa delay.

### 🐛 Bug Fixes
- Fix ParserError di ZeroShell prompt — ganti `-Command` ke `-EncodedCommand` (Base64).
- Fix ZeroShell crash saat klik gear — `TerminalSettingsWindow` dihapus, kembali ke `SettingsOverlay` inline.
- Fix Terminal Customizer tidak bisa scroll — tambah `ScrollViewer` di konten settings.
- Fix tombol BROWSE Terminal Wallpaper tidak bisa diklik — tambah `IsHitTestVisible="False"` pada TextBlock.
- Fix Search Overlay kotak-kotak setelah penggunaan ke-2.
- Fix SleepMode custom background tidak tersimpan.
- Fix ZeroMix.recorder video output hitam.
- Fix video wallpaper tidak bisa diputar.
- Fix uninstaller tidak hapus registry dan startup shortcut.
- Fix window list recorder tidak lengkap — explorer tidak di-skip, icon fetch dipisah.

### 🔧 Changes
- Hapus `SleepSettingsWindow.xaml/.cs`, diganti panel `SleepContent` di MainWindow.
- Hapus `TerminalSettingsWindow.xaml/.cs`, kembali pakai `SettingsOverlay` inline di ZeroShellWindow.
- Virtual Assistant: hapus tombol mic dari UI overlay.
- Virtual Assistant: eye tracking 200ms → 300ms, vision timer 30s → 60s.
- Wallpapers: video langsung play tanpa pre-process ffmpeg.
- Uninstaller: auto kill, hapus registry, startup shortcut, LocalAppData.
- Window Picker: ukuran minimum 100px → 50px, window minimize tidak di-skip.

---

## 🚀 v5.2.9 — Lates

### ✨ Planned Features
- **Multi-Monitor Support**: Rekam atau capture layar dari monitor lebih dari satu sekaligus.
- **Streaming Mode**: Dukungan output langsung ke platform streaming (OBS-compatible).
- **Advanced Audio Mixer**: Kontrol volume per-source (mic, system, game) secara terpisah.
- **Plugin Marketplace**: Browser plugin langsung dari dalam aplikasi.
- **Cloud Sync Settings**: Sinkronisasi konfigurasi antar perangkat via cloud.

---

## ✅ v5.2.5 — Latest

### ✨ Features
- **Sleep Mode Music**: Opsi musik opsional (MP3/WAV/FLAC/OGG/M4A) — bisa dipakai bareng background apapun, ada volume slider.
- **Sleep Mode No Delay**: Custom background (gambar/video) langsung tampil tanpa delay.

### 🐛 Bug Fixes
- Fix Search Overlay kotak-kotak setelah penggunaan ke-2 (root cause: window di-reuse, sekarang selalu buat instance baru).
- Fix SleepMode custom background tidak tersimpan saat settings dibuka ulang.
- Fix ZeroMix.recorder video output hitam (tambah -vsync cfr flag ke FFmpeg).
- Fix video wallpaper tidak bisa diputar — error handling diperbaiki.
- Fix uninstaller tidak hapus registry context menu dan startup shortcut.

### 🔧 Changes
- Virtual Assistant: hapus tombol mic dari UI overlay (tetap bisa diakses dari halaman list model).
- Virtual Assistant: eye tracking 200ms → 300ms, vision timer 30s → 60s (hemat CPU/RAM ~10-15MB).
- Wallpapers: hapus pre-process ffmpeg yang tidak perlu, video langsung play.
- Uninstaller: auto kill ZeroMix.exe, hapus registry, startup shortcut, LocalAppData.
- Sleep Mode: BitmapImage di-Freeze() setelah load, DispatcherTimer pakai Background priority.

---

## ✅ v5.2.3 — Latest

### ✨ Features
- **AOD Style Picker**: Sleep Mode kini punya 5 style Always-On Display — Minimal Clock, Neon Glow, Analog, Date Focus, Blank.
- **Analog Clock AOD**: Jarum jam real-time dengan anti burn-in (posisi geser otomatis tiap 10 detik).
- **Sidebar Update Badge**: Dot merah pulsing di icon System Info saat ada update tersedia.
- **Auto Silent Update Check**: Cek update otomatis saat startup tanpa popup.

### 🐛 Bug Fixes
- Fix double instance — klik icon desktop saat app sudah jalan di tray tidak lagi buka instance baru.
- Fix Search Overlay (Ctrl+Space) lambat — overlay sekarang di-preload saat startup.
- Fix "Show Terminal" masih muncul di tray menu.
- Fix installer popup "applications using files" — ZeroMix di-close otomatis sebelum update.
- Fix Sleep Mode overlay crash saat laptop wake up dari sleep/hibernate (PowerModeChanged event).

### 🔧 Changes
- Sleep Mode settings: section Visuals diganti jadi AOD style card picker.
- Brightness slider sekarang tampilkan persentase langsung.
- CHANGELOG auto-generate dari git commits + file yang berubah via release script.
- Release script: ForceBuild baca `-CommitMessage` parameter langsung untuk feat/fix.

---

## ✅ v5.2.2 — Published

### ✨ Features
- **Parallel Chunked Download**: Updater script pakai 8 koneksi paralel (IDM-style).
- **Progress Bar Updater**: Tampilkan speed (KB/s) dan ETA saat download update.

### 🐛 Bug Fixes
- Fix download lambat di zeromix-update.ps1 — ganti Invoke-WebRequest ke HttpWebRequest dengan buffer 128KB.
- Fix path Downloads yang salah di beberapa sistem.
- Fix installer popup minta close app sebelum update.

### 🔧 Changes
- README ditambah versi Indonesia (README.id.md).

---

## ✅ v5.2.1 — Published

### ✨ Features
- Publish SDK ke NuGet.org (`ZeroMix.PluginSDK`).
- Add PluginSDK dan refactor `IZeroMixHost`.

### 🐛 Bug Fixes
- Fix ZeroMix.Recorder crash pada beberapa konfigurasi GPU.
- Fix Virtual Assistant thumbnail tidak muncul.
- Fix Bitrate control slider untuk pilih kualitas recording.

---

## 🚀 v5.2.0 — Upcoming

### ✨ Planned Features
- **Multi-Monitor Support**: Rekam atau capture layar dari monitor lebih dari satu sekaligus.
- **Streaming Mode**: Dukungan output langsung ke platform streaming (OBS-compatible).
- **Advanced Audio Mixer**: Kontrol volume per-source (mic, system, game) secara terpisah.
- **Plugin Marketplace**: Browser plugin langsung dari dalam aplikasi.
- **Cloud Sync Settings**: Sinkronisasi konfigurasi antar perangkat via cloud.

---

## ✅ v5.1.9 — Published

### ✨ Features
- **Game Mode Translator**: Mode terjemahan khusus game via Clipboard Paste (Ctrl+V).
- **CI/CD Sign Fix**: Perbaikan pipeline signing executable di GitHub Actions.

### 🐛 Bug Fixes
- Fix `osslsigncode` tidak dikenali setelah `choco install` pada Windows runner.
- Fallback ke path hardcoded `C:\ProgramData\chocolatey\bin\`.

---

## ✅ v5.1.5 — Published

### ✨ Features
- **Ask AI Mode Overlay**: Tombol "Ask AI" di Search Box.
- **Smart Intent Detection**: carikan foto, beli barang, shopee → otomatis buka.
- **Text-to-Speech (TTS)**: Frieren/Fern/HuoHuo bisa bicara.
- **Lip-Sync Animation**: Gerakan mulut sinkron dengan suara.

### 🐛 Bug Fixes
- Fix Force Close saat tombol rekam diklik pada beberapa GPU.
- Fix Temp Cleanup lebih stabil.

---

## 🔧 v5.1.3 — Fix Release

### 🐛 Bug Fixes
- Fix crash recorder pada GPU tertentu.
- Fix memory leak pada engine Live2D.
- Fix pembersihan `%temp%` yang tidak lengkap.
- Fix encoder fallback yang menyebabkan hang saat NVENC tidak tersedia.

---

## ✅ v5.1.2 — Standard Industrial

### ✨ Features
- AI Control & Search Assistant (dasar).
- Virtual Assistant upgrade awal (TTS & Lip-Sync prototype).
- Recorder Engine stability improvements.
- Dashboard & System Health optimization.

---

*ZeroMix - Smart Desktop Launcher*
