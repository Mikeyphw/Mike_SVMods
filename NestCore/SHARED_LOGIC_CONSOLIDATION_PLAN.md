# Shared Logic Consolidation Plan
## Centralizing Common Code in NestCore

### Current State Analysis

**NestCore** serves as a shared dependency for:
- **LetMeHatch** - Uses: constants, helpers, GameStateQueries, API interface
- **EggsInWild** - Uses: constants, helpers, GameStateQueries, ModAssetHandler
- **RoosterControl** - Does NOT use NestCore (should!)

---

## What's Currently Shared

### From NestCore → Used by Other Mods

| Component | Used By | What It Does |
|-----------|---------|--------------|
| `EnumsAndStrings.cs` | LetMeHatch, WildEggs | Item IDs, Animal Keys, Constants |
| `DailyLuckHelper.cs` | LetMeHatch, WildEggs, RoosterControl | Daily luck calculations |
| `WeightedRandomPicker.cs` | LetMeHatch, WildEggs | Weighted random selection |
| `GameStateQueries/` | LetMeHatch, WildEggs | Complex game state evaluation |
| `ModAssetHandler.cs` | LetMeHatch, WildEggs | Asset path handling |
| `ObjectCloner.cs` | LetMeHatch | Deep/shallow object cloning |
| `ILetMeHatchApi.cs` | LetMeHatch | API interface for LetMeHatch |
| `CoreModConfig.cs` | LetMeHatch, WildEggs | Base config class |
| `DataManager.cs` | LetMeHatch | Generic data load/save |

---

## Duplication Issues Found

### 1. Daily Luck Calculation

**In RoosterControl** (should use NestCore):
```csharp
// ModEntry.cs - inline luck calculation
if (conditionSuperUnlucky.IsMatch) { DailyLuck = LuckyTypes.SuperUnlucky; }
else if (conditionUnlucky.IsMatch) { DailyLuck = LuckyTypes.Unlucky; }
// ... 5 different conditions
```

**In NestCore** (already exists but not used by RoosterControl):
```csharp
// DailyLuckHelper.cs
public static LuckyTypes GetDailyLuck() { ... }
public static double AdjustChanceByDailyLuck(double chance, double dailyLuckModifier) { ... }
```

### 2. Enums Duplication

**LetMeHatch has its own EnumsAndStrings that duplicates:**
- `VanillaAnimals` (from NestCore)
- `ItemIds` (from NestCore)

```csharp
// LetMeHatch/EnumsAndStrings.cs
using static Mikew.NestCore.EnumsAndStrings.Strings.ItemIds;
// Then redefines many of the same constants!
```

### 3. WeightedRandomPicker Variations

- **NestCore**: `WeightedRandomPicker<T>` - generic version (good!)
- **RoosterControl**: `WeightedRandomPicker` - non-generic, awkward dict-of-dict
- **EggsInWild**: Uses `_refactor`'s `WeightedTable<T>` (best!)

### 4. LuckyTypes Enum

Defined in **three places**:
1. `NestCore/EnumsAndStrings.cs` - `LuckyTypes` enum
2. `RoosterControl/Enums.cs` - `LuckyTypes` enum (DUPLICATE!)
3. `EggsInWild/_refactor/Core/Enums/LuckyTypes.cs` - (new refactor)

---

## Proposed Consolidation

### Phase 1: Fix RoosterControl to Use NestCore

**Add to NestCore:**
1. Export constants/enums that RoosterControl needs
2. Ensure DailyLuckHelper is accessible

**In RoosterControl, replace:**
- `RoosterControl/Enums.cs:LuckyTypes` → use `NestCore.EnumsAndStrings.LuckyTypes`
- Manual luck calculation → use `DailyLuckHelper.GetDailyLuck()`

### Phase 2: Improve Shared Components

#### A. Upgrade WeightedRandomPicker → WeightedTable<T>

Move the proven `WeightedTable<T>` from `_refactor` into NestCore:
```csharp
// Replace current WeightedRandomPicker<T> with this cleaner version
public sealed class WeightedTable<T>
{
    private readonly List<WeightedEntry<T>> _entries;
    private readonly IRandomProvider? _randomProvider;
    
    public T Roll() { ... }
    public T Roll(Random? random) { ... }
}
```

#### B. Create Unified LuckyTypes

Ensure all mods use the same enum:
```csharp
// In NestCore.EnumsAndStrings
public enum LuckyTypes
{
    SuperUnlucky,
    Unlucky, 
    Neutral,
    Lucky,
    SuperLucky
}
```

