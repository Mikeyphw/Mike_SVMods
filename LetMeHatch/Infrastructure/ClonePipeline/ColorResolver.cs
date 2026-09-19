using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Xna.Framework;

namespace LetMeHatch.Infrastructure.ClonePipeline;

/// <summary>
/// Deterministic color resolution:
/// config override → saved override → context tags → stable fallback palette.
/// </summary>
public sealed class ColorResolver : IColorResolver
{
    private static readonly IReadOnlyDictionary<string, Color> TagColors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
    {
        ["red"] = Color.Red,
        ["orange"] = Color.Orange,
        ["yellow"] = Color.Yellow,
        ["green"] = Color.Green,
        ["blue"] = Color.CornflowerBlue,
        ["purple"] = Color.Purple,
        ["pink"] = Color.HotPink,
        ["brown"] = new Color(139, 69, 19),
        ["black"] = Color.Black,
        ["white"] = Color.White,
        ["gray"] = Color.Gray,
        ["grey"] = Color.Gray,
    };

    private static readonly Color[] FallbackPalette =
    [
        new Color(255, 99, 71),    // tomato
        new Color(255, 165, 0),    // orange
        new Color(255, 215, 0),    // gold
        new Color(50, 205, 50),    // lime green
        new Color(64, 224, 208),   // turquoise
        new Color(100, 149, 237),  // cornflower
        new Color(138, 43, 226),   // blue violet
        new Color(255, 105, 180),  // hot pink
        new Color(160, 82, 45),    // sienna
        new Color(192, 192, 192),  // silver
    ];

    public Color Resolve(ColorResolutionContext context)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        if (context.ConfigOverrides is not null && context.ConfigOverrides.TryGetValue(context.Key, out var configColor))
            return configColor;

        if (context.SavedOverrides is not null && context.SavedOverrides.TryGetValue(context.Key, out var savedColor))
            return savedColor;

        var tagColor = TryResolveFromTags(context.ContextTags);
        if (tagColor.HasValue)
            return tagColor.Value;

        return ResolveFallback(context.SeedKey);
    }

    private static Color? TryResolveFromTags(IReadOnlyCollection<string>? tags)
    {
        if (tags is null || tags.Count == 0)
            return null;

        foreach (var tag in tags.OrderBy(static t => t, StringComparer.OrdinalIgnoreCase))
        {
            if (!tag.StartsWith("color_", StringComparison.OrdinalIgnoreCase))
                continue;

            var key = tag.Substring("color_".Length);
            if (TagColors.TryGetValue(key, out var color))
                return color;
        }

        return null;
    }

    private static Color ResolveFallback(string seedKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(seedKey ?? string.Empty));
        var v = BitConverter.ToUInt32(bytes, 0);
        return FallbackPalette[v % (uint)FallbackPalette.Length];
    }
}
