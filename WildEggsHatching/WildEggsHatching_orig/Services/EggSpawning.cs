using Mikew.BirdsOfAFeather.Helpers;
using Microsoft.Xna.Framework;
using Mikew.BirdsOfAFeather.Helpers;
using StardewModdingAPI;
using StardewValley;
using Mikew.BirdsOfAFeather.Helpers;
using WildEggsHatching.DesertTomb;
using WildEggsHatching.DesertTomb.Configuration;
using WildEggsHatching.EnumsAndStrings;
using WildEggsHatching.Framework;
using WildEggsHatching.Models;
using static Mikew.BirdsOfAFeather.EnumsAndStrings.Enums;
using static WildEggsHatching.EnumsAndStrings.Enums;

namespace WildEggsHatching.Services;

public class EggSpawning
{
    private Dictionary<string, LocationPack> LocationPacks;
    private Dictionary<string, HashSet<Area>> SpawnAreas { get; set; }
    private HashSet<SpawnedEggInfo> SpawnedEggsLocations { get; set; }
    private readonly ModData _modSaveData;
    private readonly SpawnConfiguration _spawnConfig;
    private readonly HashSet<WildEgg> _wildEggs;
    private Dictionary<string, int> MapSpawnedEggs;
    private int WorldSpawnedEggsVanilla;
    private int WorldSpawnedEggsPacks;
    private SpawnParameters? Parameters;
    private JsonDataGenerator JsonGenerator;
    private int MaxWorldEggsPacks;
    private int MaxWorldEggsVanilla;
    private Dictionary<string, int> SpawnedEggsByID { get; set; }
    private Dictionary<EggType, int> SpawnedEggsByType { get; set; }

    public EggSpawning(ModData modData, SpawnConfiguration spawnConfiguration)
    {
        WorldSpawnedEggsPacks = 0;
        WorldSpawnedEggsVanilla = 0;
        MapSpawnedEggs = new();
        SpawnedEggsByID = new();
        _modSaveData = modData;
        _spawnConfig = spawnConfiguration;
        _wildEggs = new();
        SpawnedEggsLocations = new();
        LocationPacks = new();
        SpawnAreas = new();
        JsonGenerator = new JsonDataGenerator(ModEntry.HelperInstance);
        SpawnedEggsByType = new();
    }

    private void InitializeFromJsons(HashSet<WildAnimalData> loadedWildAnimals)
    {
        LocationPacks = LoadLocationPacks();

        ModEntry.LoggerHelper.Log($"Loaded location packs: {LocationPacks.Count}", LogLevel.Trace);
        Parameters = LoadSpawnParameters();
        SpawnAreas = LoadSpawnAreas();
        InitializeLocationPacks();
        InitializeSpawnAreas(loadedWildAnimals);
    }

    internal void OnDayStart(HashSet<WildEgg> wildEggs, HashSet<WildAnimalData> loadedWildAnimals)
    {
        _wildEggs.AddRange(wildEggs);
        InitializeFromJsons(loadedWildAnimals);

        ApplyChanceModifier();
        SetSavedValuesFromJson();

        SaveFinalSpawnData();
        SaveFinalLoadedPacksData();
        SpawnEggs();
    }

    internal void ResetSpawnParameters()
    {
        Parameters = new();
        ModEntry.HelperInstance.Data.ReadJsonFile<SpawnParameters>("ModData/SpawnParameters.json");
    }

    private void SetSavedValuesFromJson()
    {
        SetMaxEggPack();
        SetMinEggPack();
        SetMinEggArea();
        SetMaxEggArea();
    }

    /// <summary>
    /// Loads spawn areas from a JSON file.
    /// </summary>
    private Dictionary<string, LocationPack> LoadLocationPacks()
    {
        var packs = ModEntry.HelperInstance.Data.ReadJsonFile<Dictionary<string, LocationPack>>(
            "ModData/LocationPacks.json"
        );
        return packs != null
            ? packs
                .Where(p => ModIdEvaluator.Evaluate(p.Key, ModEntry.HelperInstance.ModRegistry))
                .ToDictionary(x => x.Key, y => y.Value)
            : JsonGenerator.GenerateLocationPacks();
    }

    /// <summary>
    /// Loads spawn areas from a JSON file.
    /// </summary>
    private Dictionary<string, HashSet<Area>> LoadSpawnAreas()
    {
        return ModEntry.HelperInstance.Data.ReadJsonFile<Dictionary<string, HashSet<Area>>>(
                "ModData/SpawnAreas.json"
            ) ?? new();
    }

    private SpawnParameters LoadSpawnParameters()
    {
        var parameters =
            ModEntry.HelperInstance.Data.ReadJsonFile<SpawnParameters>(
                "ModData/SpawnParameters.json"
            ) ?? new();
        ModEntry.HelperInstance.Data.WriteJsonFile("ModData/SpawnParameters.json", parameters);
        return parameters;
    }

    private void InitializeLocationPacks()
    {
        foreach (var pack in LocationPacks.Values)
        {
            pack.NumberOfSpawnedEggs = 0;
        }
    }

