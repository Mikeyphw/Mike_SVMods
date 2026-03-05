# RoosterControl GMCM Standardization Slice — IDE Agent Instruction File

## Mission

Execute **Phase 16A → 16D** for `Mike_SVMods` to standardize the RoosterControl GMCM structure using NestCore-first patterns, while keeping every phase buildable and commit-ready.

---

## Non-negotiable execution rules

* Execute phases strictly in order:

  * `16A`
  * `16B`
  * `16C`
  * `16D`
* Do not skip phases.
* Do not merge phases.
* Do not reorder steps.
* End **every phase** with a successful build.
* Create **exactly one commit per phase** after build succeeds.
* Do not create fixup commits.
* Do not amend previous phase commits.
* Do not refactor unrelated code.
* Do not rename:

  * GMCM page ids
  * GMCM option ids
  * GMCM field ids
  * translation keys
* Do not change config schema.
* Do not change runtime behavior.
* Do not touch:

  * `EggsInWild/**`
  * `LetMeHatch/**`
* Do not extract `compat` or `females` in this slice.
* Do not add a new GMCM action/button widget in this slice.
* Do not normalize i18n naming in this slice.
* Do not change save-aware rebuild behavior in this slice.
* Prefer NestCore helpers where explicitly instructed.
* If any step requires a design choice not explicitly described here, stop and report.

---

## Build gate for every phase

Run from repo root:

```bash
dotnet build NestCore/NestCore.csproj
dotnet build RoosterControl/RoosterControl.csproj
dotnet build Mike_SVMods.sln
```

If build fails:

* fix only errors introduced by the current phase,
* do not expand scope,
* if the failure appears unrelated, stop and report the first 3 errors.

---

## Commit messages

* Phase 16A:

  * `phase-16a: add NestCore GMCM batch helpers`
* Phase 16B:

  * `phase-16b: extract RoosterControl static GMCM pages`
* Phase 16C:

  * `phase-16c: extract RoosterControl rooster GMCM pages`
* Phase 16D:

  * `phase-16d: add RoosterControl GMCM menu orchestrator`

---

## Scope

### In scope

* `NestCore/Integrations/Gmcm/GmcmPageBuilder.cs`

* `NestCore/Integrations/Gmcm/GmcmMenuLayout.cs`

* `NestCore/GMCM_BUILDER.md`

* `NestCore/docs/PLAN.md`

* `NestCore/docs/CHECKLIST.md`

* `NestCore/docs/TODO.md`

* `RoosterControl/Application/RoosterCoordinator.cs`

* `RoosterControl/Application/RoosterCoordinator.Gmcm.cs`

* `RoosterControl/Integrations/Gmcm/RoosterControlGmcmMenu.cs`

* `RoosterControl/Integrations/Gmcm/Pages/RoosterRootPage.cs`

* `RoosterControl/Integrations/Gmcm/Pages/RoosterProbabilitiesPage.cs`

* `RoosterControl/Integrations/Gmcm/Pages/RoosterDebugPage.cs`

* `RoosterControl/Integrations/Gmcm/Pages/RoosterTypePages.cs`

* `RoosterControl/docs/PLAN.md`

* `RoosterControl/docs/CHECKLIST.md`

* `RoosterControl/docs/TODO.md`

### Out of scope

* `EggsInWild/**`
* `LetMeHatch/**`
* tests
* patch packaging
* config schema changes
* translation key renames
* runtime compat/hatching logic changes

---

## Phase 16A — NestCore GMCM batch helpers

### Objective

Add small NestCore DSL helpers for batched page links and batched page registration without changing existing behavior.

### Allowed files

* `NestCore/Integrations/Gmcm/GmcmPageBuilder.cs`
* `NestCore/Integrations/Gmcm/GmcmMenuLayout.cs`
* `NestCore/GMCM_BUILDER.md`
* `NestCore/docs/PLAN.md`
* `NestCore/docs/CHECKLIST.md`
* `NestCore/docs/TODO.md`

### Forbidden changes

* No changes to `GmcmConfigHost<TConfig>`
* No changes to `IGMCMOptionsHelper`
* No new widget/control types
* No call-site changes outside docs in this phase

### Step 16A.1

Edit file:

* `NestCore/Integrations/Gmcm/GmcmPageBuilder.cs`

Find this method:

```csharp
public void Link(string pageId, bool tooltip = false)
    => _menu.AddPageLink(pageId, tooltip);
```

Immediately after it, insert:

