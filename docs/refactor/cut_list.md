Here’s the **exact internal section map** for the **WildAnimal detail page** loop in your current `EggsInWild/ModEntry.cs`, with **surgical anchors** (translation keys + elementIds + nearby statements) and **suggested helper method names** an AI agent can extract *verbatim*.

This is scoped to the block:

* `foreach (var key in wildAnimalKeysForGmcm) { layout.Page(pageId: BuildWildAnimalPageId(key), ... build: page => { ... }) }`
* In your snapshot, that page-build body spans roughly **L554–1873**, but the plan below uses **anchor strings** so line numbers don’t matter.

---

# WildAnimal Detail Page: Exact Section Map (Anchors → Helper Methods)

## Target file + main entry (what it becomes)

**Diff intent**

* `A EggsInWild/Infrastructure/Integrations/Gmcm/Pages/WildAnimals/WildAnimalDetailPages.cs`
* `M .../WildAnimalsPages.cs` (or wherever detail pages currently live)

  * call into `WildAnimalDetailPages.DefineGmcmWildAnimalDetailPages(...)`

**Proposed top-level method structure (call order matches current UI order):**

1. `AddWildAnimalStatusParagraph(...)`
2. `AddWildAnimalScalarsParagraph(...)`
3. `AddWildAnimalOverridesSummaryParagraph(...)`
4. `AddScalarOverrideControls(...)`
5. `AddLocationTypesSection(...)`
6. `AddExtraModAnimalKeysSectionIntroAndSets(...)`
7. `AddExtraModAnimalKeysCustomBlockActions(...)`
8. `AddExtraModAnimalKeysUtilities(...)`
9. `AddBulkStageSection(...)`
10. `AddBulkApplySection(...)`
11. `AddValidationWarningsSection(...)`
12. `AddPerModGroupEditorsSection(...)`
13. `AddDiagnosticsSection(...)`
14. `AddResetOverrideAction(...)`

---

## 0) Setup locals (keep near top of `build: page => {}`)

### 0.1 Defaults + Mod availability

**Anchors**

* `var defaultEnabled = definition?.Enabled ?? true;`
* `var defaultLargeEgg = definition?.LargeEgg ?? false;`
* `var defaultPrice = definition?.Price ?? 0;`
* `var defaultIsMagic = definition?.IsMagic ?? false;`
* `var modAvailability = new EggsInWild.Infrastructure.Mods.SmapiModAvailability(Helper.ModRegistry);`

**Keep as-is** at top of build body.
**No helper method** yet (used by multiple later sections).

---

## 1) Status / summaries (3 paragraphs)

### 1.1 Effective enabled status paragraph

**Helper:** `AddWildAnimalStatusParagraph(page, i18n, key, defaultEnabled, /*ctx: config*/)`
**Start anchor**

* `page.ParagraphDynamic(` with translation key:

  * `"config.paragraph.wildAnimals.status.text"`
    **End anchor**
* `id: $"wildAnimals_status_{...Normalize(key)}");`

**Internal logic contains anchors**

* `var source = GetWildAnimalEnabledSource(cfg, key, defaultEnabled);`
* `var effective = GetWildAnimalEffectiveEnabled(cfg, key, defaultEnabled);`

---

### 1.2 Effective scalars paragraph

**Helper:** `AddWildAnimalScalarsParagraph(page, i18n, key, defaultLargeEgg, defaultPrice, defaultIsMagic, GetOverride)`
**Start anchor**

* `page.ParagraphDynamic(` with translation key:

  * `"config.paragraph.wildAnimals.scalars.text"`
    **End anchor**
* `id: $"wildAnimals_scalars_{...Normalize(key)}");`

**Internal anchor strings**

* `"Effective scalars: LargeEgg=...; Price=...; IsMagic=..."`
* `largeEggSource`, `priceSource`, `isMagicSource`

---

### 1.3 Overrides summary paragraph

