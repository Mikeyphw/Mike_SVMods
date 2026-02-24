using LetMeHatch.Application.UseCases;
using Mikew.NestCore.Helpers;
using StardewModdingAPI.Events;
using static LetMeHatch.EnumsAndStrings.Enums;

namespace LetMeHatch.Infrastructure.Content.Loaders;

/// <summary>Loads LMH's cloned eggs texture asset and applies its post-load edit.</summary>
public sealed class ClonedEggsAssetLoader : AssetLoaderBase
{
    private readonly IAssetLoads _loads;
    private readonly string _clonedItemsGamePath;

    public ClonedEggsAssetLoader(ModAssetHandler assetHandler, IAssetLoads loads)
        : base(AssetLoadPriority.Exclusive)
    {
        _loads = loads;
        _clonedItemsGamePath = assetHandler.GetAssetPath(Assets.Object.ClonedItems, ModAssetHandler.AssetPathType.GamePath);
    }

    public override string DebugHandles => "NestCore game path: Assets.Object.ClonedItems";

    public override string DebugGates => "assetNameLower equals cloned items game path.";

    public override bool CanLoad(string assetNameLower, AssetRequestedEventArgs e)
        => assetNameLower == _clonedItemsGamePath;

    public override void Load(string assetNameLower, AssetRequestedEventArgs e)
        => _loads.OnClonedEggsAssetRequested(e);
}
