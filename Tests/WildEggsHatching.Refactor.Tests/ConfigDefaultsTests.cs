using WildEggsHatching.Infrastructure.Config;
using Xunit;

namespace WildEggsHatching.Refactor.Tests;

public sealed class ConfigDefaultsTests
{
    [Fact]
    public void Defaults_Are_Stable()
    {
        var cfg = new ModConfig();

        Assert.True(cfg.ProgressionMode);
        Assert.True(cfg.NeedsFern);
        Assert.True(cfg.HatchDinoEggs);
        Assert.Equal("6000", cfg.DinoPrice);
        Assert.Equal("default", cfg.VoidChickenPrice);
        Assert.Equal("500", cfg.OstrichPrice);
    }
}
