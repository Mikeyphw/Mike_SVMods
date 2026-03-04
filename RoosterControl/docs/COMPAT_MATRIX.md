# Compat matrix

This doc is a **living test matrix** for RoosterControl’s compat catalogs. It’s meant to answer:

- *What sources/providers exist today?*
- *What do they depend on?*
- *What should we see in `ra_compat status/validate`?*

See also:
- `docs/PLAN.md` (phases)
- `docs/CHECKLIST.md` (completion tracking)
- `docs/TODO.md` (next work)
- `docs/COMPAT_TEMPLATES.md` (schemas + content-pack templates)
- `examples/contentpacks/RoosterControl.ExampleCompatSources/` (known-good pack)

## How compat works (high level)

RoosterControl loads two CP-patchable catalogs:

- **Egg sources**: `Mods/<RoosterControlId>/compat/egg_sources`
- **Rooster providers**: `Mods/<RoosterControlId>/compat/rooster_sources`

The mod also has built-in defaults (see `Application/Compat/CompatDefaults.cs`) which are used as the baseline for content-pack patching.

**Selections are per-save**. If a selected entry becomes invalid (missing item / missing `Data/FarmAnimals` key / bad index), RoosterControl will:
- **WARN** in `ra_compat validate` and `ra_compat status`
- deterministically repair selections (or disable the affected feature when catalogs are empty)

## Commands for QA

- `ra_compat status` — single-block health + selections summary
- `ra_compat validate [EggKind]` — deeper validation (missing deps are **WARN**)
- `ra_compat list` — prints catalogs + current per-save selections
- Reset:
  - `ra_compat reset-all`
  - `ra_compat reset-eggs <EggKind|all>`
  - `ra_compat reset-roosters`

## Egg sources catalog (defaults)

| EggKind | SourceId | DisplayName | Type | Ref | Requirements |
|---|---|---|---|---|---|
| BrownEgg | `farmanimal:brown-chicken:egg0` | Brown Chicken (EggItemIds[0]) | FarmAnimalRef | `Brown Chicken` → `EggItemIds[0]` | Requires `Data/FarmAnimals` key `Brown Chicken` with `EggItemIds[0]`. |
| LargeBrownEgg | `farmanimal:brown-chicken:egg1` | Brown Chicken (EggItemIds[1]) | FarmAnimalRef | `Brown Chicken` → `EggItemIds[1]` | Requires `Data/FarmAnimals` key `Brown Chicken` with `EggItemIds[1]`. |
| WhiteEgg | `farmanimal:white-chicken:egg0` | White Chicken (EggItemIds[0]) | FarmAnimalRef | `White Chicken` → `EggItemIds[0]` | Requires `Data/FarmAnimals` key `White Chicken` with `EggItemIds[0]`. |
| LargeWhiteEgg | `farmanimal:white-chicken:egg1` | White Chicken (EggItemIds[1]) | FarmAnimalRef | `White Chicken` → `EggItemIds[1]` | Requires `Data/FarmAnimals` key `White Chicken` with `EggItemIds[1]`. |
| BlueEgg | `farmanimal:blue-chicken:egg0` | Blue Chicken (EggItemIds[0]) | FarmAnimalRef | `Blue Chicken` → `EggItemIds[0]` | Requires `Data/FarmAnimals` key `Blue Chicken` with `EggItemIds[0]`. |
| BlueEgg | `meara:blue-egg` | Meara Roosters: Blue Egg | ItemId | `Meara.RoostersBlueEgg` | Requires item `Meara.RoostersBlueEgg` (likely `Meara` pack/mod). |
| LargeBlueEgg | `farmanimal:blue-chicken:egg1` | Blue Chicken (EggItemIds[1]) | FarmAnimalRef | `Blue Chicken` → `EggItemIds[1]` | Requires `Data/FarmAnimals` key `Blue Chicken` with `EggItemIds[1]`. |
| LargeBlueEgg | `meara:large-blue-egg` | Meara Roosters: Large Blue Egg | ItemId | `Meara.RoostersLargeBlueEgg` | Requires item `Meara.RoostersLargeBlueEgg` (likely `Meara` pack/mod). |
| VoidEgg | `farmanimal:void-chicken:egg0` | Void Chicken (EggItemIds[0]) | FarmAnimalRef | `Void Chicken` → `EggItemIds[0]` | Requires `Data/FarmAnimals` key `Void Chicken` with `EggItemIds[0]`. |
| LargeVoidEgg | `farmanimal:void-chicken:egg1` | Void Chicken (EggItemIds[1]) | FarmAnimalRef | `Void Chicken` → `EggItemIds[1]` | Requires `Data/FarmAnimals` key `Void Chicken` with `EggItemIds[1]`. |
| LargeVoidEgg | `meara:large-void-egg` | Meara Roosters: Large Void Egg | ItemId | `Meara.RoostersLargevoidEgg` | Requires item `Meara.RoostersLargevoidEgg` (likely `Meara` pack/mod). |
| GoldEgg | `farmanimal:golden-chicken:egg0` | Golden Chicken (EggItemIds[0]) | FarmAnimalRef | `Golden Chicken` → `EggItemIds[0]` | Requires `Data/FarmAnimals` key `Golden Chicken` with `EggItemIds[0]`. |
| LargeGoldEgg | `farmanimal:golden-chicken:egg1` | Golden Chicken (EggItemIds[1]) | FarmAnimalRef | `Golden Chicken` → `EggItemIds[1]` | Requires `Data/FarmAnimals` key `Golden Chicken` with `EggItemIds[1]`. |
| LargeGoldEgg | `meara:large-gold-egg` | Meara Roosters: Large Gold Egg | ItemId | `Meara.RoostersLargegoldEgg` | Requires item `Meara.RoostersLargegoldEgg` (likely `Meara` pack/mod). |
| FertilizedBrownEgg | `meara:fert-brown` | Meara Roosters: Fertilized Brown Egg | ItemId | `Meara.Roosters_FertEggBrown` | Requires item `Meara.Roosters_FertEggBrown` (likely `Meara` pack/mod). |
| FertilizedWhiteEgg | `meara:fert-white` | Meara Roosters: Fertilized White Egg | ItemId | `Meara.Roosters_FertEggWhite` | Requires item `Meara.Roosters_FertEggWhite` (likely `Meara` pack/mod). |
| FertilizedBlueEgg | `meara:fert-blue` | Meara Roosters: Fertilized Blue Egg | ItemId | `Meara.Roosters_FertEggBlue` | Requires item `Meara.Roosters_FertEggBlue` (likely `Meara` pack/mod). |
| FertilizedVoidEgg | `meara:fert-void` | Meara Roosters: Fertilized Void Egg | ItemId | `Meara.Roosters_FertEggVoid` | Requires item `Meara.Roosters_FertEggVoid` (likely `Meara` pack/mod). |
| FertilizedGoldEgg | `meara:fert-gold` | Meara Roosters: Fertilized Gold Egg | ItemId | `Meara.Roosters_FertEggGold` | Requires item `Meara.Roosters_FertEggGold` (likely `Meara` pack/mod). |
| RoosterEgg | `blaklyte:rooster-egg` | Blaklyte Roosters: Rooster Egg | ItemId | `Blaklyte.CP.Rooster_rooster_egg` | Requires item `Blaklyte.CP.Rooster_rooster_egg` (likely `Blaklyte` pack/mod). |


