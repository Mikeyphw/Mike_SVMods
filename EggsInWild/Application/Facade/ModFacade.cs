using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Mikew.NestCore.Framework.Random;
using StardewValley;
using EggsInWild.Application.AnimalNotes;
using EggsInWild.Application.Progression;
using EggsInWild.Application.Spawning;
using EggsInWild.Application.Spawning.Diagnostics;
using EggsInWild.Core.Enums;
using EggsInWild.Infrastructure.Config;
using EggsInWild.Infrastructure.Diagnostics;
using EggsInWild.Infrastructure.Game;
using Mikew.NestCore.Helpers.Logging;
using EggsInWild.Infrastructure.ModData;
using Mikew.NestCore.Helpers.SaveData;
using Mikew.NestCore.Helpers.Config;
using LogScopedHelper = Mikew.NestCore.Helpers.Logging.LogScopedHelper;
using static Mikew.NestCore.Helpers.Logging.ScopedLogCatalog;

namespace EggsInWild.Application.Facade;

public sealed class ModFacade : IModFacade
{
    private readonly ISaveDataStore _store;
    private readonly SmapiConfigProvider<ModConfig> _config;
    private readonly ILog _log;

    private readonly Func<bool> _isWorldReady;
    private readonly Func<bool> _isMainPlayer;

    private readonly SmapiReloadAll _reloadAll;

    private readonly WeggVerifier _verifier;

    private readonly IModDataLoader _dataLoader;
    private readonly EggsInWild.Infrastructure.Mods.IModAvailability _mods;

#if WEGG_DEV
    private readonly Func<FacadeResult>? _forceInstallDesertTombPatches;
#endif

    private readonly ProgressionService? _progression;
    private readonly WildEggSpawnOrchestrator? _spawner;
    private readonly AnimalNotesOrchestrator? _animalNotes;
    private readonly AnimalNotesStartupValidator? _animalNotesValidator;

    private readonly IWorldLocations _world;
    private readonly IItemRegistry _items;
    private readonly IGameStateReader _game;
    private readonly ITileFinder _tileFinder;
    private readonly IGameObjectPlacer _placer;
    private readonly ITileSearchHistogram _histogram;

    public ModFacade(
        ISaveDataStore store,
        SmapiConfigProvider<ModConfig> config,
        ILog log,
        Func<bool> isWorldReady,
        Func<bool> isMainPlayer,
        SmapiReloadAll reloadAll,
        ProgressionService? progression,
        WildEggSpawnOrchestrator? spawner,
        AnimalNotesOrchestrator? animalNotes,
        AnimalNotesStartupValidator? animalNotesValidator,
        IWorldLocations world,
        IItemRegistry items,
        IGameStateReader game,
        ITileFinder tileFinder,
        IGameObjectPlacer placer,
        EggsInWild.Infrastructure.ModData.IModDataLoader dataLoader,
        EggsInWild.Infrastructure.Mods.IModAvailability mods,
        WeggVerifier verifier,
        ITileSearchHistogram? histogram = null
#if WEGG_DEV
        , Func<FacadeResult>? forceInstallDesertTombPatches = null
#endif
    )
    {
        _store = store;
        _config = config;
        _log = log;
        _isWorldReady = isWorldReady;
        _isMainPlayer = isMainPlayer;
        _reloadAll = reloadAll;

        _dataLoader = dataLoader ?? throw new ArgumentNullException(nameof(dataLoader));
        _mods = mods ?? throw new ArgumentNullException(nameof(mods));
        _verifier = verifier;
#if WEGG_DEV
        _forceInstallDesertTombPatches = forceInstallDesertTombPatches;
#endif

        _progression = progression;
        _spawner = spawner;
        _animalNotes = animalNotes;
        _animalNotesValidator = animalNotesValidator;

        _world = world;
        _items = items;
        _game = game;
        _tileFinder = tileFinder;
        _placer = placer;
        _histogram = histogram ?? new TileSearchHistogram();
    }

    public ProgressionStep GetProgressionStep()
        => _progression?.GetCurrentStep() ?? ProgressionStep.PartI;

    public ModConfig GetConfigSnapshot() => Clone(_config.Get());

