using ContentPatcher;
using LetMeHatchThisEgg.Core.Abstractions;
using LetMeHatchThisEgg.Framework;
using StardewModdingAPI;

namespace LetMeHatchThisEgg.Infrastructure.Runtime;

/// <summary>Simple runtime service implementations used by the composition root.</summary>
internal sealed class StaticConfigProvider<TConfig> : IConfigProvider<TConfig>
    where TConfig : class
{
    public TConfig Config { get; }

    public StaticConfigProvider(TConfig config)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));
    }
}

/// <summary>SMAPI-backed asset registry.</summary>
internal sealed class SmapiAssetRegistry : IAssetRegistry
{
    private readonly IModHelper helper;

    public string ModUniqueId { get; }

    public SmapiAssetRegistry(string modUniqueId, IModHelper helper)
    {
        ModUniqueId = string.IsNullOrWhiteSpace(modUniqueId)
            ? throw new ArgumentException("Value cannot be null/empty.", nameof(modUniqueId))
            : modUniqueId;
        this.helper = helper ?? throw new ArgumentNullException(nameof(helper));
    }

    public string GetModAssetPath(string relativePath)
        => helper.ModContent.GetInternalAssetName(relativePath).Name;
}

/// <summary>Static integration service wrapper.</summary>
internal sealed class StaticIntegrationService : IIntegrationService
{
    public IContentPatcherAPI? ContentPatcher { get; }
    public IWildEggsApi? WildEggs { get; }
    public IRoosterHatchingAPI? RoostersAddon { get; }

    public StaticIntegrationService(LmheIntegrations integrations)
    {
        ContentPatcher = integrations.ContentPatcher;
        WildEggs = integrations.WildEggs;
        RoostersAddon = integrations.RoostersAddon;
    }
}
