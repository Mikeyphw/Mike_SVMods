using System;
using System.Linq;
using WildEggsHatching.Infrastructure.Game;
using WildEggsHatching.Infrastructure.ModData.Models;

namespace WildEggsHatching.Application.Spawning;

public sealed class DefaultAreaConditionsEvaluator : IAreaConditionsEvaluator
{
    private readonly IGameStateReader _game;
    private readonly IGameStateQueryEvaluator _gsq;

    public DefaultAreaConditionsEvaluator(IGameStateReader game, IGameStateQueryEvaluator gsq)
    {
        _game = game;
        _gsq = gsq;
    }

    public bool AreMet(ExtraConditionsJson conditions)
    {
        if (!string.IsNullOrWhiteSpace(conditions.GameStateQuery) && !_gsq.Check(conditions.GameStateQuery))
            return false;

        if (_game.GetYear() < conditions.StartYear)
            return false;

        if (conditions.Seasons.Count > 0 && !conditions.Seasons.Contains(_game.GetSeason(), StringComparer.OrdinalIgnoreCase))
            return false;

        if (conditions.Days.Count > 0 && !conditions.Days.Contains(_game.GetDayOfMonth()))
            return false;

        // Weather conditions intentionally deferred (orig uses stored weather strings).
        return true;
    }
}
