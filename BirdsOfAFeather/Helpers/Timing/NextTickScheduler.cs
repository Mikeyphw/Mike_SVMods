using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace Mikew.BirdsOfAFeather.Helpers.Timing
{
    /// <summary>
    /// Queues actions to run on the next SMAPI update tick.
    /// </summary>
    public sealed class NextTickScheduler : IDisposable
    {
        private readonly IModHelper _helper;
        private readonly IMonitor _monitor;
        private readonly Queue<Action> _queue = new();
        private bool _subscribed;

        public NextTickScheduler(IModHelper helper, IMonitor monitor)
        {
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            Subscribe();
        }

        public void Enqueue(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            _queue.Enqueue(action);
        }

        public void Dispose()
        {
            Unsubscribe();
            _queue.Clear();
        }

        private void Subscribe()
        {
            if (_subscribed)
                return;

            _helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
                return;

            _helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
            _subscribed = false;
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (_queue.Count == 0)
                return;

            while (_queue.Count > 0)
            {
                var action = _queue.Dequeue();
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    _monitor.Log($"NextTick action failed.\n{ex}", LogLevel.Error);
                }
            }
        }
    }
}
