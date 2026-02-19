# PreBuildValidation (LetMeHatchThisEgg)

This is a lightweight, WEH-inspired guardrail to keep the refactor honest:

- **Core** must stay pure (no SMAPI / StardewValley / XNA references).
- **No BOAF-local duplicates** may exist in LMH (BOAF owns plumbing).

## Quick run

### PowerShell (Windows)

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\Tools\Validate-Core.ps1
```

### Bash (macOS/Linux)

```bash
bash ./Tools/validate-core.sh
```

## What it checks

### 1) Core purity

Scans `Core/**/*.cs` for forbidden references:

- `StardewValley`
- `StardewModdingAPI`
- `SMAPI`
- `Microsoft.Xna.Framework`

> If you need these, create an **Infrastructure gateway** and pass data via **Application**.

### 2) BOAF duplicate regression

Fails if LMH contains any of these BOAF-equivalent re-implementations:

- `SmapiConfigProvider.cs`
- `ILog.cs`
- `IAssetEditor.cs`
- `AssetEditorBase.cs`
- `SmapiContentCoordinator.cs`
- `GameAssets.cs`

(These must come from BOAF.)

## Updating the checks

If BOAF adds a new shared helper and LMH starts duplicating it, add the filename (or a path pattern) to the duplicate list in the scripts.
