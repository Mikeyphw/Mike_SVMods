# TODO — RoosterAddon

> Location (required): `Mike_SVMods/RoosterAddon/docs/TODO.md`  
> Last updated: **2026-02-19T04:18:29Z**  
> Rule: only remaining work lives here. Remove items as they are done.

## Now
- Create repo-root/Core `docs/PLAN.md`, `docs/CHECKLIST.md`, `docs/TODO.md` for Phase 10 (CI) and keep them in sync with RoosterAddon docs.
- Create `Mike_SVMods/Tests/docs/{PLAN,CHECKLIST,TODO}.md` for Phase 7 and verify current Tests project layout.
- Decide CI strategy (pure tests only vs self-hosted runner for SMAPI/game-dependent builds).

## Next
- Implement Phase 10.1–10.3 (workflow + pure tests + guard tests) in Core/Tests zips and mark Phase 10 done across docs.
- Add `ra_catalog` command coverage tests (diff/export/reload) under Tests.

## Later
- Add optional self-hosted CI job for game/SMAPI integration tests.
- Release packaging hardening (version stamping, artifact checks).

---

## By phase (optional)
### Phase 7
- [ ] 7.1 Create Tests docs + project layout — tracked in `Mike_SVMods/Tests/docs/`
- [ ] 7.2 Add unit tests for pure logic — tracked in `Mike_SVMods/Tests/docs/`
- [ ] 7.3 Wire tests into CI — tracked in `Mike_SVMods/Tests/docs/`

### Phase 10
- [ ] 10.1 Add GitHub Actions build gate — tracked in repo-root `docs/`
- [ ] 10.2 Run pure tests in CI — tracked in repo-root `docs/`
- [ ] 10.3 Add guard tests — tracked in repo-root `docs/`