#### C. Extract Common Constants

Move to shared location:
```
NestCore/
├── Shared/
│   ├── Constants/
│   │   ├── ItemIds.cs        # All item IDs
│   │   ├── AnimalKeys.cs     # All animal keys
│   │   └── AssetNames.cs    # All asset names
│   ├── Enums/
│   │   └── LuckyTypes.cs    # Single source of truth
│   └── Helpers/
│       ├── DailyLuckHelper.cs
│       ├── WeightedTable.cs
│       └── DataManager.cs
```

### Phase 3: Create Shared Infrastructure

#### A. Add GameStateReader Interface

```csharp
// In NestCore
namespace Mikew.NestCore.Infrastructure;

public interface IGameStateReader
{
    LuckyTypes GetDailyLuck(double modifier);
    int GetCurrentDay();
    GameLocation? GetLocation(string name);
    // ... other common queries
}

public class GameStateReader : IGameStateReader { ... }
```

#### B. Add Config Base Classes

```csharp
// NestCore/Framework
public abstract class BaseModConfig
{
    public bool VerboseLogging { get; set; }
    public bool TestingLogging { get; set; }
}

public abstract class ModConfigWithApi : BaseModConfig
{
    // Common API-related options
}
```

### Phase 4: Improve ModAssetHandler

The current implementation is tightly coupled. Refactor to:
```csharp
public interface IAssetLoader
{
    void LoadTexture(AssetRequestedEventArgs e, string path);
    void LoadMap(AssetRequestedEventArgs e, string path);
}

public class ModAssetHandler
{
    private readonly IAssetLoader _loader;
    public ModAssetHandler(IAssetLoader loader) { _loader = loader; }
}
```

---

## Implementation Roadmap

### Step 1: Quick Fixes (1 day)
- [ ] Update RoosterControl to use `NestCore.DailyLuckHelper`
- [ ] Remove duplicate `LuckyTypes` from RoosterControl
- [ ] Update RoosterControl to use `NestCore.Enums`

### Step 2: Shared Infrastructure (2-3 days)
- [ ] Move `WeightedTable<T>` from _refactor to NestCore
- [ ] Create unified `LuckyTypes` enum
- [ ] Add `IGameStateReader` interface to NestCore

### Step 3: Clean Up Duplication (2-3 days)
- [ ] Remove duplicate EnumsAndStrings from LetMeHatch
- [ ] Update all mods to use shared constants
- [ ] Create shared config base classes

### Step 4: Long-term (1 week)
- [ ] Refactor ModAssetHandler to use interfaces
- [ ] Add comprehensive unit tests
- [ ] Document shared API

---

## Components to Add to NestCore

### 1. Core/Namespace Restructure
```
NestCore/
├── Core/
│   ├── Constants/      # Item IDs, Animal Keys
│   ├── Enums/         # LuckyTypes, etc.
│   └── Random/        # WeightedTable
├── Infrastructure/
│   ├── Game/          # IGameStateReader
│   └── Assets/        # IAssetLoader
├── Framework/
│   └── ModConfig.cs   # Already exists, enhance
└── Helpers/
    ├── DailyLuckHelper.cs  # Already exists
    └── DataManager.cs      # Already exists
```

### 2. New Files to Create

| File | Purpose |
|------|---------|
| `Core/Random/WeightedTable.cs` | Generic weighted selection |
| `Infrastructure/Game/IGameStateReader.cs` | Game state interface |
| `Infrastructure/Game/GameStateReader.cs` | Game state implementation |
| `Core/Constants/SharedItemIds.cs` | Unified item IDs |
| `Core/Constants/SharedAnimalKeys.cs` | Unified animal keys |

---

## COMPLETE CODE OVERLAP ANALYSIS

### 1. Daily Luck Logic

**NestCore (SHARED):**
- `DailyLuckHelper.cs` - Complete daily luck calculation + adjustment

**RoosterControl (DUPLICATE):**
- `ModEntry.cs:261-295` - Inline luck determination (DUPLICATE!)
- `RoosterControl.cs:353-366` - Luck adjustment switch (DUPLICATE!)
- `Enums.cs` - LuckyTypes enum (DUPLICATE!)

**EggsInWild (_refactor):**
- `Core/Enums/LuckyTypes.cs` - Separate enum (should use NestCore!)

### 2. Weighted Random Selection

**NestCore (BEST):**
- `Helpers/Extensions.cs` - `GetRandomItem<T>()` extension methods
- `WeightedRandomPicker.cs` - Generic class version

