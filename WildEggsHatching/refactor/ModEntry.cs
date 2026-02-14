using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using WildEggsHatching.Coordination;
using WildEggsHatching.Infrastructure;
using WildEggsHatching.Infrastructure.Content;
using WildEggsHatching.Infrastructure.Content.Editors;
using WildEggsHatching.Infrastructure.Game;
using WildEggsHatching.Infrastructure.Smapi;

namespace WildEggsHatching;

/// <summary>
/// The main entry point for the Wild Eggs Hatching mod.
/// This is a thin entry point that wires up infrastructure and delegates all logic to services.
/// </summary>
public sealed class ModEntry : StardewModdingAPI.Mod
{
    /// <summary>Global access to the SMAPI mod helper.</summary>
    public static IModHelper? HelperInstance { get; private set; }

    /// <summary>Global access to the SMAPI monitor for logging.</summary>
    public static IMonitor? MonitorInstance { get; private set; }

    /// <summary>Global helper for logging with consistent formatting.</summary>
    public static LoggerHelper LoggerHelper { get; private set; } = null!;

    /// <summary>The main coordinator that orchestrates all services.</summary>
    private WildEggCoordinator? _coordinator;

    /// <summary>The SMAPI event router that handles all event subscriptions.</summary>
    private SmapiEventRouter? _eventRouter;

    /// <summary>The content editor for handling asset modifications.</summary>
    private SmapiContentEditor? _contentEditor;

    /// <summary>The config provider for accessing mod configuration.</summary>
    private IConfigProvider? _configProvider;

    /// <summary>
    /// Called when the mod is loaded by SMAPI.
    /// </summary>
    public override void Entry(IModHelper helper)
    {
        // Store static references for infrastructure classes
        HelperInstance = helper;
        MonitorInstance = Monitor;
        LoggerHelper = new LoggerHelper(Monitor, Constants.ModId);

        LoggerHelper.Log("ModEntry: Starting initialization", LogLevel.Info);

        try
        {
            // Initialize configuration
            InitializeConfig(helper);

            // Initialize infrastructure
            InitializeInfrastructure();

            // Register SMAPI events
            RegisterEvents();

            LoggerHelper.Log("ModEntry: Initialization complete", LogLevel.Info);
        }
        catch (Exception ex)
        {
            LoggerHelper.Log($"ModEntry: Initialization failed - {ex.Message}", LogLevel.Error);
            LoggerHelper.Log($"Stack trace: {ex.StackTrace}", LogLevel.Error);
        }
    }

    /// <summary>
    /// Initializes the mod configuration.
    /// </summary>
    private void InitializeConfig(IModHelper helper)
    {
        var config = helper.ReadConfig<ModConfig>();
        
        _configProvider = new ConfigProvider(config);
        LoggerHelper.Log("ModEntry: Configuration loaded", LogLevel.Debug);
    }

    /// <summary>
    /// Initializes all infrastructure components.
    /// </summary>
    private void InitializeInfrastructure()
    {
        // Create the coordinator with infrastructure dependencies
        _coordinator = new WildEggCoordinator(
            new GameStateReader(),
            new GameObjectPlacer(),
            new GameMailService());

        // Create the content editor
        _contentEditor = new SmapiContentEditor(HelperInstance, MonitorInstance);

        // Wire up progression-aware editors
        WireUpAssetEditors();

        LoggerHelper.Log("ModEntry: Infrastructure initialized", LogLevel.Debug);
    }

    /// <summary>
    /// Wires up all asset editors to the content editor.
    /// </summary>
    private void WireUpAssetEditors()
    {
        if (_contentEditor == null || _configProvider == null)
            return;

        // Create progression service for progression-aware editors
        var progressionService = _coordinator?.PhaseProgressionService;

        // Register each editor with the content editor
        // ObjectsEditor - for wild egg items
        var objectsEditor = new ObjectsEditor(progressionService!, _configProvider, MonitorInstance!, HelperInstance!);
        _contentEditor.RegisterAssetEditor(
            GameAssets.Objects,
            AssetEditPriority.Default,
            e => objectsEditor.Edit(e, GameAssets.Objects));

        // FarmAnimalsEditor - for animal definitions (no IModHelper in constructor)
        var farmAnimalsEditor = new FarmAnimalsEditor(progressionService!, _configProvider, MonitorInstance!);
        _contentEditor.RegisterAssetEditor(
            GameAssets.FarmAnimals,
            AssetEditPriority.Low,
            e => farmAnimalsEditor.Edit(e, GameAssets.FarmAnimals));

        // MachinesEditor - for incubator output rules
        var machinesEditor = new MachinesEditor(progressionService!, _configProvider, MonitorInstance!, HelperInstance!);
        _contentEditor.RegisterAssetEditor(
            GameAssets.MachineOutput,
            AssetEditPriority.Default,
            e => machinesEditor.Edit(e, GameAssets.MachineOutput));

        // MailEditor - for progression mail
        var mailEditor = new MailEditor(progressionService!, _configProvider, MonitorInstance!, HelperInstance!);
        _contentEditor.RegisterAssetEditor(
            GameAssets.Mail,
            AssetEditPriority.Default,
            e => mailEditor.Edit(e, GameAssets.Mail));

        // QuestEditor - for quest definitions
        var questEditor = new QuestEditor(progressionService!, _configProvider, MonitorInstance!, HelperInstance!);
        _contentEditor.RegisterAssetEditor(
            GameAssets.Quests,
            AssetEditPriority.Default,
            e => questEditor.Edit(e, GameAssets.Quests));

        // SpecialOrdersEditor - for special order definitions
        var specialOrdersEditor = new SpecialOrdersEditor(progressionService!, _configProvider, MonitorInstance!, HelperInstance!);
        _contentEditor.RegisterAssetEditor(
            GameAssets.SpecialOrders,
            AssetEditPriority.Default,
            e => specialOrdersEditor.Edit(e, GameAssets.SpecialOrders));

        // BigCraftablesEditor - for incubator craftable
        var bigCraftablesEditor = new BigCraftablesEditor(progressionService!, _configProvider, MonitorInstance!, HelperInstance!);
        _contentEditor.RegisterAssetEditor(
            GameAssets.BigCraftables,
            AssetEditPriority.Default,
            e => bigCraftablesEditor.Edit(e, GameAssets.BigCraftables));

        // CraftingRecipesEditor - for incubator recipe
        var craftingRecipesEditor = new CraftingRecipesEditor(progressionService!, _configProvider, MonitorInstance!, HelperInstance!);
        _contentEditor.RegisterAssetEditor(
            GameAssets.CraftingRecipes,
            AssetEditPriority.Default,
            e => craftingRecipesEditor.Edit(e, GameAssets.CraftingRecipes));

        LoggerHelper.Log("ModEntry: Asset editors wired up", LogLevel.Debug);
    }

