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
using IWildEggsApi = Mikew.NestCore.Framework.IWildEggsApi;
using LuckyTypes = Mikew.NestCore.EnumsAndStrings.Enums.LuckyTypes;
using Mikew.NestCore.Helpers;
using Mikew.NestCore.Helpers.Content;
using Mikew.NestCore.Helpers.Timing;
using Mikew.NestCore.Helpers.Logging;
using RoosterControl.Infrastructure.Assets;
using RoosterControl.Application.FarmAnimals;
using NestAssetData = Mikew.NestCore.Models.AssetData;
using NestModAssetHandler = Mikew.NestCore.Helpers.ModAssetHandler;
using Mikew.NestCore.Diagnostics;

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

    internal I18TranslationHelper I18n => _i18n;
internal RoosterHelper RoosterHelper = null!;
internal ILetMeHatchApi? HatchApi;
internal IWildEggsApi? WildEggsApi;

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
            "ra",
            "RoosterControl commands. Use: ra help",
            RaCommand
        );

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

        if (TryLoadFarmAnimals(out var farmAnimals, out _))
            EnsureCompatValidationState(farmAnimals, reason: "save loaded", log: true, category: ModRoosterControlCategory.SaveData);

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
        foreach (var line in BuildStatusText().Split('\n', StringSplitOptions.RemoveEmptyEntries))
            LogScopedHelper.Log(_log, ModRoosterControlCategory.General, line, LogLevel.Info);
    }

    internal string BuildStatusText()
        => string.Join(
            Environment.NewLine,
            $"WorldReady={Context.IsWorldReady}",
            $"MainPlayer={Context.IsMainPlayer}",
            $"LetMeHatchApiReady={HatchApi is not null}",
            $"LetMeHatchApiVersion={(HatchApi is null ? "n/a" : HatchApi.ApiVersion)}",
            $"LetMeHatchReady={(HatchApi is not null && HatchApi.FinishedEarlyStuff)}",
            $"WildEggsApiReady={WildEggsApi is not null}",
            $"WildEggsApiVersion={(WildEggsApi is null ? "n/a" : WildEggsApi.ApiVersion)}",
            $"WildEggsParityAnimals={(WildEggsApi?.ValidAnimals.Count ?? 0)}",
            $"Catalogs: roosters={_catalogs.Roosters.Roosters.Count}, produce={_catalogs.Produce.Roosters.Count}, hatching={_catalogs.Hatching.Roosters.Count}",
            $"InvalidateBatchPending~={_debugInvalidationsRequested.Count}",
            $"FarmAnimalsRefreshQueued={_farmAnimalsRefreshQueued}",
            $"NeedsRoosterSetup={_needsRoosterSetup}");

    internal DiagnosticReport BuildVerifyReport()
    {
        var issues = new List<DiagnosticIssue>
        {
            new("WORLD_READY", Context.IsWorldReady ? DiagnosticSeverity.Pass : DiagnosticSeverity.Warn, "runtime", Context.IsWorldReady ? "World is ready." : "World not ready."),
            new("MAIN_PLAYER", Context.IsMainPlayer ? DiagnosticSeverity.Pass : DiagnosticSeverity.Warn, "runtime", Context.IsMainPlayer ? "Running on host player." : "Running on farmhand or title screen."),
            new("LMH_API", HatchApi is not null ? DiagnosticSeverity.Pass : DiagnosticSeverity.Warn, "integration", HatchApi is not null ? $"LetMeHatch API ready ({HatchApi.ApiVersion})." : "LetMeHatch API not available."),
            new("WILD_EGGS_API", WildEggsApi is not null ? DiagnosticSeverity.Pass : DiagnosticSeverity.Warn, "integration", WildEggsApi is not null ? $"WildEggs API ready ({WildEggsApi.ApiVersion})." : "WildEggs API not available."),
            new("CATALOG_ROOSTERS", _catalogs.Roosters.Roosters.Count > 0 ? DiagnosticSeverity.Pass : DiagnosticSeverity.Fail, "catalog", $"Roosters catalog count={_catalogs.Roosters.Roosters.Count}."),
            new("CATALOG_PRODUCE", _catalogs.Produce.Roosters.Count > 0 ? DiagnosticSeverity.Pass : DiagnosticSeverity.Fail, "catalog", $"Produce catalog count={_catalogs.Produce.Roosters.Count}."),
            new("CATALOG_HATCHING", _catalogs.Hatching.Roosters.Count > 0 ? DiagnosticSeverity.Pass : DiagnosticSeverity.Fail, "catalog", $"Hatching catalog count={_catalogs.Hatching.Roosters.Count}."),
        };

        return new DiagnosticReport("ra verify", issues);
    }

    private void RaCommand(string command, string[] args)
    {
        if (args.Length == 0 || IsHelpArg(args[0]))
        {
            Monitor.Log(BuildRaHelpText(), LogLevel.Info);
            return;
        }

        switch (args[0].ToLowerInvariant())
        {
            case "help":
                Monitor.Log(BuildRaHelpText(), LogLevel.Info);
                return;
            case "status":
                PrintStatus();
                return;
            case "verify":
                HandleRaVerify(args.Skip(1).ToArray());
                return;
            case "reload":
                HandleRaReload(args.Skip(1).ToArray());
                return;
            case "save":
                HandleSaveCommand(args.Skip(1).ToArray());
                return;
            case "compat":
                CompatCommand(command, args.Skip(1).ToArray());
                return;
            case "catalog":
                CatalogCommand(command, args.Skip(1).ToArray());
                return;
            case "plan":
                DailyPlanCommand(command, args.Skip(1).ToArray());
                return;
            case "debug":
#if RCTRL_DEV
                HandleRaDebug(command, args.Skip(1).ToArray());
#else
                Monitor.Log("Dev-only commands are unavailable in this build.", LogLevel.Warn);
#endif
                return;
            default:
                Monitor.Log($"Unknown subcommand '{args[0]}'.\n\n{BuildRaHelpText()}", LogLevel.Warn);
                return;
        }
    }

    private void HandleRaVerify(string[] args)
    {
        var json = args.Any(static x => string.Equals(x, "--json", StringComparison.OrdinalIgnoreCase));
        var filteredArgs = args.Where(static x => !string.Equals(x, "--json", StringComparison.OrdinalIgnoreCase)).ToArray();

        if (filteredArgs.Length == 0)
        {
            var report = BuildVerifyReport();
            var rendered = json
                ? DiagnosticReportFormatter.FormatJson(report, "ra verify")
                : DiagnosticReportFormatter.FormatText(report, "ra verify");
            Monitor.Log(rendered, report.FailCount > 0 ? LogLevel.Warn : LogLevel.Info);
            return;
        }

        switch (filteredArgs[0].ToLowerInvariant())
        {
            case "compat":
                CompatCommand("ra", new[] { "validate" }.Concat(filteredArgs.Skip(1)).ToArray());
                return;
            case "catalog":
                CatalogCommand("ra", new[] { "validate" }.Concat(filteredArgs.Skip(1)).ToArray());
                return;
            default:
                Monitor.Log($"Unknown verify area '{filteredArgs[0]}'.\n\n{BuildRaHelpText()}", LogLevel.Warn);
                return;
        }
    }

    private void HandleRaReload(string[] args)
    {
        if (args.Length == 0 || string.Equals(args[0], "catalog", StringComparison.OrdinalIgnoreCase))
        {
            HandleCatalogReload();
            return;
        }

        Monitor.Log($"Unknown reload area '{args[0]}'.\n\n{BuildRaHelpText()}", LogLevel.Warn);
    }

    private void HandleSaveCommand(string[] args)
    {
        if (args.Length == 0 || IsHelpArg(args[0]))
        {
            Monitor.Log("Usage: ra save clear --yes", LogLevel.Info);
            return;
        }

        if (!string.Equals(args[0], "clear", StringComparison.OrdinalIgnoreCase))
        {
            Monitor.Log($"Unknown save subcommand '{args[0]}'.\n\n{BuildRaHelpText()}", LogLevel.Warn);
            return;
        }

        if (!args.Any(static x => string.Equals(x, "--yes", StringComparison.OrdinalIgnoreCase)))
        {
            Monitor.Log("Refusing to clear save data without confirmation. Usage: ra save clear --yes", LogLevel.Warn);
            return;
        }

        if (!Context.IsWorldReady)
        {
            Monitor.Log("Save not loaded.", LogLevel.Warn);
            return;
        }

        if (!Context.IsMainPlayer)
        {
            Monitor.Log("Host-only.", LogLevel.Warn);
            return;
        }

        _saveData.Delete(SaveStateKey);
        _saveData.Delete(DailyPlanSaveKey);
        _saveData.Delete(DailyPlanBackupSaveKey);

        _saveState = new State.RoosterControlSaveState();
        _dailyPlan = null;
        _dailyDecisions = null;
        _previousDailyDecisions = null;
        _needsRoosterSetup = true;
        _lastRoosterSetupDay = -1;
        _lastDebugDumpDay = -1;

        Monitor.Log("Cleared RoosterControl save data for this save.", LogLevel.Info);
    }

