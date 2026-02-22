using System;
using System.Reflection;
using StardewValley;

namespace WildEggsHatching.Infrastructure.Game;

/// <summary>
/// Normalizes SDV weather into stable string keys used by this mod's legacy weather constraints.
/// </summary>
public static class WeatherKeyResolver
{
    public static string GetWeatherKeyToday()
    {
        if (TryGetStaticBoolFieldOrProperty(typeof(Game1), "isGreenRain", out var isGreenRain) && isGreenRain)
            return "GreenRain";

        if (Game1.isSnowing)
            return "Snow";

        if (Game1.isLightning)
            return "Storm";

        if (Game1.isRaining)
            return "Rain";

        if (Game1.isDebrisWeather)
            return "Wind";

        return "Sun";
    }

    public static string GetWeatherKeyTomorrow()
    {
        var value = TryGetStaticFieldOrProperty(typeof(Game1), "weatherForTomorrow");
        if (value is null)
            return "";

        if (value is string s)
            return s;

        if (value is int i)
            return MapLegacyWeatherInt(i);

        if (value is long l)
            return MapLegacyWeatherInt((int)l);

        return value.ToString() ?? "";
    }

    private static string MapLegacyWeatherInt(int code)
        => code switch
        {
            0 => "Sun",
            1 => "Rain",
            2 => "Wind",
            3 => "Storm",
            4 => "Snow",
            _ => code.ToString()
        };

    private static object? TryGetStaticFieldOrProperty(Type type, string name)
    {
        var f = type.GetField(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (f is not null)
            return f.GetValue(null);

        var p = type.GetProperty(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (p is not null)
            return p.GetValue(null);

        return null;
    }

    private static bool TryGetStaticBoolFieldOrProperty(Type type, string name, out bool value)
    {
        value = false;

        var o = TryGetStaticFieldOrProperty(type, name);
        if (o is bool b)
        {
            value = b;
            return true;
        }

        return false;
    }
}
