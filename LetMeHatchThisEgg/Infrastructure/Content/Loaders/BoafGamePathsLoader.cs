using Mikew.BirdsOfAFeather.Helpers;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using static Mikew.BirdsOfAFeather.Helpers.ModAssetHandler;

namespace LetMeHatchThisEgg.Infrastructure.Content.Loaders;

/// <summary>Loads BoAF-managed game-path assets (excluding LMH's cloned-eggs special case).</summary>
public sealed class BoafGamePathsLoader : AssetLoaderBase
{
    private readonly ModAssetHandler _assetHandler;
    private readonly string _clonedItemsGamePath;

    public BoafGamePathsLoader(ModAssetHandler assetHandler)
        : base(AssetLoadPriority.Exclusive)
    {
        _assetHandler = assetHandler;
        _clonedItemsGamePath = assetHandler.GetAssetPath(Assets.Object.ClonedItems, AssetPathType.GamePath);
    }

    public override string DebugHandles => "BoAF GamePaths (excluding cloned items)";

    public override string DebugGates => "_assetHandler.GamePaths contains assetNameLower and assetNameLower != cloned items game path.";

    public override bool CanLoad(string assetNameLower, AssetRequestedEventArgs e)
        => _assetHandler.GamePaths.Contains(assetNameLower) && assetNameLower != _clonedItemsGamePath;

    public override void Load(string assetNameLower, AssetRequestedEventArgs e)
    {
        _assetHandler.LoadModAssets(e);
    }
}
