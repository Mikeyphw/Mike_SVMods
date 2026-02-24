using System;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters;
using EggsInWild.Application.DesertTomb.Generation;
using EggsInWild.Application.DesertTomb.Spawning;
using EggsInWild.Core.Enums;
using EggsInWild.DesertTomb.Core.Configuration;
using EggsInWild.DesertTomb.Core.Model;
using EggsInWild.DesertTomb.Core.Runtime;
using EggsInWild.DesertTomb.Core.Spawning;
using EggsInWild.Infrastructure.Config;
using ConfigTombDifficulty = EggsInWild.Infrastructure.Config.TombDifficulty;
using RuntimeTombDifficulty = EggsInWild.DesertTomb.Core.Enums.DesertTombEnums.TombDifficulty;
using Mikew.NestCore.Helpers.Logging;
using EggsInWild.Infrastructure.DesertTomb.Diagnostics;
using static EggsInWild.DesertTomb.Core.Enums.DesertTombEnums;
using Mikew.NestCore.Helpers.Config;
using EggsInWild.Core.Assets;
using LogScopedHelper = Mikew.NestCore.Helpers.Logging.LogScopedHelper;
using static Mikew.NestCore.Helpers.Logging.ScopedLogCatalog;

namespace EggsInWild.Application.DesertTomb.Runtime;

/// <summary>
/// Runtime integration for DesertTomb:
/// - Detect tomb floors (via random chance on mine floors),
/// - Apply level visuals/containers,
/// - Plan and spawn tomb monsters with paced waves (Phase 3).
/// </summary>
public sealed class DesertTombRuntime
{
    private const string ProcessedKey = Constants.ModId + "/deserttomb/processed";

    public const string LadderSuppressedKey = Constants.ModId + "/deserttomb/ladderSuppressed";
    public const string BreakableDropRateMultiplierKey = Constants.ModId + "/deserttomb/breakableDropRateMult";

    private readonly SmapiConfigProvider<ModConfig> _config;
    private readonly Func<ProgressionStep> _getStep;
    private readonly ILog _log;
    private readonly Func<bool> _isMainPlayer;
    private readonly DesertTombLevelGenerator _generator;
    private readonly DesertTombSpawnExecutor _spawnExecutor;
    private readonly TombRoleEnforcer _roleEnforcer;

    private readonly DesertTombDiagnostics? _diagnostics;
    private ActiveTombFloorState? _activeFloor;

    private MineShaft? _pending;

    private bool _loggedTombLoadLevelVerifier;
    private bool _loggedTombPopulateVerifier;

    public DesertTombRuntime(
        SmapiConfigProvider<ModConfig> config,
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
        _roleEnforcer = new TombRoleEnforcer(_log);
        _diagnostics = diagnostics;
    }

    /// <summary>
    /// Schedule tomb activation for the next tick (avoids running during the warp transition).
    /// </summary>
    public void OnWarped(GameLocation newLocation)
    {
        if (newLocation is not MineShaft mineShaft)
        {
            _pending = null;
            _activeFloor = null;
            return;
        }

        if (_activeFloor is not null && !ReferenceEquals(_activeFloor.MineShaft, mineShaft))
            _activeFloor = null;

        // Next-tick activation is a fallback: only schedule if the harmony path didn't already process today.
        if (!AlreadyProcessedToday(mineShaft) && ShouldEdit(mineShaft))
            _pending = mineShaft;
        else
            _pending = null;
    }


