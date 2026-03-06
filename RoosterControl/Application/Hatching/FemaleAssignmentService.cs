using System;
using System.Collections.Generic;
using System.Linq;
using RoosterControl.Constants;
using RoosterControl.Enums;
using RoosterControl.Framework;
using RoosterControl.Framework.Config;
using RoosterControl.Helpers;
using RoosterControl.Infrastructure.SaveData;
using StardewValley.GameData.FarmAnimals;
using RoosterControl.Application.FarmAnimals;

using ILetMeHatchApi = Mikew.NestCore.Framework.ILetMeHatchApi;
using IWildEggsApi = Mikew.NestCore.Framework.IWildEggsApi;

namespace RoosterControl.Application.Hatching;

/// <summary>
/// Keeps <see cref="RoosterControlConfig.FemalesOptions"/> in sync with the game data + LetMeHatch clone info.
/// </summary>

internal sealed class FemaleAssignmentsPlanResult
{
    public Dictionary<string, FemaleRoosterAssignment> FemalesOptions { get; }
    public Dictionary<RoosterType, List<string>> FemalesByRooster { get; }
    public Dictionary<string, Dictionary<EggTypeEntries, string>> FemaleEggs { get; }
    public bool ConfigChanged { get; }

    public FemaleAssignmentsPlanResult(
        Dictionary<string, FemaleRoosterAssignment> femalesOptions,
        Dictionary<RoosterType, List<string>> femalesByRooster,
        Dictionary<string, Dictionary<EggTypeEntries, string>> femaleEggs,
        bool configChanged
    )
    {
        FemalesOptions = femalesOptions ?? throw new ArgumentNullException(nameof(femalesOptions));
        FemalesByRooster = femalesByRooster ?? throw new ArgumentNullException(nameof(femalesByRooster));
        FemaleEggs = femaleEggs ?? throw new ArgumentNullException(nameof(femaleEggs));
        ConfigChanged = configChanged;
    }
}

