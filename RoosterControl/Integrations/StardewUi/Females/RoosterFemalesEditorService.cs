using System;
using System.Collections.Generic;
using System.Linq;
using RoosterControl.Application.Hatching;
using RoosterControl.Enums;
using RoosterControl.Framework;
using RoosterControl.Framework.Config;
using StardewValley.GameData.FarmAnimals;

using ILetMeHatchApi = Mikew.NestCore.Framework.ILetMeHatchApi;
using IWildEggsApi = Mikew.NestCore.Framework.IWildEggsApi;

namespace RoosterControl.Integrations.StardewUi.Females;

internal sealed class RoosterFemalesEditorService
{
    public RoosterFemalesEditorSnapshot LoadSnapshot(
        RoosterControlConfig config,
        IDictionary<string, FarmAnimalData> animalData,
        Dictionary<RoosterType, RoosterData> roosters,
        AnimalExclusion animalExclusions,
        ILetMeHatchApi? hatchApi,
        IWildEggsApi? wildEggsApi)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(animalData);
        ArgumentNullException.ThrowIfNull(roosters);
        ArgumentNullException.ThrowIfNull(animalExclusions);

        var plan = FemaleAssignmentService.ComputeFemaleAssignmentsPlan(
            config,
            animalData,
            roosters,
            animalExclusions,
            hatchApi,
            wildEggsApi);

        var females = plan.FemalesOptions.Keys
            .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
            .Select(key => BuildFemaleRecord(key, plan, animalData))
            .ToList();

        return new RoosterFemalesEditorSnapshot(
            females,
            BuildSummaryText(plan));
    }

    public RoosterFemalesEditorDraft CreateDraft(RoosterFemalesEditorSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return new RoosterFemalesEditorDraft(
            snapshot.Females.ToDictionary(
                female => female.FemaleKey,
                female => female.CurrentAssignment,
                StringComparer.OrdinalIgnoreCase));
    }

    public void ApplyDraft(RoosterControlConfig config, RoosterFemalesEditorDraft draft)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(draft);

        config.FemalesOptions = new Dictionary<string, FemaleRoosterAssignment>(
            draft.Assignments,
            StringComparer.OrdinalIgnoreCase);
    }

    private static RoosterFemalesFemaleRecord BuildFemaleRecord(
        string femaleKey,
        FemaleAssignmentsPlanResult plan,
        IDictionary<string, FarmAnimalData> animalData)
    {
        var assignment = plan.FemalesOptions.TryGetValue(femaleKey, out var current)
            ? current
            : FemaleRoosterAssignment.Off;

        var label = BuildFemaleLabel(femaleKey, animalData);
        var eggSummary = BuildEggSummary(femaleKey, plan);
        var options = Enum.GetValues(typeof(FemaleRoosterAssignment))
            .Cast<FemaleRoosterAssignment>()
            .Select(value => new RoosterFemalesAssignmentOptionRecord(
                value,
                value.ToString(),
                BuildAssignmentDescription(value, assignment)))
            .ToList();

        return new RoosterFemalesFemaleRecord(
            femaleKey,
            label,
            $"{assignment} rooster | {eggSummary}",
            eggSummary,
            BuildFemaleTooltip(femaleKey, plan),
            assignment,
            options);
    }

    private static string BuildSummaryText(FemaleAssignmentsPlanResult plan)
    {
        var assignedCount = plan.FemalesOptions.Count(kvp => kvp.Value != FemaleRoosterAssignment.Off);
        return $"Females: {plan.FemalesOptions.Count} | Assigned: {assignedCount}";
    }

    private static string BuildFemaleLabel(string femaleKey, IDictionary<string, FarmAnimalData> animalData)
    {
        if (!animalData.TryGetValue(femaleKey, out var entry) || entry is null)
            return femaleKey;

        var displayName = entry.DisplayName?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(displayName) || string.Equals(displayName, femaleKey, StringComparison.OrdinalIgnoreCase))
            return femaleKey;

        return $"{displayName} ({femaleKey})";
    }

    private static string BuildFemaleTooltip(string femaleKey, FemaleAssignmentsPlanResult plan)
    {
        var normalEgg = "-";
        var largeEgg = "-";

        if (plan.FemaleEggs.TryGetValue(femaleKey, out var eggs))
        {
            if (eggs.TryGetValue(EggTypeEntries.Normal, out var normal) && !string.IsNullOrWhiteSpace(normal))
                normalEgg = normal;
            if (eggs.TryGetValue(EggTypeEntries.Large, out var large) && !string.IsNullOrWhiteSpace(large))
                largeEgg = large;
        }

        return $"Key: {femaleKey}\nNormal egg: {normalEgg}\nLarge egg: {largeEgg}";
    }

    private static string BuildEggSummary(string femaleKey, FemaleAssignmentsPlanResult plan)
    {
        var normalEgg = "-";
        var largeEgg = "-";

        if (plan.FemaleEggs.TryGetValue(femaleKey, out var eggs))
        {
            if (eggs.TryGetValue(EggTypeEntries.Normal, out var normal) && !string.IsNullOrWhiteSpace(normal))
                normalEgg = normal;
            if (eggs.TryGetValue(EggTypeEntries.Large, out var large) && !string.IsNullOrWhiteSpace(large))
                largeEgg = large;
        }

        return $"Normal: {normalEgg} | Large: {largeEgg}";
    }

    private static string BuildAssignmentDescription(FemaleRoosterAssignment value, FemaleRoosterAssignment currentAssignment)
        => value == currentAssignment
            ? "Current assignment"
            : $"Assign to {value} rooster";
}

internal sealed record RoosterFemalesEditorSnapshot(
    IReadOnlyList<RoosterFemalesFemaleRecord> Females,
    string SummaryText);

internal sealed record RoosterFemalesFemaleRecord(
    string FemaleKey,
    string Label,
    string MetaText,
    string EggSummaryText,
    string TooltipText,
    FemaleRoosterAssignment CurrentAssignment,
    IReadOnlyList<RoosterFemalesAssignmentOptionRecord> AssignmentOptions);

internal sealed record RoosterFemalesAssignmentOptionRecord(
    FemaleRoosterAssignment Value,
    string ButtonText,
    string DescriptionText);

internal sealed record RoosterFemalesEditorDraft(
    IReadOnlyDictionary<string, FemaleRoosterAssignment> Assignments);
