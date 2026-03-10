using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Mikew.NestCore.Framework.Random;
using StardewValley;
using StardewValley.GameData.FarmAnimals;
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
using EggsInWild.Infrastructure.ModData.Models;
using Mikew.NestCore.Helpers.SaveData;
using Mikew.NestCore.Helpers.Config;
using LogScopedHelper = Mikew.NestCore.Helpers.Logging.LogScopedHelper;
using static Mikew.NestCore.Helpers.Logging.ScopedLogCatalog;

namespace EggsInWild.Application.Facade;

public sealed class ModFacade : IModFacade
{
    private readonly string _modDirectoryPath;
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
        string modDirectoryPath,
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
        _modDirectoryPath = modDirectoryPath ?? throw new ArgumentNullException(nameof(modDirectoryPath));
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
            return new FacadeResult(false, "Refusing to wipe without confirmation. Usage: we save clear --yes");

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
    public FacadeResult RuntimeStatus()
    {
        try
        {
            var cfg = _config.Get();
            var snapshot = _dataLoader.Load();
            var enabledWildAnimals = snapshot.WildAnimals.Count(static kvp => kvp.Value.Enabled);

            var lines = new[]
            {
                $"WorldReady={_isWorldReady()}",
                $"MainPlayer={_isMainPlayer()}",
                $"ProgressionStep={GetProgressionStep()}",
                $"ProgressionMode={cfg.ProgressionMode}",
                $"DesertTombEnabled={cfg.DesertTombModuleEnabled}",
                $"AnimalNotesDebugMode={cfg.AnimalNotesDebugMode}",
                $"QuestCompletionTracked={GetQuestCompletionDays().Count}",
                $"WildAnimalsTotal={snapshot.WildAnimals.Count}",
                $"WildAnimalsEnabled={enabledWildAnimals}",
                $"SpawnAreas={snapshot.WildEggSpawnDefinitions.Count}",
                $"LocationPacks={snapshot.LocationPacks.Count}",
                $"AnimalNotesServiceReady={_animalNotes is not null}",
                $"AnimalNotesValidatorReady={_animalNotesValidator is not null}",
                $"SpawnerReady={_spawner is not null}",
                $"VerifierReady=True",
            };

            return new FacadeResult(true, string.Join(Environment.NewLine, lines));
        }
        catch (Exception ex)
        {
            LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.General, $"RuntimeStatus failed: {ex}", StardewModdingAPI.LogLevel.Error);
            return new FacadeResult(false, "Failed building runtime diagnostics (see log).");
        }
    }

    public FacadeResult PipelineStatus()
    {
        try
        {
            var editorAssets = NestCorePipelineSnapshot.EditorAssets;
            var modAssetPaths = NestCorePipelineSnapshot.ModAssetPaths;

            var lines = new List<string>
            {
                $"PipelineAttached={NestCorePipelineSnapshot.IsAttached}",
                $"EditorCount={editorAssets.Count}",
                $"ModAssetPathCount={modAssetPaths.Count}",
            };

            foreach (var asset in editorAssets)
                lines.Add($"EditorAsset={asset}");

            foreach (var path in modAssetPaths)
                lines.Add($"ModAssetPath={path}");

            return new FacadeResult(true, string.Join(Environment.NewLine, lines));
        }
        catch (Exception ex)
        {
            LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.General, $"PipelineStatus failed: {ex}", StardewModdingAPI.LogLevel.Error);
            return new FacadeResult(false, "Failed building pipeline diagnostics (see log).");
        }
    }

    public FacadeResult WriteStateDump(string? pathArg)
    {
        try
        {
            if (!_isWorldReady())
                return new FacadeResult(false, "World not ready.");

            if (!_isMainPlayer())
                return new FacadeResult(false, "Host-only.");

            var runtime = RuntimeStatus();
            if (!runtime.Success)
                return runtime;

            var pipeline = PipelineStatus();
            if (!pipeline.Success)
                return pipeline;

            var defaultName = $"we_state_{Game1.Date.Year}_{Game1.Date.Season}_{Game1.Date.DayOfMonth}.json";
            var path = ResolveOutputPath(pathArg, Path.Combine("state-dumps", defaultName));
            var payload = new
            {
                GeneratedAtUtc = DateTime.UtcNow,
                SaveId = Game1.uniqueIDForThisGame,
                Date = new
                {
                    Game1.Date.Year,
                    Season = Game1.Date.Season.ToString(),
                    Game1.Date.DayOfMonth
                },
                Runtime = runtime.Message.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                Pipeline = pipeline.Message.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
            };

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
            return new FacadeResult(true, $"Wrote state dump: {path}");
        }
        catch (Exception ex)
        {
            LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.General, $"WriteStateDump failed: {ex}", StardewModdingAPI.LogLevel.Error);
            return new FacadeResult(false, "Failed writing state dump (see log).");
        }
    }

    public FacadeResult ForceInstallDesertTombHarmonyPatches()
        => _forceInstallDesertTombPatches is null
            ? new FacadeResult(false, "DesertTomb patch installer is not available.")
            : _forceInstallDesertTombPatches();