    /// <summary>
    /// Registers all SMAPI event handlers.
    /// </summary>
    private void RegisterEvents()
    {
        if (_coordinator == null)
        {
            LoggerHelper.Log("ModEntry: Cannot register events - coordinator not initialized", LogLevel.Error);
            return;
        }

        // Get SMAPI events from helper
        var events = HelperInstance?.Events;
        if (events == null)
        {
            LoggerHelper.Log("ModEntry: Cannot get SMAPI events", LogLevel.Error);
            return;
        }

        // Create and register the event router
        _eventRouter = new SmapiEventRouter(events, _coordinator);
        _eventRouter.Register();

        // Register content editors
        events.Content.AssetRequested += OnAssetRequested;

        LoggerHelper.Log("ModEntry: Events registered", LogLevel.Debug);
    }

    /// <summary>
    /// Handles asset requests from SMAPI's content pipeline.
    /// </summary>
    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        // Forward to content editor for handling
        _contentEditor?.OnAssetRequested(e);
    }

    /// <summary>
    /// Called when the mod is being notified that the player is starting a new day.
    /// </summary>
    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        _coordinator?.OnDayStarted();
    }

    /// <summary>
    /// Called when the mod is being notified that the player is warping to a new location.
    /// </summary>
    private void OnWarped(object? sender, WarpedEventArgs e)
    {
        _coordinator?.OnWarped(e);
    }

    /// <summary>
    /// Called when the mod is being notified that the game is saving.
    /// </summary>
    private void OnSaving(object? sender, SavingEventArgs e)
    {
        _coordinator?.OnSaving();
    }

    /// <summary>
    /// Called when the mod is being notified that the player is loading a save.
    /// </summary>
    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        _coordinator?.OnSaveLoaded();
    }

    /// <summary>
    /// Called when the mod is being notified that the game has finished launching.
    /// </summary>
    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        _coordinator?.OnGameLaunched();
    }

    /// <summary>
    /// Called when the mod is being notified that the in-game clock has ticked.
    /// </summary>
    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        // Forward to coordinator for tick handling
        // Note: This can be expensive, consider removing if not needed
    }

    /// <summary>
    /// Called when the mod is being unloaded.
    /// </summary>
    public override void Exit()
    {
        LoggerHelper.Log("ModEntry: Exiting mod", LogLevel.Info);

        // Clean up event router
        _eventRouter?.Dispose();

        // Clean up coordinator
        _coordinator?.Cleanup();

        LoggerHelper.Log("ModEntry: Exit complete", LogLevel.Info);
    }
}

/// <summary>
/// Helper class for consistent logging across the mod.
/// </summary>
public class LoggerHelper
{
    private readonly IMonitor _monitor;
    private readonly string _modId;

    public LoggerHelper(IMonitor monitor, string modId)
    {
        _monitor = monitor;
        _modId = modId;
    }

    public void Log(string message, LogLevel level = LogLevel.Debug)
    {
        _monitor.Log($"[{_modId}] {message}", level);
    }

    public void LogInfo(string message) => Log(message, LogLevel.Info);
    public void LogDebug(string message) => Log(message, LogLevel.Debug);
    public void LogWarn(string message) => Log(message, LogLevel.Warn);
    public void LogError(string message) => Log(message, LogLevel.Error);
}

/// <summary>
/// Implementation of IConfigProvider that wraps ModConfig.
/// </summary>
public sealed class ConfigProvider : IConfigProvider
{
    public ModConfig Config { get; }

    public ConfigProvider(ModConfig config)
    {
        Config = config ?? new ModConfig();
    }
}

