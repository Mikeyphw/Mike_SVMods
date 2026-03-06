# RoosterControl TODO

## Next: Phase 15 — Compat test matrix + content-pack examples
- [x] Compat matrix doc
- [x] Example templates/content packs + schema notes
- [ ] Reference patterns (match types + gating)
- [ ] Unit tests: catalog loading + deterministic repair
- [ ] Unit tests: fertilized mapping determinism (native vs synthesized)
- [ ] Manual QA checklist + smoke-test notes
- [ ] Docs wired to templates + matrix

## After Phase 15: Phase 16 — GMCM standardization slice
- [x] 16A: adopt NestCore GMCM batching helpers in RoosterControl
- [x] 16B: extract root/probabilities/debug page builders into `Integrations/Gmcm/Pages/*`
- [x] 16C: extract rooster page registration and editors into `RoosterTypePages`
- [x] 16D: add `RoosterControlGmcmMenu` as the layout orchestrator
- [x] Decide whether `compat` remains coordinator-owned
- [x] Decide whether `females` remains coordinator-owned
- [x] Keep all GMCM page ids / option ids / i18n keys unchanged during Phase 16
- [x] Phase 17: Document GMCM page ownership decisions (both kept coordinator-owned due to runtime coupling)

## R2 — Monolithic file splits

### Phase RC-R2-1 — Extract debug record models
- [ ] Create `RoosterControl/Helpers/RoosterDebugModels.cs`
- [ ] Move 5 debug record types to new file
- [ ] Run build gate
- [ ] Commit: `r2-rc-01: extract RoosterHelper debug record models into file`

### Phase RC-R2-2 — Split debug methods into partial file
- [ ] Convert `RoosterHelper` to `partial class`
- [ ] Create `RoosterHelper.Debug.cs`
- [ ] Move 4 debug methods to new file
- [ ] Run build gate
- [ ] Commit: `r2-rc-02: split RoosterHelper debug methods into partial file`

## Done
- [x] Phase 14: Actionable validation messaging
- [x] Phase 14: Reset affordances (GMCM arm+save + console commands)
- [x] Phase 14: GMCM Compat page layout pass (grouping/headings/spacing)
- [x] Phase 14: Picker affordances (icons/labels) + robust fallback without GMCMOptions
- [x] Phase 14: QoL toggle — dedicated All providers mode
- [x] Phase 13: Docs: upgrade notes + release checklist
- [x] Phase 13: Build/test verified + `ra_compat status` + GMCM i18n for empty sources
- [x] Phase 13: Safe defaults for empty/missing catalogs (empty catalogs, null fields, actionable warnings)
- [x] Phase 13: Fail-fast checks + actionable errors
- [x] Phase 13: Harden selection repair/persistence logs + no-op clarity
- [x] Phase 13: Clear coop rooster scan overrides on empty selection (prevents stale overrides)
- [x] Phase 12: GMCM compat selection UI (arrow-cycle pickers when GMCMOptions is installed + inline validation notes)
- [x] Phase 11C: fertilized egg synthesis + deterministic IDs


## R5 — StardewUI White Rooster Produce Editor
- [x] Implement White rooster StardewUI produce editor behind keybind
- [ ] Verify build

## Duplicate StardewUI shells
- [x] Phase SUI-SML-1: add inert `RoosterCompatEditor.sml`
- [x] Phase SUI-SML-2: add inert `RoosterFemalesEditor.sml`
- [ ] Wire duplicate RoosterControl StardewUI shells in a later code phase

## StardewUI compat migration
- [x] Phase SUI-1: extract shared compat editor service/catalog and route GMCM compat reads through it
- [x] Phase SUI-2: register duplicate StardewUI compat editor and GMCM launcher
- [ ] Phase SUI-3: register duplicate StardewUI females editor and GMCM launcher
