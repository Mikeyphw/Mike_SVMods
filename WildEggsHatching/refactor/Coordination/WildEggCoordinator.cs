using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewModdingAPI.Events;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Core.Events;
using WildEggsHatching.Core.Models;
using WildEggsHatching.Core.Random;
using WildEggsHatching.Data;
using WildEggsHatching.Infrastructure.Game;
using WildEggsHatching.Notes;
using WildEggsHatching.Progression;
using WildEggsHatching.Spawning;
using WildEggsHatching.Tracking;

namespace WildEggsHatching.Coordination;

/// <summary>
/// The main coordinator that wires together all services.
/// This is the Application layer that orchestrates domain services and infrastructure.
/// All dependencies are created here and passed to services.
/// </summary>
public sealed class WildEggCoordinator
{
    // Core infrastructure
    public EventBus EventBus { get; }
    public PlayerWildState PlayerState { get; }
    public ProgressionState ProgressionState { get; }
    public PlayerStateRepository StateRepository { get; }

    // Infrastructure ports
    private readonly IGameStateReader _gameStateReader;
    private readonly IGameObjectPlacer _objectPlacer;
    private readonly IGameMailService _mailService;
    private readonly IRandomProvider _randomProvider;

    // Feature services
    public SpawnChanceCalculator SpawnChanceCalculator { get; }
    public EggSpawnService EggSpawnService { get; }
    public EggPlacementService EggPlacementService { get; }
    public PhaseProgressionService PhaseProgressionService { get; }
    public UnlockService UnlockService { get; }
    public AnimalTrackingService AnimalTrackingService { get; }
    public NoteService NoteService { get; }
    public AnimalCatalog AnimalCatalog { get; }

    /// <summary>
    /// Creates the coordinator with explicit infrastructure dependencies.
    /// </summary>
    /// <param name="gameStateReader">Game state reader interface.</param>
    /// <param name="objectPlacer">Object placer interface.</param>
    /// <param name="mailService">Mail service interface.</param>
    /// <param name="randomProvider">Random provider for seeded RNG.</param>
    public WildEggCoordinator(
        IGameStateReader gameStateReader,
        IGameObjectPlacer objectPlacer,
        IGameMailService mailService,
        IRandomProvider? randomProvider = null)
    {
        // === Initialize infrastructure ports ===
        _gameStateReader = gameStateReader ?? throw new ArgumentNullException(nameof(gameStateReader));
        _objectPlacer = objectPlacer ?? throw new ArgumentNullException(nameof(objectPlacer));
        _mailService = mailService ?? throw new ArgumentNullException(nameof(mailService));
        _randomProvider = randomProvider ?? new RandomProvider();

        // === Create core domain infrastructure ===
        EventBus = new EventBus();
        PlayerState = new PlayerWildState();
        ProgressionState = new ProgressionState();
        StateRepository = new PlayerStateRepository();

        // === Load saved state ===
        LoadState();

        // === Create data catalog ===
        AnimalCatalog = new AnimalCatalog(new List<WildAnimalData>());

        // === Create spawn system ===
        SpawnChanceCalculator = new SpawnChanceCalculator(baseChance: 0.15f);
        EggPlacementService = new EggPlacementService(EventBus, _objectPlacer);
        EggSpawnService = new EggSpawnService(
            SpawnChanceCalculator,
            EventBus,
            AnimalCatalog,
            _gameStateReader,
            _randomProvider);

        // === Create progression system ===
        PhaseProgressionService = new PhaseProgressionService(
            ProgressionState,
            PlayerState,
            EventBus,
            _mailService);
        UnlockService = new UnlockService(PlayerState, EventBus, _mailService);

        // === Create tracking and notes ===
        AnimalTrackingService = new AnimalTrackingService(EventBus, _gameStateReader);
        NoteService = new NoteService(EventBus, PlayerState, _objectPlacer, _mailService);
    }

    /// <summary>
    /// Creates the coordinator with default infrastructure implementations.
    /// Use this constructor for production use.
    /// </summary>
    public WildEggCoordinator() : this(
        new GameStateReader(),
        new GameObjectPlacer(),
        new GameMailService(),
        new RandomProvider())
    {
    }

    /// <summary>
    /// Loads player state from save data.
    /// </summary>
    private void LoadState()
    {
        var loaded = StateRepository.Load();

        // Copy loaded state to our instances
        foreach (var animal in loaded.UnlockedAnimals)
        {
            PlayerState.Unlock(animal);
        }

        foreach (var animal in loaded.DiscoveredAnimals)
        {
            PlayerState.Discover(animal);
        }

        foreach (var note in loaded.CollectedNotes)
        {
            PlayerState.CollectNote(note);
        }
    }

    /// <summary>
    /// Saves player state to save data.
    /// </summary>
    public void SaveState()
    {
        StateRepository.Save(PlayerState);
    }

