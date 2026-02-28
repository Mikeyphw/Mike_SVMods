using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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
private readonly HashSet<string> _pendingInvalidations = new(StringComparer.OrdinalIgnoreCase);
    private readonly Queue<Action> _nextTickActions = new();
    private bool _nextTickHooked;
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
            "Dev-only: dump deterministic chosen-produce seed + results. Usage: ra_debug_produce <roosterType> [day] [locationName] [buildingType]",
            DebugProduceCommand
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
        QueueNextTickAction(RefreshFarmAnimalsOnLocationChange);
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
        _pendingInvalidations.Clear();
        _nextTickActions.Clear();

        if (_nextTickHooked)
        {
            Helper.Events.GameLoop.UpdateTicked -= OnNextTick;
            _nextTickHooked = false;
        }

        RequestGmcmRebuild();
    }

    private void RequestInvalidate(string assetName)
    {
        if (string.IsNullOrWhiteSpace(assetName))
            return;

        if (assetName.Equals(FarmAnimalsAssetName, StringComparison.OrdinalIgnoreCase))
            InvalidateRuleCaches("FarmAnimals invalidated");

        _pendingInvalidations.Add(assetName);
        QueueNextTickAction(FlushInvalidations);
    }

    private void PrintStatus()
    {
        LogScopedHelper.Log(_log, ModRoosterControlCategory.General, $"WorldReady={Context.IsWorldReady}", LogLevel.Info);
        LogScopedHelper.Log(_log, ModRoosterControlCategory.General, $"MainPlayer={Context.IsMainPlayer}", LogLevel.Info);
        LogScopedHelper.Log(_log, ModRoosterControlCategory.General, $"LetMeHatchApiReady={HatchApi is not null}", LogLevel.Info);
        LogScopedHelper.Log(_log, ModRoosterControlCategory.General,
            $"Catalogs: roosters={_catalogs.Roosters.Roosters.Count}, produce={_catalogs.Produce.Roosters.Count}, hatching={_catalogs.Hatching.Roosters.Count}",
            LogLevel.Info);
        LogScopedHelper.Log(_log, ModRoosterControlCategory.General, $"PendingInvalidations={_pendingInvalidations.Count}", LogLevel.Info);
        LogScopedHelper.Log(_log, ModRoosterControlCategory.General, $"NeedsRoosterSetup={_needsRoosterSetup}", LogLevel.Info);
    }

    private void FlushInvalidations()
    {
        foreach (var name in _pendingInvalidations)
            _ = Helper.GameContent.InvalidateCache(name);

        _pendingInvalidations.Clear();
    }

    private void QueueNextTickAction(Action action)
    {
        _nextTickActions.Enqueue(action);
        if (_nextTickHooked)
            return;

        _nextTickHooked = true;
        Helper.Events.GameLoop.UpdateTicked += OnNextTick;
    }

    private void OnNextTick(object? sender, UpdateTickedEventArgs e)
    {
        while (_nextTickActions.Count > 0)
        {
            var action = _nextTickActions.Dequeue();
            try
            {
                action();
            }
            catch (Exception ex)
            {
                LogScopedHelper.Log(_log, ModRoosterControlCategory.General, $"Deferred action failed: {ex}", LogLevel.Error);
            }
        }

        Helper.Events.GameLoop.UpdateTicked -= OnNextTick;
        _nextTickHooked = false;
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
