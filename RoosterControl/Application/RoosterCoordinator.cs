using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using RoosterControl.Application.Hatching;
using RoosterControl.Application.Catalogs;
using RoosterControl.Constants;
using RoosterControl.Enums;
using RoosterControl.Framework;
using RoosterControl.Framework.Config;
using RoosterControl.Helpers;
using RoosterControl.Infrastructure.SaveData;
using RoosterControl.Infrastructure.StateDumps;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.GameData.FarmAnimals;

using ILetMeHatchApi = Mikew.NestCore.Framework.ILetMeHatchApi;
using LuckyTypes = Mikew.NestCore.EnumsAndStrings.Enums.LuckyTypes;
using Mikew.NestCore.Helpers;
using Mikew.NestCore.Helpers.Content;
using Mikew.NestCore.Helpers.Timing;
using Mikew.NestCore.Helpers.Logging;
using RoosterControl.Infrastructure.Assets;
using RoosterControl.Application.FarmAnimals;
using NestAssetData = Mikew.NestCore.Models.AssetData;
using NestModAssetHandler = Mikew.NestCore.Helpers.ModAssetHandler;

using static Mikew.NestCore.Helpers.Logging.ScopedLogCatalog;

namespace RoosterControl.Application;

internal sealed partial class RoosterCoordinator : IRoosterCoordinator
{
    private const string FarmAnimalsAssetName = "Data/FarmAnimals";
    private const string ObjectsAssetName = "Data/Objects";

    private readonly ModEntry _host;
    private readonly ISaveDataStore _saveData;
    private readonly ILog _log;
    private readonly I18TranslationHelper _i18n;

    

    private readonly CatalogManager _catalogManager;
    private CatalogBundle _catalogs = new();

    private IModHelper Helper => _host.Helper;
    private IMonitor Monitor => _host.Monitor;
    private IManifest ModManifest => _host.ModManifest;

    private string SaveStateKey => $"{ModManifest.UniqueID}-save-state";

    private RoosterControlConfig? _config;

    internal RoosterControlConfig Config
    {
        get => _config ?? throw new InvalidOperationException("Config not initialized yet.");
        set
        {
            _config = value ?? throw new ArgumentNullException(nameof(value));
            RoosterHelper?.UpdateConfig(_config);
        }
    }
    internal RoosterHelper RoosterHelper = null!;
internal ILetMeHatchApi? HatchApi;

    internal Dictionary<Textures, Dictionary<TextureDetails, string>> ModTextures = new();

    internal NestModAssetHandler AssetHandler = null!;
    private AssetPipelineHost<RoosterCoordinator>? _assetPipeline;

    private State.RoosterControlSaveState _saveState = new();

    private AnimalExclusion _animalExclusions = new();

    

    
    private readonly FarmAnimalExclusionsService _farmAnimalExclusionsService = new();
private readonly FarmAnimalHatchingService _farmAnimalHatching = new();
private NextTickScheduler _nextTick;
    private AssetInvalidationBatcher _invalidations;

    private readonly HashSet<string> _debugInvalidationsRequested = new(StringComparer.OrdinalIgnoreCase);
    private bool _debugInvalidationClearQueued;

    private bool _farmAnimalsRefreshQueued;
    private bool _femaleAssignmentsDirty;
    private bool _needsRoosterSetup = true;
    private int _lastRoosterSetupDay = -1;
    private int _lastDebugDumpDay = -1;

    private bool HasDayStarted;
    private LuckyTypes DailyLuck = LuckyTypes.Neutral;

    // "Stuck inside" affects harvest type.
    private bool isStuckInside;

    internal RoosterCoordinator(ModEntry host, ISaveDataStore saveData)
    {
        _host = host;
        _saveData = saveData;
        _log = new SmapiLog(Monitor);
        _i18n = new I18TranslationHelper(host.Helper);
        _catalogManager = new CatalogManager(_log);
        _nextTick = new NextTickScheduler(host.Helper, _log);
        _invalidations = new AssetInvalidationBatcher(host.Helper, _log, _nextTick);
    }