    private void InitializeSpawnAreas(HashSet<WildAnimalData> loadedWildAnimals)
    {
        string notWinterGSQuery = "!SEASON Winter";
        LocationType[] notExposed =
        {
            LocationType.BugLand,
            LocationType.Island,
            LocationType.DeepIsland,
            LocationType.IslandBeach,
            LocationType.Magical,
            LocationType.Swamp,
            LocationType.Desert,
            LocationType.Cave,
        };

        var loadedWildEggs = _wildEggs
            .Where(e => loadedWildAnimals.Any(a => a.LocationTypes[e.Location]))
            .ToHashSet();

        var eggEntriesToRemove = _wildEggs.Except(loadedWildEggs).Select(e => e.ID);
        SpawnAreas.RemoveWhere(x => eggEntriesToRemove.Contains(x.Key));

        // Iterate through each location pack
        foreach (var (id, pack) in LocationPacks)
        {
            if (!loadedWildEggs.Any())
            {
                ModEntry.LoggerHelper.Log($"There's no wild egg is loaded.", LogLevel.Warn);
            }
            // Iterate through each wild egg
            foreach (var egg in loadedWildEggs)
            {
                // Set egg game locations based on the pack
                SetEggGameLocations(pack.Locations, egg);
                if (!SpawnAreas.ContainsKey(egg.ID))
                {
                    SpawnAreas[egg.ID] = new HashSet<Area>();
                }
                HashSet<string>? packLocations = null;

                // Get the locations associated with the current egg
                if (!pack.Locations.ContainsKey(egg.Location))
                {
                    ModEntry.LoggerHelper.Log(
                        $"Egg location {egg.Location} not found in pack locations."
                    );
                    continue;
                }
                packLocations = pack.Locations[egg.Location];
                ModEntry.LoggerHelper.Log(
                    $"Found {packLocations.Count} locations for egg {egg.ID}."
                );

                // Get existing map names for the current egg
                var existingMapNames = SpawnAreas[egg.ID]
                    .Where(a => a.Location == egg.Location)
                    .Select(a => a.MapName)
                    .ToHashSet();

                ModEntry.LoggerHelper.Log(
                    $"Existing map names for egg {egg.ID}: {string.Join(", ", existingMapNames)}"
                );

                // Iterate through each location name in the pack locations
                foreach (
                    var locationName in packLocations.Where(l => !existingMapNames.Contains(l))
                )
                {
                    // Find similar areas based on map name and location
                    var similarAreas = SpawnAreas
                        .SelectMany(l => l.Value)
                        .Where(a => a.MapName == locationName && a.Location == egg.Location)
                        .ToList();

                    // Create a new area to add
                    var areaToAdd = new Area
                    {
                        MapName = locationName,
                        ModID = id,
                        Location = egg.Location,
                        RandomChance = GetRandomChance(egg),
                        IncludeTerrainTypes = GetTerrainTypes(egg.Location),
                    };
                    if (!notExposed.Contains(egg.Location))
                    {
                        areaToAdd.ExtraConditions.GameStateQuery = notWinterGSQuery;
                    }

                    // If similar areas exist, clone the first one
                    if (similarAreas.Any())
                    {
                        areaToAdd = similarAreas[0].DeepClone();
                    }

                    // Add the new area to the spawn areas
                    SpawnAreas[egg.ID].Add(areaToAdd);
                }

                // Remove areas that are not in the current pack locations for the egg
                var areasToRemove = existingMapNames
                    .Where(m => !packLocations.Contains(m))
                    .ToList();
                SpawnAreas[egg.ID]
                    .RemoveWhere(a =>
                        a.ModID.EqualsIgnoreCase(id) && !packLocations.Contains(a.MapName)
                    );
            }
        }

        // Reset SpawnedEggs for all areas
        foreach (var (key, areas) in SpawnAreas)
        {
            foreach (var area in areas)
            {
                area.SpawnedEggs = 0;
                if (!notExposed.Contains(area.Location))
                {
                    area.ExtraConditions.GameStateQuery = notWinterGSQuery;
                }
            }
        }
        ModEntry.HelperInstance.Data.WriteJsonFile("ModData/FilteredSpawnAreas.json", SpawnAreas);
    }

    private float GetRandomChance(WildEgg egg, bool useDefault = true, float? initialChance = null)
    {
        float multiplier = (egg.IsLarge, egg.IsMagic) switch
        {
            (true, true) => _spawnConfig.RandomChanceModifiers.LargeMagicEggModifier,
            (true, false) => _spawnConfig.RandomChanceModifiers.LargeEggModifier,
            (false, true) => _spawnConfig.RandomChanceModifiers.MagicEggModifier,
            _ => _spawnConfig.RandomChanceModifiers.DefaultModifier,
        };
        float baseChance = 0f;
        if (useDefault || (!useDefault && !initialChance.HasValue))
        {
            baseChance = Parameters!.BaseRandomChance;
        }
        else if (initialChance.HasValue)
        {
            baseChance = initialChance.Value;
        }

        var multiplierLuckAdjusted = DailyLuckHelper.AdjustChanceByDailyLuck(
            multiplier,
            _spawnConfig.DailyLuckModifiers.BaseDailyLuckModifier
        );
        return (float)Math.Clamp(baseChance + multiplierLuckAdjusted, 0, 1);
    }

    public HashSet<string> GetTerrainTypes(LocationType location)
    {
        HashSet<string> terrainTypes = new HashSet<string>();

        switch (location)
        {
            case LocationType.Farm:
            case LocationType.Forest:
            case LocationType.Woods:
            case LocationType.Mountain:
            case LocationType.Town:
            case LocationType.Cliff:
            case LocationType.Clearing:
                terrainTypes.Add("Grass");
                break;
            case LocationType.Beach:
            case LocationType.IslandBeach:
                terrainTypes.Add("Dirt");
                break;
            case LocationType.Island:
            case LocationType.DeepIsland:
            case LocationType.Magical:
            case LocationType.Desert:
            case LocationType.BugLand:
            case LocationType.Cave:
            case LocationType.Swamp:
                // Return an empty HashSet for these locations
                break;
        }

        return terrainTypes; // This will be empty for unhandled cases
    }

    private void SetEggGameLocations(
        Dictionary<LocationType, HashSet<string>> locations,
        WildEgg egg
    )
    {
        if (locations.ContainsKey(egg.Location))
        {
            foreach (
                var location in locations.Where(l => l.Key == egg.Location).SelectMany(x => x.Value)
            )
            {
                if (!egg.GameLocations.Contains(location))
                {
                    egg.GameLocations.Add(location);
                }
            }
        }
    }

