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
- [ ] Add a tiny consumer mod example (outside NestCore) wired to a real `assets/views/*.sml` view.
- [ ] Consider a SMAPI main-thread scheduler helper for async viewmodels that update UI-bound properties.
