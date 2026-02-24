using System;
using System.Collections.Generic;
using RoosterControl.Application.Catalogs;
using RoosterControl.Constants;

namespace RoosterControl.Application.Produces;

/// <summary>Resolves produce/egg choice keys into concrete item IDs, supporting catalog-driven random groups.</summary>
public interface IProduceResolver
{
    /// <summary>Resolve an item choice key (or random group key) into an item ID.</summary>
    string Resolve(string choiceKey);
}

/// <summary>
/// Random group -> choice resolver. Random groups are provided by <see cref="ProduceCatalog.RandomGroups"/>.
/// </summary>
public sealed class RandomGroupProduceResolver : IProduceResolver
{
    private readonly IDictionary<string, List<string>> _groups;
    private readonly Random _random;

    public RandomGroupProduceResolver(ProduceCatalog? produceCatalog, Random random)
    {
        _random = random ?? throw new ArgumentNullException(nameof(random));
        _groups =
            (produceCatalog?.RandomGroups as IDictionary<string, List<string>>)
            ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
    }

    public string Resolve(string choiceKey)
    {
        if (string.IsNullOrWhiteSpace(choiceKey))
            return string.Empty;

        return ResolveInner(choiceKey.Trim(), new HashSet<string>(StringComparer.OrdinalIgnoreCase));
    }

    private string ResolveInner(string key, HashSet<string> seen)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;

        if (!seen.Add(key))
            return Translate(key);

        if (_groups.TryGetValue(key, out var options) && options is { Count: > 0 })
        {
            var picked = options[_random.Next(options.Count)];
            return ResolveInner(picked?.Trim() ?? string.Empty, seen);
        }

        return Translate(key);
    }

    private static string Translate(string key)
        => ItemIds.VariableValues.TryGetValue(key, out var translated) ? translated : key;
}
