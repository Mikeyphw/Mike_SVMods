# LetMeHatchThisEgg Refactoring Plan

## Overview

This document outlines a comprehensive refactoring plan for **LetMeHatchThisEgg** mod, following the same architectural patterns established in the `/home/mike/Documents/Projects/WildEggsHatching/_refactor` directory.

---

## Current Architecture Issues

### 1. ModEntry.cs (~450 lines)
- Does too many jobs: event wiring, initialization, asset routing, API integration
- Directly manipulates game data in event handlers
- Mixed concerns: initialization logic + event handling + asset editing

### 2. GameAssetManager.cs (~600 lines)
- God class handling too many responsibilities:
  - Texture management
  - Item cloning
  - Breeding data loading
  - Machine data editing
  - Egg list editing
  - Birth text editing
- Tightly coupled to multiple managers (ItemManager, TextureManager, BreedingDataLoader, etc.)

### 3. ModConfig.cs - Mixed Config Approach
- Uses typed properties for simple config
- But still has nested dictionaries:
  ```csharp
  Dictionary<string, Dictionary<CloneItemType, Dictionary<ItemType, string>>> ItemToClone
  Dictionary<string, Dictionary<string, bool>> EggColors
  ```

### 4. BreedingSystem.cs & BreedingDataLoader.cs
- BreedingSystem: Business logic but tightly coupled to game data
- BreedingDataLoader: Content pack loading mixed with data filtering
- Large switch/logic in GetVanillaEggsID() method
- Complex nested dictionary access patterns

### 5. Cloning/AnimalCloner.cs (~500 lines)
- Huge EditClonedAnimal method with 50+ property assignments
- Repeated patterns: SetIfValueIsPlaceholder called many times
- Mixed concerns: data editing + content pack handling

### 6. Static Fields Everywhere
- ModEntry uses excessive static properties:
  ```csharp
  public static ModConfig Config { get; private set; }
  public static IMonitor MonitorInstance { get; private set; }
  public static IModHelper HelperInstance { get; private set; }
  public static Logger LoggerHelper { get; private set; }
  public static I18TranslationHelper TranslationHelper { get; private set; }
  public static ModAssetHandler AssetHandler { get; private set; }
  ```

---

## Target Architecture (Following _refactor Pattern)

```
LetMeHatchThisEgg/
├── Core/                              # Pure C#, no game dependencies
│   ├── Models/                        # Data classes
│   │   ├── CloneDefinition.cs        # What gets cloned
│   │   ├── BreedingPair.cs           # Male/female breeding pair
│   │   ├── EggChances.cs             # Chance configuration
│   │   └── ClonedAnimalState.cs      # Runtime state
│   ├── Enums/                        # Enums (already exists, need cleanup)
│   └── Events/                       # Event bus for decoupling
│       ├── EventBus.cs
│       ├── AnimalClonedEvent.cs
│       ├── BreedingStartedEvent.cs
│       └── EggSpawnedEvent.cs
│
├── Services/                          # Single-responsibility services
│   ├── BreedingService.cs             # Breeding logic (from BreedingSystem)
│   ├── CloningService.cs             # Animal cloning logic
│   ├── EggProbabilityService.cs       # Egg chance calculations
│   ├── ContentPackService.cs          # Load/filter content packs
│   └── IntegrationService.cs          # Handle external mod APIs
│
├── Data/                              # Data catalogs and loaders
│   ├── AnimalCatalog.cs              # Static animal definitions
│   ├── EggCatalog.cs                  # Egg definitions
│   └── BreedingDataRepository.cs       # Breeding data access
│
├── Infrastructure/                    # Game API adapters (ports)
│   ├── Game/
│   │   ├── IGameStateReader.cs        # Interface for game state
│   │   ├── GameStateReader.cs         # Implementation
│   │   ├── IFarmAnimalEditor.cs       # Interface for editing
│   │   └── FarmAnimalEditor.cs        # Implementation
│   ├── Config/
│   │   ├── IConfigReader.cs           # Config interface
│   │   └── ConfigMapper.cs            # Map dict → typed config
│   └── Content/
│       ├── AssetEditorRouter.cs        # Route asset requests
│       └── TextureLoader.cs            # Load mod textures
│
├── Coordination/                      # Wiring layer
│   └── HatchCoordinator.cs            # Main coordinator (like WildEggCoordinator)
│
└── UI/                                # Config UI (separate from logic)
    ├── GmcmPageBuilder.cs             # Build GMCM pages
    └── OptionDescriptor.cs            # Describe options (like _refactor)
```

