# TODO — LetMeHatch

## Repo Phase R1 — GMCM id/i18n Normalization

- [x] Phase R1A: Define repo GMCM id and i18n normalization rules in docs
- [x] Phase R1B: Normalize LetMeHatch GMCM ids and i18n keys
- [x] Phase R1C: Prune RoosterControl legacy GMCM i18n aliases
- [x] Phase R1D: Verify EggsInWild GMCM ids already normalized
- [x] Phase R1E: Finalize repo GMCM id/i18n normalization

See [Refactor/R1 Documentation](refactor/Repo-wide_GMCM_ID_i18n_Normalization_R1D.md) for full details.

---

## Repo Phase R2 — Monolithic File Splits

### EggsInWild

- [ ] EIW-R2-1: Split DebugKnobs config types into files
- [ ] EIW-R2-2: Split WeggVerifier helper types into files
- [ ] EIW-R2-3: Extract Dump* models from AnimalNotesOrchestrator
- [ ] EIW-R2-4: Split AnimalNotesOrchestrator into partial files

### LetMeHatch

- [ ] LMH-R2-1: Move #region Eggs into partial file
- [ ] LMH-R2-2: Move #region Items into partial file

### RoosterControl

- [ ] RC-R2-1: Extract debug record models into file
- [ ] RC-R2-2: Split debug methods into partial file

**Reference**: [Repo-wide R2 Documentation](refactor/Repo-wide_R2_Monolithic_File_Splits.md)
