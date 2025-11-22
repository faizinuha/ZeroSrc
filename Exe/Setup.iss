; ==============================================================================
; ZeroMix Installer Script (Revisi Profesional v2.1)
; ==============================================================================

[Setup]
AppName=ZeroMix
AppVersion=2.1.0
VersionInfoVersion=2.1.1.3
VersionInfoCompany=ZeroMix Development
VersionInfoDescription=ZeroMix - Smart Desktop Launcher & System Utilities
VersionInfoTextVersion=2.1.0.0
VersionInfoProductVersion=2.1.0.0
AppVerName=ZeroMix v2.1.0
AppPublisher=ZeroMix Team
AppPublisherURL=https://zeromix.vercel.app
AppCopyright=Copyright (c) 2025 - All Rights Reserved
AppComments=Smart launcher with system monitoring, clock widget, and wallpaper manager

; Installation Configuration
DefaultDirName={pf}\ZeroMix
DefaultGroupName=ZeroMix
AllowNoIcons=yes
OutputDir=.
OutputBaseFilename=ZeroMix-Setup-v2.1.0
Compression=lzma
SolidCompression=yes
DisableProgramGroupPage=no
UninstallDisplayIcon={app}\zeromix.ico

; UI Configuration
WizardStyle=modern
SetupIconFile=zeromix.ico
WizardImageFile=zeromix.bmp
WizardSmallImageFile=zeromix.bmp
WizardResizable=yes
PrivilegesRequired=admin

; Auto-close running instances
CloseApplications=yes
CloseApplicationsFilter=ZeroMix.exe
RestartIfNeededByRun=yes

; Performance & Behavior
ArchitecturesInstallIn64BitMode=x64
ArchitecturesAllowed=x64
MinVersion=10.0.19041
VersionInfoProductTextVersion=2.1.0

; --- File yang akan diinstal ---
[Files]
; Main application from publish folder
Source: "..\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Application icon
Source: "zeromix.ico"; DestDir: "{app}"; Flags: ignoreversion
; Resource files (wallpapers, images, videos)
Source: "..\Resource\*"; DestDir: "{app}\Resource"; Flags: ignoreversion recursesubdirs createallsubdirs
; License file
Source: "..\LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion
; README
Source: "..\Readme.md"; DestDir: "{app}"; Flags: ignoreversion
; Update Checker CLI
Source: "..\ZeroMixUpdateCli\bin\Release\net9.0\win-x64\publish\zeromix-update.exe"; DestDir: "{app}\bin"; Flags: ignoreversion
; CLI Wrapper
Source: "..\ZeroMixUpdateCli\zeromix.bat"; DestDir: "{app}\bin"; Flags: ignoreversion

; --- Shortcut ---
[Icons]
; Start Menu shortcut (main entry)
Name: "{group}\ZeroMix"; Filename: "{app}\ZeroMix.exe"; IconFilename: "{app}\zeromix.ico"; WorkingDir: "{app}"
; Desktop shortcut (optional via Tasks)
Name: "{commondesktop}\ZeroMix"; Filename: "{app}\ZeroMix.exe"; IconFilename: "{app}\zeromix.ico"; WorkingDir: "{app}"; Tasks: desktopicon; Flags: createonlyiffileexists
; Uninstall shortcut in Start Menu
Name: "{group}\Uninstall ZeroMix"; Filename: "{uninstallexe}"

; --- Pilihan Tambahan ---
[Tasks]
; Desktop shortcut option
Name: "desktopicon"; Description: "Buat &desktop shortcut"; GroupDescription: "Shortcut:"; Flags: unchecked
; Auto-launch on startup
Name: "startup"; Description: "Jalankan ZeroMix saat Windows &startup"; GroupDescription: "Startup:"; Flags: unchecked

; --- Jalankan aplikasi setelah install ---
[Run]
; Launch ZeroMix after installation (optional)
Filename: "{app}\ZeroMix.exe"; Description: "&Jalankan ZeroMix sekarang"; Flags: nowait postinstall skipifsilent; Tasks: ; Check: not CurTaskExists('autostart')
; Create registry entry for startup
Filename: "reg.exe"; Parameters: "add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Run"" /V ""ZeroMix"" /t REG_SZ /D ""{app}\ZeroMix.exe"" /F"; Tasks: startup; Flags: runhidden
; Add to PATH
Filename: "cmd.exe"; Parameters: "/c setx PATH ""%PATH%;{app}\bin"""; Flags: runhidden

; --- Bersihkan file saat uninstall ---
[UninstallDelete]
; ⭐ PERBAIKAN: Hapus penghapusan AppData dari sini. Biarkan hanya folder instalasi utama.
Type: filesandordirs; Name: "{app}"

; --- Teks Custom Welcome & Selesai ---
[Messages]
; Welcome and info
WelcomeLabel1=Selamat datang di Program Penginstal ZeroMix v2.1.0
WelcomeLabel2=Program ini akan menginstal ZeroMix, aplikasi smart launcher dengan fitur sistem monitoring, clock widget, dan wallpaper manager.%n%nZeroMix memungkinkan Anda membuka web dan aplikasi desktop dengan cepat dan efisien.%n%n⚠️ Disarankan untuk menutup semua aplikasi lain sebelum melanjutkan penginstalan.
; Installation complete
FinishedLabel=ZeroMix telah berhasil diinstal di komputer Anda!%n%nSilakan tekan tombol 'Selesai' untuk menutup Program Penginstal.
FinishedHeadingLabel=Penyelesaian Penginstalan ZeroMix
; General messages
AboutSetupNote=ZeroMix Setup v2.1.0 dibuat menggunakan Inno Setup%nInno Setup tersedia secara gratis dari https://jrsoftware.org
; Directory settings
SelectDirLabel3=Pilih folder tempat Program Penginstal akan menginstal ZeroMix:
InvalidPath=Anda harus memasukkan path lengkap dengan drive letter; contoh: C:\
DiskSpaceMbLabel=Ruang disk bebas yang diperlukan minimal [1] MB
; Program start error
ExistingFileError1=File %1 sudah ada. Pengguna tidak dapat menimpa file di lokasi lain.

