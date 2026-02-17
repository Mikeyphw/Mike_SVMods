using LetMeHatchThisEgg.Core.Abstractions.Integrations;
using LetMeHatchThisEgg.Framework;
using StardewModdingAPI;

namespace LetMeHatchThisEgg.Infrastructure.Integrations;

/// <summary>Roosters Addon integration adapter resolved via SMAPI mod registry.</summary>
public sealed class SmapiRoostersAddonIntegration : IRoostersAddonIntegration
{
    private const string ModId = "Mikew.roostersaddon.smapi";

    public bool IsAvailable => Api is not null;

    public IRoosterHatchingAPI? Api { get; }

    public SmapiRoostersAddonIntegration(IModRegistry modRegistry)
    {
        if (modRegistry is null)
            throw new ArgumentNullException(nameof(modRegistry));

        Api = modRegistry.GetApi<IRoosterHatchingAPI>(ModId);
    }
}