```csharp
public void Links(IEnumerable<string> pageIds, bool tooltip = false)
{
    foreach (var pageId in pageIds)
        Link(pageId, tooltip);
}

public void Links(bool tooltip = false, params string[] pageIds)
    => Links((IEnumerable<string>)pageIds, tooltip);
```

### Step 16A.2

Edit file:

* `NestCore/Integrations/Gmcm/GmcmMenuLayout.cs`

Find this method:

```csharp
public GmcmMenuLayout<TConfig> Page(string pageId, Func<string> title, Action<GmcmPageBuilder<TConfig>> build, bool printId = false)
{
    _pages.Add(PageDef.Custom(pageId, title, build, printId));
    return this;
}
```

Immediately after it, insert:

```csharp
public GmcmMenuLayout<TConfig> Pages(
    IEnumerable<(string PageId, Action<GmcmPageBuilder<TConfig>> Build)> pages,
    bool printId = false)
{
    foreach (var (pageId, build) in pages)
        Page(pageId, build, printId);

    return this;
}

public GmcmMenuLayout<TConfig> Pages(
    IEnumerable<(string PageId, Func<string> Title, Action<GmcmPageBuilder<TConfig>> Build)> pages,
    bool printId = false)
{
    foreach (var (pageId, title, build) in pages)
        Page(pageId, title, build, printId);

    return this;
}
```

### Step 16A.3

Edit file:

* `NestCore/GMCM_BUILDER.md`

Append this section at the end of the file:

## Batch helpers

### `GmcmPageBuilder<TConfig>.Links(...)`

Use this when a page needs to register a list of page links without repeating `Link(...)` calls.

```csharp
root.Links(
    tooltip: true,
    "compat",
    "probabilities",
    "females",
    "debug"
);
```

### `GmcmMenuLayout<TConfig>.Pages(...)`

Use this when a menu needs to register a repeated set of subpages.

```csharp
layout.Pages(new (string PageId, Action<GmcmPageBuilder<MyConfig>> Build)[]
{
    ("rooster.white", b => BuildRoosterPage(b, RoosterType.White, "rooster.white")),
    ("rooster.brown", b => BuildRoosterPage(b, RoosterType.Brown, "rooster.brown")),
});
```

### Step 16A.4

Edit file:

* `NestCore/docs/PLAN.md`

Append:

## Phase 8 — GMCM batching helpers

Goal: add small GMCM DSL helpers that reduce repeated page-link/page-registration code without changing behavior.

Steps:

1. Add `GmcmPageBuilder<TConfig>.Links(...)` overloads for batched page-link registration.
2. Add `GmcmMenuLayout<TConfig>.Pages(...)` overloads for batched page registration.
3. Keep existing `Root(...)`, `Page(...)`, and `Build()` behavior unchanged.
4. Document the new helpers in `GMCM_BUILDER.md` with one generated-subpages example.
5. Update `docs/CHECKLIST.md` and `docs/TODO.md` to track the new helper surface.
6. Verify `NestCore` builds cleanly.

Acceptance:

* `Links(...)` exists and delegates to existing `Link(...)`.
* `Pages(...)` exists and delegates to existing `Page(...)`.
* Existing GMCM call sites do not require changes.
* `GMCM_BUILDER.md` includes helper usage notes and example.

### Step 16A.5

Edit file:

* `NestCore/docs/CHECKLIST.md`

Append:

## Phase 8 — GMCM batching helpers

* [ ] Add `GmcmPageBuilder<TConfig>.Links(...)` overloads for batched page-link registration
* [ ] Add `GmcmMenuLayout<TConfig>.Pages(...)` overloads for batched page registration
* [ ] Keep existing `Root(...)`, `Page(...)`, and `Build()` behavior unchanged
* [ ] Document the new helpers in `GMCM_BUILDER.md`
* [ ] Update `docs/TODO.md` and `docs/PLAN.md`
* [ ] Verify `NestCore` builds cleanly

### Step 16A.6

Edit file:

* `NestCore/docs/TODO.md`

Append near the top unchecked items block:

* [ ] Phase 8: Add `GmcmPageBuilder<TConfig>.Links(...)` overloads for batched page-link registration.
* [ ] Phase 8: Add `GmcmMenuLayout<TConfig>.Pages(...)` overloads for batched page registration.
* [ ] Phase 8: Document batched GMCM page/link helpers in `GMCM_BUILDER.md`.

### Step 16A.7

Run the build gate.

### Step 16A.8

If build succeeds, create commit:

```bash
git add NestCore/Integrations/Gmcm/GmcmPageBuilder.cs NestCore/Integrations/Gmcm/GmcmMenuLayout.cs NestCore/GMCM_BUILDER.md NestCore/docs/PLAN.md NestCore/docs/CHECKLIST.md NestCore/docs/TODO.md
git commit -m "phase-16a: add NestCore GMCM batch helpers"
```

