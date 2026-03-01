using System;
using System.Collections.Generic;
using System.Linq;
using EggsInWild.Core.Enums;
using EggsInWild.Core.Models;
using EggsInWild.Infrastructure.ModData.Models;
using EggsInWild.Infrastructure.Mods;
using Mikew.NestCore.Helpers.Logging;
using LogScopedHelper = Mikew.NestCore.Helpers.Logging.LogScopedHelper;
using static Mikew.NestCore.Helpers.Logging.ScopedLogCatalog;

namespace EggsInWild.Infrastructure.ModData;

/// <summary>
/// Builds a filtered catalog from WildAnimalsList JSON based on installed mods.
/// This catalog is currently used for diagnostics and future UI scenarios.
/// </summary>
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
            var ids = FilterFarmAnimalIds(wa.ExtraModAnimalKeys);

            dict[wildAnimalKey] = new WildAnimalDefinition(
                WildAnimalKey: wildAnimalKey,
                LargeEgg: wa.LargeEgg,
                Price: wa.Price,
                IsMagic: wa.IsMagic,
                LocationTypes: locations,
                FarmAnimalIds: ids);
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

    private IReadOnlyList<string> FilterFarmAnimalIds(Dictionary<string, ExtraModAnimalKeysJson> modGroups)
    {
        var ids = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (modId, keys) in modGroups)
        {
            if (!_mods.IsLoaded(modId))
                continue;

            foreach (var id in keys.GetEffectiveFarmAnimalKeys())
            {
                if (seen.Add(id))
                    ids.Add(id);
            }
        }

        if (ids.Count == 0)
        {
            LogScopedHelper.Log(
                _log,
                ScopedLogCatalog.ModWeCategory.SaveData,
                "Animal entry had no farm animal IDs for loaded mods; it will never spawn a farm animal.",
                StardewModdingAPI.LogLevel.Trace);
        }

        return ids;
    }
}
