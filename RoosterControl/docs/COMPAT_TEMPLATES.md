# Compat templates & content pack examples

This doc provides **ready-to-copy templates** for RoosterControl’s CP‑patchable compat catalogs, plus a **known‑good Content Patcher pack** you can use as a starting point.

RoosterControl exposes two JSON assets which **Content Patcher can `Load`**:

- Egg sources: `Mods/<RoosterControlId>/compat/egg_sources`
- Rooster providers: `Mods/<RoosterControlId>/compat/rooster_sources`

For this repo build, `<RoosterControlId>` is:

- `Mikew.roostersaddon.smapi`

## Quick start (recommended)

1) Copy the example content pack under:

- `examples/contentpacks/RoosterControl.ExampleCompatSources/`

2) Edit the JSON under `assets/` to add your sources/providers.

3) Run in-game:

- `ra_compat validate`
- `ra_compat status`

## Schema notes

These assets deserialize into:

- `Application/Compat/CompatCatalogModels.cs`

### EggSourcesCatalog

```json
{
  "SchemaVersion": 1,
  "Sources": [
    {
      "SourceId": "string",
      "DisplayName": "string",
      "EggKind": "BrownEgg | LargeBrownEgg | ... | RoosterEgg",
      "ItemId": "Optional string",
      "FarmAnimalRef": {
        "FarmAnimalKey": "Data/FarmAnimals key",
        "List": "EggItemIds | ProduceItemIds | DeluxeProduceItemIds",
        "Index": 0
      }
    }
  ]
}
```

Rules:
- Exactly **one** of `ItemId` **or** `FarmAnimalRef` should be set.
- `SourceId` must be stable and unique across the list.
- `EggKind` is the logical role (what the UI selects), not necessarily the vanilla item.

### RoosterSourcesCatalog

```json
{
  "SchemaVersion": 1,
  "Sources": [
    {
      "SourceId": "string",
      "DisplayName": "string",
      "RoosterEggItemId": "Optional string (used for GMCM icons)",
      "FarmAnimalKeys": [
        {
          "Key": "Data/FarmAnimals key",
          "BreedingRole": "Male | Female | Both | None",
          "EggLayerRole": "InheritFromFarmAnimalData | LaysEggs | NoEggs"
        }
      ]
    }
  ]
}
```

Rules:
- `FarmAnimalKeys[].Key` should exist in `Data/FarmAnimals` after all packs load.
- Missing keys are reported by `ra_compat validate`.

## Content Patcher example

The included example pack uses `Load` for both catalogs:

- `Action: Load`
- `Target: Mods/Mikew.roostersaddon.smapi/compat/egg_sources`
- `Target: Mods/Mikew.roostersaddon.smapi/compat/rooster_sources`

This replaces the full catalog content for that asset. Start from RoosterControl’s reference copies:

- `data/compat/egg_sources.json`
- `data/compat/rooster_sources.json`
