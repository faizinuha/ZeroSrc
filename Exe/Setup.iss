; ==============================================================================
; ZeroMix Installer Script (Fixed Professional v2.2)
; ==============================================================================

[Setup]
; --- PENTING: AppId Unik (Dibuat Baru) ---
AppId={{ZeroMix-v2-ZeroMix-identifier}}
AppName=ZeroMix

; Allow overriding AppVersion via command line: /DAppVersion=X.X.X
#ifndef AppVersion
  #define AppVersion "2.5.0"
#endif

AppVersion={#AppVersion}
VersionInfoVersion={#AppVersion}.0
VersionInfoCompany=Frieren
VersionInfoDescription=ZeroMix - Smart Desktop Launcher & System Utilities
VersionInfoTextVersion={#AppVersion}.0
VersionInfoProductVersion={#AppVersion}.0
AppVerName=ZeroMix v{#AppVersion}
AppPublisher=ZeroMix Team
AppPublisherURL=https://zeromix.vercel.app
AppCopyright=Copyright (c) 2025 - All Rights Reserved

; Installation Configuration
DefaultDirName={pf}\ZeroMix
DefaultGroupName=ZeroMix
AllowNoIcons=yes
OutputDir=.
OutputBaseFilename=ZeroMix-Setup-v{#AppVersion}
Compression=lzma2
SolidCompression=yes
DisableProgramGroupPage=no
UninstallDisplayIcon={app}\zeromix.ico

; UI Configuration
WizardStyle=modern
UninstallStyle=modern
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

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "indonesian"; MessagesFile: "Languages\Indonesian.isl"
Name: "japanese"; MessagesFile: "Languages\Japanese.isl"
Name: "chinese"; MessagesFile: "Languages\Chinese.isl"

[Dirs]
; Memberikan akses tulis ke folder aplikasi agar config.json bisa disimpan/diupdate oleh aplikasi (User biasa)
Name: "{app}"; Permissions: users-modify

[Files]
; Main application
Source: "..\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Icon
Source: "zeromix.ico"; DestDir: "{app}"; Flags: ignoreversion
; Resources
Source: "..\Resource\*"; DestDir: "{app}\Resource"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\Plugins\zeromix.weather\assets\*"; DestDir: "{app}\Plugins\zeromix.weather\assets"; Flags: ignoreversion recursesubdirs createallsubdirs

; Docs
Source: "Privacy.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "../LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "../Readme.md"; DestDir: "{app}"; Flags: ignoreversion

; CLI Tools (commented out - folder not found)
;Source: "../ZeroMixUpdateCli/index.js"; DestDir: "{app}\bin"; Flags: ignoreversion
;Source: "../ZeroMixUpdateCli/package.json"; DestDir: "{app}\bin"; Flags: ignoreversion
;Source: "../ZeroMixUpdateCli/zeromix-cli.bat"; DestDir: "{app}\bin"; Flags: ignoreversion

; FFmpeg Tools
; NOTE: The build fails because ffmpeg.exe is missing from the FFMPEG folder.
; You can download it and place it there, then uncomment the line below.
;Source: "../FFMPEG/ffmpeg.exe"; DestDir: "{app}\FFMPEG"; Flags: ignoreversion

[Icons]
Name: "{group}\ZeroMix"; Filename: "{app}\ZeroMix.exe"; IconFilename: "{app}\zeromix.ico"; WorkingDir: "{app}"
Name: "{commondesktop}\ZeroMix"; Filename: "{app}\ZeroMix.exe"; IconFilename: "{app}\zeromix.ico"; WorkingDir: "{app}"; Tasks: desktopicon; Flags: createonlyiffileexists
Name: "{group}\Uninstall ZeroMix"; Filename: "{uninstallexe}"; Flags: runminimized
Name: "{userstartup}\ZeroMix"; Filename: "{app}\ZeroMix.exe"; WorkingDir: "{app}"; Tasks: startup

[Registry]
; Context Menu Klik Kanan di Desktop
Root: HKCR; Subkey: "Directory\Background\shell\ZeroMix"; ValueType: string; ValueData: "Open ZeroMix Dashboard"; Flags: uninsdeletekey
Root: HKCR; Subkey: "Directory\Background\shell\ZeroMix"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\ZeroMix.exe"
Root: HKCR; Subkey: "Directory\Background\shell\ZeroMix\command"; ValueType: string; ValueData: """{app}\ZeroMix.exe"""

[Tasks]
Name: "desktopicon"; Description: "Buat &desktop shortcut"; GroupDescription: "Shortcut:"; Flags: unchecked
Name: "startup"; Description: "Jalankan ZeroMix saat Windows &startup (Mungkin bikin booting lama)"; GroupDescription: "Startup:"; Flags: unchecked

[Run]
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
  UninstallKey := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\' + '{ZeroMix-v2-Frieren-identifier}_is1';
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
  LangCode: String;
  LangFileName: String;
begin
  if CurStep = ssPostInstall then
  begin
    // Determine language code based on installer selection
    if ActiveLanguage = 'indonesian' then LangCode := 'id-ID'
    else if ActiveLanguage = 'japanese' then LangCode := 'ja-JP'
    else if ActiveLanguage = 'chinese' then LangCode := 'zh-CN'
    else LangCode := 'en-US';

    // Write to config file
    LangFileName := ExpandConstant('{app}\language.ini');
    SaveStringToFile(LangFileName, LangCode, False);
  end;

  if CurStep = ssDone then
  begin
    RegisterCliTools();
    begin
      ShellExec('open', 'https://zeromix.vercel.app/ThanksYou.html', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
    end;
  end;
end;

// --- STEP CHANGE UNINSTALLER (SMOOTH FLOW) ---
procedure CurUninstallStepChanged(CurStep: TUninstallStep);
var
  ErrorCode: Integer;
  AppDataPath: String;
begin
  case CurStep of
    usUninstall:
      begin
        // Sembunyi: Unregister CLI di background
        UnregisterCliTools();
      end;
      
    usPostUninstall:
      begin
        // Step Terakhir: Tanya Data & Kasih Feedback link
        if MsgBox('Uninstall Selesai!' + #13#13 +
                  'Apakah Kakak ingin menghapus semua data pengaturan/config juga?' + #13 + 
                  '(Pilih "Tidak" jika Kakak berencana install ulang nanti)', 
                  mbConfirmation, MB_YESNO) = IDYES then
        begin
          AppDataPath := ExpandConstant('{userappdata}\ZeroMix');
          if DirExists(AppDataPath) then DelTree(AppDataPath, True, True, True);
        end;

        // Buka Feedback secara otomatis (Opsional tapi bagus untuk data)
        ShellExec('open', 'https://zeromix.vercel.app/Feedback.html', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
      end;
  end;
end;