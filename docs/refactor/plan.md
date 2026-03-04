And your constraint: **new GMCM builder files must live under**
`EggsInWild/Infrastructure/Integrations/Gmcm/...`

---

# 0) Current EggsInWild/ModEntry.cs map (anchor index)

Use these **method signatures** as the *cut points* (line ranges from your current file):

### Composition root / events

* `Entry(IModHelper helper)` **L111–129**
* `GetApi()` **L131–139**
* `OnGameLaunched(...)` **L141–158**
* `OnDayStarted(...)` **L160–164**
* `OnSaveLoaded(...)` **L166–169**
* `OnGmcmSaved()` **L171–182**
* `OnConfigUpdated(...)` **L2281–2285**
* `SyncApiBirthTextMode()` **L2287–2293**

### Config normalization

* `IsEmptyOverride(...)` **L184–191**
* `NormalizeLocationTypesOverrides(...)` **L193–257**

### GMCM monolith (the big one)

* `RebuildGmcmLayout(bool force)` **L258–2159**

  * WildAnimals GMCM section begins at the comment header:

    * `// Wild Animals (GMCM editable)` **~L332**
  * Debug pages are under:

    * `#if WEGG_DEV` **~L1877**

### GMCM helpers used by layout

* `BuildAnimalProgressionPage(...)` **L2161–2219**
* `BuildAnimalOptionName(...)` **L2221–2228**
* `BuildAnimalOptionTooltip(...)` **L2230–2242**
* `Translate(...)` **L2244–2254**
* `BuildWildAnimalPageId(...)` **L2257–2258**
* `GetWildAnimalEffectiveEnabled(...)` **L2260–2268**
* `GetWildAnimalEnabledSource(...)` **L2270–2278**

### DesertTomb / Harmony

* `TryInstallDesertTombHarmonyPatches(...)` **L2296–2353**
* `EnsureDesertTombRuntime()` **L2355–2392**
* `LogDesertTombHarmonyPatchStatus()` **L2394–2408**
* `LogPatchInfo(...)` **L2410–2434**
* `InstallHarmonyBootstrap()` **L2436–2442**

### Content pipeline

* `InitializeModAssetHandler()` **L2444–2484**
* `InitializeContentPipeline()` **L2486–2527**
* `CreateContentContext()` **L2529–2543**
* `GetContentProgressionStep()` **L2545–2554**
* `InvalidateGameAssets()` **L2556–2565**

### Commands bootstrap (currently a dependency factory)

* `RegisterConsoleCommands(IModHelper helper)` **L2567–2678**

### ExtraModAnimalKeys bulk logic block (pure-ish helpers, currently inside ModEntry)

* `private sealed class ExtraModAnimalKeysBulkState` **L2681–2687**
* record structs:

  * `ExtraModAnimalKeysBulkPlanCounts` **L2690**
  * `FarmAnimalKeyFixStats` **L2692**
* helper functions:

  * `CanonicalizeModId(...)` **L2694–2703**
  * `ApplyFarmAnimalKeyFixes(...)` **L2705–2768**
  * `ComputeExtraModAnimalKeysBulkPlanCounts(...)` **L2770–2847**
  * `BuildExtraModAnimalKeysEffectiveExportBodyLines(...)` **L2849–2881**
  * `ParseExtraModAnimalKeysBulk(...)` **L2884–3025**
  * `BuildExtraModAnimalKeysBulkFixPreviewLines(...)` **L3027–3157**
  * `ParseCsvKeyList(...)` **L3159–3178**
  * `SequenceEqualIgnoreCase(...)` **L3180–3189**

### Guardrail

* `EnsureNoLocalNestCoreDuplicates(...)` **L3191–3196**

---

# 1) PHASE E0 — “Make it splittable” (compile includes + minimal surgery) (3 steps)

## Step E0.1 — Add compile globs so new files always compile

**Diff**

* `M EggsInWild/EggsInWild.csproj`

  * If `<EnableDefaultCompileItems>false</EnableDefaultCompileItems>` exists:

    * `+ <Compile Include="ModEntry.*.cs" />`
    * `+ <Compile Include="Infrastructure\Integrations\Gmcm\**\*.cs" />`
    * `+ <Compile Include="Application\**\*.cs" />`

**Acceptance**

* Create a dummy file `EggsInWild/Infrastructure/Integrations/Gmcm/_CompileProbe.cs` and build succeeds without csproj changes.

