using LetMeHatch.Application.UseCases;
using Mikew.NestCore.Helpers;
using StardewModdingAPI.Events;

namespace LetMeHatch.Infrastructure.Content.Loaders;

/// <summary>Loads the NestCore GamePaths asset used to seed fast HasGamePath checks.</summary>
public sealed class NestCoreGamePathsLoader : AssetLoaderBase
{
    private readonly IAssetLoads _loads;
    private readonly string _nestcoreGamePaths;

    public NestCoreGamePathsLoader(ModAssetHandler assetHandler, IAssetLoads loads)
        : base(AssetLoadPriority.Exclusive)
    {
        _loads = loads;
        _nestcoreGamePaths = string.Empty;
    }

    public override string DebugHandles => "NestCore game path: Assets.NestCore.GamePaths";

    public override string DebugGates => "assetNameLower equals nestcore game paths.";

    public override bool CanLoad(string assetNameLower, AssetRequestedEventArgs e)
        => !string.IsNullOrEmpty(_nestcoreGamePaths) && assetNameLower == _nestcoreGamePaths;

    public override void Load(string assetNameLower, AssetRequestedEventArgs e)
        => _loads.OnNestCoreGamePathsRequested(e);
}
