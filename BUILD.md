# Build Configuration

## Known-Good Versions

| Component | Version | Notes |
|-----------|---------|-------|
| .NET SDK | 8.0.x (latest feature band) | Pinned via `global.json` with `rollForward: latestFeature` |
| Windows App SDK | 1.8.260710003 | Latest stable 1.8 servicing release. Fixes XamlCompiler bug from 1.5/1.6. Uses standard `win-*` RIDs (no `win10-*` injection). |
| Windows SDK BuildTools | 10.0.22621.3233 | Provides WinRT projections for `net8.0-windows10.0.19041.0` |
| CommunityToolkit.Mvvm | 8.2.2 | |
| SkiaSharp.Views.WinUI | 2.88.8 | |
| Microsoft.Extensions.* | 8.0.0 / 8.0.1 | |

## Target Framework

`net8.0-windows10.0.19041.0` — targets Windows 10 2004+ (19041). Pinned explicitly; do not change without testing.

## Runtime Identifier

`win-x64` — used consistently in csproj (`Platform`), `dotnet publish -r`, and CI workflow. WASDK 1.8 uses standard `win-*` RIDs; the old `win10-*` RIDs from WASDK 1.5 are gone.

## Publish Strategy: Self-Contained Folder

```
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish
```

### Why folder publish (not SingleFile)?

| Approach | Pros | Cons |
|----------|------|------|
| **SingleFile** (`PublishSingleFile=true`) | Single .exe, ~50 MB | Requires `EnableMsixTooling=true` + `WindowsAppSDKSelfContained=true`. Triggers `Microsoft.WindowsAppSDK.SingleFile.targets` which has had cascading CI failures across WASDK versions. The XamlCompiler.exe (net472) crashes on CI in some configurations. |
| **Folder** (default) | Zero CI fragility, no special MSBuild targets invoked, XAML compilation works reliably with WASDK 1.8 | ~150 MB output, multiple files in publish dir |

**Chosen: Folder publish.** The game distributes as a zip of the publish folder. Users extract and run `Grimoire.App.exe`. The extra 100 MB is acceptable for a game distribution.

### Why self-contained?

- Users don't need .NET 8 runtime installed
- WASDK runtime is bundled (`WindowsAppSDKSelfContained=true`) — no separate WASDK installer needed
- Total zip size ~150 MB, acceptable for a game

### Why NOT framework-dependent?

- Requires users to install both .NET 8 runtime AND WASDK runtime separately
- Adds two installation steps before the game can run
- Unacceptable for a consumer game distribution

## WASDK Version History (why 1.8)

| WASDK | Issue |
|-------|-------|
| 1.5.x | XamlCompiler.exe (net472) crashes silently on CI with exit code 1. Injects `win10-*` RIDs that .NET 8 SDK doesn't recognize (NETSDK1083). Requires `EnableMsixTooling` for SingleFile. |
| 1.6.x | Same XamlCompiler.exe crash. Different `win10-*` RID handling. |
| 1.7.x | Transitional. |
| **1.8.x** | **Fixes XamlCompiler bug** (confirmed by Microsoft: "fixed in current releases — WASDK >= 1.8 on the 1.x line"). Uses standard `win-*` RIDs. Stable self-contained folder publish. |

## CI Workflow

The GitHub Actions workflow (`.github/workflows/release.yml`) triggers on `v*.*.*` tags:

1. **Checkout** → **Setup .NET 8** → **dotnet publish** (self-contained folder, `-v normal`)
2. **Verify publish output** — checks `Grimoire.App.exe`, WASDK runtime DLLs, and SkiaSharp native
3. **Smoke test** — launches the exe, waits 5 seconds, checks for crash logs
4. **Zip** → **Create GitHub Release**
5. (Optional) **SignPath signing** — signs the exe if SignPath is configured

### Upgrading WASDK

When upgrading WASDK:
1. Update `Microsoft.WindowsAppSDK` version in `Grimoire.App.csproj`
2. Verify the new version is in the 1.8.x or later stable line (not experimental/preview)
3. Check release notes for breaking changes to `WindowsAppSDKSelfContained` or XAML compilation
4. Tag and push — the CI workflow will validate the full publish pipeline

## Local Build

```bash
dotnet publish src/Grimoire.App/Grimoire.App.csproj -c Release -r win-x64 --self-contained true -o ./publish
```

Output: `./publish/Grimoire.App.exe` + dependencies.

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
