using System;
using System.Collections.Generic;
using System.Linq;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Core.Models;
using WildEggsHatching.Infrastructure.Config;
using WildEggsHatching.Infrastructure.Logging;
using WildEggsHatching.Infrastructure.ModData;

namespace WildEggsHatching.Application.Incubation;

public sealed class WildEggHatchService
{
    private readonly AnimalCatalog _catalog;
    private readonly IConfigProvider _config;
    private readonly ILog _log;

    public WildEggHatchService(AnimalCatalog catalog, IConfigProvider config, ILog log)
    {
        _catalog = catalog;
        _config = config;
        _log = log;
    }

    public string? PickWildAnimalKey(LocationType locationType, EggKind eggKind, Random rng)
    {
        var candidates = _catalog
            .All()
            .Where(a => Matches(a, locationType, eggKind))
            .Select(a => a.WildAnimalKey)
            .ToList();

        if (candidates.Count == 0)
            return null;

        return candidates[rng.Next(candidates.Count)];
    }

    public string? PickFarmAnimalType(string wildAnimalKey, Random rng)
    {
        if (!_catalog.TryGet(wildAnimalKey, out var wild))
            return null;

        var weighted = wild.FarmAnimalIdsWeighted.Select(x => (Id: x.Id, BaseWeight: x.Weight)).ToList();
        var same = wild.FarmAnimalIdsSameWeight.Select(id => (Id: id, BaseWeight: 1.0d)).ToList();

        var originalOrdered = same.Select(x => x.Id).Concat(weighted.Select(x => x.Id)).ToList();

        var choices = new List<(string Id, double Weight)>();
        foreach (var entry in same.Concat(weighted))
        {
            var w = ResolveOverrideWeight(wildAnimalKey, entry.Id, entry.BaseWeight);
            if (w > 0)
                choices.Add((entry.Id, w));
        }

        if (choices.Count == 0)
        {
            var pickFirst = originalOrdered.FirstOrDefault();
            _log.Log($"Weights override eliminated all candidates for wild '{wildAnimalKey}'. Picking first='{pickFirst ?? "<none>"}'.", LogLevel.Warn);
            return pickFirst;
        }

        var total = choices.Sum(x => x.Weight);
        var roll = rng.NextDouble() * total;
        foreach (var (id, w) in choices)
        {
            roll -= w;
            if (roll <= 0)
                return id;
        }

        return choices[choices.Count - 1].Id;
    }

    private bool Matches(WildAnimalDefinition animal, LocationType locationType, EggKind eggKind)
    {
        if (!animal.LocationTypes.TryGetValue(locationType, out var allowed) || !allowed)
            return false;

        if (eggKind.IsMagic() != animal.IsMagic)
            return false;

        if (eggKind.IsLarge() != animal.LargeEgg)
            return false;

        return true;
    }

    private double ResolveOverrideWeight(string wildKey, string farmId, double baseWeight)
    {
        var key = $"{wildKey}:{farmId}";
        var map = _config.Config.ChosenWeights;
        if (map.TryGetValue(key, out var overrideWeight))
            return overrideWeight;

        return baseWeight;
    }
}
