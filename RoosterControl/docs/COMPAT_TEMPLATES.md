# Compat templates (Content Patcher)

This doc provides **known-good templates** for extending RoosterControl via Content Patcher.

RoosterControl exposes two CP-patchable compat catalogs:

- **Egg sources**: `Mods/<RoosterControlId>/compat/egg_sources`
- **Rooster providers**: `Mods/<RoosterControlId>/compat/rooster_sources`

`<RoosterControlId>` is the mod’s `UniqueID` (see `RoosterControl/manifest.json`).

> Why `Load`?
> The catalogs are models which contain a `Sources: [...]` list keyed by `SourceId` (not `Id`), so `EditData` can’t reliably append entries. The supported extension point today is **replacing the catalog** with a `Load` patch, starting from the reference copies under `RoosterControl/data/compat/`.

## Quick start (recommended workflow)

1) Copy these reference files into your content pack:
   - `RoosterControl/data/compat/egg_sources.json`
   - `RoosterControl/data/compat/rooster_sources.json`

2) Edit them:
   - add new `Sources[]` entries (keep `SourceId` stable and unique);
   - keep `SchemaVersion` unchanged unless RoosterControl bumps it.

3) Use Content Patcher `Load` to replace the catalogs with your files.

## Example pack

See `examples/contentpacks/RoosterControl.ExampleCompatSources/` for a working pack with:

- `assets/egg_sources.json`
- `assets/rooster_sources.json`
- `content.json` which loads both catalogs.

## Schema notes

### EggSourcesCatalog

```json
{
  "SchemaVersion": 1,
  "Sources": [
    {
      "SourceId": "your.unique.id",
      "DisplayName": "Text shown in GMCM / logs",
      "EggKind": "BrownEgg | LargeBrownEgg | ... | RoosterEgg",
      "ItemId": "(O)174",
      "FarmAnimalRef": null
    }
  ]
}
```

Rules:
- Exactly one of `ItemId` or `FarmAnimalRef` should be set.
- `EggKind` must match `Application/Compat/EggKind`.
- `FarmAnimalRef` shape:

```json
{
  "FarmAnimalKey": "White Chicken",
  "List": "EggItemIds | ProduceItemIds | DeluxeProduceItemIds",
  "Index": 0
}
```

### RoosterSourcesCatalog

```json
{
  "SchemaVersion": 1,
  "Sources": [
    {
      "SourceId": "provider.id",
      "DisplayName": "Provider name",
      "RoosterEggItemId": "(O)174",
      "FarmAnimalKeys": [
        { "Key": "cockwhite", "BreedingRole": "Male", "EggLayerRole": "NoEggs" }
      ]
    }
  ]
}
```

Notes:
- `FarmAnimalKeys[].Key` must match keys in `Data/FarmAnimals`.
- `RoosterEggItemId` is optional; RoosterControl uses it as a **picker icon** hint in GMCM.

## Troubleshoot

- Run `ra_compat validate` for actionable warnings (missing keys, invalid selections).
- If a pack removes a selected entry, use:
  - GMCM reset toggles (arm + save), or
  - console: `ra_compat reset-all / reset-eggs all / reset-roosters`.
