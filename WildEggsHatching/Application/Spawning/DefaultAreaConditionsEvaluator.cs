using System;
using System.Linq;
using StardewValley;
using WildEggsHatching.Infrastructure.Game;
using WildEggsHatching.Infrastructure.ModData.Models;

namespace WildEggsHatching.Application.Spawning;

public sealed class DefaultAreaConditionsEvaluator : IAreaConditionsEvaluator
{
    private readonly IGameStateReader _game;
    private readonly SpawnWeatherHistory _weather;

    public DefaultAreaConditionsEvaluator(IGameStateReader game, SpawnWeatherHistory weather)
    {
        _game = game;
        _weather = weather;
    }

    public bool AreMet(ExtraConditionsJson conditions)
    {
        if (!string.IsNullOrWhiteSpace(conditions.GameStateQuery) && !GameStateQuery.CheckConditions(conditions.GameStateQuery))
            return false;

        if (_game.GetYear() < conditions.StartYear)
            return false;

        if (conditions.Seasons.Count > 0 && !conditions.Seasons.Contains(_game.GetSeason(), StringComparer.OrdinalIgnoreCase))
            return false;

        if (conditions.Days.Count > 0 && !conditions.Days.Contains(_game.GetDayOfMonth()))
            return false;

        if (conditions.SpawnTimeHour is >= 0 and <= 23)
        {
            var currentHour = _game.GetTimeOfDay() / 100;
            if (currentHour != conditions.SpawnTimeHour)
                return false;
        }

        var w = _weather.GetSnapshot();

        if (conditions.WeatherToday.Count > 0 && !conditions.WeatherToday.Any(x => string.Equals(x, w.WeatherToday, StringComparison.OrdinalIgnoreCase)))
            return false;

        if (conditions.WeatherTomorrow.Count > 0 && !conditions.WeatherTomorrow.Any(x => string.Equals(x, w.WeatherTomorrow, StringComparison.OrdinalIgnoreCase)))
            return false;

        if (conditions.WeatherYesterday.Count > 0 && !conditions.WeatherYesterday.Any(x => string.Equals(x, w.WeatherYesterday, StringComparison.OrdinalIgnoreCase)))
            return false;

        return true;
    }
}
