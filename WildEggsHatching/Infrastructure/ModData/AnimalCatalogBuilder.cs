using System;
using System.Collections.Generic;
using System.Linq;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Core.Models;
using Mikew.BirdsOfAFeather.Helpers.Logging;
using WildEggsHatching.Infrastructure.ModData.Models;
using WildEggsHatching.Infrastructure.Mods;

namespace WildEggsHatching.Infrastructure.ModData;

public sealed class AnimalCatalogBuilder
{
    private readonly IModAvailability _mods;
    private readonly ILog _log;

    public AnimalCatalogBuilder(IModAvailability mods, ILog log)
    {
        _mods = mods;
        _log = log;
    }

    public AnimalCatalog Build(IReadOnlyDictionary<string, WildAnimalJson> json)
    {
        var dict = new Dictionary<string, WildAnimalDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (var (wildAnimalKey, wa) in json)
        {
            var locations = BuildLocationMap(wa.LocationTypes);

            var (sameWeight, weighted) = FilterFarmAnimalIds(wa.ExtraModAnimalKeys);

            dict[wildAnimalKey] = new WildAnimalDefinition(
                WildAnimalKey: wildAnimalKey,
                LargeEgg: wa.LargeEgg,
                Price: wa.Price,
                IsMagic: wa.IsMagic,
                LocationTypes: locations,
                FarmAnimalIdsSameWeight: sameWeight,
                FarmAnimalIdsWeighted: weighted);
        }

        return new AnimalCatalog(dict);
    }

    private IReadOnlyDictionary<LocationType, bool> BuildLocationMap(Dictionary<string, bool> raw)
    {
        var map = new Dictionary<LocationType, bool>();
        foreach (LocationType lt in Enum.GetValues(typeof(LocationType)))
        {
            var key = lt.ToString();
            map[lt] = raw.TryGetValue(key, out var allowed) && allowed;
        }
        return map;
    }

    private (IReadOnlyList<string> sameWeight, IReadOnlyList<WeightedFarmAnimalId> weighted) FilterFarmAnimalIds(
        Dictionary<string, ExtraModAnimalKeysJson> modGroups)
    {
        var same = new List<string>();
        var weighted = new List<WeightedFarmAnimalId>();

        foreach (var (modId, keys) in modGroups)
        {
            if (!_mods.IsLoaded(modId))
                continue;

            keys.FarmAnimalKeysSameWeight ??= new List<string>();
            keys.FarmAnimalKeysWeighted ??= new List<WeightedIdJson>();

            same.AddRange(keys.FarmAnimalKeysSameWeight.Where(s => !string.IsNullOrWhiteSpace(s)));

            foreach (var w in keys.FarmAnimalKeysWeighted)
            {
                if (string.IsNullOrWhiteSpace(w.Id) || w.Weight <= 0)
                    continue;

                weighted.Add(new WeightedFarmAnimalId(w.Id, w.Weight));
            }
        }

        if (same.Count == 0 && weighted.Count == 0)
            _log.Log("Animal entry had no farm animal IDs for loaded mods; it will never spawn a farm animal.", StardewModdingAPI.LogLevel.Trace);

        return (same, weighted);
    }
}
