using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using StardewValley;
using StardewValley.Locations;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Infrastructure.Logging;

namespace WildEggsHatching.Infrastructure.Harmony;

internal static class IncubatorPatches
{
    private static Func<string, (EggKind Kind, LocationType Location)>? _eggClassifier;
    private static Func<LocationType, EggKind, Random, string?>? _pickWildAnimalKey;
    private static Func<string, Random, string?>? _pickFarmAnimalType;
    private static Func<string, bool>? _isWildEggItemId;
    private static ILog? _log;

    public static void Install(
        Harmony harmony,
        Func<string, bool> isWildEggItemId,
        Func<string, (EggKind Kind, LocationType Location)> eggClassifier,
        Func<LocationType, EggKind, Random, string?> pickWildAnimalKey,
        Func<string, Random, string?> pickFarmAnimalType,
        ILog log)
    {
        _isWildEggItemId = isWildEggItemId;
        _eggClassifier = eggClassifier;
        _pickWildAnimalKey = pickWildAnimalKey;
        _pickFarmAnimalType = pickFarmAnimalType;
        _log = log;

        var method = AccessTools.Method(typeof(StardewValley.Object), "OutputIncubator");
        if (method is null)
        {
            log.Log("IncubatorPatches: failed to find StardewValley.Object.OutputIncubator to patch.", LogLevel.Warn);
            return;
        }

        harmony.Patch(method, prefix: new HarmonyMethod(typeof(IncubatorPatches), nameof(Prefix_OutputIncubator)));
    }

    private static bool Prefix_OutputIncubator(object __instance, ref Item? __result, object[] __args)
    {
        try
        {
            if (_isWildEggItemId is null || _eggClassifier is null || _pickWildAnimalKey is null || _pickFarmAnimalType is null || _log is null)
                return true;

            if (__instance is not StardewValley.Object machine)
                return true;

            // Only intercept our incubator machines (big-craftable with WE item id).
            var machineId = TryGetQualifiedItemId(machine);
            if (machineId is null || !machineId.Contains("Mikew.wildeggshatching_WildEggIncubator", StringComparison.OrdinalIgnoreCase))
                return true;

            var input = __args.OfType<Item>().FirstOrDefault(i => !ReferenceEquals(i, machine));
            var inputId = TryGetQualifiedItemId(input);
            if (inputId is null || !_isWildEggItemId(inputId))
                return true;

            var (kind, locationType) = _eggClassifier(inputId);

            var rng = Game1.random;
            var wildKey = _pickWildAnimalKey(locationType, kind, rng);
            if (string.IsNullOrWhiteSpace(wildKey))
            {
                _log.Log($"IncubatorPatches: no wild animal candidates for egg '{inputId}' ({locationType}, {kind}).", LogLevel.Warn);
                return true;
            }

            var farmType = _pickFarmAnimalType(wildKey!, rng);
            if (string.IsNullOrWhiteSpace(farmType))
            {
                _log.Log($"IncubatorPatches: no farm-animal candidates for wild key '{wildKey}'.", LogLevel.Warn);
                return true;
            }

            var location = __args.OfType<GameLocation>().FirstOrDefault() ?? machine.Location;
            if (location is not AnimalHouse house)
            {
                _log.Log($"IncubatorPatches: incubator used outside an AnimalHouse (loc='{location?.NameOrUniqueName}'). Falling back to vanilla.", LogLevel.Warn);
                return true;
            }

            if (!TrySpawnFarmAnimal(house, farmType!, out var error))
            {
                _log.Log($"IncubatorPatches: failed to spawn animal '{farmType}' for wild '{wildKey}': {error}. Falling back to vanilla.", LogLevel.Warn);
                return true;
            }

            // We performed the side-effect; do not produce an output item.
            __result = null;
            return false;
        }
        catch (Exception ex)
        {
            _log?.Log($"IncubatorPatches: exception in OutputIncubator prefix: {ex}", LogLevel.Error);
            return true;
        }
    }

    private static string? TryGetQualifiedItemId(Item? item)
    {
        if (item is null)
            return null;

        try
        {
            // 1.6: Item.QualifiedItemId
            var prop = item.GetType().GetProperty("QualifiedItemId");
            if (prop?.GetValue(item) is string s && !string.IsNullOrWhiteSpace(s))
                return s;
        }
        catch
        {
            // ignore
        }

        try
        {
            // Some older paths use ItemId or Name; try ItemId.
            var prop = item.GetType().GetProperty("ItemId");
            if (prop?.GetValue(item) is string s && !string.IsNullOrWhiteSpace(s))
                return s;
        }
        catch
        {
            // ignore
        }

        return null;
    }

    private static bool TrySpawnFarmAnimal(AnimalHouse house, string animalType, out string error)
    {
        error = string.Empty;
        try
        {
            var building = house.buildingOfIndoors;
            if (building is null)
            {
                error = "buildingOfIndoors is null";
                return false;
            }

            var newId = Game1.multiplayer.getNewID();
            var owner = Game1.player.UniqueMultiplayerID;

            var animal = TryCreateFarmAnimal(animalType, newId, owner, out error);
            if (animal is null)
                return false;

            animal.home = building;
            animal.homeLocation.Value = building.tileX.Value + "," + building.tileY.Value;
            animal.setRandomPosition(house);
            house.animals.Add(animal.myID.Value, animal);
            house.animalsThatLiveHere.Add(animal.myID.Value);

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static FarmAnimal? TryCreateFarmAnimal(string type, long id, long ownerId, out string error)
    {
        error = string.Empty;

        try
        {
            // Typical ctor: FarmAnimal(string type, long id, long ownerId)
            return (FarmAnimal?)Activator.CreateInstance(typeof(FarmAnimal), type, id, ownerId);
        }
        catch
        {
            // ignore; try other ctor shapes.
        }

        try
        {
            // Some versions: FarmAnimal(string type, long id, long ownerId, long homeId)
            return (FarmAnimal?)Activator.CreateInstance(typeof(FarmAnimal), type, id, ownerId, -1L);
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return null;
        }
    }
}
