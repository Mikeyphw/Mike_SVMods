using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RoosterControl.Application.Catalogs;
using RoosterControl.Enums;
using RoosterControl.Framework.Config;
using Xunit;

namespace RoosterControl.Descriptors.Tests;

public sealed class DescriptorRegistryTests
{
    [Fact]
    public void Config_Defaults_ContainAllRoosterTypes()
    {
        var cfg = RoosterControlConfig.CreateDefault();
        var expected = Enum.GetValues<RoosterType>().ToHashSet();

        Assert.Equal(expected.Count, cfg.Roosters.Count);
        foreach (var roosterType in expected)
            Assert.Contains(roosterType, cfg.Roosters.Keys);
    }

    [Fact]
    public void CatalogDefaults_ContainAllRoosterTypeNames()
    {
        var defaults = CatalogDefaults.CreateDefaults();
        var expectedNames = Enum.GetNames<RoosterType>().ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.True(expectedNames.SetEquals(defaults.Roosters.Roosters.Keys));
        Assert.True(expectedNames.SetEquals(defaults.Produce.Roosters.Keys));
        Assert.True(expectedNames.SetEquals(defaults.Hatching.Roosters.Keys));
    }

    [Fact]
    public void I18n_DefaultJson_IsPresent_AndValidJson()
    {
        var path = FindDefaultI18nPath();
        var json = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(File.ReadAllText(path));

        Assert.NotNull(json);
        Assert.NotEmpty(json!);
    }

    private static string FindDefaultI18nPath()
    {
        var candidates = new[]
        {
            Path.Combine("Mike_SVMods", "RoosterControl", "i18n", "default.json"),
            Path.Combine("RoosterControl", "i18n", "default.json"),
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

        throw new FileNotFoundException("Could not find RoosterControl i18n/default.json by walking up from test output directory.");
    }
}