---

## Phase 16B — Extract RoosterControl static GMCM pages

### Objective

Move the root, probabilities, and debug GMCM page bodies out of `RoosterCoordinator.Gmcm.cs` into dedicated helper classes, with no behavior change.

### Allowed files

* `RoosterControl/Application/RoosterCoordinator.Gmcm.cs`
* `RoosterControl/Integrations/Gmcm/Pages/RoosterRootPage.cs`
* `RoosterControl/Integrations/Gmcm/Pages/RoosterProbabilitiesPage.cs`
* `RoosterControl/Integrations/Gmcm/Pages/RoosterDebugPage.cs`
* `RoosterControl/docs/PLAN.md`
* `RoosterControl/docs/CHECKLIST.md`
* `RoosterControl/docs/TODO.md`

### Forbidden changes

* Do not change `BuildCompatPage`
* Do not change `BuildFemalesPage`
* Do not change page ids
* Do not change option ids
* Do not change `#if RCTRL_DEV` behavior
* Do not touch `RoosterCoordinator.cs`

### Step 16B.1

Create file:

* `RoosterControl/Integrations/Gmcm/Pages/RoosterRootPage.cs`

Contents:

```csharp
using Mikew.NestCore.Integrations.Gmcm;
using RoosterControl.Framework.Config;

namespace RoosterControl.Integrations.Gmcm.Pages;

internal static class RoosterRootPage
{
    public static void Build(GmcmPageBuilder<RoosterControlConfig> b)
    {
        b.Section("root.general");
        b.Enum(cfg => cfg.General.UpdateRate, optionId: "general.updateRate", tooltip: true);
        b.Enum(cfg => cfg.General.HatchingMode, optionId: "general.hatchingMode", tooltip: true);
        b.Enum(cfg => cfg.General.HarvestType, optionId: "general.harvestType", tooltip: true);

        b.Separator(simple: true);

        b.Section("root.pages");
        b.Links(
            tooltip: true,
            "compat",
            "probabilities",
            "females",
            "debug"
        );

        b.Separator(simple: true);

        b.Section("root.roosters");
        b.Links(
            tooltip: true,
            "rooster.white",
            "rooster.brown",
            "rooster.blue",
            "rooster.void",
            "rooster.gold"
        );
    }
}
```

### Step 16B.2

Create file:

* `RoosterControl/Integrations/Gmcm/Pages/RoosterProbabilitiesPage.cs`

Contents:

```csharp
using Mikew.NestCore.Integrations.Gmcm;
using RoosterControl.Framework.Config;

namespace RoosterControl.Integrations.Gmcm.Pages;

internal static class RoosterProbabilitiesPage
{
    public static void Build(GmcmPageBuilder<RoosterControlConfig> b)
    {
        b.Section("probabilities.eggs");
        b.Bool(cfg => cfg.General.EnableEggProbability, optionId: "prob.enableEggProbability", tooltip: true);
        b.Int(cfg => cfg.General.EggProbability, optionId: "prob.eggProbability", tooltip: true, min: 0, max: 100, interval: 1);

        b.Bool(cfg => cfg.General.EnableLargeEggProbability, optionId: "prob.enableLargeEggProbability", tooltip: true);
        b.Int(cfg => cfg.General.LargeEggProbability, optionId: "prob.largeEggProbability", tooltip: true, min: 0, max: 100, interval: 1);

        b.Int(cfg => cfg.General.FertilizedEggProbability, optionId: "prob.fertilizedEggProbability", tooltip: true, min: 0, max: 100, interval: 1);

        b.Separator(simple: true);

        b.Section("probabilities.breeding");
        b.Int(cfg => cfg.General.RoosterToChickenRatio, optionId: "prob.roosterToChickenRatio", tooltip: true, min: 0, max: 100, interval: 1);
        b.Float(cfg => cfg.General.BoostByRooster, optionId: "prob.boostByRooster", tooltip: true, min: 0, max: 10, interval: 0.05f);
        b.Int(cfg => cfg.General.ChosenPercentageDailyLuckBonus, optionId: "prob.chosenDailyLuckBonusPct", tooltip: true, min: 0, max: 100, interval: 1);
    }
}
```

### Step 16B.3

Create file:

* `RoosterControl/Integrations/Gmcm/Pages/RoosterDebugPage.cs`

Contents:

