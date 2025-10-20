; --- Informasi Aplikasi & Penanda Tangan (Semua di dalam [Setup]) ---
[Setup]
AppName=ZeroMix
AppVersion=1.7.0
VersionInfoVersion=1.7.0.0
VersionInfoCompany=Zaki
VersionInfoDescription=ZeroMix smart launcher
VersionInfoTextVersion=1.7.0
AppVerName=ZeroMix
AppPublisher=Zaki
AppPublisherURL=https://zeromix.pages.dev
AppSupportURL=https://zeromix.pages.dev/support
AppUpdatesURL=https://zeromix.pages.dev/updates
AppCopyright=Copyright © 2025 Zaki. All rights reserved.
MinVersion=6.1sp1
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64
CloseApplications=yes
RestartApplications=no
AlwaysRestart=no
DisableDirPage=auto
DisableWelcomePage=no
DisableReadyPage=no
DisableFinishedPage=no
AppMutex=ZeroMixAppMutex_1.7.0
WizardStyle=modern
SetupMutex=ZeroMixSetupMutex_1.7.0,Global\ZeroMixSetupMutex_1.7.0
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
; Signer configuration — gunakan sertifikat yang ada di folder Exe
#define CERT_PATH "Exe\\ZeroMixCert.pfx"
; Use environment variable ZEROMIX_CERT_PASS when set to avoid embedding password in script
#if GetEnv('ZEROMIX_CERT_PASS') == ''
#define CERT_PASS "ZeroMixPass"
#else
#define CERT_PASS GetEnv('ZEROMIX_CERT_PASS')
#endif
SignTool=signtool
SignedUninstaller=yes

[SignTool]
; Gunakan signtool.exe untuk menandatangani installer dan uninstaller
signtool=signtool.exe sign /f "{#CERT_PATH}" /p "{#CERT_PASS}" /d "ZeroMix" /du "https://zeromix.pages.dev" /tr http://timestamp.digicert.com /td sha256 /fd sha256 $f

[Code]
function VerifyFileHash(const FilePath: string; const ExpectedHash: string): Boolean;
var
  CalculatedHash: string;
  HashAlg: HCRYPTPROV;
  HashCtx: HCRYPTHASH;
  FileHandle: THandle;
  BytesRead: DWORD;
  Buffer: array[0..4095] of Byte;
  HashValue: array[0..31] of Byte;
  HashSize: DWORD;
  i: Integer;
begin
  Result := False;
  
  if not CryptAcquireContext(HashAlg, nil, nil, PROV_RSA_AES, CRYPT_VERIFYCONTEXT) then
    Exit;
  try
    if not CryptCreateHash(HashAlg, CALG_SHA_256, 0, 0, HashCtx) then
      Exit;
    try
      FileHandle := CreateFile(FilePath, GENERIC_READ, FILE_SHARE_READ, nil, OPEN_EXISTING, 0, 0);
      if FileHandle = INVALID_HANDLE_VALUE then
        Exit;
      try
        repeat
          if not ReadFile(FileHandle, Buffer, SizeOf(Buffer), BytesRead, nil) then
            Exit;
          if BytesRead > 0 then
            if not CryptHashData(HashCtx, @Buffer, BytesRead, 0) then
              Exit;
        until BytesRead = 0;
        
        HashSize := SizeOf(HashValue);
        if not CryptGetHashParam(HashCtx, HP_HASHVAL, @HashValue, HashSize, 0) then
          Exit;
          
        CalculatedHash := '';
        for i := 0 to HashSize - 1 do
          CalculatedHash := CalculatedHash + IntToHex(HashValue[i], 2);
          
        Result := CompareText(CalculatedHash, ExpectedHash) = 0;
      finally
        CloseHandle(FileHandle);
      end;
    finally
      CryptDestroyHash(HashCtx);
    end;
  finally
    CryptReleaseContext(HashAlg, 0);
  end;
end;

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
WelcomeLabel2=Program ini akan menginstal ZeroMix versi 1.7.0 di komputer Anda.%n%nZeroMix adalah aplikasi pintar yang memungkinkan Anda membuka web dan aplikasi desktop dengan cepat dan efisien.%n%nDisarankan untuk menutup semua aplikasi lain sebelum melanjutkan.
FinishedLabel=ZeroMix telah berhasil diinstal di komputer Anda.%n%nSilakan tekan Selesai untuk keluar dari Program Penginstal.
FinishedHeadingLabel=Penyelesaian Penginstalan ZeroMix
AboutSetupNote=Program Penginstal ZeroMix dibuat dengan Inno Setup.%nInno Setup tersedia secara gratis dari jrsoftware.org.

[CustomMessages]
LaunchProgram=&Jalankan ZeroMix
AdditionalTasks=Tugas tambahan:
WindowsServiceNote=Layanan Windows:

[LicenseFile]
InfoBeforeFile=README-Installer.md

[Code]
// Fungsi untuk memeriksa apakah ZeroMix sedang berjalan
function IsAppRunning(): Boolean;
var
  FoundWnd: HWND;
begin
  FoundWnd := FindWindowByWindowName('ZeroMix');
  Result := (FoundWnd <> 0);
end;

// Event sebelum instalasi dimulai
function InitializeSetup(): Boolean;
begin
  Result := True;

  // Periksa apakah aplikasi sedang berjalan
  if IsAppRunning() then begin
    if MsgBox('ZeroMix sedang berjalan. Aplikasi harus ditutup untuk melanjutkan instalasi.' + #13#10 +
              'Apakah Anda ingin menutup ZeroMix sekarang?', 
              mbConfirmation, MB_YESNO) = IDYES then begin
      Exec('taskkill.exe', '/F /IM ZeroMix.exe', '', SW_HIDE, ewWaitUntilTerminated, ErrorCode);
    end else begin
      Result := False;
      exit;
    end;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
 ErrorCode: Integer;
begin
 if CurStep = ssDone then
 begin
  MsgBox('Terima kasih sudah menginstall ZeroMix!' + #13#10 + 'Dukungan Anda sangat berarti bagi kami.', mbInformation, MB_OK);
  // Open the thank you page in the user's default web browser
  // khusus 1.7.0
  ShellExec('open', 'https://zeromix.pages.dev/ThanksYou', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
 end;
end;

procedure CurUninstallStepChanged(CurStep: TUninstallStep);
var
ErrorCode: Integer;
begin
 if CurStep = usPostUninstall then
 begin
  ShellExec('open', 'https://zeromix.pages.dev/Feedback.html', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
 end;
end;