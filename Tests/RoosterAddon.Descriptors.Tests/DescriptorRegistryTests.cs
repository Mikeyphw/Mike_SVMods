using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using RoosterAddon.Application.Catalogs;
using RoosterAddon.Application.Gmcm;
using RoosterAddon.Application.Gmcm.Descriptors;
using RoosterAddon.Framework.Config;
using Xunit;

namespace RoosterAddon.Descriptors.Tests;

public sealed class DescriptorRegistryTests
{
    [Fact]
    public void OptionIds_AreUnique()
    {
        var index = RoosterGmcmDescriptorIndex.Build();
        var dupes = index.DuplicateOptionIds();
        Assert.True(dupes.Count == 0, "Duplicate option IDs: " + string.Join(", ", dupes));
    }

    [Fact]
    public void Pages_ContainAllExpectedPages()
    {
        var index = RoosterGmcmDescriptorIndex.Build();

        var expected = new[]
        {
            "females",
            "roosters_white",
            "roosters_brown",
            "roosters_blue",
            "roosters_void",
            "roosters_gold",
        };

        foreach (var id in expected)
            Assert.Contains(id, index.PageIds);
    }

    [Fact]
    public void OptionIds_AreGrouped()
    {
        var index = RoosterGmcmDescriptorIndex.Build();
        var re = new Regex(@"^(general|roosters|females)\.", RegexOptions.Compiled);

        foreach (var id in index.OptionIds)
            Assert.Matches(re, id);
    }

    [Fact]
    public void I18n_Covers_AllDescriptors()
    {
        var i18nPath = FindDefaultI18nPath();
        var json = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(File.ReadAllText(i18nPath))
                   ?? throw new InvalidOperationException("Failed to parse i18n default.json");

        var requiredKeys = BuildRequiredI18nKeys();

        var missing = requiredKeys
            .Where(k => !json.ContainsKey(k))
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Assert.True(missing.Count == 0, "Missing i18n keys:\n" + string.Join("\n", missing));
    }

    private static string FindDefaultI18nPath()
    {
        var candidates = new[]
        {
            Path.Combine("Mike_SVMods", "RoosterAddon", "i18n", "default.json"),
            Path.Combine("RoosterAddon", "i18n", "default.json"),
        };

        var start = new DirectoryInfo(AppContext.BaseDirectory);

        for (var i = 0; i < 10 && start is not null; i++)
        {
            foreach (var rel in candidates)
            {
                var full = Path.Combine(start.FullName, rel);
                if (File.Exists(full))
                    return full;
            }

            start = start.Parent;
        }

        throw new FileNotFoundException("Could not find RoosterAddon i18n/default.json by walking up from test output directory.");
    }

    private static HashSet<string> BuildRequiredI18nKeys()
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var cfg = RoosterAddonConfig.CreateDefault();
        var catalogs = CatalogDefaults.CreateDefaults();

        var descriptors = new List<IGmcmDescriptor<RoosterAddonConfig>>();
        descriptors.AddRange(RoosterGmcmDescriptorRegistry.RootGeneral());
        descriptors.AddRange(RoosterGmcmDescriptorRegistry.RootRoostersLinks());

        var pages = RoosterGmcmDescriptorRegistry.Pages(() => cfg, () => catalogs, () => { });

        foreach (var p in pages)
        {
            keys.Add($"config.page.{p.PageId}.name");
            descriptors.AddRange(p.Content);
        }

        foreach (var d in descriptors)
        {
            switch (d)
            {
                case GmcmSectionDescriptor<RoosterAddonConfig> s:
                    keys.Add($"config.section.{s.SectionId}.text");
                    if (s.Tooltip)
                        keys.Add($"config.section.{s.SectionId}.description");
                    break;

                case GmcmParagraphDescriptor<RoosterAddonConfig> p:
                    keys.Add($"config.paragraph.{p.ParagraphId}.text");
                    break;

                case GmcmPageLinkDescriptor<RoosterAddonConfig> l:
                    keys.Add($"config.pagelink.{l.PageId}.name");
                    if (l.Tooltip)
                        keys.Add($"config.pagelink.{l.PageId}.description");
                    break;

                case GmcmOptionDescriptor<RoosterAddonConfig> o:
                    keys.Add($"config.{o.OptionId}.name");
                    if (o.Tooltip)
                        keys.Add($"config.{o.OptionId}.description");
                    break;
            }
        }

        AddEnumAllowedKeys(keys);

        return keys;
    }

    private static void AddEnumAllowedKeys(HashSet<string> keys)
    {
        static void Add(HashSet<string> k, string optionId, Type enumType)
        {
            foreach (var v in Enum.GetNames(enumType))
                k.Add($"config.{optionId}.allowed.{v}");
        }

        Add(keys, "general.updateRate", typeof(UpdateRateMode));
        Add(keys, "general.catalogApplyMode", typeof(CatalogApplyMode));
        Add(keys, "general.hatchingMode", typeof(HatchingMode));
        Add(keys, "general.harvestType", typeof(HarvestType));

        foreach (var c in new[] { "white", "brown", "blue", "void", "gold" })
        {
            Add(keys, $"roosters.{c}.buildingRequired", typeof(RequiredBuilding));
            Add(keys, $"roosters.{c}.produceMode", typeof(ProduceMode));
            Add(keys, $"roosters.{c}.deluxeProduceMode", typeof(DeluxeProduceMode));
        }

        foreach (var s in new[] { "whiteChicken", "brownChicken", "blueChicken", "voidChicken", "goldenChicken" })
            Add(keys, $"females.{s}.assignment", typeof(FemaleRoosterAssignment));
    }
}