    internal void Initialize()
    {
        LoadExclusionsData();

        var configPath = Path.Combine(Helper.DirectoryPath, "config.json");
        var isNewConfig = !File.Exists(configPath);

        Config = ConfigManager.LoadOrCreate(Helper, Monitor);

        _catalogs = _catalogManager.LoadOrCreateAll(Helper, Monitor);
        var catalogsChangedConfig = _catalogManager.ApplyToConfig(Config, _catalogs, isNewConfig, Config.General.CatalogApplyMode, Monitor);
        if (catalogsChangedConfig)
            Helper.WriteConfig(Config);

        Helper.ConsoleCommands.Add(
            "roostercontrol_dump_state",
            "Write a JSON snapshot of RoosterControl state to data/state-dumps. Add 'farmanimals' to include farm animal keys.",
            DumpStateCommand
        );

                Helper.ConsoleCommands.Add(
    "ra_plan",
    "Daily hatching plan commands. Use: ra_plan help",
    DailyPlanCommand
);

Helper.ConsoleCommands.Add(
    "ra_catalog",
    "Catalog commands. Use: ra_catalog help",
    CatalogCommand
);

Helper.ConsoleCommands.Add(
    "ra_status",
    "Print RoosterControl runtime/integration status.",
    (_, _) => PrintStatus()
);

#if RCTRL_DEV
        Helper.ConsoleCommands.Add(
            "ra_debug_produce",
            "Dev-only: dump deterministic chosen-produce decisions. Usage: ra_debug_produce [--full] [--json] <roosterType> [day] [locationName] [buildingType]",
            DebugProduceCommand
        );

        Helper.ConsoleCommands.Add(
            "ra_debug_plan",
            "Dev-only: dump daily hatching plan snapshot. Usage: ra_debug_plan [--full] [--json] [day] [buildingKey]",
            DebugPlanCommand
        );

        Helper.ConsoleCommands.Add(
            "ra_debug_diff",
            "Dev-only: diff previous vs current daily decisions snapshot. Usage: ra_debug_diff [--full] [--json]",
            DebugDiffCommand
        );

        Helper.ConsoleCommands.Add(
            "ra_debug_pipeline",
            "Dev-only: dump rooster produce/hatching pipeline state. Usage: ra_debug_pipeline",
            DebugPipelineCommand
        );
private void DebugPipelineCommand(string _command, string[] args)
{
    var sb = new StringBuilder();

    sb.AppendLine($"WorldReady={Context.IsWorldReady} MainPlayer={Context.IsMainPlayer} HasDayStarted={HasDayStarted}");
    sb.AppendLine($"FarmAnimalsRefreshQueued={_farmAnimalsRefreshQueued}");

    if (_debugInvalidationsRequested.Count > 0)
        sb.AppendLine($"InvalidationsRequested={string.Join(", ", _debugInvalidationsRequested)}");
    else
        sb.AppendLine("InvalidationsRequested=<none>");

    if (!Context.IsWorldReady)
    {
        Monitor.Log(sb.ToString(), LogLevel.Info);
        return;
    }

    var today = SDate.Now().DaysSinceStart;
    var farmId = unchecked((long)Game1.uniqueIDForThisGame);

    sb.AppendLine($"Day={today} FarmId={farmId} DailyLuck={DailyLuck}");
    sb.AppendLine($"DailyPlanFingerprint={_dailyPlanFingerprint}");

    if (_dailyDecisions is null)
    {
        sb.AppendLine("DailyDecisions=<null>");
        Monitor.Log(sb.ToString(), LogLevel.Info);
        return;
    }

    sb.AppendLine($"DailyDecisions.Day={_dailyDecisions.Day} FarmId={_dailyDecisions.FarmId} Fingerprint={_dailyDecisions.Fingerprint}");
    sb.AppendLine($"DailyDecisions.MatchesToday={_dailyDecisions.MatchesToday(farmId, today)}");

    var plan = _dailyDecisions.Plan;
    if (plan is null)
    {
        sb.AppendLine("DailyPlan=<null>");
        Monitor.Log(sb.ToString(), LogLevel.Info);
        return;
    }

    var buildings = plan.Buildings?.Count ?? 0;
    var roosterCount = 0;
    var femaleCount = 0;
    var normalEggPlans = 0;
    var largeEggPlans = 0;
    var fertilizedOnlyPlans = 0;

    foreach (var b in plan.Buildings.Values)
    {
        roosterCount += b.Roosters.Count;
        foreach (var r in b.Roosters.Values)
        {
            femaleCount += r.Females.Count;
            foreach (var f in r.Females.Values)
            {
                if (f.NormalEgg is not null) normalEggPlans++;
                if (f.LargeEgg is not null) largeEggPlans++;
                if (f.FertilizedOnly is not null) fertilizedOnlyPlans++;
            }
        }
    }

    sb.AppendLine($"DailyPlan: version={plan.Version} nonce={plan.Nonce} fingerprint={plan.Fingerprint}");
    sb.AppendLine($"DailyPlanCounts: buildings={buildings} roosters={roosterCount} females={femaleCount} eggPlans(normal={normalEggPlans}, large={largeEggPlans}) fertilizedOnly={fertilizedOnlyPlans}");

    Monitor.Log(sb.ToString(), LogLevel.Info);
}

#endif


AssetHandler = new NestModAssetHandler(ModManifest.UniqueID, Monitor);
        AssetHandler.AddAssets(
            new List<NestAssetData>
            {
                new(
                    enumAssetType: typeof(Textures),
                    enumAssetFolder: RoosterControlAssetFolder.Textures,
                    fileExt: NestModAssetHandler.FileExtension.Png,
                    gamePath: $"Mods/{ModManifest.UniqueID}"
                )
            }
        );
        AssetHandler.SetUpAssets();

        var texturesList = new List<Textures>
        {
            Textures.AllRoosters,
            Textures.BlueRooster,
            Textures.BrownRooster,
            Textures.GoldRooster,
            Textures.VoidRooster,
            Textures.WhiteRooster,
            Textures.VoidShopIcon,
            Textures.GoldShopIcon
        };

        foreach (var item in texturesList)
        {
            ModTextures[item] = new Dictionary<TextureDetails, string>
            {
                { TextureDetails.GamePath, AssetHandler.GetAssetPath(item, NestModAssetHandler.AssetPathType.GamePath) },
                { TextureDetails.MoldFolderPath, AssetHandler.GetAssetPath(item, NestModAssetHandler.AssetPathType.ModFolderPath) }
            };
        }

        RoosterHelper = new RoosterHelper(Config, Helper, Monitor, _catalogs.Roosters, _catalogs.Produce);
        InitializeAssetPipeline();
    }

