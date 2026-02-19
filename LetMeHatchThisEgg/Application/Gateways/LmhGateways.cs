using System;

namespace LetMeHatchThisEgg.Application.Gateways;

/// <summary>Bundles gateway dependencies for LMH application-layer services.</summary>
public sealed class LmhGateways
{
    public LmhGateways(IGameDataGateway gameData, IWorldGateway world, IClock clock, IAssetGateway assets)
    {
        GameData = gameData ?? throw new ArgumentNullException(nameof(gameData));
        World = world ?? throw new ArgumentNullException(nameof(world));
        Clock = clock ?? throw new ArgumentNullException(nameof(clock));
        Assets = assets ?? throw new ArgumentNullException(nameof(assets));
    }

    public IGameDataGateway GameData { get; }
    public IWorldGateway World { get; }
    public IClock Clock { get; }
    public IAssetGateway Assets { get; }
}
