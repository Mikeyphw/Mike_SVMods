using LetMeHatchThisEgg.Framework;

namespace LetMeHatchThisEgg.Core.Abstractions.Integrations;

/// <summary>Adapter for Roosters Addon integration.</summary>
public interface IRoostersAddonIntegration
{
    /// <summary>Whether the integration API is available.</summary>
    bool IsAvailable { get; }

    /// <summary>The underlying Roosters Addon API if available; otherwise <see langword="null"/>.</summary>
    IRoosterHatchingAPI? Api { get; }
}
