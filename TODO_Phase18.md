# Phase 18 GMCM Standardization - Implementation TODO

## Phase 18A: Add NestCore GMCM Action Option
- [ ] 1. Inspect NestCore GmcmPageBuilder and identify where to add Action method
- [ ] 2. Add `Action(...)` method to GmcmPageBuilder
- [ ] 3. Implement backing option registration (complex option or custom handler)
- [ ] 4. Update GMCM_BUILDER.md with Action documentation
- [ ] 5. Update NestCore docs (PLAN.md, CHECKLIST.md, TODO.md)
- [ ] 6. Run build gate
- [ ] 7. Create commit: "phase-18a: add NestCore GMCM action option"

## Phase 18B: Standardize LetMeHatch GMCM DSL
- [ ] 1. Inspect LetMeHatch/Integrations/Gmcm/LmhGmcmMenu.cs
- [ ] 2. Classify options (bound/unbound/one-shot)
- [ ] 3. Convert direct config members to bound DSL
- [ ] 4. Update docs
- [ ] 5. Run build gate
- [ ] 6. Create commit: "phase-18b: standardize LetMeHatch GMCM DSL usage"

## Phase 18C: Complete EggsInWild GMCM Migration
- [ ] 1. Inspect EggsInWild/ModEntry.Gmcm.Layout.cs and compare with EiwGmcmMenu.cs
- [ ] 2. Identify duplicate code (BuildAnimalProgressionPage is duplicated)
- [ ] 3. Remove duplicates from ModEntry.Gmcm.Layout.cs
- [ ] 4. Replace pseudo-button bools with Action (if any exist)
- [ ] 5. Update docs
- [ ] 6. Run build gate
- [ ] 7. Create commit: "phase-18c: complete EggsInWild GMCM NestCore migration"

## Phase 18D: Align Repo GMCM Conventions
- [ ] 1. Update NestCore/GMCM_BUILDER.md with repo convention section
- [ ] 2. Update all relevant docs
- [ ] 3. Run build gate
- [ ] 4. Create commit: "phase-18d: align repo GMCM conventions and docs"

## Phase 18E: Finalize
- [ ] 1. Final grep for pseudo-button patterns
- [ ] 2. Final review for inline GMCM bodies
- [ ] 3. Update final docs
- [ ] 4. Run build gate
- [ ] 5. Create commit: "phase-18e: finalize repo-wide GMCM standardization"