## Rooster providers catalog (defaults)

> If **no rooster providers** are available, RoosterControl will warn and disable provider-based rooster roles until a pack provides sources.

| ProviderId | DisplayName | RoosterEggItemId | FarmAnimal keys | Requirements |
|---|---|---|---|---|
| `meara` | Meara Roosters | — | `cockbrown`, `cockwhite`, `cockblue`, `cockvoid`, `cockgold` | Requires `Data/FarmAnimals` keys: `cockbrown`, `cockwhite`, `cockblue`, `cockvoid`, `cockgold` |
| `blaklyte` | Blaklyte Roosters | `Blaklyte.CP.Rooster_rooster_egg` | `Rooster`, `White Rooster`, `Blue Rooster`, `Desert Rooster`, `Trans Rooster`, `Cockatrice` | Requires `Data/FarmAnimals` keys: `Rooster`, `White Rooster`, `Blue Rooster`, `Desert Rooster`, `Trans Rooster`, `Cockatrice` |


## Scenario matrix (expected outcomes)

| Scenario | Expected `ra_compat status` | Expected `ra_compat validate` | Notes |
|---|---|---|---|
| No rooster packs installed | WARN: rooster sources catalog empty | WARN: provider deps missing / empty catalogs | Rooster-provider features disabled until providers exist. |
| Only Meara installed | Providers include `meara` | No WARN for `cock*` keys; egg item IDs present | Blue/void/gold + fertilized items come from Meara item IDs where selected. |
| Only Blaklyte installed | Providers include `blaklyte` | No WARN for Blaklyte farm animal keys; RoosterEgg item present | Rooster egg source available as `blaklyte:rooster-egg`. |
| Both installed | Providers include `meara`, `blaklyte` | No WARN for either provider | Union mode should include both; single-provider mode cycles deterministically. |

## Adding a new compat entry

1) Add sources/providers via a Content Patcher pack targeting:
   - `Mods/<RoosterControlId>/compat/egg_sources`
   - `Mods/<RoosterControlId>/compat/rooster_sources`
2) Keep `SourceId` stable and unique.
3) Prefer `FarmAnimalRef` when you want “whatever `Data/FarmAnimals` currently says”.
4) Prefer `ItemId` when you want a specific item (be explicit about the dependency).

