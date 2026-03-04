# RoosterControl TODO

## Next: Phase 13 — Release hardening
- [x] Performance pass (edits/invalidation batching)
- [x] Safe defaults for empty/missing catalogs
- [x] Build/test workflow verified

## Upcoming: Phase 14 — UX polish (non-optional)
- [ ] GMCM Compat page layout pass + reset affordances
- [ ] Picker affordances (icons/labels) + robust fallback without GMCMOptions
- [ ] Actionable validation messaging

## Later: Phase 15 — Compat test matrix + content-pack examples
- [ ] Compat matrix doc
- [ ] Example templates/content packs + schema notes
- [ ] Unit tests + manual QA checklist

## Done
- [x] Phase 13: Build/test verified + `ra_compat status` + GMCM i18n for empty sources
- [x] Phase 13: Safe defaults for empty/missing catalogs (empty catalogs, null fields, actionable warnings)
- [x] Phase 13: Fail-fast checks + actionable errors
- [x] Phase 13: Harden selection repair/persistence logs + no-op clarity
- [x] Phase 13: Clear coop rooster scan overrides on empty selection (prevents stale overrides)
- [x] Phase 12: GMCM compat selection UI (arrow-cycle pickers when GMCMOptions is installed + inline validation notes)
- [x] Phase 11C: fertilized egg synthesis + deterministic IDs
