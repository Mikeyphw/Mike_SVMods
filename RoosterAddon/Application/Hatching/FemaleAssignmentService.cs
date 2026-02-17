using System;
using System.Collections.Generic;
using System.Linq;
using RoosterAddon.Constants;
using RoosterAddon.Enums;
using RoosterAddon.Framework.Config;
using RoosterAddon.Helpers;
using RoosterAddon.Infrastructure.SaveData;
using StardewValley.GameData.FarmAnimals;
using RoosterAddon.Application.FarmAnimals;

using ILetMeHatchApi = Mikew.BirdsOfAFeather.Framework.ILetMeHatchApi;

namespace RoosterAddon.Application.Hatching;

/// <summary>
/// Keeps <see cref="RoosterAddonConfig.FemalesOptions"/> in sync with the game data + LetMeHatchThisEgg clone info.
/// </summary>
internal static class FemaleAssignmentService
{
    public static bool SyncFemaleAssignments(
        RoosterAddonConfig config,
        RoosterControl roosterHelper,
        IDictionary<string, FarmAnimalData> animalData,
        Dictionary<RoosterType, RoosterData> roosters,
        AnimalExclusion animalExclusions,
        ILetMeHatchApi? hatchApi
    )
    {
        var eggTypes = roosters
            .Values.SelectMany(rd => rd.Eggs.Values)
            .Where(ed => ed.ContainsKey(RoosterItemDataEntries.Name))
            .Select(ed => ed[RoosterItemDataEntries.Name])
            .ToList();

        var defaultFemales = roosters.Values.ToDictionary(
            x => x.Female,
            x => ToFemaleAssignment(x.RoosterType)
        );

        var foundFemales = defaultFemales.Keys.ToList();

        if (hatchApi is not null && hatchApi.GetClonedAnimalsInfo().Count > 0)
            foundFemales.AddRange(GetClonedFemales(animalData, eggTypes, animalExclusions, hatchApi).OrderBy(f => f));

        config.FemalesOptions ??= new Dictionary<string, FemaleRoosterAssignment>(StringComparer.OrdinalIgnoreCase);

        var changed = false;

        foreach (var female in config.FemalesOptions.Keys.ToList())
        {
            if (!foundFemales.Contains(female, StringComparer.OrdinalIgnoreCase))
            {
                config.FemalesOptions.Remove(female);
                changed = true;
            }
        }

        foreach (var female in foundFemales)
        {
            if (!config.FemalesOptions.ContainsKey(female))
            {
                config.FemalesOptions[female] = defaultFemales.TryGetValue(female, out var def)
                    ? def
                    : FemaleRoosterAssignment.Off;
                changed = true;
            }
        }

        foreach (var roosterType in roosters.Keys)
        {
            roosterHelper.Roosters[roosterType].Females = config.FemalesOptions
                .Where(kvp => AssignmentMatchesRooster(kvp.Value, roosterType))
                .Select(kvp => kvp.Key)
                .ToList();

            roosterHelper.UpdateFemaleEggs(roosterHelper.Roosters[roosterType], animalData);
        }

        return changed;
    }

    public static void UpdateChosenFemalesApi(RoosterAddonConfig config)
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
