# 📋 Release & WinGet Update Flow

## Alur Otomatis

```
1. User jalankan: .\Scripts\release-version.ps1 -NewVersion X.X.X
   ↓
2. Script push tag vX.X.X
   ↓
3. build-release.yml trigger
   ├─ Build app
   ├─ Create Release (DRAFT)
   ├─ Upload assets
   └─ Publish Release (auto-triggered)
   ↓
4. winget-releaser.yml trigger (saat Release di-publish)
   ├─ Pull dari faizinuha/winget-pkgs
   ├─ Update manifest versi terbaru
   └─ Submit PR ke microsoft/winget-pkgs
   ↓
5. 🤖 Auto-Merge PR di ZeroMix (fix/release-publish)
   ├─ Tunggu CI lulus
   └─ Auto-squash merge ke main
   ↓
6. 📥 Microsoft review PR (~1-3 hari kerja)
   ├─ Auto-merge di microsoft/winget-pkgs
   └─ (atau manual review → merge)
   ↓
7. ✅ User bisa: winget install Zeromix.ZeroMix
```

## Key Points

- **release-version.ps1** → Update versi, commit, push tag
- **build-release.yml** → Build, upload, publish (tidak race condition lagi)
- **winget-releaser.yml** → Auto-submit PR ke WinGet (vedantmgoyal9 action)
- **auto-merge-release.yml** → Auto-merge fix/release-* PR di ZeroMix
- **WINGET_ACC_TOKEN** → GitHub PAT untuk fork faizinuha/winget-pkgs

## PR Microsoft WinGet

PR yang di-submit vedantmgoyal9/winget-releaser ke microsoft/winget-pkgs perlu di-approve Microsoft.
Bisa monitor di: https://github.com/microsoft/winget-pkgs/pulls

Kalo ada issue, microsoft akan comment di PR. WinGet bot team biasanya fast-respond.

## Test Release

Dry-run dulu:
```powershell
.\Scripts\release-version.ps1 -NewVersion X.X.X -DryRun
```

Kalo OK, jalankan beneran:
```powershell
.\Scripts\release-version.ps1 -NewVersion X.X.X
```

Terus monitor di Actions → kelilingi workflow runner sampai winget-releaser submit PR.
