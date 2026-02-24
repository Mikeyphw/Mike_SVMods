# LetMeHatch Strict Parity Audit

## Scope
- Refactored: `/home/mike/Documents/Projects/Mike_SVMods/LetMeHatch`
- Original: `/home/mike/Projects/LetMeHatch_orig`
- Audit mode: strict, feature-by-feature (behavioral parity + operability)
- Date: 2026-02-23

## Verdict
- **Refactor is not complete yet** under strict parity criteria.
- Main blockers:
  1. Refactor currently does **not compile** (`39` syntax errors in current tree).
  2. GMCM parity is **partial**: old per-content-pack female/male breeding tuning pages are not present in the new menu flow.

## Build status (refactor)
- Command:
  - `dotnet build /home/mike/Documents/Projects/Mike_SVMods/LetMeHatch/LetMeHatch.csproj -v minimal -p:EnableModDeploy=false -p:EnableModZip=false`
- Result:
  - `Build FAILED`
  - `39 Error(s)`
- Highest-impact broken files:
  - `Infrastructure/Commands/LmhConsoleCommands.cs`
  - `Integrations/Gmcm/LmhGmcmMenu.cs`
  - `Managers/GameAssetManagement/BirthTextEditor.cs`
  - `Managers/TextureManager.cs`
  - `Infrastructure/Smapi/DependencyGuards.cs`

## Feature Matrix
| Feature Area | Original | Refactor | Status | Notes |
|---|---|---|---|---|
| Manifest identity + dependencies | `manifest.json` | `manifest.json` | Complete | Name/UniqueID/EntryDll/dependencies match. |
| Entry lifecycle/events | `ModEntry.cs` day start/end, update tick, game launched, warped | `ModEntry.cs` + `Application/Coordination/LmhCoordinator.cs` same event surfaces | Complete (architecture changed) | Behavior routing moved to coordinator; lifecycle coverage remains. |
| Public SMAPI API object | `LetMeHatchAPI.cs` | `LetMeHatchAPI.cs` | Complete+ | Preserves core properties/methods; now explicitly implements NestCore `ILetMeHatchApi`. |
| WildEggs integration contract | `Framework/IWildEggsAPI.cs` local interface | NestCore shared `IWildEggsApi` usage in coordinator/manager | Complete (migrated) | Local interface file removed intentionally; integration moved to shared contract. |
| Rooster Control integration | `Framework/IRoosterHatchingAPI.cs`, integrations + data pack | same `IRoosterHatchingAPI`, integration files, same data pack path/content | Complete | Functional surface retained. |
| Content Patcher integration | `ModEntry.cs` gets `IContentPatcherAPI` | `LmhCoordinator.OnGameLaunched()` gets `IContentPatcherAPI` | Complete | Same dependency + fail-fast checks. |
| Asset request coverage | `ModEntry.HandleAssetSpecificLogic()` for FarmAnimals/Objects/Machines/male clone textures/game paths | `Infrastructure/Content/LetMeHatchContentPipeline.cs` loaders+editors for same domains | Complete (recomposed) | Moved from monolith to pipeline handlers. |
| FarmAnimals edits stack | egg list, birth text, clones, placeholders, original item replacement | matching editors + application adapters/services | Complete | Same edit categories exist with ordered editor chain. |
| Objects edits | cloned eggs + fertilized egg objects | `ObjectsClonedEggsEditor` + `ObjectsFertilizedEggsEditor` | Complete | Coverage retained. |
| Machines edit | machine rule cloning | `MachinesRulesEditor` + `MachinesAssetEditsService` | Complete | Same functional intent retained. |
| Breeding engine + loader | `Breeding/BreedingSystem.cs`, `Breeding/BreedingDataLoader.cs` | same files/flow retained | Complete | Wild egg filtering and integration loading preserved. |
| Day reset/early init behavior | `HandleEarlyActions`, day reset, cache invalidation | `LmhCoordinator.OnUpdateTicked()`, `OnDayStarted()`, `OnDayEnding()` | Complete | Equivalent flow via coordinator. |
| Console commands | `lmh_reloadcp`, `lmh_checki18n` | same two + `lmh_content_handlers`, `lmh_invalidate_all` | Expanded | Backward commands preserved, extra diagnostics added. |
| Config option parity (`ModConfig`) | legacy options set + defaults | same set + `DeterministicBreedingRng`, `StabilizeFarmAnimalsAssetOrder` | Complete+ | No original option removed; two new options added. |
| GMCM parity (top-level toggles/pages) | `UI/GMCMenu.cs` legacy custom menu | `Integrations/Gmcm/LmhGmcmMenu.cs` NestCore builder pages | Partial | Core toggles/pages exist; architecture changed intentionally. |
| GMCM parity (per-pack/per-female breeding tuning UI) | Old `GMCMenu` had per-content-pack female pages and male config controls | New menu currently exposes global breeding controls and per-animal edit toggles, but not old detailed per-female/per-male page flow | Missing | Strict parity gap vs old interactive tuning surface. |
| Localization key parity | old default i18n had 100 keys | refactor default i18n has 150 keys | Partial | 7 old keys missing (`config.pagelink.*` set), many new keys introduced. |
| Data assets parity | `data/**`, `assets/**` | same paths/files present | Complete | File-set parity for data/assets is intact. |
| Compile/runtime operability | original builds as shipped baseline | refactor currently fails compile | Missing | This alone blocks “refactor done” status in strict mode. |

## Concrete parity deltas
1. Original-only source files (not present in refactor):
- `Framework/IWildEggsAPI.cs` (migrated to NestCore shared interface)
- `UI/GMCMenu.cs` (replaced by NestCore GMCM builder)

2. I18n keys present in original but missing in refactor default locale:
- `config.pagelink.BirthText.name`
- `config.pagelink.CloneEggs.name`
- `config.pagelink.EggColors.name`
- `config.pagelink.EggList.name`
- `config.pagelink.EggsEnabled.name`
- `config.pagelink.ManageChosenColors.name`
- `config.pagelink.eggRelations.name`

3. New refactor-only console commands:
- `lmh_content_handlers`
- `lmh_invalidate_all`

## Strict completion criteria (remaining)
1. Restore compile health to `0` errors.
2. Decide GMCM parity target explicitly:
 
- Option B: declare old detailed breeding pages intentionally retired and document as accepted parity deviation.
3. Resolve i18n key policy:
- Either reintroduce old `config.pagelink.*` aliases or mark as intentional non-backward-compat changes in docs.

## Bottom line
- Feature port is substantial and mostly present.
- Under strict parity, status is **Partial / Not complete** until compile is fixed and GMCM parity decision is closed.
