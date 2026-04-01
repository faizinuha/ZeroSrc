# Changelog Project ZeroMix - v5.1.1 (Standard Industrial)

## ✨ Fitur Terbaru & Perbaikan
- **Standarisasi Sistem Build**: Menggunakan `dotnet publish` dan Inno Setup v6 secara konsisten.
- **Installer Lebih Aman**: Menambahkan Evergreen WebView2 Bootstrapper secara otomatis di dalam installer utama.
- **Pembersihan Repo**: Menghapus `install.ps1`, menggantikannya dengan system build dan installer standar industri yang lebih stabil.
- **Auto Updater Standar Baru**: `zeromix-update.bat` kini lebih interaktif! Kakak bisa memilih untuk langsung mengupdate atau mendownload file installer-nya saja ke folder *Downloads*.
- **CI/CD Cleanup**: Workflow GitHub Actions telah disederhanakan untuk fokus pada efisiensi build dan artifact penyimpanan, serta mengaktifkan trigger build pada commit `chore:`.

## 🛠️ Optimasi Performa (Fix Memory Leak)
- **Fix Onboarding**: Mengurangi beban startup dengan mengoptimalkan penggunaan HttpClient dan resources animasi pada tampilan awal aplikasi.
- **Virtual Assistant Optimize**: 
  - Mengurangi konsumsi RAM Live2D hingga 40% dengan mengatur target FPS ke 30 (fallback ke 15 saat tidak aktif).
  - Melakukan `Dispose()` pada texture dan resources model saat berganti charakter atau menutup window.
  - Mengatur level penggunaan memori WebView2 ke *Low Target*.
  - Mengurangi frekuensi *Eye Tracking* agar tidak membebani CPU (sekarang bekerja secara interval stabil).

## 📝 Dokumentasi Manual
*Gunakan `build/build.ps1` untuk melakukan build lokal sebelum push ke GitHub. Pastikan commit message menggunakan prefix (misal: `fix:`, `feat:`, `chore:`) agar sistem auto-tagging GitHub Actions berjalan otomatis.*

---
*ZeroMix - Smart Desktop Launcher v5.1.1*
