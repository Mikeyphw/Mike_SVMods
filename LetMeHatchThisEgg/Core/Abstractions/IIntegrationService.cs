using LetMeHatchThisEgg.Core.Abstractions.Integrations;

namespace LetMeHatchThisEgg.Core.Abstractions;

/// <summary>Provides access to optional integrations (Content Patcher, WildEggs, RoostersAddon) via adapters.</summary>
public interface IIntegrationService
{
    /// <summary>Content Patcher integration adapter (may be unavailable).</summary>
    IContentPatcherIntegration ContentPatcher { get; }

    /// <summary>WildEggsHatching integration adapter (may be unavailable).</summary>
    IWildEggsIntegration WildEggs { get; }

    /// <summary>Roosters Addon integration adapter (may be unavailable).</summary>
    IRoostersAddonIntegration RoostersAddon { get; }
}
