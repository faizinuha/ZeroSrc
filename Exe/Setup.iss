; ZeroMix Installer Script (Professional v)
; ==============================================================================

[Setup]
; --- App Identity ---
AppId={{077E54A3-2CC5-439F-AC7E-32FA2A8BDD5A}}
AppName=ZeroMix
#define AppVersion "7.2.2"
AppVersion={#AppVersion}
VersionInfoVersion={#AppVersion}.0
VersionInfoCompany=Frieren
VersionInfoDescription=ZeroMix - Smart Desktop Launcher & System Utilities
VersionInfoProductVersion={#AppVersion}.0
AppVerName=ZeroMix v{#AppVersion}
AppPublisher=ZeroMix Team
AppPublisherURL=https://zeromix.vercel.app
AppCopyright=Copyright (c) 2026 - All Rights Reserved

; --- Installation Path ---
DefaultDirName={autopf}\ZeroMix
DefaultGroupName=ZeroMix
AllowNoIcons=yes
OutputDir=.
OutputBaseFilename=ZeroMix-Setup-v{#AppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
DisableProgramGroupPage=no
UninstallDisplayIcon={app}\zeromix.ico

; --- Appearance & Security ---
WizardStyle=modern
UninstallStyle=modern
SetupIconFile=zeromix.ico
WizardResizable=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64
ArchitecturesAllowed=x64
MinVersion=10.0.19041

; --- Language Selection Dialog at Start ---
ShowLanguageDialog=yes
LanguageDetectionMethod=locale

; --- Performance ---
CloseApplications=yes
CloseApplicationsFilter=ZeroMix.exe
RestartIfNeededByRun=yes

[Languages]
Name: "english";    MessagesFile: "compiler:Default.isl"
Name: "indonesian"; MessagesFile: "Languages\Indonesian.isl"
Name: "japanese";   MessagesFile: "Languages\Japanese.isl"
Name: "chinese";    MessagesFile: "Languages\Chinese.isl"
Name: "korean";     MessagesFile: "Languages\Korean.isl"

[Dirs]
Name: "{app}"
Name: "{userappdata}\ZeroMix"; Permissions: users-modify

[Files]
; Main Core - dari folder publish hasil dotnet publish
; Exclude file-file yang tidak perlu di root install dir
Source: "..\publish\win-x64\ZeroMix.exe";        DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\win-x64\*.dll";               DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\win-x64\*.json";              DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "..\publish\win-x64\runtimes\*";          DestDir: "{app}\runtimes"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist

; Assets & Resources
Source: "zeromix.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\Assets\Icons\**";                     DestDir: "{app}\Assets\Icons"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\Assets\Resources\**";                 DestDir: "{app}\Assets\Resources"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\Assets\Data\anim\**";                 DestDir: "{app}\Assets\Data\anim"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\Assets\Data\Images\**";               DestDir: "{app}\Assets\Data\Images"; Flags: ignoreversion
Source: "..\Assets\Data\Video\**";                DestDir: "{app}\Assets\Data\Video"
Source: "..\Assets\zeromix-high-resolution-logo-transparent.png"; DestDir: "{app}\Assets"; Flags: ignoreversion

; Plugins
Source: "..\publish\win-x64\Tools\Plugins\**";   DestDir: "{app}\Tools\Plugins"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist

; Virtual Assistant — ambil dari publish folder (sudah di-build, tidak ada source code)
Source: "..\publish\win-x64\Virtual_Assisten\**"; DestDir: "{app}\Virtual_Assisten"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist

; Cat Gatekeeper assets — mp4 (converted from webm)
Source: "..\Tools\Plugins\zeromix.CatGatekeeper\assets\neko1.mp4";     DestDir: "{app}\Tools\Plugins\zeromix.CatGatekeeper\assets"; Flags: ignoreversion skipifsourcedoesntexist
Source: "..\Tools\Plugins\zeromix.CatGatekeeper\assets\neko2.mp4";     DestDir: "{app}\Tools\Plugins\zeromix.CatGatekeeper\assets"; Flags: ignoreversion skipifsourcedoesntexist
Source: "..\Tools\Plugins\zeromix.CatGatekeeper\assets\nekoicon128.png"; DestDir: "{app}\Tools\Plugins\zeromix.CatGatekeeper\assets"; Flags: ignoreversion skipifsourcedoesntexist

; Documentation
Source: "..\LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion

; Update scripts
Source: "..\Scripts\zeromix-update.ps1"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "..\Scripts\zeromix-update.bat"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "..\ZeroMix.ps1";        DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist

[Icons]
Name: "{group}\ZeroMix"; Filename: "{app}\ZeroMix.exe"; IconFilename: "{app}\zeromix.ico"
Name: "{commondesktop}\ZeroMix"; Filename: "{app}\ZeroMix.exe"; IconFilename: "{app}\zeromix.ico"; Tasks: desktopicon
Name: "{group}\Uninstall ZeroMix"; Filename: "{uninstallexe}"
Name: "{userstartup}\ZeroMix"; Filename: "{app}\ZeroMix.exe"; WorkingDir: "{app}"; Tasks: startup

[Registry]
; Context Menu klik kanan di Desktop
Root: HKCR; Subkey: "Directory\Background\shell\ZeroMix"; ValueType: string; ValueData: "Open ZeroMix Dashboard"; Flags: uninsdeletekey
Root: HKCR; Subkey: "Directory\Background\shell\ZeroMix"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\ZeroMix.exe"
Root: HKCR; Subkey: "Directory\Background\shell\ZeroMix\command"; ValueType: string; ValueData: """{app}\ZeroMix.exe"""

[Tasks]
Name: "desktopicon"; Description: "Buat shortcut di Desktop"; GroupDescription: "Shortcut:"; Flags: checkedonce
Name: "startup"; Description: "Jalankan otomatis saat Windows Startup"; GroupDescription: "Startup:"; Flags: unchecked

[Run]
Filename: "{app}\ZeroMix.exe"; Description: "{cm:LaunchProgram}"; Flags: nowait postinstall skipifsilent

[Messages]
indonesian.WelcomeLabel1=Selamat datang di ZeroMix v{#AppVersion}
indonesian.WelcomeLabel2=Siap untuk mengubah tampilan desktop Kakak jadi lebih estetik dan pintar?%n%nPastikan untuk menutup aplikasi lain yang sedang berjalan.
korean.WelcomeLabel1=ZeroMix v{#AppVersion}에 오신 것을 환영합니다
korean.WelcomeLabel2=설치를 시작하기 전에 다른 모든 응용 프로그램을 닫아 주세요.

[Code]
// ── Cek .NET 9 Desktop Runtime ─────────────────────────────────────────────
function IsDotNet9Installed(): Boolean;
var
  Version: String;
begin
  Result := RegQueryStringValue(HKLM,
    'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost',
    'Version', Version) and (Pos('9.', Version) = 1);
end;

// ── Init: cek .NET 9 sebelum install ───────────────────────────────────────
function InitializeSetup(): Boolean;
var
  ErrorCode: Integer;
begin
  Result := True;

  if not IsDotNet9Installed() then
  begin
    if MsgBox(
      'ZeroMix membutuhkan .NET 9.0 Desktop Runtime.' + #13#10#13#10 +
      'Apakah Kakak ingin mendownloadnya sekarang?',
      mbConfirmation, MB_YESNO) = IDYES then
      ShellExec('open', 'https://dotnet.microsoft.com/en-us/download/dotnet/9.0',
        '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
    Result := False;
    Exit;
  end;
end;

// ── Post-install: simpan language.ini ke AppData ────────────────────────────
procedure CurStepChanged(CurStep: TSetupStep);
var
  ErrorCode: Integer;
  LangCode: String;
  AppDataDir: String;
begin
  if CurStep = ssPostInstall then
  begin
    // Map installer language → app language code
    if ActiveLanguage = 'indonesian' then
      LangCode := 'id-ID'
    else if ActiveLanguage = 'japanese' then
      LangCode := 'ja-JP'
    else if ActiveLanguage = 'chinese' then
      LangCode := 'zh-CN'
    else if ActiveLanguage = 'korean' then
      LangCode := 'ko-KR'
    else
      LangCode := 'en-US';

    // Simpan ke AppData\ZeroMix\language.ini (sama dengan yang dibaca app)
    AppDataDir := ExpandConstant('{userappdata}\ZeroMix');
    ForceDirectories(AppDataDir);
    SaveStringToFile(AppDataDir + '\language.ini', LangCode, False);
  end;

  if CurStep = ssDone then
    ShellExec('open', 'https://zeromix.vercel.app/ThanksYou.html',
      '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
end;

// ── Uninstall ───────────────────────────────────────────────────────────────
procedure CurUninstallStepChanged(CurStep: TUninstallStep);
var
  ResultCode: Integer;
  ErrorCode: Integer;
begin
  if CurStep = usUninstall then
    Exec('taskkill.exe', '/f /im ZeroMix.exe', '', SW_HIDE,
      ewWaitUntilTerminated, ResultCode);

  if CurStep = usPostUninstall then
  begin
    // Hapus registry context menu
    RegDeleteKeyIncludingSubkeys(HKCR, 'Directory\Background\shell\ZeroMix');
    // Hapus startup shortcut
    DeleteFile(ExpandConstant('{userstartup}\ZeroMix.lnk'));
    // Hapus AppData & LocalAppData tanpa dialog konfirmasi
    DelTree(ExpandConstant('{userappdata}\ZeroMix'), True, True, True);
    DelTree(ExpandConstant('{localappdata}\ZeroMix'), True, True, True);

    // Buka halaman feedback setelah uninstall selesai
    ShellExec('open', 'https://zeromix.vercel.app/Feedback.html',
      '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
  end;
end;