    public IReadOnlyDictionary<string, int> GetQuestCompletionDays()
        => _progression?.GetQuestCompletionDays() ?? new Dictionary<string, int>();

    public FacadeResult ReloadAll()
    {
        try
        {
            _reloadAll.Invoke();
            LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.General, "ReloadAll: config + ModData reloaded; assets invalidated.", StardewModdingAPI.LogLevel.Info);
            return new FacadeResult(true, "Reloaded config + ModData and invalidated assets.");
        }
        catch (Exception ex)
        {
            LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.General, $"Reload failed: {ex}", StardewModdingAPI.LogLevel.Warn);
            return new FacadeResult(false, "Reload failed; see log.");
        }
    }

    public FacadeResult TrySetProgressionStep(ProgressionStep step, bool applySideEffects, string reason)
    {
        if (_progression is null)
            return new FacadeResult(false, "Progression service unavailable.");

        if (!_isWorldReady())
            return new FacadeResult(false, "World not ready.");

        if (!_isMainPlayer())
            return new FacadeResult(false, "Host-only.");

        if (!_progression.TryForceSetStep(step, applySideEffects, reason, out var msg))
            return new FacadeResult(false, msg);

        _progression.SaveNow();
        return new FacadeResult(true, msg);
    }


    public FacadeResult PreviewStepEffects(ProgressionStep step)
    {
        if (_progression is null)
            return new FacadeResult(false, "Progression service unavailable.");

        if (!_isWorldReady())
            return new FacadeResult(false, "World not ready.");

        if (!_isMainPlayer())
            return new FacadeResult(false, "Host-only.");

        var text = _progression.PreviewStepEffects(step);
        return new FacadeResult(true, text);
    }


    public FacadeResult TryRunSpawnPlanNow()
    {
        if (_spawner is null)
            return new FacadeResult(false, "Spawner unavailable.");

        if (!_isWorldReady())
            return new FacadeResult(false, "World not ready.");

        if (!_isMainPlayer())
            return new FacadeResult(false, "Host-only.");

        _spawner.OnDayStarted();
        LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.General, "SpawnNow: executed spawn plan via console.", StardewModdingAPI.LogLevel.Info);
        return new FacadeResult(true, "Executed spawn plan for today.");
    }

    public FacadeResult TryPlaceItem(string locationName, string qualifiedItemId, int count, int maxAttempts)
    {
        if (!_isWorldReady())
            return new FacadeResult(false, "World not ready.");

        if (!_isMainPlayer())
            return new FacadeResult(false, "Host-only.");

        if (count <= 0)
            return new FacadeResult(false, "Count must be > 0.");

        if (maxAttempts <= 0)
            return new FacadeResult(false, "Max attempts must be > 0.");

        if (!_world.Exists(locationName))
            return new FacadeResult(false, $"Unknown location '{locationName}'.");

        if (!_items.Exists(qualifiedItemId))
            return new FacadeResult(false, $"Unknown itemId '{qualifiedItemId}' (must be qualified, e.g. '(O)390').");

        var location = _world.TryGetByName(locationName);
        if (location is null)
            return new FacadeResult(false, $"Location '{locationName}' not loaded.");

        var saveId = _game.GetSaveUniqueId();
        var dayNumber = _game.GetDayNumber();

        var placed = 0;

        for (var i = 0; i < count; i++)
        {
            var rng = new SeededRandomProvider(DeterministicSeed.For(saveId, dayNumber, "console_place", qualifiedItemId, locationName, i, placed));

            var job = new SpawnJob(
                EggItemId: qualifiedItemId,
                EggKind: EggKind.Wild,
                PackId: "console",
                LocationType: "console",
                MapName: locationName,
                SpawnChance: 1,
                BypassRandomChance: true,
                EggsToSpawn: 1,
                IncludeTerrainTypes: Array.Empty<string>(),
                ExcludeTerrainTypes: Array.Empty<string>(),
                IncludeRects: Array.Empty<Rectangle>(),
                ExcludeRects: Array.Empty<Rectangle>());

            var tile = _tileFinder.FindTile(location, job, rng, maxAttempts, out var stats);
            _histogram.Record(locationName, qualifiedItemId, stats, foundTile: tile is not null);

            if (tile is null)
            {
                var top = _histogram.FormatTopRejections(locationName, qualifiedItemId, topN: 3);
                LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.General, $"Console place failed: no valid tile (map={locationName} item={qualifiedItemId}) attempts={stats.Attempts} top=[{top}]", StardewModdingAPI.LogLevel.Warn);
                return new FacadeResult(false, $"No valid tile found (attempts={stats.Attempts}). Top=[{top}]");
            }

            if (!_placer.TryPlaceObject(location, tile.Value, qualifiedItemId, 1))
            {
                LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.General, $"Console place failed: unable to place object (map={locationName} item={qualifiedItemId} tile={tile})", StardewModdingAPI.LogLevel.Warn);
                return new FacadeResult(false, "Found a tile but couldn't place the object.");
            }

            placed++;
        }

        LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.General, $"Console place: placed={placed} map={locationName} item={qualifiedItemId}", StardewModdingAPI.LogLevel.Info);
        return new FacadeResult(true, $"Placed {placed} object(s) ({qualifiedItemId}) in {locationName}.");
    }

    private static ModConfig Clone(ModConfig c)
        => new()
        {
            ProgressionMode = c.ProgressionMode,
            DesertTombModuleEnabled = c.DesertTombModuleEnabled,
            DesertTombPharaohTrackerEnabled = c.DesertTombPharaohTrackerEnabled,
            DesertTombDebugLogging = c.DesertTombDebugLogging,
            TombChance = c.TombChance,
            AlternatePotionSprite = c.AlternatePotionSprite,
            NeedsFern = c.NeedsFern,
            HatchDinoEggs = c.HatchDinoEggs,
            DinoEggsExclusiveHatch = c.DinoEggsExclusiveHatch,
            BirthText = c.BirthText,
            DinoPrice = c.DinoPrice,
            VoidChickenPrice = c.VoidChickenPrice,
            OstrichPrice = c.OstrichPrice,
            AnimalNotesDebugMode = c.AnimalNotesDebugMode,
            AnimalNotesWarnAfterDays = c.AnimalNotesWarnAfterDays,
            AnimalNotesPlacementRadius = c.AnimalNotesPlacementRadius,
            AnimalNotesQuestMailHostOnly = c.AnimalNotesQuestMailHostOnly,
            AnimalNotesAllowClientDiscoveries = c.AnimalNotesAllowClientDiscoveries,
        };

    public FacadeResult DumpAnimalNotes()
    {
        if (_animalNotes is null)
            return new FacadeResult(false, "AnimalNotes service unavailable.");

        if (!_isWorldReady())
            return new FacadeResult(false, "World not ready.");

        if (!_isMainPlayer())
            return new FacadeResult(false, "Host-only.");

        return new FacadeResult(true, _animalNotes.DumpState());
    }

        public FacadeResult DumpAnimalNotes(AnimalNotesDumpOptions options)
    {
        if (_animalNotes is null)
            return new FacadeResult(false, "AnimalNotes service unavailable.");

        if (!_isWorldReady())
            return new FacadeResult(false, "World not ready.");

        if (!_isMainPlayer())
            return new FacadeResult(false, "Host-only.");

        return new FacadeResult(true, _animalNotes.DumpState(options));
    }

    public FacadeResult RepairAnimalNotes(bool requeueMail = true, bool reconcileNotes = true, bool force = false)
    {
        if (_animalNotes is null)
            return new FacadeResult(false, "AnimalNotes service unavailable.");

        if (!_isWorldReady())
            return new FacadeResult(false, "World not ready.");

        if (!_isMainPlayer())
            return new FacadeResult(false, "Host-only.");

        return new FacadeResult(true, _animalNotes.Repair(requeueMail, reconcileNotes, force));
    }

