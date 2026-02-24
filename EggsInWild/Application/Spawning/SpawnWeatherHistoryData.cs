namespace EggsInWild.Application.Spawning;

public sealed class SpawnWeatherHistoryData
{
    public int LastUpdatedDayNumber { get; set; } = -1;

    public string WeatherYesterday { get; set; } = "";
    public string WeatherToday { get; set; } = "";
    public string WeatherTomorrow { get; set; } = "";
}

public readonly record struct SpawnWeatherSnapshot(
    string WeatherYesterday,
    string WeatherToday,
    string WeatherTomorrow);