    /// <summary>
    /// Saves the final spawn data to a JSON file.
    /// </summary>
    private void SaveFinalLoadedPacksData(bool overwrite = false)
    {
        if (overwrite)
        {
            var backup = ModEntry.HelperInstance.Data.ReadJsonFile<
                Dictionary<string, LocationPack>
            >("ModData/LocationPacks.json");
            if (backup != null)
            {
                ModEntry.HelperInstance.Data.WriteJsonFile(
                    "ModData/LocationPacks.json.bak",
                    LocationPacks
                );
            }
            ModEntry.HelperInstance.Data.WriteJsonFile("ModData/LocationPacks.json", LocationPacks);
        }
        else
        {
            ModEntry.HelperInstance.Data.WriteJsonFile(
                "ModData/FinalLocationPacks.json",
                LocationPacks
            );
        }
    }

    /// <summary>
    /// Saves the final spawn data to a JSON file.
    /// </summary>
    private void SaveFinalSpawnData(bool overwrite = false)
    {
        if (overwrite)
        {
            var backup = ModEntry.HelperInstance.Data.ReadJsonFile<
                Dictionary<string, HashSet<Area>>
            >("ModData/SpawnAreas.json");
            if (backup != null)
            {
                ModEntry.HelperInstance.Data.WriteJsonFile(
                    "ModData/FinalSpawnAreas.json.bak",
                    SpawnAreas
                );
            }
            ModEntry.HelperInstance.Data.WriteJsonFile("ModData/FinalSpawnAreas.json", SpawnAreas);
        }
        else
        {
            ModEntry.HelperInstance.Data.WriteJsonFile("ModData/FinalSpawnAreas.json", SpawnAreas);
        }
    }

    // Method to check if the current time is valid for spawning
    public bool IsAreaSpawnTimeValid(int spawnTime)
    {
        // Assuming SpawnTime is in hours (0-23)
        int currentHour = Game1.timeOfDay / 100; // Get current hour from game time
        return currentHour == spawnTime; // Check if current hour matches the spawn time
    }

    // Method to check if extra conditions are met
    public bool AreAreaExtraConditionsMet(
        ModData modSaveData,
        Area area,
        bool bypassRandom = false,
        bool debug = false
    )
    {
        var extraConditions = area.ExtraConditions;
        // Check Game State Query
        if (!string.IsNullOrEmpty(extraConditions.GameStateQuery))
        {
            bool gameStateCheck = GameStateQuery.CheckConditions(extraConditions.GameStateQuery);
            if (!gameStateCheck)
            {
                if (debug)
                    ModEntry.LoggerHelper.Log(
                        "Game state query condition not met.",
                        LogLevel.Trace
                    );
                return false;
            }
        }

        // Check Random Chance

        double randomValue = Game1.random.NextDouble();
        if (!bypassRandom && randomValue >= area.RandomChance)
        {
            if (debug)
                ModEntry.LoggerHelper.Log(
                    $"Random chance condition not met. Random value: {randomValue}, Required: {area.RandomChance}.",
                    LogLevel.Trace
                );
            return false;
        }

        // Check years
        if (Game1.year <= extraConditions.StartYear)
        {
            //if (debug)
            ModEntry.LoggerHelper.Log(
                $"Year condition not met. Current year: {Game1.year}, Required: {extraConditions.StartYear}.",
                LogLevel.Trace
            );
            return false;
        }

        // Check seasons
        if (
            extraConditions.Seasons.Count > 0
            && !extraConditions.Seasons.Contains(Game1.currentSeason)
        )
        {
            //if (debug)
            ModEntry.LoggerHelper.Log(
                $"Season condition not met. Current season: {Game1.currentSeason}.",
                LogLevel.Trace
            );
            return false;
        }

        // Check days
        if (extraConditions.Days.Count > 0 && !extraConditions.Days.Contains(Game1.dayOfMonth))
        {
            //if (debug)
            ModEntry.LoggerHelper.Log(
                $"Day condition not met. Current day: {Game1.dayOfMonth}.",
                LogLevel.Trace
            );
            return false;
        }

        // Check weather conditions
        string currentWeather = modSaveData.WeatherToday;
        if (
            extraConditions.WeatherToday.Count > 0
            && !extraConditions.WeatherToday.Contains(currentWeather)
        )
        {
            if (debug)
                ModEntry.LoggerHelper.Log(
                    $"Today's weather condition not met. Current weather: {currentWeather}.",
                    LogLevel.Trace
                );
            return false;
        }

        string weatherTomorrow = modSaveData.WeatherTomorrow;
        if (
            extraConditions.WeatherTomorrow.Count > 0
            && !extraConditions.WeatherTomorrow.Contains(weatherTomorrow)
        )
        {
            if (debug)
                ModEntry.LoggerHelper.Log(
                    $"Tomorrow's weather condition not met. Tomorrow's weather: {weatherTomorrow}.",
                    LogLevel.Trace
                );
            return false;
        }

        string weatherYesterday = modSaveData.WeatherYesterday;
        if (
            extraConditions.WeatherYesterday.Count > 0
            && !extraConditions.WeatherYesterday.Contains(weatherYesterday)
        )
        {
            //if (debug)
            ModEntry.LoggerHelper.Log(
                $"Yesterday's weather condition not met. Yesterday's weather: {weatherYesterday}.",
                LogLevel.Trace
            );
            return false;
        }

        if (debug)
            ModEntry.LoggerHelper.Log("All extra conditions are met.", LogLevel.Trace);

        return true; // All conditions are met
    }