internal static class FemaleAssignmentService
{
public static bool SyncFemaleAssignments(
    RoosterControlConfig config,
    RoosterHelper roosterHelper,
    IDictionary<string, FarmAnimalData> animalData,
    Dictionary<RoosterType, RoosterData> roosters,
    AnimalExclusion animalExclusions,
    ILetMeHatchApi? hatchApi,
    IWildEggsApi? wildEggsApi
)
{
    var plan = ComputeFemaleAssignmentsPlan(config, animalData, roosters, animalExclusions, hatchApi, wildEggsApi);
    ApplyFemaleAssignmentsPlan(config, roosterHelper, plan);
    return plan.ConfigChanged;
}

/// <summary>
/// Computes female assignment changes and the derived female egg map without mutating <paramref name="config"/> or <paramref name="roosters"/>.
/// </summary>
public static FemaleAssignmentsPlanResult ComputeFemaleAssignmentsPlan(
    RoosterControlConfig config,
    IDictionary<string, FarmAnimalData> animalData,
    Dictionary<RoosterType, RoosterData> roosters,
    AnimalExclusion animalExclusions,
    ILetMeHatchApi? hatchApi,
    IWildEggsApi? wildEggsApi
)
{
    if (config is null) throw new ArgumentNullException(nameof(config));
    if (animalData is null) throw new ArgumentNullException(nameof(animalData));
    if (roosters is null) throw new ArgumentNullException(nameof(roosters));

    var eggTypes = roosters
        .Values.SelectMany(rd => rd.Eggs.Values)
        .Where(ed => ed.ContainsKey(RoosterItemDataEntries.Name))
        .Select(ed => ed[RoosterItemDataEntries.Name])
        .ToList();

    var defaultFemales = roosters.Values.ToDictionary(
        x => x.Female,
        x => ToFemaleAssignment(x.RoosterType),
        StringComparer.OrdinalIgnoreCase
    );

    var foundFemales = new HashSet<string>(defaultFemales.Keys, StringComparer.OrdinalIgnoreCase);

    if (hatchApi is not null && hatchApi.FinishedEarlyStuff && hatchApi.GetClonedAnimalsInfo().Count > 0)
    {
        foreach (var f in GetClonedFemales(animalData, eggTypes, animalExclusions, hatchApi))
            foundFemales.Add(f);
    }

    ApplyWildEggsParity(foundFemales, wildEggsApi);

    var existing = config.FemalesOptions is null
        ? new Dictionary<string, FemaleRoosterAssignment>(StringComparer.OrdinalIgnoreCase)
        : new Dictionary<string, FemaleRoosterAssignment>(config.FemalesOptions, StringComparer.OrdinalIgnoreCase);

    var desired = new Dictionary<string, FemaleRoosterAssignment>(existing, StringComparer.OrdinalIgnoreCase);
    var changed = false;

    foreach (var female in desired.Keys.ToList())
    {
        if (!foundFemales.Contains(female))
        {
            desired.Remove(female);
            changed = true;
        }
    }

    foreach (var female in foundFemales.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
    {
        if (desired.ContainsKey(female))
            continue;

        desired[female] = defaultFemales.TryGetValue(female, out var def)
            ? def
            : FemaleRoosterAssignment.Off;
        changed = true;
    }

    var femalesByRooster = new Dictionary<RoosterType, List<string>>();
    foreach (var roosterType in roosters.Keys)
    {
        femalesByRooster[roosterType] = desired
            .Where(kvp => AssignmentMatchesRooster(kvp.Value, roosterType))
            .Select(kvp => kvp.Key)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    var femaleEggs = new Dictionary<string, Dictionary<EggTypeEntries, string>>(StringComparer.OrdinalIgnoreCase);

    var customFieldMap = new Dictionary<string, EggTypeEntries>(StringComparer.OrdinalIgnoreCase)
    {
        ["Normal"] = EggTypeEntries.Normal,
        ["Large"] = EggTypeEntries.Large,
    };

    foreach (var rooster in roosters.Values)
    {
        // Ensure default female exists with the rooster's configured eggs.
        if (!femaleEggs.TryGetValue(rooster.Female, out var defaultEggs))
        {
            defaultEggs = new Dictionary<EggTypeEntries, string>();
            femaleEggs[rooster.Female] = defaultEggs;
        }

        if (rooster.Eggs.TryGetValue(EggTypeEntries.Normal, out var n)
            && n.TryGetValue(RoosterItemDataEntries.Name, out var nId)
            && !string.IsNullOrWhiteSpace(nId))
        {
            defaultEggs[EggTypeEntries.Normal] = nId;
        }

        if (rooster.Eggs.TryGetValue(EggTypeEntries.Large, out var l)
            && l.TryGetValue(RoosterItemDataEntries.Name, out var lId)
            && !string.IsNullOrWhiteSpace(lId))
        {
            defaultEggs[EggTypeEntries.Large] = lId;
        }
    }

    foreach (var (roosterType, females) in femalesByRooster)
    {
        foreach (var female in females)
        {
            if (!animalData.TryGetValue(female, out var entry) || entry.CustomFields is null)
                continue;

            if (!femaleEggs.TryGetValue(female, out var eggs))
            {
                eggs = new Dictionary<EggTypeEntries, string>();
                femaleEggs[female] = eggs;
            }

            foreach (var (k, v) in entry.CustomFields)
            {
                if (!customFieldMap.TryGetValue(k, out var eggType))
                    continue;

                if (!string.IsNullOrWhiteSpace(v))
                    eggs[eggType] = v;
            }
        }
    }

    return new FemaleAssignmentsPlanResult(desired, femalesByRooster, femaleEggs, changed);
}

private static void ApplyWildEggsParity(HashSet<string> foundFemales, IWildEggsApi? wildEggsApi)
{
    if (foundFemales.Count == 0 || wildEggsApi is null)
        return;

    var validAnimals = wildEggsApi.ValidAnimals;
    if (validAnimals is null || validAnimals.Count == 0)
        return;

    foundFemales.RemoveWhere(female => !validAnimals.Contains(female));
}

/// <summary>
/// Applies a computed plan: updates config + rooster female lists, and refreshes the in-memory female egg map.
/// </summary>
public static void ApplyFemaleAssignmentsPlan(
    RoosterControlConfig config,
    RoosterHelper roosterHelper,
    FemaleAssignmentsPlanResult plan
)
{
    if (config is null) throw new ArgumentNullException(nameof(config));
    if (roosterHelper is null) throw new ArgumentNullException(nameof(roosterHelper));
    if (plan is null) throw new ArgumentNullException(nameof(plan));

    config.FemalesOptions = new Dictionary<string, FemaleRoosterAssignment>(plan.FemalesOptions, StringComparer.OrdinalIgnoreCase);

    foreach (var roosterType in plan.FemalesByRooster.Keys)
    {
        if (roosterHelper.Roosters.ContainsKey(roosterType))
            roosterHelper.Roosters[roosterType].Females = plan.FemalesByRooster[roosterType];
    }

    // Preserve any existing fertilized mappings created elsewhere.
    var existingFertilized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    foreach (var (femaleKey, eggs) in roosterHelper.FemaleEggs)
    {
        if (eggs.TryGetValue(EggTypeEntries.Fertilized, out var fert) && !string.IsNullOrWhiteSpace(fert))
            existingFertilized[femaleKey] = fert;
    }

    roosterHelper.FemaleEggs.Clear();
    foreach (var (femaleKey, eggs) in plan.FemaleEggs)
    {
        roosterHelper.FemaleEggs[femaleKey] = new Dictionary<EggTypeEntries, string>(eggs);

        if (existingFertilized.TryGetValue(femaleKey, out var fert))
            roosterHelper.FemaleEggs[femaleKey][EggTypeEntries.Fertilized] = fert;
    }
}

    public static void UpdateChosenFemalesApi(RoosterControlConfig config)
    {
        RoosterHatchingApi.Instance.UpdateChosenFemales(
            config.FemalesOptions
                .Where(kvp => kvp.Value != FemaleRoosterAssignment.Off)
                .Select(kvp => kvp.Key)
                .ToList()
        );
    }

    private static List<string> GetClonedFemales(
        IDictionary<string, FarmAnimalData> animalData,
        List<string> eggTypes,
        AnimalExclusion animalExclusions,
        ILetMeHatchApi hatchApi
    )
    {
        var matcher = FarmAnimalExclusionMatcher.From(animalExclusions);

        var clonedFemales = new HashSet<string>(
            hatchApi.GetClonedAnimalsInfo().Select(t => t.FemaleKey),
            StringComparer.OrdinalIgnoreCase
        );

        return animalData
            .Where(a =>
                clonedFemales.Contains(a.Key)
                && a.Value.CustomFields != null
                && a.Value.CustomFields.TryGetValue("ClonedEggs", out var clonedEggsRaw)
                && GetClonedEggsList(clonedEggsRaw).Any(egg => eggTypes.Contains(egg, StringComparer.OrdinalIgnoreCase))
                && !matcher.IsExcluded(a.Key, a.Value)
            )
            .Select(a => a.Key)
            .ToList();
    }

    private static List<string> GetClonedEggsList(string original)
        => original
            .Replace(" ", "", StringComparison.Ordinal)
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .ToList();

    private static FemaleRoosterAssignment ToFemaleAssignment(RoosterType roosterType)
        => roosterType switch
        {
            RoosterType.White => FemaleRoosterAssignment.White,
            RoosterType.Brown => FemaleRoosterAssignment.Brown,
            RoosterType.Blue => FemaleRoosterAssignment.Blue,
            RoosterType.Void => FemaleRoosterAssignment.Void,
            RoosterType.Gold => FemaleRoosterAssignment.Gold,
            _ => FemaleRoosterAssignment.Off
        };

    private static bool AssignmentMatchesRooster(FemaleRoosterAssignment assignment, RoosterType roosterType)
        => assignment switch
        {
            FemaleRoosterAssignment.White => roosterType == RoosterType.White,
            FemaleRoosterAssignment.Brown => roosterType == RoosterType.Brown,
            FemaleRoosterAssignment.Blue => roosterType == RoosterType.Blue,
            FemaleRoosterAssignment.Void => roosterType == RoosterType.Void,
            FemaleRoosterAssignment.Gold => roosterType == RoosterType.Gold,
            _ => false
        };
}
