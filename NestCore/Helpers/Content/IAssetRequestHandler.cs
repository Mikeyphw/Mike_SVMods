using StardewModdingAPI.Events;

namespace Mikew.NestCore.Helpers.Content
{
    /// <summary>
    /// Asset request handler abstraction intended for advanced scenarios (multiple edits, load overrides, etc).
    /// </summary>
    public interface IAssetRequestHandler<in TContext>
    {
        int Order { get; }
        bool StopAfterHandle { get; }
        bool IsMatch(AssetRequestedEventArgs e);
        bool ShouldApply(TContext context, AssetRequestedEventArgs e);
        void Handle(AssetRequestedEventArgs e, TContext context);
    }
}
