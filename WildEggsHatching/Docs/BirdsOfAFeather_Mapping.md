# BirdsOfAFeather mapping (WildEggsHatching Phase 8)

WildEggsHatching depends on **BirdsOfAFeather** and intentionally reuses BOAF helpers instead of re‑implementing them.

## Guardrails
- **Fail-fast** if BirdsOfAFeather isn't installed (UniqueID: `Mikew.birdsofafeather`).
- **Fail-fast** if any local copies exist under `WildEggsHatching/` (no vendor/duplicate BOAF binaries).

## Reused BOAF helpers

### Localization
- **BOAF:** `Mikew.BirdsOfAFeather.Helpers.I18TranslationHelper`
- **Used by:** `Infrastructure/Content/ContentContext`, `Application/AnimalNotes/AnimalNotesOrchestrator`, content editors via `GetOrDefault`.

### Logging
- **BOAF:** `Mikew.BirdsOfAFeather.Helpers.Logger`
- **Adapter:** `Infrastructure/Logging/BoafLog` (routes `ILog` through BOAF Logger).

### Asset loading
- **BOAF:** `Mikew.BirdsOfAFeather.Helpers.ModAssetHandler`
- **Used by:** `Infrastructure/Assets/BoafAssetRegistry`
- **Notes:** assets must exist under lowercase `assets/<folder>/<name>.<ext>`.
  - Example: `assets/mines/level77377.tmx`, `assets/mines/z_tombtilesheet.png`.

### GameStateQuery evaluation
- **Removed wrappers:** `IGameStateQueryEvaluator` + `SmapiGameStateQueryEvaluator`
- **Now:** direct `StardewValley.GameStateQuery.CheckConditions(...)`.

## Not replaced (mod-specific)
- DesertTomb loot/spawn planners (domain logic specific to this mod).