**Helper:** `AddWildAnimalOverridesSummaryParagraph(page, i18n, key, GetOverride)`
**Start anchor**

* `page.ParagraphDynamic(` with translation key:

  * `"config.paragraph.wildAnimals.overrides.text"`
    **End anchor**
* `id: $"wildAnimals_overrides_{...Normalize(key)}");`

**Internal anchor strings**

* `var unset = Translate(i18n, "config.wildAnimals.value.unset", "(unset)");`
* `Overrides: Enabled=...; LargeEgg=...; Price=...; IsMagic=...`

---

## 2) Scalar override controls (Enabled/LargeEgg/Price/IsMagic)

### 2.1 Separator before controls

**Helper:** inline (or `AddSeparator(page)`)
**Anchor**

* `page.Separator(simple: true);` immediately before first `page.BoolLabel(elementId: $"wildAnimals.{key}.enabled", ...)`

---

### 2.2 Enabled toggle

**Helper:** `AddEnabledOverrideControl(...)`
**Start anchor**

* `page.BoolLabel(` with:

  * `elementId: $"wildAnimals.{key}.enabled"`
  * name key: `"config.wildAnimals.enabled.name"`
    **End anchor**
* the closing `});` for this BoolLabel

---

### 2.3 LargeEgg toggle

**Helper:** `AddLargeEggOverrideControl(...)`
**Start anchor**

* `page.BoolLabel(` with:

  * `elementId: $"wildAnimals.{key}.largeEgg"`
  * name key: `"config.wildAnimals.largeEgg.name"`

---

### 2.4 Price IntLabel

**Helper:** `AddPriceOverrideControl(...)`
**Start anchor**

* `page.IntLabel(` with:

  * `elementId: $"wildAnimals.{key}.price"`
  * name key: `"config.wildAnimals.price.name"`
    **End anchor**
* includes min/max/interval anchors:

  * `min: 0,`
  * `max: 20000,`
  * `interval: 25);`

---

### 2.5 IsMagic toggle

**Helper:** `AddIsMagicOverrideControl(...)`
**Start anchor**

* `page.BoolLabel(` with:

  * `elementId: $"wildAnimals.{key}.isMagic"`
  * name key: `"config.wildAnimals.isMagic.name"`

---

## 3) LocationTypes section

### 3.1 Section header + description

**Helper:** `AddLocationTypesSectionHeader(...)`
**Start anchors**

* `page.Separator(simple: true);`
* `page.SectionLabel(() => Translate(i18n, "config.section.wildAnimals.locationTypes.label", "Location types"));`
* `page.ParagraphDynamic(` with:

  * `"config.paragraph.wildAnimals.locationTypes.description.text"`

---

### 3.2 Allowed values + per-locationType dropdown loop

**Helper:** `AddLocationTypesOverridesGrid(...)`
**Start anchors**

* `var allowedValues = new[] { "default", "allow", "deny" };`
* `foreach (var locationTypeKey in locationTypeKeysForGmcm)`
  **Inside-loop anchor**
* `page.TextLabel(` with:

  * `elementId: $"wildAnimals.{key}.locationTypes.{locationTypeKey}"`
  * tooltip key: `"config.wildAnimals.locationTypes.type.tooltip"`
    **End anchor**
* end of that `foreach (var locationTypeKey...)` loop

---

## 4) ExtraModAnimalKeys section (intro + set construction)

### 4.1 Section label + description

**Helper:** `AddExtraModAnimalKeysSectionHeader(...)`
**Start anchors**

* `page.Separator(simple: true);`
* `page.SectionLabel(() => Translate(i18n, "config.section.wildAnimals.extraModAnimalKeys.label", "Farm animal keys"));`
* `page.ParagraphDynamic(` with:

  * `"config.paragraph.wildAnimals.extraModAnimalKeys.description.text"`

---

### 4.2 Build `extraModIdsForGmcm` / `extraModIdsOrdered`

