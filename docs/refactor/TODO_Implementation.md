# Refactoring Implementation TODO

## Phase E1 — GMCM extraction into Infrastructure/Integrations/Gmcm (6 steps)

### Step E1.1 — Create GMCM context + orchestrator (empty shell)
- [x] Create `EggsInWild/Infrastructure/Integrations/Gmcm/EiwGmcmContext.cs`
  - Holds explicit dependencies: IModHelper, IMonitor, IManifest, config access, bulk state
- [x] Create `EggsInWild/Infrastructure/Integrations/Gmcm/EiwGmcmMenu.cs`
  - Add `public void RebuildLayout(IGenericModConfigMenuApi api, GmcmMenuLayout<ModConfig> layout, EiwGmcmContext ctx, bool force)`

### Step E1.2 — Move Bulk staged state storage into GMCM context
- [x] Move `_extraModAnimalKeysBulkByWildAnimalKey` from ModEntry to EiwGmcmContext
- [x] Add `public Dictionary<string, ExtraModAnimalKeysBulkState> ExtraModAnimalKeysBulkByWildAnimalKey` to EiwGmcmContext

### Step E1.3 — Move the current GMCM entry point to call EiwGmcmMenu
- [x] Modify `ModEntry.Gmcm.cs` - Replace body of `RebuildGmcmLayout(bool force)` to create ctx and call EiwGmcmMenu
- [x] Update ModEntry to expose needed dependencies through EiwGmcmContext

### Step E1.4 — Create WildAnimals pages file and move WildAnimals section ONLY
- [x] Create `EggsInWild/Infrastructure/Integrations/Gmcm/Pages/WildAnimals/WildAnimalsPages.cs`
- [x] Move the WildAnimals block from `ModEntry.Gmcm.WildAnimals.cs` to the new file
- [x] Expose: `public static void DefineGmcmWildAnimalsPages(GmcmMenuLayout<ModConfig> layout, EiwGmcmContext ctx, I18TranslationHelper i18n, ...)`

### Step E1.5 — Split WildAnimals into index vs detail
- [x] Create `EggsInWild/Infrastructure/Integrations/Gmcm/Pages/WildAnimals/WildAnimalIndexPages.cs`
  - Contains index page methods: `DefineWildAnimalsIndexPage()` and `DefineWildAnimalBucketPages()`
- [x] Create `EggsInWild/Infrastructure/Integrations/Gmcm/Pages/WildAnimals/WildAnimalDetailPages.cs`
  - Contains per-key detail page methods: `DefineWildAnimalDetailPages()`
- [x] Update `WildAnimalsPages.cs` to call the new methods
- [x] Update `EiwGmcmMenu.cs` to reference new methods

### Step E1.6 — Move debug GMCM pages into Debug folder
- [x] Create `EggsInWild/Infrastructure/Integrations/Gmcm/Pages/Debug/DebugPages.cs`
- [x] Move debug pages from `ModEntry.Gmcm.Debug.cs` to the new location
- [x] Update `EiwGmcmMenu.cs` to call DebugPages

---

## Phase E2 — Extract ExtraModAnimalKeys bulk logic into Application (future)
- Create Application/Bulk types + services
- Switch GMCM detail pages to use Application bulk service
- Delete old bulk helpers from ModEntry partial

---

## Implementation Notes

### Dependencies needed in EiwGmcmContext:
- `IModHelper Helper`
- `IMonitor Monitor`
- `IManifest Manifest`  
- `Func<ModConfig> GetConfig`
- `Action SaveConfig`
- `Dictionary<string, ExtraModAnimalKeysBulkState> ExtraModAnimalKeysBulkByWildAnimalKey`

### Current field to move:
- `private readonly Dictionary<string, ExtraModAnimalKeysBulkState> _extraModAnimalKeysBulkByWildAnimalKey` from ModEntry.cs

### New File Structure:

```
EggsInWild/Infrastructure/Integrations/Gmcm/
├── EiwGmcmContext.cs           ✅ (already existed)
├── EiwGmcmMenu.cs             ✅ (already existed)
└── Pages/
    ├── Debug/
    │   └── DebugPages.cs      ✅ (newly created)
    └── WildAnimals/
        ├── WildAnimalsPages.cs     ✅ (orchestrator - refactored)
        ├── WildAnimalIndexPages.cs ✅ (newly created)
        └── WildAnimalDetailPages.cs ✅ (newly created)
```