```csharp
using Mikew.NestCore.Integrations.Gmcm;
using RoosterControl.Framework.Config;

namespace RoosterControl.Integrations.Gmcm.Pages;

internal static class RoosterDebugPage
{
    public static void Build(GmcmPageBuilder<RoosterControlConfig> b)
    {
        b.ParagraphGmcmOptionsStatus();
        b.Separator(simple: true);

        b.Section("debug.flags");
        b.Bool(cfg => cfg.General.EnableDebug, optionId: "debug.enableDebug", tooltip: true);

        b.Separator(simple: true);

        b.Section("debug.logging");
        b.Bool(cfg => cfg.VerboseLogging, optionId: "debug.verboseLogging", tooltip: true);
        b.Bool(cfg => cfg.TestingLogging, optionId: "debug.testingLogging", tooltip: true);

        b.Paragraph("debug.note");

#if RCTRL_DEV
        b.Separator(simple: true);
        b.Section("debug.tuning.luck");
        b.Float(cfg => cfg.DebugKnobs.BonusCalculationMultiplier, optionId: "debug.tuning.bonusCalculation", tooltip: true, min: 0, max: 10, interval: 0.05f);
        b.Float(cfg => cfg.DebugKnobs.SuperUnluckyBonusMultiplier, optionId: "debug.tuning.superUnluckyBonus", tooltip: true, min: -10, max: 0, interval: 0.25f);
        b.Float(cfg => cfg.DebugKnobs.SuperLuckyBonusMultiplier, optionId: "debug.tuning.superLuckyBonus", tooltip: true, min: 0, max: 10, interval: 0.25f);
        b.Float(cfg => cfg.DebugKnobs.LuckAdjustmentOffset, optionId: "debug.tuning.luckAdjustment", tooltip: true, min: -100, max: 100, interval: 1f);
#endif
    }
}
```

### Step 16B.4

Edit file:

* `RoosterControl/Application/RoosterCoordinator.Gmcm.cs`

Add using:

```csharp
using RoosterControl.Integrations.Gmcm.Pages;
```

### Step 16B.5

In `DefineGmcmLayout(...)`, replace the inline root page body with:

```csharp
layout.Root(RoosterRootPage.Build);
```

### Step 16B.6

In `DefineGmcmLayout(...)`, replace the inline probabilities page body with:

```csharp
layout.Page("probabilities", RoosterProbabilitiesPage.Build);
```

### Step 16B.7

In `DefineGmcmLayout(...)`, replace the inline debug page body with:

```csharp
layout.Page("debug", RoosterDebugPage.Build);
```

### Step 16B.8

Do not change these lines in this phase:

```csharp
layout.Page("compat", BuildCompatPage);
layout.Page("females", BuildFemalesPage);
AddRoosterPages(layout);
```

### Step 16B.9

Remove only the moved inline page bodies from `RoosterCoordinator.Gmcm.cs`.

### Step 16B.10

Edit file:

* `RoosterControl/docs/PLAN.md`

Add under `## Phase status summary`:

* Phase 16A–16D: planned (GMCM standardization slice after Phase 15).

Append at end:

## Phase 16A — Adopt NestCore GMCM batching helpers

Goal: use tiny NestCore DSL improvements to reduce repeated GMCM layout/link code before extracting pages.

Steps:

1. Consume NestCore `Links(...)` helper from the RoosterControl root GMCM page.
2. Keep all existing page ids, option ids, and i18n keys unchanged.
3. Do not change compat/females/rooster runtime behavior.
4. Update `docs/CHECKLIST.md` and `docs/TODO.md`.
5. Verify `RoosterControl` and solution builds cleanly.

Acceptance:

* Root page link registration uses batched helper(s).
* No GMCM ids or config behavior changes.
* Build passes.

## Phase 16B — Extract static GMCM pages

Goal: move root/probabilities/debug page definitions out of `RoosterCoordinator.Gmcm.cs` into dedicated GMCM page helper classes.

Steps:

1. Add `Integrations/Gmcm/Pages/RoosterRootPage.cs`.
2. Add `Integrations/Gmcm/Pages/RoosterProbabilitiesPage.cs`.
3. Add `Integrations/Gmcm/Pages/RoosterDebugPage.cs`.
4. Update `DefineGmcmLayout(...)` to delegate to those page helper classes.
5. Keep `compat` and `females` pages in the coordinator for now.
6. Update docs and verify build.

Acceptance:

* `RoosterCoordinator.Gmcm.cs` no longer contains inline root/probabilities/debug page bodies.
* All page ids and option ids are unchanged.
* Build passes.

## Phase 16C — Extract rooster-type GMCM pages

Goal: move rooster subpage registration and rooster-page content into a dedicated helper.

