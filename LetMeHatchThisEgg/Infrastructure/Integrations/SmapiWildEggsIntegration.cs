using LetMeHatchThisEgg.Core.Abstractions.Integrations;
using LetMeHatchThisEgg.Framework;
using StardewModdingAPI;

namespace LetMeHatchThisEgg.Infrastructure.Integrations;

/// <summary>WildEggsHatching integration adapter resolved via SMAPI mod registry.</summary>
public sealed class SmapiWildEggsIntegration : IWildEggsIntegration
{
    private const string ModId = "Mikew.wildeggshatching";

    public bool IsAvailable => Api is not null;

    public IWildEggsApi? Api { get; }

    public SmapiWildEggsIntegration(IModRegistry modRegistry)
    {
        if (modRegistry is null)
            throw new ArgumentNullException(nameof(modRegistry));

        Api = modRegistry.GetApi<IWildEggsApi>(ModId);
    }
}
