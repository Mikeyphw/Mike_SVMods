using System;
using System.Collections.Generic;

namespace WildEggsHatching.Core.Events;

/// <summary>
/// A simple event bus for publishing and subscribing to domain events.
/// Services communicate through events instead of direct method calls.
/// </summary>
public sealed class EventBus
{
    /// <summary>Mapping of event types to their list of handlers.</summary>
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();

    /// <summary>
    /// Subscribes a handler to receive events of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of event to subscribe to.</typeparam>
    /// <param name="handler">The action to invoke when the event is published.</param>
    public void Subscribe<T>(Action<T> handler) where T : IGameEvent
    {
        var type = typeof(T);
        if (!_handlers.ContainsKey(type))
            _handlers[type] = new List<Delegate>();
        _handlers[type].Add(handler);
    }

    /// <summary>
    /// Publishes an event to all subscribed handlers.
    /// </summary>
    /// <typeparam name="T">The type of event to publish.</typeparam>
    /// <param name="event">The event instance to publish.</param>
    public void Publish<T>(T @event) where T : IGameEvent
    {
        var type = typeof(T);
        if (_handlers.TryGetValue(type, out var handlers))
        {
            foreach (var handler in handlers)
            {
                // Safe cast since we only add Action<T> handlers
                (handler as Action<T>)?.Invoke(@event);
            }
        }
    }

    /// <summary>
    /// Unsubscribes a specific handler from receiving events.
    /// Useful for cleaning up event subscriptions.
    /// </summary>
    /// <typeparam name="T">The type of event to unsubscribe from.</typeparam>
    /// <param name="handler">The handler to remove.</param>
    public void Unsubscribe<T>(Action<T> handler) where T : IGameEvent
    {
        var type = typeof(T);
        if (_handlers.TryGetValue(type, out var handlers))
        {
            handlers.Remove(handler);
            if (handlers.Count == 0)
                _handlers.Remove(type);
        }
    }
}


