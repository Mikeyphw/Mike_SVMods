using System.Text.RegularExpressions;
using RoosterAddon.Application.Gmcm;
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
    public void Pages_ContainAllRoosterPages()
    {
        var index = RoosterGmcmDescriptorIndex.Build();

        var expected = new[]
        {
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
        var re = new Regex(@"^(general|roosters)\.", RegexOptions.Compiled);

        foreach (var id in index.OptionIds)
            Assert.Matches(re, id);
    }
}
