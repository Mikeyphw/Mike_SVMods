# WildEggsHatching Refactor Plan

## 📋 CODE ANALYSIS (Based on Actual Code Review)

### Multipliers Found in Your Code

| Source | Multiplier | Purpose |
|--------|-----------|---------|
| `RandomChanceModifiers` | LargeMagicEgg=-0.13, Large=-0.05, Magic=-0.10 | Egg type penalty |
| `DailyLuckModifiers` | SuperUnlucky=-2, Unlucky=-1, Lucky=1, SuperLucky=1.5 | Daily luck influence |
| `BaseDailyLuckModifier` | 0.02 | Luck scaling |
| `DifficultyModifierMult` | Easy: +0.2/-0.25, Hard: +0.4/-0.25 | Difficulty scaling |
| `MineMultiplier` | 1.0 | Mine level modifier |
| `ProgressionStep` | 6 parts (PartI to PartVI) | Unlock progression |

### Classes to Refactor

| File | Lines | Problems |
|------|-------|----------|
| `Managers/ProgressionManager.cs` | 700+ | Does EVERYTHING |
| `Services/EggSpawning.cs` | 1100+ | Does EVERYTHING |
| `Managers/WildListManager.cs` | 300+ | Animal catalog only |
| `Framework/ModData.cs` | 200+ | State management |
| `Json/Configuration/SpawnConfiguration.cs` | 500+ | Scattered config |

---

## 🎯 REFACTOR RECOMMENDATIONS

### 1. Simplify Multipliers (Reduce from 6 sources to 3)

**CURRENT (Too Messy):**
```
RandomChanceModifiers + DailyLuckModifiers + DifficultyModifierMult + MineMultiplier + ProgressionStep + BaseDailyLuckModifier
```

**RECOMMENDED (3 Layers Only):**

```csharp
// Layer 1: Base Chance (from SpawnParameters)
float baseChance = 0.15f;

// Layer 2: Type Modifier (SIMPLIFIED)
float typeModifier = eggType switch
{
    EggType.largeMagicWildEgg => -0.13f,
    EggType.magicWildEgg => -0.10f,
    EggType.largeWildEgg => -0.05f,
    _ => 0f
};

// Layer 3: Luck Influence (SIMPLIFIED - optional, small)
float luckInfluence = dailyLuck switch
{
    LuckyTypes.SuperUnlucky => -0.02f,
    LuckyTypes.Unlucky => -0.01f,
    LuckyTypes.Lucky => 0.01f,
    LuckyTypes.SuperLucky => 0.02f,
    _ => 0f
};

float finalChance = baseChance + typeModifier + luckInfluence;
```

**REMOVE:**
- ❌ DifficultyModifierMult (overcomplicates)
- ❌ MineMultiplier (never used meaningfully)
- ❌ BaseDailyLuckModifier (redundant)
- ❌ All "modifier" naming

**KEEP:**
- ✅ Type-based adjustments (clear purpose)
- ✅ Small luck influence (optional, cosmetic)
- ✅ ProgressionStep (but separate from spawn math)

---

### 2. New Folder Structure

```
WildEggsHatching/
├── Core/
│   ├── Models/
│   │   ├── AnimalDefinition.cs
│   │   ├── SpawnParameters.cs
│   │   ├── SpawnContext.cs
│   │   ├── SpawnResult.cs
│   │   ├── PlayerWildState.cs
│   │   └── ProgressionState.cs
│   ├── Enums/
│   │   ├── ProgressionStep.cs
│   │   └── LuckyTypes.cs
│   └── Events/
│       ├── IGameEvent.cs
│       ├── EventBus.cs
│       ├── AnimalUnlockedEvent.cs
│       ├── PhaseUnlockedEvent.cs
│       ├── EggSpawnedEvent.cs
│       └── NoteSpawnedEvent.cs
├── Data/
│   ├── AnimalCatalog.cs
│   └── PhaseCatalog.cs
├── State/
│   └── PlayerStateRepository.cs
├── Progression/
│   ├── IPhaseRule.cs
│   ├── PhaseEvaluator.cs
│   └── PhaseProgressionService.cs
├── Spawning/
│   ├── SpawnChanceCalculator.cs
│   ├── WeightedTable.cs
│   ├── EggSpawnService.cs
│   └── EggPlacementService.cs
├── Tracking/
│   └── AnimalTrackingService.cs
├── Notes/
│   └── NoteService.cs
├── Coordination/
│   └── WildEggCoordinator.cs
├── Config/
│   └── ModConfig.cs
```

