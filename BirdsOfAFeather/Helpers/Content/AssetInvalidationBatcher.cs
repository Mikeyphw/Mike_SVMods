using System;
using System.Collections.Generic;
using Mikew.BirdsOfAFeather.Helpers.Logging;
using Mikew.BirdsOfAFeather.Helpers.Timing;
using StardewModdingAPI;

namespace Mikew.BirdsOfAFeather.Helpers.Content
{
    /// <summary>
    /// Batches invalidate requests and flushes them on the next tick to avoid spamming.
    /// </summary>
    public sealed class AssetInvalidationBatcher : IDisposable
    {
        private readonly IModHelper _helper;
        private readonly ILog _log;
        private readonly NextTickScheduler _nextTick;
        private readonly HashSet<string> _pending = new(StringComparer.OrdinalIgnoreCase);
        private bool _flushQueued;

        public AssetInvalidationBatcher(IModHelper helper, ILog log, NextTickScheduler? nextTick = null)
        {
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _nextTick = nextTick ?? new NextTickScheduler(_helper, _log);
        }

        public AssetInvalidationBatcher(IModHelper helper, IMonitor monitor, NextTickScheduler? nextTick = null)
            : this(helper, new SmapiLog(monitor), nextTick)
        {
        }

        public void Request(string assetName)
        {
            if (string.IsNullOrWhiteSpace(assetName))
                return;

            _pending.Add(assetName);

            if (_flushQueued)
                return;

            _flushQueued = true;
            _nextTick.Enqueue(Flush);
        }

        public void Flush()
        {
            _flushQueued = false;

            if (_pending.Count == 0)
                return;

            foreach (var name in _pending)
            {
                try
                {
                    _helper.GameContent.InvalidateCache(name);
                }
                catch (Exception ex)
                {
                    _log.Log($"Failed to invalidate '{name}'.\n{ex}", LogLevel.Error);
                }
            }

            _pending.Clear();
        }

        public void Dispose()
        {
            _pending.Clear();
            _nextTick.Dispose();
        }
    }
}
