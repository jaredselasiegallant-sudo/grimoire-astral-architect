# Build Configuration

## Known-Good Versions

| Component | Version | Notes |
|-----------|---------|-------|
| .NET SDK | 8.0.x (latest feature band) | Pinned via `global.json` with `rollForward: latestFeature` |
| WPF | Built into .NET 8 SDK | No separate package; `UseWPF=true` |
| SkiaSharp + SkiaSharp.Views.WPF | 2.88.8 | `SKElement` hosts the game canvas |
| CommunityToolkit.Mvvm | 8.2.2 | |
| Microsoft.Extensions.* | 8.0.0 / 8.0.1 | |
| Microsoft.Data.Sqlite | 8.0.6 | SQLite schema v5, auto-migrating |

## Target Framework

`net8.0-windows` — Windows-only, built-in WPF support. No Windows SDK build tools,
no WinRT projections, no Windows App SDK. This is the whole point.

## Runtime Identifier

`win-x64` — used consistently in `dotnet publish -r` and the CI workflow.

## Publish Strategy: Self-Contained Folder

```
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish
```

### Why this works (and why WinUI 3 did not)

WPF is a first-class `dotnet publish` citizen:
- No MSIX, no `XamlCompiler`, no `resources.pri`, no WASDK runtime bootstrapping
- No VS2022 requirement — plain `dotnet build`/`dotnet publish` on any machine
- Native `libSkiaSharp.dll` is copied to the output root automatically

The previous WinUI 3 stack (WASDK 1.8) failed repeatedly under exactly this
deployment shape (unpackaged self-contained folder). See `ARCHITECTURE.md` for the
full decision record. **Do not reintroduce WinUI 3 without a new ADR.**

## Local Build

```bash
dotnet restore
dotnet build Grimoire.AstralArchitect.sln -c Release
dotnet test Grimoire.AstralArchitect.sln -c Release
dotnet publish src/Grimoire.App/Grimoire.App.csproj -c Release -r win-x64 --self-contained true -o ./publish
```

Output: `./publish/Grimoire.App.exe` + dependencies (~170 MB folder, ~60 MB zipped).

## CI Workflow

The GitHub Actions workflow (`.github/workflows/release.yml`) triggers on `v*.*.*` tags:

1. **Checkout** → **Setup .NET 8** → **restore** → **build** → **unit tests**
2. **dotnet publish** (self-contained folder)
3. **Verify publish output** — checks `Grimoire.App.exe`, `Grimoire.App.dll`, and `libSkiaSharp.dll`
4. **Smoke test** — launches the exe, waits 8 seconds, **fails the build if a crash log is written**
5. **Zip** → **Create GitHub Release**
6. (Optional) **SignPath signing** — signs the exe if SignPath is configured

## Code Signing

### Option 1: SignPath Foundation (Free for Open Source)

SignPath provides free code signing for qualifying open-source projects.

**Setup steps:**

1. **Apply at** https://signpath.io/foundation — register your GitHub repo
2. **Create a GitHub repository secret** named `SIGNPATH_API_TOKEN` with your SignPath API token
3. **Create GitHub repository variables:**
   - `SIGNPATH_ORG_ID` — your SignPath organization ID
   - `SIGNING_CONFIGURATION_ID` — the signing configuration to use (from SignPath dashboard)
4. **Uncomment the SignPath step** in `.github/workflows/release.yml`
5. **Tag and push** — the CI will sign the exe before uploading

**Requirements:**
- Public GitHub repository (ours is public ✓)
- Non-commercial / open-source project
- Code must be buildable from source (ours is ✓)

### Option 2: Self-Signed Certificate (Development)

For local testing only. Will trigger SmartScreen warnings for users.

```powershell
# Generate self-signed cert (run as Admin)
$cert = New-SelfSignedCertificate `
  -Subject "CN=Grimoire Astral Architect" `
  -Type CodeSigningCert `
  -CertStoreLocation Cert:\CurrentUser\My `
  -NotAfter (Get-Date).AddYears(2)

# Export PFX
$pfxPath = "$env:USERPROFILE\grimoire-signing.pfx"
Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password (ConvertTo-SecureString -String "password" -Force -AsPlainText)
```

### Option 3: Commercial OV Certificate

Purchased from a Certificate Authority (DigiCert, Sectigo, GlobalSign). ~$150-300/year.

```bash
# Sign with signtool.exe (from Windows SDK)
signtool.exe sign /f grimoire-signing.pfx /p "password" /tr http://timestamp.digicert.com /td sha256 /fd sha256 Grimoire.App.exe
```
