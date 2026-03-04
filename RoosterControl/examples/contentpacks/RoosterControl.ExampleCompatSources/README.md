# RoosterControl - Example Compat Sources (Content Patcher)

This is a **known-good template pack** for overriding RoosterControl’s compat catalogs.

## What it changes

- `Mods/Mikew.roostersaddon.smapi/compat/egg_sources`
- `Mods/Mikew.roostersaddon.smapi/compat/rooster_sources`

It uses `Action: Load` (with `Priority: Low`), so another pack can intentionally override it.

## How to use

1) Copy this folder into `Stardew Valley/Mods/`.
2) Change `manifest.json` `UniqueID` + `Name` to your own.
3) Replace `assets/*.json` with your customized versions.

Recommended: start from RoosterControl’s reference copies:
- `RoosterControl/data/compat/egg_sources.json`
- `RoosterControl/data/compat/rooster_sources.json`

## Troubleshoot

- `ra_compat validate`
- `ra_compat status`
