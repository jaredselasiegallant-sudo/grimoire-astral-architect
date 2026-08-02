# Grimoire: Astral Architect

A cozy base-builder/idle RPG with gesture-based spell casting, familiar bonding, and alchemical crafting. 100% offline — no account, no internet, no microtransactions.

## Tech Stack

- **.NET 8** / **C# 12**
- **WPF** for the UI shell (see `ARCHITECTURE.md` for why)
- **SkiaSharp** for 2D rendering
- **SQLite** (WAL mode) for persistence
- **CommunityToolkit.Mvvm** for data binding

## Download & Install

1. Go to the [Releases](https://github.com/jaredselasiegallant-sudo/grimoire-astral-architect/releases) page.
2. Download `Grimoire-Astral-Architect-win-x64.zip` from the latest release.
3. Extract the zip to a folder of your choice (e.g. `C:\Games\Grimoire`).
4. Run `Grimoire.App.exe`.
5. If Windows SmartScreen shows a warning, click **More info** → **Run anyway**.

No installation required. The app is fully self-contained (no .NET runtime download needed). Works on Windows 10 and Windows 11.

## Building from Source

Requires only the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0). No Visual Studio required.

```bash
dotnet restore
dotnet build
dotnet test
```

To publish a self-contained folder:

```bash
dotnet publish src/Grimoire.App/Grimoire.App.csproj -c Release -r win-x64 --self-contained true -o ./publish
```

Run `./publish/Grimoire.App.exe`.

## Architecture

| Project | Purpose |
|---------|---------|
| `Grimoire.Core` | Domain models, enums, interfaces, services |
| `Grimoire.Data` | SQLite persistence layer (schema v5, auto-migrating) |
| `Grimoire.Engine` | SkiaSharp rendering, gesture recognition, particles, music |
| `Grimoire.App` | WPF shell, game loop, input handling |
| `Grimoire.Core.Tests` | xUnit unit tests |

See `ARCHITECTURE.md` for the full design record.

## License

MIT
