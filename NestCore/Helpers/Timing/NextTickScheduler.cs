using System;
using System.Collections.Generic;
using Mikew.NestCore.Helpers.Logging;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace Mikew.NestCore.Helpers.Timing
{
    /// <summary>
    /// Queues actions to run on the next SMAPI update tick.
    /// </summary>
    public sealed class NextTickScheduler : IDisposable
    {
        private readonly IModHelper _helper;
        private readonly ILog _log;
        private readonly Queue<Action> _queue = new();
        private bool _subscribed;

        public NextTickScheduler(IModHelper helper, ILog log)
        {
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            Subscribe();
        }

        public NextTickScheduler(IModHelper helper, IMonitor monitor)
            : this(helper, new SmapiLog(monitor))
        {
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
                    _log.Log($"NextTick action failed.\n{ex}", LogLevel.Error);
                }
            }
        }
    }
}