Steps:

1. Add `Integrations/Gmcm/Pages/RoosterTypePages.cs`.
2. Move rooster-page registration and editor logic into that helper.
3. Add minimal coordinator access needed for translation/config access only.
4. Keep `compat` and `females` pages in the coordinator.
5. Update docs and verify build.

Acceptance:

* `RoosterCoordinator.Gmcm.cs` no longer owns rooster page registration or rooster-page content.
* Rooster page ids and option ids are unchanged.
* Build passes.

## Phase 16D — Add RoosterControl GMCM orchestrator

Goal: introduce a dedicated GMCM layout orchestrator so the coordinator stops owning menu composition.

Steps:

1. Add `Integrations/Gmcm/RoosterControlGmcmMenu.cs`.
2. Move overall layout composition into that class.
3. Keep coordinator ownership only for host setup, rebuild/save/reset, and the still-coupled `compat`/`females` page delegates.
4. Replace direct `DefineGmcmLayout(...)` usage with the orchestrator.
5. Update docs and verify build.

Acceptance:

* `RoosterControlGmcmMenu` becomes the single GMCM layout orchestrator.
* `compat` and `females` remain on the coordinator.
* No GMCM ids change.
* Build passes.

## Deferred after Phase 16D

* Decide whether `compat` should remain in the coordinator due world/runtime coupling.
* Decide whether `females` should remain in the coordinator due plan/runtime coupling.
* Consider a NestCore-first action/button DSL primitive before attempting repo-wide GMCM action standardization.

### Step 16B.11

Edit file:

* `RoosterControl/docs/CHECKLIST.md`

Append:

## Phase 16A — Adopt NestCore GMCM batching helpers

* [ ] Consume NestCore `Links(...)` helper from the RoosterControl root GMCM page
* [ ] Keep all existing page ids, option ids, and i18n keys unchanged
* [ ] Update docs (PLAN/CHECKLIST/TODO)
* [ ] Verify build

## Phase 16B — Extract static GMCM pages

* [ ] Add `Integrations/Gmcm/Pages/RoosterRootPage.cs`
* [ ] Add `Integrations/Gmcm/Pages/RoosterProbabilitiesPage.cs`
* [ ] Add `Integrations/Gmcm/Pages/RoosterDebugPage.cs`
* [ ] Delegate `DefineGmcmLayout(...)` to the extracted page helpers
* [ ] Keep `compat` and `females` on the coordinator
* [ ] Verify build

## Phase 16C — Extract rooster-type GMCM pages

* [ ] Add `Integrations/Gmcm/Pages/RoosterTypePages.cs`
* [ ] Move rooster page registration and editor logic into that helper
* [ ] Add only minimal coordinator access needed for translation/config
* [ ] Keep `compat` and `females` on the coordinator
* [ ] Verify build

## Phase 16D — Add RoosterControl GMCM orchestrator

* [ ] Add `Integrations/Gmcm/RoosterControlGmcmMenu.cs`
* [ ] Move layout composition into the orchestrator
* [ ] Keep coordinator ownership only for host setup, rebuild/save/reset, and `compat`/`females`
* [ ] Replace direct `DefineGmcmLayout(...)` usage with the orchestrator
* [ ] Update docs (PLAN/CHECKLIST/TODO)
* [ ] Verify build

### Step 16B.12

Edit file:

* `RoosterControl/docs/TODO.md`

Replace the `## Next:` block with:

## Next: Phase 15 — Compat test matrix + content-pack examples

* [x] Compat matrix doc
* [x] Example templates/content packs + schema notes
* [ ] Reference patterns (match types + gating)
* [ ] Unit tests: catalog loading + deterministic repair
* [ ] Unit tests: fertilized mapping determinism (native vs synthesized)
* [ ] Manual QA checklist + smoke-test notes
* [ ] Docs wired to templates + matrix

## After Phase 15: Phase 16 — GMCM standardization slice

* [ ] 16A: adopt NestCore GMCM batching helpers in RoosterControl
* [ ] 16B: extract root/probabilities/debug page builders into `Integrations/Gmcm/Pages/*`
* [ ] 16C: extract rooster page registration and editors into `RoosterTypePages`
* [ ] 16D: add `RoosterControlGmcmMenu` as the layout orchestrator
* [ ] Decide whether `compat` remains coordinator-owned
* [ ] Decide whether `females` remains coordinator-owned
* [ ] Keep all GMCM page ids / option ids / i18n keys unchanged during Phase 16

### Step 16B.13

Run the build gate.

### Step 16B.14

If build succeeds, create commit:

