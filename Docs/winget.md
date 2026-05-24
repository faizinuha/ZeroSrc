Windows Installer (winget) — Petunjuk singkat untuk workflow dan manifest

Ringkasan langkah untuk publish ke winget via GitHub Actions

1) Persiapkan paket (MSIX/ZIP/Installer)
- Untuk winget, prefer format MSIX atau installer .exe/.msi yang memiliki silent install switches.
- Pastikan versi di nuspec/installer sesuai semver.

2) Menambahkan manifest manifest/ dan metadata
- Gunakan wingetcreate atau winget-pkgs repo untuk membuat manifest.
- Aturan utama: PackageIdentifier harus unik (misal: "com.faizinuha.zeromix") dan versi harus cocok dengan tag rilis.

3) GitHub Actions workflow (contoh minimal)
- Buat job yang build release artifacts and upload a release asset.
- Setelah release dibuat, jalankan step yang memakai winget-publish action atau membuat PR ke winget-pkgs.

Contoh snippet (pseudo):

jobs:
  release:
    runs-on: windows-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'

      - name: Build Release
        run: dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true -o out

      - name: Create GitHub Release and upload installer
        uses: ncipollo/release-action@v1
        with:
          artifacts: out\**\*

      - name: Publish to winget (manual or PR)
        # Either call an action that files a PR to winget-pkgs or run wingetcreate with credentials
        run: echo "Manual: create PR to winget-pkgs or use winget-publish action"

4) Workflow tips for winget
- Automate creation of installer artifact and attach to Release.
- Use tags/releases to trigger winget submission job.
- For automated PR to winget-pkgs, use a bot account with fork + PR permissions.

5) docs/winget checklist for repo
- Add docs/winget.md (this file)
- Document where artifacts are created and naming convention
- Document required installer switches for silent install (e.g., /S or /quiet)
- Provide the PackageIdentifier and publisher information

Jika ingin, saya bisa:
- Menambahkan a sample GitHub Actions workflow file .github/workflows/winget-publish.yml
- Membuat MSIX packaging steps or NSIS script for installer