public FacadeResult ValidateAnimalNotes()
    {
        if (_animalNotesValidator is null)
            return new FacadeResult(false, "AnimalNotes validator unavailable.");

        if (!_isWorldReady())
            return new FacadeResult(false, "World not ready.");

        if (!_isMainPlayer())
            return new FacadeResult(false, "Host-only.");

        _animalNotesValidator.ValidateAndLog();
        return new FacadeResult(true, "AnimalNotes validation complete (see log).");
    }




public FacadeResult Verify(string[] args)
{
    var asJson = Array.Exists(args, x => string.Equals(x, "--json", StringComparison.OrdinalIgnoreCase));
    var full = Array.Exists(args, x => string.Equals(x, "--full", StringComparison.OrdinalIgnoreCase));

    if (full && !_isMainPlayer())
        return new FacadeResult(false, "Verify --full is host-only.");

    var report = _verifier.Run(new WeggVerifyOptions(
        AsJson: asJson,
        Full: full,
        IsWorldReady: _isWorldReady(),
        IsMainPlayer: _isMainPlayer(),
        ProgressionStep: _progression?.GetCurrentStep() ?? ProgressionStep.PartI
    ));

    return new FacadeResult(report.Success, report.Rendered);
}

    public FacadeResult WipeSchemaData(bool confirmed)
    {
        if (!_isWorldReady())
            return new FacadeResult(false, "World not ready.");

        if (!_isMainPlayer())
            return new FacadeResult(false, "Host-only.");

        if (!confirmed)
            return new FacadeResult(false, "Refusing to wipe without confirmation. Usage: wegg schema wipe --yes");

        try
        {
            _store.Delete(EggsInWild.Application.Progression.SaveKeys.Progression);
            _store.Delete(EggsInWild.Application.AnimalNotes.SaveKeys.AnimalNotes);

            LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.General, "Schema wipe: deleted Progression + AnimalNotes save keys for this mod.", StardewModdingAPI.LogLevel.Warn);
            return new FacadeResult(true, "Deleted Progression + AnimalNotes save data for this mod. Reload the save to reinitialize.");
        }
        catch (Exception ex)
        {
            LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.General, $"Schema wipe failed: {ex}", StardewModdingAPI.LogLevel.Error);
            return new FacadeResult(false, "Schema wipe failed; see log.");
        }
    }

