# WildEggsHatching Build Error Fix Plan (Diff-Like)

## Scope and constraints
- Compile surface restored in `WildEggsHatching/WildEggsHatching.csproj` to:
  - `Application/**/*.cs`
  - `Core/**/*.cs`
  - `Infrastructure/**/*.cs`
- Keep existing config/API decisions:
  - `BirthText` remains present and wired.
  - `DinoEggsExclusiveHatch` default remains `true`.
- This document is a **plan only**. No code fixes below are applied in this pass.

## Build snapshot
- Command used:
  - `dotnet build WildEggsHatching/WildEggsHatching.csproj -v minimal -p:EnableModDeploy=false -p:EnableModZip=false`
- Result:
  - `Build FAILED`
  - `94 Error(s)`
- Dominant error families:
  - `CS1739` named-argument mismatch on `MonsterSpawnStats` (42)
  - `CS0103` missing symbol/method (30)
  - `CS1061` missing method/member on SDV/abstraction types (28)
  - `CS7036` missing required argument after signature drift (14)

## Execution order (recommended)
1. DesertTomb model/signature mismatches (collapses most cascades).
2. SMAPI/SDV API drift adapters (tile/placeable/location helpers).
3. AnimalNotes missing helper methods + record schema alignment.
4. Verifier namespace/typing cleanup.
5. Shop progression and LMH clone integration cleanup.
6. Content editor type drift (`MachinesEditor`).
7. Final pass on remaining edge errors (`ThePharaoh`, factory typing).

---

## 1) DesertTomb: `MonsterSpawnStats` named args and planner signatures

### 1.1 `SpawnConfiguration` named args must match record parameter names
File: `WildEggsHatching/Core/DesertTomb/Configuration/SpawnConfiguration.cs`

```diff
- new MonsterSpawnStats(weight: 3, minPerArea: 0, maxPerArea: 3, packMin: 1, packMax: 1, difficultyAdd: 1, speedAdd: 0)
+ new MonsterSpawnStats(Weight: 3, MinPerArea: 0, MaxPerArea: 3, PackMin: 1, PackMax: 1, DifficultyAdd: 1, SpeedAdd: 0)
```

Apply the same rename pattern to all `MonsterSpawnStats(...)` initializers in this file.

### 1.2 `TombSpawnPlanner.AddWithCaps` requires `role`
File: `WildEggsHatching/Core/DesertTomb/Spawning/TombSpawnPlanner.cs`

```diff
- AddWithCaps(..., stats: statsByType[guardType]);
+ AddWithCaps(..., stats: statsByType[guardType], role: TombMonsterRole.Guard);

- AddWithCaps(..., statsByType[wanderTypes[0]]);
+ AddWithCaps(..., statsByType[wanderTypes[0]], TombMonsterRole.Wander);
```

Also ensure every call site in this file passes an explicit role.

### 1.3 `DesertTombRuntimePolicy` signature drift and removed helpers
File: `WildEggsHatching/Application/DesertTomb/Runtime/DesertTombRuntime.cs`

```diff
- DesertTombRuntimePolicy.IsEligibleByProgression(cfg.ProgressionMode, step)
+ DesertTombRuntimePolicy.IsEligibleByProgression(cfg.ProgressionMode, cfg.DesertTombModuleEnabled, step)

- if (!RollTombChance(cfg.TombChance, mineShaft.mineLevel))
+ if (!DesertTombRuntimePolicy.RollTombChance(cfg.TombChance, mineShaft.mineLevel))

- DesertTombRuntimePolicy.IsTombCandidate(mineShaft.mineLevel, mineShaft.mapPath.Value)
+ DesertTombRuntimePolicy.IsTombCandidate(mineShaft.mapPath.Value)
```

If `RollTombChance` is intentionally local, reintroduce it as a private wrapper calling policy.

