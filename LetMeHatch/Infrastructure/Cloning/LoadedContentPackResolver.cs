using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;

namespace LetMeHatch.Infrastructure.Cloning;

/// <summary>Resolver over the currently loaded content packs list.</summary>
public sealed class LoadedContentPackResolver : IContentPackResolver
{
    private readonly IReadOnlyList<IContentPack> _contentPacks;

    public LoadedContentPackResolver(IReadOnlyList<IContentPack> contentPacks)
    {
        _contentPacks = contentPacks ?? throw new ArgumentNullException(nameof(contentPacks));
    }

    public IContentPack GetById(string uniqueId)
    {
        if (string.IsNullOrWhiteSpace(uniqueId))
            throw new ArgumentException("Unique ID is required.", nameof(uniqueId));

        return _contentPacks.First(cp => cp.Manifest.UniqueID == uniqueId);
    }
}