    /// <summary>
    /// Consolidated eligibility gate for Harmony patches.
    /// This is intentionally side-effect free.
    /// </summary>
    public bool ShouldEdit(MineShaft mineShaft)
    {
        if (mineShaft is null)
            return false;

        if (mineShaft.mineLevel == 77377)
            return false;

        var cfg = _config.Get();

        if (!cfg.DesertTombModuleEnabled)
            return false;

        if (!_isMainPlayer())
            return false;

        var step = _getStep();

        if (!DesertTombRuntimePolicy.IsEligibleByProgression(cfg.ProgressionMode, cfg.KeepMineLevel, step))
            return false;

        if (mineShaft.mineLevel <= 120)
            return false;

        if (!DesertTombRuntimePolicy.RollTombChance(cfg.TombChance, mineShaft.mineLevel))
            return false;

        if (AlreadyProcessedToday(mineShaft))
            return false;

        return true;
    }

    /// <summary>
    /// Harmony hook: force the tomb TMX map layout during MineShaft.loadLevel.
    /// </summary>
    public void ApplyTombMapLayoutIfNeeded(MineShaft mineShaft)
    {
        if (mineShaft is null)
            return;

        var cfg = _config.Get();

        if (!cfg.DesertTombModuleEnabled)
            return;

        if (!_isMainPlayer())
            return;

        var step = _getStep();

        if (!DesertTombRuntimePolicy.IsEligibleByProgression(cfg.ProgressionMode, cfg.KeepMineLevel, step))
            return;

        if (mineShaft.mineLevel <= 120 || mineShaft.mineLevel == 77377)
            return;

        var chosen = ChooseTombMapPath(mineShaft, cfg.ProgressionMode, step);

        var before = mineShaft.mapPath.Value;

        if (!string.Equals(before, chosen, StringComparison.OrdinalIgnoreCase))
        {
            mineShaft.mapPath.Value = chosen;
            mineShaft.loadedMapNumber = DesertTombRuntimePolicy.ParseLoadedMapNumber(chosen);
            mineShaft.updateMap();

            if (!_loggedTombLoadLevelVerifier)
            {
                _loggedTombLoadLevelVerifier = true;
                LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.DesertTomb, $"[Verifier][DesertTomb] loadLevel override applied: mineLevel={mineShaft.mineLevel}, before={before}, after={chosen}", LogLevel.Info);
            }
        }

