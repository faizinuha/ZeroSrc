[Setup]
AppName=Google Box
AppVersion=1.0.5
DriversVersion=1.0.5
AppPublisher=Vibes
AppPublisherURL=https://mardev-v8.vercel.app/
AppSupportURL=https://mardev-v8.vercel.app/
AppUpdatesURL=https://mardev-v8.vercel.app/
AppCopyright=Copyright (c) 2023 Vibes
AppDescription=Google Box is a web browser that allows you to access the internet with ease and convenience. It is designed to provide a fast and secure browsing experience, with features such as tabbed browsing, bookmarks, and history management.
AppComments=Google Box is a web browser that allows you to access the internet with ease and convenience. It is designed to provide a fast and secure browsing experience, with features such as tabbed browsing, bookmarks, and history management.
AppVerName=Google Box 1.0.5
AppId={4A1B2C3D-5E6F-7A8B-9A0B-C1D2E3F4G5H6}
DefaultDirName={commonpf}\GoogleBox
DefaultGroupName=Google Box
OutputDir=.
OutputBaseFilename=Google-Box
SetupIconFile=favicon.ico
Compression=lzma
SolidCompression=yes

[Files]
Source: "..\bin\Debug\net9.0-windows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "favicon.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Google Box"; Filename: "{app}\ZeroSrc.exe"; IconFilename: "{app}\favicon.ico"
Name: "{commondesktop}\Google Box"; Filename: "{app}\ZeroSrc.exe"; IconFilename: "{app}\favicon.ico"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"
