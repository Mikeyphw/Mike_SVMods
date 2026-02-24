using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RoosterControl.Constants;
using RoosterControl.Framework.Config;

namespace RoosterControl.Application.Catalogs;

internal static class CatalogSanitizer
{
    internal static void Sanitize(CatalogBundle bundle)
    {
        if (bundle is null)
            return;

        bundle.Roosters ??= new RoosterCatalog();
        bundle.Produce ??= new ProduceCatalog();
        bundle.Hatching ??= new HatchingCatalog();

        SanitizeRoosters(bundle.Roosters);
        SanitizeProduce(bundle.Produce);
        SanitizeHatching(bundle.Hatching);
    }

    private static void SanitizeRoosters(RoosterCatalog catalog)
    {
        catalog.Roosters = ToCaseInsensitive(catalog.Roosters);

        foreach (var key in catalog.Roosters.Keys.ToList())
        {
            catalog.Roosters.TryGetValue(key, out var entry);
            entry ??= new RoosterCatalogEntry();

            entry.FarmAnimalKey ??= string.Empty;
            entry.DefaultFemaleKey ??= string.Empty;
            entry.AdditionalFemaleKeys ??= new List<string>();

            entry.Eggs ??= new EggCatalog();
            entry.Eggs.Normal ??= string.Empty;
            entry.Eggs.Large ??= string.Empty;
            entry.Eggs.Fertilized ??= string.Empty;

            catalog.Roosters[key] = entry;
        }
    }

    private static void SanitizeProduce(ProduceCatalog catalog)
    {
        catalog.RandomGroups = ToCaseInsensitive(catalog.RandomGroups);
        catalog.Roosters = ToCaseInsensitive(catalog.Roosters);

        foreach (var key in catalog.RandomGroups.Keys.ToList())
            catalog.RandomGroups[key] ??= new List<string>();

        foreach (var key in catalog.Roosters.Keys.ToList())
        {
            catalog.Roosters.TryGetValue(key, out var entry);
            entry ??= new ProduceCatalogEntry();

            entry.Produce ??= new List<ProduceEntryDto>();
            entry.Deluxe ??= new List<ProduceEntryDto>();

            foreach (var dto in entry.Produce)
                SanitizeProduceDto(dto);

            foreach (var dto in entry.Deluxe)
                SanitizeProduceDto(dto);

            catalog.Roosters[key] = entry;
        }
    }

    private static void SanitizeProduceDto(ProduceEntryDto dto)
    {
        if (dto is null)
            return;

        dto.Choice ??= string.Empty;
        dto.CustomItemName ??= string.Empty;
        dto.Probability = Math.Clamp(dto.Probability, 0, 100);
    }

    private static void SanitizeHatching(HatchingCatalog catalog)
    {
        catalog.General ??= new HatchingGeneralCatalog();
        catalog.Roosters = ToCaseInsensitive(catalog.Roosters);

        catalog.General.UpdateRate ??= catalog.General.UpdateRate;
        catalog.General.HarvestType ??= catalog.General.HarvestType;
        catalog.General.HatchingMode ??= catalog.General.HatchingMode;

        if (catalog.General.ChosenPercentageDailyLuckBonus.HasValue)
            catalog.General.ChosenPercentageDailyLuckBonus = Math.Clamp(catalog.General.ChosenPercentageDailyLuckBonus.Value, 0, 100);

        if (catalog.General.EggProbability.HasValue)
            catalog.General.EggProbability = Math.Clamp(catalog.General.EggProbability.Value, 0, 100);

        if (catalog.General.LargeEggProbability.HasValue)
            catalog.General.LargeEggProbability = Math.Clamp(catalog.General.LargeEggProbability.Value, 0, 100);

        if (catalog.General.FertilizedEggProbability.HasValue)
            catalog.General.FertilizedEggProbability = Math.Clamp(catalog.General.FertilizedEggProbability.Value, 0, 100);

        if (catalog.General.RoosterToChickenRatio.HasValue)
            catalog.General.RoosterToChickenRatio = Math.Clamp(catalog.General.RoosterToChickenRatio.Value, 0, 100);

        if (catalog.General.BoostByRooster.HasValue)
            catalog.General.BoostByRooster = Math.Clamp(catalog.General.BoostByRooster.Value, 0f, 1000f);

        foreach (var key in catalog.Roosters.Keys.ToList())
        {
            catalog.Roosters.TryGetValue(key, out var entry);
            entry ??= new HatchingRoosterCatalog();

            entry.BuildingRequired ??= entry.BuildingRequired;
            entry.ProduceOption ??= entry.ProduceOption;
            entry.DeluxeProduceOption ??= entry.DeluxeProduceOption;

            if (entry.Price.HasValue)
                entry.Price = Math.Max(0, entry.Price.Value);

            if (entry.MatureDays.HasValue)
                entry.MatureDays = Math.Max(0, entry.MatureDays.Value);

            if (entry.ProduceDays.HasValue)
                entry.ProduceDays = Math.Max(0, entry.ProduceDays.Value);

            catalog.Roosters[key] = entry;
        }
    }