public FacadeResult DumpEffectiveWildAnimals()
{
    try
    {
        var baseSnapshot = _dataLoader.LoadBase();
        var snapshot = _dataLoader.Load();
        var builder = new AnimalCatalogBuilder(_mods, _log);
        var baseCatalog = builder.Build(baseSnapshot.WildAnimals);
        var catalog = builder.Build(snapshot.WildAnimals);

        var list = new List<WildAnimalEffectiveDumpEntry>();

        var cfg = _config.Get();

        var knownFarmAnimalIds = TryLoadKnownFarmAnimalIdsOrNull();

        foreach (var (key, wa) in snapshot.WildAnimals.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase))
        {
            var baseWa = baseSnapshot.WildAnimals.TryGetValue(key, out var baseDefWa) ? baseDefWa : wa;

            var baseIds = new List<string>();
            if (baseCatalog.TryGet(key, out var baseDef))
                baseIds = baseDef.FarmAnimalIds.ToList();

            var ids = new List<string>();
            if (catalog.TryGet(key, out var def))
                ids = def.FarmAnimalIds.ToList();

            var ov = TryGetWildAnimalOverride(cfg, key);

            var locationResolved = BuildResolvedLocationTypes(wa.LocationTypes);
            var locationSources = BuildResolvedLocationTypeSources(ov?.LocationTypes);

            var extraEffective = BuildExtraModAnimalKeysEffective(wa.ExtraModAnimalKeys);
            var extraLoaded = BuildExtraModAnimalKeysLoaded(wa.ExtraModAnimalKeys);
            var resolvedByGroup = BuildResolvedFarmAnimalIdsByModGroup(wa.ExtraModAnimalKeys);
            var validationWarnings = BuildExtraModAnimalKeysValidationWarnings(wa.ExtraModAnimalKeys, knownFarmAnimalIds);
            var extraSources = BuildExtraModAnimalKeysSources(ov?.ExtraModAnimalKeys, extraEffective.Keys);

            var overrides = new WildAnimalOverridesDump(
                Enabled: ov?.Enabled,
                LargeEgg: ov?.LargeEgg,
                Price: ov?.Price,
                IsMagic: ov?.IsMagic,
                LocationTypes: ov?.LocationTypes,
                ExtraModAnimalKeys: BuildExtraModAnimalKeysOverrides(ov?.ExtraModAnimalKeys));

            var sources = new WildAnimalSourcesDump(
                Enabled: ov?.Enabled is null ? "default" : "override",
                LargeEgg: ov?.LargeEgg is null ? "default" : "override",
                Price: ov?.Price is null ? "default" : "override",
                IsMagic: ov?.IsMagic is null ? "default" : "override",
                LocationTypes: locationSources,
                ExtraModAnimalKeys: extraSources);

            var diff = BuildWildAnimalDiffOrNull(
                baseWa: baseWa,
                effectiveWa: wa,
                baseFarmAnimalIds: baseIds,
                effectiveFarmAnimalIds: ids);

            var diffText = diff is null ? null : BuildWildAnimalDiffText(key, diff);

            list.Add(new WildAnimalEffectiveDumpEntry(
                WildAnimalKey: key,
                Enabled: wa.Enabled,
                LargeEgg: wa.LargeEgg,
                IsMagic: wa.IsMagic,
                Price: wa.Price,
                FarmAnimalIds: ids,
                LocationTypes: locationResolved,
                ExtraModAnimalKeys: extraEffective,
                ExtraModAnimalKeysLoaded: extraLoaded,
                ResolvedFarmAnimalIdsByModGroup: resolvedByGroup,
                ValidationWarnings: validationWarnings,
                Diff: diff,
                DiffText: diffText,
                Overrides: overrides,
                Sources: sources
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

        var baseSnapshot = _dataLoader.LoadBase();
        var snapshot = _dataLoader.Load();
        var canonicalKey = ResolveWildAnimalKey(snapshot.WildAnimals.Keys, wildAnimalKey);

        if (canonicalKey is null)
            return new FacadeResult(false, BuildUnknownWildAnimalMessage(snapshot.WildAnimals.Keys, wildAnimalKey));

        var wa = snapshot.WildAnimals[canonicalKey];
        var baseWa = baseSnapshot.WildAnimals.TryGetValue(canonicalKey, out var baseDefWa) ? baseDefWa : wa;

        var builder = new AnimalCatalogBuilder(_mods, _log);
        var baseCatalog = builder.Build(baseSnapshot.WildAnimals);
        var catalog = builder.Build(snapshot.WildAnimals);

        var baseIds = new List<string>();
        if (baseCatalog.TryGet(canonicalKey, out var baseDef))
            baseIds = baseDef.FarmAnimalIds.ToList();

        var ids = new List<string>();
        if (catalog.TryGet(canonicalKey, out var def))
            ids = def.FarmAnimalIds.ToList();

        var cfg = _config.Get();
        var ov = TryGetWildAnimalOverride(cfg, canonicalKey);
        var knownFarmAnimalIds = TryLoadKnownFarmAnimalIdsOrNull();

        var locationResolved = BuildResolvedLocationTypes(wa.LocationTypes);
        var locationSources = BuildResolvedLocationTypeSources(ov?.LocationTypes);

        var extraEffective = BuildExtraModAnimalKeysEffective(wa.ExtraModAnimalKeys);
        var extraLoaded = BuildExtraModAnimalKeysLoaded(wa.ExtraModAnimalKeys);
        var resolvedByGroup = BuildResolvedFarmAnimalIdsByModGroup(wa.ExtraModAnimalKeys);
        var validationWarnings = BuildExtraModAnimalKeysValidationWarnings(wa.ExtraModAnimalKeys, knownFarmAnimalIds);
        var extraSources = BuildExtraModAnimalKeysSources(ov?.ExtraModAnimalKeys, extraEffective.Keys);

        var overrides = new WildAnimalOverridesDump(
            Enabled: ov?.Enabled,
            LargeEgg: ov?.LargeEgg,
            Price: ov?.Price,
            IsMagic: ov?.IsMagic,
            LocationTypes: ov?.LocationTypes,
            ExtraModAnimalKeys: BuildExtraModAnimalKeysOverrides(ov?.ExtraModAnimalKeys));

        var sources = new WildAnimalSourcesDump(
            Enabled: ov?.Enabled is null ? "default" : "override",
            LargeEgg: ov?.LargeEgg is null ? "default" : "override",
            Price: ov?.Price is null ? "default" : "override",
            IsMagic: ov?.IsMagic is null ? "default" : "override",
            LocationTypes: locationSources,
            ExtraModAnimalKeys: extraSources);

        var diff = BuildWildAnimalDiffOrNull(
            baseWa: baseWa,
            effectiveWa: wa,
            baseFarmAnimalIds: baseIds,
            effectiveFarmAnimalIds: ids);

        var diffText = diff is null ? null : BuildWildAnimalDiffText(canonicalKey, diff);

        var entry = new WildAnimalStatusEntry(
            WildAnimalKey: canonicalKey,
            EffectiveEnabled: wa.Enabled,
            LargeEgg: wa.LargeEgg,
            IsMagic: wa.IsMagic,
            Price: wa.Price,
            FarmAnimalIds: ids,
            LocationTypes: locationResolved,
            ExtraModAnimalKeys: extraEffective,
            ExtraModAnimalKeysLoaded: extraLoaded,
            ResolvedFarmAnimalIdsByModGroup: resolvedByGroup,
            ValidationWarnings: validationWarnings,
            Diff: diff,
            DiffText: diffText,
            Overrides: overrides,
            Sources: sources
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

        var baseSnapshot = _dataLoader.LoadBase();
        var snapshot = _dataLoader.Load();
        var canonicalKey = ResolveWildAnimalKey(snapshot.WildAnimals.Keys, wildAnimalKey);

        if (canonicalKey is null)
            return new FacadeResult(false, BuildUnknownWildAnimalMessage(snapshot.WildAnimals.Keys, wildAnimalKey));

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


public FacadeResult ExportWildAnimalEffectiveBulk(string wildAnimalKey)
{
    try
    {
        if (string.IsNullOrWhiteSpace(wildAnimalKey))
            return new FacadeResult(false, "Missing wildAnimalKey.");

        var baseSnapshot = _dataLoader.LoadBase();
        var snapshot = _dataLoader.Load();

        var canonicalKey = ResolveWildAnimalKey(snapshot.WildAnimals.Keys, wildAnimalKey);
        if (canonicalKey is null)
            return new FacadeResult(false, BuildUnknownWildAnimalMessage(snapshot.WildAnimals.Keys, wildAnimalKey));

        if (!baseSnapshot.WildAnimals.TryGetValue(canonicalKey, out var definition) || definition is null)
            return new FacadeResult(false, $"Wild animal '{canonicalKey}' not found in base WildAnimalsList.json.");

        var cfg = _config.Get();
        var ov = TryGetWildAnimalOverride(cfg, canonicalKey);

        var modIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Vanilla",
        };

        foreach (var mid in definition.ExtraModAnimalKeys.Keys)
            modIds.Add(mid);

        if (ov?.ExtraModAnimalKeys is not null)
        {
            foreach (var mid in ov.ExtraModAnimalKeys.Keys)
                modIds.Add(mid);
        }

        var ordered = modIds
            .OrderBy(mid => string.Equals(mid, "Vanilla", StringComparison.OrdinalIgnoreCase) ? "" : mid, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var body = new List<string>();
        foreach (var modId in ordered)
        {
            IReadOnlyList<string> effectiveKeys;

            if (ov?.ExtraModAnimalKeys is not null
                && ov.ExtraModAnimalKeys.TryGetValue(modId, out var blockOv)
                && blockOv?.FarmAnimalKeys is { Count: > 0 } ovKeys)
            {
                effectiveKeys = ovKeys;
            }
            else
            {
                effectiveKeys = definition.ExtraModAnimalKeys.TryGetValue(modId, out var baseBlock)
                    ? baseBlock.GetEffectiveFarmAnimalKeys()
                    : Array.Empty<string>();
            }

            if (effectiveKeys.Count == 0)
                continue;

            body.Add($"{modId} | {string.Join(", ", effectiveKeys)}");
        }

        var outLines = new List<string>
        {
            $"[wegg][ExtraModAnimalKeys] Export effective blocks for '{canonicalKey}':",
        };

        if (body.Count == 0)
        {
            outLines.Add("(none)");
        }
        else
        {
            outLines.AddRange(body);
        }

        return new FacadeResult(true, string.Join(Environment.NewLine, outLines));
    }
    catch (Exception ex)
    {
        LogScopedHelper.Log(
            _log,
            ModWeCategory.SaveData,
            $"ExportWildAnimalEffectiveBulk failed: {ex}",
            StardewModdingAPI.LogLevel.Error);

        return new FacadeResult(false, "Export failed (see log).");
    }
}

public FacadeResult ValidateWildAnimalBulk(string wildAnimalKey, string rawOrPath)
{
    try
    {
        if (string.IsNullOrWhiteSpace(wildAnimalKey))
            return new FacadeResult(false, "Missing wildAnimalKey.");

        var baseSnapshot = _dataLoader.LoadBase();
        var snapshot = _dataLoader.Load();

        var canonicalKey = ResolveWildAnimalKey(snapshot.WildAnimals.Keys, wildAnimalKey);
        if (canonicalKey is null)
            return new FacadeResult(false, BuildUnknownWildAnimalMessage(snapshot.WildAnimals.Keys, wildAnimalKey));

        if (!baseSnapshot.WildAnimals.TryGetValue(canonicalKey, out var definition) || definition is null)
            return new FacadeResult(false, $"Wild animal '{canonicalKey}' not found in base WildAnimalsList.json.");

        var cfg = _config.Get();
        var ov = TryGetWildAnimalOverride(cfg, canonicalKey);

        var knownModIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Vanilla",
        };

        foreach (var mid in definition.ExtraModAnimalKeys.Keys)
            knownModIds.Add(mid);

        if (ov?.ExtraModAnimalKeys is not null)
        {
            foreach (var mid in ov.ExtraModAnimalKeys.Keys)
                knownModIds.Add(mid);
        }

        var raw = ReadBulkPayload(rawOrPath);
        if (string.IsNullOrWhiteSpace(raw))
            return new FacadeResult(true, $"[wegg][ExtraModAnimalKeys] Bulk validation for '{canonicalKey}':\n(no content)");

        var knownFarmAnimalIds = TryLoadKnownFarmAnimalIdsOrNull();

        var parsed = ParseBulkPayload(raw, knownModIds);
        var upserts = parsed.Upserts;
        var resets = parsed.Resets;

        var modGroups = new Dictionary<string, ExtraModAnimalKeysJson>(StringComparer.OrdinalIgnoreCase);
        foreach (var (modId, rawKeys) in upserts)
            modGroups[modId] = new ExtraModAnimalKeysJson { FarmAnimalKeys = rawKeys };

        var warnings = new List<string>();
        warnings.AddRange(parsed.Warnings);

        if (modGroups.Count > 0)
            warnings.AddRange(ExtraModAnimalKeysWarnings.Build(modGroups, _mods, knownFarmAnimalIds));

        var outLines = new List<string>
        {
            $"[wegg][ExtraModAnimalKeys] Bulk validation for '{canonicalKey}':",
            $"Parsed: {upserts.Count} upsert(s), {resets.Count} reset(s).",
        };

        if (warnings.Count == 0)
        {
            outLines.Add("No warnings.");
        }
        else
        {
            outLines.Add("Warnings:");
            outLines.AddRange(warnings.Take(200).Select(x => $" - {x}"));
            if (warnings.Count > 200)
                outLines.Add($" - (+{warnings.Count - 200} more)");
        }

        return new FacadeResult(true, string.Join(Environment.NewLine, outLines));
    }
    catch (FileNotFoundException fnf)
    {
        return new FacadeResult(false, fnf.Message);
    }
    catch (Exception ex)
    {
        LogScopedHelper.Log(
            _log,
            ModWeCategory.SaveData,
            $"ValidateWildAnimalBulk failed: {ex}",
            StardewModdingAPI.LogLevel.Error);

        return new FacadeResult(false, "Validation failed (see log).");
    }
}

private static string ReadBulkPayload(string rawOrPath)
{
    if (string.IsNullOrWhiteSpace(rawOrPath))
        return string.Empty;

    var arg = rawOrPath.Trim();

    if (arg.StartsWith("@", StringComparison.Ordinal) && arg.Length > 1)
    {
        var path = arg[1..].Trim().Trim('"');

        if (!File.Exists(path))
            throw new FileNotFoundException($"Bulk file not found: {path}", path);

        return File.ReadAllText(path);
    }

    // Allow passing literal \n in a single argument.
    return arg
        .Replace("\\r", "\r", StringComparison.Ordinal)
        .Replace("\\n", "\n", StringComparison.Ordinal);
}


private sealed record BulkParseResult(Dictionary<string, List<string>> Upserts, HashSet<string> Resets, List<string> Warnings);

private static BulkParseResult ParseBulkPayload(string raw, ISet<string> knownModIds)
{
    var upserts = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
    var resets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var warnings = new List<string>();

    foreach (var rawLine in raw.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
    {
        var line = rawLine.Trim();
        if (line.Length == 0)
            continue;

        if (line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith("//", StringComparison.Ordinal))
            continue;

        var sepIndex = line.IndexOf('|');
        if (sepIndex < 0) sepIndex = line.IndexOf(':');
        if (sepIndex < 0) sepIndex = line.IndexOf('=');

        if (sepIndex <= 0)
        {
            warnings.Add($"Bulk line ignored (missing separator): '{line}'. Expected: modId | csvKeys");
            continue;
        }

        var modIdsRaw = line[..sepIndex].Trim();
        var keysRaw = sepIndex >= line.Length - 1 ? "" : line[(sepIndex + 1)..];

        if (modIdsRaw.Length == 0)
        {
            warnings.Add($"Bulk line ignored (blank modId): '{line}'.");
            continue;
        }

        var modIds = modIdsRaw
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (modIds.Count == 0)
        {
            warnings.Add($"Bulk line ignored (no valid modIds): '{line}'.");
            continue;
        }

        foreach (var modId in modIds)
        {
            if (!knownModIds.Contains(modId))
                warnings.Add($"Bulk line has unknown modId (not on this animal): '{modId}'. Line: '{line}'.");
        }

        var keysTrim = keysRaw.Trim();
        var isReset = keysTrim.Length == 0
            || keysTrim == "*"
            || string.Equals(keysTrim, "reset", StringComparison.OrdinalIgnoreCase)
            || string.Equals(keysTrim, "remove", StringComparison.OrdinalIgnoreCase);

        if (isReset)
        {
            foreach (var modId in modIds)
            {
                upserts.Remove(modId);
                resets.Add(modId);
            }

            continue;
        }

        var parts = keysRaw.Split(new[] { ',', ';' }, StringSplitOptions.None).ToList();

        foreach (var modId in modIds)
        {
            resets.Remove(modId);
            upserts[modId] = parts;
        }
    }

    return new BulkParseResult(upserts, resets, warnings);
}

private static string BuildUnknownWildAnimalMessage(IEnumerable<string> keys, string raw)
{
    var known = keys
        .Where(k => !string.IsNullOrWhiteSpace(k))
        .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
        .Take(10)
        .ToList();

    var preview = known.Count == 0 ? "(none)" : string.Join(", ", known);
    return $"Unknown wild animal '{raw}'. Known keys (first {known.Count}): {preview}. Use: wegg wildanimals dump_effective";
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

private static Dictionary<string, bool> BuildResolvedLocationTypes(Dictionary<string, bool> raw)
{
    var map = new Dictionary<string, bool>(StringComparer.Ordinal);
    foreach (LocationType lt in Enum.GetValues(typeof(LocationType)))
    {
        var key = lt.ToString();
        map[key] = raw.TryGetValue(key, out var allowed) && allowed;
    }

    return map;
}

private static Dictionary<string, string> BuildResolvedLocationTypeSources(Dictionary<string, bool>? locationTypeOverrides)
{
    var sources = new Dictionary<string, string>(StringComparer.Ordinal);
    foreach (LocationType lt in Enum.GetValues(typeof(LocationType)))
    {
        var key = lt.ToString();
        sources[key] = locationTypeOverrides is not null && ContainsKeyIgnoreCase(locationTypeOverrides, key) ? "override" : "default";
    }

    return sources;
}

private static Dictionary<string, List<string>> BuildExtraModAnimalKeysEffective(Dictionary<string, ExtraModAnimalKeysJson> modGroups)
{
    var dict = new Dictionary<string, List<string>>(StringComparer.Ordinal);

    foreach (var (modId, block) in modGroups.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase))
        dict[modId] = block.GetEffectiveFarmAnimalKeys().ToList();

    return dict;
}

private Dictionary<string, bool> BuildExtraModAnimalKeysLoaded(Dictionary<string, ExtraModAnimalKeysJson> modGroups)
{
    var dict = new Dictionary<string, bool>(StringComparer.Ordinal);

    foreach (var modId in modGroups.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        dict[modId] = _mods.IsLoaded(modId);

    return dict;
}

private Dictionary<string, List<string>> BuildResolvedFarmAnimalIdsByModGroup(Dictionary<string, ExtraModAnimalKeysJson> modGroups)
{
    var dict = new Dictionary<string, List<string>>(StringComparer.Ordinal);

    foreach (var (modId, block) in modGroups.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase))
    {
        if (!_mods.IsLoaded(modId))
            continue;

        dict[modId] = block.GetEffectiveFarmAnimalKeys().ToList();
    }

    return dict;
}



private HashSet<string>? TryLoadKnownFarmAnimalIdsOrNull()
{
    try
    {
        if (!_isWorldReady())
            return null;

        var data = Game1.content.Load<Dictionary<string, FarmAnimalData>>("Data/FarmAnimals");
        return new HashSet<string>(data.Keys, StringComparer.OrdinalIgnoreCase);
    }
    catch (Exception ex)
    {
        LogScopedHelper.Log(
            _log,
            ScopedLogCatalog.ModWeCategory.SaveData,
            $"Failed loading Data/FarmAnimals for validation: {ex.Message}",
            StardewModdingAPI.LogLevel.Trace);

        return null;
    }
}

private List<string> BuildExtraModAnimalKeysValidationWarnings(Dictionary<string, ExtraModAnimalKeysJson> modGroups, ISet<string>? knownFarmAnimalIds)
{
    return ExtraModAnimalKeysWarnings.Build(modGroups, _mods, knownFarmAnimalIds);
}

private static Dictionary<string, string> BuildExtraModAnimalKeysSources(
    Dictionary<string, ExtraModAnimalKeysOverrideConfig>? extraModOverrides,
    IEnumerable<string> effectiveModIds)
{
    var sources = new Dictionary<string, string>(StringComparer.Ordinal);

    foreach (var modId in effectiveModIds.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        sources[modId] = extraModOverrides is not null && ContainsKeyIgnoreCase(extraModOverrides, modId) ? "override" : "default";

    return sources;
}

private static Dictionary<string, List<string>>? BuildExtraModAnimalKeysOverrides(
    Dictionary<string, ExtraModAnimalKeysOverrideConfig>? extraModOverrides)
{
    if (extraModOverrides is null || extraModOverrides.Count == 0)
        return null;

    var dict = new Dictionary<string, List<string>>(StringComparer.Ordinal);

    foreach (var (modId, block) in extraModOverrides.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase))
    {
        var list = block.FarmAnimalKeys ?? new List<string>();
        if (list.Count == 0)
            continue;

        dict[modId] = list;
    }

    return dict.Count == 0 ? null : dict;
}

private static bool ContainsKeyIgnoreCase<TValue>(Dictionary<string, TValue> dict, string key)
{
    foreach (var k in dict.Keys)
    {
        if (string.Equals(k, key, StringComparison.OrdinalIgnoreCase))
            return true;
    }

    return false;
}

private sealed record WildAnimalOverridesDump(
    bool? Enabled,
    bool? LargeEgg,
    int? Price,
    bool? IsMagic,
    Dictionary<string, bool>? LocationTypes,
    Dictionary<string, List<string>>? ExtraModAnimalKeys
);

private sealed record WildAnimalSourcesDump(
    string Enabled,
    string LargeEgg,
    string Price,
    string IsMagic,
    Dictionary<string, string> LocationTypes,
    Dictionary<string, string> ExtraModAnimalKeys
);


private sealed record ValueDiff<T>(T Base, T Effective);

private sealed record ListDiffDump(List<string> Added, List<string> Removed);

private sealed record WildAnimalDiffDump(
    ValueDiff<bool>? Enabled,
    ValueDiff<bool>? LargeEgg,
    ValueDiff<int>? Price,
    ValueDiff<bool>? IsMagic,
    Dictionary<string, ValueDiff<bool>> LocationTypes,
    Dictionary<string, ListDiffDump> ExtraModAnimalKeys,
    ListDiffDump FarmAnimalIds,
    Dictionary<string, ListDiffDump> ResolvedFarmAnimalIdsByModGroup
);

private WildAnimalDiffDump? BuildWildAnimalDiffOrNull(
    WildAnimalJson baseWa,
    WildAnimalJson effectiveWa,
    IReadOnlyList<string> baseFarmAnimalIds,
    IReadOnlyList<string> effectiveFarmAnimalIds)
{
    var enabled = baseWa.Enabled == effectiveWa.Enabled ? null : new ValueDiff<bool>(baseWa.Enabled, effectiveWa.Enabled);
    var largeEgg = baseWa.LargeEgg == effectiveWa.LargeEgg ? null : new ValueDiff<bool>(baseWa.LargeEgg, effectiveWa.LargeEgg);
    var price = baseWa.Price == effectiveWa.Price ? null : new ValueDiff<int>(baseWa.Price, effectiveWa.Price);
    var isMagic = baseWa.IsMagic == effectiveWa.IsMagic ? null : new ValueDiff<bool>(baseWa.IsMagic, effectiveWa.IsMagic);

    var locationDiff = BuildLocationTypesDiff(baseWa.LocationTypes, effectiveWa.LocationTypes);

    var baseExtra = BuildExtraModAnimalKeysEffective(baseWa.ExtraModAnimalKeys);
    var effectiveExtra = BuildExtraModAnimalKeysEffective(effectiveWa.ExtraModAnimalKeys);
    var extraDiff = BuildExtraModAnimalKeysDiff(baseExtra, effectiveExtra);

    var farmIdsDiff = DiffListIgnoreCase(baseFarmAnimalIds, effectiveFarmAnimalIds);

    var baseResolvedByGroup = BuildResolvedFarmAnimalIdsByModGroup(baseWa.ExtraModAnimalKeys);
    var effectiveResolvedByGroup = BuildResolvedFarmAnimalIdsByModGroup(effectiveWa.ExtraModAnimalKeys);
    var resolvedByGroupDiff = BuildResolvedByGroupDiff(baseResolvedByGroup, effectiveResolvedByGroup);

    var any =
        enabled is not null
        || largeEgg is not null
        || price is not null
        || isMagic is not null
        || locationDiff.Count > 0
        || extraDiff.Count > 0
        || farmIdsDiff.Added.Count > 0
        || farmIdsDiff.Removed.Count > 0
        || resolvedByGroupDiff.Count > 0;

    if (!any)
        return null;

    return new WildAnimalDiffDump(
        Enabled: enabled,
        LargeEgg: largeEgg,
        Price: price,
        IsMagic: isMagic,
        LocationTypes: locationDiff,
        ExtraModAnimalKeys: extraDiff,
        FarmAnimalIds: farmIdsDiff,
        ResolvedFarmAnimalIdsByModGroup: resolvedByGroupDiff);
}

private static string BuildWildAnimalDiffText(string wildAnimalKey, WildAnimalDiffDump diff)
{
    var sb = new StringBuilder();
    sb.AppendLine($"Diff for '{wildAnimalKey}' (base -> effective):");

    if (diff.Enabled is not null)
        sb.AppendLine($"- Enabled: {diff.Enabled.Base} -> {diff.Enabled.Effective}");
    if (diff.LargeEgg is not null)
        sb.AppendLine($"- LargeEgg: {diff.LargeEgg.Base} -> {diff.LargeEgg.Effective}");
    if (diff.Price is not null)
        sb.AppendLine($"- Price: {diff.Price.Base} -> {diff.Price.Effective}");
    if (diff.IsMagic is not null)
        sb.AppendLine($"- IsMagic: {diff.IsMagic.Base} -> {diff.IsMagic.Effective}");

    if (diff.LocationTypes.Count > 0)
    {
        sb.AppendLine("- LocationTypes:");
        foreach (var (k, v) in diff.LocationTypes.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase))
            sb.AppendLine($"  - {k}: {v.Base} -> {v.Effective}");
    }

    if (diff.ExtraModAnimalKeys.Count > 0)
    {
        sb.AppendLine("- ExtraModAnimalKeys:");
        foreach (var (modId, d) in diff.ExtraModAnimalKeys.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase))
            AppendListDiff(sb, $"  - {modId}", d);
    }

    if (diff.FarmAnimalIds.Added.Count > 0 || diff.FarmAnimalIds.Removed.Count > 0)
        AppendListDiff(sb, "- FarmAnimalIds", diff.FarmAnimalIds);

    if (diff.ResolvedFarmAnimalIdsByModGroup.Count > 0)
    {
        sb.AppendLine("- ResolvedFarmAnimalIdsByModGroup:");
        foreach (var (modId, d) in diff.ResolvedFarmAnimalIdsByModGroup.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase))
            AppendListDiff(sb, $"  - {modId}", d);
    }

    return sb.ToString().TrimEnd();
}

private static void AppendListDiff(StringBuilder sb, string label, ListDiffDump d)
{
    if (d.Added.Count == 0 && d.Removed.Count == 0)
        return;

    sb.AppendLine($"{label}:");
    if (d.Added.Count > 0)
        sb.AppendLine($"    + {string.Join(", ", d.Added)}");
    if (d.Removed.Count > 0)
        sb.AppendLine($"    - {string.Join(", ", d.Removed)}");
}

private static Dictionary<string, ValueDiff<bool>> BuildLocationTypesDiff(
    IDictionary<string, bool> baseTypes,
    IDictionary<string, bool> effectiveTypes)
{
    var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var k in baseTypes.Keys)
        keys.Add(k);
    foreach (var k in effectiveTypes.Keys)
        keys.Add(k);

    var diff = new Dictionary<string, ValueDiff<bool>>(StringComparer.OrdinalIgnoreCase);
    foreach (var key in keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
    {
        var b = baseTypes.TryGetValue(key, out var bv) && bv;
        var e = effectiveTypes.TryGetValue(key, out var ev) && ev;
        if (b != e)
            diff[key] = new ValueDiff<bool>(b, e);
    }

    return diff;
}

private static Dictionary<string, ListDiffDump> BuildExtraModAnimalKeysDiff(
    IReadOnlyDictionary<string, List<string>> baseKeys,
    IReadOnlyDictionary<string, List<string>> effectiveKeys)
{
    var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var k in baseKeys.Keys)
        keys.Add(k);
    foreach (var k in effectiveKeys.Keys)
        keys.Add(k);

    var diff = new Dictionary<string, ListDiffDump>(StringComparer.OrdinalIgnoreCase);
    foreach (var modId in keys.OrderBy(k => k == "Vanilla" ? "" : k, StringComparer.OrdinalIgnoreCase))
    {
        var b = baseKeys.TryGetValue(modId, out var bv) ? bv : new List<string>();
        var e = effectiveKeys.TryGetValue(modId, out var ev) ? ev : new List<string>();
        var d = DiffListIgnoreCase(b, e);
        if (d.Added.Count > 0 || d.Removed.Count > 0)
            diff[modId] = d;
    }

    return diff;
}

private static Dictionary<string, ListDiffDump> BuildResolvedByGroupDiff(
    IReadOnlyDictionary<string, List<string>> baseResolved,
    IReadOnlyDictionary<string, List<string>> effectiveResolved)
{
    var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var k in baseResolved.Keys)
        keys.Add(k);
    foreach (var k in effectiveResolved.Keys)
        keys.Add(k);

    var diff = new Dictionary<string, ListDiffDump>(StringComparer.OrdinalIgnoreCase);
    foreach (var modId in keys.OrderBy(k => k == "Vanilla" ? "" : k, StringComparer.OrdinalIgnoreCase))
    {
        var b = baseResolved.TryGetValue(modId, out var bv) ? bv : new List<string>();
        var e = effectiveResolved.TryGetValue(modId, out var ev) ? ev : new List<string>();
        var d = DiffListIgnoreCase(b, e);
        if (d.Added.Count > 0 || d.Removed.Count > 0)
            diff[modId] = d;
    }

    return diff;
}

