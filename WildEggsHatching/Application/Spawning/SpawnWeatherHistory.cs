using System;
using Mikew.BirdsOfAFeather.Helpers.Logging;
using Mikew.BirdsOfAFeather.Helpers.SaveData;
using Mikew.BirdsOfAFeather.Models;
using WildEggsHatching.Infrastructure.Game;

namespace WildEggsHatching.Application.Spawning;

public sealed class SpawnWeatherHistory
{
    private readonly ISaveDataStore _store;
    private readonly ILog _log;

    public SpawnWeatherHistory(ISaveDataStore store, ILog log)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    public void UpdateForDayStarted(int dayNumber)
    {
        if (dayNumber <= 0)
            return;

        var data = TryRead() ?? new SpawnWeatherHistoryData();

        if (data.LastUpdatedDayNumber == dayNumber)
            return;

        var previousToday = data.WeatherToday;
        data.WeatherYesterday = previousToday;

        // Keep original semantics: "today" becomes "yesterday's predicted tomorrow" when available.
        data.WeatherToday = string.IsNullOrWhiteSpace(data.WeatherTomorrow)
            ? WeatherKeyResolver.GetWeatherKeyToday()
            : data.WeatherTomorrow;

        data.WeatherTomorrow = WeatherKeyResolver.GetWeatherKeyTomorrow();
        data.LastUpdatedDayNumber = dayNumber;

        TryWrite(data);

        _log.Log(
            $"SpawnWeatherHistory updated (day={dayNumber} y={data.WeatherYesterday} t={data.WeatherToday} tm={data.WeatherTomorrow})",
            StardewModdingAPI.LogLevel.Trace);
    }

    public SpawnWeatherSnapshot GetSnapshot()
    {
        var data = TryRead() ?? new SpawnWeatherHistoryData();
        return new SpawnWeatherSnapshot(
            WeatherYesterday: data.WeatherYesterday,
            WeatherToday: data.WeatherToday,
            WeatherTomorrow: data.WeatherTomorrow);
    }

    private SpawnWeatherHistoryData? TryRead()
    {
        try
        {
            var env = _store.ReadEnvelope<SpawnWeatherHistoryData>(SpawnSaveKeys.WeatherHistory);
            return env?.Data;
        }
        catch
        {
            return null;
        }
    }

    private void TryWrite(SpawnWeatherHistoryData data)
    {
        try
        {
            _store.WriteEnvelope(
                SpawnSaveKeys.WeatherHistory,
                new SaveDataEnvelope<SpawnWeatherHistoryData>(SpawnSaveKeys.Version, data));
        }
        catch (Exception ex)
        {
            _log.Log($"Failed writing SpawnWeatherHistory. {ex}", StardewModdingAPI.LogLevel.Warn);
        }
    }
}
