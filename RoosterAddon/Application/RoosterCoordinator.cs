using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RoosterAddon.Application.Hatching;
using RoosterAddon.Application.Catalogs;
using RoosterAddon.Constants;
using RoosterAddon.Enums;
using RoosterAddon.Framework;
using RoosterAddon.Framework.Config;
using RoosterAddon.Helpers;
using RoosterAddon.Infrastructure.SaveData;
using RoosterAddon.Infrastructure.StateDumps;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.GameData.FarmAnimals;

using ILetMeHatchApi = Mikew.BirdsOfAFeather.Framework.ILetMeHatchApi;
using LuckyTypes = Mikew.BirdsOfAFeather.EnumsAndStrings.Enums.LuckyTypes;
using Mikew.BirdsOfAFeather.Helpers;
using Mikew.BirdsOfAFeather.Models;
using RoosterAddon.Infrastructure.Assets;
using RoosterAddon.Application.FarmAnimals;

namespace RoosterAddon.Application;

internal sealed partial class RoosterCoordinator : IRoosterCoordinator
{
    private const string FarmAnimalsAssetName = "Data/FarmAnimals";
    private const string ObjectsAssetName = "Data/Objects";

    private readonly ModEntry _host;
    private readonly ISaveDataStore _saveData;

    

    private readonly CatalogManager _catalogManager = new();
    private CatalogBundle _catalogs = new();

    private IModHelper Helper => _host.Helper;
    private IMonitor Monitor => _host.Monitor;
    private IManifest ModManifest => _host.ModManifest;

    private string SaveStateKey => $"{ModManifest.UniqueID}/save-state";

    private RoosterAddonConfig? _config;

    internal RoosterAddonConfig Config
    {
        get => _config ?? throw new InvalidOperationException("Config not initialized yet.");
        set
        {
            _config = value ?? throw new ArgumentNullException(nameof(value));
            RoosterHelper?.UpdateConfig(_config);
        }
    }
    internal RoosterControl RoosterHelper = null!;
internal ILetMeHatchApi? HatchApi;

    internal Dictionary<Textures, Dictionary<TextureDetails, string>> ModTextures = new();

    internal ModAssetHandler AssetHandler = null!;

    private State.RoosterAddonSaveState _saveState = new();

    private AnimalExclusion _animalExclusions = new();

    

    
    private readonly FarmAnimalExclusionsService _farmAnimalExclusionsService = new();
private readonly FarmAnimalHatchingService _farmAnimalHatching = new();
private readonly HashSet<string> _pendingInvalidations = new(StringComparer.OrdinalIgnoreCase);
    private readonly Queue<Action> _nextTickActions = new();
    private bool _nextTickHooked;
    private bool _gmcmRegistered;
    private bool _femaleAssignmentsDirty;
    private bool _needsRoosterSetup = true;
    private int _lastRoosterSetupDay = -1;
    private int _lastDebugDumpDay = -1;

    private bool HasDayStarted;
    private LuckyTypes DailyLuck = LuckyTypes.Neutral;

    // "Stuck inside" affects harvest type; maintained via CP conditions for parity.
    private Mikew.BirdsOfAFeather.Framework.ManagedConditions? conditionStuckInsideSeason;
    private Mikew.BirdsOfAFeather.Framework.ManagedConditions? conditionStuckInsideWeather;
    private bool isStuckInside;

    internal RoosterCoordinator(ModEntry host, ISaveDataStore saveData)
    {
        _host = host;
        _saveData = saveData;
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
            "roosteraddon_dump_state",
            "Write a JSON snapshot of RoosterAddon state to data/state-dumps. Add 'farmanimals' to include farm animal keys.",
            DumpStateCommand
        );

                Helper.ConsoleCommands.Add(
    "ra_plan",
    "Daily hatching plan commands. Use: ra_plan help",
    DailyPlanCommand
);

AssetHandler = new ModAssetHandler(ModManifest.UniqueID, Monitor);
        AssetHandler.AddAssets(
            new List<AssetData>
            {
                new(
                    enumAssetType: typeof(Textures),
                    enumAssetFolder: RoosterAddonAssetFolder.Textures,
                    fileExt: ModAssetHandler.FileExtension.Png,
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
                { TextureDetails.GamePath, AssetHandler.GetAssetPath(item, ModAssetHandler.AssetPathType.GamePath) },
                { TextureDetails.MoldFolderPath, AssetHandler.GetAssetPath(item, ModAssetHandler.AssetPathType.ModFolderPath) }
            };
        }

        RoosterHelper = new RoosterControl(Config, Helper, Monitor, _catalogs.Roosters);
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
        if (Config.General.UpdateRate == UpdateRateMode.OnLocationChange)
            _ = Helper.GameContent.InvalidateCache(FarmAnimalsAssetName);
    }

    public void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        _saveState = _saveData.Read<State.RoosterAddonSaveState>(SaveStateKey) ?? new State.RoosterAddonSaveState();
        _needsRoosterSetup = true;
        _lastRoosterSetupDay = -1;
        _lastDebugDumpDay = -1;
        InvalidateDailyPlanCache("save loaded");

        RunStartupValidationV2();
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
        _gmcmRegistered = false;
        InvalidateDailyPlanCache("returned to title");
        _pendingInvalidations.Clear();
        _nextTickActions.Clear();

        if (_nextTickHooked)
        {
            Helper.Events.GameLoop.UpdateTicked -= OnNextTick;
            _nextTickHooked = false;
        }
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
                Monitor.Log($"Deferred action failed: {ex}", LogLevel.Error);
            }
        }

        Helper.Events.GameLoop.UpdateTicked -= OnNextTick;
        _nextTickHooked = false;
    }



    private void LoadExclusionsData()
    {
        _animalExclusions = _farmAnimalExclusionsService.LoadOrCreate(Helper);
    }

    private void DumpStateCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            Monitor.Log("Save not loaded; nothing to dump yet.", LogLevel.Info);
            return;
        }

        var includeFarmAnimals = args.Any(a => a.Equals("farmanimals", StringComparison.OrdinalIgnoreCase));
        var payload = BuildStateDump(includeFarmAnimals);

        var now = DateTimeOffset.Now;
        var fileName = StateDumpJson.BuildFileName("roosteraddon_state", now);
        WriteStateDump(fileName, payload);

        Monitor.Log($"Wrote state dump: {fileName}", LogLevel.Info);
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
