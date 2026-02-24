using RoosterControl.Application.Assets;
using RoosterControl.Application.Objects;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;

namespace RoosterControl.Application;

internal sealed partial class RoosterCoordinator
{
    private FarmAnimalsAssetEditor? _farmAnimalsEditor;
    private ObjectsAssetEditor? _objectsEditor;
    private FertilizedEggsAssetEditor? _fertilizedEggsEditor;

    private BetterTrufflesContextTagService? _betterTrufflesTags;

    public void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        // Texture requests can happen before DayStarted (e.g., catalog/UI preload).
        // Serve them regardless of gameplay initialization state.
        if (IsModTextureAsset(e))
        {
            LoadModTexture(e);
            return;
        }

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
                cloneEggsEnabled: GetHatchApiCloneEggsOption,
                includeMearasEggs: true
            );
            _fertilizedEggsEditor.Edit(e);

            _betterTrufflesTags ??= new BetterTrufflesContextTagService();
            _objectsEditor ??= new ObjectsAssetEditor(RoosterHelper, _betterTrufflesTags);
            _objectsEditor.Edit(e);
            return;
        }

    }

    private static bool IsFarmAnimalsAsset(AssetRequestedEventArgs e) =>
        e.NameWithoutLocale.IsEquivalentTo(FarmAnimalsAssetName);

    private static bool IsObjectsAsset(AssetRequestedEventArgs e) =>
        e.NameWithoutLocale.IsEquivalentTo(ObjectsAssetName);

    private bool IsModTextureAsset(AssetRequestedEventArgs e) =>
        e.NameWithoutLocale.BaseName.StartsWith($"Mods/{ModManifest.UniqueID}", System.StringComparison.OrdinalIgnoreCase);

    private void LoadModTexture(AssetRequestedEventArgs e) => AssetHandler.LoadModAssets(e);
}
