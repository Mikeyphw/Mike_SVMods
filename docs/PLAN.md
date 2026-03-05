# PLAN — GMCM Menu (LetMeHatch)

## Repo-wide GMCM ID / i18n Normalization (Phase R1)

See [Refactor/R1 Documentation](refactor/Repo-wide_GMCM_ID_i18n_Normalization_R1D.md) for details.

---

## Repo-wide R2 — Monolithic File Splits

See [Refactor/R2 Documentation](refactor/Repo-wide_R2_Monolithic_File_Splits.md) for the complete R2 plan.

### Summary

| Mod | Phase | Target File | Description |
|-----|-------|-------------|-------------|
| EggsInWild | EIW-R2-1 | DebugKnobs.cs | Split 15 config POCO classes |
| EggsInWild | EIW-R2-2 | WeggVerifier.cs | Split 8 helper types |
| EggsInWild | EIW-R2-3 | AnimalNotesOrchestrator.cs | Extract 5 Dump* models |
| EggsInWild | EIW-R2-4 | AnimalNotesOrchestrator.cs | Split into 7 partial files |
| LetMeHatch | LMH-R2-1 | ItemManager.cs | Move #region Eggs |
| LetMeHatch | LMH-R2-2 | ItemManager.cs | Move #region Items |
| RoosterControl | RC-R2-1 | RoosterControl.cs | Extract 5 debug records |
| RoosterControl | RC-R2-2 | RoosterHelper.cs | Split debug methods |

## Scope
- Subdir: `LetMeHatch`
- Dependency: NestCore Phase 1 (host helper). Dynamic editor work depends on NestCore Phase 5.

## Phase 0 — Keep docs in sync
1. Update `docs/PLAN.md`, `docs/CHECKLIST.md`, `docs/TODO.md` as phases/steps complete.
2. Keep all debug/diagnostics options on GMCM page id `debug` only.
3. Keep option IDs stable forever (translation keys and field IDs depend on this).

## Phase 3 — Baseline GMCM menu via NestCore host + DSL
1. Remove/retire legacy GMCM UI plumbing in favor of NestCore host + DSL.
2. Wire `GmcmConfigHost` in `GameLaunched`; if GMCM missing, return.
3. Build pages: `root`, `cloning`, `colors`, `breeding`, `advanced`, `debug`.
4. Bind scalar options with stable `optionId`s; put all logging/show-debug toggles on `debug`.
5. Add i18n keys under `config.*`; use `onSaved` only if cache refresh is needed.

## Phase 7A — Dynamic per-pack/per-animal toggles (low risk)
1. Add page `edits` populated from LMH’s known sources (packs/animal keys).
2. Render one toggle per entry with custom label funcs (friendly name fallback to key).
3. Use default-vs-override dictionary storage (remove key when equals default).
4. Rebuild on `SaveLoaded` so dynamic sources are available.
5. Keep option IDs stable using NestCore dynamic ID helper.

## Phase 7B — Egg color controls (medium risk)
1. Keep scalar toggles on `colors` page (`ChangeEggColors`, `LinkAnimalColorToEgg`).
2. Add page `eggPalette` to control which color families are allowed when LMH randomizes colors per **egg item** (dictionary `EggColors[eggId][colorFamily]`).
3. Add page `eggColorOverrides` to optionally force a fixed color per **egg item** (dictionary `EggColors_ColorObject[eggId]`), using GMCM Options color picker when available (falls back to native GMCM).
4. Keep UI bounded: enumerate egg items from `Data/FarmAnimals` + `Data/Objects` and fall back to config keys if those assets aren’t available.
5. Use stable element IDs derived from eggId + color key via `GmcmDynamicIds`.

## Phase 7C — ItemToClone editor (highest risk; bounded UI)
1. Add page `itemMappings` limited to curated/known animal relation order.
2. Use dropdowns for known choices + a “Custom” fallback text entry.
3. Keep allowed-values lists curated (avoid huge item catalogs).
4. Validate and normalize mapping data before saving.
5. Preserve config-file editing as an escape hatch.

## Phase 6 — Dynamic per-clone texture loader (flagged)
1. Add an asset loader for `Mods/<UniqueId>/Objects/Clones/<cloneId>` returning a generated 16×16 `Texture2D`.
2. Tint using `Core.ClonePipeline.ColorResolver` from `color_*` context tags (fallback deterministic).
3. Cache generated textures in-memory.
4. Register the loader in `Infrastructure/Content/LetMeHatchContentPipeline.cs`.
5. Build must remain green.