---

## Refactoring Priorities

### Phase 1: Infrastructure Isolation (Highest Priority)
1. Create `IGameStateReader` interface
2. Extract asset routing from ModEntry
3. Remove static fields, use dependency injection

### Phase 2: Config Type Safety
1. Replace nested dictionaries in ModConfig with typed properties
2. Create `ConfigMapper` to handle migration from old format
3. Use OptionDescriptor pattern for GMCM

### Phase 3: Service Extraction
1. Extract `BreedingService` from BreedingSystem
2. Extract `CloningService` from AnimalCloner
3. Create `ContentPackService` from BreedingDataLoader

### Phase 4: Event-Driven Communication
1. Implement EventBus
2. Replace direct method calls with events
3. Make services loosely coupled

### Phase 5: Data-Driven Approaches
1. Replace switch statements in GetVanillaEggsID with dictionaries
2. Use WeightedTable<T> for random selection
3. Create static catalogs

---

## Specific Changes

### 1. Replace Nested Dictionaries in ModConfig

**Before:**
```csharp
public Dictionary<string, Dictionary<CloneItemType, Dictionary<ItemType, string>>> ItemToClone {get; set;}
```

**After:**
```csharp
public class CloneMapping
{
    public string SourceAnimal { get; set; }
    public CloneItemType ItemType { get; set; }
    public ItemType EggType { get; set; }
    public string TargetItemId { get; set; }
}

public List<CloneMapping> CloneMappings { get; set; }
```

### 2. Replace Static Fields with DI

**Before:**
```csharp
public static ModConfig Config { get; private set; }
public static IMonitor MonitorInstance { get; private set; }
```

**After:**
```csharp
public class HatchCoordinator
{
    private readonly ModConfig _config;
    private readonly IMonitor _monitor;
    private readonly IModHelper _helper;
    
    public HatchCoordinator(ModConfig config, IMonitor monitor, IModHelper helper)
    {
        _config = config;
        _monitor = monitor;
        _helper = helper;
    }
}
```

### 3. Replace Switch with Dictionary

**Before (BreedingDataLoader.cs):**
```csharp
private Dictionary<ItemType, string?> GetVanillaEggsID(string animalKey)
{
    return animalKey switch
    {
        VanillaAnimals.WhiteChicken => (AnimalProducts.WhiteEgg, AnimalProducts.LargeWhiteEgg),
        VanillaAnimals.BrownChicken => (AnimalProducts.BrownEgg, AnimalProducts.LargeBrownEgg),
        // ... 20+ more cases
    };
}
```

**After:**
```csharp
private static readonly Dictionary<string, (string Normal, string Large)> VanillaEggs = new()
{
    { VanillaAnimals.WhiteChicken, (AnimalProducts.WhiteEgg, AnimalProducts.LargeWhiteEgg) },
    { VanillaAnimals.BrownChicken, (AnimalProducts.BrownEgg, AnimalProducts.LargeBrownEgg) },
    // ...
};

private Dictionary<ItemType, string?> GetVanillaEggsID(string animalKey)
{
    if (VanillaEggs.TryGetValue(animalKey, out var eggs))
    {
        return new() { [ItemType.Normal] = eggs.Normal, [ItemType.Large] = eggs.Large };
    }
    return new();
}
```

### 4. Use WeightedTable for Random Selection

**Before (BreedingSystem.cs):**
```csharp
var chosenMale = males
    .Where(m => m.Value.Enabled)
    .GetRandomItem(male => GetMaleQuantity(male.Key));
```

