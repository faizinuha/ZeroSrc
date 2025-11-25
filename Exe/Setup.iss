; ==============================================================================
; ZeroMix Installer Script (Fixed Professional v2.2)
; ==============================================================================

[Setup]
; --- PENTING: AppId Unik (Dibuat Baru) ---
AppId={{A1B2C3D4-E5F6-7890-ZEROMIX-IDENTIFIER}}
AppName=ZeroMix
AppVersion=2.2.2
VersionInfoVersion=2.2.2.0
VersionInfoCompany=Frieren
VersionInfoDescription=ZeroMix - Smart Desktop Launcher & System Utilities
VersionInfoTextVersion=2.2.2.0
VersionInfoProductVersion=2.2.2.0
AppVerName=ZeroMix v2.2.2
AppPublisher=ZeroMix Team
AppPublisherURL=https://zeromix.vercel.app
AppCopyright=Copyright (c) 2025 - All Rights Reserved

; Installation Configuration
DefaultDirName={pf}\ZeroMix
DefaultGroupName=ZeroMix
AllowNoIcons=yes
OutputDir=.
OutputBaseFilename=ZeroMix-Setup-v2.2.2
Compression=lzma2
SolidCompression=yes
DisableProgramGroupPage=no
UninstallDisplayIcon={app}\zeromix.ico

; UI Configuration
WizardStyle=modern
; Pastikan file .ico dan .bmp ada di folder yang sama dengan script .iss
SetupIconFile=zeromix.ico
; WizardImageFile=zeromix.bmp 
; WizardSmallImageFile=zeromix.bmp
WizardResizable=yes
PrivilegesRequired=admin

; Auto-close & Performance
CloseApplications=yes
; Filter ini akan menutup 'ZeroMix.exe' yang sedang berjalan
CloseApplicationsFilter=ZeroMix.exe
RestartIfNeededByRun=yes
ArchitecturesInstallIn64BitMode=x64
ArchitecturesAllowed=x64
MinVersion=10.0.19041

[Files]
; Main application
Source: "..\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Icon
Source: "zeromix.ico"; DestDir: "{app}"; Flags: ignoreversion
; Resources
Source: "..\Resource\*"; DestDir: "{app}\Resource"; Flags: ignoreversion recursesubdirs createallsubdirs
; Docs
Source: "..\LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\Readme.md"; DestDir: "{app}"; Flags: ignoreversion

; CLI Tools
Source: "..\ZeroMixUpdateCli\index.js"; DestDir: "{app}\bin"; Flags: ignoreversion
Source: "..\ZeroMixUpdateCli\package.json"; DestDir: "{app}\bin"; Flags: ignoreversion
Source: "..\ZeroMixUpdateCli\zeromix-cli.bat"; DestDir: "{app}\bin"; Flags: ignoreversion

; FFmpeg Tools
Source: "..\FFMPEG\ffmpeg.exe"; DestDir: "{app}\FFMPEG"; Flags: ignoreversion
Source: "..\FFMPEG\ffplay.exe"; DestDir: "{app}\FFMPEG"; Flags: ignoreversion
Source: "..\FFMPEG\ffprobe.exe"; DestDir: "{app}\FFMPEG"; Flags: ignoreversion

[Icons]
Name: "{group}\ZeroMix"; Filename: "{app}\ZeroMix.exe"; IconFilename: "{app}\zeromix.ico"; WorkingDir: "{app}"
Name: "{commondesktop}\ZeroMix"; Filename: "{app}\ZeroMix.exe"; IconFilename: "{app}\zeromix.ico"; WorkingDir: "{app}"; Tasks: desktopicon; Flags: createonlyiffileexists
Name: "{group}\Uninstall ZeroMix"; Filename: "{uninstallexe}"; Flags: runminimized

[Tasks]
Name: "desktopicon"; Description: "Buat &desktop shortcut"; GroupDescription: "Shortcut:"; Flags: unchecked
Name: "startup"; Description: "Jalankan ZeroMix saat Windows &startup"; GroupDescription: "Startup:"; Flags: unchecked

[Run]
; Registry startup (Lebih aman menggunakan Registry flag di [Registry] sebenarnya, tapi [Run] juga oke)
Filename: "reg.exe"; Parameters: "add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Run"" /V ""ZeroMix"" /t REG_SZ /D ""{app}\ZeroMix.exe"" /F"; Tasks: startup; Flags: runhidden
; Add to PATH
Filename: "cmd.exe"; Parameters: "/c setx PATH ""%PATH%;{app}\bin"""; Flags: runhidden
; Npm install
Filename: "cmd.exe"; Parameters: "/c cd /d ""{app}\bin"" && npm install --production 2>nul"; Flags: runhidden skipifsilent
; Jalankan Aplikasi
Filename: "{app}\ZeroMix.exe"; Description: "&Jalankan ZeroMix sekarang"; Flags: nowait postinstall skipifsilent; Tasks: ; Check: not CurTaskExists('autostart')

