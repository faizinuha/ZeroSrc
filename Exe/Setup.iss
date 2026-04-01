; ZeroMix Installer Script (Professional v5.0.3)
; ==============================================================================

[Setup]
; --- App Identity ---
AppId={{ZeroMix-v2-ZeroMix-identifier}}
AppName=ZeroMix
#define AppVersion "5.1.1"
AppVersion={#AppVersion}
VersionInfoVersion={#AppVersion}.0
VersionInfoCompany=Frieren
VersionInfoDescription=ZeroMix - Smart Desktop Launcher & System Utilities
VersionInfoProductVersion={#AppVersion}.0
AppVerName=ZeroMix v{#AppVersion}
AppPublisher=ZeroMix Team
AppPublisherURL=https://zeromix.vercel.app
AppCopyright=Copyright (c) 2025 - All Rights Reserved

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

; --- Performance ---
CloseApplications=yes
CloseApplicationsFilter=ZeroMix.exe
RestartIfNeededByRun=yes

[Languages]
Name: "indonesian"; MessagesFile: "Languages\Indonesian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "japanese"; MessagesFile: "Languages\Japanese.isl"
Name: "chinese"; MessagesFile: "Languages\Chinese.isl"

[Dirs]
Name: "{app}"
Name: "{userappdata}\ZeroMix"; Permissions: users-modify

[Files]
; Main Core - Mengambil dari folder publish hasil build dotnet
Source: "..\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; Assets & Resources
Source: "zeromix.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\Resource\*"; DestDir: "{app}\Resource"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\Plugins\**"; DestDir: "{app}\Plugins"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\Virtual_Assisten\*"; DestDir: "{app}\Virtual_Assisten"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\FFMPEG\ffmpeg.exe"; DestDir: "{app}\FFMPEG"; Flags: ignoreversion
Source: "..\zeromix-update.bat"; DestDir: "{app}"; Flags: ignoreversion
Source: "MicrosoftEdgeWebview2Setup.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall

; Documentation
Source: "Privacy.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\Readme.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\ZeroMix"; Filename: "{app}\ZeroMix.exe"; IconFilename: "{app}\zeromix.ico"
Name: "{commondesktop}\ZeroMix"; Filename: "{app}\ZeroMix.exe"; IconFilename: "{app}\zeromix.ico"; Tasks: desktopicon
Name: "{group}\Uninstall ZeroMix"; Filename: "{uninstallexe}"
Name: "{userstartup}\ZeroMix"; Filename: "{app}\ZeroMix.exe"; WorkingDir: "{app}"; Tasks: startup

[Registry]
; Context Menu Klik Kanan di Desktop
Root: HKCR; Subkey: "Directory\Background\shell\ZeroMix"; ValueType: string; ValueData: "Open ZeroMix Dashboard"; Flags: uninsdeletekey
Root: HKCR; Subkey: "Directory\Background\shell\ZeroMix"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\ZeroMix.exe"
Root: HKCR; Subkey: "Directory\Background\shell\ZeroMix\command"; ValueType: string; ValueData: """{app}\ZeroMix.exe"""

[Tasks]
Name: "desktopicon"; Description: "Buat shortcut di Desktop"; GroupDescription: "Shortcut:"; Flags: checkedonce
Name: "startup"; Description: "Jalankan otomatis saat Windows Startup"; GroupDescription: "Startup:"; Flags: unchecked

[Run]
; Jalankan Aplikasi setelah install
Filename: "{app}\ZeroMix.exe"; Description: "{cm:LaunchProgram}"; Flags: nowait postinstall skipifsilent
Filename: "{tmp}\MicrosoftEdgeWebview2Setup.exe"; Parameters: "/silent /install"; StatusMsg: "Installing WebView2 Runtime..."; Flags: runhidden

[Messages]
indonesian.WelcomeLabel1=Selamat datang di ZeroMix Professional v{#AppVersion}
indonesian.WelcomeLabel2=Siap untuk mengubah tampilan desktop Kakak jadi lebih estetik dan pintar?%n%nPastikan untuk menutup aplikasi lain yang sedang berjalan.

[Code]
// --- CEK DEPENDENCY: .NET 9 & WEBVIEW2 ---

function IsDotNet9Installed(): Boolean;
var
  Success: Boolean;
  InstallRoot: String;
begin
  // Cek instalasi .NET 9 Desktop Runtime (x64)
  Success := RegQueryStringValue(HKLM, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost', 'Version', InstallRoot);
  Result := Success and (Pos('9.', InstallRoot) = 1);
end;


function InitializeSetup(): Boolean;
var
  ErrorCode: Integer;
begin
  Result := True;
  
  // Cek .NET 9 Desktop Runtime
  if not IsDotNet9Installed() then
  begin
    if MsgBox('ZeroMix membutuhkan .NET 9.0 Desktop Runtime.' + #13#10 + #13#10 + 'Apakah Kakak ingin mendownloadnya sekarang?', mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open', 'https://dotnet.microsoft.com/en-us/download/dotnet/9.0', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
    end;
    Result := False;
    Exit;
  end;

end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ErrorCode: Integer;
begin
  if CurStep = ssPostInstall then
  begin
    // Simpan bahasa yang dipilih ke file config sederhana
    SaveStringToFile(ExpandConstant('{app}\language.txt'), ActiveLanguage, False);
  end;
  
  if CurStep = ssDone then
  begin
    // Buka halaman terima kasih
    ShellExec('open', 'https://zeromix.vercel.app/ThanksYou.html', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
  end;
end;

procedure CurUninstallStepChanged(CurStep: TUninstallStep);
begin
  if CurStep = usPostUninstall then
  begin
    if MsgBox('Apakah Kakak juga ingin menghapus folder pengaturan (config) di AppData?', mbConfirmation, MB_YESNO) = IDYES then
    begin
      DelTree(ExpandConstant('{userappdata}\ZeroMix'), True, True, True);
    end;
  end;
end;
