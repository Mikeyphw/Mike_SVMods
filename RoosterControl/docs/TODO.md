# RoosterControl TODO

## Next: Phase 11D (tests/regression safety)
- [ ] Add unit tests in `RoosterControl.Tests` for:
  - synthesized fertilized ID scheme
  - native vs synthesized selection behavior
  - `Data/Objects` injection results (golden snapshot style)

## After: Phase 12 (GMCM UI)
- [ ] Add compat selection UI + validation messaging.

## Done (Phase 11C)
- [x] Prefer native fertilized eggs if provided by the active egg source.
- [x] Otherwise synthesize fertilized variants into `Data/Objects` (deterministic IDs).
- [x] Ensure fertilized-only hatching uses resolved fertilized IDs everywhere.