[Messages]
WelcomeLabel1=Selamat datang di Installer ZeroMix 
WelcomeLabel2=Program ini akan menginstal ZeroMix pada komputer Anda.%n%n⚠️ Disarankan untuk menutup semua aplikasi lain sebelum melanjutkan.

[CustomMessages]
LaunchProgram=&Jalankan ZeroMix sekarang

; ==============================================================================
; LOGIKA KODE (DIPERBAIKI)
; ==============================================================================
[Code]
var
  WasRunning: Boolean;

// FIX 1: Definisi FindWindow yang Benar (Harus 2 parameter: ClassName, WindowName)
// Menggunakan PChar agar kompatibel dengan string null-terminated Windows
function FindWindow(lpClassName, lpWindowName: String): HWND;
external 'FindWindowW@user32.dll stdcall';

// Helper untuk cek task
function CurTaskExists(const TaskName: String): Boolean;
begin
  Result := WizardIsTaskSelected(TaskName);
end;

// Setup CLI Tools
procedure RegisterCliTools();
begin
  // Logika registrasi CLI tambahan jika diperlukan
end;

procedure UnregisterCliTools();
begin
  try
    RegDeleteValue(HKEY_LOCAL_MACHINE, 
      'SYSTEM\CurrentControlSet\Control\Session Manager\Environment', 
      'ZEROMIX_HOME');
  except
  end;
end;

// --- FUNGSI SAAT INSTALASI DIMULAI ---
function InitializeSetup(): Boolean;
var
  Wnd: HWND;
  UninstallKey: String;
begin
  // --- DIAGNOSTIC CHECK ---
  // This checks if an old version is already installed.
  UninstallKey := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\' + '{A1B2C3D4-E5F6-7890-ZEROMIX-IDENTIFIER}_is1';
  if RegKeyExists(HKLM, UninstallKey) or RegKeyExists(HKCU, UninstallKey) then
  begin
    MsgBox('Installer mendeteksi bahwa ZeroMix sudah terinstal. Proses uninstall dari versi lama akan berjalan terlebih dahulu. Ini adalah bagian normal dari proses upgrade. Klik OK untuk melanjutkan.', mbInformation, MB_OK);
  end;
  // --- END DIAGNOSTIC ---

  Result := True;
  WasRunning := False;
  
 
  Wnd := FindWindow('', 'ZeroMix');
  
  if Wnd <> 0 then
  begin
    WasRunning := True;
    // Kita beri info, tapi biarkan 'CloseApplications' di [Setup] yang menangani penutupan task secara otomatis
    MsgBox('ZeroMix terdeteksi sedang berjalan. Installer akan menutupnya secara otomatis untuk melanjutkan update.', mbInformation, MB_OK);
  end;
end;

// --- FUNGSI SAAT UNINSTALL DIMULAI ---
function InitializeUninstall(): Boolean;
var
  ResultCode: Integer;
begin
  // Konfirmasi Uninstall
  if MsgBox('Apakah Anda yakin ingin menghapus ZeroMix dan semua komponennya?', mbConfirmation, MB_YESNO) = IDYES then
  begin
    // Matikan proses jika masih berjalan
    Exec('taskkill.exe', '/IM ZeroMix.exe /F', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Result := True;
  end
  else
  begin
    Result := False;
  end;
end;

// --- STEP CHANGE INSTALLER ---
procedure CurStepChanged(CurStep: TSetupStep);
var
  ErrorCode: Integer;
begin
  if CurStep = ssDone then
  begin
    RegisterCliTools();
    // Hindari popup berlebihan, cukup checklist di [Run] atau silent logic
    // Jika tetap ingin membuka web:
    if MsgBox('Instalasi selesai! Buka halaman Info?', mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open', 'https://zeromix.vercel.app/ThanksYou.html', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
    end;
  end;
end;

// --- STEP CHANGE UNINSTALLER ---
procedure CurUninstallStepChanged(CurStep: TUninstallStep);
var
  ErrorCode: Integer;
begin
  if CurStep = usUninstall then
  begin
    UnregisterCliTools();
    if MsgBox('Hapus juga data konfigurasi (AppData)?', mbConfirmation, MB_YESNO) = IDYES then
    begin
      DelTree(ExpandConstant('{userappdata}\ZeroMix'), True, True, True);
    end;
  end;

  if CurStep = usPostUninstall then
  begin
    // Feedback
    ShellExec('open', 'https://zeromix.vercel.app/Feedback.html', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
  end;
end;