---

## Step E0.2 — Make ModEntry a composition root via partials (no behavior change)

> This step reduces `ModEntry.cs` size immediately, before deeper extraction.

**Diff**

* `M EggsInWild/ModEntry.cs`

  * Keep:

    * fields
    * `Entry`, `GetApi`
    * (optionally) `EnsureNoLocalNestCoreDuplicates`
* `A EggsInWild/ModEntry.Events.cs`

  * MOVE methods:

    * `OnGameLaunched` **L141–158**
    * `OnDayStarted` **L160–164**
    * `OnSaveLoaded` **L166–169**
    * `OnGmcmSaved` **L171–182**
    * `OnConfigUpdated` **L2281–2285**
    * `SyncApiBirthTextMode` **L2287–2293**
* `A EggsInWild/ModEntry.Config.cs`

  * MOVE methods:

    * `IsEmptyOverride` **L184–191**
    * `NormalizeLocationTypesOverrides` **L193–257**

**Acceptance**

* Build passes.
* No functional changes.

---

## Step E0.3 — Split the big blocks into file-level partials (still no behavior change)

**Diff**

* `A EggsInWild/ModEntry.Gmcm.cs`

  * MOVE:

    * `RebuildGmcmLayout` **L258–2159**
    * GMCM helpers:

      * `BuildAnimalProgressionPage` **L2161–2219**
      * `BuildAnimalOptionName` **L2221–2228**
      * `BuildAnimalOptionTooltip` **L2230–2242**
      * `Translate` **L2244–2254**
      * `BuildWildAnimalPageId` **L2257–2258**
      * `GetWildAnimalEffectiveEnabled` **L2260–2268**
      * `GetWildAnimalEnabledSource` **L2270–2278**
* `A EggsInWild/ModEntry.DesertTomb.cs`

  * MOVE DesertTomb methods:

    * **L2296–2442** set above
* `A EggsInWild/ModEntry.Content.cs`

  * MOVE content methods:

    * **L2444–2565**
* `A EggsInWild/ModEntry.Commands.cs`

  * MOVE:

    * `RegisterConsoleCommands` **L2567–2678**
* `A EggsInWild/ModEntry.ExtraModAnimalKeysBulk.cs`

  * MOVE:

    * `ExtraModAnimalKeysBulkState` **L2681–2687**
    * `ExtraModAnimalKeysBulkPlanCounts` **L2690**
    * `FarmAnimalKeyFixStats` **L2692**
    * helper methods **L2694–3189**

**Acceptance**

* Build passes.
* `EggsInWild/ModEntry.cs` drops from ~3197 lines to “wiring + fields”.

---

# 2) PHASE E1 — GMCM extraction into Infrastructure/Integrations/Gmcm (6 steps)

> End state: ModEntry no longer contains GMCM layout code.

## Step E1.1 — Create GMCM context + orchestrator (empty shell)

**Diff**

* `A EggsInWild/Infrastructure/Integrations/Gmcm/EiwGmcmContext.cs`

  * Holds explicit dependencies currently accessed from ModEntry GMCM:

    * `IModHelper Helper`
    * `IMonitor Monitor` (or NestCore `ILog`)
    * `IManifest Manifest`
    * config access: `Func<ModConfig> GetConfig`, `Action SaveConfig` (or provider)
    * `Dictionary<string, /*bulk state*/>` (moved off ModEntry)
* `A EggsInWild/Infrastructure/Integrations/Gmcm/EiwGmcmMenu.cs`

  * `public void RebuildLayout(IGenericModConfigMenuApi api, EiwGmcmContext ctx, bool force)`

**Acceptance**

* Build passes, even if unused.

---

## Step E1.2 — Move Bulk staged state storage into GMCM context (not ModEntry field)

**Diff**

* `M EggsInWild/ModEntry.cs`

  * REMOVE field:

    * `_extraModAnimalKeysBulkByWildAnimalKey` (**currently at L54**)
* `M EggsInWild/Infrastructure/Integrations/Gmcm/EiwGmcmContext.cs`

  * ADD:

    * `public Dictionary<string, ExtraModAnimalKeysBulkState> ExtraModAnimalKeysBulkByWildAnimalKey { get; }`

**Acceptance**

* Build passes after adjusting references (next step will rewire).

---