---

## 📝 DETAILED MIGRATION STEPS

### Phase 1: Core Models & State (Start Here)

**Create `Core/Models/PlayerWildState.cs`:**
```csharp
public sealed class PlayerWildState
{
    public HashSet<string> UnlockedAnimals { get; private set; } = new();
    public HashSet<string> DiscoveredAnimals { get; private set; } = new();
    public HashSet<string> CollectedNotes { get; private set; } = new();

    public bool IsUnlocked(string animalId) => UnlockedAnimals.Contains(animalId);

    public void Unlock(string animalId)
    {
        if (!UnlockedAnimals.Contains(animalId))
            UnlockedAnimals.Add(animalId);
    }
}
```

**Create `Core/Models/ProgressionState.cs`:**
```csharp
public sealed class ProgressionState
{
    public ProgressionStep CurrentStep { get; private set; } = ProgressionStep.PartI;

    public void AdvanceTo(ProgressionStep step)
    {
        if (step > CurrentStep)
            CurrentStep = step;
    }
}
```

**Create `State/PlayerStateRepository.cs`:**
```csharp
public sealed class PlayerStateRepository
{
    private const string SaveKey = "wildEggs-animalData";

    public PlayerWildState Load()
    {
        var data = ModEntry.HelperInstance.Data.ReadSaveData<ModData>(SaveKey);
        // Convert from old ModData format
        return new PlayerWildState
        {
            UnlockedAnimals = data?.HatchedAnimals
                .Select(a => a.FarmAnimalName)
                .ToHashSet() ?? new HashSet<string>()
        };
    }

    public void Save(PlayerWildState state)
    {
        // Convert to old format for compatibility
        var modData = new ModData();
        foreach (var animal in state.UnlockedAnimals)
        {
            modData.HatchedAnimals.Add(new ChosenAnimal { FarmAnimalName = animal });
        }
        ModEntry.HelperInstance.Data.WriteSaveData(SaveKey, modData);
    }
}
```

---

### Phase 2: Progression System (Replace ProgressionManager)

**Create `Core/Enums/ProgressionStep.cs`:**
```csharp
public enum ProgressionStep
{
    PartI,     // Always unlocked
    PartII,    // After SpecialOrderDoneI
    PartIII,   // After WizardStudy + 2 days
    PartIV,    // After SpecialOrderWildEggsII + 3 days
    PartV,     // After TheDesertScroll + 3 days
    PartVI,    // After TheDesertScroll + 5 days
}
```

**Create `Progression/PhaseProgressionService.cs`:**
```csharp
public sealed class PhaseProgressionService
{
    private readonly ProgressionState _state;
    private readonly EventBus _eventBus;

    public void EvaluateNextStep()
    {
        var nextStep = _state.CurrentStep + 1;
        if (nextStep > ProgressionStep.PartVI) return;

        if (IsStepUnlocked(nextStep))
        {
            _state.AdvanceTo(nextStep);
            _eventBus.Publish(new PhaseUnlockedEvent(nextStep));
        }
    }

    private bool IsStepUnlocked(ProgressionStep step)
    {
        return step switch
        {
            ProgressionStep.PartI => true,
            ProgressionStep.PartII => GameAssetManager.HasMail(Mail.SpecialOrderDoneI),
            ProgressionStep.PartIII => HasQuestWithDays(Quests.WizardStudy, 2),
            ProgressionStep.PartIV => HasQuestWithDays(Quests.SpecialOrderWildEggsII, 3),
            ProgressionStep.PartV => HasQuestWithDays(Quests.TheDesertScroll, 3),
            ProgressionStep.PartVI => HasQuestWithDays(Quests.TheDesertScroll, 5),
            _ => false
        };
    }

    private bool HasQuestWithDays(string questId, int daysRequired)
    {
        if (!GameAssetManager.HasCompletedQuest(questId)) return false;
        var completedDay = ModSaveData.QuestCompletedDay[questId];
        return Game1.Date.TotalDays >= completedDay + daysRequired;
    }
}
```

**Delete `Managers/ProgressionManager.cs`** (after migration complete)

---

### Phase 3: Spawn System (Replace EggSpawning)

