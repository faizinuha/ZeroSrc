; --- Informasi Aplikasi ---
[Setup]
AppName=Google Box
AppVersion=1.1.0
AppVerName=Google-Box
AppPublisher=Vibes
AppPublisherURL=https://mardev-v8.vercel.app/
AppSupportURL=https://mardev-v8.vercel.app/
AppUpdatesURL=https://mardev-v8.vercel.app/
AppCopyright=Copyright (c) 2023 Vibes
AppComments=Google Box is a smart launcher for easy access to web and desktop apps.
DefaultDirName={commonpf}\GoogleBox
DefaultGroupName=Google Box
OutputDir=.
OutputBaseFilename=Google-Box
SetupIconFile=favicon.ico
Compression=lzma
SolidCompression=yes
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\favicon.ico
WizardImageFile=Logs.bmp

; --- File yang akan diinstal ---
[Files]
Source: "..\bin\Debug\net9.0-windows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "favicon.ico"; DestDir: "{app}"; Flags: ignoreversion

; --- Shortcut ---
[Icons]
Name: "{group}\Google Box"; Filename: "{app}\ZeroSrc.exe"; IconFilename: "{app}\favicon.ico"
Name: "{commondesktop}\Google Box"; Filename: "{app}\ZeroSrc.exe"; IconFilename: "{app}\favicon.ico"; Tasks: desktopicon

; --- Pilihan Tambahan ---
[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"

; --- Jalankan aplikasi setelah install ---
[Run]
Filename: "{app}\ZeroSrc.exe"; Description: "Jalankan Google Box"; Flags: nowait postinstall skipifsilent

; --- Bersihkan file saat uninstall ---
[UninstallDelete]
Type: filesandordirs; Name: "{app}"
Type: filesandordirs; Name: "{userappdata}\GoogleBox"

; --- Tutup proses saat uninstall ---
[UninstallRun]
Filename: "taskkill.exe"; Parameters: "/IM ZeroSrc.exe /F"; StatusMsg: "Menutup aplikasi Google Box..."; Flags: runhidden
Filename: "cmd.exe"; Parameters: "/C echo Terima kasih telah menggunakan Google Box! && timeout /t 3"; Flags: runhidden
IconFilename: "{app}\favicon.ico"; Description: "Google Box Uninstaller"; Flags: runhidden

; --- Pesan Instalasi ---
[Messages]
InstallTitle=Instalasi Google Box
InstallMessage=Instalasi Google Box sedang berlangsung. Mohon tunggu...
InstallMessage2=Proses ini mungkin memakan waktu beberapa menit tergantung pada kecepatan sistem kamu.
; --- Pesan Uninstall ---
[UninstallMessages]
UninstallTitle=Penghapusan Google Box
UninstallMessage=Penghapusan Google Box sedang berlangsung. Mohon tunggu...
UninstallMessage2=Proses ini mungkin memakan waktu beberapa menit tergantung pada kecepatan sistem kamu.


; --- Pesan Selamat Datang dan Selesai ---
[Welcome]
WelcomeTitle=Selamat datang di Google Box Installer
; --- Teks Custom Welcome & Selesai ---
[Messages]
WelcomeLabel1=Selamat datang di penginstal Google Box!
WelcomeLabel2=Aplikasi pintar untuk membuka web dan aplikasi desktop dengan cepat.
FinishedLabel=Google Box berhasil diinstal. Kamu bisa menjalankannya dari desktop atau Start Menu.
FinishedMessage=Terima kasih telah menggunakan Google Box! Jika ada pertanyaan, kunjungi https://github.com/faizinuha/ZeroSrc/issues.
FinishedMessage2=Jangan lupa untuk memberikan ulasan di https://github.com/faizinuha/ZeroSrc/issues jika kamu menyukai aplikasi ini!
; --- Pesan Peringatan ---
[WarningMessages]
WarningUninstall=Apakah kamu yakin ingin menghapus Google Box? Semua data akan dihapus.
WarningUninstallData=Ini akan menghapus semua data aplikasi. Pastikan kamu sudah mencadangkan data penting sebelum melanjutkan.
; --- Pesan Konfirmasi ---
[ConfirmationMessages]
ConfirmationUninstall=Kamu akan menghapus Google Box. Apakah kamu yakin ingin melanjutkan?
ConfirmationUninstallData=Ini akan menghapus semua data aplikasi. Pastikan kamu sudah mencadangkan data penting sebelum melanjutkan.
; --- Pesan Informasi Tambahan ---
[AdditionalInfoMessages]
AdditionalInfo=Google Box adalah aplikasi peluncur pintar yang memudahkan akses ke aplikasi web dan desktop. Dengan antarmuka yang sederhana, kamu bisa membuka aplikasi favorit dengan cepat.
AdditionalInfo2=Jika kamu mengalami masalah, kunjungi https://github.com/faizinuha/ZeroSrc/issues untuk mendapatkan bantuan. Kami selalu siap membantu!
; --- Pesan Penutup ---
[ClosingMessages]
ClosingMessage=Terima kasih telah menginstal Google Box! Kami harap kamu menikmati pengalaman menggunakan aplikasi ini.
ClosingMessage2=Jika kamu menyukai aplikasi ini, jangan lupa untuk memberikan ulasan di https://github.com/faizinuha/ZeroSrc/issues. Umpan balik kamu sangat berarti bagi kami!
ClosingMessage3=Jangan ragu untuk menghubungi kami di https://github.com/faizinuha/ZeroSrc/issues jika ada pertanyaan atau saran. Kami selalu siap membantu!
; --- Pesan Pemberitahuan ---
[NotificationMessages]
NotificationUpdateAvailable=Versi baru Google Box tersedia! Kunjungi https://github.com/faizinuha/ZeroSrc/issues untuk memperbarui.
NotificationNewFeature=Fitur baru telah ditambahkan! Cek https://github.com/faizinuha/ZeroSrc/issues untuk informasi lebih lanjut.
