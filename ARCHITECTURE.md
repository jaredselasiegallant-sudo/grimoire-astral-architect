# Architecture Decision Record: UI Framework

## Status

**Accepted** (2026-08-02). This decision is a binding record — do not reverse it
without a new ADR documenting the rationale and the failure history it replaces.

## Decision

The UI layer (`Grimoire.App`) was migrated from **WinUI 3 (Windows App SDK)** to
**WPF + SkiaSharp**. `Grimoire.Core`, `Grimoire.Data`, and `Grimoire.Engine` were
kept **unchanged in intent** — they were already framework-agnostic.

## Why: the failure history that led here

The project shipped three WinUI 3 releases (v0.1.x). Every release experienced
some form of the same class of failure:

| Failure | Root cause |
|---|---|
| SingleFile + `EnableMsixTooling` cascading build failures | WinUI 3's supported deployment is MSIX; single-file unpackaged is an unsupported corner |
| `XamlCompiler.exe` (net472) crashes during CI builds | WinUI 3's XAML compiler is a VS2022 toolchain, not part of `dotnet build` |
| RID mismatches (`win10-*` vs `win-*`) | WASDK 1.x injects its own RIDs that conflict with the .NET SDK |
| `resources.pri` missing at launch | Self-contained WASDK runtime deployment is fragile |
| `XamlParseException` at `InitializeComponent` on a bare `Window`+`Grid`+`TextBlock`, on the user's machine only | Self-contained WASDK runtime bootstrap failing to load the WinUI runtime DLLs — even with zero app XAML content |

The last item was the smoking gun: the crash occurred with **no app code at all**
in the window. The problem was never the game code — it was the deployment shape.

## Options considered

### A. Keep WinUI 3, package properly as MSIX

The "supported" path. Trade-offs:
- Requires a code-signing certificate for non-Store installs (SignPath Foundation
  is free but slow and open-source-only; commercial OV certs are ~$150–300/yr)
- MSIX installer UX instead of extract-and-run
- Local development still requires VS2022 (`XamlCompiler` is not `dotnet build`)
  — the user could never build locally, which was a core blocker
- Does not eliminate the class of problem; it only makes WinUI 3 "official"

### B. WPF + SkiaSharp ✅ (chosen)

- `dotnet publish` works from the .NET SDK alone — **local builds restored**
- No MSIX, no XamlCompiler, no WASDK runtime bootstrapping, no `resources.pri`,
  no RID wars — the entire failure class is eliminated
- `Grimoire.Engine` already used SkiaSharp 2.88.8 with `SkiaSharp.Views.Desktop`
  surfaces; the WPF `SKElement` control is a near drop-in for the old canvas
- MVVM (`CommunityToolkit.Mvvm`), ViewModels, DI, and logging carried over unchanged
- Folder or single-file publish; extract → run; ideal for non-technical users
- Trade-offs accepted: WPF's pen/touch stack is older than WinUI 3's (the current
  gesture engine uses pointer position/timestamps, so this is a non-issue today);
  WPF lacks WinUI's fluent styling (it is themed with custom brushes here)

### C. Avalonia UI

Modern, cross-platform, `dotnet publish`-friendly. Rejected because the game spec
is Windows-only and 100% offline — cross-platform capability is cost without benefit,
and WPF is more proven for this exact shape (Windows-only desktop + SkiaSharp canvas).

## What was preserved vs replaced

| Layer | Status |
|---|---|
| `Grimoire.Core` | **Untouched** — domain models, enums, interfaces, services, events. Already `net8.0`, no UI deps |
| `Grimoire.Data` | **Bug-fixed only** — schema v5 (see below). SQLite persistence, repositories |
| `Grimoire.Engine` | **Untouched** — SkiaSharp rendering, gesture recognition, particles, audio |
| `Grimoire.Core.Tests` | Untouched, still green |
| `Grimoire.App` | **Replaced** — WPF window, `SKElement` canvas, WPF input/Dispatcher, JSON settings (replaced `Windows.Storage`) |

## Deployment model

- **Self-contained folder publish**: `dotnet publish -r win-x64 --self-contained true`
- No runtime installs required (`.NET` and native `libSkiaSharp.dll` bundled)
- Distributed as a zip; user extracts and runs `Grimoire.App.exe`
- Minimum OS: Windows 10 (any recent build) / Windows 11

## Bugs fixed during the migration

The rewrite was verified by an actual local smoke test (the app is launched and run
for 10+ seconds against a real database). This surfaced five latent bugs that had
never been reachable because the app previously crashed before game code ran:

1. `MainViewModel` read `CurrentState` before `InitialiseAsync` — added
   `IGameStateService.IsInitialised` and `RefreshFromState()`.
2. The canvas painted during the first layout pass, before state init — the
   `PaintSurface` handler now renders a blank frame until initialised.
3. `SeedDefaultDataAsync` referenced v3/v4 tables before they existed — singleton
   seeding now runs after all migrations complete.
4. The v3 `CorruptionState` table was missing three columns the repository reads —
   schema fixed and a v5 migration repairs existing databases.
5. `AstralEventScheduler` could index templates with a negative array index when
   `GetHashCode()` returned a negative value — added `PositiveMod`.

## Schema version

`Grimoire.Data` schema is now **v5** (see `DatabaseInitializer`). Migrations are
idempotent and run automatically at startup.

## CI

`.github/workflows/release.yml` is written from scratch for WPF:
build → unit tests → self-contained publish → verify exe/SkiaSharp DLL →
real smoke test (launch, wait, **fail on crash log**) → zip → GitHub Release.

## Signing

Unchanged intent: SignPath Foundation remains the preferred free signing path for
open source (see `BUILD.md`). The CI step is present but disabled until approved.
