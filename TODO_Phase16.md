# Phase 16 A-D Progress

## Phase 16A — NestCore GMCM batch helpers
- [ ] 16A.1: Add Links() overloads to GmcmPageBuilder.cs
- [ ] 16A.2: Add Pages() overloads to GmcmMenuLayout.cs
- [ ] 16A.3: Update GMCM_BUILDER.md
- [ ] 16A.4: Update NestCore docs/PLAN.md
- [ ] 16A.5: Update NestCore docs/CHECKLIST.md
- [ ] 16A.6: Update NestCore docs/TODO.md
- [ ] 16A.7: Build gate
- [ ] 16A.8: Commit

## Phase 16B — Extract RoosterControl static GMCM pages
- [ ] 16B.1: Create RoosterRootPage.cs
- [ ] 16B.2: Create RoosterProbabilitiesPage.cs
- [ ] 16B.3: Create RoosterDebugPage.cs
- [ ] 16B.4: Add using to RoosterCoordinator.Gmcm.cs
- [ ] 16B.5: Replace inline root page with delegate
- [ ] 16B.6: Replace inline probabilities page with delegate
- [ ] 16B.7: Replace inline debug page with delegate
- [ ] 16B.8: Keep compat/females/roosters unchanged
- [ ] 16B.9: Remove moved inline bodies
- [ ] 16B.10: Update RoosterControl docs/PLAN.md
- [ ] 16B.11: Update RoosterControl docs/CHECKLIST.md
- [ ] 16B.12: Update RoosterControl docs/TODO.md
- [ ] 16B.13: Build gate
- [ ] 16B.14: Commit

## Phase 16C — Extract rooster-type GMCM pages
- [ ] 16C.1: Add I18n accessor to RoosterCoordinator.cs
- [ ] 16C.2: Create RoosterTypePages.cs shell
- [ ] 16C.3: Move helper methods
- [ ] 16C.4: Update method signatures
- [ ] 16C.5: Replace Config/_i18n with owner.Config/owner.I18n
- [ ] 16C.6: Add using to RoosterCoordinator.Gmcm.cs
- [ ] 16C.7: Replace AddRoosterPages with RoosterTypePages.Define
- [ ] 16C.8: Delete moved methods from coordinator
- [ ] 16C.9: Update RoosterRootPage to use RoosterTypePages.PageIds
- [ ] 16C.10: Update docs
- [ ] 16C.11: Build gate
- [ ] 16C.12: Commit

## Phase 16D — Add RoosterControl GMCM orchestrator
- [ ] 16D.1: Create RoosterControlGmcmMenu.cs
- [ ] 16D.2: Add using to RoosterCoordinator.Gmcm.cs
- [ ] 16D.3: Add _gmcmMenu field
- [ ] 16D.4: Instantiate orchestrator in OnGameLaunched
- [ ] 16D.5: Replace initial RebuildLayout define
- [ ] 16D.6: Replace RequestGmcmRebuild define
- [ ] 16D.7: Handle DefineGmcmLayout wrapper
- [ ] 16D.8: Update docs
- [ ] 16D.9: Build gate
- [ ] 16D.10: Commit

