using LetMeHatchThisEgg.GameAssetManagement;
using Mikew.BirdsOfAFeather.Helpers;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using static Mikew.BirdsOfAFeather.Helpers.ModAssetHandler;

namespace LetMeHatchThisEgg.Infrastructure.Content.Loaders;

/// <summary>Loads LMH's cloned eggs texture asset and applies its post-load edit.</summary>
public sealed class ClonedEggsAssetLoader : AssetLoaderBase
{
    private readonly GameAssetManager _assets;
    private readonly string _clonedItemsGamePath;

    public ClonedEggsAssetLoader(ModAssetHandler assetHandler, GameAssetManager assets)
        : base(AssetLoadPriority.Exclusive)
    {
        _assets = assets;
        _clonedItemsGamePath = assetHandler.GetAssetPath(Assets.Object.ClonedItems, AssetPathType.GamePath);
    }

    public override string DebugHandles => "BoAF game path: Assets.Object.ClonedItems";

    public override string DebugGates => "assetNameLower equals cloned items game path.";

    public override bool CanLoad(string assetNameLower, AssetRequestedEventArgs e)
        => assetNameLower == _clonedItemsGamePath;

    public override void Load(string assetNameLower, AssetRequestedEventArgs e)
    {
        _assets.HandleClonedEggsAsset(e);
    }
}
