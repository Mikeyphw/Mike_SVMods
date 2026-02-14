# Refactor Gap Analysis - What's Incomplete (DETAILED)

## ❌ WHAT'S ACTUALLY MISSING

### 1. AnimalCatalog - EMPTY (Critical)

**Location**: `refactor/Data/AnimalCatalog.cs`

**Current State**: The catalog exists but has NO animal data loaded - it's just a dictionary wrapper.

```csharp
// CURRENT REFACTOR - refactor/Data/AnimalCatalog.cs
public sealed class AnimalCatalog
{
    private readonly Dictionary<string, WildAnimalData> _animals;

    public AnimalCatalog(IEnumerable<WildAnimalData> animals)
    {
        _animals = animals.ToDictionary(a => a.Id, a => a);
    }
    // ... basic lookup methods only
}
```

**What's Missing**: Needs to load from JSON files (WildAnimalsList.json has ~100 animals). Original code in `WildEggsHatching_orig/Managers/WildListManager.cs`:

```csharp
// ORIGINAL - WildListManager.cs (what needs to be ported)
public void SetUpAnimalLists()
{
    // Load from JSON
    var wildAnimalsJson = HelperInstance.Data.ReadJsonFile<Dictionary<string, WildAnimalData>>(
        "ModData/WildAnimalsList.json"
    );
    
    // Filter by enabled mods
    foreach (var (animalKey, animalData) in wildAnimalsJson)
    {
        // Check if mod is loaded for ExtraModAnimalKeys
        if (!IsModLoaded(animalKey)) continue;
        
        // Add to valid animals
        ValidWildAnimals.Add(animalKey, animalData);
    }
}

private bool IsModLoaded(string modId)
{
    if (modId == "Vanilla") return true;
    return HelperInstance.ModRegistry.IsLoaded(modId);
}
```

---

### 2. EggSpawning Logic - SIMPLIFIED (Critical)

**Location**: `refactor/Spawning/EggSpawnService.cs`

**Current State**: Simplified single-location spawner (~150 lines).

```csharp
// CURRENT REFACTOR - Simplified version
public SpawnResult TrySpawn(SpawnContext context)
{
    // Only spawns in Farm location
    var farm = _gameStateReader.GetLocation("Farm");
    if (farm == null) return SpawnResult.None();

    // Simple single-location logic
    var eggType = GetEggTypeForLocation(context.Location);
    float chance = _chanceCalculator.Calculate(eggType, context.DailyLuck);
    
    if (_randomProvider.NextDouble() >= chance)
        return SpawnResult.None();

    var animalId = SelectAnimal(context);
    var tile = FindValidTile(context);
    
    return SpawnResult.Success(eggId, animalId, context.Location, tile, eggType);
}
```

**What's Missing**: Original `Services/EggSpawning.cs` (1100+ lines) includes:

```csharp
// ORIGINAL - EggSpawning.cs (what needs to be ported)
public class EggSpawning
{
    private Dictionary<string, LocationPack> LocationPacks;
    private Dictionary<string, HashSet<Area>> SpawnAreas { get; set; }
    private HashSet<SpawnedEggInfo> SpawnedEggsLocations { get; set; }
    
    // Missing: JSON loading
    private void InitializeFromJsons(HashSet<WildAnimalData> loadedWildAnimals)
    {
        LocationPacks = LoadLocationPacks();
        Parameters = LoadSpawnParameters();
        SpawnAreas = LoadSpawnAreas();
        InitializeLocationPacks();
        InitializeSpawnAreas(loadedWildAnimals);
    }

    // Missing: Complex multi-location spawning
    public void SpawnEggs()
    {
        // 1. Clear counters
        SpawnedEggsLocations.Clear();
        
        // 2. Load spawn areas from all locations
        var gameLocationNames = Game1.locations.Select(l => l.Name).ToHashSet();
        var spawnAreasFlattened = GetValidSpawnAreas(gameLocationNames);
        
        // 3. Get certain spawn areas (guaranteed spawns)
        var certainSpawnAreas = GetCertainSpawnAreas(spawnAreasFlattened);
        
        // 4. Spawn certain eggs first
        SpawnCertainEggs(certainSpawnAreas, packMaxEggQuantities);
        
        // 5. Spawn remaining eggs based on luck
        SpawnRemainingEggs(spawnAreasFlattened, packMaxEggQuantities);
    }

    // Missing: Area validation with conditions
    public bool AreAreaExtraConditionsMet(Area area)
    {
        // Check GameStateQuery
        if (!string.IsNullOrEmpty(extraConditions.GameStateQuery))
            if (!GameStateQuery.CheckConditions(extraConditions.GameStateQuery))
                return false;
        
        // Check random chance
        if (Game1.random.NextDouble() >= area.RandomChance)
            return false;
        
        // Check seasons
        if (!extraConditions.Seasons.Contains(Game1.currentSeason))
            return false;
        
        // Check weather
        string currentWeather = modSaveData.WeatherToday;
        if (!extraConditions.WeatherToday.Contains(currentWeather))
            return false;
            
        return true;
    }

    // Missing: Location pack management
    private Dictionary<string, LocationPack> LoadLocationPacks()
    {
        var packs = HelperInstance.Data.ReadJsonFile<Dictionary<string, LocationPack>>(
            "ModData/LocationPacks.json"
        );
        return packs.Where(p => ModIdEvaluator.Evaluate(p.Key)).ToDictionary(...);
    }
}
```

