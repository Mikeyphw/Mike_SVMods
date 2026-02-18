using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using StardewModdingAPI;
using Microsoft.Xna.Framework;
using Mikew.BirdsOfAFeather.Framework.Random;
using StardewValley;
using WildEggsHatching.Application.AnimalNotes;
using WildEggsHatching.Application.Progression;
using WildEggsHatching.Application.Spawning;
using WildEggsHatching.Application.Spawning.Diagnostics;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Infrastructure.Config;
using WildEggsHatching.Infrastructure.Game;
using WildEggsHatching.Infrastructure.Logging;
using WildEggsHatching.Infrastructure.ModData;
using WildEggsHatching.Infrastructure.SaveData;

namespace WildEggsHatching.Application.Facade;

public sealed class ModFacade : IModFacade
{
    private readonly IModHelper _helper;
    private readonly ISaveDataStore _store;
    private readonly IConfigProvider _config;
    private readonly ILog _log;

    private readonly Func<bool> _isWorldReady;
    private readonly Func<bool> _isMainPlayer;

    private readonly SmapiReloadAll _reloadAll;

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
        IModHelper helper,
        ISaveDataStore store,
        IConfigProvider config,
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
        ITileSearchHistogram? histogram = null)
    {
        _helper = helper;
        _store = store;
        _config = config;
        _log = log;
        _isWorldReady = isWorldReady;
        _isMainPlayer = isMainPlayer;
        _reloadAll = reloadAll;

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

    public ModConfig GetConfigSnapshot() => Clone(_config.Config);

    public IReadOnlyDictionary<string, int> GetQuestCompletionDays()
        => _progression?.GetQuestCompletionDays() ?? new Dictionary<string, int>();

    public FacadeResult ReloadAll()
    {
        try
        {
            _reloadAll.Invoke();
            _log.Log("ReloadAll: config + ModData reloaded; assets invalidated.", StardewModdingAPI.LogLevel.Info);
            return new FacadeResult(true, "Reloaded config + ModData and invalidated assets.");
        }
        catch (Exception ex)
        {
            _log.Log($"Reload failed: {ex}", StardewModdingAPI.LogLevel.Warn);
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

    public FacadeResult TryRunSpawnPlanNow()
    {
        if (_spawner is null)
            return new FacadeResult(false, "Spawner unavailable.");

        if (!_isWorldReady())
            return new FacadeResult(false, "World not ready.");

        if (!_isMainPlayer())
            return new FacadeResult(false, "Host-only.");

        _spawner.OnDayStarted();
        _log.Log("SpawnNow: executed spawn plan via console.", StardewModdingAPI.LogLevel.Info);
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
                _log.Log($"Console place failed: no valid tile (map={locationName} item={qualifiedItemId}) attempts={stats.Attempts} top=[{top}]", StardewModdingAPI.LogLevel.Warn);
                return new FacadeResult(false, $"No valid tile found (attempts={stats.Attempts}). Top=[{top}]");
            }

            if (!_placer.TryPlaceObject(location, tile.Value, qualifiedItemId, 1))
            {
                _log.Log($"Console place failed: unable to place object (map={locationName} item={qualifiedItemId} tile={tile})", StardewModdingAPI.LogLevel.Warn);
                return new FacadeResult(false, "Found a tile but couldn't place the object.");
            }

            placed++;
        }

        _log.Log($"Console place: placed={placed} map={locationName} item={qualifiedItemId}", StardewModdingAPI.LogLevel.Info);
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
            _store.Delete(WildEggsHatching.Application.Progression.SaveKeys.Progression);
            _store.Delete(WildEggsHatching.Application.AnimalNotes.SaveKeys.AnimalNotes);

            _log.Log("Schema wipe: deleted Progression + AnimalNotes save keys for this mod.", StardewModdingAPI.LogLevel.Warn);
            return new FacadeResult(true, "Deleted Progression + AnimalNotes save data for this mod. Reload the save to reinitialize.");
        }
        catch (Exception ex)
        {
            _log.Log($"Schema wipe failed: {ex}", StardewModdingAPI.LogLevel.Error);
            return new FacadeResult(false, "Schema wipe failed; see log.");
        }
    }



    public FacadeResult Verify(bool full, bool asJson)
    {
        try
        {
            var report = WildEggsHatching.Infrastructure.Diagnostics.WeggVerifier.Run(
                helper: _helper,
                store: _store,
                config: _config,
                log: _log,
                full: full);

            if (asJson)
            {
                var json = System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                });
                return new FacadeResult(report.Success, json);
            }

            var msg = report.Summary + Environment.NewLine +
                      string.Join(Environment.NewLine, report.Checks.Select(c => $"{c.Status.ToUpperInvariant()}: {c.Id} - {c.Message}"));
            return new FacadeResult(report.Success, msg);
        }
        catch (Exception ex)
        {
            _log.Log($"verify failed: {ex}", StardewModdingAPI.LogLevel.Error);
            return new FacadeResult(false, $"verify failed: {ex.Message}");
        }
    }

}

/// <summary>Delegate used to keep reloading logic centralized in ModEntry.</summary>
public delegate void SmapiReloadAll();
