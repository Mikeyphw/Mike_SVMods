using StardewModdingAPI.Events;

namespace LetMeHatchThisEgg.Application.UseCases;

/// <summary>Application entrypoints for asset load handlers (Infrastructure triggers, logic owned by mod).</summary>
public interface IAssetLoads
{
    void OnClonedEggsAssetRequested(AssetRequestedEventArgs e);
    void OnMaleClonesTextureRequested(AssetRequestedEventArgs e);
    void OnBoafGamePathsRequested(AssetRequestedEventArgs e);
}