### 1.4 Logging API mismatch (`ILog` has no `Info/Warn/Error` members)
Files:
- `WildEggsHatching/Application/DesertTomb/Runtime/DesertTombRuntime.cs`
- `WildEggsHatching/Application/DesertTomb/Spawning/TombPopulationManager.cs`

```diff
- _log.Info("...")
+ _log.Log("...", StardewModdingAPI.LogLevel.Info)

- _log.Warn("...")
+ _log.Log("...", StardewModdingAPI.LogLevel.Warn)

- _log.Error("...")
+ _log.Log("...", StardewModdingAPI.LogLevel.Error)
```

### 1.5 `TombSpawnPlan.TotalPlanned` no longer exists
Files:
- `WildEggsHatching/Application/DesertTomb/Runtime/DesertTombRuntime.cs`
- `WildEggsHatching/Infrastructure/DesertTomb/Diagnostics/DesertTombDiagnostics.cs`

```diff
- var total = plan.TotalPlanned;
+ var total = plan.Areas.Sum(a => a.Spawns.Sum(s => s.Count));
```

(Adjust property names to actual `TombSpawnPlan` model members.)

---

## 2) SDV/SMAPI API drift in game adapters

### 2.1 `GameStateReader` type mismatches
File: `WildEggsHatching/Infrastructure/Game/GameStateReader.cs`

```diff
- public Rectangle GetViewport() => Game1.viewport;
+ public Rectangle GetViewport() => new(Game1.viewport.X, Game1.viewport.Y, Game1.viewport.Width, Game1.viewport.Height);

- public long GetSaveUniqueId() => Game1.uniqueIDForThisGame;
+ public long GetSaveUniqueId() => unchecked((long)Game1.uniqueIDForThisGame);
```

### 2.2 `WorldLocations.GetAll()` return type covariance
File: `WildEggsHatching/Infrastructure/Game/WorldLocations.cs`

```diff
- public IReadOnlyList<GameLocation> GetAll() => Game1.locations;
+ public IReadOnlyList<GameLocation> GetAll() => Game1.locations.ToList();
```

### 2.3 Tile/placeable checks removed/renamed in SDV API
Files:
- `WildEggsHatching/Infrastructure/Game/GameObjectPlacer.cs`
- `WildEggsHatching/Infrastructure/Game/TileRules/GameTileQuery.cs`
- `WildEggsHatching/Application/DesertTomb/Spawning/TombSpawnTileCache.cs`

```diff
- location.isTileLocationTotallyClearAndPlaceable(tile)
+ location.isTileLocationTotallyClearAndPlaceableIgnoreFloors(tile)
# or equivalent modern helper used elsewhere in repo
```

If no direct modern equivalent exists, centralize a replacement via `GameLocationExtensions` adapter and call it from all these files.

### 2.4 Object ctor signature mismatch
File: `WildEggsHatching/Infrastructure/Game/GameObjectPlacer.cs`

```diff
- new StardewValley.Object(tile, parentSheetIndex, 1)
+ new StardewValley.Object(itemId: parentSheetIndex.ToString(), stack: 1)
+ { TileLocation = tile, Location = location }
```

(Or use canonical object factory helper if present in codebase.)

### 2.5 Deprecated tile getters on entities
Files:
- `WildEggsHatching/Application/DesertTomb/Spawning/TombRoleEnforcer.cs`
- `WildEggsHatching/Application/DesertTomb/Spawning/TombPopulationManager.cs`
- `WildEggsHatching/Application/DesertTomb/Spawning/DesertTombSpawnExecutor.cs`
- `WildEggsHatching/Infrastructure/DesertTomb/Diagnostics/PharaohTrackerOverlay.cs`

```diff
- actor.getTileLocationPoint()
+ actor.TilePoint

- pharaoh.getTileLocation()
+ pharaoh.Tile
```

---

## 3) AnimalNotes compile completion

