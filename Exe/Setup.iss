; --- Informasi Aplikasi & Penanda Tangan (Semua di dalam [Setup]) ---
[Setup]
AppName=ZeroMix
AppVersion=2.0.0
VersionInfoVersion=2.0.0.0
VersionInfoCompany=Zaki
VersionInfoDescription=ZeroMix smart launcher
VersionInfoTextVersion=2.0.0.0
VersionInfoProductVersion=2.0.0.0
SetupIconFile=zeromix.ico
AppVerName=ZeroMix
AppPublisher=Frieren
AppPublisherURL=Mardve7.vercel.app
AppCopyright=Copyright (c) 2025
AppComments=ZeroMix smart launcher
DefaultDirName={pf}\ZeroMix
DefaultGroupName=ZeroMix
AllowNoIcons=yes
OutputDir=.
OutputBaseFilename=ZeroMix-Setup
SetupIconFile=zeromix.ico
Compression=lzma
SolidCompression=yes
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\zeromix.ico
WizardImageFile=zeromix.bmp
WizardSmallImageFile=zeromix.bmp
WizardStyle=modern
; Digital signature settings
SignTool= bin\osslsigncode.exe
SignToolParameters=sign -pkcs12 "ZeroMixCert.pfx" -pass "ZeroMixPass" -n "ZeroMix Installer" -i "https://zeromix.vercel.app" -t "http://timestamp.digicert.com" $f

; NOTE for packagers:
; To avoid requiring users to install the .NET runtime, publish your app as
; a self-contained single-file for the target runtime (e.g. win-x64) and point
; the [Files] section below at the publish output directory.
; Example command (from project root):
; dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -p:PublishTrimmed=true --self-contained true -o ..\publish\win-x64
; Then run Inno Setup using this script; the installer will include the self-contained exe and dependencies.
; Optionally sign both ZeroMix.exe and the installer with a code signing certificate to reduce false-positive antivirus flags.

; --- File yang akan diinstal ---
[Files]
; Use published self-contained output (see NOTE above). Adjust runtime id (win-x64/win-x86) as needed.
; The Inno Setup compiler resolves relative paths from the script's folder.
; To avoid "Source not found" errors, copy your publish output into the Exe\publish\win-x64 folder
; (from project root: dotnet publish ... -o ./publish/win-x64), or adjust the path here to the correct publish location.
Source: "..\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "zeromix.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\Resource\*"; DestDir: "{app}\Resource"; Flags: ignoreversion recursesubdirs createallsubdirs

; --- Shortcut ---
[Icons]
Name: "{group}\ZeroMix"; Filename: "{app}\ZeroMix.exe"; IconFilename: "{app}\zeromix.ico"
Name: "{commondesktop}\ZeroMix"; Filename: "{app}\ZeroMix.exe"; IconFilename: "{app}\zeromix.ico"; Tasks: desktopicon

; --- Pilihan Tambahan ---
[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"

; --- Jalankan aplikasi setelah install ---
[Run]
Filename: "{app}\ZeroMix.exe"; Description: "Jalankan ZeroMix"; Flags: nowait postinstall skipifsilent

; --- Bersihkan file saat uninstall ---
[UninstallDelete]
Type: filesandordirs; Name: "{app}"
; Hapus folder konfigurasi dari AppData pengguna
Type: filesandordirs; Name: "{userappdata}\ZeroMix"

; --- Tutup proses saat uninstall ---
[UninstallRun]
Filename: "taskkill.exe"; Parameters: "/IM ZeroMix.exe /F"; StatusMsg: "Menutup aplikasi..."; Flags: runhidden
Filename: "cmd.exe"; Parameters: "/C echo Terima kasih telah menggunakan! && timeout /t 3"; Flags: runhidden


; --- Teks Custom Welcome & Selesai ---
[Messages]
WelcomeLabel1=Selamat datang di Program Penginstal ZeroMix
WelcomeLabel2=Program ini akan menginstal ZeroMix di komputer Anda.%n%nZeroMix adalah aplikasi pintar yang memungkinkan Anda membuka web dan aplikasi desktop dengan cepat dan efisien.%n%nDisarankan untuk menutup semua aplikasi lain sebelum melanjutkan.
FinishedLabel=ZeroMix telah berhasil diinstal di komputer Anda.%n%nSilakan tekan Selesai untuk keluar dari Program Penginstal.
FinishedHeadingLabel=Penyelesaian Penginstalan ZeroMix
AboutSetupNote=Program Penginstal ZeroMix dibuat dengan Inno Setup.%nInno Setup tersedia secara gratis dari jrsoftware.org.

[CustomMessages]
LaunchProgram=&Jalankan ZeroMix
AdditionalTasks=Tugas tambahan:
WindowsServiceNote=Layanan Windows:

[LicenseFile]
InfoBeforeFile=../SECURITY.md

[Code]


procedure CurStepChanged(CurStep: TSetupStep);
var
 ErrorCode: Integer;
begin
 if CurStep = ssDone then
 begin
  MsgBox('Terima kasih sudah menginstall ZeroMix!' + #13#10 + 'Dukungan Anda sangat berarti bagi kami.', mbInformation, MB_OK);
  // Open the thank you page in the user's default web browser
  // khusus 
  ShellExec('open', 'https://zeromix.vercel.app/ThanksYou.html', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
 end;
end;

function InitializeUninstall(): Boolean;
begin
  // Menampilkan dialog konfirmasi sebelum uninstall
  if MsgBox('Apakah Anda yakin ingin meng-uninstall ZeroMix?', mbConfirmation, MB_YESNO) = IDYES then
    Result := True // Jika pengguna memilih 'Yes', lanjutkan uninstall
  else
    Result := False; // Jika pengguna memilih 'No', batalkan uninstall
end;

procedure CurUninstallStepChanged(CurStep: TUninstallStep);
var
ErrorCode: Integer;
begin
 if CurStep = usPostUninstall then
 begin
  ShellExec('open', 'https://zeromix.vercel.app/Feedback.html', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
 end;
end;