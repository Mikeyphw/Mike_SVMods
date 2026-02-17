using System;
using System.Collections.Generic;

namespace RoosterAddon.Application.Planning;

/// <summary>
/// Persisted daily plan for hatching-related FarmAnimals edits.
/// Computed once/day (and per building-key) and reused across content reloads.
/// </summary>
internal sealed class DailyHatchingPlanEnvelope
{
    public int Version { get; set; } = 1;

    public long FarmId { get; set; }

    public int Day { get; set; }

    /// <summary>Changes when config, luck, or female assignments change.</summary>
    public string Fingerprint { get; set; } = string.Empty;

    /// <summary>
    /// Allows explicit recompute mid-day (dev mode). Included in roll seeds.
    /// </summary>
    public long Nonce { get; set; }

    public Dictionary<string, BuildingHatchingPlan> Buildings { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}

internal sealed class BuildingHatchingPlan
{
    public string BuildingKey { get; set; } = string.Empty;

    public Dictionary<string, RoosterHatchingPlan> Roosters { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}

internal sealed class RoosterHatchingPlan
{
    public string RoosterKey { get; set; } = string.Empty;

    public Dictionary<string, FemaleHatchingPlan> Females { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}

internal sealed class FemaleHatchingPlan
{
    public string FemaleKey { get; set; } = string.Empty;

    public EggChancePlan? NormalEgg { get; set; }

    public EggChancePlan? LargeEgg { get; set; }

    public FertilizedOnlyPlan? FertilizedOnly { get; set; }
}

internal sealed class EggChancePlan
{
    public bool Enabled { get; set; }

    public int ChancePercent { get; set; }

    public bool RolledSuccess { get; set; }

    public string EggIdToAddToRooster { get; set; } = string.Empty;

    public string? FemaleEggIdToRemove { get; set; }
}

internal sealed class FertilizedOnlyPlan
{
    public float FemaleUpgradeChancePercent { get; set; }

    public int RoosterToChickenRatioPercent { get; set; }

    public bool UpgradeFemaleProduceToFertilized { get; set; }

    public bool RoosterHatchesFertilizedEgg { get; set; }

    public string RoosterFertilizedEggId { get; set; } = string.Empty;

    public string FemaleFertilizedEggId { get; set; } = string.Empty;
}
