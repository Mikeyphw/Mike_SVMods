using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley.GameData.FarmAnimals;
using EggsInWild.Infrastructure.ModData.Models;

namespace EggsInWild.Application.Animals;

/// <summary>Resolves a preview sprite for a wild animal using mapped farm animal textures (original behavior).</summary>
public static class WildAnimalPreviewResolver
{
    public sealed record Preview(string FarmAnimalKey, string TexturePath, Rectangle SourceRect);

    public static Preview? TryResolvePreview(
        string wildAnimalKey,
        WildAnimalJson wildAnimal,
        IModRegistry registry,
        IReadOnlyDictionary<string, FarmAnimalData> farmAnimals)
    {
        if (wildAnimal.ExtraModAnimalKeys.Count == 0)
            return null;

        // Prefer mappings from installed mods; fall back to Vanilla if present.
        var candidateBlocks = wildAnimal.ExtraModAnimalKeys
            .Where(kvp => IsVanilla(kvp.Key) || registry.IsLoaded(kvp.Key))
            .OrderBy(kvp => IsVanilla(kvp.Key) ? 1 : 0) // non-vanilla first
            .Select(kvp => kvp.Value)
            .ToList();

        foreach (var block in candidateBlocks)
        {
            var key = PickFarmAnimalKey(block, farmAnimals);
            if (key is null)
                continue;

            var data = farmAnimals[key];
            var rect = new Rectangle(0, 0, data.SpriteWidth, data.SpriteHeight);
            return new Preview(key, data.Texture, rect);
        }

        return null;
    }

    private static bool IsVanilla(string modId) =>
        string.Equals(modId, "Vanilla", StringComparison.OrdinalIgnoreCase);

    private static string? PickFarmAnimalKey(ExtraModAnimalKeysJson block, IReadOnlyDictionary<string, FarmAnimalData> farmAnimals)
    {
        foreach (var key in block.GetEffectiveFarmAnimalKeys())
        {
            if (farmAnimals.ContainsKey(key))
                return key;
        }

        return null;
    }
}
