# BirdsOfAFeather Refactoring Plan

## Overview

This document outlines a refactoring plan for **BirdsOfAFeather** mod, following the same architectural patterns established in `/home/mike/Documents/Projects/WildEggsHatching/_refactor`.

**Note:** This is already the simplest mod of the three (RoosterAddon, LetMeHatchThisEgg, BirdsOfAFeather). It has less structural issues but can still benefit from cleaner architecture.

---

## Current Architecture

### File Structure
```
BirdsOfAFeather/
├── ModEntry.cs                    # ~100 lines - event wiring, GMCM setup
├── Framework/
│   └── ModConfig.cs               # Simple CoreModConfig with 2 bools
├── Helpers/
│   ├── DailyLuckHelper.cs         # Daily luck calculations
│   ├── DataManager.cs             # Generic data load/save
│   ├── ModAssetHandler.cs         # Asset path handling
│   ├── I18TranslationHelper.cs    # i18n utilities
│   ├── Logger.cs                  # Logging wrapper
│   ├── ObjectCloner.cs            # (not examined)
│   └── GameStateQueries/          # Complex game state evaluation
│       ├── GameStateQueries.cs
│       ├── GameStateQueries.EventCondition.cs
│       ├── GameStateQueries.ItemConditions.cs
│       ├── GameStateQueries.PlayerCondition.cs
│       ├── GameStateQueries.PlayerRelationships.cs
│       ├── GameStateQueries.Randomization.cs
│       ├── GameStateQueries.TimeCondition.cs
│       └── GameStateQueries.WorldCondition.cs
├── Models/
│   └── AssetData.cs               # Asset metadata
└── WeightedRandomPicker.cs         # Generic weighted selection
```

---

## Current Issues

### 1. Static Fields in ModEntry
```csharp
internal static IModHelper HelperInstance;
internal static CoreModConfig ModCfg { get; private set; }
public static IMonitor? MonitorInstance { get; private set; }
public static IManifest? ModManifestInstance { get; private set; }
```

### 2. WeightedRandomPicker vs WeightedTable
- Already uses generics: `WeightedRandomPicker<T>` - good!
- Could adopt `_refactor`'s `WeightedTable<T>` pattern for consistency

### 3. GameStateQueries - Potential Extraction
- Complex game state evaluation system
- Could be extracted to a reusable library
- Currently mixed into Helpers folder

### 4. No Coordinator Pattern
- ModEntry directly handles everything
- No separation between wiring and logic

---

## Target Architecture

```
BirdsOfAFeather/
├── Core/
│   ├── Events/                    # Event bus
│   │   └── EventBus.cs
│   ├── Models/
│   │   └── AssetDefinition.cs
│   └── Random/
│       └── WeightedTable.cs       # Replace WeightedRandomPicker
│
├── Infrastructure/
│   ├── Game/
│   │   ├── IGameStateReader.cs    # Interface
│   │   └── GameStateReader.cs     # Implementation
│   └── Config/
│       └── ConfigLoader.cs        # Config loading
│
├── Services/
│   ├── LuckService.cs             # From DailyLuckHelper
│   └── (future game services)
│
├── Coordination/
│   └── BirdCoordinator.cs        # Main coordinator
│
├── Libraries/                     # Extracted utilities
│   └── GameStateQueries/          # Already exists, just organize
│
└── UI/
    └── GmcmBuilder.cs             # GMCM setup
```

---

## Refactoring Steps

### Phase 1: Basic Cleanup (Quick Wins)

1. **Replace WeightedRandomPicker with WeightedTable<T>**
   - Adopt the proven `WeightedTable<T>` from _refactor
   - Remove custom implementation

2. **Extract Logger/Translation to Infrastructure**
   - Move to proper layer organization

### Phase 2: Coordinator Pattern

1. **Create BirdCoordinator**
   ```csharp
   public class BirdCoordinator
   {
       private readonly IGameStateReader _gameState;
       private readonly CoreModConfig _config;
       private readonly IMonitor _monitor;
       
       public void OnGameLaunched() { }
       public void OnDayStarted() { }
       public void OnAssetRequested(AssetRequestedEventArgs e) { }
   }
   ```

2. **Remove Static Fields from ModEntry**
   - Pass dependencies through constructor
   - Use coordinator pattern

### Phase 3: Game State Queries

1. **Organize GameStateQueries**
   - Keep as-is since it works well
   - Could extract to a separate namespace/library
   - Consider making it a reusable NuGet package

### Phase 4: Add Event Bus (Optional)

Since this is a simpler mod, an event bus may be overkill. Only add if the mod grows in complexity.

---

## Specific Changes

### 1. Replace Static Fields

**Before:**
```csharp
internal static IModHelper HelperInstance;
internal static CoreModConfig ModCfg { get; private set; }
```

**After:**
```csharp
public class BirdCoordinator
{
    private readonly IModHelper _helper;
    private readonly CoreModConfig _config;
    private readonly IMonitor _monitor;
}
```

### 2. Use WeightedTable<T>

**Before:**
```csharp
public class WeightedRandomPicker<T>
{
    private Dictionary<T, float> _weights = new();
    // ... custom implementation
}
```

**After:** Use `_refactor`'s `WeightedTable<T>` directly or create a thin wrapper.

### 3. Organize GameStateQueries

```
Helpers/GameStateQueries/
├── GameStateQueries.cs           # Main class + base conditions
├── Events/                       # Event conditions
│   └── EventCondition.cs
├── Items/                        # Item conditions
│   └── ItemConditions.cs
├── Player/                      # Player conditions
│   ├── PlayerCondition.cs
│   └── PlayerRelationships.cs
├── Random/                       # Random conditions
│   └── Randomization.cs
├── Time/                         # Time conditions
│   └── TimeCondition.cs
└── World/                        # World conditions
    └── WorldCondition.cs
```

---

## Comparison with Other Mods

| Aspect | BirdsOfAFeather | LetMeHatchThisEgg | RoosterAddon |
|--------|-----------------|-------------------|--------------|
| **Size** | Small (~1000 lines) | Large (~4000 lines) | Medium (~2500 lines) |
| **Complexity** | Low | High | Medium |
| **Static Fields** | 4 | 6+ | 2 |
| **God Classes** | None | GameAssetManager | ModEntry, RoosterControl |
| **Refactor Priority** | Low | High | Medium |

---

## Priority Recommendations

### Do First (Quick Wins)
1. Replace WeightedRandomPicker with WeightedTable<T>
2. Add XML docs to existing classes

### Do Next (Architecture)
1. Create BirdCoordinator class
2. Remove static fields
3. Organize GameStateQueries into subfolders

### Skip for Now
- Event Bus - overkill for this mod's size
- Extensive service extraction - not needed yet

---

## Notes

- **This mod is already well-structured** for its size
- The main improvement is moving from static fields to coordinator pattern
- GameStateQueries is actually a well-designed system - just needs organization
- Consider making GameStateQueries a reusable NuGet package if it proves valuable

---

## Files to Create

1. `Coordination/BirdCoordinator.cs` - Main coordinator
2. `Infrastructure/Game/IGameStateReader.cs` - Interface
3. `Infrastructure/Game/GameStateReader.cs` - Implementation  
4. `Core/Random/WeightedTable.cs` - Replace WeightedRandomPicker

## Files to Modify

1. `ModEntry.cs` - Use coordinator
2. `WeightedRandomPicker.cs` - Deprecate in favor of WeightedTable
3. Move `Helpers/GameStateQueries/` to `Libraries/GameStateQueries/`

