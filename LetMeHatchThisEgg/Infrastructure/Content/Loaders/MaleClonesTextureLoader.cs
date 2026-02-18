using System.Text.RegularExpressions;
using LetMeHatchThisEgg.GameAssetManagement;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace LetMeHatchThisEgg.Infrastructure.Content.Loaders;

/// <summary>Loads male clone animal textures from content packs.</summary>
public sealed class MaleClonesTextureLoader : AssetLoaderBase
{
    private static readonly Regex Pattern = new(@"^Animals/MaleClones", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly GameAssetManager _assets;

    public MaleClonesTextureLoader(GameAssetManager assets)
        : base(AssetLoadPriority.Exclusive)
    {
        _assets = assets;
    }

    public override bool CanLoad(string assetNameLower, AssetRequestedEventArgs e)
        => Pattern.IsMatch(assetNameLower);

    public override void Load(string assetNameLower, AssetRequestedEventArgs e)
    {
        _assets.TextureManager.LoadTextureFromContentPack(
            assetNameLower,
            _assets.ClonedAnimalsInfoList,
            _assets.LoadedContentPacks,
            e,
            Priority
        );
    }
}
