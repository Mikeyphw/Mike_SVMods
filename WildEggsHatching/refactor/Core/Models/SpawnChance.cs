using System;
using StardewValley;
using WildEggsHatching.Core.Enums;

namespace WildEggsHatching.Core.Models;

/// <summary>
/// Represents a spawn chance value with built-in validation.
/// Ensures spawn chance is always clamped to valid range [0, 1].
/// This is a value object - it should be treated as immutable.
/// </summary>
public readonly struct SpawnChance : IEquatable<SpawnChance>
{
    /// <summary>The raw spawn chance value (always clamped to 0-1).</summary>
    public float Value { get; }

    /// <summary>
    /// Creates a new spawn chance from a raw value.
    /// The value will be clamped to the valid range [0, 1].
    /// </summary>
    /// <param name="value">The raw spawn chance value.</param>
    public SpawnChance(float value)
    {
        Value = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Creates a spawn chance from a percentage (0-100).
    /// </summary>
    /// <param name="percentage">The percentage value (0-100).</param>
    /// <returns>A SpawnChance with the equivalent decimal value.</returns>
    public static SpawnChance FromPercentage(float percentage)
    {
        return new SpawnChance(percentage / 100f);
    }

    /// <summary>
    /// Rolls against this spawn chance using the provided random generator.
    /// </summary>
    /// <param name="random">The random number generator to use.</param>
    /// <returns>True if the roll succeeds (spawn should occur).</returns>
    public bool Roll(Random random)
    {
        return random.NextDouble() < Value;
    }

    /// <summary>
    /// Rolls against this spawn chance using the game-wide random.
    /// </summary>
    /// <returns>True if the roll succeeds (spawn should occur).</returns>
    public bool Roll()
    {
        return Game1.random.NextDouble() < Value;
    }

    /// <summary>
    /// Combines two spawn chances using AND logic.
    /// The resulting chance is the product of both chances.
    /// </summary>
    /// <param name="other">Another spawn chance to combine with.</param>
    /// <returns>A new SpawnChance representing the combined probability.</returns>
    public SpawnChance And(SpawnChance other)
    {
        return new SpawnChance(Value * other.Value);
    }

    /// <summary>
    /// Combines two spawn chances using OR logic.
    /// The resulting chance is: chanceA + chanceB - (chanceA * chanceB)
    /// </summary>
    /// <param name="other">Another spawn chance to combine with.</param>
    /// <returns>A new SpawnChance representing the combined probability.</returns>
    public SpawnChance Or(SpawnChance other)
    {
        return new SpawnChance(Value + other.Value - (Value * other.Value));
    }

    /// <summary>
    /// Inverts this spawn chance.
    /// </summary>
    /// <returns>A new SpawnChance representing the inverse probability.</returns>
    public SpawnChance Not()
    {
        return new SpawnChance(1f - Value);
    }

    /// <summary>
    /// Applies a multiplier to this spawn chance.
    /// </summary>
    /// <param name="multiplier">The multiplier to apply (e.g., 0.5 for 50%, 1.5 for 150%).</param>
    /// <returns>A new SpawnChance with the multiplier applied.</returns>
    public SpawnChance MultiplyBy(float multiplier)
    {
        return new SpawnChance(Value * multiplier);
    }

    /// <summary>
    /// Adds an offset to this spawn chance.
    /// The result will still be clamped to [0, 1].
    /// </summary>
    /// <param name="offset">The offset to add (can be negative).</param>
    /// <returns>A new SpawnChance with the offset applied.</returns>
    public SpawnChance AddOffset(float offset)
    {
        return new SpawnChance(Value + offset);
    }

    /// <summary>
    /// Returns a string representation of this spawn chance.
    /// </summary>
    public override string ToString()
    {
        return $"{Value:P1}"; // Shows as percentage with 1 decimal
    }

    /// <summary>
    /// Returns the hash code for this spawn chance.
    /// </summary>
    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    /// <summary>
    /// Determines whether this spawn chance is equal to another.
    /// </summary>
    public bool Equals(SpawnChance other)
    {
        return Math.Abs(Value - other.Value) < 0.0001f;
    }

    /// <summary>
    /// Determines whether this spawn chance is equal to another object.
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj is SpawnChance other && Equals(other);
    }

    /// <summary>
    /// Equality operator for SpawnChance.
    /// </summary>
    public static bool operator ==(SpawnChance left, SpawnChance right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Inequality operator for SpawnChance.
    /// </summary>
    public static bool operator !=(SpawnChance left, SpawnChance right)
    {
        return !(left == right);
    }
}

