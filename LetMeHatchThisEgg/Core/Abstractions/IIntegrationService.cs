using ContentPatcher;
using LetMeHatchThisEgg.Framework;

namespace LetMeHatchThisEgg.Core.Abstractions;

/// <summary>Provides access to optional integrations (Content Patcher, WildEggs, RoostersAddon).</summary>
public interface IIntegrationService
{
    /// <summary>Content Patcher API if available; otherwise <see langword="null"/>.</summary>
    IContentPatcherAPI? ContentPatcher { get; }

    /// <summary>WildEggsHatching API if available; otherwise <see langword="null"/>.</summary>
    IWildEggsApi? WildEggs { get; }

    /// <summary>Rooster Control Addon API if available; otherwise <see langword="null"/>.</summary>
    IRoosterHatchingAPI? RoostersAddon { get; }
}
