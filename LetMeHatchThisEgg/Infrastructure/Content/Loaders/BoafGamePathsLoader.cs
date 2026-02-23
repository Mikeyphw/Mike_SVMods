using LetMeHatchThisEgg.Application.UseCases;
using Mikew.BirdsOfAFeather.Helpers;
using StardewModdingAPI.Events;

namespace LetMeHatchThisEgg.Infrastructure.Content.Loaders;

/// <summary>Loads the BOAF GamePaths asset used to seed fast HasGamePath checks.</summary>
public sealed class BoafGamePathsLoader : AssetLoaderBase
{
    private readonly IAssetLoads _loads;
    private readonly string _boafGamePaths;

    public BoafGamePathsLoader(ModAssetHandler assetHandler, IAssetLoads loads)
        : base(AssetLoadPriority.Exclusive)
    {
        _loads = loads;
        _boafGamePaths = string.Empty;
    }

    public override string DebugHandles => "BoAF game path: Assets.Boaf.GamePaths";

    public override string DebugGates => "assetNameLower equals boaf game paths.";

    public override bool CanLoad(string assetNameLower, AssetRequestedEventArgs e)
        => !string.IsNullOrEmpty(_boafGamePaths) && assetNameLower == _boafGamePaths;

    public override void Load(string assetNameLower, AssetRequestedEventArgs e)
        => _loads.OnBoafGamePathsRequested(e);
}
