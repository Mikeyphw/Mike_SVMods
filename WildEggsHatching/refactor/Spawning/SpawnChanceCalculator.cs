using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Core.Models;
using WildEggsHatching.EnumsAndStrings;

namespace WildEggsHatching.Spawning;

/// <summary>
/// A function that modifies a spawn chance.
/// Takes the current chance and context, returns the modified chance.
/// </summary>
/// <param name="currentChance">The current spawn chance.</param>
/// <param name="context">The spawn context containing all relevant information.</param>
/// <returns>The modified spawn chance.</returns>
public delegate float SpawnChanceModifier(float currentChance, SpawnContext context);

/// <summary>
/// Calculates spawn chance using a simplified 3-layer modifier system.
/// Modifiers are applied as a composable list instead of scattered if statements.
/// </summary>
public sealed class SpawnChanceCalculator
{
    /// <summary>The base chance for egg spawning (default: 0.15 = 15%).</summary>
    private readonly SpawnChance _baseChance;

    /// <summary>The list of modifiers to apply in order.</summary>
    private readonly List<SpawnChanceModifier> _modifiers;

    /// <summary>
    /// Creates a new spawn chance calculator with default base chance.
    /// </summary>
    /// <param name="baseChance">The base spawn chance (0.0 to 1.0). Defaults to 0.15.</param>
    public SpawnChanceCalculator(float baseChance = 0.15f)
    {
        _baseChance = new SpawnChance(baseChance);
        _modifiers = new List<SpawnChanceModifier>();

        // Register default modifiers
        RegisterDefaultModifiers();
    }

    /// <summary>
    /// Creates a new spawn chance calculator with custom base chance and modifiers.
    /// </summary>
    /// <param name="baseChance">The base spawn chance.</param>
    /// <param name="modifiers">The list of modifiers to apply.</param>
    public SpawnChanceCalculator(SpawnChance baseChance, List<SpawnChanceModifier> modifiers)
    {
        _baseChance = baseChance;
        _modifiers = modifiers ?? new List<SpawnChanceModifier>();
    }

    /// <summary>
    /// Registers the default set of modifiers.
    /// </summary>
    private void RegisterDefaultModifiers()
    {
        // Egg type modifier - larger and magic eggs are rarer
        AddModifier(EggTypeModifier);

        // Daily luck influence - small, cosmetic effect
        AddModifier(DailyLuckModifier);
    }

    /// <summary>
    /// Adds a modifier to the list.
    /// </summary>
    /// <param name="modifier">The modifier function to add.</param>
    public void AddModifier(SpawnChanceModifier modifier)
    {
        _modifiers.Add(modifier ?? throw new ArgumentNullException(nameof(modifier)));
    }

    /// <summary>
    /// Removes a modifier from the list.
    /// </summary>
    /// <param name="modifier">The modifier function to remove.</param>
    /// <returns>True if the modifier was found and removed.</returns>
    public bool RemoveModifier(SpawnChanceModifier modifier)
    {
        return _modifiers.Remove(modifier);
    }

    /// <summary>
    /// Clears all modifiers, leaving only the base chance.
    /// </summary>
    public void ClearModifiers()
    {
        _modifiers.Clear();
    }

    /// <summary>
    /// Gets the number of registered modifiers.
    /// </summary>
    public int ModifierCount => _modifiers.Count;

    /// <summary>
    /// Calculates the final spawn chance by applying all modifiers.
    /// </summary>
    /// <param name="eggType">The type of egg being spawned.</param>
    /// <param name="dailyLuck">The current daily luck state.</param>
    /// <returns>The final spawn chance as a SpawnChance value object.</returns>
    public SpawnChance Calculate(EggType eggType, LuckyTypes dailyLuck)
    {
        // Create a context for the modifiers
        var context = new SpawnContext
        {
            EggType = eggType,
            DailyLuck = dailyLuck,
        };

        // Start with base chance as a float
        float currentChance = _baseChance.Value;

        // Apply each modifier in order
        foreach (var modifier in _modifiers)
        {
            currentChance = modifier(currentChance, context);
        }

        // Return as clamped SpawnChance
        return new SpawnChance(currentChance);
    }