private static ListDiffDump DiffListIgnoreCase(IEnumerable<string> baseList, IEnumerable<string> effectiveList)
{
    baseList ??= Array.Empty<string>();
    effectiveList ??= Array.Empty<string>();

    var baseSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var s in baseList)
    {
        var t = s?.Trim();
        if (!string.IsNullOrWhiteSpace(t))
            baseSet.Add(t);
    }

    var effSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var s in effectiveList)
    {
        var t = s?.Trim();
        if (!string.IsNullOrWhiteSpace(t))
            effSet.Add(t);
    }

    var added = new List<string>();
    foreach (var s in effectiveList)
    {
        var t = s?.Trim();
        if (string.IsNullOrWhiteSpace(t))
            continue;
        if (!baseSet.Contains(t) && !added.Contains(t, StringComparer.OrdinalIgnoreCase))
            added.Add(t);
    }

    var removed = new List<string>();
    foreach (var s in baseList)
    {
        var t = s?.Trim();
        if (string.IsNullOrWhiteSpace(t))
            continue;
        if (!effSet.Contains(t) && !removed.Contains(t, StringComparer.OrdinalIgnoreCase))
            removed.Add(t);
    }

    added.Sort(StringComparer.OrdinalIgnoreCase);
    removed.Sort(StringComparer.OrdinalIgnoreCase);
    return new ListDiffDump(Added: added, Removed: removed);
}