**Helper:** `BuildExtraModIdsSets(definition, cfgOverride) -> (HashSet extraModIdsForGmcm, List extraModIdsOrdered)`
**Start anchors**

* `var extraModIdsForGmcm = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Vanilla", };`
* `if (definition?.ExtraModAnimalKeys is not null) { foreach (...) extraModIdsForGmcm.Add(modId); }`
* `{ var cfg = _config!.Get(); var ov = GetOverride(cfg); if (ov?.ExtraModAnimalKeys is not null) ... }`
* `var extraModIdsOrdered = extraModIdsForGmcm.OrderBy(...).ToList();`

> ⚠️ **Build-break risk when extracted**: this is where missing locals often happen. Keep these as **explicit outputs** returned to callers.

---

### 4.3 Cached farm-animal-id loader

**Helper:** `GetKnownFarmAnimalIdsCached()` (or keep as local)
**Start anchors**

* `HashSet<string>? cachedFarmAnimalIds = null;`
* `var cachedFarmAnimalIdsLoaded = false;`
* `HashSet<string>? GetKnownFarmAnimalIds()`
  **End anchor**
* the end of that local function

---

## 5) ExtraModAnimalKeys “custom blocks” actions

### 5.1 Custom upsert action

**Helper:** `AddCustomModIdUpsertTextLabel(...)`
**Start anchor**

* `page.TextLabel(` with:

  * `elementId: $"wildAnimals.{key}.extraModAnimalKeys.customUpsert"`
  * name key: `"config.wildAnimals.extraModAnimalKeys.customUpsert.name"`
    **Notable internal anchors**
* `Format: modId | csvKeys`
* `ParseCsvKeyList(keysRaw)`
* `ov.ExtraModAnimalKeys[...] = new ExtraModAnimalKeysOverrideConfig { FarmAnimalKeys = ... }`

---

### 5.2 Custom remove action

**Helper:** `AddCustomModIdRemoveTextLabel(...)`
**Start anchor**

* `page.TextLabel(` with:

  * `elementId: $"wildAnimals.{key}.extraModAnimalKeys.customRemove"`
  * name key: `"config.wildAnimals.extraModAnimalKeys.customRemove.name"`

---

## 6) Utilities for staged bulk + export

### 6.1 Clear staged bulk input toggle

**Helper:** `AddClearStagedBulkToggle(...)`
**Start anchor**

* `page.Bool(` with:

  * name key: `"config.wildAnimals.extraModAnimalKeys.bulkClear.name"`
  * and the side-effect anchor:

    * `_extraModAnimalKeysBulkByWildAnimalKey.Remove(key);`

---

### 6.2 Export effective blocks to SMAPI log

**Helper:** `AddExportEffectiveToLogAction(...)`
**Start anchor**

* `page.BoolLabel(` with:

  * `elementId: $"wildAnimals.{key}.extraModAnimalKeys.exportEffective"`
  * name key: `"config.wildAnimals.extraModAnimalKeys.exportEffective.name"`
    **Internal anchors**
* `"[GMCM][ExtraModAnimalKeys] Export effective blocks for '{key}':"`
* `BuildExtraModAnimalKeysEffectiveExportBodyLines(...)`
* `Monitor.Log(... LogLevel.Info)`

---

### 6.3 Copy export into bulk stage (overwrite)

**Helper:** `AddCopyExportIntoBulkOverwriteAction(...)`
**Start anchor**

* `elementId: $"wildAnimals.{key}.extraModAnimalKeys.copyExportToBulkOverwrite"`
  **Internal anchor**
* `_extraModAnimalKeysBulkByWildAnimalKey[key] = ParseExtraModAnimalKeysBulk(raw, extraModIdsForGmcm, GetKnownFarmAnimalIds());`

---

### 6.4 Copy export into bulk stage (append)

**Helper:** `AddCopyExportIntoBulkAppendAction(...)`
**Start anchor**

