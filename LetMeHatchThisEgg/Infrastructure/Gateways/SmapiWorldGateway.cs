using LetMeHatchThisEgg.Application.Gateways;
using StardewValley;

namespace LetMeHatchThisEgg.Infrastructure.Gateways;

/// <summary>Reads basic world/player state from Game1.</summary>
public sealed class SmapiWorldGateway : IWorldGateway
{
    public bool IsWorldReady => Context.IsWorldReady;

    public string? CurrentLocationName => Context.IsWorldReady ? Game1.player?.currentLocation?.NameOrUniqueName : null;
}
