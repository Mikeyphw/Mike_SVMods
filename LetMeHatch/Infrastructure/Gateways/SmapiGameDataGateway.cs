using System;
using LetMeHatch.Application.Gateways;
using StardewModdingAPI;

namespace LetMeHatch.Infrastructure.Gateways;

/// <summary>Loads assets via SMAPI's game content API.</summary>
public sealed class SmapiGameDataGateway : IGameDataGateway
{
    private readonly IGameContentHelper _gameContent;

    public SmapiGameDataGateway(IGameContentHelper gameContent)
    {
        _gameContent = gameContent ?? throw new ArgumentNullException(nameof(gameContent));
    }

    public T Load<T>(string assetName) where T : notnull
    {
        if (string.IsNullOrWhiteSpace(assetName))
            throw new ArgumentException("Asset name must be non-empty.", nameof(assetName));

        return _gameContent.Load<T>(assetName);
    }
}