private sealed record WildAnimalStatusEntry(
    string WildAnimalKey,
    bool EffectiveEnabled,
    bool LargeEgg,
    bool IsMagic,
    int Price,
    List<string> FarmAnimalIds,
    Dictionary<string, bool> LocationTypes,
    Dictionary<string, List<string>> ExtraModAnimalKeys,
    Dictionary<string, bool> ExtraModAnimalKeysLoaded,
    Dictionary<string, List<string>> ResolvedFarmAnimalIdsByModGroup,
    List<string> ValidationWarnings,
    WildAnimalDiffDump? Diff,
    string? DiffText,
    WildAnimalOverridesDump Overrides,
    WildAnimalSourcesDump Sources
);

private sealed record WildAnimalEffectiveDumpEntry(
    string WildAnimalKey,
    bool Enabled,
    bool LargeEgg,
    bool IsMagic,
    int Price,
    List<string> FarmAnimalIds,
    Dictionary<string, bool> LocationTypes,
    Dictionary<string, List<string>> ExtraModAnimalKeys,
    Dictionary<string, bool> ExtraModAnimalKeysLoaded,
    Dictionary<string, List<string>> ResolvedFarmAnimalIdsByModGroup,
    List<string> ValidationWarnings,
    WildAnimalDiffDump? Diff,
    string? DiffText,
    WildAnimalOverridesDump Overrides,
    WildAnimalSourcesDump Sources
);

private string ResolveOutputPath(string? pathArg, string defaultFileName)
{
    var baseDir = Path.Combine(_modDirectoryPath, "data");

    if (string.IsNullOrWhiteSpace(pathArg))
        return Path.Combine(baseDir, defaultFileName);

    var candidate = pathArg.Trim();
    if (!Path.IsPathRooted(candidate))
        candidate = Path.Combine(baseDir, candidate);

    if (Directory.Exists(candidate))
        return Path.Combine(candidate, Path.GetFileName(defaultFileName));

    return candidate;
}
#endif



}

/// <summary>Delegate used to keep reloading logic centralized in ModEntry.</summary>
public delegate void SmapiReloadAll();
