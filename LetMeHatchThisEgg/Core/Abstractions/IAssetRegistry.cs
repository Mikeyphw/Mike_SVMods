namespace LetMeHatchThisEgg.Core.Abstractions;

/// <summary>Abstraction over LMH asset naming/paths.</summary>
public interface IAssetRegistry
{
    /// <summary>Gets the owning mod's unique ID.</summary>
    string ModUniqueId { get; }

    /// <summary>Gets a mod-relative asset path (e.g. "assets/foo.png").</summary>
    string GetModAssetPath(string relativePath);
}
