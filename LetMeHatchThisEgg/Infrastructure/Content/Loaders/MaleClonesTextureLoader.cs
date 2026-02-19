using LetMeHatchThisEgg.Application.UseCases;
using Mikew.BirdsOfAFeather.Helpers;
using StardewModdingAPI.Events;
using static Mikew.BirdsOfAFeather.Helpers.ModAssetHandler;

namespace LetMeHatchThisEgg.Infrastructure.Content.Loaders;

/// <summary>Loads LMH's male clones texture asset.</summary>
public sealed class MaleClonesTextureLoader : AssetLoaderBase
{
    private readonly IAssetLoads _loads;
    private readonly string _maleClonesGamePath;

    public MaleClonesTextureLoader(ModAssetHandler assetHandler, IAssetLoads loads)
        : base(AssetLoadPriority.Exclusive)
    {
        _loads = loads;
        _maleClonesGamePath = assetHandler.GetAssetPath(Assets.Animals.MaleClones, AssetPathType.GamePath);
    }

    public override string DebugHandles => "BoAF game path: Assets.Animals.MaleClones";

    public override string DebugGates => "assetNameLower equals male clones game path.";

    public override bool CanLoad(string assetNameLower, AssetRequestedEventArgs e)
        => assetNameLower == _maleClonesGamePath;

    public override void Load(string assetNameLower, AssetRequestedEventArgs e)
        => _loads.OnMaleClonesTextureRequested(e);
}