    public bool HasModCheck(string modID) => Helper.ModRegistry.IsLoaded(modID);

    public static bool HasMod(string modID, IModHelper helper)
    {
        if (modID.Contains("&&", StringComparison.Ordinal))
        {
            var mods = modID.Split("&&", StringSplitOptions.TrimEntries);
            foreach (var m in mods)
            {
                if (!helper.ModRegistry.IsLoaded(m))
                    return false;
            }
            return true;
        }

        if (modID.Contains("||", StringComparison.Ordinal))
        {
            var mods = modID.Split("||", StringSplitOptions.TrimEntries);
            foreach (var m in mods)
            {
                if (helper.ModRegistry.IsLoaded(m))
                    return true;
            }
            return false;
        }

        return helper.ModRegistry.IsLoaded(modID);
    }

    public void OnWarped(object? sender, WarpedEventArgs e)
    {
        if (Config.General.UpdateRate != UpdateRateMode.OnLocationChange)
            return;

        InvalidateRuleCaches("update rate: location change");
        _ = Helper.GameContent.InvalidateCache(FarmAnimalsAssetName);

        // Avoid side effects in AssetRequested; refresh and reload after the cache invalidation.
        _nextTick.Enqueue(RefreshFarmAnimalsOnLocationChange);
    }

    public void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        _saveState = _saveData.Read<State.RoosterControlSaveState>(SaveStateKey) ?? new State.RoosterControlSaveState();
        _needsRoosterSetup = true;
        _lastRoosterSetupDay = -1;
        _lastDebugDumpDay = -1;
        InvalidateDailyPlanCache("save loaded");

        RunStartupValidationV2();

        RequestGmcmRebuild();
    }

    public void OnSaving(object? sender, SavingEventArgs e)
    {
        if (_femaleAssignmentsDirty)
        {
            ConfigManager.Save(Helper, Config);
            _femaleAssignmentsDirty = false;
        }


        _saveState.LastRoosterSetupDay = _lastRoosterSetupDay;
        _saveState.LastDebugDumpDay = _lastDebugDumpDay;
        _saveState.LastKnownLuck = DailyLuck;

        _saveData.Write(SaveStateKey, _saveState);
    }

    public void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        HasDayStarted = false;
        InvalidateDailyPlanCache("returned to title");

        ResetSchedulers();
        _debugInvalidationsRequested.Clear();
        _debugInvalidationClearQueued = false;
        _farmAnimalsRefreshQueued = false;

RequestGmcmRebuild();
    }

    
private void ResetSchedulers()
{
    _invalidations.Dispose();
    _nextTick.Dispose();

    _nextTick = new NextTickScheduler(Helper, _log);
    _invalidations = new AssetInvalidationBatcher(Helper, _log, _nextTick);
}

private void RequestInvalidate(string assetName)
{
    if (string.IsNullOrWhiteSpace(assetName))
        return;

    if (assetName.Equals(FarmAnimalsAssetName, StringComparison.OrdinalIgnoreCase))
        InvalidateRuleCaches("FarmAnimals invalidated");

    _invalidations.Request(assetName);

    // Debug-only: keep a short-lived snapshot of invalidation requests for pipeline dumps.
    _debugInvalidationsRequested.Add(assetName);
    if (!_debugInvalidationClearQueued)
    {
        _debugInvalidationClearQueued = true;
        _nextTick.Enqueue(() =>
        {
            _debugInvalidationClearQueued = false;
            _debugInvalidationsRequested.Clear();
        });
    }
}


