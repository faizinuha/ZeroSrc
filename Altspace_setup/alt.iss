; --- Informasi Aplikasi ---
[Setup]
AppName=Google Box
AppVersion=1.1.5
AppVerName=Google Box
AppPublisher=Vibes
AppPublisherURL=https://mardev-v8.vercel.app/
AppSupportURL=https://github.com/faizinuha/ZeroSrc/issues
AppUpdatesURL=https://github.com/faizinuha/ZeroSrc/issues
AppCopyright=Copyright (c) 2023 Vibes
AppComments=Google Box is a smart launcher for quick access to web and desktop apps.
DefaultDirName={autopf}\GoogleBox
DefaultGroupName=Google Box
OutputDir=.
OutputBaseFilename=Google-Box-Setup-v{#SetupSetting("AppVersion")}
SetupIconFile=favicon.ico
WizardStyle=modern
WizardResizable=yes
Compression=lzma2/ultra64
SolidCompression=yes
UninstallDisplayIcon={app}\favicon.ico
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
MinVersion=10.0
PrivilegesRequired=admin
DisableProgramGroupPage=no
DisableReadyPage=no
DisableFinishedPage=no
ShowLanguageDialog=no
SetupLogging=yes
CloseApplications=yes
RestartApplications=yes

; --- File yang akan diinstal ---
[Files]
Source: "..\bin\Debug\net9.0-windows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "favicon.ico"; DestDir: "{app}"; Flags: ignoreversion; Check: FileExists(ExpandConstant('{srcexe}\..\favicon.ico'))

; --- Registry ---
[Registry]
Root: HKCU; Subkey: "SOFTWARE\GoogleBox"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"
Root: HKCU; Subkey: "SOFTWARE\GoogleBox"; ValueType: string; ValueName: "Version"; ValueData: "{#SetupSetting('AppVersion')}"
Root: HKCU; Subkey: "SOFTWARE\GoogleBox"; ValueType: dword; ValueName: "FirstRun"; ValueData: 1
Root: HKCU; Subkey: "SOFTWARE\GoogleBox"; ValueType: string; ValueName: "InstallDate"; ValueData: "{code:GetCurrentDateTime}"

; --- Shortcut ---
[Icons]
Name: "{group}\Google Box"; Filename: "{app}\ZeroSrc.exe"; IconFilename: "{app}\favicon.ico"; Comment: "Jalankan Google Box"
Name: "{group}\Uninstall Google Box"; Filename: "{uninstallexe}"; Comment: "Hapus Google Box"
Name: "{commondesktop}\Google Box"; Filename: "{app}\ZeroSrc.exe"; IconFilename: "{app}\favicon.ico"; Tasks: desktopicon; Comment: "Google Box - Smart Launcher"

; --- Pilihan Tambahan ---
[Tasks]
Name: "desktopicon"; Description: "Buat shortcut di &desktop"; GroupDescription: "Shortcut tambahan:"
Name: "autorun"; Description: "Jalankan otomatis saat &Windows startup"; GroupDescription: "Opsi startup:"

; --- Jalankan aplikasi setelah install ---
[Run]
Filename: "{app}\ZeroSrc.exe"; Description: "Jalankan Google Box sekarang"; Flags: nowait postinstall skipifsilent
Filename: "https://github.com/faizinuha/ZeroSrc"; Description: "Kunjungi halaman GitHub kami"; Flags: shellexec postinstall skipifsilent unchecked

; --- Bersihkan file saat uninstall ---
[UninstallDelete]
Type: filesandordirs; Name: "{app}"
Type: filesandordirs; Name: "{userappdata}\GoogleBox"

; --- Tutup proses saat uninstall ---
[UninstallRun]
Filename: "taskkill.exe"; Parameters: "/IM ZeroSrc.exe /F"; StatusMsg: "Menutup aplikasi Google Box..."; Flags: runhidden

; --- Pesan Kustom ---
[Messages]
WelcomeLabel2=Selamat datang di penginstal Google Box!%n%nAplikasi pintar untuk membuka web dan aplikasi desktop dengan cepat.%n%nSetup akan memandu Anda melalui proses instalasi.
ClickNext=Klik Lanjut untuk melanjutkan, atau Batal untuk keluar dari Setup.
ClickInstall=Klik Instal untuk memulai instalasi Google Box.
ClickFinish=Klik Selesai untuk menyelesaikan Setup.
FinishedHeadingLabel=Instalasi Google Box Selesai
FinishedLabelNoIcons=Google Box berhasil diinstal di komputer Anda.
FinishedLabel=Google Box berhasil diinstal. Anda dapat menjalankannya dari desktop atau Start Menu.%n%nTerima kasih telah menggunakan Google Box!
ConfirmUninstall=Apakah Anda yakin ingin menghapus Google Box?%n%nSemua data aplikasi akan dihapus.
UninstallStatusLabel=Mohon tunggu sementara Google Box dihapus dari komputer Anda...
UninstalledAll=Google Box berhasil dihapus dari komputer Anda.%n%nTerima kasih telah menggunakan Google Box!

; --- Event Code yang Disederhanakan ---
[Code]
var
  InstallProgressPage: TOutputProgressWizardPage;

// Fungsi untuk mengecek file exists
function FileExists(const FileName: string): Boolean;
begin
  Result := FileOrDirExists(FileName);
end;

// Fungsi untuk mendapatkan waktu saat ini
function GetCurrentDateTime(Param: String): String;
begin
  Result := GetDateTimeString('yyyy-mm-dd hh:nn:ss', #0, #0);
end;

// Initialize Wizard
procedure InitializeWizard;
begin
  InstallProgressPage := CreateOutputProgressPage(
    '🚀 Menginstal Google Box', 
    '⏳ Mohon tunggu sementara Google Box diinstal di komputer Anda...'
  );
end;

// Initialize Uninstall
function InitializeUninstall(): Boolean;
var
  Response: Integer;
begin
  Response := MsgBox(
    '⚠️  KONFIRMASI PENGHAPUSAN' + #13#10 + #13#10 +
    '📦 Aplikasi: Google Box v' + ExpandConstant('{#SetupSetting("AppVersion")}') + #13#10 +
    '📁 Lokasi: ' + ExpandConstant('{app}') + #13#10 + #13#10 +
    '❗ Peringatan:' + #13#10 +
    '• Semua file aplikasi akan dihapus' + #13#10 +
    '• Data pengguna akan hilang' + #13#10 +
    '• Shortcut akan dihapus' + #13#10 + #13#10 +
    '🤔 Apakah Anda yakin ingin melanjutkan?',
    mbConfirmation, MB_YESNO or MB_DEFBUTTON2
  );
  
  Result := (Response = IDYES);
end;

// Event saat instalasi berlangsung
procedure CurStepChanged(CurStep: TSetupStep);
begin
  case CurStep of
    ssInstall:
    begin
      InstallProgressPage.SetText('📦 Menginstal file aplikasi...', '');
      InstallProgressPage.SetProgress(0, 100);
      InstallProgressPage.Show;
    end;
    
    ssPostInstall:
    begin
      try
        InstallProgressPage.SetText('🔗 Membuat shortcut...', '');
        InstallProgressPage.SetProgress(60, 100);
        Sleep(800);
        
        InstallProgressPage.SetText('📝 Mendaftarkan aplikasi...', '');
        InstallProgressPage.SetProgress(80, 100);
        Sleep(800);
        
        // Buat file konfigurasi
        SaveStringToFile(ExpandConstant('{app}\config.ini'), 
          '[Settings]' + #13#10 + 
          'FirstRun=true' + #13#10 + 
          'Version=' + ExpandConstant('{#SetupSetting("AppVersion")}') + #13#10 + 
          'InstallDate=' + GetCurrentDateTime(''), False);
        
        InstallProgressPage.SetText('✅ Menyelesaikan instalasi...', '');
        InstallProgressPage.SetProgress(100, 100);
        Sleep(1000);
        
        InstallProgressPage.Hide;
        
        // Pesan sukses
        MsgBox(
          '🎉 INSTALASI BERHASIL!' + #13#10 + #13#10 +
          '✅ Google Box v' + ExpandConstant('{#SetupSetting("AppVersion")}') + ' berhasil diinstal' + #13#10 +
          '✅ Shortcut telah dibuat di desktop dan Start Menu' + #13#10 +
          '✅ Aplikasi siap digunakan' + #13#10 +
          '✅ Konfigurasi default telah disiapkan' + #13#10 + #13#10 +
          '🚀 CARA MENJALANKAN:' + #13#10 +
          '• Klik shortcut di desktop' + #13#10 +
          '• Cari "Google Box" di Start Menu' + #13#10 +
          '• Jalankan dari folder instalasi' + #13#10 + #13#10 +
          '💝 Terima kasih telah memilih Google Box!' + #13#10 + #13#10 +
          '🌟 DUKUNG KAMI:' + #13#10 +
          'GitHub: https://github.com/faizinuha/ZeroSrc' + #13#10 +
          '⭐ Berikan bintang jika Anda suka!',
          mbInformation, MB_OK
        );
        
      except
        MsgBox('❌ Terjadi kesalahan saat instalasi!' + #13#10 + 'Silakan coba install ulang atau hubungi support.', mbError, MB_OK);
      end;
    end;
  end;
end;

// Event saat uninstall
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  case CurUninstallStep of
    usPostUninstall:
    begin
      try
        // Pesan sukses uninstall
        MsgBox(
          '✅ PENGHAPUSAN BERHASIL!' + #13#10 + #13#10 +
          '🗑️  Google Box telah dihapus dari sistem Anda' + #13#10 +
          '🧹 File dan registry telah dibersihkan' + #13#10 +
          '🔗 Shortcut telah dihapus' + #13#10 +
          '📁 Folder aplikasi telah dihapus' + #13#10 + #13#10 +
          '😊 TERIMA KASIH!' + #13#10 +
          'Terima kasih telah menggunakan Google Box!' + #13#10 +
          'Kami harap aplikasi ini bermanfaat untuk Anda.' + #13#10 + #13#10 +
          '💬 FEEDBACK & DUKUNGAN:' + #13#10 +
          '🌟 GitHub: https://github.com/faizinuha/ZeroSrc' + #13#10 +
          '🐛 Laporkan bug di GitHub Issues' + #13#10 +
          '⭐ Berikan bintang jika Anda suka aplikasi ini!' + #13#10 +
          '💡 Punya ide? Bagikan di GitHub Discussions!' + #13#10 + #13#10 +
          '🔄 INSTALL LAGI?' + #13#10 +
          'Download versi terbaru dari GitHub Releases' + #13#10 + #13#10 +
          '👋 Sampai jumpa lagi!',
          mbInformation, MB_OK
        );
        
      except
        MsgBox('❌ Terjadi kesalahan saat penghapusan!', mbError, MB_OK);
      end;
    end;
  end;
end;

// Prepare to Install
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
begin
  Result := '';
  NeedsRestart := False;
  
  try
    Exec('taskkill.exe', '/IM ZeroSrc.exe /F', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Sleep(2000);
  except
    Result := '';
  end;
end;