    /// <summary>
    /// Spawns eggs in the designated areas.
    /// </summary>
    public void SpawnEggs()
    {
        SpawnedEggsLocations.Clear();
        WorldSpawnedEggsPacks = 0;
        WorldSpawnedEggsVanilla = 0;
        var gameLocationNames = Game1.locations.Select(l => l.Name).ToHashSet();

        MapSpawnedEggs = LocationPacks
            .Values.SelectMany(x => x.Locations.SelectMany(l => l.Value))
            .Distinct()
            .ToDictionary(x => x, y => 0);
        SpawnedEggsByID = SpawnAreas.Keys.ToDictionary(x => x, y => 0);
        SpawnedEggsByType = EnumExtensions.GetValues<EggType>().ToDictionary(x => x, y => 0);
        MaxWorldEggsPacks = DetermineSpawnQuantityWithLuck(
            Parameters!.Parameters_Packs.MinEggs,
            Parameters!.Parameters_Packs.MaxEggs_All
        );

        MaxWorldEggsVanilla = DetermineSpawnQuantityWithLuck(
            Parameters!.Parameters_Vanilla.MinEggs,
            Parameters!.Parameters_Vanilla.MaxEggs_All
        );

        DetermineEggsToSpawn();

        var spawnAreasFlattened = GetValidSpawnAreas(gameLocationNames);

        var certainSpawnAreas = GetCertainSpawnAreas(spawnAreasFlattened);

        var packMaxEggQuantities = GetPackMaxEggQuantities();

        SpawnCertainEggs(certainSpawnAreas, packMaxEggQuantities);

        SpawnRemainingEggs(spawnAreasFlattened, packMaxEggQuantities);

        SaveFinalSpawnData();
        SaveFinalLoadedPacksData();

        PrintSpawnedEggsLocations();
    }

    private void DetermineEggsToSpawn()
    {
        foreach (var area in SpawnAreas.SelectMany(a => a.Value))
        {
            area.EggsToSpawn = DetermineSpawnQuantityWithLuck(area.MinEggs, area.MaxEggs);
        }
    }

    private HashSet<(string Key, Area Area)> GetValidSpawnAreas(HashSet<string> gameLocationNames)
    {
        return SpawnAreas
            .SelectMany(a =>
                a.Value.Where(x =>
                        LocationPacks.ContainsKey(x.ModID) && gameLocationNames.Contains(x.MapName)
                    )
                    .Select(area => (a.Key, Area: area))
            )
            .OrderBy(x => Game1.random.NextDouble() + x.Area.RandomChance)
            .ToHashSet();
    }

    private HashSet<(string Key, Area Area)> GetCertainSpawnAreas(
        HashSet<(string Key, Area Area)> spawnAreasFlattened
    )
    {
        var certainSpawnAreas = new HashSet<(string Key, Area Area)>();

        var certainSpawnAreasVanilla = spawnAreasFlattened
            .Where(x => x.Area.ModID.EqualsIgnoreCase(Strings.Vanilla))
            .Take(Parameters!.Parameters_Vanilla.CertainEggs);

        certainSpawnAreas.AddRange(certainSpawnAreasVanilla);

        var certainSpawnAreasPacks = spawnAreasFlattened
            .Where(x => !x.Area.ModID.EqualsIgnoreCase(Strings.Vanilla))
            .Take(Parameters!.Parameters_Packs.CertainEggs);

        certainSpawnAreas.AddRange(certainSpawnAreasPacks);

        return certainSpawnAreas;
    }

    private Dictionary<string, int> GetPackMaxEggQuantities()
    {
        return LocationPacks.ToDictionary(
            x => x.Key,
            y =>
                DetermineSpawnQuantityWithLuck(
                    y.Value.MinimumSpawnsPerDay,
                    y.Value.MaximumSpawnsPerDay
                )
        );
    }

    private void SpawnCertainEggs(
        HashSet<(string Key, Area Area)> certainSpawnAreas,
        Dictionary<string, int> packMaxEggQuantities
    )
    {
        foreach (var (eggID, area) in certainSpawnAreas)
        {
            var pack = LocationPacks.FirstOrDefault(x => x.Key == area.ModID);
            if (pack.Value == null)
            {
                ModEntry.LoggerHelper.Log(
                    $"Pack not found for ModID: {area.ModID}.",
                    LogLevel.Warn
                );
                continue;
            }

            SpawnEggsInDesignatedArea(
                eggID,
                area,
                pack.Value,
                packMaxEggQuantities[area.ModID],
                bypassRandom: true
            );
        }
    }

    private void SpawnRemainingEggs(
        HashSet<(string Key, Area Area)> spawnAreasFlattened,
        Dictionary<string, int> packMaxEggQuantities
    )
    {
        int maxIterations = Parameters!.MaxSpawningTries * 10;
        maxIterations = 400;
        int iteration = 0;

        while (iteration < maxIterations)
        {
            foreach (var (eggID, area) in spawnAreasFlattened)
            {
                ModEntry.LoggerHelper.Log(
                    $"Spawning eggs for egg ID: {eggID} in map: {area.MapName}",
                    LogLevel.Info
                );

                var pack = LocationPacks.FirstOrDefault(x => x.Key == area.ModID);
                if (pack.Value == null)
                {
                    ModEntry.LoggerHelper.Log(
                        $"Pack not found for ModID: {area.ModID}.",
                        LogLevel.Warn
                    );
                    continue;
                }

                var maxWorldEggs = area.ModID.EqualsIgnoreCase(Strings.Vanilla)
                    ? MaxWorldEggsVanilla
                    : MaxWorldEggsPacks;

                SpawnEggsInDesignatedArea(
                    eggID,
                    area,
                    pack.Value,
                    packMaxEggQuantities[area.ModID],
                    bypassRandom: false
                );
            }
            spawnAreasFlattened.RemoveWhere(a =>
                ShouldAreaBeSkipped(
                    a.Key,
                    a.Area,
                    packMaxEggQuantities,
                    a.Area.ModID.EqualsIgnoreCase(Strings.Vanilla)
                )
            );
            if (spawnAreasFlattened.Count == 0)
            {
                ModEntry.LoggerHelper.Log(
                    $"All areas have reached spawn limits or conditions. Ending spawning loop at iteration {iteration + 1}.",
                    LogLevel.Info
                );
                break;
            }
            iteration++;
            if (iteration >= maxIterations)
            {
                ModEntry.LoggerHelper.Log(
                    $"Max iterations reached while spawning eggs.",
                    LogLevel.Warn
                );
            }
        }
    }

