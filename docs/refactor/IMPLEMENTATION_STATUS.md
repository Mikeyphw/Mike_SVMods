# Refactoring Implementation Status

## Phase E1 — GMCM extraction into Infrastructure/Integrations/Gmcm

### ✅ COMPLETED Steps:

#### Step E1.1 — Create GMCM context + orchestrator (empty shell)
- [x] `EggsInWild/Infrastructure/Integrations/Gmcm/EiwGmcmContext.cs` - Created
- [x] `EggsInWild/Infrastructure/Integrations/Gmcm/EiwGmcmMenu.cs` - Created

#### Step E1.2 — Move Bulk staged state storage into GMCM context
- [x] `ExtraModAnimalKeysBulkState` moved to EiwGmcmContext
- [x] `ExtraModAnimalKeysBulkByWildAnimalKey` dictionary in EiwGmcmContext

#### Step E1.3 — Move the current GMCM entry point to call EiwGmcmMenu
- [x] ModEntry.Gmcm.cs calls EiwGmcmMenu.RebuildLayout()

#### Step E1.4 — Create WildAnimals pages file
- [x] `EggsInWild/Infrastructure/Integrations/Gmcm/Pages/WildAnimals/WildAnimalsPages.cs` - Created

---

### 🔲 REMAINING Steps:

#### Step E1.5 — Split WildAnimals into index vs detail
- [ ] Create `WildAnimalIndexPages.cs` - Contains index/bucket page methods:
  - `DefineWildAnimalsIndexPage()` - Main index with summary
  - `DefineWildAnimalBucketPages()` - A-F, G-L, M-R, S-Z, Other buckets
- [ ] Create `WildAnimalDetailPages.cs` - Contains per-key detail page:
  - `DefineWildAnimalDetailPages()` - The foreach loop for each wild animal
- [ ] Update `WildAnimalsPages.cs` to call the new methods
- [ ] Update `EiwGmcmMenu.cs` to reference new methods

#### Step E1.6 — Move debug GMCM pages into Debug folder
- [ ] Create `EggsInWild/Infrastructure/Integrations/Gmcm/Pages/Debug/DebugPages.cs`
- [ ] Move debug pages from `ModEntry.Gmcm.Debug.cs`
- [ ] Update `EiwGmcmMenu.cs` to call DebugPages

---

## Phase E2 — Extract ExtraModAnimalKeys bulk logic into Application (future)

---

## File Structure After Completion:

```
EggsInWild/Infrastructure/Integrations/Gmcm/
├── EiwGmcmContext.cs           ✅ Exists
├── EiwGmcmMenu.cs              ✅ Exists
└── Pages/
    ├── Debug/
    │   └── DebugPages.cs       🔲 To create
    └── WildAnimals/
        ├── WildAnimalsPages.cs     ✅ Exists (orchestrator)
        ├── WildAnimalIndexPages.cs 🔲 To create
        └── WildAnimalDetailPages.cs 🔲 To create
```

