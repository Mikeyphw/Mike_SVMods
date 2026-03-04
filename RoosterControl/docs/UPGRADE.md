# RoosterControl Upgrade Notes

This document summarizes player-facing changes that may matter when upgrading RoosterControl.

## Phase 9–13: Compat catalogs + selections

RoosterControl now supports **compat catalogs** that allow multiple mods/content packs to contribute:
- **Egg sources** per logical `EggKind` (brown/white/blue/void/gold + large + fertilized + rooster egg).
- **Rooster providers** that own the coop rooster scan overrides and farm-animal key detection.

### Save data changes
- The mod persists compat selections into the save-state under the mod's save data.
- On load, RoosterControl repairs invalid or missing selections deterministically:
  - If a saved selection is invalid (missing/removed), it will be cleared to `(unset)`.
  - If a catalog is empty, selections are cleared and a single actionable warning is logged.

### Behavior changes
- Coop rooster scan overrides are always derived from the enabled rooster providers.
  - If the enabled providers list is empty, overrides are cleared to avoid stale behavior.
- Invalidations are batched and applied on the next tick (NestCore batching), avoiding excessive cache churn.

### Commands
- `ra_compat list` — show discovered compat sources (read-only).
- `ra_compat status` — single-block health + current selection dump.
- `ra_compat validate` — optional validation report for FarmAnimalRef resolution.

## GMCM notes
- With GMCMOptions installed, the compat pickers render as arrow-cycle image pickers.
- Without GMCMOptions, the UI falls back to stable text options.

## Troubleshooting
- If you see “no valid sources found” for an egg kind:
  - Ensure at least one compat catalog provides a valid source for that kind.
  - Check the SMAPI log for the actionable asset names that were empty/missing.
