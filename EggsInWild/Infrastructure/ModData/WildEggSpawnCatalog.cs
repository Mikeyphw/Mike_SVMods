using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewValley.GameData.FarmAnimals;
using EggsInWild.Application.Animals;
using EggsInWild.Core.Enums;
using EggsInWild.Infrastructure.ModData.Models;
using Mikew.NestCore.Helpers.Logging;

namespace EggsInWild.Infrastructure.ModData;

public sealed record WildEggAnimalCandidateBucket(
    string WildAnimalKey,
    IReadOnlyList<string> CandidateFarmAnimalKeys,
    IReadOnlyList<string> MissingFarmAnimalKeys);

public sealed record WildEggSpawnDefinition(
    string EggItemId,
    string LocationToken,
    EggKind Kind,
    IReadOnlyList<SpawnAreaRuleJson>? RawRules,
    IReadOnlyList<SpawnAreaRuleJson> Rules,
    IReadOnlyList<WildEggAnimalCandidateBucket> EligibleWildAnimals)
{
    public bool HasUsableRules => Rules.Count > 0;
    public bool HasValidCandidates => EligibleWildAnimals.Count > 0;
}

public sealed record WildEggSpawnCatalog(
    IReadOnlyList<WildEggSpawnDefinition> DeclaredEggDefinitions,
    IReadOnlyList<WildEggSpawnDefinition> ActiveEggDefinitions);

public static class WildEggSpawnCatalogBuilder
{
    private static readonly EggKind[] AllKinds =
    {
        EggKind.Wild,
        EggKind.Magic,
        EggKind.LargeWild,
        EggKind.LargeMagic,
    };

    public static WildEggSpawnCatalog Build(
        IReadOnlyDictionary<string, WildEggSpawnLocationJson> definitions,
        IReadOnlyDictionary<string, WildAnimalJson> wildAnimals,
        IReadOnlyDictionary<string, FarmAnimalData> farmAnimals,
        IModRegistry registry,
        ILog log)
    {
        var declared = new List<WildEggSpawnDefinition>();

        foreach (var (locationToken, locationDefinition) in definitions.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            foreach (var kind in AllKinds)
            {
                var rawRules = GetRawRules(locationDefinition, kind);
                var rules = NormalizeRules(locationToken, kind, rawRules);
                var eligibleWild = WildEggCandidateResolver.GetEligibleWildCandidates(locationToken, kind, wildAnimals, farmAnimals, registry, log);

                declared.Add(new WildEggSpawnDefinition(
                    EggItemId: WildEggSpawnDefinitionEggIds.BuildEggItemId(locationToken, kind),
                    LocationToken: locationToken,
                    Kind: kind,
                    RawRules: rawRules,
                    Rules: rules,
                    EligibleWildAnimals: eligibleWild));
            }
        }

        var active = declared
            .Where(def => def.HasValidCandidates)
            .ToList();

        return new WildEggSpawnCatalog(
            DeclaredEggDefinitions: declared,
            ActiveEggDefinitions: active);
    }

    private static IReadOnlyList<SpawnAreaRuleJson>? GetRawRules(WildEggSpawnLocationJson? locationDefinition, EggKind kind)
    {
        if (locationDefinition is null)
            return null;

        var size = kind.IsLarge() ? locationDefinition.Large : locationDefinition.Normal;
        if (size is null)
            return null;

        return kind.IsMagic() ? size.Magic : size.Common;
    }

    private static IReadOnlyList<SpawnAreaRuleJson> NormalizeRules(string locationToken, EggKind kind, IReadOnlyList<SpawnAreaRuleJson>? rawRules)
    {
        if (rawRules is null || rawRules.Count == 0)
            return Array.Empty<SpawnAreaRuleJson>();

        var eggTypeToken = WildEggSpawnDefinitionEggIds.EggTypeTokenForKind(kind);

        return rawRules
            .Where(rule => rule is not null)
            .Select(rule => CloneRule(rule, locationToken, eggTypeToken))
            .ToList();
    }

    private static SpawnAreaRuleJson CloneRule(SpawnAreaRuleJson rule, string locationToken, string eggTypeToken)
    {
        return new SpawnAreaRuleJson
        {
            EggTypeToken = eggTypeToken,
            Location = locationToken,
            MapName = rule.MapName,
            ModID = rule.ModID,
            MinEggs = rule.MinEggs,
            MaxEggs = rule.MaxEggs,
            EggsToSpawn = rule.EggsToSpawn,
            RandomChance = rule.RandomChance,
            UseBaseRandomChance = rule.UseBaseRandomChance,
            IncludeTerrainTypes = rule.IncludeTerrainTypes?.ToList() ?? new List<string>(),
            ExcludeTerrainTypes = rule.ExcludeTerrainTypes?.ToList() ?? new List<string>(),
            IncludeCoordinates = rule.IncludeCoordinates?.ToList() ?? new List<string>(),
            ExcludeCoordinates = rule.ExcludeCoordinates?.ToList() ?? new List<string>(),
            ExtraConditions = new ExtraConditionsJson
            {
                StartYear = rule.ExtraConditions?.StartYear ?? 1,
                Seasons = rule.ExtraConditions?.Seasons?.ToList() ?? new List<string>(),
                Days = rule.ExtraConditions?.Days?.ToList() ?? new List<int>(),
                SpawnTimeHour = rule.ExtraConditions?.SpawnTimeHour ?? -1,
                GameStateQuery = rule.ExtraConditions?.GameStateQuery ?? string.Empty,
                WeatherYesterday = rule.ExtraConditions?.WeatherYesterday?.ToList() ?? new List<string>(),
                WeatherToday = rule.ExtraConditions?.WeatherToday?.ToList() ?? new List<string>(),
                WeatherTomorrow = rule.ExtraConditions?.WeatherTomorrow?.ToList() ?? new List<string>(),
            },
            SpawnedEggs = rule.SpawnedEggs,
            HitMaxSpawnTries = rule.HitMaxSpawnTries,
        };
    }
}