## Step E1.3 — Move the current GMCM entry point to call EiwGmcmMenu

**Diff**

* `M EggsInWild/ModEntry.Gmcm.cs`

  * Replace body of `RebuildGmcmLayout(bool force)` with:

    * get api + create ctx + call `EiwGmcmMenu.RebuildLayout(...)`
* `M EggsInWild/Infrastructure/Integrations/Gmcm/EiwGmcmMenu.cs`

  * TEMP: paste the full GMCM layout body (verbatim) from `RebuildGmcmLayout` to compile.
  * Replace direct field access with `ctx.*`:

    * `_config!.Get()` → `ctx.GetConfig()`
    * `_config.Save()` → `ctx.SaveConfig()`
    * `Helper` → `ctx.Helper`
    * `Monitor` → `ctx.Monitor`
    * `_extraModAnimalKeysBulkByWildAnimalKey` → `ctx.ExtraModAnimalKeysBulkByWildAnimalKey`

**Acceptance**

* GMCM builds.
* Behavior unchanged.

---

## Step E1.4 — Create WildAnimals pages file and move WildAnimals section ONLY (exact cut anchors)

**Exact cut anchors inside the GMCM body**

* START anchor:

  * `// ============================`
  * `// Wild Animals (GMCM editable)`
* END anchor:

  * the line just before `#if WEGG_DEV` (Debug pages header)

**Diff**

* `A EggsInWild/Infrastructure/Integrations/Gmcm/Pages/WildAnimals/WildAnimalsPages.cs`

  * MOVE the entire WildAnimals block (approx **~L332–1876** from old monolith)
  * Expose: `public static void Define(IGenericModConfigMenuApi api, LayoutBuilder layout, EiwGmcmContext ctx, I18TranslationHelper i18n, ...)`
* `M EggsInWild/Infrastructure/Integrations/Gmcm/EiwGmcmMenu.cs`

  * Replace the removed block with a call:

    * `WildAnimalsPages.Define(...);`

**Acceptance**

* Build passes.

---

## Step E1.5 — Split WildAnimals into index vs detail (your request)

This is the *exact split point* in your current WildAnimals code:

* Index pages setup lives around:

  * local fn `BuildWildAnimalIndexPage(...)` and calls:

    * `BuildWildAnimalIndexPage("wildAnimals_all", ...)`
    * `BuildWildAnimalIndexPage("wildAnimals_index_af", ...)`
    * etc.
* Detail pages begin at:

  * `foreach (var key in wildAnimalKeysForGmcm)` (the second one)

**Diff**

* `M EggsInWild/Infrastructure/Integrations/Gmcm/Pages/WildAnimals/WildAnimalsPages.cs`

  * Replace:

    * `DefineGmcmWildAnimalsPages(...)`
  * With two methods **in the same file**:

    * `DefineGmcmWildAnimalsIndexPages(...)`

      * MOVE local helpers currently inside GMCM:

        * `CountWildAnimalKeysBucket(...)` (local)
        * `CountWildAnimalKeysOther(...)` (local)
        * `WildAnimalIndexBucket(...)` (local)
        * `BuildWildAnimalIndexPage(...)` (local)
    * `DefineGmcmWildAnimalDetailPages(...)`

      * MOVE the per-key `layout.Page(pageId: BuildWildAnimalPageId(key), ...)` loop

**Acceptance**

* Build passes.
* No UI changes.

---

## Step E1.6 — Move debug GMCM pages into Debug folder (WEGG_DEV)

**Diff**

* `A EggsInWild/Infrastructure/Integrations/Gmcm/Pages/Debug/DebugPages.cs`

  * MOVE everything under `#if WEGG_DEV` “Debug subpages” header
* `M EiwGmcmMenu.cs`

  * `#if WEGG_DEV DebugPages.Define(...); #endif`

**Acceptance**

* Build passes with and without `WEGG_DEV`.

---

# 3) PHASE E2 — Extract ExtraModAnimalKeys bulk logic into Application (7 steps, very mechanical)

> This is the “make GMCM + dev commands share one implementation” phase.

## Step E2.1 — Create Application/Bulk types + services (copy first, then delete old)

**Diff**

