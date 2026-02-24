# Pre-build validation (local, offline)

This patch is a **NestCore sweep** on the refactored EggsInWild codebase.

## What changed
- Centralized Desert Tomb mine-map keys in `Core/Assets/WeggGameAssetNames.cs` and replaced scattered string literals.
- Removed unused NestCore-duplicate asset registry (`Infrastructure/Assets/NestCoreAssetRegistry.cs`).
- Consolidated test projects under the shared repo `Tests/` folder.

## Remaining string-literal game keys
These are intentionally left as literals because they are either:
- Mod-specific integration keys (not a vanilla asset name), or
- Centralized in `WeggGameAssetNames`.

Current literals detected in C#:
- `Characters/Monsters` (used for mod texture routing in `ModEntry`).
- `Data/Mines/Level14`, `Data/Mines/28.tmx`, `Maps/Mines/SomeOtherMap` (tests only).
- All Desert Tomb mine-map keys are now centralized in `WeggGameAssetNames`.

## How to build locally (repo root)
```bash
dotnet --info
dotnet build Mike_SVMods/EggsInWild/EggsInWild.csproj -c Release
dotnet test Mike_SVMods/Tests/EggsInWild.Tests/EggsInWild.Tests.csproj -c Release
dotnet test Mike_SVMods/Tests/EggsInWild.AnimalNotes.Tests/EggsInWild.AnimalNotes.Tests.csproj -c Release
```

If build fails due to NestCore reference issues, confirm:
- `NestCore` is referenced (ProjectReference or assembly reference) and up-to-date.
- The mod manifest declares NestCore as a dependency.