**RoosterControl (AWKWARD):**
- `Helpers/WeightedRandomPicker.cs` - Non-generic, uses dict-of-dict

**EggsInWild (_refactor - BEST):**
- `_refactor/Spawning/WeightedTable.cs` - Cleanest implementation!

### 3. Extension Methods

**NestCore (SHARED):**
```csharp
// In Extensions.cs:
- TryAdd()              # Used by LetMeHatch, EggsInWild
- GetRandomElementOrDefault()  
- IsEmptyOrWhitespace()
- GetValues<TEnum>()
- GetRandomItem<T>()
- ContainsCI()
```

**LetMeHatch (uses NestCore):**
- Uses all above via `using Mikew.NestCore.Helpers;`

**EggsInWild (DUPLICATES!):**
- `Services/EggSpawning.cs` - Uses `TryAdd` from Dictionary
- `Managers/GameAssetManagement/*.cs` - Uses `TryAddMany`, `PopulateIfEmpty`

### 4. Configuration

**NestCore (SHARED):**
- `Framework/ModConfig.cs` - `CoreModConfig` base class
```csharp
public class CoreModConfig {
    public bool VerboseLogging;
    public bool TestingLogging;
}
```

**LetMeHatch (extends NestCore):**
- `Config/ModConfig.cs` - extends `CoreModConfig` ✅

**EggsInWild (DUPLICATE!):**
- `Framework/ModConfig.cs` - Own implementation! (should extend CoreModConfig)

**RoosterControl (NO SHARING):**
- `Framework/ModConfig.cs` - Own standalone class

### 5. Data Loading/Saving

**NestCore (SHARED):**
```csharp
// DataManager.cs
public static T LoadData<T>(IModHelper helper, string? fileName = null)
public static void SaveData<T>(T data, IModHelper helper, string? fileName = null)
```

**LetMeHatch (uses NestCore):**
- Uses `DataManager` ✅

**EggsInWild (_refactor):**
- `State/PlayerStateRepository.cs` - Own implementation (should use DataManager)

### 6. Object Cloning

**NestCore (SHARED):**
- `Helpers/ObjectCloner.cs` - Deep/Shallow copy via JSON

**LetMeHatch (uses NestCore):**
- Uses `ObjectCloner.Clone<T>()` ✅

### 7. Game State Queries

**NestCore (SHARED):**
- `Helpers/GameStateQueries/GameStateQueries.cs`
- `Helpers/GameStateQueries/GameStateQueryHelper.cs`

**LetMeHatch (uses NestCore):**
- Uses all GameStateQueries ✅

**EggsInWild (DUPLICATE):**
- Has same/similar queries inline in multiple managers

---

## Summary Table

| Component | NestCore | LetMeHatch | EggsInWild | RoosterControl |
|-----------|-----------------|-------------------|------------------|--------------|
| **DailyLuckHelper** | ✅ Source | Uses ✅ | Should use | DUPLICATE! |
| **LuckyTypes enum** | ✅ Source | Uses ✅ | _refactor has own | DUPLICATE! |
| **WeightedTable** | ✅ Basic | Uses ✅ | _refactor BEST | DUPLICATE! |
| **Extensions** | ✅ Source | Uses ✅ | DUPLICATE! | No |
| **CoreModConfig** | ✅ Source | Extends ✅ | DUPLICATE! | No |
| **DataManager** | ✅ Source | Uses ✅ | DUPLICATE! | No |
| **ObjectCloner** | ✅ Source | Uses ✅ | No | No |
| **GameStateQueries** | ✅ Source | Uses ✅ | DUPLICATE! | No |

---

## Mod-Specific Updates Required

### RoosterControl Changes (HIGH PRIORITY)
1. Remove `Enums.cs:LuckyTypes` - use NestCore
2. Remove inline luck calculation - use `DailyLuckHelper`
3. Remove `WeightedRandomPicker` - use NestCore version
4. Add reference to NestCore if not present
5. Replace nested dictionary config with typed config

### LetMeHatch Changes (LOW - Already good!)
1. Already uses NestCore properly ✅

### EggsInWild Changes (MEDIUM)
1. Migrate from custom luck logic to `DailyLuckHelper`
2. Remove duplicate Extensions - use NestCore
3. Use `CoreModConfig` as base for ModConfig
4. Use `DataManager` for save/load
5. Remove duplicate GameStateQueries - use NestCore
6. Consider using _refactor's `WeightedTable<T>` (best version!)

