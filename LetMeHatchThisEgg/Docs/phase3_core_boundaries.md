# Phase 3 (Core extraction) — boundaries snapshot

This doc is a lightweight “seam map” used to keep the refactor honest.

## Pure Core logic (no SMAPI)
- Chance rolls + deterministic RNG seeding
- Egg assignment rules (who gets which egg, chance clamping, weighted choice)
- Clone mapping rules (later)
- Config normalization/validation (later)

## Infrastructure (SMAPI / Stardew types)
- Reading/writing `Data/FarmAnimals`, `Data/Objects`, `Data/Machines`
- Access to `Game1.random`, daily luck, world state
- Content event wiring (`AssetRequested`)
- Logging + i18n + GMCM UI

## Current step delivered
- `Core/Random/DeterministicSeed` (stable FNV-1a seeding)
- `Core/Breeding/EggSelectionService` using BoAF `IRandomProvider`
- `BreedingSystem` now takes an optional BoAF RNG provider and uses `EggSelectionService` instead of `Game1.random` directly.
