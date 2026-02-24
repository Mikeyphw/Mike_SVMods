# NestCore mapping (EggsInWild Phase 8)

EggsInWild depends on **NestCore** and intentionally NestCorees NestCore helpers instead of re‑implementing them.

## Guardrails
- **Fail-fast** if NestCore isn't installed (UniqueID: `Mikew.nestcore`).
- **Fail-fast** if any local copies exist under `EggsInWild/` (no vendor/duplicate NestCore binaries).

## Reused NestCore helpers

### Localization
- **NestCore:** `Mikew.NestCore.Helpers.I18TranslationHelper`
- **Used by:** `Infrastructure/Content/ContentContext`, `Application/AnimalNotes/AnimalNotesOrchestrator`, content editors via `GetOrDefault`.

### Logging
- **NestCore:** `Mikew.NestCore.Helpers.Logger`
- **Adapter:** `Infrastructure/Logging/NestCoreLog` (routes `ILog` through NestCore Logger).

### Asset loading
- **NestCore:** `Mikew.NestCore.Helpers.ModAssetHandler`
- **Used by:** `Infrastructure/Assets/NestCoreAssetRegistry`
- **Notes:** assets must exist under lowercase `assets/<folder>/<name>.<ext>`.
  - Example: `assets/mines/level77377.tmx`, `assets/mines/z_tombtilesheet.png`.

### GameStateQuery evaluation
- **Removed wrappers:** `IGameStateQueryEvaluator` + `SmapiGameStateQueryEvaluator`
- **Now:** direct `StardewValley.GameStateQuery.CheckConditions(...)`.

## Not replaced (mod-specific)
- DesertTomb loot/spawn planners (domain logic specific to this mod).
