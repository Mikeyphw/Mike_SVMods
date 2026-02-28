using System;
using System.Collections.Generic;

namespace Mikew.NestCore.Helpers.Config;

/// <summary>
/// Helpers for the "default + overrides" config pattern:
/// store only values that differ from a default; remove keys when they match defaults.
/// </summary>
public static class OverrideDictionaries
{
    /// <summary>
    /// Get an overridden value if present; otherwise returns <paramref name="defaultValue"/>.
    /// </summary>
    public static TValue GetValue<TKey, TValue>(
        IReadOnlyDictionary<TKey, TValue>? overrides,
        TKey key,
        TValue defaultValue)
        where TKey : notnull
    {
        if (overrides is not null && overrides.TryGetValue(key, out var v))
            return v;

        return defaultValue;
    }

    /// <summary>
    /// Set an override value, or remove the key if it matches <paramref name="defaultValue"/>.
    /// </summary>
    public static void SetOverride<TKey, TValue>(
        IDictionary<TKey, TValue> overrides,
        TKey key,
        TValue value,
        TValue defaultValue,
        IEqualityComparer<TValue>? comparer = null)
        where TKey : notnull
    {
        if (overrides is null)
            throw new ArgumentNullException(nameof(overrides));

        comparer ??= EqualityComparer<TValue>.Default;

        if (comparer.Equals(value, defaultValue))
            overrides.Remove(key);
        else
            overrides[key] = value;
    }

    /// <summary>
    /// Remove any entries whose value matches <paramref name="defaultValue"/>.
    /// </summary>
    public static void Normalize<TKey, TValue>(
        IDictionary<TKey, TValue> overrides,
        TValue defaultValue,
        IEqualityComparer<TValue>? comparer = null)
        where TKey : notnull
    {
        if (overrides is null)
            throw new ArgumentNullException(nameof(overrides));

        comparer ??= EqualityComparer<TValue>.Default;

        var toRemove = new List<TKey>();
        foreach (var (k, v) in overrides)
        {
            if (comparer.Equals(v, defaultValue))
                toRemove.Add(k);
        }

        foreach (var k in toRemove)
            overrides.Remove(k);
    }
}
