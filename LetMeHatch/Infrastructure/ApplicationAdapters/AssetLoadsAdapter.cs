using System;
using LetMeHatch.Application.UseCases;
using LetMeHatch.GameAssetManagement;
using StardewModdingAPI.Events;

namespace LetMeHatch.Infrastructure.ApplicationAdapters;

/// <summary>Infrastructure adapter that forwards asset-load hooks to the existing asset manager.</summary>
public sealed class AssetLoadsAdapter : IAssetLoads
{
    private readonly GameAssetManager _assets;

    public AssetLoadsAdapter(GameAssetManager assets)
    {
        _assets = assets ?? throw new ArgumentNullException(nameof(assets));
    }

    public void OnMaleClonesTextureRequested(AssetRequestedEventArgs e)
    {
        // Intentionally no-op until MaleClones texture asset wiring is finalized.
    }

    public void OnNestCoreGamePathsRequested(AssetRequestedEventArgs e)
    {
        // Intentionally no-op until NestCore game paths asset wiring is finalized.
    }
}
