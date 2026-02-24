using System;
using LetMeHatch.Application.Gateways;
using Mikew.NestCore.Helpers.Content;

namespace LetMeHatch.Infrastructure.Gateways;

/// <summary>Invalidates asset caches via SMAPI's game content API.</summary>
public sealed class SmapiAssetGateway : IAssetGateway
{
    private readonly AssetInvalidationBatcher _batcher;

    public SmapiAssetGateway(AssetInvalidationBatcher batcher)
    {
        _batcher = batcher ?? throw new ArgumentNullException(nameof(batcher));
    }

    public void Invalidate(string assetName)
    {
        if (string.IsNullOrWhiteSpace(assetName))
            return;

        _batcher.Request(assetName);
    }
}
