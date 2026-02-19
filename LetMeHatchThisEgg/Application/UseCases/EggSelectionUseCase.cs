using System;
using System.Collections.Generic;
using LetMeHatchThisEgg.Core.Breeding;
using Mikew.BirdsOfAFeather.Framework.Random;

namespace LetMeHatchThisEgg.Application.UseCases;

/// <summary>
/// Application-level façade for egg-selection decisions (chance rolls + weighted choice).
/// </summary>
public sealed class EggSelectionUseCase
{
    private readonly EggSelectionService _selector;

    public EggSelectionUseCase(IRandomProvider rng)
    {
        if (rng is null) throw new ArgumentNullException(nameof(rng));
        _selector = new EggSelectionService(rng);
    }

    public bool RollChance(double chance) => _selector.RollChance(chance);

    public bool RollChanceInclusive(double chance) => _selector.RollChanceInclusive(chance);

    public EggRecipient ChooseRecipient(double chanceForMale) => _selector.ChooseRecipient(chanceForMale);

    public T RollWeightedOrDefault<T>(IEnumerable<(T item, double weight)> entries, T fallback)
        => _selector.RollWeightedOrDefault(entries, fallback);
}
