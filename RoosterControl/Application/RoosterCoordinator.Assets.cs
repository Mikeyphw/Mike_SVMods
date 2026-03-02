using RoosterControl.Application.Assets;
using RoosterControl.Application.Objects;
using RoosterControl.Application.Compat;
using Mikew.NestCore.Helpers.Content;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace RoosterControl.Application;

internal sealed partial class RoosterCoordinator
{
    private FarmAnimalsAssetEditor? _farmAnimalsEditor;
    private ObjectsAssetEditor? _objectsEditor;
    private FertilizedEggsAssetEditor? _fertilizedEggsEditor;

    private BetterTrufflesContextTagService? _betterTrufflesTags;

    private string GetCompatAssetName(string suffix)
        => $"Mods/{ModManifest.UniqueID}/{suffix}";

    private void InitializeAssetPipeline()
    {
        _assetPipeline = new AssetPipelineHost<RoosterCoordinator>(Helper, Monitor, () => this);
        _assetPipeline.RegisterModAssets(AssetHandler, order: -100);

        // Compat catalogs (CP-patchable). These load from built-in defaults; Content Patcher can override them.
        _assetPipeline.RegisterHandler(
            AssetRequestRegistration<RoosterCoordinator>.FromHandler(
                debugName: "CompatEggSourcesCatalog",
                order: -50,
                isMatch: e => e.NameWithoutLocale.BaseName.Equals(GetCompatAssetName(RoosterControl.Application.Compat.CompatDefaults.EggSourcesAssetSuffix), System.StringComparison.OrdinalIgnoreCase),
                shouldApply: (_, _) => true,
                handle: (ctx, e) =>
                {
                    e.LoadFrom(
                        () => RoosterControl.Application.Compat.CompatDefaults.CreateEggSourcesCatalog(),
                        AssetLoadPriority.Exclusive
                    );
                })
        );

        _assetPipeline.RegisterHandler(
            AssetRequestRegistration<RoosterCoordinator>.FromHandler(
                debugName: "CompatRoosterSourcesCatalog",
                order: -49,
                isMatch: e => e.NameWithoutLocale.BaseName.Equals(GetCompatAssetName(RoosterControl.Application.Compat.CompatDefaults.RoosterSourcesAssetSuffix), System.StringComparison.OrdinalIgnoreCase),
                shouldApply: (_, _) => true,
                handle: (ctx, e) =>
                {
                    e.LoadFrom(
                        () => RoosterControl.Application.Compat.CompatDefaults.CreateRoosterSourcesCatalog(),
                        AssetLoadPriority.Exclusive
                    );
                })
        );


        _assetPipeline.RegisterHandler(
            AssetRequestRegistration<RoosterCoordinator>.FromHandler(
                debugName: nameof(FarmAnimalsAssetEditor),
                order: 10,
                isMatch: e => e.NameWithoutLocale.BaseName.Equals(FarmAnimalsAssetName, System.StringComparison.OrdinalIgnoreCase),
                shouldApply: (ctx, _) => ctx.HasDayStarted,
                handle: (ctx, e) =>
                {
                    ctx._farmAnimalsEditor ??= new FarmAnimalsAssetEditor(ctx.RoosterHelper, ctx.SetRoosterData, ctx.TryApplyDailyFarmAnimalsEdits);
                    ctx._farmAnimalsEditor.Edit(e);
                })
        );

        _assetPipeline.RegisterHandler(
            AssetRequestRegistration<RoosterCoordinator>.FromHandler(
                debugName: nameof(ObjectsAssetEditor),
                order: 20,
                isMatch: e => e.NameWithoutLocale.BaseName.Equals(ObjectsAssetName, System.StringComparison.OrdinalIgnoreCase),
                shouldApply: (ctx, _) => ctx.HasDayStarted,
                handle: (ctx, e) =>
                {
                    ctx._fertilizedEggsEditor ??= new FertilizedEggsAssetEditor(
                        roosterHelper: ctx.RoosterHelper,
                        getFertilizedNameSuffix: () => ctx._i18n.GetString("egg.fertilized.name"),
                        getFertilizedDescription: () => ctx._i18n.GetString("egg.fertilized.description"),
                        cloneEggsEnabled: ctx.GetHatchApiCloneEggsOption,
                        includeMearasEggs: true
                    );
                    ctx._fertilizedEggsEditor.Edit(e);

                    ctx._betterTrufflesTags ??= new BetterTrufflesContextTagService();
                    ctx._objectsEditor ??= new ObjectsAssetEditor(ctx.RoosterHelper, ctx._betterTrufflesTags);
                    ctx._objectsEditor.Edit(e);
                })
        );

        _assetPipeline.Attach();
    }
}
