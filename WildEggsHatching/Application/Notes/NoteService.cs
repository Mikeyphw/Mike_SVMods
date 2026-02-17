using Microsoft.Xna.Framework;
using WildEggsHatching.Infrastructure.Game;

namespace WildEggsHatching.Application.Notes;

public sealed class NoteService
{
    private readonly IGameStateReader _game;
    private readonly IGameObjectPlacer _placer;
    private readonly IGameMailService _mail;

    public NoteService(IGameStateReader game, IGameObjectPlacer placer, IGameMailService mail)
    {
        _game = game;
        _placer = placer;
        _mail = mail;
    }

    public bool TryPlaceNote(Vector2 tile, int noteObjectId, string mailIdOnPlace)
    {
        var location = _game.GetLocation();
        if (!_placer.IsTilePlaceable(location, tile))
            return false;

        if (!_placer.TryPlaceObject(location, tile, noteObjectId))
            return false;

        _mail.AddMailForTomorrow(mailIdOnPlace, noLetter: true);
        return true;
    }
}
