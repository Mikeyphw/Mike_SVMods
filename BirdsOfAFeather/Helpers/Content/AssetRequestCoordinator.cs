using System;
using System.Collections.Generic;
using System.Linq;
using Mikew.BirdsOfAFeather.Helpers.Logging;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace Mikew.BirdsOfAFeather.Helpers.Content
{
    /// <summary>
    /// Central coordinator for SMAPI asset requests. Supports both editor-style registrations and advanced handlers.
    /// </summary>
    public sealed class AssetRequestCoordinator<TContext> : IDisposable
    {
        private readonly IModHelper _helper;
        private readonly ILog _log;
        private readonly Func<TContext> _contextFactory;
        private readonly List<IAssetRequestHandler<TContext>> _registrations = new();
        private bool _subscribed;
        private bool _sorted;

        public AssetRequestCoordinator(IModHelper helper, ILog log, Func<TContext> contextFactory)
        {
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        public AssetRequestCoordinator(IModHelper helper, IMonitor monitor, Func<TContext> contextFactory)
            : this(helper, new SmapiLog(monitor), contextFactory)
        {
        }

        public void Attach()
        {
            if (_subscribed)
                return;

            _helper.Events.Content.AssetRequested += OnAssetRequested;
            _subscribed = true;
        }

        public void Detach()
        {
            if (!_subscribed)
                return;

            _helper.Events.Content.AssetRequested -= OnAssetRequested;
            _subscribed = false;
        }

        public void Register(IAssetRequestHandler<TContext> registration)
        {
            if (registration == null)
                throw new ArgumentNullException(nameof(registration));

            _registrations.Add(registration);
            _sorted = false;
        }

        public void RegisterEditor(IAssetEditor<TContext> editor, int order = 0, Func<TContext, AssetRequestedEventArgs, bool>? shouldApply = null)
        {
            Register(AssetRequestRegistration<TContext>.FromEditor(editor, order, shouldApply));
        }

        public void Handle(AssetRequestedEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            if (_registrations.Count == 0)
                return;

            EnsureSorted();

            TContext? context = default;
            var hasContext = false;

            foreach (var reg in _registrations)
            {
                bool match;
                try
                {
                    match = reg.IsMatch(e);
                }
                catch (Exception ex)
                {
                    _log.Log($"Asset match failed ({reg.GetType().Name}).\n{ex}", LogLevel.Error);
                    continue;
                }

                if (!match)
                    continue;

                if (!hasContext)
                {
                    try
                    {
                        context = _contextFactory();
                        hasContext = true;
                    }
                    catch (Exception ex)
                    {
                        _log.Log($"Asset context factory failed.\n{ex}", LogLevel.Error);
                        return;
                    }
                }

                try
                {
                    if (!reg.ShouldApply(context!, e))
                        continue;
                }
                catch (Exception ex)
                {
                    _log.Log($"Asset gating failed ({reg.GetType().Name}).\n{ex}", LogLevel.Error);
                    continue;
                }

                try
                {
                    reg.Handle(e, context!);
                }
                catch (Exception ex)
                {
                    _log.Log($"Asset handler failed ({reg.GetType().Name}).\n{ex}", LogLevel.Error);
                }

                if (reg.StopAfterHandle)
                    break;
            }
        }

        public void Dispose()
        {
            Detach();
            _registrations.Clear();
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            Handle(e);
        }

        private void EnsureSorted()
        {
            if (_sorted)
                return;

            _registrations.Sort((a, b) => a.Order.CompareTo(b.Order));
            _sorted = true;
        }
    }
}