    /// <summary>
    /// Spawns eggs in the specified areas for a given wild egg.
    /// </summary>
    /// <param name="eggID">The ID of the wild egg to spawn.</param>
    /// <param name="area">The area where the egg will be spawned.</param>
    /// <param name="packsEggQuantities">A dictionary containing the quantities of eggs to spawn for each pack.</param>
    private void SpawnEggsInDesignatedArea(
        string eggID,
        Area area,
        LocationPack pack,
        int packEggQuantity,
        bool bypassRandom
    )
    {
        bool vanillaArea = area.ModID.EqualsIgnoreCase(Strings.Vanilla);
        // Check if extra conditions for the area are met
        if (
            ShouldAreaBeSkipped(eggID, area, pack.NumberOfSpawnedEggs, packEggQuantity, vanillaArea)
            || !AreAreaExtraConditionsMet(
                _modSaveData,
                area,
                debug: true,
                bypassRandom: bypassRandom
            )
        )
        {
            LogSkippingArea(area, eggID);
            return; // Exit if conditions are not met
        }

        // Find the location corresponding to the area
        var location = Game1.locations.FirstOrDefault(l => l.Name == area.MapName);
        if (location == null)
        {
            ModEntry.LoggerHelper.Log(
                $"Location not found for area {area.MapName} and egg ID {eggID}.",
                LogLevel.Warn
            );
            return; // Exit if the location is not found
        }

        // Attempt to find a valid tile for spawning the egg
        Vector2? tile = FindValidTile(location, area, Parameters!.MaxSpawningTries, eggID);
        if (tile != null)
        {
            // Add the egg object to the location if a valid tile is found
            if (AddEggObjectToLocation(location, tile.Value, eggID, area))
            {
                UpdateSpawnCounters(area, pack, eggID, tile.Value);
                /*ModEntry.LoggerHelper.Log(
                    $"Successfully spawned egg ID {eggID} in area {area.MapName}.",
                    LogLevel.Info
                );*/
            }
            else
            {
                ModEntry.LoggerHelper.Log(
                    $"Failed to spawn egg ID {eggID} in area {area.MapName}.",
                    LogLevel.Error
                );
            }
            area.HitMaxSpawnTries = false;
        }
        else
        {
            ModEntry.LoggerHelper.Log(
                $"No valid tile found for egg ID {eggID} in area {area.MapName} after {Parameters!.MaxSpawningTries} attempts.",
                LogLevel.Warn
            );
            area.HitMaxSpawnTries = true;
            ModEntry.LoggerHelper.Log($"HitMaxSpawnTries: {area.HitMaxSpawnTries}", LogLevel.Warn);
        }
    }

    private void UpdateSpawnCounters(Area area, LocationPack pack, string eggID, Vector2 tile)
    {
        area.SpawnedEggs++;
        MapSpawnedEggs[area.MapName]++;
        pack.NumberOfSpawnedEggs++;
        SpawnedEggsLocations.Add(new(eggID, area.ModID, area.MapName, tile));
        SpawnedEggsByID[eggID]++;
        SpawnedEggsByType[WildEgg.GetType(eggID)]++;

        // Update world spawn counters based on area type
        if (area.ModID.EqualsIgnoreCase(Strings.Vanilla))
        {
            WorldSpawnedEggsVanilla++;
        }
        else
        {
            WorldSpawnedEggsPacks++;
        }
    }

    private bool ShouldAreaBeSkipped(
        string eggID,
        Area area,
        Dictionary<string, int> packMaxEggQuantities,
        bool vanillaArea,
        bool logDecision = false
    )
    {
        var pack = LocationPacks.FirstOrDefault(x => x.Key == area.ModID);
        if (pack.Value == null)
        {
            ModEntry.LoggerHelper.Log($"Pack not found for ModID: {area.ModID}.", LogLevel.Warn);
            return true;
        }
        return ShouldAreaBeSkipped(
            eggID,
            area,
            pack.Value.NumberOfSpawnedEggs,
            packMaxEggQuantities[area.ModID],
            vanillaArea,
            logDecision
        );
    }

    private bool ShouldAreaBeSkipped(
        string eggID,
        Area area,
        int packSpawnedEggs,
        int packMaxEggQuantity,
        bool vanillaArea,
        bool logDecision = false
    )
    {
        var maxWorldEggs = vanillaArea ? MaxWorldEggsVanilla : MaxWorldEggsPacks;
        var spawnedWorldEggs = vanillaArea ? WorldSpawnedEggsVanilla : WorldSpawnedEggsPacks;

        var eggType = WildEgg.GetType(eggID);
        var isMagicEgg = new[] { EggType.magicWildEgg, EggType.largeMagicWildEgg }.Contains(
            eggType
        );
        var isLargeEgg = new[] { EggType.largeWildEgg, EggType.largeMagicWildEgg }.Contains(
            eggType
        );

        (int Max, int Current) spawnedEggsType = GetSpawnedEggsType(
            area,
            eggType,
            isMagicEgg,
            isLargeEgg
        );

        string reason = GetSkipReason(
            area,
            WildEgg.GetType(eggID),
            packSpawnedEggs,
            packMaxEggQuantity,
            spawnedWorldEggs,
            maxWorldEggs,
            spawnedEggsType,
            isMagicEgg,
            isLargeEgg
        );

        bool shouldSkip = !string.IsNullOrEmpty(reason);

        if (logDecision)
        {
            LogSkipDecision(
                area,
                shouldSkip,
                reason,
                packSpawnedEggs,
                packMaxEggQuantity,
                spawnedWorldEggs,
                maxWorldEggs
            );
        }

        return shouldSkip;
    }

