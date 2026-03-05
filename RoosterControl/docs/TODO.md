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
- [ ] 16D: add `RoosterControlGmcmMenu` as the layout orchestrator
- [ ] Decide whether `compat` remains coordinator-owned
- [ ] Decide whether `females` remains coordinator-owned
- [ ] Keep all GMCM page ids / option ids / i18n keys unchanged during Phase 16

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