---

### 3. NoteSpawning - PARTIAL

**Location**: `refactor/Notes/NoteService.cs`

**Original**: `WildEggsHatching_orig/Services/NoteSpawning.cs`

Needs to implement junimo note spawning when animals hatch.

---

### 4. AnimalTrackingService - PARTIAL

**Location**: `refactor/Tracking/AnimalTrackingService.cs`

**Original**: `WildEggsHatching_orig/Services/NewAnimalMonitoring.cs`

Missing: Hatch detection, tracking animals needing notes.

```csharp
// ORIGINAL - NewAnimalMonitoring.cs (what needs to be ported)
public class NewAnimalMonitoring
{
    private HashSet<string> AnimalsNeedingNotes { get; set; }
    
    public void CheckForNewlyHatchedAnimals()
    {
        // Check all farm buildings
        foreach (var building in Game1.getFarm().buildings)
        {
            if (building.buildingType.Contains("Coop") || building.buildingType.Contains("Barn"))
            {
                var animals = building.animals.Values();
                foreach (var animal in animals)
                {
                    // Check if animal just hatched (newly born)
                    if (IsNewlyHatched(animal) && !HasNoteBeenSent(animal))
                    {
                        QueueNoteForAnimal(animal);
                    }
                }
            }
        }
    }
}
```

---

## Detailed Missing Components

### A. Location Pack Loading
```csharp
// Need to add to refactor
public class LocationPackManager
{
    public Dictionary<string, LocationPack> LoadLocationPacks(IModHelper helper)
    {
        return helper.Data.ReadJsonFile<Dictionary<string, LocationPack>>(
            "ModData/LocationPacks.json"
        ) ?? new();
    }
}
```

### B. Spawn Area Loading
```csharp
// Need to add to refactor
public class SpawnAreaManager
{
    public Dictionary<string, HashSet<Area>> LoadSpawnAreas(IModHelper helper)
    {
        return helper.Data.ReadJsonFile<Dictionary<string, HashSet<Area>>>(
            "ModData/SpawnAreas.json"
        ) ?? new();
    }
}
```

### C. WildListManager Port
```csharp
// Need to port from WildEggsHatching_orig/Managers/WildListManager.cs
public class WildListManager
{
    private ILetMeHatchApi? LetMeHatchApi { get; set; }
    public HashSet<WildAnimalData> ValidWildAnimals { get; private set; }
    
    public void SetUpAnimalLists()
    {
        // Load from JSON
        var wildAnimalsJson = HelperInstance.Data.ReadJsonFile<Dictionary<string, WildAnimalData>>(
            "ModData/WildAnimalsList.json"
        );
        
        // Check mod compatibility
        foreach (var kvp in wildAnimalsJson)
        {
            if (IsModLoaded(kvp.Key))
            {
                ValidWildAnimals.Add(kvp.Key, kvp.Value);
            }
        }
    }
}
```

---

## Verification Checklist

When complete, verify:
- [x] ModEntry has no game logic
- [x] All Game1 references removed from domain
- [x] SpawnContext includes all needed data
- [x] Event subscriptions centralized in SmapiEventRouter
- [x] IGameStateReader uses string quest IDs
- [x] SmapiContentEditor wires all editors
- [x] All Asset Editors complete
- [ ] AnimalCatalog loads animal data from JSON
- [ ] EggSpawnService handles multi-location spawning
- [ ] Build compiles without errors

