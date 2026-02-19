using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RoosterAddon.Constants;
using RoosterAddon.Framework.Config;

namespace RoosterAddon.Application.Catalogs;

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
        catalog.Roosters ??= new Dictionary<string, RoosterCatalogEntry>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in catalog.Roosters.Values)
        {
            entry.FarmAnimalKey ??= string.Empty;
            entry.DefaultFemaleKey ??= string.Empty;
            entry.AdditionalFemaleKeys ??= new List<string>();
            entry.Eggs ??= new EggCatalog();

            entry.Eggs.Normal ??= string.Empty;
            entry.Eggs.Large ??= string.Empty;
            entry.Eggs.Fertilized ??= string.Empty;
        }
    }

    private static void SanitizeProduce(ProduceCatalog catalog)
    {
        catalog.RandomGroups ??= new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        catalog.Roosters ??= new Dictionary<string, ProduceCatalogEntry>(StringComparer.OrdinalIgnoreCase);

        foreach (var groupKey in catalog.RandomGroups.Keys.ToList())
        {
            if (catalog.RandomGroups[groupKey] is null)
                catalog.RandomGroups[groupKey] = new List<string>();
        }

        foreach (var entry in catalog.Roosters.Values)
        {
            entry.BlueEggChoice ??= entry.BlueEggChoice;

            entry.Produce ??= new List<ProduceEntryDto>();
            entry.Deluxe ??= new List<ProduceEntryDto>();

            foreach (var dto in entry.Produce)
                SanitizeProduceDto(dto);

            foreach (var dto in entry.Deluxe)
                SanitizeProduceDto(dto);
        }
    }

    private static void SanitizeProduceDto(ProduceEntryDto dto)
    {
        dto.Choice ??= string.Empty;
        dto.CustomItemName ??= string.Empty;
        dto.Probability = Math.Clamp(dto.Probability, 0, 100);
    }

    private static void SanitizeHatching(HatchingCatalog catalog)
    {
        catalog.General ??= new HatchingGeneralCatalog();
        catalog.Roosters ??= new Dictionary<string, HatchingRoosterCatalog>(StringComparer.OrdinalIgnoreCase);

        catalog.General.HarvestType ??= catalog.General.HarvestType;
        catalog.General.HatchingMode ??= catalog.General.HatchingMode;

        foreach (var entry in catalog.Roosters.Values)
        {
            entry.BuildingRequired ??= entry.BuildingRequired;
            entry.ProduceOption ??= entry.ProduceOption;
            entry.DeluxeProduceOption ??= entry.DeluxeProduceOption;
        }
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