* `A EggsInWild/Application/ExtraModAnimalKeys/Bulk/BulkTypes.cs`
* `A EggsInWild/Application/ExtraModAnimalKeys/Bulk/BulkInputParser.cs`
* `A EggsInWild/Application/ExtraModAnimalKeys/Bulk/BulkPlanner.cs`
* `A EggsInWild/Application/ExtraModAnimalKeys/Bulk/BulkExporter.cs`
* `A EggsInWild/Application/ExtraModAnimalKeys/Bulk/BulkWarnings.cs`

**Move mapping (source → dest)**
From `ModEntry.ExtraModAnimalKeysBulk.cs` (currently moved from tail of ModEntry):

* `ExtraModAnimalKeysBulkState` → becomes `BulkParseResult`-like DTO
* `ExtraModAnimalKeysBulkPlanCounts` → `BulkPlanCounts`
* `FarmAnimalKeyFixStats` → `KeyFixStats`
* `CanonicalizeModId` → `BulkInputParser.CanonicalizeModId`
* `ApplyFarmAnimalKeyFixes` → `BulkInputParser.ApplyKeyFixes`
* `ParseCsvKeyList` → `BulkInputParser.ParseCsvKeyList`
* `ParseExtraModAnimalKeysBulk` → `BulkInputParser.Parse(...)`
* `ComputeExtraModAnimalKeysBulkPlanCounts` → `BulkPlanner.ComputePlan(...)`
* `BuildExtraModAnimalKeysEffectiveExportBodyLines` → `BulkExporter.BuildBodyLines(...)`
* `BuildExtraModAnimalKeysBulkFixPreviewLines` → `BulkWarnings.BuildFixPreviewLines(...)`
* `SequenceEqualIgnoreCase` → `BulkTypes.SequenceEqualIgnoreCase` or helper

**Acceptance**

* Build passes.

## Step E2.2 — Switch GMCM detail pages to use Application bulk service

**Diff**

* `M .../WildAnimalDetailPages.cs`

  * Replace inline calls to:

    * `ParseExtraModAnimalKeysBulk`
    * `ComputeExtraModAnimalKeysBulkPlanCounts`
    * `BuildExtraModAnimalKeysEffectiveExportBodyLines`
    * `BuildExtraModAnimalKeysBulkFixPreviewLines`
  * With calls into Application classes.

**Acceptance**

* Build passes; behavior same.

## Step E2.3 — Delete old bulk helpers from ModEntry partial

**Diff**

* `D EggsInWild/ModEntry.ExtraModAnimalKeysBulk.cs` (or remove contents)
* Remove any remaining nested types from ModEntry.

**Acceptance**

* Build passes.

*(Steps E2.4–E2.7 would be tests + dev command parity, but you didn’t ask for that detail right now; I can expand similarly when you want.)*

---

# 4) LetMeHatch exact-moves list (tailored to current code)

Your current `LetMeHatch/ModEntry.cs` is short, but it’s “globally static” heavy.

## Phase L0 — Reduce static surface while keeping behavior (4 steps)

### Step L0.1 — Introduce runtime container (new file)

**Diff**

* `A LetMeHatch/Application/Runtime/LmhRuntime.cs`

  * Contains:

    * config, helper, monitor, manifest, i18n, logger, asset handler, invalidation batcher
  * Owns `DynamicCloneObjectTextureLoader.ClearCache()` side effects.

### Step L0.2 — Replace global static storage with runtime instance

**Move mapping**
From `ModEntry.cs`:

* Static fields **L23–35** → move into `LmhRuntime` instance fields
* `InitializeStaticFields(...)` **L69–83** → becomes `LmhRuntime.Create(...)` or `ApplyConfig(...)`

### Step L0.3 — Coordinator binds events (already mostly true)

**Diff**

* `M LetMeHatch/ModEntry.cs`

  * `BindEvents(...)` remains but uses `_runtime.Coordinator` or `runtime.Coordinator`

### Step L0.4 — Optional: keep static accessors but make them wrappers (compat bridge)

**Diff**

* Keep `public static` properties but implement as:

  * `=> _runtime!.Config` etc
* Mark internal-only if possible

**Acceptance**

* Build passes and mod works, but global state is centralized.

---

# 5) What you get after these moves

* `EggsInWild/ModEntry.cs` becomes small and readable.
* GMCM lives under **Infrastructure/Integrations/Gmcm** with:

  * WildAnimals split into **Index** + **Detail** (your request)
* Bulk logic becomes a reusable Application service (next phase).
* LetMeHatch reduces static sprawl.

---

