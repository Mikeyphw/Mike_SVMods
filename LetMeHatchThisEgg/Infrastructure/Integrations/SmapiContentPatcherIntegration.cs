using ContentPatcher;
using LetMeHatchThisEgg.Core.Abstractions.Integrations;
using StardewModdingAPI;

namespace LetMeHatchThisEgg.Infrastructure.Integrations;

/// <summary>Content Patcher integration adapter resolved via SMAPI mod registry.</summary>
public sealed class SmapiContentPatcherIntegration : IContentPatcherIntegration
{
    private const string ModId = "Pathoschild.ContentPatcher";

    public bool IsAvailable => Api is not null;

    public IContentPatcherAPI? Api { get; }

    public SmapiContentPatcherIntegration(IModRegistry modRegistry)
    {
        if (modRegistry is null)
            throw new ArgumentNullException(nameof(modRegistry));

        Api = modRegistry.GetApi<IContentPatcherAPI>(ModId);
    }
}