[CustomMessages]
; Custom action descriptions
LaunchProgram=&Jalankan ZeroMix sekarang
AdditionalTasks=Tugas tambahan:
WindowsServiceNote=Layanan Windows:
; Startup task description (Indonesian)
id.StartupDescription=Jalankan ZeroMix saat Windows startup

[LicenseFile]
; Display license during installation
LicenseFile=../LICENSE.txt

[InfoBefore]
; Display info before installation starts
InfoBeforeFile=../Readme.md 

; --- Kode Pascal Script untuk Logika Lanjut ---
[Code]
// Variabel global
var
  WasRunning: Boolean;

// Fungsi yang dipanggil pada awal instalasi
function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;
  WasRunning := False;
  
  // Cek jika ZeroMix sudah berjalan
  if FindWindowByWindowName('ZeroMix') <> 0 then
  begin
    WasRunning := True;
    MsgBox('ZeroMix sedang berjalan. Aplikasi akan ditutup secara otomatis untuk melanjutkan instalasi.', mbInformation, MB_OK);
    // Taskkill will handle by CloseApplications setting
  end;
  
  Result := True;
end;

// Fungsi yang dipanggil sebelum uninstal
function InitializeUninstall(): Boolean;
var
  ResultCode: Integer;
begin
  // Tampilkan dialog konfirmasi sebelum uninstal
  if MsgBox('Apakah Anda yakin ingin meng-uninstall ZeroMix?' + #13 + #13 + 'Semua file aplikasi akan dihapus.', mbConfirmation, MB_YESNO) = IDYES then
  begin
    // Penutupan proses paksa (backup untuk CloseApplications)
    Exec('taskkill.exe', '/IM ZeroMix.exe /F', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Result := True; // Lanjutkan uninstall
  end
  else
    Result := False; // Batalkan uninstall
end;

// Fungsi yang dipanggil ketika langkah instalasi berubah
procedure CurStepChanged(CurStep: TSetupStep);
var
  ErrorCode: Integer;
begin
  // Setelah instalasi selesai
  if CurStep = ssDone then
  begin
    // Setup CLI tools
    RegisterCliTools();
    
    // Langsung buka halaman terima kasih (tanpa pop-up)
    if MsgBox('Instalasi ZeroMix selesai! Apakah Anda ingin membuka halaman terima kasih kami?', mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open', 'https://zeromix.vercel.app/ThanksYou.html', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
    end;
  end;
end;

// Fungsi yang dipanggil ketika langkah uninstal berubah
procedure CurUninstallStepChanged(CurStep: TUninstallStep);
var
  ErrorCode: Integer;
begin
  // Selama proses uninstal
  if CurStep = usUninstall then
  begin
    // Unregister CLI tools
    UnregisterCliTools();
    
    // Tanya apakah pengguna ingin menghapus data konfigurasi
    if MsgBox('Apakah Anda ingin **menghapus data konfigurasi dan pengaturan** ZeroMix dari AppData?', mbConfirmation, MB_YESNO) = IDYES then
    begin
      // Hapus folder AppData secara rekursif
      DelTree(ExpandConstant('{userappdata}\ZeroMix'), True, True, True);
    end;
  end;

  // Setelah uninstal selesai
  if CurStep = usPostUninstall then
  begin
    // Tanya apakah pengguna ingin memberikan feedback
    if MsgBox('Terima kasih telah menggunakan ZeroMix! Apakah Anda ingin memberikan feedback?', mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open', 'https://zeromix.vercel.app/Feedback.html', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
    end;
  end;
end;

// Helper function - Cari window berdasarkan nama
function FindWindowByWindowName(WindowName: string): HWND;
external 'FindWindowW@user32.dll stdcall';

// Check if a task should be executed
function CurTaskExists(const TaskName: String): Boolean;
begin
  Result := WizardIsTaskSelected(TaskName);
end;

// Register CLI tools untuk PATH dan shell command
procedure RegisterCliTools();
var
  ResultCode: Integer;
begin
  // Add {app}\bin to PATH via setx
  Exec('cmd.exe', '/c setx PATH "%PATH%;' + ExpandConstant('{app}\bin') + '"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  
  // Register "zeromix" command untuk shell
  try
    // Create registry entry untuk shell command
    if RegWriteStringValue(HKEY_LOCAL_MACHINE, 
      'SYSTEM\CurrentControlSet\Control\Session Manager\Environment', 
      'ZEROMIX_HOME', 
      ExpandConstant('{app}')) then
    begin
      // Success
    end;
  except
    // Ignore errors
  end;
end;

// Unregister CLI tools
procedure UnregisterCliTools();
begin
  try
    RegDeleteValue(HKEY_LOCAL_MACHINE, 
      'SYSTEM\CurrentControlSet\Control\Session Manager\Environment', 
      'ZEROMIX_HOME');
  except
    // Ignore errors
  end;
end;