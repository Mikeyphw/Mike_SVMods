namespace LetMeHatchThisEgg.Application.Gateways;

/// <summary>Abstracts safe access to world/player state without leaking Game1 types.</summary>
public interface IWorldGateway
{
    bool IsWorldReady { get; }
    string? CurrentLocationName { get; }
}
