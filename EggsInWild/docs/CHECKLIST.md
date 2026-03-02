# CHECKLIST — EggsInWild (WildAnimals JSON Editing)

## Phase 0 — Docs + tracking
- [ ] Update `docs/PLAN.md`, `docs/CHECKLIST.md`, `docs/TODO.md` as phases/steps complete.

## Phase 1 — Override schema + normalization (safe core)
- [ ] Extend WildAnimal override config with scalar nullable fields (Enabled/Price/LargeEgg/IsMagic).
- [ ] Normalize override keys (trim + case-insensitive match to known WildAnimals).
- [ ] Remove unknown keys from overrides.
- [ ] Remove overrides where all fields are null.
- [ ] Apply overrides in `ConfiguredModDataLoader` to produce effective WildAnimals.
- [ ] Add debug logs for normalization/removals.

## Phase 2 — GMCM per-animal editor (scalar fields)
- [ ] Add GMCM options for Enabled/Price/LargeEgg/IsMagic on each WildAnimal page.
- [ ] Add reset-all overrides per animal.
- [ ] Show base/override/effective values as read-only text.
- [ ] Add i18n keys for all UI labels/helptext (default + pt-br if present).
- [ ] Verify config-save/invalidation behavior.

## Phase 3 — Dev commands (inspection + safe edits)
- [ ] Add `wegg wildanimals dump_effective`.
- [ ] Add `wegg wildanimals status <wildAnimalKey>`.
- [ ] Add `wegg wildanimals set_enabled <wildAnimalKey> <true|false|1|0>`.
- [ ] Preserve existing `wegg tomb force_patches`.

## Phase 4 — Diagnostics (raw sources + effective ids display)
- [ ] Show `ExtraModAnimalKeys` blocks (vanilla + modded) and loaded flags.
- [ ] Show raw farm-animal keys derived from blocks.
- [ ] Show resolved effective farm-animal ids (post filtering + dedupe).

## Phase 5 — Polish + cleanup
- [ ] Ensure cleanup runs consistently and config remains minimal.
- [ ] Final docs pass.
