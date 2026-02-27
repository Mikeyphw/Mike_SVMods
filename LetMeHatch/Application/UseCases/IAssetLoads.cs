using StardewModdingAPI.Events;

namespace LetMeHatch.Application.UseCases;

/// <summary>Application entrypoints for asset load handlers (Infrastructure triggers, logic owned by mod).</summary>
public interface IAssetLoads
{
    void OnMaleClonesTextureRequested(AssetRequestedEventArgs e);
    void OnNestCoreGamePathsRequested(AssetRequestedEventArgs e);
}
