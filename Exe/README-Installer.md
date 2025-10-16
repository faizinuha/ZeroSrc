Publishing for Inno Setup (self-contained)

1. From project root, publish a self-contained single-file build for Windows x64:

   dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -p:PublishTrimmed=true --self-contained true -o ./publish/win-x64

2. Verify the publish folder contains ZeroMix.exe and any supporting files.

3. Build the installer (on Windows with Inno Setup installed):

   - Open `Setup.iss` in Inno Setup Compiler and compile.
   - Or run ISCC from command line: "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" Setup.iss

4. Code signing (recommended to reduce antivirus false positives):

   - Sign the executable:
     signtool sign /a /tr http://timestamp.digicert.com /td sha256 /fd sha256 path\to\ZeroMix.exe

   - Sign the installer:
     signtool sign /a /tr http://timestamp.digicert.com /td sha256 /fd sha256 path\to\ZeroMix-Setup.exe

Notes:
- Use the correct runtime identifier (RID) if you need win-x86 or arm64 builds.
- If your app uses runtime components not suitable for trimming, remove `PublishTrimmed=true`.
- Self-contained builds are larger but remove the need for separate .NET runtime installation.