**After:**
```csharp
var maleTable = new WeightedTable<string>(
    males.Where(m => m.Value.Enabled)
         .ToDictionary(m => m.Key, m => GetMaleQuantity(m.Key))
);
var chosenMaleKey = maleTable.Roll();
```

### 5. Extract Huge AnimalCloner Methods

**Before:** 500+ line EditClonedAnimal method

**After:** Use a builder pattern:
```csharp
public class AnimalDataBuilder
{
    private FarmAnimalData _data;
    
    public AnimalDataBuilder WithGender(FarmAnimalGender gender) { ... }
    public AnimalDataBuilder WithTexture(...) { ... }
    public AnimalDataBuilder WithEggs(...) { ... }
    public FarmAnimalData Build() => _data;
}
```

### 6. Event-Driven Asset Editing

**Before:**
```csharp
private void HandleFarmAnimalsAsset(AssetRequestedEventArgs e)
{
    if (AssetManager.ClonedAnimalsInfoList.Any())
    {
        AssetManager.CreateClonePlaceholders(e, AssetEditPriority.Early);
        AssetManager.CreateAndAddAnimalClones(e, AssetEditPriority.Late);
    }
    HandleAdditionalFarmAnimalsLogic(e);
}
```

**After:**
```csharp
// In coordinator
public void OnAssetRequested(AssetRequestedEventArgs e)
{
    _assetRouter.Route(e);
}

// AssetRouter subscribes to events and handles assets
_assetRouter.Subscribe<AnimalClonedEvent>(evt => 
{
    // Update assets when animal is cloned
});
```

---

## Migration Strategy

### Step 1: Create Infrastructure Layer
- Extract interfaces for game access
- Create coordinator class
- Keep existing code, just route through coordinator

### Step 2: Refactor Config
- Add typed properties alongside dictionaries
- Add migration mapper
- Gradually switch to new properties

### Step 3: Extract Services One by One
- Start with BreedingService (smallest, most isolated)
- Then CloningService
- Keep legacy classes as thin wrappers

### Step 4: Add Event Bus
- Add events for key actions
- Connect services through events

### Step 5: Remove Legacy Code
- Delete old wrapper classes after testing
- Verify all functionality works

---

## Design Patterns to Apply

| Pattern | Where to Apply |
|---------|----------------|
| **Strategy** | Different breeding/hatching strategies |
| **Builder** | AnimalCloner property setting |
| **Repository** | BreedingDataRepository |
| **Factory** | Create cloned animals |
| **Observer/EventBus** | Service communication |
| **Adapter** | Game API wrappers |
| **DI/Coordinator** | Wire everything together |

---

## Files to Create

1. `Core/Events/EventBus.cs` - Simple event dispatcher
2. `Core/Events/*.cs` - Domain events
3. `Infrastructure/Game/IGameStateReader.cs` - Game interface
4. `Infrastructure/Game/GameStateReader.cs` - Game implementation
5. `Coordination/HatchCoordinator.cs` - Main coordinator
6. `Services/BreedingService.cs` - Extracted breeding logic
7. `Services/CloningService.cs` - Extracted cloning logic
8. `Data/AnimalCatalog.cs` - Static catalog
9. `UI/OptionDescriptor.cs` - GMCM option descriptions
10. `UI/GmcmPageBuilder.cs` - Build GMCM pages

---

## Testing Considerations

1. **Unit test services** without SMAPI runtime
2. **Integration test** coordinator with game
3. **Migration tests** - ensure config backwards compatibility
4. **Event tests** - verify event flow works correctly

---

## Notes

- This mod has **more complexity** than RoosterAddon due to:
  - Content pack system for extensibility
  - More complex breeding logic
  - Multiple integration points (RoosterAddon, WildEggs)
  
- The _refactor architecture is a good fit because:
  - Services can be tested in isolation
  - Event bus enables loose coupling for content packs
  - Coordinator pattern handles complex initialization

- Key challenge: Maintaining content pack API while refactoring
  - Need to preserve IContentPack access for external mods
  - Consider creating a content pack abstraction layer

