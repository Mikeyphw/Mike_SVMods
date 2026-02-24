using System;
using Mikew.NestCore.Helpers.Logging;
using Mikew.NestCore.Helpers.SaveData;
using Mikew.NestCore.Models;
using EggsInWild.Infrastructure.Game;
using LogScopedHelper = Mikew.NestCore.Helpers.Logging.LogScopedHelper;
using static Mikew.NestCore.Helpers.Logging.ScopedLogCatalog;

namespace EggsInWild.Application.Spawning;

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

        LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.Spawning, $"SpawnWeatherHistory updated (day={dayNumber} y={data.WeatherYesterday} t={data.WeatherToday} tm={data.WeatherTomorrow})", LogLevel.Trace);
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
            LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.Spawning, $"Failed writing SpawnWeatherHistory. {ex}", LogLevel.Warn);
        }
    }
}