    #region Lifecycle Methods (Called by SmapiEventRouter)

    /// <summary>
    /// Called when the game finishes launching.
    /// </summary>
    public void OnGameLaunched()
    {
        ModEntry.LoggerHelper.Log(
            "WildEggCoordinator: GameLaunched",
            LogLevel.Info
        );

        // Initialize any services that need one-time setup
        AnimalTrackingService.RegisterForUpdates();
    }

    /// <summary>
    /// Called when a save file is loaded.
    /// </summary>
    public void OnSaveLoaded()
    {
        ModEntry.LoggerHelper.Log(
            "WildEggCoordinator: SaveLoaded",
            LogLevel.Info
        );

        // Reload state from save
        LoadState();
    }

    /// <summary>
    /// Called when a new day starts.
    /// </summary>
    public void OnDayStarted()
    {
        ModEntry.LoggerHelper.Log(
            $"WildEggCoordinator: DayStarted - Step: {ProgressionState.CurrentStep}",
            LogLevel.Info
        );

        // Evaluate progression first
        PhaseProgressionService.EvaluateProgression();

        // Get current daily luck from infrastructure
        var dailyLuck = GetDailyLuck();

        // Try spawning eggs
        TrySpawnEggs(dailyLuck);
    }

    /// <summary>
    /// Called when the day is ending.
    /// </summary>
    public void OnDayEnding()
    {
        ModEntry.LoggerHelper.Log(
            "WildEggCoordinator: DayEnding",
            LogLevel.Info
        );

        // Save state
        SaveState();
    }

    /// <summary>
    /// Called when the game is saving.
    /// </summary>
    public void OnSaving()
    {
        ModEntry.LoggerHelper.Log(
            "WildEggCoordinator: Saving",
            LogLevel.Info
        );

        // Additional save operations
        SaveState();
    }

    /// <summary>
    /// Called on every game tick.
    /// </summary>
    public void OnUpdateTicked(UpdateTickedEventArgs e)
    {
        // Update tick handling is done through AnimalTrackingService
        // This is a placeholder for any coordinator-level tick logic
    }

    /// <summary>
    /// Called when the player warps to a new location.
    /// </summary>
    public void OnWarped(WarpedEventArgs e)
    {
        // Check for hatching at the new location
        if (e.NewLocation != null)
        {
            AnimalTrackingService.CheckLocationForHatchingEggs(e.NewLocation);
        }
    }

    /// <summary>
    /// Called when the game requests an asset.
    /// </summary>
    public void OnAssetRequested(AssetRequestedEventArgs e)
    {
        // Asset handling is done through GameAssetManager in main code
        // This placeholder is for future content boundary isolation
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Attempts to spawn eggs for all valid locations.
    /// </summary>
    private void TrySpawnEggs(LuckyTypes dailyLuck)
    {
        // Simplified: Try spawning in Farm
        var farm = _gameStateReader.GetLocation("Farm");

        if (farm == null)
        {
            return;
        }

        // Get player tile from infrastructure
        var playerTile = _gameStateReader.GetPlayerTile();
        var tile = playerTile.HasValue
            ? new Vector2(playerTile.Value.X, playerTile.Value.Y)
            : new Vector2(farm.Map.Layers[0].LayerWidth / 2f, farm.Map.Layers[0].LayerHeight / 2f);

        var context = new SpawnContext
        {
            BaseChance = 0.15f,
            DailyLuck = dailyLuck,
            CurrentStep = ProgressionState.CurrentStep,
            CurrentDay = _gameStateReader.GetCurrentDay(),
            MineLevel = _gameStateReader.GetMineLevel(),
            Location = farm,
            Tile = tile,
        };

        var result = EggSpawnService.TrySpawn(context);

        if (result.ShouldSpawn)
        {
            EggPlacementService.Place(result);
        }
    }

    /// <summary>
    /// Converts game luck to our enum using infrastructure reader.
    /// </summary>
    private LuckyTypes GetDailyLuck()
    {
        var dailyLuck = _gameStateReader.GetDailyLuck();

        return dailyLuck switch
        {
            < -0.4 => LuckyTypes.SuperUnlucky,
            < -0.1 => LuckyTypes.Unlucky,
            > 0.4 => LuckyTypes.SuperLucky,
            > 0.1 => LuckyTypes.Lucky,
            _ => LuckyTypes.Neutral
        };
    }

    #endregion

    #region Cleanup

    /// <summary>
    /// Cleans up all event registrations and resources.
    /// Call this during mod cleanup.
    /// </summary>
    public void Cleanup()
    {
        AnimalTrackingService.UnregisterFromUpdates();

        // Clear event subscriptions
        EventBus.UnsubscribeAll();

        ModEntry.LoggerHelper.Log(
            "WildEggCoordinator: Cleanup completed",
            LogLevel.Info
        );
    }

    #endregion
}