private void QueueRefreshFarmAnimalsAfterDailyDecisions()
{
    if (_farmAnimalsRefreshQueued)
        return;

    _farmAnimalsRefreshQueued = true;
    _nextTick.Enqueue(() =>
    {
        _farmAnimalsRefreshQueued = false;
        RefreshFarmAnimalsOnLocationChange();
    });
}


    private void PrintStatus()
    {
        LogScopedHelper.Log(_log, ModRoosterControlCategory.General, $"WorldReady={Context.IsWorldReady}", LogLevel.Info);
        LogScopedHelper.Log(_log, ModRoosterControlCategory.General, $"MainPlayer={Context.IsMainPlayer}", LogLevel.Info);
        LogScopedHelper.Log(_log, ModRoosterControlCategory.General, $"LetMeHatchApiReady={HatchApi is not null}", LogLevel.Info);
        LogScopedHelper.Log(_log, ModRoosterControlCategory.General,
            $"Catalogs: roosters={_catalogs.Roosters.Roosters.Count}, produce={_catalogs.Produce.Roosters.Count}, hatching={_catalogs.Hatching.Roosters.Count}",
            LogLevel.Info);
        LogScopedHelper.Log(_log, ModRoosterControlCategory.General, $"InvalidateBatchPending~={_debugInvalidationsRequested.Count}", LogLevel.Info);
        LogScopedHelper.Log(_log, ModRoosterControlCategory.General, $"FarmAnimalsRefreshQueued={_farmAnimalsRefreshQueued}", LogLevel.Info);
        LogScopedHelper.Log(_log, ModRoosterControlCategory.General, $"NeedsRoosterSetup={_needsRoosterSetup}", LogLevel.Info);
    }


    private void LoadExclusionsData()
    {
        _animalExclusions = _farmAnimalExclusionsService.LoadOrCreate(Helper);
    }

    

