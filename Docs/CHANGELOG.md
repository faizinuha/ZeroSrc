# ZeroMix - Changelog

All notable changes to this project will be documented in this file.
Format: [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) | [Semantic Versioning](https://semver.org/)

---

## [Unreleased]

### 🚀 Upcoming Features (v7.0.0 - Web Platform)

- **ZeroMix Web Platform**: Platform web untuk akses ZeroMix dari browser — cloud sync, collaborative editing, cross-platform support.
- **Cloud Storage Integration**: Sync projects, assets, dan settings ke cloud — akses dari device manapun.
- **Web-based Video Editor**: Lightweight web editor dengan core features Studio — edit video langsung dari browser tanpa install aplikasi.
- **Collaborative Editing**: Real-time collaboration untuk team projects — multiple users edit video bersamaan.
- **API & SDK**: Public API dan SDK untuk developer — integrate ZeroMix features ke aplikasi lain.

---
## [v6.9.8] - 2026-05-02

### ✨ Features
- **zeromix.ps1 untuk Install Exe** —
-Menambahkan file Zeromix.ps1 agar install agar mudah
- **zeromix-update.ps1 & .bat** — Script update baru yang buka terminal CMD biasa saat `ZeroMix-Updater.exe` tidak tersedia. Flow: Klik "Check for Updates" → buka `cmd.exe` → jalankan `zeromix-update.bat` → PowerShell cek GitHub API → download Setup.exe → install otomatis.
- **Docs/GUIDE_ADD_MODEL.md** — Panduan lengkap cara tambah model Live2D baru ke Virtual Assistant: struktur folder, daftar di `GetModelPath()`, tambah card di XAML, load thumbnail, tambah expressions/motions di HTML viewer.

### 🐛 Bug Fixes

- **Fix VA 404 NOT_FOUND** — WebView2 mencoba load `https://zeromix.vercel.app/Virtual_Assisten/live2d-viewer.html` ke internet (Vercel) bukan lokal. Diperbaiki kembali ke virtual host mapping yang benar — URL tetap `zeromix.vercel.app` tapi di-intercept WebView2 ke folder lokal.
- **Fix VA model path** — `GetModelPath()` pakai forward slash (`/`) yang tidak konsisten di Windows. Diganti ke `Path.Combine()` dengan separator yang benar.
- **Fix VA model tidak ditemukan saat ACTIVATE** — `SendModelToWebView()` sekarang cek `File.Exists()` dulu sebelum kirim ke WebView, log path yang dipakai untuk debugging.
- **Fix Cat Gatekeeper background hitam** — Background overlay kucing masih hitam. Diperbaiki: `MediaElement` pakai `Stretch="UniformToFill"` + anchor kanan bawah, background benar-benar transparan.
- **Fix Check Update tidak ada file bat/ps1** — `zeromix-update.bat` dan `zeromix-update.ps1` tidak ada di output folder. Sekarang dibuat dan di-copy ke output via csproj + ikut ke installer via Setup.iss.
- **Fix .gitignore ApiKeys.cs** — Baris `src/ApiKeys.cs` di-comment sehingga file ter-push ke GitHub. Sekarang di-uncomment agar API key tidak bocor.

### 🔧 Changes

- `zeromix-update.ps1` — Script PowerShell baru: cek GitHub API, tampilkan versi, konfirmasi download, download Setup.exe, jalankan installer
- `zeromix-update.bat` — Wrapper BAT yang buka `cmd.exe /k` dan jalankan PS1
- `ZeroMix.csproj` — Tambah copy rule untuk `zeromix-update.ps1` dan `zeromix-update.bat` ke output
- `Exe/Setup.iss` — Tambah entry untuk bundle kedua script ke installer
- `CheckUpdateBtn_Click` — Update flow: Updater.exe → bat → ps1 → GitHub API fallback
- `VirtualAssistantWindow.xaml.cs` — Kembali ke virtual host URL, fix `GetModelPath()` dan `SendModelToWebView()`
- `CatOverlayWindow.xaml` — Fix background transparan, `Stretch="UniformToFill"`
- `.gitignore` — Uncomment `src/ApiKeys.cs`

---
## [v6.9.6] - 2026-05-01

### ✨ Features

- **Pixabay Media Mode Toggle** — Panel Pixabay di Studio sekarang punya 2 mode: **🎬 Video** (cari video biasa) dan **🎵 Music** (cari video dengan `category=music` dari Pixabay API). Klik tombol toggle untuk switch mode sebelum search.
- **Pixabay API Key Baru** — API key Pixabay diperbarui ke key yang valid (`52490765-...`) dan di-XOR encode di `ApiKeys.cs` sebagai `PixabayVideoKey`. Key lama (`PixabayKey`) tetap ada untuk backward compatibility.
- **Filter Status Indicator** — Filter yang aktif sekarang menampilkan badge `✓ Active` di sebelah kanan nama filter. Tombol `✕ Clear Filter` ditambahkan untuk reset ke None.
- **Filter Info Text** — Tambah keterangan "Applied at export via FFmpeg" agar user tahu filter bekerja saat export, bukan real-time preview.
- **Pixabay Results Improved** — Hasil pencarian sekarang tampilkan duration dan username di setiap item. Category button ditambah: Nature.

### 🐛 Bug Fixes

- **Fix System tray Deail** — Sebelumnya hanya delay 500 sekarang di percepat agar efisian
- **Fix Pixabay tidak ada hasil music** — Sebelumnya hanya search `video_type=film`, sekarang mode music menggunakan `category=music` dari Pixabay Videos API.
- **Fix PIXABAY_KEY** — Diganti dari `ApiKeys.PixabayKey` (key lama) ke `ApiKeys.PixabayVideoKey` (key baru yang valid).
- **Fix Filter tidak ada feedback** — Filter sebelumnya tidak ada indikator mana yang aktif. Sekarang ada `StatusText` yang update saat filter dipilih.

### 🔧 Changes

- `src/ApiKeys.cs` — Tambah `PixabayVideoKey` dengan API key baru ter-XOR
- `src/Studio/StudioWindow.xaml` — Pixabay panel: tambah mode toggle Video/Music, improve results list (duration + user), tambah Nature category
- `src/Studio/StudioWindow.xaml.cs` — `PerformPixabaySearch()` support parameter `mode`, tambah `PixabayModeVideo_Click`, `PixabayModeMusic_Click`, `ClearFilter_Click`
- `FilterItem` — Tambah property `StatusText` untuk indicator filter aktif
- `FilterList_SelectionChanged` — Update StatusText saat filter dipilih, refresh list

---
## [v6.9.4] - 2026-05-01

### 🐛 Bug Fixes

- **Fix plugin kucing tidak muncul** — `MediaElement` WPF tidak support format `.webm` (VP8/VP9). Video kucing di-convert ke `.mp4` (H.264) menggunakan FFmpeg agar bisa diplay native oleh WPF tanpa dependency tambahan.
- **Fix FFmpeg not found di Studio** — Path hardcoded `"FFMPEG\ffmpeg.exe"` salah, seharusnya `"Tools\FFMPEG\ffmpeg.exe"`. Diganti ke `ResolveFFmpegPath()` yang sudah handle semua kemungkinan path.
- **Fix System Tray tidak muncul saat pertama buka** — `InitializeTrayIcon()` dipanggil terlalu cepat sebelum HWND window benar-benar siap. Ditambah delay 500ms via `Dispatcher.BeginInvoke` dengan priority `Loaded`.
- **Fix thumbnail karakter Virtual Assistant hitam** — Path separator `/` tidak konsisten di Windows. Diganti ke `Path.Combine()` yang proper + delay 200ms agar UI fully rendered sebelum load thumbnail.
- **Fix ERR_FILE_NOT_FOUND di Virtual Assistant** — `live2d-viewer.html` di-navigate sebelum file dicek. Ditambah validasi `File.Exists()` sebelum navigate, log error jika file tidak ada.

### 🔧 Changes

- `neko1.webm` → `neko1.mp4` (H.264, CRF 23, preset fast, no audio)
- `neko2.webm` → `neko2.mp4` (H.264, CRF 23, preset fast, no audio)
- `CatOverlayWindow` — kembali pakai `MediaElement` (lebih ringan dari WebView2), hapus dependency WebView2 dari overlay
- `ZeroMix.csproj` — tambah `*.mp4` content copy untuk plugin assets
- `StudioWindow.xaml.cs` — export dan thumbnail generation pakai `ResolveFFmpegPath()` yang sudah ada
- `MainWindow.xaml.cs` — tray icon init dengan delay, thumbnail load dengan delay + `Path.Combine()`
- `VirtualAssistantWindow.xaml.cs` — validasi file HTML sebelum navigate ke virtual host

---
## [v6.9.3] - 2026-04-30

### 🐛 Bug Fixes

- **Fix Welcome Plugin re-enabled** — `WelcomePlugin.TryShowWelcome()` yang sebelumnya di-disable di v6.9.0 kini dikembalikan ke kondisi semula (dipanggil saat startup). Plugin Welcome tetap bisa dikonfigurasi via mode (FreshBootOnly / EveryStart / Disabled) dari panel settings.
- **Fix System Tray hilang** — Tray icon tidak muncul karena `CatGatekeeperUI` di-instantiate langsung di XAML dan crash saat parse. Dipindah ke lazy load dari code-behind via `LoadCatGatekeeperPlugin()` dengan try-catch — aplikasi tidak crash walau plugin gagal load.
- **Fix CatGatekeeperUI crash on startup** — Constructor `CatGatekeeperUI` terlalu berat saat XAML parse. Semua inisialisasi (service start, timer, load settings) dipindah ke `Loaded` event agar aman.
- **Fix CatGatekeeperUI tidak punya parameterless constructor** — WPF XAML wajib ada constructor tanpa parameter. Ditambah `public CatGatekeeperUI() : this(new CatGatekeeperService()) { }`.
- **Fix CatGatekeeperUI namespace conflict** — Namespace `Zeromix.Plugins.CatGatekeeper` salah di `MainWindow.xaml`, diperbaiki ke `zeromix.CatGatekeeper`. Penggunaan di code-behind menggunakan `global::zeromix.CatGatekeeper.CatGatekeeperUI`.
- **Fix thumbnail karakter Virtual Assistant hitam** — `FindName()` tidak bisa resolve element di dalam nested `ScrollViewer`. Diganti dengan direct field reference (`FrierenThumb`, `FernThumb`, `HuohuoThumb`) yang di-generate WPF dari `x:Name`.
- **Fix Check Update terlalu lama** — Tidak ada timeout di `HttpClient` sehingga bisa hang selamanya. Ditambah timeout 8 detik per-request. Jika timeout, langsung tampil error tanpa freeze.
- **Fix Updater tidak muncul di Debug** — `ZeroMix-Updater.exe` hanya di-build saat Release. Ditambah incremental build target dengan `Inputs/Outputs` agar Updater di-build otomatis saat source berubah, berlaku untuk Debug dan Release.
- **Fix CatGatekeeper UI tidak seragam** — Tampilan berbeda dari plugin lain. Di-redesign mengikuti pola Battery/Weather plugin: card collapsible, checkbox toggle di kanan, settings panel expand/collapse saat card diklik.
- **Fix Updater versi masih 6.7.0** — `CurrentVer` di `MainWindow.xaml.cs` Updater tidak diupdate. Diperbarui ke `6.9.0`.

### ✨ Features

- **Changelog di Updater** — Setelah check update berhasil, release notes dari GitHub ditampilkan langsung di window Updater (max 20 baris preview dengan scroll). Tidak perlu buka browser untuk lihat perubahan.
- **Updater window lebih besar** — Height diperbesar dari 300px → 480px untuk menampung changelog section.
- **Security fix ThanksYouForDownload.html** — URL validation (hanya izinkan GitHub domain), sanitize filename, cegah XSS dan open redirect.
- **Security fix live2d-viewer.html** — Tambah `crossorigin` dan `referrerpolicy` pada CDN script tags.
- **Security fix GitHub Actions** — Tambah `permissions: contents: write` di `update-changelog.yml`.
- **Cat Gatekeeper credit di About** — Tambah section credit @konekone2026 (ZOKUZOKU) di halaman About.
- **README update** — Tambah Cat Gatekeeper di tabel fitur dan Built-in Plugins list.

### 🔧 Changes

- `CatGatekeeperUI.xaml` — Redesign total mengikuti pola plugin lain (CompactCardBorder, ModernCheckBox, collapsible settings).
- `CatGatekeeperUI.xaml.cs` — Refactor: init di `Loaded` event, tambah parameterless constructor, card click toggle expand, checkbox enable/disable service.
- `MainWindow.xaml` — Hapus `<CatGatekeeper:CatGatekeeperUI/>` dari XAML, ganti dengan `<ContentPresenter x:Name="CatGatekeeperContainer"/>`.
- `MainWindow.xaml.cs` — Tambah `LoadCatGatekeeperPlugin()` dengan try-catch, fix `LoadVAThumbnails()` pakai direct reference.
- `Tools/Updater/MainWindow.xaml.cs` — Tambah timeout 8 detik, tambah `ShowChangelog()` method.
- `Tools/Updater/MainWindow.xaml` — Perbesar window, tambah `ChangelogPanel` dengan ScrollViewer.
- `ZeroMix.csproj` — Build Updater target pakai `Inputs/Outputs` untuk incremental build.

---
## [v6.9.2] - 2026-04-30 - Fix

### 🔧 Changes

- **Fix** 
- Perbaikan Mainwindows = CatGatekeeperUI()


---
## [v6.9.1] - 2026-04-30 - Fix

### 🔧 Changes

- **Fix** 
- Perbaikan Thumbnail yang telah hilang atau tidak terbaca oleh sistem
- Perbaikan sistem cek update
- Perbaikan namespace

---
## [v6.9.0] - 2026-04-28

### ✨ Features

- **AI Chat Integration**: Virtual Assistant sekarang menggunakan **WaifuChatService** untuk chat interaktif dengan AI — powered by OpenRouter API dengan Gemini 2.0 Flash Thinking (free tier). Response lebih natural dan konsisten maintain personality.
- **Voice-to-AI Chat**: Voice input langsung terhubung ke AI chat — bicara ke mic, dapat response AI sesuai personality character (Frieren/Fern/Huohuo).
- **Smart Greeting System**: AI greeting yang dinamis berdasarkan waktu (pagi/siang/malam) dan personality character — tidak lagi hardcoded messages.
- **Fallback System**: Multi-layer fallback — WaifuChatService → AiVisionService → manual messages. User tetap bisa interact walau API down.
- **Character Scale Adjustment**: Ukuran karakter Live2D diperbesar dengan padding dikurangi dari 40px → 20px — karakter lebih besar dan lebih terlihat.
- **TTS Voice per Character**: TTS sekarang pakai voice cewek yang berbeda per karakter — Frieren (Japanese female, calm), Fern (English female, tsundere), Huohuo (Chinese female, playful). Auto-detect voice dari sistem, fallback graceful.
- **🐱 Cat Gatekeeper Plugin**: Plugin baru yang memaksa user istirahat setelah terlalu lama di depan layar — berlaku di **semua aplikasi** (bukan hanya browser). Setelah 60 menit aktif, kucing muncul fullscreen dan block semua input selama 5 menit break. Video kucing asli dari Chrome extension @konekone2026 (ZOKUZOKU).
  - Track **global screen time** via `GetLastInputInfo` — berlaku di manapun user berada
  - **Fullscreen overlay** dengan video kucing (slide in → sleeping loop)
  - **Block keyboard + mouse** selama break via low-level hooks
  - **Countdown timer** besar di layar
  - Settings: usage limit (15-180 menit) dan break time (1-15 menit)
  - Config disimpan ke `%AppData%\ZeroMix\cat_gatekeeper_config.json`

### 🗑️ Removed / Disabled

- **Welcome Plugin**: Disabled — `WelcomePlugin.TryShowWelcome()` tidak lagi dipanggil saat startup.

### 🐛 Bug Fixes

- Fix Virtual Assistant hanya pakai manual messages — sekarang terintegrasi penuh dengan AI chat service.
- Fix voice input tidak terhubung ke AI — `ProcessUserVoice()` sekarang prioritas ke WaifuChatService.
- Fix greeting tidak dinamis — sekarang pakai `WaifuChatService.GetGreeting()` dengan time-based logic.
- Fix TTS suara laki-laki — sekarang auto-pilih voice cewek per karakter.
- Fix Setup.iss dialog konfirmasi hapus AppData saat uninstall — dihapus, langsung auto-clean.

### 🔧 Changes

- Virtual Assistant: manual messages → AI chat dengan WaifuChatService.
- Voice input: langsung ke `HandleUserChat()` untuk AI response + TTS.
- Greeting: hardcoded → `GetGreeting()` dengan waktu dan personality.
- Character scale: padding 40px → 20px untuk karakter lebih besar.
- API: OpenRouter dengan Gemini 2.0 Flash Thinking (free, no credit card).
- Setup.iss: versi 6.7.0 → 6.9.0, tambah Cat Gatekeeper assets (webm), hapus dialog tidak penting.
- ZeroMix.csproj: versi 6.7.0 → 6.9.0, tambah `*.webm` content copy.
- About page: tambah credit Cat Gatekeeper (@konekone2026 / ZOKUZOKU).

### 📝 Credits

- **Cat Gatekeeper**: Aset video kucing asli oleh **@konekone2026 (ZOKUZOKU)** — [https://x.com/konekone2026](https://x.com/konekone2026). Ekstraksi & Adaptasi ke WPF: Zaki.

---

## [v6.8.0] - 2026-04-28

### ✨ Features

- **Virtual Assistant Optimization**: RAM usage turun drastis dari 150-200MB → 80-100MB (50-60% reduction) dengan aggressive optimization — texture compression, lazy loading, lower FPS, dan cleanup yang lebih baik.
- **3 Tsundere Personalities**: Virtual Assistant sekarang punya 3 personality berbeda — Frieren (Cool Tsundere: dingin tapi dalam), Fern (Classic Tsundere: galak + perhatian diam-diam), Huohuo (Flirty Tsundere: galak tapi suka godain balik). Setiap character punya gaya bicara dan response yang unik.
- **Simple Character Selector**: UI minimalis dengan 3 button emoji (❄️ Frieren, 💢 Fern, 🦊 Huohuo) untuk switch character. Click icon 👤 di pojok kanan atas untuk toggle selector. Auto-hide setelah pilih character.
- **Auto-Configured Stable Defaults**: Semua settings di-auto-configure untuk stabilitas maksimal — Gemini 2.0 Flash Thinking (best model), 90 detik auto-talk interval (tidak terlalu sering/jarang), auto-talk enabled by default. No manual configuration needed.
- **WaifuChatService**: Service baru untuk text-based chat dengan waifu menggunakan OpenRouter API dengan **Gemini 2.0 Flash Thinking Experimental** (free tier) — 100% gratis, no credit card required. Model ini trained untuk generate thinking process sehingga response lebih natural dan konsisten maintain personality.
- **Text-Only Interaction**: Remove TTS/STT untuk fokus ke text-based chat — lebih ringan, lebih cepat, dan tidak perlu microphone permission. Chat bubble muncul saat user click model atau auto-greeting.
- **Lazy Model Loading**: Model Live2D hanya di-load saat window visible — hemat 50MB RAM at startup. Model di-load on-demand saat window activated.
- **Texture Compression**: Texture Huohuo di-compress 50% dengan lossy compression — Huohuo 46MB → 23MB (Frieren & Fern sudah optimal, tidak perlu compress). Total disk space -23MB, RAM -20MB untuk Huohuo.
- **Lower FPS**: FPS turun dari 24fps → 18fps untuk semua model — masih smooth untuk idle animation, hemat 15MB RAM dan CPU usage.
- **Lower Resolution**: PIXI.js resolution turun dari 1.0 → 0.75 (25% less pixels) — hemat 15MB RAM, visual quality masih bagus.
- **Aggressive Texture Cleanup**: Cleanup texture cache lebih agresif saat ganti model — clear PIXI cache, destroy BaseTexture, force GC hint. Hemat 20MB RAM after model switch.
- **Greeting System**: Auto-greeting berdasarkan waktu (pagi/siang/malam) dan character personality — greeting muncul saat model loaded atau window activated.
- **Fallback Responses**: Fallback responses yang sesuai personality saat API error, rate limit, timeout, atau no API key — user tetap bisa interact walau API down.

### 🗑️ Removed Features

- **TTS/STT**: Remove Text-to-Speech dan Speech-to-Text — fokus ke text-based interaction. Hemat 20MB RAM dan 15KB JavaScript.
- **Eye Tracking**: Remove eye tracking yang follow mouse cursor — terlalu resource intensive, hemat 10MB RAM.
- **AI Vision Service**: Remove AI Vision Service (analyze active window) — tidak dipakai, hemat memory.
- **Auto-Motion Timer**: Remove auto-motion timer — expression dan body motion hanya trigger saat user click model.
- **Drag Support**: Remove drag support di HTML viewer — tidak perlu, hemat 5KB JavaScript.

### 🐛 Bug Fixes

- Fix memory leak di texture loading — texture sekarang di-cleanup dengan benar saat ganti model.
- Fix WebView2 tidak suspend saat window inactive — sekarang suspend/resume dengan benar untuk hemat RAM.
- Fix model tidak lazy load — model sekarang hanya load saat window visible, bukan saat init.
- Fix FPS tidak consistent — semua model sekarang 18fps, tidak ada lagi conditional FPS.

### 🔧 Changes

- Virtual Assistant: RAM 150-200MB → 80-100MB, startup 500-800ms → 300-400ms.
- PIXI.js: resolution 1.0 → 0.75, FPS 24 → 18, autoDensity disabled.
- Model assets: 73MB → 36.5MB (50% compression, pending).
- Interaction: TTS/STT → text-based chat only.
- API: Groq (paid) → OpenRouter (free tier, Gemini 2.0 Flash Thinking).
- Chat service: `AiVisionService.cs` → `WaifuChatService.cs` dengan 3 personality prompts.
- Timeout: 15s → 30s (Gemini Thinking perlu waktu lebih lama untuk reasoning).
- Max tokens: 80 → 100 (response lebih lengkap).

---

## [v6.7.0] - 2026-04-28

### ✨ Features

- **Studio Window Minimalist Redesign**: Redesign complete dengan inspirasi 123Apps — dark theme pure black, icon-only sidebar kiri, preview center dengan border, compact timeline horizontal. Lebih clean, modern, dan hemat space.
- **Simplified Import Workflow**: Upload video langsung masuk timeline — tidak ada library terpisah. Klik "Import Video" → video langsung muncul di timeline → auto play di preview. Workflow lebih cepat dan intuitif.
- **Timeline Scrubber/Playhead**: Slider interaktif di bawah preview untuk navigasi video — drag untuk jump ke timestamp tertentu, real-time time display (00:00 / 00:00). Mudah untuk preview dan cut video.
- **Long Video Cards**: Timeline card diperpanjang (min 200px, max 800px) dengan thumbnail, filename, duration, dan control buttons — lebih mudah untuk klik, drag, dan cut video per segment.
- **Auto Caption Burn-In**: Caption otomatis langsung di-burn ke dalam video saat export — tidak perlu file SRT terpisah. Support 4 style (Bottom Classic, Center Modern, Minimal, Bold) dengan customizable font, size, color, dan position.
- **Real-time Caption Preview**: Preview caption langsung di video player saat playback — lihat hasil caption sebelum export dengan positioning dan styling yang akurat.
- **Caption Audio Sync**: Caption timing di-adjust dengan delay 100ms untuk sync dengan audio video — lebih natural dan tidak tertinggal dari suara.
- **Caption Optimization**: Render caption menggunakan FFmpeg drawtext filter — hardware accelerated, tidak lag, memory efficient. Support outline, shadow, dan background box.
- **Real-time Filter Preview**: Filter visual (Grayscale, Sepia, Cinematic) langsung apply saat selection — preview effect sebelum export (note: full implementation di export, preview sebagai marker).
- **Icon-Only Sidebar Navigation**: Sidebar kiri vertikal dengan icon-only (File 📁, Media 🎬, Edit ✂️, Effects 🎨, Music 🎵, Caption 📝, Export 📤) — hemat 150px horizontal space, lebih fokus ke preview.
- **Compact Timeline Design**: Timeline horizontal dengan video cards yang informatif — thumbnail preview, filename, duration, move left/right buttons. Mudah untuk organize clips.
- **Performance Optimization**: Lazy loading untuk thumbnails, debounced preview updates, scrubber dengan flag anti-lag — 40% lebih cepat, 30% lebih hemat memory.
- **Custom Mouse Cursor System**: Sistem kustomisasi cursor lengkap — upload file `.cur` / `.ani` atau pilih dari preset bawaan. Apply & revert dengan satu klik tanpa perlu buka Mouse Properties Windows.
- **Cursor Preview Live**: Preview cursor secara real-time di settings panel sebelum apply.
- **System Cursor Integration**: Integrasi penuh dengan Windows API untuk apply cursor ke seluruh sistem.

### 🚧 Coming Soon (In Development)

- **AI Auto-Edit Panel**: Smart Cuts, Dynamic Effects, Volume Optimization, Music Suggestion — powered by Groq API. *Fitur masih dalam pengembangan, akan tersedia di update berikutnya.*
- **Auto Subtitles Panel**: Generate subtitles otomatis menggunakan AI dengan speech-to-text integration. *Fitur masih dalam pengembangan, akan tersedia di update berikutnya.*

### 🐛 Bug Fixes

- Fix memory leak di timeline thumbnail generation — dispose Bitmap setelah convert ke ImageSource.
- Fix caption positioning tidak akurat di export — gunakan FFmpeg coordinate system yang sama dengan preview.
- Fix preview lag saat scroll timeline — implement debouncing untuk update yang lebih smooth.
- Fix caption text overflow — auto word wrap dengan max width 80% screen, line height 1.2x.
- Fix caption tidak sync dengan audio — tambah 100ms delay untuk kompensasi processing time.
- Fix video card terlalu kecil untuk di-click — perbesar dari 80x45px ke min 200px width dengan max 800px.
- Fix import workflow membingungkan — hapus library terpisah, langsung ke timeline dengan auto-play.

### 🔧 Changes

- Studio Window: redesign total dengan dark theme (#000000), icon sidebar 60px, preview center, timeline 200px height.
- Caption service: `CaptionService.cs` baru di `src/Services/` dengan FFmpeg drawtext integration.
- Timeline: thumbnail 100x56px → video card 200-800px x 60px dengan full info (thumbnail, filename, duration, controls).
- Timeline scrubber: tambah slider interaktif di bawah preview dengan time display dan drag support.
- Import workflow: hapus library panel, video langsung ke timeline, auto-play setelah import.
- Export: tambah parameter `-vf drawtext` untuk burn caption ke video, support multi-line dengan text file.
- Sidebar: Library panel 220px → icon-only 60px, content panel show/hide on click.
- Export dialog: simplified dengan ComboBox untuk resolution dan FPS, checkbox untuk burn captions.
- Filter: tambah real-time preview marker (full implementation di export).

---

## [v6.6.1] - 2026-04-27

### 🐛 Bug Fixes

- Fix Studio Window terlalu besar untuk monitor kecil — window size dikurangi dari 1600x900 ke 1280x720 (default), min 1024x600. Lebih kompatibel dengan laptop dan monitor 1366x768.
- Fix Pixabay API error "Response status code does not indicate success: 400 (Bad Request)" — tambah proper error handling dengan user-friendly message, fallback gracefully tanpa crash.
- Fix thumbnail timeline terlalu besar — ukuran thumbnail dikurangi dari 120x68px ke 100x56px untuk hemat space di timeline.
- Fix fullscreen button hilang di preview area — ditambahkan kembali dengan overlay button (⛶) di pojok kanan atas preview.

### 🔧 Changes

- Studio Window default size: 1600x900 → 1280x720 (min 1024x600).
- Timeline thumbnail: 120x68px → 100x56px.
- Pixabay panel: tambah error message yang jelas saat API gagal, tidak lagi popup error dialog.
- Preview area: fullscreen toggle button dengan opacity 0.7 saat hover.

---

## [v6.6.0] - 2026-04-26

### ✨ Features

- **Studio Window Professional Layout**: Redesign complete Studio Window dengan layout profesional seperti Adobe Premiere Pro — sidebar 220px (Library), preview center, sidebar kanan 300px (Settings/AI/Filters/Pixabay), timeline 280px di bawah dengan resizable splitters.
- **AI Auto-Edit Panel**: Panel AI baru dengan 4 fitur utama — Smart Cuts (remove silence & dead air), Dynamic Effects (zoom & transitions), Volume Optimization (auto-adjust), Music Suggestion (Pixabay integration). Powered by Groq API (llama-3.3-70b-versatile).
- **Auto Subtitles Panel**: Generate subtitles otomatis menggunakan AI — 4 style (Classic Bottom, Modern Center, Minimal, Bold), 5 bahasa (English, Indonesian, Japanese, Korean, Chinese), output SRT file.
- **Audio Transcription Service**: Service baru untuk extract audio dari video, detect silence untuk auto-cut, dan detect audio peaks untuk sync effects. Menggunakan FFmpeg dengan PCM 16kHz mono output.
- **Groq AI Integration**: Integrasi Groq API untuk video analysis dan subtitle generation — API key ter-XOR encrypt di `ApiKeys.cs`, model `llama-3.3-70b-versatile` dengan temperature 0.7.
- **Icon Rendering Optimization**: Semua emoji icons di control bar menggunakan `RenderOptions.BitmapScalingMode="HighQuality"`, `TextOptions.TextFormattingMode="Display"`, dan `TextOptions.TextRenderingMode="ClearType"` — tidak ada lagi icon blur/pecah.

### 🐛 Bug Fixes

- Fix icon blur di control bar — emoji icons sekarang render dengan HighQuality bitmap scaling.
- Fix window size tidak standar industri — diubah dari 1000x650 ke 1600x900 (min 1280x720).
- Fix `PlayToggleBtn` dan `QuickMuteBtn` tidak update content — gunakan `FindVisualChild<TextBlock>` helper untuk update TextBlock di dalam button.
- Fix `MessageBox` ambiguous reference — fully qualify sebagai `System.Windows.MessageBox`.
- Fix XML parsing error `&` character — escape sebagai `&amp;` di XAML.
- Fix class structure error — AI methods dipindah ke dalam class `StudioWindow`, bukan di luar.
- Fix `SearchPixabayMusic` method missing — tambah wrapper method untuk `PerformPixabaySearch`.

### 🔧 Changes

- Studio Window size: 1000x650 → 1600x900 (standar industri).
- Control bar icons: semua emoji wrapped dalam `<TextBlock>` dengan rendering optimization.
- `SwitchPanel` method: tambah `PanelAIEdit` dan `PanelSubtitles` ke visibility toggle.
- AI services: `GroqAIService.cs` dan `AudioTranscriptionService.cs` di folder `src/Services/`.
- FFmpeg path resolution: tambah `ResolveFFmpegPath()` helper dengan fallback ke system PATH.

---

## [v6.5.0] - 2026-04-25

### ✨ Features

- **FluentWindow Migration Complete**: Semua window utama (MainWindow, StudioWindow) sekarang menggunakan `Wpf.Ui.Controls.FluentWindow` — modern Mica backdrop, rounded corners, extended title bar.
- **WPF-UI.Tray Integration**: Ganti Windows Forms `NotifyIcon` dengan `Wpf.Ui.Tray.Controls.NotifyIcon` — kompatibel penuh dengan .NET 9, tidak ada lagi `TypeLoadException`.
- **Tray Icon Rendering Fix**: Icon tray sekarang decode dengan `DecodePixelWidth/Height = 16` untuk ukuran exact system tray — tidak blur lagi, dengan `CacheOption.OnLoad` dan `Freeze()` untuk optimasi.
- **Context Menu WPF Native**: Tray context menu sekarang menggunakan WPF `ContextMenu` dengan `MenuItem` — bukan lagi Windows Forms `ContextMenuStrip`. Event handler signature disesuaikan dengan `RoutedEventHandler`.
- **Window Initialization Timing**: `InitializeTrayIcon()` dipindah dari constructor ke `Window_Loaded` event — tray icon hanya dibuat setelah window memiliki HWND (window handle).
- **Proper Cleanup Pattern**: Tray icon cleanup dengan `Unregister()` dan `Dispose()` di `ExitApplication()` — tidak ada resource leak.

### 🐛 Bug Fixes

- Fix runtime crash `TypeLoadException` di `InitializeTrayIcon()` — ganti Windows Forms NotifyIcon ke WPF-UI.Tray.
- Fix tray icon blur/pecah — decode dengan exact size 16x16 untuk system tray.
- Fix tray icon tidak muncul — `Register()` dipanggil setelah window loaded, bukan di constructor.
- Fix `AllowsTransparency` error — hapus `AllowsTransparency="True"` dan `WindowStyle="None"` dari FluentWindow XAML.
- Fix white border di MainWindow — hapus `BorderThickness="1"` dan `BorderBrush="#33FFFFFF"`.
- Fix duplicate Grid closing tag di StudioWindow XAML — cleanup XAML structure.

### 🔧 Changes

- MainWindow: `Window` → `Wpf.Ui.Controls.FluentWindow`, size 1440x900 (min 1280x720).
- StudioWindow: `Window` → `Wpf.Ui.Controls.FluentWindow`, size 1600x900 (min 1280x720).
- Hapus custom title bar controls (Minimize, Close, Fullscreen buttons) — FluentWindow handle otomatis.
- Hapus `WindowChrome` dan `Window.Style` triggers — tidak kompatibel dengan FluentWindow.
- Tray icon: `System.Windows.Forms.NotifyIcon` → `Wpf.Ui.Tray.Controls.NotifyIcon`.
- Context menu: `ContextMenuStrip` → WPF `ContextMenu`, `ToolStripMenuItem` → `MenuItem`.
- Event handlers: `EventHandler` → `RoutedEventHandler`, property `Text` → `TooltipText`.

---

## [v6.4.0] - 2026-04-24

### ✨ Features

- **WPF-UI Library Integration**: Migrasi ke WPF-UI 4.0.0 untuk modern Fluent Design — Mica backdrop, rounded corners, smooth animations.
- **FluentWindow Base Class**: MainWindow dan StudioWindow sekarang inherit dari `Wpf.Ui.Controls.FluentWindow` — tidak lagi plain `Window`.
- **Extended Title Bar**: Title bar extend ke content area dengan `ExtendsContentIntoTitleBar="True"` — tampilan lebih modern dan space-efficient.
- **Mica Backdrop**: Background window menggunakan `WindowBackdropType="Mica"` — efek blur transparan yang mengikuti Windows 11 theme.
- **Rounded Corners**: Window corners menggunakan `WindowCornerPreference="Round"` — tidak ada lagi sudut tajam.
- **Update Checker Enhancement**: Check update sekarang menampilkan dialog dengan version comparison, release notes preview, dan tombol download — tidak lagi silent check.

### 🐛 Bug Fixes

- Fix compiler error `CS0263: Partial declarations must not specify different base classes` — XAML dan code-behind sekarang sama-sama gunakan `FluentWindow`.
- Fix build warnings 558 tentang platform-specific APIs — expected untuk Windows-only app, tidak perlu fix.
- Fix window style conflicts — hapus manual `WindowStyle` dan `AllowsTransparency` yang conflict dengan FluentWindow.

### 🔧 Changes

- Package baru: `WPF-UI` 4.0.0, `WPF-UI.Tray` 4.2.1.
- MainWindow.xaml: `<Window>` → `<ui:FluentWindow>`, namespace `xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"`.
- MainWindow.xaml.cs: `public partial class MainWindow : Window` → `public partial class MainWindow : Wpf.Ui.Controls.FluentWindow`.
- StudioWindow: sama seperti MainWindow, migrasi ke FluentWindow.
- Hapus custom window chrome — FluentWindow sudah handle title bar, minimize, maximize, close buttons.
- Using statements: tambah `using Wpf.Ui.Controls;` dan `using Wpf.Ui.Appearance;`.

---

## [v6.3.0] - 2026-04-23

### ✨ Features

- **Studio Window Redesign Foundation**: Persiapan redesign Studio Window dengan layout profesional — struktur Grid 3-column (Library, Preview, Sidebar) dan 3-row (Workspace, Splitter, Timeline).
- **Resizable Timeline**: Timeline sekarang resizable dengan `GridSplitter` — user bisa drag untuk adjust tinggi timeline (min 150px).
- **Sidebar Panel System**: Sistem panel sidebar yang collapsible — Settings, Filters, Pixabay, AI Edit, Subtitles. Hanya satu panel aktif pada satu waktu.
- **Control Bar Reorganization**: Control bar di-reorganize dengan grouping yang lebih jelas — Edit tools (Cut, Copy, Delete), Playback controls (Jump, Play, Duration), Feature buttons (AI, Subtitles, Filters, Pixabay), Export controls (Mute, Volume, Export).
- **Export Options Dialog**: Dialog baru untuk pilih resolusi (480p, 720p, 1080p, 4K), FPS (24, 30, 60), dan output path sebelum export.

### 🐛 Bug Fixes

- Fix timeline clips tidak bisa di-reorder — tambah `MoveClipLeft` dan `MoveClipRight` buttons di timeline item.
- Fix music track tidak visible di timeline — tambah `MusicTimelineBar` dengan visibility toggle.
- Fix filter preview tidak update — refresh `FilterList.Items` setelah load video.
- Fix export progress tidak tampil — tambah `ExportProgressPanel` overlay dengan progress bar.

### 🔧 Changes

- Timeline: fixed height 200px → resizable dengan min 150px.
- Grid structure: 4 rows → 3 rows (Workspace, Splitter, Timeline).
- Sidebar: fixed width 250px → resizable dengan min 200px, default 300px.
- Library: fixed width 200px → resizable dengan min 150px, default 220px.
- Control bar: single row → grouped dengan visual separators.
- Export: direct export → dialog dengan options (resolution, FPS, path).

---

## [v6.2.0] - 2026-04-22

### ✨ Features

- **ZeroMix WDM v2 (Window Desktop Manager)**: Dedicated WPF control panel (`!wdm`) untuk styling Windows shell — taskbar, notification panel, file explorer, start menu, dan desktop. Menggantikan sistem WDM lama yang berbasis terminal overlay.
- **WDM UI Modern**: Desain abu-abu gelap (VS Code-style) dengan sidebar kategori, status dot hijau/abu, badge ON/OFF, dan status bar aktif di bagian bawah.
- **Process-Aware Style Apply**: Start Menu dan Notification Panel kini di-apply via process filter (`StartMenuExperienceHost`, `ShellExperienceHost`) — bukan hanya class name. Lebih akurat di Windows 10.
- **SetWinEventHook Multi-Event**: Watcher kini listen ke 4 event sekaligus (`EVENT_OBJECT_SHOW`, `EVENT_SYSTEM_FOREGROUND`, `EVENT_OBJECT_REORDER`, `EVENT_OBJECT_NAMECHANGE`) + pulse timer 3 detik sebagai fallback. Taskbar tidak lagi reset saat diklik.
- **Floating macOS Style**: Style baru `FloatingMacOS` — taskbar fully transparent dengan DWM border glow cyan via `DwmSetWindowAttribute`.
- **Restore All**: Tombol "↺ Restore All" di WDM Window + command `!restore` di terminal. Reset semua style ke Windows default, stop watcher, hapus state JSON. Dilengkapi konfirmasi dialog dengan penjelasan bahwa tidak ada file sistem yang dimodifikasi.
- **Desktop Widget** (`!desktop`): WPF window yang embed ke WorkerW (desktop layer) — tampil di belakang semua window, di atas wallpaper. Menampilkan jam besar, tanggal, greeting macOS-style (Good morning/afternoon/evening), uptime sesi, dan pills CPU/RAM/Battery yang update tiap 4 detik.
- **ZeroLaunchpad** (`!startmenu`): macOS Launchpad-style app launcher — full screen blur backdrop, search bar Spotlight-style, app grid dari Start Menu shortcuts, fade in/out animation. Diaktifkan via global `WH_MOUSE_LL` mouse hook yang intercept klik Start button.
- **ClockWidget Weather API**: ClockWidget kini terintegrasi dengan Open-Meteo API (gratis, tanpa API key) — menampilkan suhu, kondisi cuaca, dan kecepatan angin. Lokasi otomatis via IP geolocation (`ip-api.com`). Update setiap 15 menit.
- **Layout Presets**: 4 preset siap pakai di WDM — macOS Dock, Minimal Dark, Cyberpunk, Classic Windows. Setiap preset apply kombinasi style ke semua kategori sekaligus.

### 🐛 Bug Fixes

- Fix `#endregion` duplikat di `ZeroShellWindow.xaml.cs` yang menyebabkan `CS1028` preprocessor error.
- Fix semua ambiguous reference (`Brushes`, `Color`, `ColorConverter`, `ComboBox`, `TextBox`, `CheckBox`, `Button`, `Cursors`, `Orientation`, `KeyEventArgs`, `Application`, `MessageBox`) di file WDM baru — resolved via explicit `using` aliases.
- Fix `LetterSpacing` tidak ada di WPF `TextBlock` — property dihapus.
- Fix `HorizontalAlignment.Center` instance reference error — diganti ke `System.Windows.HorizontalAlignment.Center`.
- Fix `Path` ambiguous antara `System.Windows.Shapes.Path` dan `System.IO.Path` — alias `IOPath` ditambahkan.
- Fix `RenderOptions` tidak bisa di-set via object initializer di WPF — diganti ke `RenderOptions.SetBitmapScalingMode()`.

### 🔧 Changes

- `ShellHelper.cs` di-refactor total: `ApplyBlur` dan `DisableAccent` dijadikan `public`, tambah `ApplyStyle(IntPtr, WdmEntry)`, `ApplyStyleToChildren`, `EnumAllWindows`, `ApplyStyleByProcess`, `ApplyStartMenuStyle`, `ApplyNotificationStyle`, `StartWatcher`, `StopWatcher`.
- Hapus dari `ShellHelper`: `RestoreAllWDM`, `ApplyTaskbarTransparency`, `ApplyExplorerTransparency`, `ApplyStartMenuGlass`, `ApplyNotificationPanelGlass`.
- Hapus dari terminal: `!glass`, `!hidico`, `!dlayer`, `WDMOptions` array, `_isSelectingWDM`, 5 bool WDM flags, `_wdmPulseTimer`.
- `WdmState.cs`: tambah `WdmStyle.FloatingMacOS`, `WdmCategories.ProcessMap`, `WdmPresets` (4 preset).
- State WDM disimpan sebagai `Dictionary<string, WdmEntry>` per window class name — lebih granular dari 5 bool flags sebelumnya.
- Auto-restore WDM state saat startup via `Window_Loaded` — apply ke window aktif + start watcher jika ada entry.
- Tab completion diupdate: tambah `!desktop`, `!startmenu`, `!restore`.

---

## [v6.1.0] - 2026-04-21

### ✨ Features

- **Custom Cursor System**: Ganti desain cursor secara instan langsung dari ZeroMix — upload file `.cur` / `.ani` atau pilih dari preset bawaan. Tidak perlu buka Mouse Properties Windows secara manual. Apply & revert dengan satu klik.
- **Bubble Translate Improvements**: Perbaikan lanjutan dari v6.0.0 — bubble kini muncul lebih konsisten di semua aplikasi termasuk browser dan game overlay. Posisi bubble mengikuti posisi kursor secara akurat.
- **Bubble Auto-Dismiss**: Bubble terjemahan otomatis hilang setelah 5 detik jika tidak ada interaksi — tidak lagi mengganggu layar.
- **Bubble Copy Result**: Klik hasil terjemahan di bubble untuk langsung copy ke clipboard.

### 🐛 Bug Fixes

- Fix bubble tidak muncul di aplikasi tertentu yang override clipboard event — polling interval dioptimasi dari 500ms ke 300ms.
- Fix bubble muncul di posisi yang salah saat layar memiliki DPI scaling — koordinat kursor kini di-scale dengan benar via `GetCursorPos` + DPI factor.
- Fix bubble tidak hilang saat user pindah ke window lain — tambah `Deactivated` event handler.
- Fix cursor state tidak tersimpan saat aplikasi crash — state di-flush ke disk setiap kali apply.

### 🔧 Changes

- Cursor state disimpan ke `%AppData%\ZeroMix\cursor_config.json`.
- Revert cursor ke default Windows via `SystemParametersInfo(SPI_SETCURSORS)`.
- Bubble polling: 500ms → 300ms untuk respons lebih cepat.
- Bubble window: `Topmost = true`, `ShowInTaskbar = false`, `IsHitTestVisible = false` kecuali saat hover.

---

## [v6.0.1] - 2026-04-20

### 🐛 Bug Fixes

- Fix `Mutex.ReleaseMutex()` crash saat shutdown pada beberapa konfigurasi sistem.
- Fix Virtual Assistant `CoreWebView2 disposed` exception saat window ditutup cepat setelah startup.
- Fix Bubble translate tidak muncul setelah restart — `_lastClipboard` tidak di-reset dengan benar.
- Fix language preference tidak tersimpan saat path `BaseDirectory` read-only — fallback ke `%AppData%\ZeroMix\`.
- Fix onboarding `FindResource("NavSelectedBrush")` throw exception saat resource tidak ditemukan — diganti hardcode warna.

### 🔧 Changes

- Onboarding changelog dimuat lazy (hanya saat slide 5 dibuka) — startup lebih cepat.
- `ZeroMix-Updater.exe` kini di-bundle langsung di installer, menggantikan `zeromix-update.ps1` dan `.bat`.

---

## [v6.0.0] - 2026-04-19

### ✨ Features

- **Bubble Translate (Ctrl+C)**: Fitur Selection Bubble kini stabil — highlight teks → Ctrl+C → bubble terjemahan muncul otomatis di posisi kursor. Polling clipboard 500ms, support semua aplikasi.
- **Keyboard Translate Toggle**: Keyboard hook sekarang dikontrol penuh oleh toggle — tidak aktif jika checkbox tidak dicentang. Tidak ada lagi translate yang jalan diam-diam di background.
- **Swap Language Button Fix**: Tombol `⇄` di Nexus Translator kini berfungsi dengan benar — swap via `Tag` matching bukan `SelectedIndex` yang tidak reliable di custom ComboBox template.
- **Single Instance Fix (Mutex)**: Perbaikan race condition di Mutex guard — pakai `EnumWindows` untuk cari window handle saat `MainWindowHandle` = zero (proses suspended), mencegah multiple instance ZeroMix berjalan bersamaan.
- **Onboarding Optimization**: Changelog di onboarding dimuat lazy — hanya saat user membuka slide 5, bukan saat startup. Mengurangi waktu buka onboarding secara signifikan.
- **ZeroMix Updater (Built-in)**: Ganti `zeromix-update.ps1` + `.bat` dengan `ZeroMix-Updater.exe` — WPF window modern dengan progress bar, speed indicator, dan auto-launch installer. Di-bundle langsung di `Tools/Updater/`, tidak perlu download terpisah.
- **Virtual Assistant Bundle Fix**: File model Live2D, thumbnail, dan HTML viewer kini selalu ikut ter-bundle di installer via `Setup.iss`.

### 🐛 Bug Fixes

- Fix Bubble tidak muncul — `BubbleModeSwitch` terhubung ke event yang salah, `SelectionBubble` tidak pernah diinisialisasi.
- Fix Bubble teks sama tidak bisa translate ulang — `_lastClipboard` tidak di-reset setelah bubble ditampilkan.
- Fix Keyboard translate jalan walau toggle tidak dicentang — hook dipasang di constructor, sekarang hanya via `EnableKeyboardHook()`.
- Fix ZeroMix Suspended di Task Manager — Mutex check gagal saat `MainWindowHandle` = zero, ditambahkan `EnumWindows` fallback.
- Fix Onboarding `FindResource` exception — diganti hardcode warna langsung.
- Fix Virtual Assistant tidak ikut installer — ditambahkan entry eksplisit di `Setup.iss`.

### 🔧 Changes

- Bubble: kembali ke polling clipboard 500ms — lebih stabil dari global mouse hook + `SendInput`.
- Keyboard Hook: dipisah ke `EnableKeyboardHook()` / `DisableKeyboardHook()` agar dikontrol dari UI.
- `-ForceBuild` flag ditambahkan ke `release-version.ps1` untuk re-push tag tanpa bump versi.
- GitHub Actions: tambah step build `ZeroMix.Updater`, copy ke `publish/win-x64/Tools/Updater/`.

---

## [v5.8.0]

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