```bash
git add RoosterControl/Application/RoosterCoordinator.Gmcm.cs RoosterControl/Integrations/Gmcm/Pages/RoosterRootPage.cs RoosterControl/Integrations/Gmcm/Pages/RoosterProbabilitiesPage.cs RoosterControl/Integrations/Gmcm/Pages/RoosterDebugPage.cs RoosterControl/docs/PLAN.md RoosterControl/docs/CHECKLIST.md RoosterControl/docs/TODO.md
git commit -m "phase-16b: extract RoosterControl static GMCM pages"
```

---

## Phase 16C — Extract rooster-type GMCM pages

### Objective

Move rooster subpage registration and rooster page editor logic into a dedicated helper file while keeping behavior unchanged.

### Allowed files

* `RoosterControl/Application/RoosterCoordinator.cs`
* `RoosterControl/Application/RoosterCoordinator.Gmcm.cs`
* `RoosterControl/Integrations/Gmcm/Pages/RoosterTypePages.cs`
* `RoosterControl/Integrations/Gmcm/Pages/RoosterRootPage.cs`
* `RoosterControl/docs/PLAN.md`
* `RoosterControl/docs/CHECKLIST.md`
* `RoosterControl/docs/TODO.md`

### Forbidden changes

* Do not change `BuildCompatPage`
* Do not change `BuildFemalesPage`
* Do not change rooster page ids
* Do not change rooster option ids
* Do not change default config behavior
* Do not add more than one new coordinator accessor

### Step 16C.1

Edit file:

* `RoosterControl/Application/RoosterCoordinator.cs`

Add exactly one new accessor near existing internal properties/accessors:

```csharp
internal I18TranslationHelper I18n => _i18n;
```

Do not add any other new accessor in this phase.

### Step 16C.2

Create file:

* `RoosterControl/Integrations/Gmcm/Pages/RoosterTypePages.cs`

Create this shell first:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mikew.NestCore.Integrations.Gmcm;
using RoosterControl.Constants;
using RoosterControl.Enums;
using RoosterControl.Framework.Config;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using RoosterControl.Application;

namespace RoosterControl.Integrations.Gmcm.Pages;

internal static class RoosterTypePages
{
    public static readonly string[] PageIds =
    {
        "rooster.white",
        "rooster.brown",
        "rooster.blue",
        "rooster.void",
        "rooster.gold",
    };

    public static void Define(GmcmMenuLayout<RoosterControlConfig> layout, RoosterCoordinator owner)
    {
        layout.Pages(new (string PageId, Action<GmcmPageBuilder<RoosterControlConfig>> Build)[]
        {
            ("rooster.white", b => BuildRoosterPage(b, owner, RoosterType.White, "rooster.white")),
            ("rooster.brown", b => BuildRoosterPage(b, owner, RoosterType.Brown, "rooster.brown")),
            ("rooster.blue", b => BuildRoosterPage(b, owner, RoosterType.Blue, "rooster.blue")),
            ("rooster.void", b => BuildRoosterPage(b, owner, RoosterType.Void, "rooster.void")),
            ("rooster.gold", b => BuildRoosterPage(b, owner, RoosterType.Gold, "rooster.gold")),
        });
    }
}
```

### Step 16C.3

Move these members from `RoosterCoordinator.Gmcm.cs` into `RoosterTypePages.cs`:

* `GetOrCreateRoosterSettings`
* `ProduceChoiceAllowedValues`
* `BuildProduceChoiceAllowedValues`
* `EnsureProduceEntriesNormalized`
* `BuildProduceEntriesEditor`
* `BuildRoosterPage`

Move them as-is first, then adapt references.

### Step 16C.4

After moving, make these signature changes:

```csharp
private static void BuildRoosterPage(
    GmcmPageBuilder<RoosterControlConfig> b,
    RoosterCoordinator owner,
    RoosterType type,
    string pageId)
```

```csharp
private static RoosterSettings GetOrCreateRoosterSettings(RoosterCoordinator owner, RoosterType type)
```

```csharp
private static void BuildProduceEntriesEditor(
    GmcmPageBuilder<RoosterControlConfig> b,
    RoosterCoordinator owner,
    string pageId,
    bool isDeluxe,
    IList<ProduceEntry> entries)
