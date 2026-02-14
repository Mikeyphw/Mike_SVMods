using StardewModdingAPI.Events;

namespace WildEggsHatching.Infrastructure.Smapi;

/// <summary>
/// Abstraction for SMAPI update tick events.
/// This allows the domain layer to receive ticks without direct SMAPI coupling.
/// </summary>
public interface IUpdateTicker
{
    /// <summary>Registers a handler for update ticks.</summary>
    void Register(Action<UpdateTickedEventArgs> handler);

    /// <summary>Unregisters a handler for update ticks.</summary>
    void Unregister(Action<UpdateTickedEventArgs> handler);
}

/// <summary>
/// Default implementation that uses SMAPI events.
/// </summary>
public sealed class UpdateTicker : IUpdateTicker
{
    private readonly IModEvents _events;
    private bool _disposed;

    public UpdateTicker(IModEvents events)
    {
        _events = events ?? throw new System.ArgumentNullException(nameof(events));
    }

    public void Register(Action<UpdateTickedEventArgs> handler)
    {
        _events.GameLoop.UpdateTicked += handler;
    }

    public void Unregister(Action<UpdateTickedEventArgs> handler)
    {
        _events.GameLoop.UpdateTicked -= handler;
    }

    public void Dispose()
    {
        _disposed = true;
    }
}

/// <summary>
/// No-op ticker for testing - never fires ticks.
/// </summary>
public sealed class NoOpUpdateTicker : IUpdateTicker
{
    public void Register(Action<UpdateTickedEventArgs> handler) { }
    public void Unregister(Action<UpdateTickedEventArgs> handler) { }
}
