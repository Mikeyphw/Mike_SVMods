using System;
using System.Collections.Generic;
using System.Linq;
using EggsInWild.Core.Enums;
using EggsInWild.Infrastructure.ModData.Models;

namespace EggsInWild.Infrastructure.ModData;

public static class WildEggSpawnDefinitionEggIds
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    public static string BuildEggItemId(string locationToken, EggKind kind)
        => $"{Constants.ModId}_{SanitizeToken(locationToken)}{EggTypeTokenForKind(kind)}";

    public static string EggTypeTokenForKind(EggKind kind) => kind switch
    {
        EggKind.Wild => "WEgg",
        EggKind.LargeWild => "LWEgg",
        EggKind.Magic => "MWEgg",
        EggKind.LargeMagic => "LMWEgg",
        _ => "WEgg",
    };

    public static IReadOnlyDictionary<string, WildEggSpawnLocationJson> FromLegacyFlat(
        IReadOnlyDictionary<string, List<SpawnAreaRuleJson>> legacyDefinitions)
    {
        var result = new Dictionary<string, WildEggSpawnLocationJson>(Comparer);

        foreach (var (key, rules) in legacyDefinitions)
        {
            var location = ExtractRegionTokenFromEggItemId(key);
            var kind = EggKindExtensions.FromEggItemId(key);

            if (!result.TryGetValue(location, out var locationJson))
            {
                locationJson = new WildEggSpawnLocationJson();
                result[location] = locationJson;
            }

            var size = kind.IsLarge()
                ? locationJson.Large ??= new WildEggSpawnSizeJson()
                : locationJson.Normal ??= new WildEggSpawnSizeJson();

            var cleaned = (rules ?? new List<SpawnAreaRuleJson>())
                .Where(rule => rule is not null)
                .Select(CloneRuleWithoutDerivedFields)
                .ToList();

            if (kind.IsMagic())
                size.Magic = cleaned;
            else
                size.Common = cleaned;
        }

        return result;
    }

    public static string ExtractRegionTokenFromEggItemId(string eggItemId)
    {
        const string prefix = "Mikew.eggsinwild_";

        var token = eggItemId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? eggItemId[prefix.Length..]
            : eggItemId;

        foreach (var suffix in new[] { "LMWEgg", "LWEgg", "MWEgg", "WEgg" })
        {
            if (token.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return token[..^suffix.Length];
        }

        return token;
    }

    private static string SanitizeToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return "";

        var chars = token.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').ToArray();
        return new string(chars);
    }

    private static SpawnAreaRuleJson CloneRuleWithoutDerivedFields(SpawnAreaRuleJson rule)
    {
        return new SpawnAreaRuleJson
        {
            EggTypeToken = "",
            Location = "",
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
