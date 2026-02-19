using RoosterAddon.Application.Assets;
using RoosterAddon.Application.Objects;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;

namespace RoosterAddon.Application;

internal sealed partial class RoosterCoordinator
{
    private FarmAnimalsAssetEditor? _farmAnimalsEditor;
    private ObjectsAssetEditor? _objectsEditor;
    private FertilizedEggsAssetEditor? _fertilizedEggsEditor;

    private BetterTrufflesContextTagService? _betterTrufflesTags;

    public void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (!HasDayStarted)
            return;

        if (IsFarmAnimalsAsset(e))
        {
            _farmAnimalsEditor ??= new FarmAnimalsAssetEditor(RoosterHelper, SetRoosterData);
            _farmAnimalsEditor.Edit(e);
            return;
        }

        if (IsObjectsAsset(e))
        {
            _fertilizedEggsEditor ??= new FertilizedEggsAssetEditor(
                roosterHelper: RoosterHelper,
                getFertilizedNameSuffix: () => Helper.Translation.Get("egg.fertilized.name"),
                getFertilizedDescription: () => Helper.Translation.Get("egg.fertilized.description"),
                cloneEggsEnabled: GetHatchApiCloneEggsOption
            );
            _fertilizedEggsEditor.Edit(e);

            _betterTrufflesTags ??= new BetterTrufflesContextTagService();
            _objectsEditor ??= new ObjectsAssetEditor(RoosterHelper, _betterTrufflesTags);
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
