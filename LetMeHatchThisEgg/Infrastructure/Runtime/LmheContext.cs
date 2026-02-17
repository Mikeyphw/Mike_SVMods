using LetMeHatchThisEgg.Config;
using LetMeHatchThisEgg.Infrastructure.Logging;
using Mikew.BirdsOfAFeather.Framework;
using Mikew.BirdsOfAFeather.Helpers;

namespace LetMeHatchThisEgg.Infrastructure.Runtime;

/// <summary>
/// Root runtime context for LMH. Intended as the single object passed into composition root / DI.
/// </summary>
public sealed class LmheContext
{
    /// <summary>BirdsOfAFeather core context (helper/monitor/manifest/i18n).</summary>
    public CoreContext Core { get; }

    /// <summary>LMH configuration.</summary>
    public ModConfig Config { get; }

    /// <summary>LMH logger wrapper.</summary>
    public LmheLogger Logger { get; }

    /// <summary>BirdsOfAFeather asset handler for this mod.</summary>
    public ModAssetHandler Assets { get; }

    /// <summary>Optional integration APIs.</summary>
    public LmheIntegrations Integrations { get; }

    /// <summary>Creates a new LMH context.</summary>
    public LmheContext(
        CoreContext core,
        ModConfig config,
        LmheLogger logger,
        ModAssetHandler assets,
        LmheIntegrations? integrations = null
    )
    {
        Core = core ?? throw new ArgumentNullException(nameof(core));
        Config = config ?? throw new ArgumentNullException(nameof(config));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Assets = assets ?? throw new ArgumentNullException(nameof(assets));
        Integrations = integrations ?? new(null, null, null);
    }
}
