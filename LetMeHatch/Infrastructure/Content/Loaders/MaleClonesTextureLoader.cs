using LetMeHatch.Application.UseCases;
using Mikew.NestCore.Helpers;
using StardewModdingAPI.Events;

namespace LetMeHatch.Infrastructure.Content.Loaders;

/// <summary>Loads LMH's male clones texture asset.</summary>
public sealed class MaleClonesTextureLoader : AssetLoaderBase
{
    private readonly IAssetLoads _loads;
    private readonly string _maleClonesGamePath;

    public MaleClonesTextureLoader(ModAssetHandler assetHandler, IAssetLoads loads)
        : base(AssetLoadPriority.Exclusive)
    {
        _loads = loads;
        _maleClonesGamePath = string.Empty;
    }

    public override string DebugHandles => "NestCore game path: Assets.Animals.MaleClones";

    public override string DebugGates => "assetNameLower equals male clones game path.";

    public override bool CanLoad(string assetNameLower, AssetRequestedEventArgs e)
        => !string.IsNullOrEmpty(_maleClonesGamePath) && assetNameLower == _maleClonesGamePath;

    public override void Load(string assetNameLower, AssetRequestedEventArgs e)
        => _loads.OnMaleClonesTextureRequested(e);
}