    private (int Max, int Current) GetSpawnedEggsType(
        Area area,
        EggType eggType,
        bool isMagicEgg,
        bool isLargeEgg
    )
    {
        if (isMagicEgg)
        {
            return (
                Parameters!.MaxEggPerMap_Magic,
                GetEggsInThisLocationByType(area.MapName, true)
            );
        }
        else if (isLargeEgg)
        {
            return (
                Parameters!.MaxEggPerMap_Large,
                GetEggsInThisLocationByType(area.MapName, false)
            );
        }
        return (0, 0);
    }

    private string GetSkipReason(
        Area area,
        EggType eggType,
        int packSpawnedEggs,
        int packMaxEggQuantity,
        int spawnedWorldEggs,
        int maxWorldEggs,
        (int Max, int Current) spawnedEggsType,
        bool isMagicEgg,
        bool isLargeEgg
    )
    {
        if (packMaxEggQuantity == 0)
            return $"No eggs to spawn for pack {area.ModID}.";
        if (area.EggsToSpawn == 0)
            return $"No eggs to spawn for area {area.ModID}-{area.MapName}.";
        if (isMagicEgg && spawnedEggsType.Current >= spawnedEggsType.Max)
            return $"Maximum spawned eggs reached for magic eggs. Max: {spawnedEggsType.Max}; Current: {spawnedEggsType.Current}.";
        if (isLargeEgg && spawnedEggsType.Current >= spawnedEggsType.Max)
            return $"Maximum spawned eggs reached for large eggs. Max: {spawnedEggsType.Max}; Current: {spawnedEggsType.Current}.";
        if (area.HitMaxSpawnTries)
            return "Maximum spawn tries reached.";
        if (SpawnedEggsByType[eggType] >= Parameters!.MaxWorldEggs[eggType])
            return $"Maximum spawned eggs of type {eggType} reached for world. Max: {Parameters!.MaxWorldEggs[eggType]}; Current: {SpawnedEggsByType[eggType]}.";
        if (spawnedWorldEggs >= maxWorldEggs)
            return $"Maximum spawned eggs reached for world. Max: {maxWorldEggs}; Current: {spawnedWorldEggs}.";
        if (packSpawnedEggs >= packMaxEggQuantity)
            return $"Maximum spawned eggs reached for pack {area.ModID}. Max: {packMaxEggQuantity}; Current: {packSpawnedEggs}.";
        if (area.SpawnedEggs >= area.EggsToSpawn)
            return $"Maximum spawned eggs reached for area {area.ModID} - {area.MapName}. Max: {area.EggsToSpawn}; Current: {area.SpawnedEggs}.";

        var eggsInThisLocation = GetEggsInThisLocation(area.MapName);
        if (eggsInThisLocation >= Parameters!.MaxEggPerMap_Any)
            return $"Maximum spawned eggs reached for location {area.MapName}. Max: {Parameters!.MaxEggPerMap_Any}; Current: {eggsInThisLocation}.";

        return string.Empty; // No reason to skip
    }

    private void LogSkipDecision(
        Area area,
        bool shouldSkip,
        string reason,
        int packSpawnedEggs,
        int packMaxEggQuantity,
        int spawnedWorldEggs,
        int maxWorldEggs
    )
    {
        if (shouldSkip)
        {
            ModEntry.LoggerHelper.Log(
                $"[Skip] Area '{area.MapName}' (ModID: {area.ModID}) skipped. Reason: {reason}. "
                    + $"Spawned Eggs: {area.SpawnedEggs}/{area.EggsToSpawn}, "
                    + $"Pack Spawned Eggs: {packSpawnedEggs}/{packMaxEggQuantity}, "
                    + $"Total World Spawned Eggs: {spawnedWorldEggs}/{maxWorldEggs}",
                LogLevel.Debug
            );
        }
        else
        {
            ModEntry.LoggerHelper.Log(
                $"[Eligible] Area '{area.MapName}' (ModID: {area.ModID}) is eligible for spawning. "
                    + $"Spawned Eggs: {area.SpawnedEggs}/{area.EggsToSpawn}, "
                    + $"Pack Spawned Eggs: {packSpawnedEggs}/{packMaxEggQuantity}, "
                    + $"Total World Spawned Eggs: {spawnedWorldEggs}/{maxWorldEggs}",
                LogLevel.Debug,
                false
            );
        }
    }

    private int GetEggsInThisLocation(string location, string? eggID = null)
    {
        var contextTag = "wildegg_item";
        var gameLocation = Game1.locations.FirstOrDefault(l => l.Name.EqualsIgnoreCase(location));
        if (gameLocation == null)
        {
            return 0;
        }
        var quantity = string.IsNullOrEmpty(eggID)
            ? gameLocation.Objects.SelectMany(o => o.Values).Count(o => o.HasContextTag(contextTag))
            : gameLocation.Objects.SelectMany(o => o.Values).Count(o => o.ItemId == eggID);
        return quantity;
    }

    private int GetEggsInThisLocationByType(string location, bool magic)
    {
        var gameLocation = Game1.locations.FirstOrDefault(l => l.Name.EqualsIgnoreCase(location));
        if (gameLocation == null)
        {
            return 0;
        }
        var contextTags = magic switch
        {
            true => new string[]
            {
                $"wildegg_{EggType.magicWildEgg}",
                $"wildegg_{EggType.largeMagicWildEgg}",
            },
            false => new string[]
            {
                $"wildegg_{EggType.largeWildEgg}",
                $"wildegg_{EggType.largeMagicWildEgg}",
            },
        };
        return gameLocation
                .Objects.SelectMany(o => o.Values)
                .Count(o => o.HasContextTag(contextTags[0]))
            + gameLocation
                .Objects.SelectMany(o => o.Values)
                .Count(o => o.HasContextTag(contextTags[1]));
    }

