namespace WildEggsHatching.DesertTomb.Core.Model;

/// <summary>
/// Inclusive min/max capacity constraints.
/// </summary>
public readonly record struct CapacityRange(int Min, int Max);
