using LetMeHatchThisEgg.Framework;

namespace LetMeHatchThisEgg.Core.Abstractions.Integrations;

/// <summary>Adapter for WildEggsHatching integration.</summary>
public interface IWildEggsIntegration
{
    /// <summary>Whether the integration API is available.</summary>
    bool IsAvailable { get; }

    /// <summary>The underlying WildEggsHatching API if available; otherwise <see langword="null"/>.</summary>
    IWildEggsApi? Api { get; }
}
