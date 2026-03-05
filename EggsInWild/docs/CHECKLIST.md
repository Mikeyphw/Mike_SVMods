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
- [x] Add GMCM UI controls for LocationTypes.
- [x] Add i18n keys + docs updates.

## Phase 7 — ExtraModAnimalKeys editor
- [x] Extend WildAnimal override config with ExtraModAnimalKeys overrides.
- [x] Normalize ExtraModAnimalKeys override blocks and FarmAnimalKeys lists.
- [x] Remove redundant ExtraModAnimalKeys overrides matching base JSON.
- [x] Apply ExtraModAnimalKeys overrides in `ConfiguredModDataLoader`.
- [x] Add GMCM UI for mod-group blocks and FarmAnimalKeys CSV editing.
- [x] Add static GMCM helper to add/remove custom modId override blocks (no dynamic search).
- [x] Add per-block 'reset to default' helper for mod-group blocks.
- [x] Update diagnostics/dev outputs to show override sources for complex fields.
- [x] Add i18n keys + docs updates.

## Phase 8 — Polish/hardening
- [x] Enhance dev commands (status/dump_effective) to show override sources for complex fields.
- [x] Add base-vs-effective diffs to dev command output.
- [x] Guard against accidental config churn (only save when normalization changes).
- [x] Docs + checklist alignment.

## Phase 9 — GMCM bulk UX + validation (post-Phase 8)
- [x] Phase 9A: bulk header + skip-noop Bulk Apply.
- [x] Phase 9B: export effective blocks as `modId | csvKeys` to SMAPI log.
- [x] Phase 9C: surface richer bulk validation warnings (unknown modIds, blank/duplicate keys, keys not in game data when available).
- [x] Phase 9D: copy export into bulk stage (overwrite/append) for round-trip editing.
- [x] Phase 9E: maintenance pass (dedupe bulk planning + export helper; reuse cached game-data load).


## Phase 10 — Bulk tooling parity (tests + dev commands)
- [x] Phase 10C: add unit tests covering bulk parser + planner behavior (set/clear/no-op).
- [x] Phase 10D: add dev commands for export_effective_bulk + validate_bulk.
- [ ] Add dev-command-focused tests for export/validate output formatting.

## Phase 18C — NestCore-first GMCM consolidation
- [x] Keep `EiwGmcmMenu` as the single GMCM layout orchestrator.
- [x] Keep page bodies under `Infrastructure/Integrations/Gmcm/Pages/*`.
- [x] Replace fake-bool one-shot GMCM command controls with `Action(...)`.
- [x] Move/remove duplicated Animal Progression GMCM page logic from `ModEntry.Gmcm.cs`.
- [x] Preserve save-aware rebuild behavior and known-location-type normalization handoff.
- [x] Verify build gate (`NestCore`, `LetMeHatch`, `EggsInWild`, `RoosterControl`, solution).

## Phase 18D — Repo GMCM convention alignment
- [x] EggsInWild docs mark NestCore-first GMCM plumbing as the standard.
- [x] Docs confirm `EiwGmcmMenu` + `Pages/*` architecture alignment.
- [x] Docs confirm one-shot commands use `Action(...)`.

## Phase 18E — Final validation
- [x] Re-scan found no remaining fake-bool pseudo-button action controls.
- [x] Remove dead legacy GMCM partial layout files from `ModEntry.Gmcm*`.
- [x] Confirm final architecture is `EiwGmcmMenu` + `Infrastructure/Integrations/Gmcm/Pages/*`.
