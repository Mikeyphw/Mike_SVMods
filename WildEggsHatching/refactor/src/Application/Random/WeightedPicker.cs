using System;
using Mikew.BirdsOfAFeather.Framework.Random;

namespace WildEggsHatching.Application.Random;

/// <summary>Phase 0: demonstrates shared RNG/weighted behavior via BirdsOfAFeather.</summary>
public sealed class WeightedPicker<T>
{
    private readonly WeightedTable<T> _table;

    public WeightedPicker(IRandomProvider random)
    {
        _table = new WeightedTable<T>(random ?? throw new ArgumentNullException(nameof(random)));
    }

    public void Add(T item, double weight) => _table.Add(item, weight);

    public T Pick() => _table.Next();
}
