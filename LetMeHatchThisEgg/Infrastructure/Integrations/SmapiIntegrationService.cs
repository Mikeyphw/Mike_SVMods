using LetMeHatchThisEgg.Core.Abstractions;
using LetMeHatchThisEgg.Core.Abstractions.Integrations;
using StardewModdingAPI;

namespace LetMeHatchThisEgg.Infrastructure.Integrations;

/// <summary>SMAPI-backed integration resolver with null-object fallbacks.</summary>
public sealed class SmapiIntegrationService : IIntegrationService
{
    public IContentPatcherIntegration ContentPatcher { get; }
    public IWildEggsIntegration WildEggs { get; }
    public IRoostersAddonIntegration RoostersAddon { get; }

    public SmapiIntegrationService(IModRegistry modRegistry)
    {
        if (modRegistry is null)
            throw new ArgumentNullException(nameof(modRegistry));

        ContentPatcher = new SmapiContentPatcherIntegration(modRegistry);
        WildEggs = new SmapiWildEggsIntegration(modRegistry);
        RoostersAddon = new SmapiRoostersAddonIntegration(modRegistry);
    }
}