**Create `Spawning/SpawnChanceCalculator.cs`:**
```csharp
public sealed class SpawnChanceCalculator
{
    // SIMPLIFIED: Only 3 layers of modifiers
    public float Calculate(float baseChance, EggType eggType, LuckyTypes dailyLuck)
    {
        // Layer 1: Type modifier (clear intent)
        float typeModifier = eggType switch
        {
            EggType.largeMagicWildEgg => -0.13f,
            EggType.magicWildEgg => -0.10f,
            EggType.largeWildEgg => -0.05f,
            _ => 0f
        };

        // Layer 2: Small luck influence (optional, cosmetic)
        float luckInfluence = dailyLuck switch
        {
            LuckyTypes.SuperUnlucky => -0.02f,
            LuckyTypes.Unlucky => -0.01f,
            LuckyTypes.Lucky => 0.01f,
            LuckyTypes.SuperLucky => 0.02f,
            _ => 0f
        };

        return Math.Clamp(baseChance + typeModifier + luckInfluence, 0f, 1f);
    }
}
```

**Create `Spawning/EggSpawnService.cs`:**
```csharp
public sealed class EggSpawnService
{
    private readonly SpawnChanceCalculator _chanceCalculator;

    public SpawnResult TrySpawn(SpawnContext context)
    {
        // 1. Calculate chance (SIMPLIFIED)
        float chance = _chanceCalculator.Calculate(
            context.BaseChance,
            context.EggType,
            context.DailyLuck
        );

        // 2. Roll for spawn
        if (Game1.random.NextDouble() >= chance)
            return SpawnResult.None();

        // 3. Select animal (weighted)
        var animal = SelectEligibleAnimal(context);

        return SpawnResult.Success(animal, context.Tile);
    }

    private string SelectEligibleAnimal(SpawnContext context)
    {
        // Use weighted table for clean selection
        var table = new WeightedTable<string>(context.EligibleAnimals);
        return table.Roll(Game1.random);
    }
}
```

**Delete `Services/EggSpawning.cs`** (after migration complete)

---

### Phase 4: Event Bus (For Decoupling)

**Create `Core/Events/EventBus.cs`:**
```csharp
public sealed class EventBus
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();

    public void Subscribe<T>(Action<T> handler) where T : IGameEvent
    {
        var type = typeof(T);
        if (!_handlers.ContainsKey(type))
            _handlers[type] = new List<Delegate>();
        _handlers[type].Add(handler);
    }

    public void Publish<T>(T event) where T : IGameEvent
    {
        var type = typeof(T);
        if (_handlers.TryGetValue(type, out var handlers))
        {
            foreach (var handler in handlers.Cast<Action<T>>())
                handler(event);
        }
    }
}
```

**Events to create:**
- `AnimalUnlockedEvent(string animalId)`
- `PhaseUnlockedEvent(ProgressionStep step)`
- `EggSpawnedEvent(string animalId, Vector2 tile)`
- `NoteSpawnedEvent(string animalId)`

---

## ❌ WHAT TO DELETE (After Migration)

| File | Reason |
|------|--------|
| `Managers/ProgressionManager.cs` | Replaced by PhaseProgressionService |
| `Services/EggSpawning.cs` | Replaced by EggSpawnService + EggPlacementService |
| `Managers/WildListManager.cs` | Replaced by AnimalCatalog |
| `Json/Configuration/SpawnConfiguration.cs` | Replaced by simple SpawnParameters |
| `Framework/ModData.cs` | Replaced by PlayerWildState |

---

## ⚠️ IMPORTANT NOTES

1. **Keep DesertTomb Separate** - Don't merge DesertTomb code into the new structure yet.

2. **Mine Level Modifier** - Your `MineMultiplier` is set to 1.0 and never used. Consider removing entirely or implementing properly.

3. **Daily Luck** - Currently adds complexity. Keep only if you want cosmetic variation.

4. **Progression Steps** - Your 6-step progression is fine, just extract it from ProgressionManager.

5. **JSON Loading** - Keep JsonLoader for animal definitions, but don't mix with spawning logic.

---

## 🚀 RECOMMENDED IMPLEMENTATION ORDER

1. Create Core/Models (PlayerWildState, ProgressionState, SpawnContext)
2. Create State/PlayerStateRepository
3. Create Progression/PhaseProgressionService
4. Create EventBus
5. Create Spawning/SpawnChanceCalculator
6. Create Spawning/EggSpawnService
7. Create Coordination/WildEggCoordinator
8. Delete old managers (one at a time, test after each)

