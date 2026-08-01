# Grimoire: Astral Architect

A cozy base-builder/idle RPG with gesture-based spell casting, familiar bonding, and alchemical crafting. 100% offline — no account, no internet, no microtransactions.

## Tech Stack

- **.NET 8** / **C# 12**
- **WinUI 3** (Windows App SDK 1.5)
- **SkiaSharp** for 2D rendering
- **SQLite** (WAL mode) for persistence
- **CommunityToolkit.Mvvm** for data binding

## Download & Install

1. Go to the [Releases](https://github.com/jaredselasiegallant-sudo/grimoire-astral-architect/releases) page.
2. Download `Grimoire-Astral-Architect-win-x64.zip` from the latest release.
3. Extract the zip to a folder of your choice (e.g. `C:\Games\Grimoire`).
4. Run `Grimoire.App.exe`.
5. If Windows SmartScreen shows a warning, click **More info** → **Run anyway**. The build is unsigned — this is expected.

No installation required. The app is fully self-contained (no .NET runtime download needed).

## Building from Source

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) and [Visual Studio 2022](https://visualstudio.microsoft.com/) with the **.NET desktop development** and **Windows App SDK** workloads.

```bash
dotnet restore
dotnet build
```

To publish a self-contained single-file executable:

```bash
dotnet publish src/Grimoire.App/Grimoire.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

## Architecture

| Project | Purpose |
|---------|---------|
| `Grimoire.Core` | Domain models, enums, interfaces, services |
| `Grimoire.Data` | SQLite persistence layer |
| `Grimoire.Engine` | SkiaSharp rendering, gesture recognition, particles, music |
| `Grimoire.App` | WinUI 3 shell, game loop, input handling |
| `Grimoire.Core.Tests` | xUnit unit tests |

## License

MIT
