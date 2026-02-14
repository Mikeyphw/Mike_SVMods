using WildEggsHatching.Infrastructure.Game;

namespace WildEggsHatching.Application.Progression;

public sealed record UnlockContext(string LocationName, string PlayerName);

public sealed class UnlockContextProvider
{
    private readonly IGameStateReader _game;

    public UnlockContextProvider(IGameStateReader game)
    {
        _game = game;
    }

    public UnlockContext Get()
    {
        var location = _game.GetLocation();
        var player = _game.GetPlayer();
        return new UnlockContext(location.NameOrUniqueName, player.Name);
    }
}
