using System.Collections.Generic;
using LetMeHatch.Core.ClonePipeline;
using Microsoft.Xna.Framework;
using Xunit;

namespace LetMeHatch.Core.Tests;

public sealed class ColorResolverTests
{
    [Fact]
    public void Resolve_ConfigOverride_Wins()
    {
        var resolver = new ColorResolver();
        var ctx = new ColorResolutionContext(
            Key: "eggA",
            SeedKey: "seed",
            ConfigOverrides: new Dictionary<string, Color> { ["eggA"] = Color.Aquamarine },
            SavedOverrides: new Dictionary<string, Color> { ["eggA"] = Color.Red },
            ContextTags: new[] { "color_blue" }
        );

        Assert.Equal(Color.Aquamarine, resolver.Resolve(ctx));
    }

    [Fact]
    public void Resolve_SavedOverride_Second()
    {
        var resolver = new ColorResolver();
        var ctx = new ColorResolutionContext(
            Key: "eggA",
            SeedKey: "seed",
            ConfigOverrides: null,
            SavedOverrides: new Dictionary<string, Color> { ["eggA"] = Color.Red },
            ContextTags: new[] { "color_blue" }
        );

        Assert.Equal(Color.Red, resolver.Resolve(ctx));
    }

    [Fact]
    public void Resolve_ContextTags_Third()
    {
        var resolver = new ColorResolver();
        var ctx = new ColorResolutionContext(
            Key: "eggA",
            SeedKey: "seed",
            ContextTags: new[] { "color_green", "color_red" }
        );

        Assert.Equal(Color.Green, resolver.Resolve(ctx));
    }

    [Fact]
    public void Resolve_Fallback_IsDeterministic()
    {
        var resolver = new ColorResolver();

        var a = resolver.Resolve(new ColorResolutionContext("k", "seed-key"));
        var b = resolver.Resolve(new ColorResolutionContext("k", "seed-key"));

        Assert.Equal(a, b);
    }
}
