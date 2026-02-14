using System;
using System.Collections.Generic;
using System.Linq;

namespace Mikew.BirdsOfAFeather.Helpers;

public static class StringArrayExtensions
{
    public static bool ContainsCI(this string[] array, string? value)
    {
        if (value is null)
        {
            return false;
        }

        return array.Any(s => string.Equals(s, value, StringComparison.OrdinalIgnoreCase));
    }
}

public static class StringExtensions
{
    public static bool IsEmptyOrWhitespace(this string? input)
    {
        return string.IsNullOrWhiteSpace(input);
    }
}

public static class EnumExtensions
{
    /// <summary>
    /// Returns all values of the enum type.
    /// </summary>
    public static IEnumerable<TEnum> GetValues<TEnum>() where TEnum : Enum
    {
        return Enum.GetValues(typeof(TEnum)).Cast<TEnum>();
    }
}

public static class EnumerableExtensions
{
    public static IEnumerable<T> AddIf<T>(this IEnumerable<T> source, T element, Func<T, bool> condition)
    {
        return condition(element) ? source.Concat(new[] { element }) : source;
    }
}

public static class ListExtensions
{
    public static T? GetRandomElementOrDefault<T>(this List<T> list, Random random)
    {
        if (list is null || list.Count == 0)
        {
            return default;
        }

        ArgumentNullException.ThrowIfNull(random);

        return list[random.Next(list.Count)];
    }

    public static bool TryAdd<T>(this List<T> list, T item)
    {
        ArgumentNullException.ThrowIfNull(list);

        if (list.Contains(item))
        {
            return false;
        }

        list.Add(item);
        return true;
    }
}