* `elementId: $"wildAnimals.{key}.extraModAnimalKeys.copyExportToBulkAppend"`

---

### 6.5 Refresh note paragraph

**Helper:** `AddCustomBlocksRefreshNoteParagraph(...)`
**Start anchor**

* `page.ParagraphDynamic(` with:

  * `"config.wildAnimals.extraModAnimalKeys.customRefreshNote.text"`

---

## 7) Bulk edit (stage) section

### 7.1 Section label + bulk header paragraph

**Helper:** `AddBulkStageHeader(...)`
**Start anchors**

* `page.Separator(simple: true);`
* `page.SectionLabel(() => Translate(i18n, "config.section.wildAnimals.extraModAnimalKeys.bulk.label", "Bulk edit (stage)"));`
* `page.ParagraphDynamic(` with:

  * `"config.paragraph.wildAnimals.extraModAnimalKeys.bulkHeader.empty"`
  * and uses `ComputeExtraModAnimalKeysBulkPlanCounts(...)`

---

### 7.2 Bulk input text label

**Helper:** `AddBulkInputTextLabel(...)`
**Start anchor**

* `page.TextLabel(` with:

  * `elementId: $"wildAnimals.{key}.extraModAnimalKeys.bulkInput"`
  * name key: `"config.wildAnimals.extraModAnimalKeys.bulkInput.name"`
    **Internal anchor**
* `ParseExtraModAnimalKeysBulk(rawValue, extraModIdsForGmcm, GetKnownFarmAnimalIds());`

---

### 7.3 Bulk preview paragraph (largest chunk)

**Helper:** `AddBulkPreviewParagraph(...)`
**Start anchor**

* `page.ParagraphDynamic(` with:

  * `"config.paragraph.wildAnimals.extraModAnimalKeys.bulkPreview.header"`
    **Key internal anchors (sub-subsections inside preview)**
* `ComputeExtraModAnimalKeysBulkPlanCounts(...)`
* `FormatKeySample(...)` local
* lines list includes translation keys:

  * `"bulkPreview.summary"`
  * `"bulkPreview.plan"`
  * `"bulkPreview.fixes"`
  * `"bulkPreview.noop"`
* “Warnings:” header literal
* “Upserts:” and “Resets:” literal blocks

> This is the #1 candidate to break into *internal* helpers later:

* `BuildPreviewHeaderLines(...)`
* `BuildPreviewWarningsLines(...)`
* `BuildPreviewFixPreviewLines(...)`
* `BuildPreviewUpsertsLines(...)`
* `BuildPreviewResetsLines(...)`

…but keep it as one extraction first (move-then-refactor).

---

## 8) Bulk apply section + quick-fix toggles

### 8.1 Apply note paragraph

**Helper:** `AddBulkApplyNoteParagraph(...)`
**Start anchor**

* `page.ParagraphDynamic(` with:

  * `"config.paragraph.wildAnimals.extraModAnimalKeys.bulkApply.note"`

---

### 8.2 Skip-noop toggle (config flag)

**Helper:** `AddSkipNoopToggle(...)`
**Anchor**

* `page.Bool(cfg => cfg.GmcmExtraModAnimalKeysBulkApplySkipNoop, optionId: "wildAnimals.extraModAnimalKeys.bulkSkipNoop", tooltip: true);`

---

### 8.3 “Bulk apply quick-fixes” section + toggles

**Helper:** `AddBulkFixesSection(...)`
**Start anchors**

* `page.Separator(simple: true);`
* `page.SectionLabel(() => Translate(i18n, "config.section.wildAnimals.extraModAnimalKeys.bulkFixes.label", "Bulk apply quick-fixes"));`
* toggles:

  * `bulkFixes.trim`
  * `bulkFixes.dedupe`
  * `bulkFixes.dropUnknown`

---

### 8.4 Apply staged bulk changes toggle (writes config + clears stage)

**Helper:** `AddBulkApplyToggle(...)`
**Start anchors**

