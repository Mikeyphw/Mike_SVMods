# CHECKLIST — EggsInWild (WildAnimals JSON Editing)

This checklist tracks the phased work defined in `docs/PLAN.md`.

## Phase 0 — Docs + tracking
- [x] Keep `docs/PLAN.md`, `docs/CHECKLIST.md`, `docs/TODO.md` aligned as phases/steps complete.

## Phase 1 — Override schema + normalization (safe core)
- [x] Extend WildAnimal override config with scalar nullable fields (Enabled/Price/LargeEgg/IsMagic).
- [x] Normalize override keys (trim + case-insensitive match to known WildAnimals).
- [x] Remove unknown keys from overrides.
- [x] Remove overrides where all fields are null.
- [x] Apply overrides in `ConfiguredModDataLoader` to produce effective WildAnimals.
- [x] Add debug logs for normalization/removals.

## Phase 2 — GMCM per-animal editor (scalar fields)
- [x] Add GMCM options for Enabled/Price/LargeEgg/IsMagic on each WildAnimal page.
- [x] Add reset-all overrides per animal.
- [x] Show base/override/effective values as read-only text.
- [x] Add i18n keys for all UI labels/helptext (default).
- [x] Verify config-save behavior (preserve other overrides when toggling a single field).

## Phase 3 — Dev commands (inspection + safe edits)
- [x] Keep existing `wegg tomb force_patches` intact.
- [x] Add `wegg wildanimals dump_effective`.
- [x] Add `wegg wildanimals status <wildAnimalKey>`.
- [x] Add `wegg wildanimals set_enabled <wildAnimalKey> <true|false|1|0>`.

## Phase 4 — GMCM diagnostics (raw sources + resolved IDs)
- [x] Show per-animal `ExtraModAnimalKeys` blocks (Vanilla + modded), loaded=true/false, raw `FarmAnimalKeys`.
- [x] Show resolved effective `FarmAnimalIds` after installed-mod filtering + dedupe.

## Phase 5 — Polish + dev-output hardening
- [x] Fill missing i18n keys for WildAnimals GMCM + dev-command usage/errors.
- [x] Harden dev command JSON output to include overrides + source (default/override) per scalar.
- [x] Refresh docs to reflect Phases 1–5 completion.

## Phase 6 — LocationTypes editor
- [x] Extend WildAnimal override config with LocationTypes overrides.
- [x] Normalize LocationTypes override keys (trim + case-insensitive match to base when possible).
- [x] Remove redundant LocationTypes overrides matching base JSON.
- [x] Apply LocationTypes overrides in `ConfiguredModDataLoader`.
- [ ] Add GMCM UI controls for LocationTypes.
- [ ] Add i18n keys + docs updates.

## Phase 7 — ExtraModAnimalKeys editor
- [x] Extend WildAnimal override config with ExtraModAnimalKeys overrides.
- [x] Normalize ExtraModAnimalKeys override blocks and FarmAnimalKeys lists.
- [x] Remove redundant ExtraModAnimalKeys overrides matching base JSON.
- [x] Apply ExtraModAnimalKeys overrides in `ConfiguredModDataLoader`.
- [ ] Add GMCM UI for mod-group blocks and FarmAnimalKeys CSV editing.
- [ ] Update diagnostics/dev outputs to show override sources for complex fields.
- [ ] Add i18n keys + docs updates.
