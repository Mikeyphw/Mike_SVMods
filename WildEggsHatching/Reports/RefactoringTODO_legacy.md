# Refactoring TODO

## Phase 4: Progression - COMPLETING

### Current Issues Found:
1. ✅ ~~PhaseProgressionService uses `Quests.WizardStudy` etc. - these constants don't exist in refactored code~~
2. ✅ ~~IGameMailService uses `int` for quest IDs but custom quests use string IDs~~
3. ✅ ~~GetQuestCompletionDay returns null - needs tracking in PlayerWildState~~
4. ✅ ~~UnlockRuleEvaluator has wrong quest ID strings~~ (FIXED)

### Plan:
- [x] Add MailIds and QuestIds constants to WildEggConstants
- [x] Refactor MailEditor to use WildEggConstants
- [x] Refactor QuestEditor to use WildEggConstants  
- [x] Refactor SpecialOrdersEditor to use WildEggConstants
- [x] Refactor BigCraftablesEditor to use WildEggConstants
- [x] ✅ Update PhaseProgressionService to use string quest IDs (was already done)
- [x] ✅ Add QuestCompletionDays tracking to PlayerWildState (was already done)
- [x] ✅ Update IGameMailService to support string quest methods (was already done)
- [x] Fix UnlockRuleEvaluator quest ID strings to match constants (COMPLETED)
- [x] Update TODOrefactor_PLAN.md with completion

## Phase 5: Asset/Content - COMPLETED

Files to complete:
- [x] SmapiContentEditor - GameAssetManager logic ported to refactor editors/pipeline.
