using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters;
using WildEggsHatching.Application.DesertTomb.Generation;
using WildEggsHatching.Application.DesertTomb.Spawning;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.DesertTomb.Core.Configuration;
using WildEggsHatching.DesertTomb.Core.Model;
using WildEggsHatching.DesertTomb.Core.Runtime;
using WildEggsHatching.DesertTomb.Core.Spawning;
using WildEggsHatching.Infrastructure.Config;
using WildEggsHatching.Infrastructure.Logging;
using WildEggsHatching.Infrastructure.DesertTomb.Diagnostics;
using static WildEggsHatching.DesertTomb.Core.Enums.DesertTombEnums;

namespace WildEggsHatching.Application.DesertTomb.Runtime;

/// <summary>
/// Runtime integration for DesertTomb:
/// - Detect tomb floors (via AdditionalMineMaps or direct tomb map paths),
/// - Apply level visuals/containers,
/// - Plan and spawn tomb monsters with paced waves (Phase 3).
/// </summary>
public sealed class DesertTombRuntime
{
    private const string ProcessedKey = Constants.ModId + "/deserttomb/processed";

    private readonly IConfigProvider _config;
    private readonly Func<ProgressionStep> _getStep;
    private readonly ILog _log;
    private readonly Func<bool> _isMainPlayer;
    private readonly DesertTombLevelGenerator _generator;
    private readonly DesertTombSpawnExecutor _spawnExecutor;

    private readonly DesertTombDiagnostics? _diagnostics;
    private ActiveTombFloorState? _activeFloor;

    private MineShaft? _pending;

    public DesertTombRuntime(
        IConfigProvider config,
        Func<ProgressionStep> getStep,
        ILog log,
        Func<bool> isMainPlayer,
        DesertTombLevelGenerator generator,
        DesertTombSpawnExecutor spawnExecutor,
        DesertTombDiagnostics? diagnostics = null
    )
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _getStep = getStep ?? throw new ArgumentNullException(nameof(getStep));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _isMainPlayer = isMainPlayer ?? throw new ArgumentNullException(nameof(isMainPlayer));
        _generator = generator ?? throw new ArgumentNullException(nameof(generator));
        _spawnExecutor = spawnExecutor ?? throw new ArgumentNullException(nameof(spawnExecutor));
        _diagnostics = diagnostics;
    }

    /// <summary>
    /// Schedule tomb activation for the next tick (avoids running during the warp transition).
    /// </summary>
    public void OnWarped(GameLocation newLocation)
    {
        _activeFloor = null;

        if (newLocation is MineShaft mineShaft)
            _pending = mineShaft;
        else
            _pending = null;
    }

    public void OnDayStarted()
    {
        _pending = null;
        _activeFloor = null;
    }

    public void OnUpdateTicked()
    {
        // Activation takes priority on the tick after a warp.
        if (_pending is not null)
        {
            var mineShaft = _pending;
            _pending = null;
            TryActivate(mineShaft);
        }

        // Runtime population pacing (Phase 3).
        if (_activeFloor is not null)
        {
            if (!ReferenceEquals(Game1.currentLocation, _activeFloor.MineShaft))
            {
                _activeFloor = null;
                return;
            }

            _activeFloor.Population.UpdateTicked();
        }
    }

    private void TryActivate(MineShaft mineShaft)
    {
        var cfg = _config.Config;

        if (!cfg.DesertTombModuleEnabled)
            return;

        if (!_isMainPlayer())
            return;

        var step = _getStep();

        if (!DesertTombRuntimePolicy.IsEligibleByProgression(cfg.ProgressionMode, step))
            return;

        var mapPath = mineShaft.mapPath.Value;
        if (!DesertTombRuntimePolicy.IsTombCandidate(mineShaft.mineLevel, mapPath))
            return;

        if (AlreadyProcessedToday(mineShaft))
            return;

        var layoutLevel = DesertTombRuntimePolicy.GuessMineLevelOrDefault(mapPath);

        if (!DefaultTombLayouts.ByLevel.TryGetValue(layoutLevel, out var layout))
        {
            _log.Warn($"[DesertTomb] No layout registered for {layoutLevel}; skipping.");
            return;
        }

        // Remove any vanilla mons.
        RemoveExistingMonsters(mineShaft);

        var config = SpawnConfiguration.Default;

        _generator.Generate(mineShaft);

        var planner = new TombSpawnPlanner(new SystemRandomProvider(mineShaft.mineRandom));
        var progressionFlags = new TombSpawnProgressionFlags(cfg.ProgressionMode, step);
        var plan = planner.BuildPlan(layout, config, config.Difficulty, progressionFlags, mapTags: null);

        var areaStates = TombAreaStateBuilder.Build(mineShaft, plan);

        var population = new TombPopulationManager(
            mineShaft: mineShaft,
            plan: plan,
            config: config,
            areas: areaStates,
            executor: _spawnExecutor,
            log: _log
        );

        var spawnedInitial = population.SpawnInitialWave();

        _activeFloor = new ActiveTombFloorState(
            mineShaft: mineShaft,
            layoutLevel: layoutLevel,
            mapPath: mapPath,
            areas: areaStates,
            plan: plan,
            config: config,
            population: population
        );

        MarkProcessedToday(mineShaft);

        _diagnostics?.OnActivated(layoutLevel, mapPath, mineShaft, plan, spawnedInitial);

        _log.Log(
            $"[DesertTomb] Activated on MineLevel={mineShaft.mineLevel} MapPath='{mapPath}'. " +
            $"Layout={layoutLevel} SpawnedInitial={spawnedInitial} (Planned={plan.TotalPlanned}).",
            LogLevel.Info
        );
    }

    private static void RemoveExistingMonsters(MineShaft mineShaft)
    {
        for (int i = mineShaft.characters.Count - 1; i >= 0; i--)
        {
            if (mineShaft.characters[i] is Monster)
                mineShaft.characters.Remove(mineShaft.characters[i]);
        }
    }

    private static bool AlreadyProcessedToday(MineShaft mineShaft)
    {
        if (!mineShaft.modData.TryGetValue(ProcessedKey, out var value))
            return false;

        return string.Equals(value, TodayMarker(mineShaft), StringComparison.Ordinal);
    }

    private static void MarkProcessedToday(MineShaft mineShaft)
        => mineShaft.modData[ProcessedKey] = TodayMarker(mineShaft);

    private static string TodayMarker(MineShaft mineShaft)
        => $"{Game1.stats.DaysPlayed}:{mineShaft.mineLevel}";

    private sealed class ActiveTombFloorState
    {
        public MineShaft MineShaft { get; }
        public int LayoutLevel { get; }
        public string MapPath { get; }
        public IReadOnlyList<TombAreaState> Areas { get; }
        public TombSpawnPlan Plan { get; }
        public SpawnConfiguration Config { get; }
        public TombPopulationManager Population { get; }

        public ActiveTombFloorState(
            MineShaft mineShaft,
            int layoutLevel,
            string mapPath,
            IReadOnlyList<TombAreaState> areas,
            TombSpawnPlan plan,
            SpawnConfiguration config,
            TombPopulationManager population
        )
        {
            MineShaft = mineShaft ?? throw new ArgumentNullException(nameof(mineShaft));
            LayoutLevel = layoutLevel;
            MapPath = mapPath ?? throw new ArgumentNullException(nameof(mapPath));
            Areas = areas ?? throw new ArgumentNullException(nameof(areas));
            Plan = plan ?? throw new ArgumentNullException(nameof(plan));
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Population = population ?? throw new ArgumentNullException(nameof(population));
        }
    }
}