#if WEGG_DEV
    public FacadeResult ForceInstallDesertTombHarmonyPatches()
        => _forceInstallDesertTombPatches is null
            ? new FacadeResult(false, "DesertTomb patch installer is not available.")
            : _forceInstallDesertTombPatches();


public FacadeResult DumpEffectiveWildAnimals()
{
    try
    {
        var snapshot = _dataLoader.Load();
        var builder = new AnimalCatalogBuilder(_mods, _log);
        var catalog = builder.Build(snapshot.WildAnimals);

        var list = new List<WildAnimalEffectiveDumpEntry>();

        foreach (var (key, wa) in snapshot.WildAnimals.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase))
        {
            var ids = new List<string>();
            if (catalog.TryGet(key, out var def))
                ids = def.FarmAnimalIds.ToList();

            list.Add(new WildAnimalEffectiveDumpEntry(
                WildAnimalKey: key,
                Enabled: wa.Enabled,
                LargeEgg: wa.LargeEgg,
                IsMagic: wa.IsMagic,
                Price: wa.Price,
                FarmAnimalIds: ids
            ));
        }

        var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = false });
        return new FacadeResult(true, json);
    }
    catch (Exception ex)
    {
        LogScopedHelper.Log(
            _log,
            ScopedLogCatalog.ModWeCategory.SaveData,
            $"DumpEffectiveWildAnimals failed: {ex}",
            StardewModdingAPI.LogLevel.Error);
        return new FacadeResult(false, "Failed dumping effective wild animals (see log).");
    }
}


