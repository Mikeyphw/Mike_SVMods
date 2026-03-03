using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using RoosterControl.Application.Compat;
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

    private CompatSelectionService _compatSelection = null!;

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
            "ra_compat",
            "Compat selection commands. Use: ra_compat help",
            CompatCommand
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
            "Dev-only: dump daily hatching plan decisions. Usage: ra_debug_plan [--full] [--json] [day] [locationName] [buildingType]",
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
        _compatSelection = new CompatSelectionService(Helper, ModManifest, _log);
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

        RequestInvalidate(FarmAnimalsAssetName);

        // Avoid side effects in AssetRequested; refresh and reload after the cache invalidation.
        QueueRefreshFarmAnimals("update rate: location change");
}

        private void EnsureCompatInitialized()
    {
        if (_compatSelection.IsInitialized)
            return;

        var changed = _compatSelection.InitializeOrRepair(_saveState.Compat, out var summary);

        if (_compatSelection.EggCatalog.Sources.Count == 0)
            LogScopedHelper.Log(_log, ModRoosterControlCategory.Assets,
                $"Compat egg sources catalog is empty (asset: Mods/{ModManifest.UniqueID}/compat/egg_sources). Egg resolution will be disabled until a pack provides sources.",
                LogLevel.Warn);

        if (_compatSelection.RoosterCatalog.Sources.Count == 0)
            LogScopedHelper.Log(_log, ModRoosterControlCategory.Assets,
                $"Compat rooster sources catalog is empty (asset: Mods/{ModManifest.UniqueID}/compat/rooster_sources). Coop rooster scan overrides will be cleared and provider-based roles disabled until a pack provides sources.",
                LogLevel.Warn);

        if (!changed)
            return;

        _needsRoosterSetup = true;
        InvalidateDailyPlanCache("compat selection repaired");

        if (!string.IsNullOrWhiteSpace(summary))
            LogScopedHelper.Log(_log, ModRoosterControlCategory.SaveData, $"Compat selection repaired:\n{summary}", LogLevel.Info);
        else
            LogScopedHelper.Log(_log, ModRoosterControlCategory.SaveData, "Compat selection repaired.", LogLevel.Info);

        if (Context.IsMainPlayer && Context.IsWorldReady)
            _saveData.Write(SaveStateKey, _saveState);

        RequestInvalidate(FarmAnimalsAssetName);
        if (RoosterHelper.FemaleEggs.Count > 0)
            RequestInvalidate(ObjectsAssetName);

        QueueRefreshFarmAnimals("compat selection repaired");
    }


    public void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        _saveState = _saveData.Read<State.RoosterControlSaveState>(SaveStateKey) ?? new State.RoosterControlSaveState();

        if (_saveState.SchemaVersion < 2)
        {
            _saveState.SchemaVersion = 2;
            _saveState.CurrentDailyDecisions = null;
            _saveState.PreviousDailyDecisions = null;
        }

        if (_saveState.SchemaVersion < 3)
            _saveState.SchemaVersion = 3;

        EnsureCompatInitialized();

        _needsRoosterSetup = true;
        _lastRoosterSetupDay = -1;
        _lastDebugDumpDay = -1;

        InvalidateDailyPlanCache("save loaded");

        _previousDailyDecisions = _saveState.CurrentDailyDecisions ?? _saveState.PreviousDailyDecisions;
        if (RoosterHelper is not null)
            RoosterHelper.InvalidateProduceResolverCaches();

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

        _saveState.SchemaVersion = 3;
        _saveState.LastRoosterSetupDay = _lastRoosterSetupDay;
        _saveState.LastDebugDumpDay = _lastDebugDumpDay;
        _saveState.LastKnownLuck = DailyLuck;

        _saveState.CurrentDailyDecisions = _dailyDecisions;
        _saveState.PreviousDailyDecisions = _previousDailyDecisions;

        _saveData.Write(SaveStateKey, _saveState);
    }

    public void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        HasDayStarted = false;
        InvalidateDailyPlanCache("returned to title");

        if (RoosterHelper is not null)
            RoosterHelper.InvalidateProduceResolverCaches();


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



private void QueueRefreshFarmAnimals(string reason)
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

private void QueueRefreshFarmAnimalsAfterDailyDecisions()
    => QueueRefreshFarmAnimals("daily decisions applied");


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