#if RCTRL_DEV
    private void DebugProduceCommand(string _command, string[] args)
    {
        if (RoosterHelper is null)
        {
            Monitor.Log("[ra_debug_produce] RoosterHelper is not initialized yet.", LogLevel.Warn);
            return;
        }

    private static (List<string> Positional, bool Full, bool Json) ParseDebugArgs(string[] args)
    {
        var positional = new List<string>();
        var full = false;
        var json = false;

        foreach (var raw in args ?? Array.Empty<string>())
        {
            var token = (raw ?? string.Empty).Trim();
            if (token.Length == 0)
                continue;

            if (token.Equals("--full", StringComparison.OrdinalIgnoreCase))
            {
                full = true;
                continue;
            }

            if (token.Equals("--json", StringComparison.OrdinalIgnoreCase))
            {
                json = true;
                continue;
            }

            positional.Add(raw);
        }

        return (positional, full, json);
    }

    private void DebugPlanCommand(string _command, string[] args)
    {
        var (positional, full, json) = ParseDebugArgs(args);

        if (positional.Count > 0 && positional[0].Equals("help", StringComparison.OrdinalIgnoreCase))
        {
            Monitor.Log(
                "Usage: ra_debug_plan [--full] [--json] [day] [buildingKey]\n" +
                "Example: ra_debug_plan\n" +
                "Example: ra_debug_plan 1234 \"Deluxe Coop\"\n" +
                "Example: ra_debug_plan --json",
                LogLevel.Info
            );
            return;
        }

        if (!Context.IsWorldReady)
        {
            Monitor.Log("[ra_debug_plan] Save not loaded.", LogLevel.Info);
            return;
        }

        int? dayOverride = null;
        string? buildingKeyFilter = null;

        if (positional.Count >= 1 && int.TryParse(positional[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var day))
        {
            dayOverride = day;
            if (positional.Count >= 2)
                buildingKeyFilter = positional[1];
        }
        else if (positional.Count >= 1)
        {
            buildingKeyFilter = positional[0];
        }

        var plan = _dailyDecisions?.Plan ?? _dailyPlan;
        if (plan is null)
        {
            try
            {
                var data = Helper.GameContent.Load<Dictionary<string, FarmAnimalData>>(FarmAnimalsAssetName);
                plan = EnsureDailyPlanForToday(data, forceRecompute: false, out _);
            }
            catch (Exception ex)
            {
                Monitor.Log($"[ra_debug_plan] Failed to load {FarmAnimalsAssetName}: {ex}", LogLevel.Error);
                return;
            }
        }

        if (plan is null)
        {
            Monitor.Log("[ra_debug_plan] No plan available.", LogLevel.Info);
            return;
        }

        if (dayOverride is not null && plan.Day != dayOverride.Value)
        {
            Monitor.Log($"[ra_debug_plan] Plan day {plan.Day} does not match requested day {dayOverride.Value}.", LogLevel.Info);
            return;
        }

        var dump = json
            ? JsonSerializer.Serialize(BuildDebugPlanDump(plan, buildingKeyFilter, full), new JsonSerializerOptions { WriteIndented = false })
            : BuildDebugPlanText(plan, buildingKeyFilter, full);

        Monitor.Log(dump, LogLevel.Info);
    }

    private void DebugDiffCommand(string _command, string[] args)
    {
        var (positional, full, json) = ParseDebugArgs(args);

        if (positional.Count > 0 && positional[0].Equals("help", StringComparison.OrdinalIgnoreCase))
        {
            Monitor.Log(
                "Usage: ra_debug_diff [--full] [--json]\n" +
                "Example: ra_debug_diff\n" +
                "Example: ra_debug_diff --full\n" +
                "Example: ra_debug_diff --json",
                LogLevel.Info
            );
            return;
        }

        if (_dailyDecisions is null)
        {
            Monitor.Log("[ra_debug_diff] No current daily decisions snapshot.", LogLevel.Info);
            return;
        }

        if (_previousDailyDecisions is null)
        {
            Monitor.Log("[ra_debug_diff] No previous daily decisions snapshot (nothing to diff yet).", LogLevel.Info);
            return;
        }

        var dump = json
            ? JsonSerializer.Serialize(BuildDebugDiffDump(_previousDailyDecisions, _dailyDecisions, full), new JsonSerializerOptions { WriteIndented = false })
            : BuildDebugDiffText(_previousDailyDecisions, _dailyDecisions, full);

        Monitor.Log(dump, LogLevel.Info);
    }

    private sealed record DebugPlanDump(
        int Day,
        long FarmId,
        string Fingerprint,
        long Nonce,
        List<DebugBuildingPlan> Buildings
    );

    private sealed record DebugBuildingPlan(string BuildingKey, List<DebugRoosterPlan> Roosters);
    private sealed record DebugRoosterPlan(string RoosterKey, List<DebugFemalePlan> Females);
    private sealed record DebugFemalePlan(string FemaleKey, EggChancePlan? NormalEgg, EggChancePlan? LargeEgg, FertilizedOnlyPlan? FertilizedOnly);

    private DebugPlanDump BuildDebugPlanDump(DailyHatchingPlanEnvelope plan, string? buildingKeyFilter, bool full)
    {
        var buildings = new List<DebugBuildingPlan>();

        foreach (var (buildingKey, building) in plan.Buildings.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(buildingKeyFilter)
                && !buildingKey.Contains(buildingKeyFilter.Trim(), StringComparison.OrdinalIgnoreCase))
                continue;

            var roosters = new List<DebugRoosterPlan>();
            foreach (var (roosterKey, rooster) in building.Roosters.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
            {
                var females = new List<DebugFemalePlan>();
                foreach (var (femaleKey, female) in rooster.Females.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
                {
                    females.Add(new DebugFemalePlan(
                        FemaleKey: femaleKey,
                        NormalEgg: full ? female.NormalEgg : null,
                        LargeEgg: full ? female.LargeEgg : null,
                        FertilizedOnly: full ? female.FertilizedOnly : null
                    ));
                }

                roosters.Add(new DebugRoosterPlan(roosterKey, females));
            }

            buildings.Add(new DebugBuildingPlan(buildingKey, roosters));
        }

        return new DebugPlanDump(plan.Day, plan.FarmId, plan.Fingerprint, plan.Nonce, buildings);
    }

    private string BuildDebugPlanText(DailyHatchingPlanEnvelope plan, string? buildingKeyFilter, bool full)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[ra_debug_plan] Day={plan.Day} FarmId={plan.FarmId} Fingerprint={plan.Fingerprint} Nonce={plan.Nonce}");

        foreach (var (buildingKey, building) in plan.Buildings.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(buildingKeyFilter)
                && !buildingKey.Contains(buildingKeyFilter.Trim(), StringComparison.OrdinalIgnoreCase))
                continue;

            sb.AppendLine($"Building: {buildingKey}");

            foreach (var (roosterKey, rooster) in building.Roosters.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
            {
                sb.AppendLine($"  Rooster: {roosterKey}");

                foreach (var (femaleKey, female) in rooster.Females.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"    Female: {femaleKey}");

                    if (!full)
                        continue;

                    if (female.NormalEgg is not null)
                        sb.AppendLine($"      NormalEgg: enabled={female.NormalEgg.Enabled} chance={female.NormalEgg.ChancePercent} rolled={female.NormalEgg.RolledSuccess} add={female.NormalEgg.EggIdToAddToRooster} remove={female.NormalEgg.FemaleEggIdToRemove}");

                    if (female.LargeEgg is not null)
                        sb.AppendLine($"      LargeEgg: enabled={female.LargeEgg.Enabled} chance={female.LargeEgg.ChancePercent} rolled={female.LargeEgg.RolledSuccess} add={female.LargeEgg.EggIdToAddToRooster} remove={female.LargeEgg.FemaleEggIdToRemove}");

                    if (female.FertilizedOnly is not null)
                        sb.AppendLine($"      FertilizedOnly: upgradeChance={female.FertilizedOnly.FemaleUpgradeChancePercent} ratio={female.FertilizedOnly.RoosterToChickenRatioPercent} upgrade={female.FertilizedOnly.UpgradeFemaleProduceToFertilized} roosterHatches={female.FertilizedOnly.RoosterHatchesFertilizedEgg} roosterEgg={female.FertilizedOnly.RoosterFertilizedEggId} femaleEgg={female.FertilizedOnly.FemaleFertilizedEggId}");
                }
            }
        }

        return sb.ToString().TrimEnd();
    }

    private sealed record DebugDiffDump(
        int PreviousDay,
        string PreviousFingerprint,
        int CurrentDay,
        string CurrentFingerprint,
        List<string> Changes
    );

    private DebugDiffDump BuildDebugDiffDump(DailyFarmAnimalsDecisions prev, DailyFarmAnimalsDecisions curr, bool full)
    {
        var changes = BuildDebugDiffChanges(prev.Plan, curr.Plan, full);
        return new DebugDiffDump(prev.Day, prev.Fingerprint, curr.Day, curr.Fingerprint, changes);
    }

    private string BuildDebugDiffText(DailyFarmAnimalsDecisions prev, DailyFarmAnimalsDecisions curr, bool full)
    {
        var changes = BuildDebugDiffChanges(prev.Plan, curr.Plan, full);

        var sb = new StringBuilder();
        sb.AppendLine($"[ra_debug_diff] prevDay={prev.Day} prevFingerprint={prev.Fingerprint}");
        sb.AppendLine($"[ra_debug_diff] currDay={curr.Day} currFingerprint={curr.Fingerprint}");

        if (changes.Count == 0)
            sb.AppendLine("No changes detected.");
        else
            changes.ForEach(c => sb.AppendLine(c));

        return sb.ToString().TrimEnd();
    }

    private static List<string> BuildDebugDiffChanges(DailyHatchingPlanEnvelope? prev, DailyHatchingPlanEnvelope? curr, bool full)
    {
        var changes = new List<string>();

        if (prev is null && curr is null)
            return changes;

        if (prev is null)
        {
            changes.Add("Plan created (no previous plan).");
            return changes;
        }

        if (curr is null)
        {
            changes.Add("Plan cleared (no current plan).");
            return changes;
        }

        if (!string.Equals(prev.Fingerprint, curr.Fingerprint, StringComparison.OrdinalIgnoreCase))
            changes.Add($"Fingerprint changed: '{prev.Fingerprint}' -> '{curr.Fingerprint}'");

        if (!full)
        {
            changes.Add($"Buildings: prev={prev.Buildings.Count} curr={curr.Buildings.Count}");
            return changes;
        }

        var prevBuildings = prev.Buildings.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var currBuildings = curr.Buildings.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var b in prevBuildings.Except(currBuildings).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            changes.Add($"Building removed: {b}");
        foreach (var b in currBuildings.Except(prevBuildings).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            changes.Add($"Building added: {b}");

        foreach (var b in prevBuildings.Intersect(currBuildings).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            var pb = prev.Buildings[b];
            var cb = curr.Buildings[b];

            var prevRoosters = pb.Roosters.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var currRoosters = cb.Roosters.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var r in prevRoosters.Except(currRoosters).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                changes.Add($"Rooster removed: {b}/{r}");
            foreach (var r in currRoosters.Except(prevRoosters).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                changes.Add($"Rooster added: {b}/{r}");

            foreach (var r in prevRoosters.Intersect(currRoosters).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                var pr = pb.Roosters[r];
                var cr = cb.Roosters[r];

                var prevFemales = pr.Females.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
                var currFemales = cr.Females.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var f in prevFemales.Except(currFemales).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                    changes.Add($"Female removed: {b}/{r}/{f}");
                foreach (var f in currFemales.Except(prevFemales).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                    changes.Add($"Female added: {b}/{r}/{f}");

                foreach (var f in prevFemales.Intersect(currFemales).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                {
                    var pf = pr.Females[f];
                    var cf = cr.Females[f];

                    if (!AreEggPlansEqual(pf.NormalEgg, cf.NormalEgg))
                        changes.Add($"NormalEgg changed: {b}/{r}/{f}");
                    if (!AreEggPlansEqual(pf.LargeEgg, cf.LargeEgg))
                        changes.Add($"LargeEgg changed: {b}/{r}/{f}");
                    if (!AreFertilizedPlansEqual(pf.FertilizedOnly, cf.FertilizedOnly))
                        changes.Add($"FertilizedOnly changed: {b}/{r}/{f}");
                }
            }
        }

        return changes;
    }

    private static bool AreEggPlansEqual(EggChancePlan? a, EggChancePlan? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;

        return a.Enabled == b.Enabled
            && a.ChancePercent == b.ChancePercent
            && a.RolledSuccess == b.RolledSuccess
            && string.Equals(a.EggIdToAddToRooster, b.EggIdToAddToRooster, StringComparison.OrdinalIgnoreCase)
            && string.Equals(a.FemaleEggIdToRemove ?? string.Empty, b.FemaleEggIdToRemove ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static bool AreFertilizedPlansEqual(FertilizedOnlyPlan? a, FertilizedOnlyPlan? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;

        return Math.Abs(a.FemaleUpgradeChancePercent - b.FemaleUpgradeChancePercent) < 0.0001f
            && a.RoosterToChickenRatioPercent == b.RoosterToChickenRatioPercent
            && a.UpgradeFemaleProduceToFertilized == b.UpgradeFemaleProduceToFertilized
            && a.RoosterHatchesFertilizedEgg == b.RoosterHatchesFertilizedEgg
            && string.Equals(a.RoosterFertilizedEggId, b.RoosterFertilizedEggId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(a.FemaleFertilizedEggId, b.FemaleFertilizedEggId, StringComparison.OrdinalIgnoreCase);
    }


        var (positional, full, json) = ParseDebugArgs(args);

        if (positional.Count == 0 || positional[0].Equals("help", StringComparison.OrdinalIgnoreCase))
        {
            Monitor.Log(
                "Usage: ra_debug_produce [--full] [--json] <roosterType> [day] [locationName] [buildingType]\n" +
                "Example: ra_debug_produce WhiteRooster\n" +
                "Example: ra_debug_produce --full WhiteRooster 1234 Farm \"Deluxe Coop\"\n" +
                "Example: ra_debug_produce --json WhiteRooster",
                LogLevel.Info
            );
            return;
        }

        if (!Enum.TryParse<RoosterType>(positional[0], ignoreCase: true, out var roosterType))
        {
            Monitor.Log($"[ra_debug_produce] Unknown roosterType '{positional[0]}'. Valid: {string.Join(", ", Enum.GetNames(typeof(RoosterType)))}", LogLevel.Warn);
            return;
        }

        int? dayOverride = null;
        if (positional.Count >= 2 && int.TryParse(positional[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var day))
            dayOverride = day;

        var locationOverride = positional.Count >= 3 ? positional[2] : null;
        var buildingOverride = positional.Count >= 4 ? positional[3] : null;

        var dump = RoosterHelper.DebugDumpChosenProduce(
            roosterType,
            dayOverride,
            locationOverride,
            buildingOverride,
            full: full,
            json: json
        );

        Monitor.Log(dump, LogLevel.Info);
    }

        if (args.Length == 0 || args[0].Equals("help", StringComparison.OrdinalIgnoreCase))
        {
            Monitor.Log(
                "Usage: ra_debug_produce <roosterType> [day] [locationName] [buildingType]\n" +
                "Example: ra_debug_produce WhiteRooster\n" +
                "Example: ra_debug_produce WhiteRooster 1234 Farm DeluxeCoop",
                LogLevel.Info
            );
            return;
        }

        if (!Enum.TryParse<RoosterType>(args[0], ignoreCase: true, out var roosterType))
        {
            Monitor.Log($"[ra_debug_produce] Unknown roosterType '{args[0]}'. Valid: {string.Join(", ", Enum.GetNames(typeof(RoosterType)))}", LogLevel.Warn);
            return;
        }

        int? dayOverride = null;
        if (args.Length >= 2 && int.TryParse(args[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var day))
            dayOverride = day;

        var locationOverride = args.Length >= 3 ? args[2] : null;
        var buildingOverride = args.Length >= 4 ? args[3] : null;

        var dump = RoosterHelper.DebugDumpChosenProduce(roosterType, dayOverride, locationOverride, buildingOverride);
        Monitor.Log(dump, LogLevel.Info);
    }
private void DebugPipelineCommand(string _command, string[] args)
{
    var sb = new StringBuilder();

    sb.AppendLine($"WorldReady={Context.IsWorldReady} MainPlayer={Context.IsMainPlayer} HasDayStarted={HasDayStarted}");
    sb.AppendLine($"FarmAnimalsRefreshQueued={_farmAnimalsRefreshQueued}");

    if (_debugInvalidationsRequested.Count > 0)
        sb.AppendLine($"InvalidationsRequested={string.Join(", ", _debugInvalidationsRequested)}");
    else
        sb.AppendLine("InvalidationsRequested=<none>");

    if (!Context.IsWorldReady)
    {
        Monitor.Log(sb.ToString(), LogLevel.Info);
        return;
    }

    var today = SDate.Now().DaysSinceStart;
    var farmId = unchecked((long)Game1.uniqueIDForThisGame);

    sb.AppendLine($"Day={today} FarmId={farmId} DailyLuck={DailyLuck}");
    sb.AppendLine($"DailyPlanFingerprint={_dailyPlanFingerprint}");

    if (_dailyDecisions is null)
    {
        sb.AppendLine("DailyDecisions=<null>");
        Monitor.Log(sb.ToString(), LogLevel.Info);
        return;
    }

    sb.AppendLine($"DailyDecisions.Day={_dailyDecisions.Day} FarmId={_dailyDecisions.FarmId} Fingerprint={_dailyDecisions.Fingerprint}");
    sb.AppendLine($"DailyDecisions.MatchesToday={_dailyDecisions.MatchesToday(farmId, today)}");

    var plan = _dailyDecisions.Plan;
    if (plan is null)
    {
        sb.AppendLine("DailyPlan=<null>");
        Monitor.Log(sb.ToString(), LogLevel.Info);
        return;
    }

    var buildings = plan.Buildings?.Count ?? 0;
    var roosterCount = 0;
    var femaleCount = 0;
    var normalEggPlans = 0;
    var largeEggPlans = 0;
    var fertilizedOnlyPlans = 0;

    foreach (var b in plan.Buildings.Values)
    {
        roosterCount += b.Roosters.Count;
        foreach (var r in b.Roosters.Values)
        {
            femaleCount += r.Females.Count;
            foreach (var f in r.Females.Values)
            {
                if (f.NormalEgg is not null) normalEggPlans++;
                if (f.LargeEgg is not null) largeEggPlans++;
                if (f.FertilizedOnly is not null) fertilizedOnlyPlans++;
            }
        }
    }

    sb.AppendLine($"DailyPlan: version={plan.Version} nonce={plan.Nonce} fingerprint={plan.Fingerprint}");
    sb.AppendLine($"DailyPlanCounts: buildings={buildings} roosters={roosterCount} females={femaleCount} eggPlans(normal={normalEggPlans}, large={largeEggPlans}) fertilizedOnly={fertilizedOnlyPlans}");

    Monitor.Log(sb.ToString(), LogLevel.Info);
}

#endif

private void DumpStateCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            LogScopedHelper.Log(_log, ModRoosterControlCategory.General, "Save not loaded; nothing to dump yet.", LogLevel.Info);
            return;
        }

        var includeFarmAnimals = args.Any(a => a.Equals("farmanimals", StringComparison.OrdinalIgnoreCase));
        var payload = BuildStateDump(includeFarmAnimals);

        var now = DateTimeOffset.Now;
        var fileName = StateDumpJson.BuildFileName("roostercontrol_state", now);
        WriteStateDump(fileName, payload);

        LogScopedHelper.Log(_log, ModRoosterControlCategory.General, $"Wrote state dump: {fileName}", LogLevel.Info);
    }

    private object BuildStateDump(bool includeFarmAnimals)
    {
        var today = SDate.Now().DaysSinceStart;
        var roosters = RoosterHelper?.Roosters?.Values.Select(r => new
        {
            r.RoosterType,
            r.FarmAnimalName,
            r.Female,
            Females = r.Females?.ToList() ?? new List<string>(),
            Eggs = r.Eggs,
            ProduceOption = r.ProduceOption,
            ChosenProduce = r.AllProduces.TryGetValue(ProduceTypeEntries.Produce, out var produce) && produce.TryGetValue(ProduceEntries.Chosen, out var chosen) ? chosen : null,
        }).ToList() ?? new();

        object? farmAnimals = null;
        if (includeFarmAnimals)
        {
            try
            {
                var data = Helper.GameContent.Load<Dictionary<string, FarmAnimalData>>(FarmAnimalsAssetName);
                farmAnimals = data.Keys.OrderBy(k => k).ToList();
            }
            catch (Exception ex)
            {
                farmAnimals = new { error = ex.Message };
            }
        }

        return new
        {
            Timestamp = DateTimeOffset.Now,
            Mod = new { ModManifest.UniqueID, ModManifest.Version },
            Save = StardewModdingAPI.Constants.SaveFolderName,
            Day = new { today, Game1.Date.Season, Game1.Date.DayOfMonth, Game1.Date.Year },
            Luck = DailyLuck,
            Config = Config,
            SaveState = _saveState,
            FemaleAssignments = Config.FemalesOptions,
            Exclusions = _animalExclusions.AnimalExclusions,
            Roosters = roosters,
            FarmAnimals = farmAnimals
        };
    }

    private void WriteStateDump(string fileName, object payload)
    {
        var dumpDir = Path.Combine(Helper.DirectoryPath, "data", "state-dumps");
        var json = StateDumpJson.SerializeIndented(payload);
        StateDumpJson.WriteUtf8(dumpDir, fileName, json);
    }
}