        ResetVanillaSpecialFlagsForTomb(mineShaft);
    }

    /// <summary>
    /// Harmony hook: normalize vanilla "special floor" flags after MineShaft.chooseLevelType.
    /// This prevents infested/dino/quarry rules from leaking into tomb floors.
    /// </summary>
    public void ApplyTombChooseLevelTypeOverrides(MineShaft mineShaft)
    {
        if (mineShaft is null)
            return;

        var cfg = _config.Get();

        if (!cfg.DesertTombModuleEnabled)
            return;

        if (!_isMainPlayer())
            return;

        var step = _getStep();

        var eligibleByProgression =
            (cfg.DesertTombForcePatch && cfg.DesertTombIgnoreProgressionGate)
            || DesertTombRuntimePolicy.IsEligibleByProgression(cfg.ProgressionMode, cfg.KeepMineLevel, step);

        if (!eligibleByProgression)
            return;

        if (mineShaft.mineLevel <= 120 || mineShaft.mineLevel == 77377)
            return;

        if (!cfg.DesertTombForcePatch)
        {
            // Only touch floors that are (or are about to become) tomb layouts.
            if (!DesertTombRuntimePolicy.IsTombCandidate(mineShaft.mineLevel))
                return;

            if (!DesertTombRuntimePolicy.RollTombChance(cfg.TombChance, mineShaft.mineLevel))
                return;

            if (AlreadyProcessedToday(mineShaft))
                return;
        }
        else
        {
            // Hard-force the layout selection at chooseLevelType time, so vanilla doesn't drift into another layout.
            var chosen = WeggGameAssetNames.DesertTombMap;
            var before = mineShaft.mapPath.Value;

            if (!string.Equals(before, chosen, StringComparison.OrdinalIgnoreCase))
            {
                mineShaft.mapPath.Value = chosen;
                mineShaft.loadedMapNumber = DesertTombRuntimePolicy.ParseLoadedMapNumber(chosen);
            }
        }

        ResetVanillaSpecialFlagsForTomb(mineShaft);
    }

    private static void ResetVanillaSpecialFlagsForTomb(MineShaft mineShaft)
    {
        mineShaft.isMonsterArea = false;
        mineShaft.isSlimeArea = false;
        mineShaft.isDinoArea = false;
        mineShaft.isQuarryArea = false;

        mineShaft.rainbowLights.Value = false;
        mineShaft.ambientFog = false;
        mineShaft.fogTime = 0;
    }


    /// <summary>
    /// Harmony hook: run DesertTomb activation during MineShaft.populateLevel and skip vanilla population.
    /// </summary>
    public void PopulateTombFloorFromGeneration(MineShaft mineShaft)
    {
        if (!ShouldEdit(mineShaft))
            return;

        if (!_loggedTombPopulateVerifier)
        {
            _loggedTombPopulateVerifier = true;
            LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.DesertTomb, $"[Verifier][DesertTomb] populateLevel fired: mineLevel={mineShaft.mineLevel}, mapPath={mineShaft.mapPath.Value}", LogLevel.Info);
        }

        ActivateCore(mineShaft);
    }

    private static string ChooseTombMapPath(MineShaft mineShaft, bool progressionModeEnabled, ProgressionStep step)
    {
        // If progression is enabled and player is not on the last phase: always use Level77377.
        // If player is on the last phase, or progression is disabled: randomly pick among available maps.
        if (progressionModeEnabled && step < ProgressionStep.PartVI)
            return WeggGameAssetNames.DesertTombMap;

        string[] available =
        {
            WeggGameAssetNames.DesertTombMap,
            WeggGameAssetNames.MineLevel39,
            WeggGameAssetNames.MineLevel28,
            WeggGameAssetNames.MineLevel19,
            WeggGameAssetNames.MineLevel14,
        };

        var r = Utility.CreateDaySaveRandom(Game1.stats.DaysPlayed, mineShaft.mineLevel, 77377);
        return available[r.Next(available.Length)];
    }


    public void OnDayStarted()
    {
        _pending = null;
        _activeFloor = null;
        _loggedTombLoadLevelVerifier = false;
        _loggedTombPopulateVerifier = false;
    }

    public void OnUpdateTicked()
    {
        // Fallback activation on the tick after a warp: only if harmony didn't already mark this floor processed today.
        if (_pending is not null)
        {
            var mineShaft = _pending;
            _pending = null;

            if (!AlreadyProcessedToday(mineShaft))
                ActivateCoreIfEligible(mineShaft);
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

    private void ActivateCoreIfEligible(MineShaft mineShaft)
    {
        if (!ShouldEdit(mineShaft))
            return;

        ActivateCore(mineShaft);
    }


    private void ActivateCore(MineShaft mineShaft)
    {
        var cfg = _config.Get();
        var step = _getStep();
        var mapPath = mineShaft.mapPath.Value;

        var layoutLevel = DesertTombRuntimePolicy.GuessMineLevelOrDefault(mapPath);

        if (!DefaultTombLayouts.ByLevel.TryGetValue(layoutLevel, out var layout))
        {
            LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.DesertTomb, $"[DesertTomb] No layout registered for {layoutLevel}; skipping.", LogLevel.Warn);
            return;
        }

        // Remove any vanilla mons.
        RemoveExistingMonsters(mineShaft);

        var config = new SpawnConfiguration();
        config.DeepCopySpawnConfiguration(SpawnConfiguration.Default, MapDifficulty(cfg.TombDifficulty));
        WriteActiveBreakableLootMultipliers(mineShaft, config.Multipliers);

        _generator.Generate(mineShaft);

        var planner = new TombSpawnPlanner(new SystemRandomProvider(mineShaft.mineRandom));
        var progressionFlags = new TombSpawnProgressionFlags(cfg.ProgressionMode, step);
        var plan = planner.BuildPlan(layout, config, config.Difficulty, progressionFlags, mapTags: null);

        TrySuppressLadderForPharaoh(mineShaft, plan);

        var areaStates = TombAreaStateBuilder.Build(mineShaft, plan);

        var population = new TombPopulationManager(
            mineShaft: mineShaft,
            plan: plan,
            config: config,
            areas: areaStates,
            executor: _spawnExecutor,
            log: _log
#if MIKE_DEVTOOLS
            , initialWaveFraction: DebugKnobs.TombPopulationInitialWaveFraction
            , reconcileIntervalTicks: DebugKnobs.TombPopulationReconcileIntervalTicks
            , maxSpawnsPerTick: DebugKnobs.TombPopulationMaxSpawnsPerTick
            , minDistanceToPlayerTiles: DebugKnobs.TombPopulationMinDistanceToPlayerTiles
            , allowRespawnAfterInitialFill: DebugKnobs.TombPopulationAllowRespawnAfterInitialFill
#endif
        );

        var spawnedInitial = population.SpawnInitialWave();

        _activeFloor = new ActiveTombFloorState(
            mineShaft: mineShaft,
            layoutLevel: (int)layoutLevel,
            mapPath: mapPath,
            areas: areaStates,
            plan: plan,
            config: config,
            population: population
        );

        MarkProcessedToday(mineShaft);

        _diagnostics?.OnActivated((int)layoutLevel, mapPath, mineShaft, plan, spawnedInitial);

        var plannedTotal = plan.Areas.Sum(a => a.Spawns.Sum(s => s.Count));

        LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.DesertTomb, $"[DesertTomb] Activated on MineLevel={mineShaft.mineLevel} MapPath='{mapPath}'. " +
            $"Layout={layoutLevel} SpawnedInitial={spawnedInitial} (Planned={plannedTotal}).", LogLevel.Info);
    }

    

    private static void TrySuppressLadderForPharaoh(MineShaft mineShaft, TombSpawnPlan plan)
    {
        if (mineShaft is null || plan is null)
            return;

        if (!plan.TotalsByMonsterType.ContainsKey(TombMonsterType.TheTombPharaoh))
            return;

        if (mineShaft.tileBeneathLadder == Vector2.Zero)
            return;

        // Ensure the ladder is unavailable until the Pharaoh is defeated.
        var ladderTile = mineShaft.tileBeneathLadder + new Vector2(0f, -1f);
        var buildings = mineShaft.map?.GetLayer("Buildings");
        if (buildings is not null)
        {
            int x = (int)ladderTile.X;
            int y = (int)ladderTile.Y;

            if (x >= 0 && y >= 0 && x < buildings.LayerWidth && y < buildings.LayerHeight)
                buildings.Tiles[x, y] = null;
        }

        mineShaft.modData[LadderSuppressedKey] = "1";
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

    private static void WriteActiveBreakableLootMultipliers(MineShaft mineShaft, MonsterMultipliers multipliers)
    {
        if (mineShaft is null || multipliers is null)
            return;

        // Used by Harmony patches to scale breakable-container loot at break time.
        mineShaft.modData[BreakableDropRateMultiplierKey] = multipliers.DropRateMultiplier.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
    }


    private static string TodayMarker(MineShaft mineShaft)
        => $"{Game1.stats.DaysPlayed}:{mineShaft.mineLevel}";

    private static RuntimeTombDifficulty MapDifficulty(ConfigTombDifficulty difficulty)
        => difficulty switch
        {
            ConfigTombDifficulty.Easy => RuntimeTombDifficulty.Easy,
            ConfigTombDifficulty.Moderate => RuntimeTombDifficulty.Moderate,
            ConfigTombDifficulty.Hard => RuntimeTombDifficulty.Hard,
            _ => RuntimeTombDifficulty.Moderate,
        };


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
