using System;
using System.Collections.Generic;
using StardewModdingAPI;
using EggsInWild.Infrastructure.Config;
using EggsInWild.Infrastructure.ModData;
using EggsInWild.DesertTomb.Core.Configuration;
using EggsInWild.Infrastructure.DesertTomb.Monsters.Definitions;
using EggsInWild.Infrastructure.Integrations.StardewUi.WildAnimals;

namespace EggsInWild.Infrastructure.Integrations.Gmcm;

/// <summary>
/// GMCM context that holds explicit dependencies for the GMCM menu system.
/// </summary>
public sealed class EiwGmcmContext
{
    public IModHelper Helper { get; }
    public IMonitor Monitor { get; }
    public IManifest Manifest { get; }
    public Func<ModConfig> GetConfig { get; }
    public Action SaveConfig { get; }
    internal WildAnimalsEditorService WildAnimalsEditor { get; }
    internal Action? OpenWildAnimalsEditor { get; set; }
    internal bool IsWildAnimalsEditorAvailable { get; set; }

    /// <summary>
    /// Bulk state storage for ExtraModAnimalKeys per wild animal key.
    /// </summary>
    internal Dictionary<string, ExtraModAnimalKeysBulkState> ExtraModAnimalKeysBulkByWildAnimalKey { get; }

    /// <summary>
    /// Known location type keys used for normalization.
    /// </summary>
    public HashSet<string> KnownLocationTypeKeys { get; set; } = new(StringComparer.OrdinalIgnoreCase);

#if WEGG_DEV
    /// <summary>
    /// Dev JSON tuning service (only available in WEGG_DEV builds).
    /// </summary>
    internal DevJsonTuningService? DevJsonTuning { get; set; }

    /// <summary>
    /// Dev monster definitions (only available in WEGG_DEV builds).
    /// </summary>
    internal Dictionary<string, TombMonsterDefinitionJson>? DevMonsterDefinitions { get; set; }

    /// <summary>
    /// Callback for writing dev monster definition changes.
    /// </summary>
    internal Action<Action>? MutateDevMonsterDefinitions { get; set; }

    /// <summary>
    /// Callback for normalizing field keys used in dev JSON controls.
    /// </summary>
    internal Func<string, string>? NormalizeDevJsonKey { get; set; }
#endif

    public EiwGmcmContext(
        IModHelper helper,
        IMonitor monitor,
        IManifest manifest,
        Func<ModConfig> getConfig,
        Action saveConfig)
    {
        Helper = helper ?? throw new ArgumentNullException(nameof(helper));
        Monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
        GetConfig = getConfig ?? throw new ArgumentNullException(nameof(getConfig));
        SaveConfig = saveConfig ?? throw new ArgumentNullException(nameof(saveConfig));
        WildAnimalsEditor = new WildAnimalsEditorService(Helper, Monitor, GetConfig);

        ExtraModAnimalKeysBulkByWildAnimalKey = new Dictionary<string, ExtraModAnimalKeysBulkState>(StringComparer.OrdinalIgnoreCase);
    }
}
