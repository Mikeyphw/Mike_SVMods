using System;
using StardewModdingAPI.Events;

namespace Mikew.BirdsOfAFeather.Helpers.Content
{
    /// <summary>
    /// Immutable registration for asset request routing.
    /// </summary>
    public sealed class AssetRequestRegistration<TContext> : IAssetRequestHandler<TContext>
    {
        private readonly Func<AssetRequestedEventArgs, bool> _isMatch;
        private readonly Func<TContext, AssetRequestedEventArgs, bool> _shouldApply;
        private readonly Action<TContext, AssetRequestedEventArgs> _handle;

        public int Order { get; }
        public bool StopAfterHandle { get; }
        public string DebugName { get; }

        public AssetRequestRegistration(
            string debugName,
            int order,
            Func<AssetRequestedEventArgs, bool> isMatch,
            Action<TContext, AssetRequestedEventArgs> handle,
            Func<TContext, AssetRequestedEventArgs, bool>? shouldApply = null,
            bool stopAfterHandle = false
        )
        {
            DebugName = string.IsNullOrWhiteSpace(debugName) ? "AssetRequestRegistration" : debugName;
            Order = order;
            _isMatch = isMatch ?? throw new ArgumentNullException(nameof(isMatch));
            _handle = handle ?? throw new ArgumentNullException(nameof(handle));
            _shouldApply = shouldApply ?? ((_, __) => true);
            StopAfterHandle = stopAfterHandle;
        }

        public bool IsMatch(AssetRequestedEventArgs e) => _isMatch(e);

        public bool ShouldApply(TContext context, AssetRequestedEventArgs e) => _shouldApply(context, e);

        public void Handle(AssetRequestedEventArgs e, TContext context) => _handle(context, e);

        public static AssetRequestRegistration<TContext> FromEditor(
            IAssetEditor<TContext> editor,
            int order,
            Func<TContext, AssetRequestedEventArgs, bool>? shouldApply = null
        )
        {
            if (editor == null)
                throw new ArgumentNullException(nameof(editor));

            return new AssetRequestRegistration<TContext>(
                debugName: editor.GetType().Name,
                order: order,
                isMatch: AssetNameMatchers.ForAsset(editor.AssetName),
                handle: (ctx, e) =>
                {
                    e.Edit(asset => editor.Edit(asset, ctx), editor.Priority);

                    if (editor is IPostPassAssetEditor<TContext> post && post.PostPasses is not null)
                    {
                        foreach (var pass in post.PostPasses)
                        {
                            if (pass is null)
                                continue;

                            e.Edit(asset => pass.Apply(asset, ctx), pass.Priority);
                        }
                    }
                },
                shouldApply: shouldApply
            );
        }

        public static AssetRequestRegistration<TContext> FromHandler(
            string debugName,
            int order,
            Func<AssetRequestedEventArgs, bool> isMatch,
            Action<TContext, AssetRequestedEventArgs> handle,
            Func<TContext, AssetRequestedEventArgs, bool>? shouldApply = null,
            bool stopAfterHandle = false
        )
        {
            return new AssetRequestRegistration<TContext>(
                debugName: debugName,
                order: order,
                isMatch: isMatch,
                handle: handle,
                shouldApply: shouldApply,
                stopAfterHandle: stopAfterHandle
            );
        }
    }
}