### 3.1 Missing helper methods in orchestrator
File: `WildEggsHatching/Application/AnimalNotes/AnimalNotesOrchestrator.cs`

Add missing private methods referenced by current flow:
```diff
+ private static string? GetNoteBlockedReason(...)
+ private static string? GetQuestBlockedReason(...)
+ private static string? GetMailBlockedReason(...)
+ private static List<string> BuildCleanupBlockers(...)
+ private static void ResetPlacementFailures(HatchedAnimalRecord p)
+ private static void RecordPlacementFailure(HatchedAnimalRecord p, int day, string reason)
+ private bool TryFindPlacementTile(GameLocation home, out Vector2 tile, out string reason)
```

Implement from legacy behavior in `WildEggsHatching_orig` with current model names.

### 3.2 `HatchedAnimalRecord` property mismatch
Files:
- `WildEggsHatching/Application/AnimalNotes/AnimalNotesOrchestrator.cs`
- `WildEggsHatching/Application/AnimalNotes/HatchedAnimalRecord.cs`

```diff
- p.QuestGiven / p.DayQuestGiven usage with missing properties
+ either add these properties back to record
+ or migrate callsites to FarmerState-based fields only
```

### 3.3 `AnimalHouse` namespace drift
File: `WildEggsHatching/Application/AnimalNotes/AnimalNotesClientObserver.cs`

```diff
- StardewValley.Locations.AnimalHouse
+ StardewValley.Buildings.AnimalHouse
# (or current actual namespace in target SDV API)
```

---

## 4) `WeggVerifier` cleanup (namespaces, typing, and structural corruption)

File: `WildEggsHatching/Infrastructure/Diagnostics/WeggVerifier.cs`

### 4.1 Ambiguous `SaveKeys`
```diff
- using WildEggsHatching.Application.AnimalNotes;
- using WildEggsHatching.Application.Progression;
+ using AnimalNotesSaveKeys = WildEggsHatching.Application.AnimalNotes.SaveKeys;
+ using ProgressionSaveKeys = WildEggsHatching.Application.Progression.SaveKeys;

- SaveKeys.Progression
+ ProgressionSaveKeys.Progression
```

### 4.2 Wrong `Strings` namespace references
```diff
- Infrastructure.Content.Strings.AssetNames.Objects
+ Mikew.BirdsOfAFeather.EnumsAndStrings.Strings.AssetNames.Objects
```

Repeat for `Quests`, `Mail`, etc.

### 4.3 Tuple type mismatch (`IList<string>` vs `IReadOnlyList<string>`)
```diff
- maleAltRefs.Add((..., e.AnimalIds));
+ maleAltRefs.Add((..., (IReadOnlyList<string>)e.AnimalIds.ToList()));
```

### 4.4 `NetStringHashSet` coalescing mismatch
```diff
- var mail = Game1.player?.mailReceived ?? new HashSet<string>(...);
+ var mail = Game1.player?.mailReceived is null
+     ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
+     : new HashSet<string>(Game1.player.mailReceived, StringComparer.OrdinalIgnoreCase);
```

### 4.5 Structural brace corruption around clone-probe block
- There is an out-of-place `try/catch`/brace segment after `AddFarmAnimalCloneLeakChecks`.
- Rebuild this method boundary from original implementation and re-run formatter.

---

## 5) Shop progression / LMH clone integration

### 5.1 Missing `ShopFieldsAccessor`
File: `WildEggsHatching/Core/ShopProgression/LetMeHatchCloneShopEdits.cs`

```diff
- ShopFieldsAccessor.GetUnlockCondition(...)
+ FarmAnimalShopFieldAccessors.GetUnlockCondition(...)
# or restore/create ShopFieldsAccessor wrapper in this namespace
```

### 5.2 Local variable shadowing (`newCond`)
File: `WildEggsHatching/Core/ShopProgression/LetMeHatchCloneShopEdits.cs`

