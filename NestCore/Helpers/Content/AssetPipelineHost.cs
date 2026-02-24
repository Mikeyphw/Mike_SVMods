using System;
using System.Collections.Generic;
using Mikew.NestCore.Helpers.Logging;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace Mikew.NestCore.Helpers.Content
{
    /// <summary>
    /// NestCore-owned host for mod asset request routing, edit registration, and batched invalidation.
    /// </summary>
    public sealed class AssetPipelineHost<TContext> : IDisposable
    {
        private readonly AssetRequestCoordinator<TContext> _coordinator;
        private readonly AssetInvalidationBatcher _invalidation;
        private readonly HashSet<string> _invalidationTargets = new(StringComparer.OrdinalIgnoreCase);
        private bool _attached;

        public AssetPipelineHost(IModHelper helper, ILog log, Func<TContext> contextFactory)
        {
            _coordinator = new AssetRequestCoordinator<TContext>(helper, log, contextFactory);
            _invalidation = new AssetInvalidationBatcher(helper, log);
        }

        public AssetPipelineHost(IModHelper helper, IMonitor monitor, Func<TContext> contextFactory)
            : this(helper, new SmapiLog(monitor), contextFactory)
        {
        }

        public void RegisterModAssets(ModAssetHandler handler, int order = -100, bool stopAfterHandle = false)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var router = new ModAssetRequestRouter(handler);
            _coordinator.Register(
                AssetRequestRegistration<TContext>.FromHandler(
                    debugName: "ModAssets",
                    order: order,
                    isMatch: e => handler.HasGamePath(e.NameWithoutLocale.BaseName),
                    handle: (_, e) => router.TryHandle(e),
                    stopAfterHandle: stopAfterHandle
                )
            );
        }

        public void RegisterEditor(
            IAssetEditor<TContext> editor,
            int order = 0,
            Func<TContext, AssetRequestedEventArgs, bool>? shouldApply = null)
        {
            if (editor == null)
                throw new ArgumentNullException(nameof(editor));

            _invalidationTargets.Add(editor.AssetName);
            _coordinator.RegisterEditor(editor, order, shouldApply);
        }

        public void RegisterHandler(IAssetRequestHandler<TContext> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            _coordinator.Register(handler);
        }

        public void AddInvalidationTarget(string assetName)
        {
            if (string.IsNullOrWhiteSpace(assetName))
                return;

            _invalidationTargets.Add(assetName);
        }

        public void Attach()
        {
            if (_attached)
                return;

            _coordinator.Attach();
            _attached = true;
        }

        public void RequestInvalidate(string assetName)
        {
            _invalidation.Request(assetName);
        }

        public void RequestInvalidateAll()
        {
            foreach (var assetName in _invalidationTargets)
                _invalidation.Request(assetName);
        }

        public void Dispose()
        {
            _coordinator.Dispose();
            _invalidation.Dispose();
            _invalidationTargets.Clear();
        }
    }
}
