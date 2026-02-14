using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace WildEggsHatching.Infrastructure.Smapi;

/// <summary>
/// Centralized SMAPI event routing.
/// All SMAPI events are registered here and forwarded to the coordinator.
/// This isolates SMAPI dependencies from the domain layer.
/// </summary>
public sealed class SmapiEventRouter : IDisposable
{
    private readonly IModEvents _events;
    private readonly WildEggCoordinator _coordinator;
    private bool _disposed;

    /// <summary>
    /// Creates a new SMAPI event router.
    /// </summary>
    /// <param name="events">The SMAPI events API.</param>
    /// <param name="coordinator">The coordinator to route events to.</param>
    public SmapiEventRouter(IModEvents events, WildEggCoordinator coordinator)
    {
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
    }

    /// <summary>
    /// Registers all SMAPI event handlers.
    /// Call this once during mod initialization.
    /// </summary>
    public void Register()
    {
        // Game lifecycle events
        _events.GameLoop.GameLaunched += OnGameLaunched;
        _events.GameLoop.SaveLoaded += OnSaveLoaded;
        _events.GameLoop.DayStarted += OnDayStarted;
        _events.GameLoop.DayEnding += OnDayEnding;
        _events.GameLoop.Saving += OnSaving;
        _events.GameLoop.UpdateTicked += OnUpdateTicked;

        // Player events
        _events.Player.Warped += OnWarped;

        // Content events
        _events.Content.AssetRequested += OnAssetRequested;

        ModEntry.LoggerHelper.Log(
            "SMAPI Event Router registered all handlers",
            LogLevel.Debug
        );
    }

    /// <summary>
    /// Unregisters all SMAPI event handlers.
    /// Call this during mod cleanup.
    /// </summary>
    public void Unregister()
    {
        if (_disposed)
            return;

        // Game lifecycle events
        _events.GameLoop.GameLaunched -= OnGameLaunched;
        _events.GameLoop.SaveLoaded -= OnSaveLoaded;
        _events.GameLoop.DayStarted -= OnDayStarted;
        _events.GameLoop.DayEnding -= OnDayEnding;
        _events.GameLoop.Saving -= OnSaving;
        _events.GameLoop.UpdateTicked -= OnUpdateTicked;

        // Player events
        _events.Player.Warped -= OnWarped;

        // Content events
        _events.Content.AssetRequested -= OnAssetRequested;

        _disposed = true;

        ModEntry.LoggerHelper.Log(
            "SMAPI Event Router unregistered all handlers",
            LogLevel.Debug
        );
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        _coordinator.OnGameLaunched();
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        _coordinator.OnSaveLoaded();
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        _coordinator.OnDayStarted();
    }

    private void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        _coordinator.OnDayEnding();
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
        _coordinator.OnSaving();
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        _coordinator.OnUpdateTicked(e);
    }

    private void OnWarped(object? sender, WarpedEventArgs e)
    {
        _coordinator.OnWarped(e);
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        _coordinator.OnAssetRequested(e);
    }

    public void Dispose()
    {
        Unregister();
        GC.SuppressFinalize(this);
    }
}