```diff
- var newCond = ...
- ...
- var newCond = ...
+ var mergedCond = ...
+ ...
+ var splitEntryCond = ...
```

### 5.3 Dictionary interface mismatch
File: `WildEggsHatching/Core/ShopProgression/FarmAnimalShopProgressionEdits.cs`

```diff
- method(..., IDictionary<string, FarmAnimalData> farmAnimals)
+ method(..., IReadOnlyDictionary<string, FarmAnimalData> farmAnimals)
# or pass `new ReadOnlyDictionary<string, FarmAnimalData>(farmAnimals)` at callsite
```

---

## 6) Content editors type updates

### 6.1 Missing machine data types
File: `WildEggsHatching/Infrastructure/Content/Editors/MachinesEditor.cs`

`QuantityModifier` and `TemporaryAnimatedSpriteDefinition` are likely renamed in current SDV game data model.

```diff
- ReadyTimeModifiers = new List<QuantityModifier> { ... }
+ ReadyTimeModifiers = new List<MachineQuantityModifier> { ... }

- TemporarySprites = new List<TemporaryAnimatedSpriteDefinition> { ... }
+ TemporarySprites = new List<MachineTemporarySpriteData> { ... }
```

Use exact current types from `StardewValley.GameData.Machines` in this target API.

---

## 7) Animals / API integration specifics

### 7.1 `ILetMeHatchApi` missing symbol and downstream typing errors
File: `WildEggsHatching/Application/Animals/ChosenEggAssignments.cs`

```diff
- ILetMeHatchApi lmh = ...
+ var lmh = _registry.GetApi<object>("...")
+ // then pattern-match to available interface/wrapper
```

or add the missing interface import/reference if it exists in repo.

### 7.2 `uint` vs `int`, method-group compare, deconstruction inference
- Normalize numeric conversions with explicit casts where needed.
- Ensure method invocation includes `()` before integer compare.
- Replace ambiguous deconstruction with explicit tuple type.

---

## 8) ThePharaoh and monster factory edge fixes

### 8.1 `ThePharaoh` numeric conversion and `Game1.multiplayer` access
File: `WildEggsHatching/Infrastructure/DesertTomb/Monsters/Creatures/ThePharaoh.cs`

```diff
- targetMummy.Health = Math.Min(..., ... + (_enraged.Value ? 180 : 80));
+ targetMummy.Health = (int)Math.Min(...);

- Game1.multiplayer.broadcastSprites(...)
+ Game1.Multiplayer.broadcastSprites(...)
# or use current public helper to broadcast sprites in SDV 1.6 API
```

### 8.2 `DesertTombMonsterFactory` switch type unification
File: `WildEggsHatching/Infrastructure/DesertTomb/Monsters/DesertTombMonsterFactory.cs`

```diff
- var monster = baseType switch { ... };
+ Monster monster = baseType switch { ... };
```

If still failing, one branch returns non-`Monster`; adjust constructor target or cast branch output.

---

## 9) Tracking checklist for implementation pass

- [ ] Phase A: DesertTomb configuration/planner/runtime signatures compile.
- [ ] Phase B: SDV adapters compile (`GameStateReader`, `GameObjectPlacer`, tile queries, entity tile getters).
- [ ] Phase C: AnimalNotes helper completeness + record schema alignment compile.
- [ ] Phase D: `WeggVerifier` namespace/typing/structure compile.
- [ ] Phase E: Shop progression + LMH clone APIs compile.
- [ ] Phase F: Content editor type migrations compile.
- [ ] Phase G: `ThePharaoh` + factory edge errors compile.
- [ ] Final: `dotnet build ... -p:EnableModDeploy=false -p:EnableModZip=false` returns `0 Error(s)`.

## Notes for next pass
- Keep `BirthText` option and API runtime feed intact while applying fixes.
- Keep `DinoEggsExclusiveHatch` default `true`.
- Re-enable deploy/zip only after compile errors are eliminated.
