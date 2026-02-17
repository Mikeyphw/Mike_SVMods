using LetMeHatchThisEgg.Config;
using LetMeHatchThisEgg.Core.Abstractions;

namespace LetMeHatchThisEgg.Infrastructure.Runtime;

/// <summary>
/// Aggregates the constructed runtime services for LMH.
/// </summary>
public sealed class LmheRuntime
{
    /// <summary>Root runtime context (core/config/logger/assets/integrations).</summary>
    public LmheContext Context { get; }

    /// <summary>Configuration provider.</summary>
    public IConfigProvider<ModConfig> ConfigProvider { get; }

    /// <summary>Logger abstraction.</summary>
    public ILogger Logger { get; }

    /// <summary>Asset registry abstraction.</summary>
    public IAssetRegistry AssetRegistry { get; }

    /// <summary>Integration service abstraction.</summary>
    public IIntegrationService Integrations { get; }

    /// <summary>Creates a new runtime bundle.</summary>
    public LmheRuntime(
        LmheContext context,
        IConfigProvider<ModConfig> configProvider,
        ILogger logger,
        IAssetRegistry assetRegistry,
        IIntegrationService integrations
    )
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        ConfigProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        AssetRegistry = assetRegistry ?? throw new ArgumentNullException(nameof(assetRegistry));
        Integrations = integrations ?? throw new ArgumentNullException(nameof(integrations));
    }
}
