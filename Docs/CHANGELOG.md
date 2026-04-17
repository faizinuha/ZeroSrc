# ZeroMix - Changelog

All notable changes to this project will be documented in this file.
Format: [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) | [Semantic Versioning](https://semver.org/)

---

## [v5.8.0] - 2026-04-17

### ✨ Features

- **Collapsible Plugin UI Design**: Semua plugin kini menggunakan desain collapsible yang konsisten dengan header kompak dan panel settings yang bisa dibuka/tutup. Menghemat ruang UI dan memberikan pengalaman yang lebih bersih.
- **Welcome Plugin Redesign**: Interface Welcome plugin dibuat collapsible dengan header yang menampilkan status mode dan uptime sistem. Tombol test untuk preview Welcome screen dan pengaturan mode yang lebih intuitif.
- **Nexus Translator Feature Toggles**: Tambah tombol gear dan sistem toggle individual untuk 3 fitur utama: Keyboard Translate, Game Mode, dan Drag Bubble. Setiap fitur memiliki card tersendiri dengan icon dan deskripsi yang jelas.
- **Welcome Window Enhancement**: Redesign complete Welcome Window dengan gradient background (bukan hitam), display Welcome.gif dengan fallback handling, greeting dinamis berdasarkan waktu, dan countdown timer otomatis.
- **Drag-to-Translate Bubble**: Ganti mekanisme Bubble dari polling Ctrl+C ke global mouse hook — cukup drag/highlight teks, bubble terjemahan muncul otomatis saat mouse dilepas tanpa perlu tekan Ctrl+C. Jauh lebih cepat dan responsif.
- **Charger Notif UI Redesign**: Tampilan settings Charger Notif dirombak total — gradient accent bar per section, message fields dengan bordered container, tombol Browse/Play/Reset dengan hover state, gradient Save button.
- **Battery Assistant UI Redesign**: Tampilan settings Battery Assistant dirombak total — greeting cards 2x2 color-coded per waktu (pagi/siang/sore/malam), battery warning section dengan warna oranye/merah, gradient progress bar dan Save button.
- **ComboBox Dark Theme Fix**: ComboBox FROM/TO di Nexus Translator kini menggunakan full ControlTemplate override sehingga dropdown tidak lagi berwarna putih.

### 🐛 Bug Fixes

- **Fix OCR Snip Hotkey Tidak Respons**: Perbaikan bug di mana Shift → ESC → Shift tidak memicu capture ulang. Implementasi rising-edge detection dan tunggu Shift dilepas sebelum re-arm hotkey.
- **Fix Build Compilation Errors**: Resolved syntax errors dan Color ambiguity issues yang mencegah successful build. Fixed extra closing braces dan namespace conflicts.
- **Fix RotateTransform Missing**: Added proper using statements untuk System.Windows.Media di plugin code-behind files untuk mengatasi RotateTransform errors.
- **Fix Plugin UI Thread Safety**: Improved thread safety untuk semua plugin UI updates menggunakan Dispatcher.Invoke pattern yang konsisten.
- **Fix NativeMethods.POINT Type Mismatch**: Resolved CS0029/CS1503 build error di SelectionBubble.cs akibat implicit conversion dari `NativeMethods.POINT` ke `System.Drawing.Point`.
- **Fix Duplicate BubbleModeSwitch**: Resolved CS0102 build error akibat nama control duplikat di TranslatePluginUI.xaml setelah refactor OCR Snip.

### 🔧 Changes

- **Hapus OCR Snip**: Fitur OCR Snip dihapus dari Nexus Translator karena tidak stabil dan sering error. Feature count diupdate dari 4 → 3.
- **Bubble: Ctrl+C → Drag Auto-Copy**: SelectionBubble kini menggunakan global `WH_MOUSE_LL` hook. Saat drag selesai, Ctrl+C dikirim otomatis via `SendInput`, clipboard diambil, lalu langsung ditranslate — tidak ada polling 500ms lagi.
- **Consistent Design Pattern**: Semua plugin (Welcome, Nexus Translator, Charger Notif, Battery Assistant) kini mengikuti design pattern yang sama dengan collapsible interface, smooth animations, dan visual consistency.
- **Feature Management System**: User sekarang bisa mengaktifkan/nonaktifkan fitur translator secara individual melalui checkbox toggles dengan live status indicators dan feature counter.
- **Code Quality Improvements**: Better error handling, proper resource disposal, dan improved namespace management across all plugin components.
- **Thread Safety Enhancements**: Proper dispatcher usage untuk UI updates, safer hotkey detection, dan improved cleanup mechanisms untuk prevent crashes.

---

## [v5.7.0] - 2026-04-14

### ✨ Features

- **OCR Snip Feature** (`zeromix.Translate`): Screen capture + OCR + Translation dalam satu fitur. User bisa drag-select area layar untuk extract text dari gambar/video, lalu otomatis diterjemahkan. Berguna dan multifungsi untuk konten visual apapun.
- **Dual OCR Engine Support**: OCR.space API dengan fallback engine (Engine 2 → Engine 1) untuk akurasi maksimal text extraction.
- **Visual Selection Overlay**: Full-screen overlay dengan crosshair cursor dan visual feedback saat memilih area capture.
- **OCR Result Window**: Popup window yang menampilkan extracted text dan hasil terjemahan dalam UI yang clean dan readable.
- **Selection Bubble Enhancement**: Perbaikan namespace conflicts yang menyebabkan build errors pada fitur highlight + Ctrl+C.

### 🐛 Bug Fixes

- Fix namespace conflicts di `SelectionBubble.cs` — ambiguous references antara `System.Windows.Forms` dan `System.Windows` namespace.
- Fix namespace conflicts di `OcrSnip.cs` — ambiguous references untuk `TextBox`, `Button`, `Cursors`, `MouseEventArgs`, dan `KeyEventArgs`.
- Fix `Clipboard` ambiguous reference — gunakan fully qualified `System.Windows.Clipboard` untuk konsistensi WPF.
- Fix build errors yang mencegah kompilasi translate plugin — semua namespace conflicts resolved.

### 🔧 Changes

- **OCR Snip UI Integration**: Tambah tombol "📷 SNIP" di translate plugin UI dengan deskripsi yang jelas.
- **Language Settings Sync**: OCR Snip otomatis menggunakan source/target language yang sama dengan translate engine.
- **Error Handling & Logging**: Comprehensive error handling dengan pesan yang informatif di live terminal log.
- **Resource Management**: Proper disposal pattern untuk screen overlay dan OCR components.
- **API Optimization**: Base64 image conversion dan HTTP client dengan timeout 30 detik untuk stabilitas.

---

## [v5.6.0] - 2026-04-12

### ✨ Features

- **Welcome Screen Plugin** (`zeromix.Welcome`): Muncul otomatis saat fresh boot/restart seperti macOS. Deteksi via `Environment.TickCount64` — tidak muncul saat Windows+L (lock screen). Auto-close 5 detik, klik untuk dismiss, greeting berubah sesuai waktu.
- **Huohuo Texture Optimization**: Resize texture dari ~46MB → ~4MB (8192px → 2048px). Load time drastis berkurang, tidak crash di spek rendah.
- **Fern Texture Optimization**: Resize texture dari ~16MB → ~4MB (4096px → 1024px).
- **GIF Animation Support** (`WpfAnimatedGif`): Ganti LottieSharp yang crash → GIF animasi di ChargerNotif. Taruh `Welcome.gif` / `Loader cat.gif` di folder `gif/`.
- **Release Notes Ringkas**: GitHub Release hanya tampilkan 4 highlight teratas + link ke `Docs/CHANGELOG.md`.
- **Makefile Auto-Version**: Versi otomatis dibaca dari `Docs/CHANGELOG.md`, tidak perlu update manual.

### 🐛 Bug Fixes

- Fix idle animation hanya jalan saat alt+tab — `app.ticker.stop()` diganti throttle 8 FPS saat blur.
- Fix `isMotionPlaying` flag stuck — pisah jadi `isMotionPlaying` (expression) dan `isBodyMotionPlaying` (body motion), masing-masing punya timeout sendiri.
- Fix watermark/credit text Fern muncul di tengah model — perluas hide logic ke keyword `credit`, `watermark`, `logo`, `copy`, juga via drawable IDs.
- Fix tombol ID (LanguageBtn) muncul di atas model Virtual Assistant — dihapus dari XAML dan code-behind.
- Fix `CoreWebView2 disposed` crash saat window ditutup — tambah flag `_isWebViewDisposed`, cek sebelum `TrySuspendAsync()` dan `Resume()`.
- Fix `_visionTimer` dan `_autoTalkTimer` start di constructor sebelum WebView siap — pindah ke setelah `NavigationCompleted`.
- Fix `LottieSharp NullReferenceException` di `KeyPath.IsContainer` — ganti ke GIF via `WpfAnimatedGif`.
- Fix `LottieAnimationView` crash empty string — hapus `FileName=""` dari XAML, set hanya dari code setelah path valid.
- Fix Plugin ChargerNotif test Not Responding — `SoundPlayer.Play()` pindah ke `Task.Run()` background thread.
- Fix `NETSDK1022 Duplicate Compile/Page items` — hapus manual `<Compile>` dan `<Page>` dari csproj.
- Fix `CS0579 Duplicate TargetFrameworkAttribute` di `dotnet watch` — tambah `GenerateTargetFrameworkAttribute=false`.
- Fix `src` folder muncul di install directory — semua `<Content>` pakai `<Link>` dengan path yang benar.
- Fix source `.cs`/`.xaml` ikut ke `Tools\Plugins\` di install dir — csproj hanya copy `.png`, `.wav`, `.gif`, `Styles.xaml`.
- Fix `Privacy.txt` dan `Readme.md` ikut ke install directory — dihapus dari `Setup.iss [Files]`.
- Fix `ResizeTexture` tool ikut ter-compile sebagai entry point — folder dihapus setelah dipakai.

### 🔧 Changes

- **Ganti Lottie → GIF**: hapus `LottieSharp`, hapus folder `json/`, tambah `WpfAnimatedGif 2.0.2`.
- Virtual Assistant PIXI: `resolution: 1`, `autoDensity: false`, FPS 24 (Huohuo 18 FPS), ticker throttle 8 FPS saat blur (tidak stop total).
- Virtual Assistant: hapus tombol Language (ID/EN/JP) dari overlay UI.
- `live2d-viewer.html`: loading guard `isLoadingModel`, GC cleanup agresif (`destroyTextureCache`), auto-motion loop dengan flag terpisah, reset flags saat ganti model.
- `SetCharacter()`: stop eye tracking timer saat ganti model, resume setelah model dikirim.
- `Setup.iss`: tambah bahasa Korea (`Korean.isl`), `language.ini` disimpan ke `%AppData%\ZeroMix\`, hapus WebView2 check, fix uninstall buka `Feedback.html`.
- `Makefile`: auto-detect versi dari CHANGELOG, hapus `BUILD_DIR` yang tidak dipakai, tambah target `version` dan `help`.
- GitHub Actions: release notes 4 highlight + link CHANGELOG (bukan dump full changelog).
- `csproj`: tambah `GenerateAssemblyInfo=false` dan `GenerateTargetFrameworkAttribute=false` untuk fix `dotnet watch` bug.

---

## [v5.5.0] - 2026-04-11 

### ✨ Features

- **Charger Notif Plugin** (`ChargerBatterynotif.core`): Plugin baru khusus event charger — notifikasi + Lottie animation + suara saat charger dicolok, dicabut, dan baterai penuh 100%.
- **Lottie Animation Support**: Integrasi `LottieSharp` NuGet untuk animasi JSON di notifikasi charger. Fallback otomatis ke PNG maskot dari `zeromix.Battery/Maskot/` jika Lottie tidak tersedia.
- **Custom Sound per Event**: User bisa pilih file `.wav` sendiri via tombol Browse untuk tiap event (Charging, Unplug, Full). Built-in WAV bawaan tersedia sebagai default. Reset ke bawaan dengan tombol ✕.
- **Sound Preview**: Tombol ▶ untuk preview suara langsung dari settings panel tanpa perlu trigger event sungguhan.
- **Config Persistence**: Pilihan pesan dan sound disimpan ke `%AppData%\ZeroMix\charger_notif_config.json`, tidak hilang saat restart.
- **Bahasa Korea (ko-KR)**: Tambah file locale `ko-KR.xaml` — ZeroMix kini mendukung 5 bahasa: English, Indonesia, 日本語, 中文, 한국어.
- **Language Switching Tanpa Reload**: Ganti bahasa langsung apply ke UI tanpa restart aplikasi. Sidebar nav labels diupdate secara programatik via `ApplyLanguageToStaticElements`.
- **Language Selector Fix**: `InitializeLanguageSelector` sekarang baca dari AppData terlebih dahulu, suppress `SelectionChanged` saat init agar tidak trigger dua kali.
- **Virtual Assistant — Fast Model Loading**: Guard `isLoadingModel` mencegah concurrent load yang menyebabkan not responding. Pause 80ms setelah destroy model lama agar GC bersih sebelum load berikutnya.
- **Virtual Assistant — Idle Animations**: `playBodyMotion()` baru yang memanggil `model.motion("", idx)` sesuai model — Frieren (2 motions), Fern (1), Huohuo (7). Body motion cycle setiap 8–15 detik, expression cycle setiap 5–10 detik. Model langsung play idle 300ms setelah loaded.
- **Video Wallpaper Optimization**: Video besar di-encode ulang di background via FFmpeg (1280×720, CRF 28, preset veryfast). Playback langsung mulai dengan file asli, swap seamless ke file teroptimasi setelah selesai. Cache disimpan di `%AppData%\ZeroMix\WallpaperCache\`.

### 🐛 Bug Fixes

- Fix `LanguageComboBox_SelectionChanged` menampilkan MessageBox error yang tidak perlu saat save language.
- Fix language preference disimpan ke BaseDirectory (read-only) — sekarang ke AppData.
- Fix Virtual Assistant model tidak load saat tombol ACTIVATE diklik — `SetCharacter` dipanggil sebelum WebView navigation selesai. Sekarang tunggu `NavigationCompleted` event.
- Fix idle animation tidak berjalan di Virtual Assistant — `startAutoMotion()` sekarang dipanggil setelah model loaded, bukan saat init.
- Fix `--disable-gpu-vsync` dan `--disable-plugins` di WebView args yang menyebabkan Live2D rendering rusak pada beberapa GPU.
- Fix video wallpaper berat tanpa optimasi — sekarang optimize async di background tanpa block UI.
- Fix `Loader cat.json` duplikat root JSON object — file dipotong ke 6374 baris yang valid.
- Fix `LottieAnimationView` crash `Unable to parse composition` — `Loader cat.json` tidak kompatibel dengan LottieSharp 1.1.0 (`NullReferenceException` di `KeyPath.IsContainer`). Solusi: ganti ke GIF via `WpfAnimatedGif`.
- Fix `LottieAnimationView` crash `empty string path` — `FileName=""` di XAML trigger callback sebelum path valid. Solusi: hapus `FileName` dari XAML, set hanya dari code.
- Fix `CoreWebView2 members cannot be accessed after WebView2 is disposed` — `TrySuspendAsync()` dipanggil setelah window ditutup. Fix: tambah flag `_isWebViewDisposed` manual.
- Fix `_visionTimer` dan `_autoTalkTimer` start di constructor sebelum WebView siap — pindah start ke setelah `NavigationCompleted`.
- Fix Plugin ChargerNotif test button Not Responding — `SoundPlayer.Play()` di UI thread. Fix: pindah ke `Task.Run()`.
- Fix `NETSDK1022 Duplicate Compile items` — hapus `<Compile>` manual dari csproj, SDK sudah auto-include.
- Fix `NETSDK1022 Duplicate Page items` — hapus `<Page>` manual dari csproj, SDK sudah auto-include.
- Fix `src` folder muncul di install directory — `src\ZeroShell\**\*.png` dan `src\Virtual_Assisten\**\*` tidak pakai `<Link>`, sekarang semua pakai `<Link>` agar output ke folder yang benar.
- Fix source `.cs`/`.xaml` ikut ke `Tools\Plugins\` di install dir — csproj sekarang hanya copy `.png`, `.wav`, `.gif`, `Styles.xaml`.
- Fix `Privacy.txt` dan `Readme.md` ikut ke install directory — dihapus dari `Setup.iss [Files]`.

### 🔧 Changes

- `ChargerBatterynotif.core` scope dipersempit: hanya handle Charging, Unplugged, Full. Low & Critical tetap di `zeromix.Battery`.
- **Ganti Lottie → GIF**: hapus `LottieSharp` package, hapus folder `json/`, tambah `WpfAnimatedGif` NuGet. Animasi kini dari `gif/Welcome.gif` dan `gif/Loader cat.gif`.
- Built-in WAV sounds: `charging.wav`, `unplug.wav`, `full.wav` (hapus `low.wav` dan `critical.wav` karena scope dipersempit).
- `zeromix.Battery/Maskot/` PNG direferensi langsung sebagai fallback jika GIF tidak ada.
- Virtual Assistant WebView: hapus `--disable-gpu-vsync` dan `--disable-plugins`, tambah `--js-flags=--max-old-space-size=128`.
- Virtual Assistant PIXI: `resolution: 1`, `autoDensity: false`, FPS cap 24, ticker stop sepenuhnya saat window blur.
- Virtual Assistant GC: pause 300ms antar model load, tambah `destroyTextureCache()`.
- `live2d-viewer.html` di-rewrite: loading guard, GC pause, auto-motion loop, body motion per karakter.
- `ZeroMix.csproj`: hapus `LottieSharp`, tambah `WpfAnimatedGif 2.0.2`, copy `*.gif` dari `Tools\Plugins\**`.
- `csproj` Virtual_Assisten: ganti wildcard broad ke explicit per-extension (`.png`, `.jpg`, `.html`, `.moc3`, `.model3.json`, dll) dengan `<Link>` agar tidak ada folder `src\` di output.
- `Setup.iss`: tambah bahasa Korea, `language.ini` disimpan ke `%AppData%\ZeroMix\`, hapus WebView2 check, tambah `zeromix-update.ps1`, fix uninstall buka `Feedback.html`.
- `Exe/Languages/Korean.isl` — file bahasa Korea baru untuk installer.

---

## [v5.4.0] - 2026-04-09

### ✨ Features
- **WDM Start Menu Glass**: Efek dark acrylic blur pada Start Menu (Windows 10 & 11).
- **WDM Notification Panel Glass**: Efek dark acrylic blur pada Action Center / Notification Panel.
- **WDM Persistence**: State WDM disimpan ke `wdm.json`, di-restore otomatis saat ZeroShell dibuka kembali.
- **Wallpaper Session**: State video wallpaper disimpan ke `wallpaper_session.json`, auto-restore saat app dibuka.

### 🐛 Bug Fixes
- Fix File Explorer glass — hapus font injection yang menyebabkan font bertolak belakang.
- Fix Start Menu glass hanya apply ke parent — sekarang apply ke semua child windows.
- Fix Notification Panel tidak berubah — tambah `ControlCenterWindow` class untuk Windows 11.
- Fix "Failed to save language preference" — `language.ini` dipindah ke `%AppData%\ZeroMix\`.
- Fix `Mutex.ReleaseMutex()` crash saat shutdown.
- Fix Plugins & About view overflow ke kanan saat window dikecilkan.
- Fix CI/CD: `v5.4.0` not valid version string — strip prefix `v` sebelum `-p:Version`.
- Fix Inno Setup output filename tidak match tag — `#define AppVersion` di-override via `/D`.

### 🔧 Changes
- `ApplyBlur` pakai `ACCENT_ENABLE_ACRYLICBLURBEHIND` dengan alpha tinggi agar warna hitam dominan.
- WDM pulse timer cover semua 4 elemen (Explorer, Taskbar, Start Menu, Notif), interval 2 detik.
- WDM section dihapus dari gear settings — hanya via `!wdm`.
- `!wdm` menu diperluas dari 5 opsi menjadi 7 opsi.
- Setup.iss: exclude video wallpaper & duplicate logo dari installer.
- Plugins & Recorder view: `UniformGrid` → `WrapPanel` untuk responsive layout.
- Release script: tidak timpa entry CHANGELOG yang sudah ditulis manual.
- GitHub Release notes diambil langsung dari `Docs/CHANGELOG.md`.

---
## [v5.3.1] - 2026-04-08

### ✨ Features
- Menambahkan Beberapa komponenen dan perbaikan , ZeroMixSell , SleepMode , Perubahan Tampilan , perbaiki Bugs , Optimize Virtual ,
- Menambahkan Beberapa komponenen dan perbaikan , ZeroMixSell , SleepMode , Perubahan Tampilan , perbaiki Bugs , Optimize Virtual ,
- Menambahkan Beberapa komponenen dan perbaikan , ZeroMixSell , SleepMode , Perubahan Tampilan , perbaiki Bugs , Optimize Virtual ,

### 🐛 Bug Fixes
- No bug fixes

### 🔧 Changes
- Rebuilt: Assets/Data/Video/Vs (5).mp4
- Rebuilt: Exe/Setup.iss
- Rebuilt: ZeroMix.csproj
- Rebuilt: src/MainWindow.xaml
- Rebuilt: src/MainWindow.xaml.cs
- Rebuilt: src/SleepMode/SleepOverlayWindow.xaml
- Rebuilt: src/SleepMode/SleepOverlayWindow.xaml.cs
- Rebuilt: src/SleepMode/SleepSettingsModel.cs
- Rebuilt: src/SleepMode/SleepSettingsWindow.xaml
- Rebuilt: src/SleepMode/SleepSettingsWindow.xaml.cs
- Rebuilt: src/Virtual_Assisten/VirtualAssistantWindow.xaml
- Rebuilt: src/Virtual_Assisten/VirtualAssistantWindow.xaml.cs
- Rebuilt: src/Wallpapers/VideoWallpaperWindow.xaml.cs
- Rebuilt: src/Wallpapers/WallpapersView.xaml.cs
- Rebuilt: src/ZeroMix.recorder/WindowPickerWindow.xaml.cs
- Rebuilt: src/ZeroShell/ZeroShellWindow.xaml
- Rebuilt: src/ZeroShell/ZeroShellWindow.xaml.cs

---
## [5.3.0] - 2026-04-07

### ✨ Features
- Sleep Mode: tambah opsi musik opsional (MP3/WAV/FLAC/OGG/M4A) — bisa dipakai bareng background apapun
- Sleep Mode: custom background (gambar/video) langsung tampil tanpa delay
- Sleep Mode: volume slider untuk musik

### 🐛 Bug Fixes
- Fix Search Overlay kotak-kotak setelah penggunaan ke-2 (root cause: window di-reuse, sekarang selalu buat instance baru)
- Fix SleepMode custom background tidak tersimpan saat settings dibuka ulang
- Fix ZeroMix.recorder video output hitam (tambah -vsync cfr flag ke FFmpeg)
- Fix video wallpaper tidak bisa diputar — error handling diperbaiki dengan pesan yang jelas
- Fix uninstaller tidak hapus registry context menu dan startup shortcut

### 🔧 Changes
- Virtual Assistant: hapus tombol mic dari UI overlay (mic tetap bisa diakses dari halaman list model)
- Virtual Assistant: eye tracking interval 200ms → 300ms, vision timer 30s → 60s (hemat CPU/RAM)
- Virtual Assistant: semua permission request di-deny kecuali yang dibutuhkan
- Wallpapers: hapus kode optimasi video background yang tidak perlu (langsung play tanpa ffmpeg pre-process)
- Uninstaller: auto kill ZeroMix.exe, hapus registry, hapus startup shortcut, hapus LocalAppData
- Sleep Mode: BitmapImage di-Freeze() setelah load → tidak makan RAM berulang
- Sleep Mode: DispatcherTimer pakai Background priority

---



-- End Version 5.2.5 -> 5.3.0 -- 



## [v5.2.5] - 2026-04-07

### ✨ Features
- Sleep Mode: tambah opsi musik opsional (MP3/WAV/FLAC/OGG/M4A) — bisa dipakai bareng background apapun
- Sleep Mode: custom background (gambar/video) langsung tampil tanpa delay
- Sleep Mode: volume slider untuk musik

### 🐛 Bug Fixes
- Fix Search Overlay kotak-kotak setelah penggunaan ke-2 (root cause: window di-reuse, sekarang selalu buat instance baru)
- Fix SleepMode custom background tidak tersimpan saat settings dibuka ulang
- Fix ZeroMix.recorder video output hitam (tambah -vsync cfr flag ke FFmpeg)
- Fix video wallpaper tidak bisa diputar — error handling diperbaiki dengan pesan yang jelas
- Fix uninstaller tidak hapus registry context menu dan startup shortcut

### 🔧 Changes
- Virtual Assistant: hapus tombol mic dari UI overlay (mic tetap bisa diakses dari halaman list model)
- Virtual Assistant: eye tracking interval 200ms → 300ms, vision timer 30s → 60s (hemat CPU/RAM)
- Virtual Assistant: semua permission request di-deny kecuali yang dibutuhkan
- Virtual Assistant: tambah AreBrowserAcceleratorKeysEnabled=false dan IsSwipeNavigationEnabled=false
- Wallpapers: hapus kode optimasi video background yang tidak perlu (langsung play tanpa ffmpeg pre-process)
- Uninstaller: auto kill ZeroMix.exe, hapus registry, hapus startup shortcut, hapus LocalAppData
- Sleep Mode: BitmapImage di-Freeze() setelah load → tidak makan RAM berulang
- Sleep Mode: DispatcherTimer pakai Background priority

---
## [v5.2.3] - 2026-04-06

### ✨ Features
- AOD Style Picker — 5 style Always-On Display
- Analog Clock AOD dengan anti burn-in
- Sidebar Update Badge
- Auto Silent Update Check

### 🐛 Bug Fixes
- Fix double instance
- Fix Search Overlay lambat
- Fix Sleep Mode overlay crash saat laptop wake up

---
## [v5.2.2] - 2026-04-05

### ✨ Features
- Sidebar update badge — dot merah pulsing di icon System Info saat ada update tersedia
- Auto silent update check saat startup (tidak popup, hanya nyalain badge)
- Parallel chunked download di updater script (8 koneksi, IDM-style)

### 🐛 Bug Fixes
- Fix download lambat di zeromix-update.ps1 — ganti Invoke-WebRequest ke HttpWebRequest
- Fix path Downloads yang salah di beberapa sistem
- Fix installer popup "applications using files" — auto kill ZeroMix sebelum install

### 🔧 Changes
- Buffer download naik dari 4KB ke 128KB per chunk
- Progress bar updater sekarang tampilkan speed (KB/s) dan ETA

---

## [v5.1.9] - 2026-04-03

### ✨ Features
- Game Mode Translator via Clipboard Paste (Ctrl+V), kompatibel dengan DirectInput/RawInput
- CI/CD Sign Fix — perbaikan pipeline signing di GitHub Actions

### 🐛 Bug Fixes
- Fix osslsigncode tidak dikenali setelah choco install pada Windows runner
- Fallback ke path hardcoded C:\ProgramData\chocolatey\bin\ jika PATH belum ter-refresh

---

## [v5.1.5] - 2026-04-02

### ✨ Features
- Ask AI Mode Overlay di Search Box
- Smart Intent Detection — carikan foto, beli barang, shopee
- Text-to-Speech (TTS) untuk Frieren/Fern/HuoHuo
- Lip-Sync Animation sinkron dengan suara

### 🐛 Bug Fixes
- Fix Force Close saat tombol rekam diklik pada beberapa GPU
- Fix Temp Cleanup — pembersihan %temp% lebih stabil

### 🚀 Improvements
- Hardware Encoder Fallback: NVENC/QSV/AMF gagal → otomatis ke CPU
- DXGI Resource Management lebih efisien
- Memory Optimization: cache Live2D dibersihkan lebih agresif
- System Health scan lebih ringan

---

## [v5.1.3] - 2026-03-28

### 🐛 Bug Fixes
- Fix crash recorder pada GPU tertentu saat pertama kali dijalankan
- Fix memory leak pada engine Live2D saat karakter aktif lama
- Fix pembersihan %temp% yang tidak lengkap
- Fix encoder fallback yang menyebabkan hang saat NVENC tidak tersedia

### 🚀 Improvements
- Stabilitas DXGI capture ditingkatkan
- Optimasi minor pada System Health scanner

---

## [v5.1.2] - 2026-03-25

### ✨ Features
- Ask AI Mode Overlay di Search Box
- Smart Intent Detection untuk foto dan marketplace
- Fallback to AI — pertanyaan umum dijawab Frieren
- Text-to-Speech dan Lip-Sync Animation

### 🐛 Bug Fixes
- Fix Force Close saat tombol rekam diklik
- Fix Temp Cleanup lebih stabil

### 🚀 Improvements
- Hardware Encoder Fallback lebih cerdas
- Memory Optimization untuk Live2D
- System Health scanner lebih ringan

---

## [v1.0.0] - Initial Release

### ✨ Features
- Desktop Recorder — full-screen dan window-specific
- GPU-Accelerated Encoding (NVENC, QuickSync, AMF)
- Plugin System dengan Lua support
- Virtual Assistant AI
- Wallpaper Management
- Transparent Taskbar
- Sleep Mode

---

**Maintained by:** ZeroMix Team | **License:** [LICENSE.txt](./LICENSE.txt)