```

### Step 16C.5

Inside moved members:

* replace `Config` with `owner.Config`
* replace `_i18n` with `owner.I18n`
* make helpers `private static` where possible

### Step 16C.6

Edit file:

* `RoosterControl/Application/RoosterCoordinator.Gmcm.cs`

Add:

```csharp
using RoosterControl.Integrations.Gmcm.Pages;
```

### Step 16C.7

In `RoosterCoordinator.Gmcm.cs`, replace:

```csharp
AddRoosterPages(layout);
```

with:

```csharp
RoosterTypePages.Define(layout, this);
```

### Step 16C.8

Delete these members from `RoosterCoordinator.Gmcm.cs` after they have been moved and compile successfully:

* `AddRoosterPages`
* `GetRoosterPages`
* `BuildRoosterPage`
* `GetOrCreateRoosterSettings`
* `ProduceChoiceAllowedValues`
* `BuildProduceChoiceAllowedValues`
* `EnsureProduceEntriesNormalized`
* `BuildProduceEntriesEditor`

### Step 16C.9

Edit file:

* `RoosterControl/Integrations/Gmcm/Pages/RoosterRootPage.cs`

Replace the rooster-links block with:

```csharp
b.Section("root.roosters");
b.Links(tooltip: true, RoosterTypePages.PageIds);
```

Do not change the non-rooster root page links.

### Step 16C.10

Mark Phase 16C items in docs as completed if the repo’s docs workflow expects same-phase status updates after implementation. Do not rewrite previous completed sections.

### Step 16C.11

Run the build gate.

### Step 16C.12

If build succeeds, create commit:

```bash
git add RoosterControl/Application/RoosterCoordinator.cs RoosterControl/Application/RoosterCoordinator.Gmcm.cs RoosterControl/Integrations/Gmcm/Pages/RoosterTypePages.cs RoosterControl/Integrations/Gmcm/Pages/RoosterRootPage.cs RoosterControl/docs/PLAN.md RoosterControl/docs/CHECKLIST.md RoosterControl/docs/TODO.md
git commit -m "phase-16c: extract RoosterControl rooster GMCM pages"
```

---

## Phase 16D — Add RoosterControl GMCM orchestrator

### Objective

Introduce a dedicated orchestrator class that owns layout composition while the coordinator retains host setup, rebuild/save/reset, and the still-coupled page delegates.

### Allowed files

* `RoosterControl/Application/RoosterCoordinator.Gmcm.cs`
* `RoosterControl/Integrations/Gmcm/RoosterControlGmcmMenu.cs`
* `RoosterControl/Integrations/Gmcm/Pages/RoosterRootPage.cs`
* `RoosterControl/Integrations/Gmcm/Pages/RoosterProbabilitiesPage.cs`
* `RoosterControl/Integrations/Gmcm/Pages/RoosterDebugPage.cs`
* `RoosterControl/Integrations/Gmcm/Pages/RoosterTypePages.cs`
* `RoosterControl/docs/PLAN.md`
* `RoosterControl/docs/CHECKLIST.md`
* `RoosterControl/docs/TODO.md`

### Forbidden changes

* Do not extract `BuildCompatPage`
* Do not extract `BuildFemalesPage`
* Do not change rebuild timing
* Do not rename `RequestGmcmRebuild`
* Do not touch runtime compat/hatching logic

### Step 16D.1

Create file:

* `RoosterControl/Integrations/Gmcm/RoosterControlGmcmMenu.cs`

Contents:

```csharp
using System;
using Mikew.NestCore.Integrations.Gmcm;
using RoosterControl.Framework.Config;
using RoosterControl.Application;
using RoosterControl.Integrations.Gmcm.Pages;

namespace RoosterControl.Integrations.Gmcm;

internal sealed class RoosterControlGmcmMenu
{
    private readonly RoosterCoordinator _owner;
    private readonly Action<GmcmPageBuilder<RoosterControlConfig>> _buildCompatPage;
    private readonly Action<GmcmPageBuilder<RoosterControlConfig>> _buildFemalesPage;

    public RoosterControlGmcmMenu(
        RoosterCoordinator owner,
        Action<GmcmPageBuilder<RoosterControlConfig>> buildCompatPage,
        Action<GmcmPageBuilder<RoosterControlConfig>> buildFemalesPage)
    {
        _owner = owner;
        _buildCompatPage = buildCompatPage;
        _buildFemalesPage = buildFemalesPage;
    }