    /// <summary>
    /// Calculates the final spawn chance with full context.
    /// </summary>
    /// <param name="context">The full spawn context.</param>
    /// <returns>The final spawn chance.</returns>
    public SpawnChance Calculate(SpawnContext context)
    {
        float currentChance = _baseChance.Value;

        foreach (var modifier in _modifiers)
        {
            currentChance = modifier(currentChance, context);
        }

        return new SpawnChance(currentChance);
    }

    /// <summary>
    /// Modifier: Applies egg type penalty.
    /// Larger and magic eggs are rarer.
    /// </summary>
    private float EggTypeModifier(float chance, SpawnContext context)
    {
        float typeModifier = context.EggType switch
        {
            EggType.largeMagicWildEgg => -0.13f,  // -13% for large magic eggs
            EggType.magicWildEgg => -0.10f,       // -10% for magic eggs
            EggType.largeWildEgg => -0.05f,       // -5% for large eggs
            EggType.wildEgg => 0f,                // No modifier for regular eggs
            _ => 0f
        };

        return chance + typeModifier;
    }

    /// <summary>
    /// Modifier: Applies daily luck influence.
    /// Small, cosmetic effect on spawn chance.
    /// </summary>
    private float DailyLuckModifier(float chance, SpawnContext context)
    {
        float luckInfluence = context.DailyLuck switch
        {
            LuckyTypes.SuperUnlucky => -0.02f,   // Slightly reduce chance
            LuckyTypes.Unlucky => -0.01f,         // Very slight reduction
            LuckyTypes.Lucky => 0.01f,            // Very slight increase
            LuckyTypes.SuperLucky => 0.02f,       // Slightly increase
            LuckyTypes.Neutral => 0f,             // No effect
            _ => 0f
        };

        return chance + luckInfluence;
    }

    /// <summary>
    /// Creates a modifier that applies a percentage multiplier.
    /// </summary>
    /// <param name="multiplier">The multiplier to apply (e.g., 0.5 for 50%, 1.0 for no change).</param>
    /// <returns>A modifier function.</returns>
    public static SpawnChanceModifier PercentageMultiplier(float multiplier)
    {
        return (chance, _) => chance * multiplier;
    }

    /// <summary>
    /// Creates a modifier that adds a flat offset.
    /// </summary>
    /// <param name="offset">The offset to add (can be negative).</param>
    /// <returns>A modifier function.</returns>
    public static SpawnChanceModifier FlatOffset(float offset)
    {
        return (chance, _) => chance + offset;
    }

    /// <summary>
    /// Creates a modifier that only applies if a condition is met.
    /// </summary>
    /// <param name="condition">A function that returns true if the modifier should apply.</param>
    /// <param name="modifier">The modifier to apply if the condition is met.</param>
    /// <returns>A modifier function.</returns>
    public static SpawnChanceModifier Conditional(
        Func<SpawnContext, bool> condition,
        SpawnChanceModifier modifier)
    {
        return (chance, context) => condition(context) ? modifier(chance, context) : chance;
    }

    /// <summary>
    /// Creates a modifier that applies a multiplier only if progression requirement is met.
    /// </summary>
    /// <param name="minimumStep">The minimum progression step required.</param>
    /// <param name="multiplier">The multiplier to apply if requirement is met.</param>
    /// <returns>A modifier function.</returns>
    public static SpawnChanceModifier ProgressionMultiplier(
        ProgressionStep minimumStep,
        float multiplier)
    {
        return (chance, context) =>
            context.CurrentStep >= minimumStep
                ? chance * multiplier
                : chance;
    }
}

