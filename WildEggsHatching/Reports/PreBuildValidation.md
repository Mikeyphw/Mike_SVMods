# Pre-build validation (local, offline)

This patch is a **BOAF sweep** on the refactored WildEggsHatching codebase.

## What changed
- Centralized Desert Tomb mine-map keys in `Core/Assets/WeggGameAssetNames.cs` and replaced scattered string literals.
- Removed unused BOAF-duplicate asset registry (`Infrastructure/Assets/BoafAssetRegistry.cs`).
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
dotnet build Mike_SVMods/WildEggsHatching/WildEggsHatching.csproj -c Release
dotnet test Mike_SVMods/Tests/WildEggsHatching.Tests/WildEggsHatching.Tests.csproj -c Release
dotnet test Mike_SVMods/Tests/WildEggsHatching.AnimalNotes.Tests/WildEggsHatching.AnimalNotes.Tests.csproj -c Release
```

If build fails due to BOAF reference issues, confirm:
- `BirdsOfAFeather` is referenced (ProjectReference or assembly reference) and up-to-date.
- The mod manifest declares BOAF as a dependency.