    public void DefineLayout(GmcmMenuLayout<RoosterControlConfig> layout)
    {
        layout.Root(RoosterRootPage.Build);
        layout.Page("compat", _buildCompatPage);
        layout.Page("probabilities", RoosterProbabilitiesPage.Build);
        layout.Page("females", _buildFemalesPage);
        layout.Page("debug", RoosterDebugPage.Build);
        RoosterTypePages.Define(layout, _owner);
    }
}
```

### Step 16D.2

Edit file:

* `RoosterControl/Application/RoosterCoordinator.Gmcm.cs`

Add using:

```csharp
using RoosterControl.Integrations.Gmcm;
```

### Step 16D.3

In `RoosterCoordinator.Gmcm.cs`, add field next to `_gmcmHost`:

```csharp
private RoosterControlGmcmMenu? _gmcmMenu;
```

### Step 16D.4

In `OnGameLaunched(...)`, after `_gmcmHost` is created and null-checked, instantiate:

```csharp
_gmcmMenu = new RoosterControlGmcmMenu(this, BuildCompatPage, BuildFemalesPage);
```

### Step 16D.5

Replace the initial `_gmcmHost.RebuildLayout(...)` call so `define:` becomes:

```csharp
define: _gmcmMenu.DefineLayout,
```

Keep all other arguments unchanged.

### Step 16D.6

In `RequestGmcmRebuild()`, replace:

```csharp
define: DefineGmcmLayout,
```

with:

```csharp
define: _gmcmMenu!.DefineLayout,
```

Keep all other arguments unchanged.

### Step 16D.7

Handle `DefineGmcmLayout(...)` as follows:

* if no references remain, delete it completely,
* otherwise reduce it to:

```csharp
private void DefineGmcmLayout(GmcmMenuLayout<RoosterControlConfig> layout)
    => _gmcmMenu!.DefineLayout(layout);
```

If the wrapper remains, it must contain no inline layout body.

### Step 16D.8

Update docs only as needed to reflect completion of 16D and add a deferred note:

* `Phase 16E (future): decide whether to extract compat/females pages or keep them coordinator-owned`

Do not add new scope beyond that deferred note.

### Step 16D.9

Run the build gate.

### Step 16D.10

If build succeeds, create commit:

```bash
git add RoosterControl/Application/RoosterCoordinator.Gmcm.cs RoosterControl/Integrations/Gmcm/RoosterControlGmcmMenu.cs RoosterControl/Integrations/Gmcm/Pages/RoosterRootPage.cs RoosterControl/Integrations/Gmcm/Pages/RoosterProbabilitiesPage.cs RoosterControl/Integrations/Gmcm/Pages/RoosterDebugPage.cs RoosterControl/Integrations/Gmcm/Pages/RoosterTypePages.cs RoosterControl/docs/PLAN.md RoosterControl/docs/CHECKLIST.md RoosterControl/docs/TODO.md
git commit -m "phase-16d: add RoosterControl GMCM menu orchestrator"
```

---

## Stop conditions

Stop immediately and report if any of these happen:

* `RoosterControl/Integrations/Gmcm/*` cannot reference `RoosterCoordinator` due visibility constraints
* moving rooster helper methods requires access to coordinator internals beyond:

  * `Config`
  * `I18n`
* `compat` or `females` must be extracted to make a phase compile
* any phase requires renaming page ids, option ids, or translation keys
* any build failure appears unrelated to the current phase

---

## Expected end state after Phase 16D

### NestCore

* `GmcmPageBuilder<TConfig>` supports batched page link registration via `Links(...)`
* `GmcmMenuLayout<TConfig>` supports batched page registration via `Pages(...)`
* `GMCM_BUILDER.md` documents both helpers

### RoosterControl

* `RoosterControl/Integrations/Gmcm/Pages/RoosterRootPage.cs` owns the root page body
* `RoosterControl/Integrations/Gmcm/Pages/RoosterProbabilitiesPage.cs` owns the probabilities page body
* `RoosterControl/Integrations/Gmcm/Pages/RoosterDebugPage.cs` owns the debug page body
* `RoosterControl/Integrations/Gmcm/Pages/RoosterTypePages.cs` owns rooster page registration and rooster page editor logic
* `RoosterControl/Integrations/Gmcm/RoosterControlGmcmMenu.cs` owns overall layout composition
* `RoosterCoordinator.Gmcm.cs` retains only:

  * GMCM host setup
  * save/reset/rebuild plumbing
  * `BuildCompatPage(...)`
  * `BuildFemalesPage(...)`
  * any still-required compat/female helper logic

---

## Deferred after this slice

Do not do these now:

* extracting `BuildCompatPage(...)`
* extracting `BuildFemalesPage(...)`
* adding a NestCore action/button DSL primitive
* repo-wide GMCM standardization for other mods
* i18n key normalization
* config schema cleanup

**a.** Turn this into a patch-by-patch execution checklist with explicit “after commit, verify X” gates
**b.** Write the next agent instruction file for the deferred decision slice: `compat` / `females` extraction vs keeping them coordinator-owned