    /// <summary>
    /// Adds the egg object to the specified location at the given tile.
    /// </summary>
    /// <param name="location">The game location where the egg will be added.</param>
    /// <param name="tile">The tile location where the egg will be placed.</param>
    /// <param name="eggID">The ID of the egg to be added.</param>
    /// <param name="area">The area where the egg is being added.</param>
    /// <returns>True if the egg was successfully added; otherwise, false.</returns>
    private bool AddEggObjectToLocation(
        GameLocation location,
        Vector2 tile,
        string eggID,
        Area area
    )
    {
        // Create a new egg object
        var eggObject = new StardewValley.Object(eggID, 1)
        {
            Location = location,
            TileLocation = tile,
            IsSpawnedObject = true, // Allow "normal" forage behavior if applicable
            destroyOvernight = true,
        };

        // Attempt to add the egg object to the location's objects
        if (location.objects.TryAdd(tile, eggObject))
        {
            ModEntry.LoggerHelper.Log(
                $"Successfully added egg object at {tile} for egg ID {eggID} in area {area.MapName}.",
                LogLevel.Info
            );
            return true;
        }
        else
        {
            ModEntry.LoggerHelper.Log(
                $"Failed to add egg object at {tile} for egg ID {eggID} in area {area.MapName}.",
                LogLevel.Error
            );
            return false;
        }
    }

    /// <summary>
    /// Determines the quantity of eggs to spawn based on the specified range.
    /// </summary>
    private static int DetermineSpawnQuantity((int min, int max) range)
    {
        int min = range.min;
        int max = range.max;
        return DetermineSpawnQuantity(min, max);
    }

    /// <summary>
    /// Determines the quantity of eggs to spawn based on the specified range.
    /// </summary>
    private static int DetermineSpawnQuantity(int min, int max)
    {
        int quantity;
        if (min == max)
        {
            quantity = min;
        }
        else
        {
            if (min > max)
            {
                int temp = min;
                min = max;
                max = temp;
            }
            quantity = Game1.random.Next(min, max + 1);
        }

        return Math.Max(quantity, 0);
    }

    /// <summary>
    /// Determines the quantity of eggs to spawn based on the specified range,
    /// adjusted by daily luck.
    /// </summary>
    public int DetermineSpawnQuantityWithLuck(int min, int max, double dailyLuckModifier = 0.02)
    {
        // Adjust min and max by daily luck modifier using AdjustChanceByDailyLuck
        double luckAdjustedMin = AdjustChanceByDailyLuck(min / 100.0, dailyLuckModifier) * 100;
        double luckAdjustedMax = AdjustChanceByDailyLuck(max / 100.0, dailyLuckModifier) * 100;

        // Convert adjusted values to int, ensuring min <= max
        int adjustedMin = (int)Math.Floor(Math.Min(luckAdjustedMin, luckAdjustedMax));
        int adjustedMax = (int)Math.Ceiling(Math.Max(luckAdjustedMin, luckAdjustedMax));

        // Clamp to non-negative values
        adjustedMin = Math.Max(adjustedMin, 0);
        adjustedMax = Math.Max(adjustedMax, 0);

        return DetermineSpawnQuantity(adjustedMin, adjustedMax);
    }

    public double AdjustChanceByDailyLuck(double chance, double dailyLuckModifier)
    {
        var luckAdjustment = DailyLuckHelper.GetDailyLuck() switch
        {
            LuckyTypes.SuperUnlucky => _spawnConfig.DailyLuckModifiers.SuperUnluckyModifier
                * dailyLuckModifier,
            LuckyTypes.Neutral => _spawnConfig.DailyLuckModifiers.NeutralModifier,
            LuckyTypes.Unlucky => _spawnConfig.DailyLuckModifiers.UnluckyModifier
                * dailyLuckModifier,
            LuckyTypes.Lucky => _spawnConfig.DailyLuckModifiers.LuckyModifier * dailyLuckModifier,
            LuckyTypes.SuperLucky => _spawnConfig.DailyLuckModifiers.SuperLuckyModifier
                * dailyLuckModifier,
            _ => _spawnConfig.DailyLuckModifiers.NeutralModifier, // Default case (unknown luck type)
        };
        var finalChance = chance + luckAdjustment;
        return Math.Clamp(finalChance, 0, 1);
    }

    /// <summary>
    /// Logs the determined spawn quantity for the area.
    /// </summary>
    private void LogSpawnQuantity(Area area, WildEgg egg, int quantity)
    {
        ModEntry.LoggerHelper.Log(
            $"In area {area.MapName}, for egg ID {egg.ID}, quantity is {quantity}.",
            LogLevel.Trace
        );
    }

    public void PrintSpawnedEggsLocations(int? option = null, LogLevel logLevel = LogLevel.Trace)
    {
        // Check if there are any spawned eggs locations to display
        if (SpawnedEggsLocations == null || !SpawnedEggsLocations.Any())
        {
            ModEntry.LoggerHelper.Log("No spawned eggs locations to display.", LogLevel.Warn);
            return;
        }

        // Log the total number of spawned eggs
        ModEntry.LoggerHelper.Log(
            $"Spawned a total of {SpawnedEggsLocations.Count} eggs. Daily Luck {DailyLuckHelper.GetDailyLuck()}",
            logLevel
        );

        // Handle different logging options
        switch (option)
        {
            case 1:
                LogEggsByType(logLevel);
                break;
            case 2:
                LogEggsByMap(logLevel);
                break;
            case null:
            case 3:
                LogEggsByMod(logLevel);
                break;
            case 4:
                LogEggsByType(logLevel);
                LogEggsByMap(logLevel);
                LogEggsByMod(logLevel);
                break;
            default:
                ModEntry.LoggerHelper.Log("Invalid option provided.", LogLevel.Warn);
                break;
        }
    }

