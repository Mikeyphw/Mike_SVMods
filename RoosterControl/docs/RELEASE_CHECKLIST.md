# RoosterControl Release Checklist

Use this checklist when preparing a tagged release.

## Build + tests
- [ ] Run `RoosterControl/scripts/verify.sh` (Linux) or equivalent:
  - restore
  - build solution
  - run unit tests
- [ ] Confirm no new warnings that could hide real issues.

## Runtime sanity checks (manual)
- [ ] Launch into an existing save with a coop and confirm:
  - GMCM loads and pages render without errors.
  - Compat Sources page shows a summary and at least one valid entry per egg kind when catalogs exist.
  - Cycling/selecting a source updates immediately and persists across reload.
- [ ] Verify rooster provider selection:
  - enabling/disabling providers updates coop rooster scan overrides correctly
  - empty enabled-provider list clears overrides (no stale behavior)

## Compat / logging
- [ ] Run `ra_compat status` and confirm it reports expected catalog sizes and selections.
- [ ] If using catalogs from other mods/packs, run `ra_compat validate` and confirm any errors are actionable.

## Localization
- [ ] Confirm new/changed GMCM strings exist in:
  - `i18n/default.json`
  - `i18n/pt-br.json` (as applicable)

## Packaging
- [ ] Update `manifest.json` version + minimum SMAPI requirements if needed.
- [ ] Ensure `assets/` + `data/compat/` reference copies are included.
- [ ] Zip the mod folder contents (no build artifacts).

## Release artifacts
- [ ] Write release notes (high-level changes + any upgrade notes).
- [ ] Attach the mod zip and publish.