* `page.Bool(` with:

  * name key: `"config.wildAnimals.extraModAnimalKeys.bulkApply.name"`
    **Notable internal anchors**
* `if (skipNoop && !plan.AnyChangePlanned) { Monitor.Log("[GMCM][ExtraModAnimalKeys] Bulk Apply skipped (no-op)..." }`
* loops:

  * `foreach (var modIdRaw in st.Resets)`
  * `foreach (var (modIdRaw, keys) in st.Upserts)`
* clears staged:

  * `_extraModAnimalKeysBulkByWildAnimalKey.Remove(key);`

---

## 9) Validation warnings section (this is where your build errors came from after extraction)

### 9.1 Section label + dynamic paragraph

**Helper:** `AddValidationWarningsSection(...)`
**Start anchors**

* `page.Separator(simple: true);`
* `page.SectionLabel(() => Translate(i18n, "config.section.wildAnimals.extraModAnimalKeys.validation.label", "Validation warnings"));`
* `page.ParagraphDynamic(` with key:

  * `"config.paragraph.wildAnimals.extraModAnimalKeys.validation.header"`

**Critical build-stability note (type anchor)**
Inside this block you currently have:

* `var groups = new Dictionary<string, ExtraModAnimalKeysJson>(StringComparer.OrdinalIgnoreCase);`

When moved to a new file, you *must* ensure either:

* `using EggsInWild.Infrastructure.ModData.Models;`
* or fully qualify:

  * `Dictionary<string, EggsInWild.Infrastructure.ModData.Models.ExtraModAnimalKeysJson>`

Also this line is the core:

* `var warnings = ExtraModAnimalKeysWarnings.Build(groups, modAvailability, knownIds);`

---

## 10) Per-mod-group editors (the big loop that actually edits each block)

> This currently occurs **after** validation warnings.

### 10.1 Foreach `extraModIdsOrdered` loop

**Helper:** `AddPerModGroupEditors(...)`
**Start anchor**

* `foreach (var modId in extraModIdsOrdered)`
  **Contents anchors**
* `page.TextLabel(` with elementId:

  * `$"wildAnimals.{key}.extraModAnimalKeys.{Normalize(modId)}"`
* `page.BoolLabel(` with elementId:

  * `$"{elementId}.reset"`
  * translation key: `"config.wildAnimals.extraModAnimalKeys.resetBlock.name"`

---

## 11) Diagnostics section

### 11.1 Section label + sources paragraph

**Helper:** `AddDiagnosticsSourcesParagraph(...)`
**Start anchors**

* `page.Separator(simple: true);`
* `page.SectionLabel(() => Translate(i18n, "config.section.wildAnimals.diagnostics.label", "Diagnostics"));`
* `page.ParagraphDynamic(` with key:

  * `"config.paragraph.wildAnimals.diagnostics.sources.text"`

---

### 11.2 Resolved IDs paragraph (loaded mods only)

**Helper:** `AddDiagnosticsResolvedIdsParagraph(...)`
**Start anchor**

* `page.ParagraphDynamic(` with key:

  * `"config.paragraph.wildAnimals.diagnostics.resolvedIds.text"`

---

## 12) Reset override action (last control on page)

### 12.1 Reset override BoolLabel

**Helper:** `AddResetOverrideAction(...)`
**Start anchor**

* `page.BoolLabel(` with:

  * `elementId: $"wildAnimals.{key}.reset"`
  * name key: `"config.wildAnimals.reset.name"`

---

# Recommended extraction order (lowest risk, “move verbatim then refactor”)

1. Extract **big contiguous chunks** first (no internal edits):

   * scalar controls (Enabled/LargeEgg/Price/IsMagic)
   * location types section
   * ExtraModAnimalKeys whole block (custom + bulk + validation + per-mod editors + diagnostics + reset)
2. Then split ExtraModAnimalKeys into the smaller helpers listed above.

---