public FacadeResult WildAnimalStatus(string wildAnimalKey)
{
    try
    {
        if (string.IsNullOrWhiteSpace(wildAnimalKey))
            return new FacadeResult(false, "Missing wildAnimalKey.");

        var snapshot = _dataLoader.Load();
        var canonicalKey = ResolveWildAnimalKey(snapshot.WildAnimals.Keys, wildAnimalKey);

        if (canonicalKey is null)
            return new FacadeResult(false, $"Unknown wild animal '{wildAnimalKey}'. Use: wegg wildanimals dump_effective");

        var wa = snapshot.WildAnimals[canonicalKey];

        var builder = new AnimalCatalogBuilder(_mods, _log);
        var catalog = builder.Build(snapshot.WildAnimals);

        var ids = new List<string>();
        if (catalog.TryGet(canonicalKey, out var def))
            ids = def.FarmAnimalIds.ToList();

        var cfg = _config.Get();
        var ov = TryGetWildAnimalOverride(cfg, canonicalKey);

        var entry = new WildAnimalStatusEntry(
            WildAnimalKey: canonicalKey,
            ConfigOverrideEnabled: ov?.Enabled,
            EffectiveEnabled: wa.Enabled,
            LargeEgg: wa.LargeEgg,
            IsMagic: wa.IsMagic,
            Price: wa.Price,
            FarmAnimalIds: ids
        );

        var json = JsonSerializer.Serialize(entry, new JsonSerializerOptions { WriteIndented = false });
        return new FacadeResult(true, json);
    }
    catch (Exception ex)
    {
        LogScopedHelper.Log(
            _log,
            ModWeCategory.SaveData,
            $"WildAnimalStatus failed: {ex}",
            StardewModdingAPI.LogLevel.Error);
        return new FacadeResult(false, "Failed getting wild animal status (see log).");
    }
}

public FacadeResult SetWildAnimalEnabled(string wildAnimalKey, bool enabled)
{
    try
    {
        if (string.IsNullOrWhiteSpace(wildAnimalKey))
            return new FacadeResult(false, "Missing wildAnimalKey.");

        var snapshot = _dataLoader.Load();
        var canonicalKey = ResolveWildAnimalKey(snapshot.WildAnimals.Keys, wildAnimalKey);

        if (canonicalKey is null)
            return new FacadeResult(false, $"Unknown wild animal '{wildAnimalKey}'. Use: wegg wildanimals dump_effective");

        _config.Update(cfg =>
        {
            cfg.WildAnimalOverrides ??= new Dictionary<string, WildAnimalOverrideConfig>(StringComparer.OrdinalIgnoreCase);

            if (!cfg.WildAnimalOverrides.TryGetValue(canonicalKey, out var ov) || ov is null)
            {
                ov = new WildAnimalOverrideConfig();
                cfg.WildAnimalOverrides[canonicalKey] = ov;
            }

            ov.Enabled = enabled;
        }, save: true);

        return WildAnimalStatus(canonicalKey);
    }
    catch (Exception ex)
    {
        LogScopedHelper.Log(
            _log,
            ModWeCategory.SaveData,
            $"SetWildAnimalEnabled failed: {ex}",
            StardewModdingAPI.LogLevel.Error);
        return new FacadeResult(false, "Failed setting wild animal override (see log).");
    }
}

private static string? ResolveWildAnimalKey(IEnumerable<string> keys, string raw)
{
    var needle = raw.Trim();
    foreach (var key in keys)
    {
        if (string.Equals(key, needle, StringComparison.OrdinalIgnoreCase))
            return key;
    }

    return null;
}

private static WildAnimalOverrideConfig? TryGetWildAnimalOverride(ModConfig cfg, string canonicalKey)
{
    if (cfg.WildAnimalOverrides is null || cfg.WildAnimalOverrides.Count == 0)
        return null;

    if (cfg.WildAnimalOverrides.TryGetValue(canonicalKey, out var direct))
        return direct;

    foreach (var (k, ov) in cfg.WildAnimalOverrides)
    {
        if (string.Equals(k, canonicalKey, StringComparison.OrdinalIgnoreCase))
            return ov;
    }

    return null;
}

private sealed record WildAnimalStatusEntry(
    string WildAnimalKey,
    bool? ConfigOverrideEnabled,
    bool EffectiveEnabled,
    bool LargeEgg,
    bool IsMagic,
    int Price,
    List<string> FarmAnimalIds
);

private sealed record WildAnimalEffectiveDumpEntry(
    string WildAnimalKey,
    bool Enabled,
    bool LargeEgg,
    bool IsMagic,
    int Price,
    List<string> FarmAnimalIds
);
#endif



}

/// <summary>Delegate used to keep reloading logic centralized in ModEntry.</summary>
public delegate void SmapiReloadAll();
