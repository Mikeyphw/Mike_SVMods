# TODO — NestCore

- [x] Ensure GMCM option IDs are stable and documented.
- [ ] Verify all debug/diagnostic/logging toggles are only on the `debug` page.
- [ ] Add/verify i18n keys for all pages/options (`config.*`).
- [x] Add docs/examples for dynamic dictionary editors with default-vs-override removal.
- [ ] Add optional unit tests for ID normalization if time allows.

- [x] Add GMCM Options section to GMCM_BUILDER.md (Color/Image/Separator/DynamicParagraph).

- [x] Add OverrideDictionaries helper for default+overrides configs.

- [x] (Optional) Add `ObservableObject` (INotifyPropertyChanged) helper for viewmodels.
- [ ] Verify `NestCore` builds (net6.0) locally/CI after adding `ObservableObject`.

- [x] Add StardewUI consumer doc (`docs/stardewui.md`).
- [x] Add `RelayCommand` helper for UI viewmodels.

- [ ] Decide whether to expand `StardewUI.Framework.IMenuController` vendored surface (currently minimal).

- [x] Phase 8: Add `GmcmPageBuilder<TConfig>.Links(...)` overloads for batched page-link registration.
- [x] Phase 8: Add `GmcmMenuLayout<TConfig>.Pages(...)` overloads for batched page registration.
- [x] Phase 8: Document batched GMCM page/link helpers in `GMCM_BUILDER.md`.

- [x] Phase 18A: add `GmcmPageBuilder<TConfig>.Action(...)` for one-shot commands (no fake bool toggle pattern).
- [x] Phase 18A: document `Action(...)` in `GMCM_BUILDER.md` with simple + reset/remove/apply examples.
- [x] Phase 18A: run full repo build gate after NestCore action integration.

- [x] Phase 18D: establish repo GMCM convention rules in `GMCM_BUILDER.md`.
- [x] Phase 18D: align NestCore/LetMeHatch/EggsInWild docs to the shared GMCM convention.
- [x] Phase 18D: run full repo build gate after docs alignment.

- [x] Phase 18E: final grep/review for fake-bool action patterns and inline GMCM bodies.
- [x] Phase 18E: final build gate and doc finalization for repo-wide GMCM standardization.

- [ ] Add a tiny consumer mod example (outside NestCore) wired to a real `assets/views/*.sml` view.
- [ ] Consider a SMAPI main-thread scheduler helper for async viewmodels that update UI-bound properties.
