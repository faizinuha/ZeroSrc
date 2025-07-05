; --- Informasi Aplikasi ---
[Setup]
AppName=Google Box
AppVersion=1.0.5
AppVerName=Google-Box 1.0.5
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
WizardImageFile=Log.bmp

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

; --- Teks Custom Welcome & Selesai ---
[Messages]
WelcomeLabel1=Selamat datang di penginstal Google Box!
WelcomeLabel2=Aplikasi pintar untuk membuka web dan aplikasi desktop dengan cepat.
FinishedLabel=Google Box berhasil diinstal. Kamu bisa menjalankannya dari desktop atau Start Menu.