    private static Dictionary<string, T> ToCaseInsensitive<T>(Dictionary<string, T>? source)
    {
        var result = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
        if (source is null)
            return result;

        foreach (var (key, value) in source)
        {
            if (string.IsNullOrWhiteSpace(key))
                continue;

            result[key] = value!;
        }

        return result;
    }
}

internal static class CatalogValidator
{
    internal static IReadOnlyList<CatalogIssue> Validate(CatalogBundle bundle)
    {
        var issues = new List<CatalogIssue>();

        if (bundle is null)
        {
            issues.Add(new CatalogIssue("bundle", "$", "Catalog bundle is null."));
            return issues;
        }

        ValidateRoosters(bundle.Roosters, issues);
        ValidateProduce(bundle.Produce, issues);
        ValidateHatching(bundle.Hatching, issues);

        return issues;
    }

    internal static void ValidateOrThrow(CatalogBundle bundle)
    {
        var issues = Validate(bundle);
        if (issues.Count == 0)
            return;

        var summary = string.Join("\n", issues.Select(i => i.ToString()));
        throw new InvalidDataException($"Catalog validation failed:\n{summary}");
    }

    private static void ValidateRoosters(RoosterCatalog catalog, List<CatalogIssue> issues)
    {
        if (catalog is null)
        {
            issues.Add(new CatalogIssue("roosters", "$", "Catalog is null."));
            return;
        }

        if (catalog.Roosters is null || catalog.Roosters.Count == 0)
        {
            issues.Add(new CatalogIssue("roosters", "Roosters", "Roosters map is missing/empty."));
            return;
        }

        foreach (var (key, entry) in catalog.Roosters)
        {
            if (string.IsNullOrWhiteSpace(key))
                issues.Add(new CatalogIssue("roosters", "Roosters[*]", "Rooster key is empty."));

            if (entry is null)
            {
                issues.Add(new CatalogIssue("roosters", $"Roosters['{key}']", "Entry is null."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(entry.FarmAnimalKey))
                issues.Add(new CatalogIssue("roosters", $"Roosters['{key}'].FarmAnimalKey", "Missing FarmAnimalKey."));

            if (string.IsNullOrWhiteSpace(entry.DefaultFemaleKey))
                issues.Add(new CatalogIssue("roosters", $"Roosters['{key}'].DefaultFemaleKey", "Missing DefaultFemaleKey."));

            if (entry.Eggs is null)
            {
                issues.Add(new CatalogIssue("roosters", $"Roosters['{key}'].Eggs", "Eggs is null."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(entry.Eggs.Normal))
                issues.Add(new CatalogIssue("roosters", $"Roosters['{key}'].Eggs.Normal", "Missing Normal egg id/key."));

            if (string.IsNullOrWhiteSpace(entry.Eggs.Large))
                issues.Add(new CatalogIssue("roosters", $"Roosters['{key}'].Eggs.Large", "Missing Large egg id/key."));

            if (string.IsNullOrWhiteSpace(entry.Eggs.Fertilized))
                issues.Add(new CatalogIssue("roosters", $"Roosters['{key}'].Eggs.Fertilized", "Missing Fertilized egg id/key."));
        }
    }

    private static void ValidateProduce(ProduceCatalog catalog, List<CatalogIssue> issues)
    {
        if (catalog is null)
        {
            issues.Add(new CatalogIssue("produce", "$", "Catalog is null."));
            return;
        }

        if (catalog.RandomGroups is null)
            issues.Add(new CatalogIssue("produce", "RandomGroups", "RandomGroups is null."));

        if (catalog.RandomGroups is not null)
        {
            foreach (var (groupKey, options) in catalog.RandomGroups)
            {
                if (string.IsNullOrWhiteSpace(groupKey))
                {
                    issues.Add(new CatalogIssue("produce", "RandomGroups[*]", "Random group key is empty."));
                    continue;
                }

                if (options is null || options.Count == 0)
                {
                    issues.Add(new CatalogIssue("produce", $"RandomGroups['{groupKey}']", "Group has no options."));
                    continue;
                }

                if (options.Any(o => string.IsNullOrWhiteSpace(o)))
                    issues.Add(new CatalogIssue("produce", $"RandomGroups['{groupKey}']", "Group contains empty option value(s)."));
            }
        }

        if (catalog.Roosters is null || catalog.Roosters.Count == 0)
        {
            issues.Add(new CatalogIssue("produce", "Roosters", "Roosters map is missing/empty."));
            return;
        }

        foreach (var (key, entry) in catalog.Roosters)
        {
            if (entry is null)
            {
                issues.Add(new CatalogIssue("produce", $"Roosters['{key}']", "Entry is null."));
                continue;
            }

            ValidateProduceList(key, "Produce", entry.Produce, issues);
            ValidateProduceList(key, "Deluxe", entry.Deluxe, issues);
        }
    }

    private static void ValidateProduceList(string roosterKey, string listName, List<ProduceEntryDto> entries, List<CatalogIssue> issues)
    {
        entries ??= new List<ProduceEntryDto>();

        for (var i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (e is null)
            {
                issues.Add(new CatalogIssue("produce", $"Roosters['{roosterKey}'].{listName}[{i}]", "Entry is null."));
                continue;
            }

            var choice = (e.Choice ?? string.Empty).Trim();
            var custom = (e.CustomItemName ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(choice))
                issues.Add(new CatalogIssue("produce", $"Roosters['{roosterKey}'].{listName}[{i}].Choice", "Choice is empty."));

            if (choice.Equals(ItemIds.CustomItems.Custom, StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(custom))
                issues.Add(new CatalogIssue("produce", $"Roosters['{roosterKey}'].{listName}[{i}].CustomItemName", "Custom choice requires CustomItemName."));

            if (e.Probability < 0 || e.Probability > 100)
                issues.Add(new CatalogIssue("produce", $"Roosters['{roosterKey}'].{listName}[{i}].Probability", "Probability must be 0..100."));
        }
    }

    private static void ValidateHatching(HatchingCatalog catalog, List<CatalogIssue> issues)
    {
        if (catalog is null)
        {
            issues.Add(new CatalogIssue("hatching", "$", "Catalog is null."));
            return;
        }

        if (catalog.General is null)
        {
            issues.Add(new CatalogIssue("hatching", "General", "General is null."));
        }
        else
        {
            RequireNotNull(catalog.General.UpdateRate, "General.UpdateRate", issues);
            RequireNotNull(catalog.General.ChosenPercentageDailyLuckBonus, "General.ChosenPercentageDailyLuckBonus", issues);
            RequireNotNull(catalog.General.EggProbability, "General.EggProbability", issues);
            RequireNotNull(catalog.General.LargeEggProbability, "General.LargeEggProbability", issues);
            RequireNotNull(catalog.General.FertilizedEggProbability, "General.FertilizedEggProbability", issues);
            RequireNotNull(catalog.General.RoosterToChickenRatio, "General.RoosterToChickenRatio", issues);
            RequireNotNull(catalog.General.BoostByRooster, "General.BoostByRooster", issues);

            if (!string.IsNullOrWhiteSpace(catalog.General.UpdateRate)
                && !Enum.TryParse<UpdateRateMode>(catalog.General.UpdateRate, ignoreCase: true, out _))
            {
                issues.Add(new CatalogIssue("hatching", "General.UpdateRate", $"Unknown UpdateRateMode '{catalog.General.UpdateRate}'."));
            }

            if (!string.IsNullOrWhiteSpace(catalog.General.HarvestType)
                && !Enum.TryParse<HarvestType>(catalog.General.HarvestType, ignoreCase: true, out _))
            {
                issues.Add(new CatalogIssue("hatching", "General.HarvestType", $"Unknown HarvestType '{catalog.General.HarvestType}'."));
            }

            if (!string.IsNullOrWhiteSpace(catalog.General.HatchingMode)
                && !Enum.TryParse<HatchingMode>(catalog.General.HatchingMode, ignoreCase: true, out _))
            {
                issues.Add(new CatalogIssue("hatching", "General.HatchingMode", $"Unknown HatchingMode '{catalog.General.HatchingMode}'."));
            }

            ValidateRange(catalog.General.ChosenPercentageDailyLuckBonus, 0, 100, "General.ChosenPercentageDailyLuckBonus", issues);
            ValidateRange(catalog.General.EggProbability, 0, 100, "General.EggProbability", issues);
            ValidateRange(catalog.General.LargeEggProbability, 0, 100, "General.LargeEggProbability", issues);
            ValidateRange(catalog.General.FertilizedEggProbability, 0, 100, "General.FertilizedEggProbability", issues);
            ValidateRange(catalog.General.RoosterToChickenRatio, 0, 100, "General.RoosterToChickenRatio", issues);

            if (catalog.General.BoostByRooster is { } boost && (boost < 0f || boost > 1000f))
                issues.Add(new CatalogIssue("hatching", "General.BoostByRooster", "BoostByRooster must be 0..1000."));
        }

        if (catalog.Roosters is null)
        {
            issues.Add(new CatalogIssue("hatching", "Roosters", "Roosters map is null."));
            return;
        }

        foreach (var (key, entry) in catalog.Roosters)
        {
            if (entry is null)
            {
                issues.Add(new CatalogIssue("hatching", $"Roosters['{key}']", "Entry is null."));
                continue;
            }

            RequireNotNull(entry.EditAllFemales, $"Roosters['{key}'].EditAllFemales", issues);

            if (!string.IsNullOrWhiteSpace(entry.BuildingRequired)
                && !Enum.TryParse<RequiredBuilding>(entry.BuildingRequired, ignoreCase: true, out _))
            {
                issues.Add(new CatalogIssue("hatching", $"Roosters['{key}'].BuildingRequired", $"Unknown RequiredBuilding '{entry.BuildingRequired}'."));
            }

            if (!string.IsNullOrWhiteSpace(entry.ProduceOption)
                && !Enum.TryParse<ProduceMode>(entry.ProduceOption, ignoreCase: true, out _))
            {
                issues.Add(new CatalogIssue("hatching", $"Roosters['{key}'].ProduceOption", $"Unknown ProduceMode '{entry.ProduceOption}'."));
            }

            if (!string.IsNullOrWhiteSpace(entry.DeluxeProduceOption)
                && !Enum.TryParse<DeluxeProduceMode>(entry.DeluxeProduceOption, ignoreCase: true, out _))
            {
                issues.Add(new CatalogIssue("hatching", $"Roosters['{key}'].DeluxeProduceOption", $"Unknown DeluxeProduceMode '{entry.DeluxeProduceOption}'."));
            }

            if (entry.Price is { } price && price < 0)
                issues.Add(new CatalogIssue("hatching", $"Roosters['{key}'].Price", "Price must be >= 0."));

            if (entry.MatureDays is { } mature && mature < 0)
                issues.Add(new CatalogIssue("hatching", $"Roosters['{key}'].MatureDays", "MatureDays must be >= 0."));

            if (entry.ProduceDays is { } produceDays && produceDays < 0)
                issues.Add(new CatalogIssue("hatching", $"Roosters['{key}'].ProduceDays", "ProduceDays must be >= 0."));
        }
    }

    private static void RequireNotNull(object? value, string path, List<CatalogIssue> issues)
    {
        if (value is null)
            issues.Add(new CatalogIssue("hatching", path, "Value is missing."));
    }

    private static void ValidateRange(int? value, int min, int max, string path, List<CatalogIssue> issues)
    {
        if (value is null)
            return;

        if (value.Value < min || value.Value > max)
            issues.Add(new CatalogIssue("hatching", path, $"Value must be {min}..{max}."));
    }
}

internal sealed class CatalogIssue
{
    public string Catalog { get; }
    public string Path { get; }
    public string Message { get; }

    public CatalogIssue(string catalog, string path, string message)
    {
        Catalog = catalog ?? "unknown";
        Path = path ?? "$";
        Message = message ?? string.Empty;
    }

    public override string ToString() => $"{Catalog}:{Path}: {Message}";
}
