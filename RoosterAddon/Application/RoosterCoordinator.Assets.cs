using RoosterAddon.Application.Assets;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;

namespace RoosterAddon.Application;

internal sealed partial class RoosterCoordinator
{
    private FarmAnimalsAssetEditor? _farmAnimalsEditor;
    private ObjectsAssetEditor? _objectsEditor;

    public void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (!HasDayStarted)
            return;

        if (IsFarmAnimalsAsset(e))
        {
            _farmAnimalsEditor ??= new FarmAnimalsAssetEditor((IRoosterAssetEdits)this);
            _farmAnimalsEditor.Edit(e);
            return;
        }

        if (IsObjectsAsset(e))
        {
            _objectsEditor ??= new ObjectsAssetEditor((IRoosterAssetEdits)this);
            _objectsEditor.Edit(e);
            return;
        }

        if (IsModTextureAsset(e))
            LoadModTexture(e);
    }

    private static bool IsFarmAnimalsAsset(AssetRequestedEventArgs e) =>
        e.NameWithoutLocale.IsEquivalentTo(FarmAnimalsAssetName);

    private static bool IsObjectsAsset(AssetRequestedEventArgs e) =>
        e.NameWithoutLocale.IsEquivalentTo(ObjectsAssetName);

    private bool IsModTextureAsset(AssetRequestedEventArgs e) =>
        e.NameWithoutLocale.BaseName.StartsWith($"Mods/{ModManifest.UniqueID}", System.StringComparison.OrdinalIgnoreCase);

    private void LoadModTexture(AssetRequestedEventArgs e) => AssetHandler.LoadModAssets(e);
}
