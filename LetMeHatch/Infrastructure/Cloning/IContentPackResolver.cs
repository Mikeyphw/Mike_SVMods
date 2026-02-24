using StardewModdingAPI;

namespace LetMeHatch.Infrastructure.Cloning;

/// <summary>Resolves loaded content packs by unique ID.</summary>
public interface IContentPackResolver
{
    /// <summary>Resolve a content pack by its manifest unique ID.</summary>
    IContentPack GetById(string uniqueId);
}