#if RCTRL_DEV
    private void HandleRaDebug(string command, string[] args)
    {
        if (args.Length == 0 || IsHelpArg(args[0]))
        {
            Monitor.Log(BuildRaDebugHelpText(), LogLevel.Info);
            return;
        }

        switch (args[0].ToLowerInvariant())
        {
            case "produce":
                DebugProduceCommand(command, args.Skip(1).ToArray());
                return;
            case "plan":
                DebugPlanCommand(command, args.Skip(1).ToArray());
                return;
            case "diff":
                DebugDiffCommand(command, args.Skip(1).ToArray());
                return;
            case "pipeline":
                DebugPipelineCommand(command, args.Skip(1).ToArray());
                return;
            case "state-dump":
                DumpStateCommand(command, args.Skip(1).ToArray());
                return;
            default:
                Monitor.Log($"Unknown debug subcommand '{args[0]}'.\n\n{BuildRaDebugHelpText()}", LogLevel.Warn);
                return;
        }
    }
#endif

    private static bool IsHelpArg(string arg)
        => string.Equals(arg, "help", StringComparison.OrdinalIgnoreCase)
           || string.Equals(arg, "-h", StringComparison.OrdinalIgnoreCase)
           || string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase);

    private static string BuildRaHelpText()
        => string.Join(
            Environment.NewLine,
            "ra help",
            "ra status",
            "ra verify [--json]",
            "ra verify compat [EggKind]",
            "ra verify catalog",
            "ra reload",
            "ra save clear --yes",
            "ra compat ...",
            "ra catalog ...",
            "ra plan ...",
#if RCTRL_DEV
            "ra debug ...",
#endif
            "",
            "Status prints a concise runtime snapshot.",
            "Verify runs read-only RoosterControl diagnostics.");

#if RCTRL_DEV
    private static string BuildRaDebugHelpText()
        => string.Join(
            Environment.NewLine,
            "ra debug produce ...",
            "ra debug plan ...",
            "ra debug diff ...",
            "ra debug pipeline",
            "ra debug state-dump [farmanimals]");
#endif


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
