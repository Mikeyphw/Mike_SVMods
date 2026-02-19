using System;
using LetMeHatchThisEgg.Application.UseCases;
using LetMeHatchThisEgg.GameAssetManagement;
using StardewModdingAPI.Events;

namespace LetMeHatchThisEgg.Infrastructure.ApplicationAdapters;

/// <summary>Infrastructure adapter that forwards asset-load hooks to the existing asset manager.</summary>
public sealed class AssetLoadsAdapter : IAssetLoads
{
    private readonly GameAssetManager _assets;

    public AssetLoadsAdapter(GameAssetManager assets)
    {
        _assets = assets ?? throw new ArgumentNullException(nameof(assets));
    }

    public void OnClonedEggsAssetRequested(AssetRequestedEventArgs e) => _assets.HandleClonedEggsAsset(e);

    public void OnMaleClonesTextureRequested(AssetRequestedEventArgs e) => _assets.HandleMaleClonesTextureAsset(e);

    public void OnBoafGamePathsRequested(AssetRequestedEventArgs e) => _assets.HandleBoafGamePathsAsset(e);
}
