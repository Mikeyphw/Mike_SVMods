using StardewModdingAPI.Events;

namespace Mikew.BirdsOfAFeather.Helpers.Content
{
    /// <summary>
    /// Asset editor abstraction intended for simple <c>e.Edit(...)</c> usage.
    /// </summary>
    public interface IAssetEditor<in TContext>
    {
        string AssetName { get; }
        AssetEditPriority Priority { get; }
        void Edit(IAssetData asset, TContext context);
    }
}