    private void LogEggsByType(LogLevel logLevel)
    {
        ModEntry.LoggerHelper.Log("\nBy Egg:", logLevel);
        foreach (var (eggID, quantity) in SpawnedEggsByID.OrderBy(x => x.Value))
        {
            ModEntry.LoggerHelper.Log($"{WildEgg.GetName(eggID)}: {quantity}", logLevel);
        }
    }

    private void LogEggsByMap(LogLevel logLevel)
    {
        ModEntry.LoggerHelper.Log("\nBy Map:", logLevel);
        foreach (var (map, quantity) in MapSpawnedEggs.Where(x => x.Value > 0))
        {
            ModEntry.LoggerHelper.Log($"{map}: {quantity}", logLevel);
        }
    }

    private void LogEggsByMod(LogLevel logLevel)
    {
        // Grouping by ModID
        var groupedEggs = SpawnedEggsLocations
            .GroupBy(eggInfo => eggInfo.ModID)
            .ToDictionary(group => group.Key, group => group.ToList());

        ModEntry.LoggerHelper.Log("\nBy Mod:", logLevel);

        void LogModGroup(string modID, List<SpawnedEggInfo> eggs)
        {
            ModEntry.LoggerHelper.Log("\n---------------------------\n", logLevel);
            ModEntry.LoggerHelper.Log($"Mod ID: {modID}. Total Eggs: {eggs.Count}", logLevel);
            foreach (var eggInfo in eggs)
            {
                string logMessage =
                    $"  Egg ID: {eggInfo.Id}, Map: {eggInfo.MapName}, Location: ({eggInfo.Tile.X}, {eggInfo.Tile.Y})";
                ModEntry.LoggerHelper.Log(logMessage, logLevel);
            }
        }

        // Print Vanilla group first if it exists
        if (groupedEggs.TryGetValue("Vanilla", out var vanillaGroup))
        {
            LogModGroup("Vanilla", vanillaGroup);
        }

        // Print other groups except Vanilla
        foreach (var modGroup in groupedEggs.Where(g => g.Key != "Vanilla"))
        {
            LogModGroup(modGroup.Key, modGroup.Value);
        }
    }

    /// <summary>
    /// Logs the reason for skipping an area.
    /// </summary>
    private void LogSkippingArea(Area area, string eggID)
    {
        string reason = "due to unmet conditions.";
        ModEntry.LoggerHelper.Log(
            $"Skipping area {area.MapName} for egg ID {eggID} {reason}",
            LogLevel.Trace
        );
    }

    /// <summary>
    /// Finds a valid tile for spawning the egg within the specified location and area.
    /// </summary>
    private Vector2? FindValidTile(
        GameLocation location,
        Area area,
        int maxAttempts,
        string eggID = ""
    )
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var tile = TileUtility.GetRandomTile(
                location,
                area.IncludeTerrainTypes,
                area.ExcludeTerrainTypes,
                RectangleConverter.ConvertToRectangleHashSet(area.IncludeCoordinates),
                RectangleConverter.ConvertToRectangleHashSet(area.ExcludeCoordinates)
            );

            if (tile != null)
            {
                ModEntry.LoggerHelper.Log(
                    $"Found valid tile at {tile.Value} for egg ID {eggID} in area {area.MapName}.",
                    LogLevel.Trace
                );
                return tile;
            }
        }

        ModEntry.LoggerHelper.Log(
            $"No valid tile found for egg ID {eggID} in area {area.MapName} after {maxAttempts} attempts.",
            LogLevel.Warn
        );
        return null;
    }

    internal void ApplyChanceModifier()
    {
        foreach (var (key, areas) in SpawnAreas)
        {
            foreach (var area in areas)
            {
                var wildEgg = _wildEggs.FirstOrDefault(e => e.ID == key);
                if (wildEgg != null)
                {
                    //area.RandomChance = Parameters!.BaseRandomChance;
                    area.RandomChance = GetRandomChance(
                        wildEgg,
                        area.UseBaseRandomChance,
                        area.RandomChance
                    );
                }
            }
        }
    }

    internal void SetMinEggPack()
    {
        foreach (var pack in LocationPacks.Values)
        {
            var parameters = pack.ModID.EqualsIgnoreCase(Strings.Vanilla)
                ? Parameters!.Parameters_Vanilla
                : Parameters!.Parameters_Packs;
            if (parameters.UseJsonValues)
            {
                pack.MinimumSpawnsPerDay = parameters.MinEggs;
            }
        }
    }

    internal void SetMaxEggPack()
    {
        foreach (var pack in LocationPacks.Values)
        {
            var parameters = pack.ModID.EqualsIgnoreCase(Strings.Vanilla)
                ? Parameters!.Parameters_Vanilla
                : Parameters!.Parameters_Packs;
            if (parameters.MaxEggPack.HasValue && parameters.UseJsonValues)
            {
                pack.MaximumSpawnsPerDay = parameters.MaxEggPack.Value;
            }
        }
    }

    internal void SetMaxEggArea()
    {
        foreach (var area in SpawnAreas.SelectMany(a => a.Value))
        {
            var parameters = area.ModID.EqualsIgnoreCase(Strings.Vanilla)
                ? Parameters!.Parameters_Vanilla
                : Parameters!.Parameters_Packs;
            if (parameters.MaxEggArea.HasValue && parameters.UseJsonValues)
            {
                area.MaxEggs = parameters.MaxEggArea.Value;
            }
        }
    }

    internal void SetMinEggArea()
    {
        foreach (var area in SpawnAreas.SelectMany(a => a.Value))
        {
            var parameters = area.ModID.EqualsIgnoreCase(Strings.Vanilla)
                ? Parameters!.Parameters_Vanilla
                : Parameters!.Parameters_Packs;
            if (parameters.MinEggArea.HasValue && parameters.UseJsonValues)
            {
                area.MinEggs = parameters.MinEggArea.Value;
            }
        }
    }
}
