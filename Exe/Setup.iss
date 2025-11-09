; ==============================================================================
; ZeroMix Installer Script (Revisi Profesional)
; ==============================================================================

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

; ⭐ PERBAIKAN: Penutupan Aplikasi Otomatis saat Upgrade/Uninstal
CloseApplications=yes
CloseApplicationsFilter=ZeroMix.exe

; Digital signature settings (Pastikan file dan password benar)
SignTool= bin\osslsigncode.exe
SignToolParameters=sign -pkcs12 "ZeroMixCert.pfx" -pass "ZeroMixPass" -n "ZeroMix Installer" -i "https://zeromix.vercel.app" -t "http://timestamp.digicert.com" $f

; --- File yang akan diinstal ---
[Files]
; PERHATIAN: Pastikan path ini benar mengarah ke output 'dotnet publish' self-contained.
Source: "..\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "zeromix.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\Resource\*"; DestDir: "{app}\Resource"; Flags: ignoreversion recursesubdirs createallsubdirs
; Source: "..\LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion  ; Contoh file lisensi yang lebih umum

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
; ⭐ PERBAIKAN: Hapus penghapusan AppData dari sini. Biarkan hanya folder instalasi utama.
Type: filesandordirs; Name: "{app}"

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
; Sesuaikan ini dengan path ke file lisensi yang sebenarnya (bukan SECURITY.md)
InfoBeforeFile=../LICENSE.txt 

; --- Kode Pascal Script untuk Logika Lanjut ---
[Code]
// Fungsi yang dipanggil sebelum uninstal
function InitializeUninstall(): Boolean;
var
  ResultCode: Integer;
begin
  // Tampilkan dialog konfirmasi sebelum uninstal
  if MsgBox('Apakah Anda yakin ingin meng-uninstall ZeroMix?', mbConfirmation, MB_YESNO) = IDYES then
  begin
    // ⭐ PERBAIKAN: Penutupan proses paksa (hanya jika CloseApplications gagal)
    // Jalankan taskkill secara tersembunyi.
    Exec('taskkill.exe', '/IM ZeroMix.exe /F', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Result := True; // Jika pengguna memilih 'Yes', lanjutkan uninstall
  end
  else
    Result := False; // Jika pengguna memilih 'No', batalkan uninstall
end;

// Fungsi yang dipanggil ketika langkah instalasi berubah
procedure CurStepChanged(CurStep: TSetupStep);
var
  ErrorCode: Integer;
begin
  if CurStep = ssDone then
  begin
    // ⭐ PERBAIKAN UX: Hapus MsgBox pop-up. Langsung buka Thank You page.
    ShellExec('open', 'https://zeromix.vercel.app/ThanksYou.html', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
  end;
end;

// Fungsi yang dipanggil ketika langkah uninstal berubah
procedure CurUninstallStepChanged(CurStep: TUninstallStep);
var
  ErrorCode: Integer;
begin
  // ⭐ PERBAIKAN KRITIS: Opsi Hapus Data Pengguna (AppData)
  // Dilakukan pada langkah usUninstall sebelum file dihapus secara fisik
  if CurStep = usUninstall then
  begin
    if MsgBox('ZeroMix akan segera dihapus. Apakah Anda ingin **menghapus data konfigurasi pengguna** (pengaturan, dll.) dari AppData?', mbConfirmation, MB_YESNO) = IDYES then
    begin
      // Hapus folder AppData secara rekursif dan paksa
      DelTree(ExpandConstant('{userappdata}\ZeroMix'), True, True, True);
    end;
  end;

  if CurStep = usPostUninstall then
  begin
    // Buka halaman Feedback setelah proses uninstal selesai
    ShellExec('open', 'https://zeromix.vercel.app/Feedback.html', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
  end;
end;