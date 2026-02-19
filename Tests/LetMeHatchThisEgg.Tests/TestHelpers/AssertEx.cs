using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace LetMeHatchThisEgg.Tests.TestHelpers;

/// <summary>
/// Test assertions used to validate "no mutation" guarantees and minimal field mutations.
/// </summary>
public static class AssertEx
{
    public static void AssertNoMutation<TKey, TValue>(
        IDictionary<TKey, TValue> dictionary,
        Func<bool> act)
        where TKey : notnull
    {
        var before = dictionary.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        var beforeCount = dictionary.Count;

        var result = act();

        Assert.False(result);
        Assert.Equal(beforeCount, dictionary.Count);
        Assert.Equal(before.Keys.OrderBy(x => x), dictionary.Keys.OrderBy(x => x));

        foreach (var (key, beforeValue) in before)
        {
            Assert.True(dictionary.TryGetValue(key, out var afterValue));
            AssertSameOrEqual(beforeValue, afterValue);
        }
    }

    public static void AssertSomePropertyMutation(object instance, Action mutate)
    {
        var snapshot = TakeSnapshot(instance);
        mutate();
        var after = TakeSnapshot(instance);

        // Expect at least one of the tracked properties to change.
        Assert.NotEqual(snapshot, after);
    }

    private static Dictionary<string, object?> TakeSnapshot(object instance)
    {
        var props = instance.GetType()
            .GetProperties()
            .Where(p => p.CanRead && p.CanWrite)
            .Where(p =>
                p.PropertyType == typeof(string) ||
                p.PropertyType == typeof(int) ||
                p.PropertyType == typeof(bool) ||
                p.PropertyType.IsEnum)
            .Take(6)
            .ToArray();

        var snapshot = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var p in props)
            snapshot[p.Name] = p.GetValue(instance);

        return snapshot;
    }

    public static void MutateSomeWritableProperties(object instance)
    {
        foreach (var p in instance.GetType().GetProperties().Where(p => p.CanRead && p.CanWrite))
        {
            if (p.PropertyType == typeof(string))
            {
                p.SetValue(instance, "TEST_MUTATED");
                return;
            }

            if (p.PropertyType == typeof(int))
            {
                p.SetValue(instance, 123456);
                return;
            }

            if (p.PropertyType == typeof(bool))
            {
                p.SetValue(instance, true);
                return;
            }

            if (p.PropertyType.IsEnum)
            {
                var values = Enum.GetValues(p.PropertyType);
                if (values.Length > 0)
                {
                    p.SetValue(instance, values.GetValue(0));
                    return;
                }
            }
        }
    }

    private static void AssertSameOrEqual<T>(T expected, T actual)
    {
        if (expected is null || actual is null)
        {
            Assert.Equal(expected, actual);
            return;
        }

        if (!typeof(T).IsValueType)
        {
            Assert.Same(expected, actual);
            return;
        }

        Assert.Equal(expected, actual);
    }
}
