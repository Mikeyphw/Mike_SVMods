using System;

namespace EggsInWild.Infrastructure.Config;

/// <summary>
/// Centralized tuning knobs for debugging.
/// In release builds, values are compile-time constants.
/// In debug builds (<c>WEGG_DEV</c>), values come from <see cref="ModConfig.DebugKnobs"/>.
/// </summary>
internal static class DebugKnobs
{
#if WEGG_DEV
    private static Func<ModConfig>? _getConfig;

    internal static void Initialize(Func<ModConfig> getConfig)
        => _getConfig = getConfig ?? throw new ArgumentNullException(nameof(getConfig));

    private static DebugKnobsConfig Cfg => _getConfig?.Invoke().DebugKnobs ?? new DebugKnobsConfig();

    private static double F(Func<DebugKnobsConfig, float> selector, double fallback)
    {
        try { return selector(Cfg); }
        catch { return fallback; }
    }

    private static int I(Func<DebugKnobsConfig, int> selector, int fallback)
    {
        try { return selector(Cfg); }
        catch { return fallback; }
    }

    private static bool B(Func<DebugKnobsConfig, bool> selector, bool fallback)
    {
        try { return selector(Cfg); }
        catch { return fallback; }
    }
#else
    internal static void Initialize(Func<ModConfig> getConfig) { }
#endif

    // -----------------------------
    // Desert tomb: generation
    // -----------------------------

    internal static double TombLightingDarkChance =>
#if WEGG_DEV
        F(c => c.DesertTomb.Generation.LightingDarkChance, 0.3);
#else
        0.3;
#endif

    internal static double TombLightingDarkAltChance =>
#if WEGG_DEV
        F(c => c.DesertTomb.Generation.LightingDarkAltChance, 0.3);
#else
        0.3;
#endif

    internal static double TombLightingSpecialDarkChance =>
#if WEGG_DEV
        F(c => c.DesertTomb.Generation.LightingSpecialDarkChance, 0.15);
#else
        0.15;
#endif

    internal static int TombBreakablesBaseNextMaxExclusive =>
#if WEGG_DEV
        I(c => c.DesertTomb.Generation.BreakablesBaseNextMaxExclusive, 5);
#else
        5;
#endif

    internal static double TombBreakablesLuckScalar =>
#if WEGG_DEV
        F(c => c.DesertTomb.Generation.BreakablesLuckScalar, 20.0);
#else
        20.0;
#endif

    internal static int TombBreakablesMapAreaDivisor =>
#if WEGG_DEV
        I(c => c.DesertTomb.Generation.BreakablesMapAreaDivisor, 40);
#else
        40;
#endif

    internal static double TombBreakablesEdgeTopChance =>
#if WEGG_DEV
        F(c => c.DesertTomb.Generation.BreakablesEdgeTopChance, 0.33);
#else
        0.33;
#endif

    internal static int TombBreakablesPlacementGuardMax =>
#if WEGG_DEV
        I(c => c.DesertTomb.Generation.BreakablesPlacementGuardMax, 512);
#else
        512;
#endif

    internal static double TombBreakablesPlaceChance =>
#if WEGG_DEV
        F(c => c.DesertTomb.Generation.BreakablesPlaceChance, 0.25);
#else
        0.25;
#endif

    internal static double TombBreakablesClusterChance =>
#if WEGG_DEV
        F(c => c.DesertTomb.Generation.BreakablesClusterChance, 0.35);
#else
        0.35;
#endif

    internal static int TombLightsMapAreaDivisor =>
#if WEGG_DEV
        I(c => c.DesertTomb.Generation.LightsMapAreaDivisor, 220);
#else
        220;
#endif

    internal static int TombLightsTargetMin =>
#if WEGG_DEV
        I(c => c.DesertTomb.Generation.LightsTargetMin, 10);
#else
        10;
#endif

    internal static int TombLightsTargetMax =>
#if WEGG_DEV
        I(c => c.DesertTomb.Generation.LightsTargetMax, 40);
#else
        40;
#endif

    internal static int TombLightsAttemptsMultiplier =>
#if WEGG_DEV
        I(c => c.DesertTomb.Generation.LightsAttemptsMultiplier, 30);
#else
        30;
#endif

    // -----------------------------
    // Desert tomb: spawn planning/execution
    // -----------------------------

    internal static double TombGuardFraction =>
#if WEGG_DEV
        F(c => c.DesertTomb.Spawning.GuardFraction, 0.7);
#else
        0.7;
#endif

    internal static double TombWanderPackChance =>
#if WEGG_DEV
        F(c => c.DesertTomb.Spawning.WanderPackChance, 0.6);
#else
        0.6;
#endif

    internal static int TombLeashRadiusMin =>
#if WEGG_DEV
        I(c => c.DesertTomb.Spawning.LeashRadiusMin, 6);
#else
        6;
#endif

    internal static int TombLeashRadiusMax =>
#if WEGG_DEV
        I(c => c.DesertTomb.Spawning.LeashRadiusMax, 20);
#else
        20;
#endif

    internal static int TombPatrolPointsMinInclusive =>
#if WEGG_DEV
        I(c => c.DesertTomb.Spawning.PatrolPointsMinInclusive, 2);
#else
        2;
#endif

    internal static int TombPatrolPointsMaxExclusive =>
#if WEGG_DEV
        I(c => c.DesertTomb.Spawning.PatrolPointsMaxExclusive, 5);
#else
        5;
#endif

    internal static int TombPatrolMaxAttempts =>
#if WEGG_DEV
        I(c => c.DesertTomb.Spawning.PatrolMaxAttempts, 200);
#else
        200;
#endif

    internal static int TombPatrolAttemptsPerCandidate =>
#if WEGG_DEV
        I(c => c.DesertTomb.Spawning.PatrolAttemptsPerCandidate, 2);
#else
        2;
#endif

    internal static int TombPatrolMinSpacingTiles =>
#if WEGG_DEV
        I(c => c.DesertTomb.Spawning.PatrolMinSpacingTiles, 2);
#else
        2;
#endif

    // -----------------------------
    // Desert tomb: population pacing
    // -----------------------------

    internal static double TombPopulationInitialWaveFraction =>
#if WEGG_DEV
        F(c => c.DesertTomb.Population.InitialWaveFraction, 0.75);
#else
        0.75;
#endif

    internal static int TombPopulationReconcileIntervalTicks =>
#if WEGG_DEV
        I(c => c.DesertTomb.Population.ReconcileIntervalTicks, 30);
#else
        30;
#endif

    internal static int TombPopulationMaxSpawnsPerTick =>
#if WEGG_DEV
        I(c => c.DesertTomb.Population.MaxSpawnsPerTick, 6);
#else
        6;
#endif

    internal static int TombPopulationMinDistanceToPlayerTiles =>
#if WEGG_DEV
        I(c => c.DesertTomb.Population.MinDistanceToPlayerTiles, 5);
#else
        5;
#endif

    internal static bool TombPopulationAllowRespawnAfterInitialFill =>
#if WEGG_DEV
        B(c => c.DesertTomb.Population.AllowRespawnAfterInitialFill, false);
#else
        false;
#endif

    // -----------------------------
    // Desert tomb: loot/luck
    // -----------------------------

    internal static double TombLuckDeltaUnlucky =>
#if WEGG_DEV
        F(c => c.DesertTomb.LootLuck.UnluckyDelta, -0.001);
#else
        -0.001;
#endif

    internal static double TombLuckDeltaLucky =>
#if WEGG_DEV
        F(c => c.DesertTomb.LootLuck.LuckyDelta, 0.001);
#else
        0.001;
#endif

    internal static double TombLuckDeltaSuperLucky =>
#if WEGG_DEV
        F(c => c.DesertTomb.LootLuck.SuperLuckyDelta, 0.01);
#else
        0.01;
#endif

    internal static double TombLuckSuperUnluckyMax =>
#if WEGG_DEV
        F(c => c.DesertTomb.LootLuck.SuperUnluckyMax, -0.07);
#else
        -0.07;
#endif

    internal static double TombLuckUnluckyMax =>
#if WEGG_DEV
        F(c => c.DesertTomb.LootLuck.UnluckyMax, -0.02);
#else
        -0.02;
#endif

    internal static double TombLuckNeutralMax =>
#if WEGG_DEV
        F(c => c.DesertTomb.LootLuck.NeutralMax, 0.02);
#else
        0.02;
#endif

    internal static double TombLuckLuckyMax =>
#if WEGG_DEV
        F(c => c.DesertTomb.LootLuck.LuckyMax, 0.07);
#else
        0.07;
#endif

    // -----------------------------
    // Desert tomb: breakables patch
    // -----------------------------

    internal static double TombBreakableDropRateMultiplierMax =>
#if WEGG_DEV
        F(c => c.DesertTomb.Breakables.DropRateMultiplierMax, 10.0);
#else
        10.0;
#endif

    // -----------------------------
    // Egg spawn chance modifiers
    // -----------------------------

    internal static double SpawnChanceModLargeMagic =>
#if WEGG_DEV
        F(c => c.SpawningChanceModifiers.LargeMagic, -0.13);
#else
        -0.13;
#endif

    internal static double SpawnChanceModLargeWild =>
#if WEGG_DEV
        F(c => c.SpawningChanceModifiers.LargeWild, -0.05);
#else
        -0.05;
#endif

    internal static double SpawnChanceModMagic =>
#if WEGG_DEV
        F(c => c.SpawningChanceModifiers.Magic, -0.10);
#else
        -0.10;
#endif

    // -----------------------------
    // Economy weighting
    // -----------------------------

    internal static int WeightHorizonDays =>
#if WEGG_DEV
        I(c => c.Weighting.HorizonDays, 7);
#else
        7;
#endif

    internal static double WeightDeluxeProbability =>
#if WEGG_DEV
        F(c => c.Weighting.DeluxeProbability, 0.1);
#else
        0.1;
#endif

    internal static double WeightK =>
#if WEGG_DEV
        F(c => c.Weighting.K, 1000.0);
#else
        1000.0;
#endif

    internal static double WeightAlpha =>
#if WEGG_DEV
        F(c => c.Weighting.Alpha, 0.5);
#else
        0.5;
#endif

    internal static double WeightCapHigh =>
#if WEGG_DEV
        F(c => c.Weighting.CapHigh, 8.0);
#else
        8.0;
#endif

    internal static double WeightCapLow =>
#if WEGG_DEV
        F(c => c.Weighting.CapLow, 20.0);
#else
        20.0;
#endif

    internal static double WeightUnlockedFactor =>
#if WEGG_DEV
        F(c => c.Weighting.UnlockedFactor, 0.85);
#else
        0.85;
#endif

    internal static double WeightHiddenNoAnchorPenalty =>
#if WEGG_DEV
        F(c => c.Weighting.HiddenNoAnchorPenalty, 0.5);
#else
        0.5;
#endif

    // -----------------------------
    // Monsters
    // -----------------------------

    internal static int PharaohVisionTiles =>
#if WEGG_DEV
        I(c => c.Monsters.Pharaoh.VisionTiles, 8);
#else
        8;
#endif

    internal static int PharaohCastRangeTiles =>
#if WEGG_DEV
        I(c => c.Monsters.Pharaoh.CastRangeTiles, 8);
#else
        8;
#endif

    internal static int PharaohRewrapRangeTiles =>
#if WEGG_DEV
        I(c => c.Monsters.Pharaoh.RewrapRangeTiles, 6);
#else
        6;
#endif

    internal static int PharaohBaseCastCooldownMs =>
#if WEGG_DEV
        I(c => c.Monsters.Pharaoh.BaseCastCooldownMs, 2600);
#else
        2600;
#endif

    internal static int PharaohEnragedCastCooldownMs =>
#if WEGG_DEV
        I(c => c.Monsters.Pharaoh.EnragedCastCooldownMs, 1700);
#else
        1700;
#endif

    internal static int PharaohCastWindupMs =>
#if WEGG_DEV
        I(c => c.Monsters.Pharaoh.CastWindupMs, 800);
#else
        800;
#endif

    internal static int PharaohRecoverMs =>
#if WEGG_DEV
        I(c => c.Monsters.Pharaoh.RecoverMs, 450);
#else
        450;
#endif

    internal static int PharaohMicroUnstuckAfterTicks =>
#if WEGG_DEV
        I(c => c.Monsters.Pharaoh.MicroUnstuckAfterTicks, 15);
#else
        15;
#endif

    internal static int PharaohPathfindUnstuckAfterTicks =>
#if WEGG_DEV
        I(c => c.Monsters.Pharaoh.PathfindUnstuckAfterTicks, 35);
#else
        35;
#endif

    internal static int PharaohSandStepAfterTicks =>
#if WEGG_DEV
        I(c => c.Monsters.Pharaoh.SandStepAfterTicks, 65);
#else
        65;
#endif

    internal static int PharaohSandStepCooldownMs =>
#if WEGG_DEV
        I(c => c.Monsters.Pharaoh.SandStepCooldownMs, 6500);
#else
        6500;
#endif

    internal static int PharaohLeashRadiusTiles =>
#if WEGG_DEV
        I(c => c.Monsters.Pharaoh.LeashRadiusTiles, 12);
#else
        12;
#endif

    internal static int PharaohLeashTicksBeforeReturn =>
#if WEGG_DEV
        I(c => c.Monsters.Pharaoh.LeashTicksBeforeReturn, 180);
#else
        180;
#endif

    internal static double PharaohRewrapChance =>
#if WEGG_DEV
        F(c => c.Monsters.Pharaoh.RewrapChance, 0.45);
#else
        0.45;
#endif

    internal static int PharaohEnrageCooldownCapMs =>
#if WEGG_DEV
        I(c => c.Monsters.Pharaoh.EnrageCooldownCapMs, 800);
#else
        800;
#endif

    internal static int PharaohReviveTimerMax =>
#if WEGG_DEV
        I(c => c.Monsters.Pharaoh.ReviveTimerMax, 600);
#else
        600;
#endif

    internal static int PharaohHealEnraged =>
#if WEGG_DEV
        I(c => c.Monsters.Pharaoh.HealEnraged, 180);
#else
        180;
#endif

    internal static int PharaohHealNormal =>
#if WEGG_DEV
        I(c => c.Monsters.Pharaoh.HealNormal, 80);
#else
        80;
#endif

    internal static double SandiDiveChanceOnHit =>
#if WEGG_DEV
        F(c => c.Monsters.Sandi.DiveChanceOnHit, 0.18);
#else
        0.18;
#endif

    internal static int SandiDiveDurationMs =>
#if WEGG_DEV
        I(c => c.Monsters.Sandi.DiveDurationMs, 2200);
#else
        2200;
#endif

    internal static int SandiDiveCooldownMs =>
#if WEGG_DEV
        I(c => c.Monsters.Sandi.DiveCooldownMs, 4200);
#else
        4200;
#endif

    internal static int CrawlerCurlTriggerDistanceTiles =>
#if WEGG_DEV
        I(c => c.Monsters.Crawler.CurlTriggerDistanceTiles, 3);
#else
        3;
#endif

    internal static int CrawlerCurlDurationMs =>
#if WEGG_DEV
        I(c => c.Monsters.Crawler.CurlDurationMs, 1800);
#else
        1800;
#endif

    internal static int CrawlerCurlCooldownMs =>
#if WEGG_DEV
        I(c => c.Monsters.Crawler.CurlCooldownMs, 3500);
#else
        3500;
#endif

    internal static int HurricaneAlignTiles =>
#if WEGG_DEV
        I(c => c.Monsters.Hurricane.AlignTiles, 10);
#else
        10;
#endif

    internal static int HurricaneSuckDistanceTiles =>
#if WEGG_DEV
        I(c => c.Monsters.Hurricane.SuckDistanceTiles, 2);
#else
        2;
#endif

    internal static int HurricaneSwirlSegmentMs =>
#if WEGG_DEV
        I(c => c.Monsters.Hurricane.SwirlSegmentMs, 600);
#else
        600;
#endif

    internal static int HurricaneChargeMs =>
#if WEGG_DEV
        I(c => c.Monsters.Hurricane.ChargeMs, 900);
#else
        900;
#endif

    internal static int HurricaneSuckMs =>
#if WEGG_DEV
        I(c => c.Monsters.Hurricane.SuckMs, 1600);
#else
        1600;
#endif

    internal static int MatangoVisionTiles =>
#if WEGG_DEV
        I(c => c.Monsters.Matango.VisionTiles, 8);
#else
        8;
#endif

    internal static int MatangoCastWindupMs =>
#if WEGG_DEV
        I(c => c.Monsters.Matango.CastWindupMs, 450);
#else
        450;
#endif

    internal static int MatangoRecoverMs =>
#if WEGG_DEV
        I(c => c.Monsters.Matango.RecoverMs, 250);
#else
        250;
#endif

    internal static int MatangoShotCooldownMs =>
#if WEGG_DEV
        I(c => c.Monsters.Matango.ShotCooldownMs, 2200);
#else
        2200;
#endif
}

#if WEGG_DEV
public sealed class DebugKnobsConfig
{
    public DesertTombKnobs DesertTomb { get; set; } = new();
    public SpawningChanceModifiersKnobs SpawningChanceModifiers { get; set; } = new();
    public WeightingKnobs Weighting { get; set; } = new();
    public MonstersKnobs Monsters { get; set; } = new();
}

public sealed class DesertTombKnobs
{
    public DesertTombGenerationKnobs Generation { get; set; } = new();
    public DesertTombSpawningKnobs Spawning { get; set; } = new();
    public DesertTombPopulationKnobs Population { get; set; } = new();
    public DesertTombLootLuckKnobs LootLuck { get; set; } = new();
    public DesertTombBreakablesKnobs Breakables { get; set; } = new();
}

public sealed class DesertTombGenerationKnobs
{
    public float LightingDarkChance { get; set; } = 0.3f;
    public float LightingDarkAltChance { get; set; } = 0.3f;
    public float LightingSpecialDarkChance { get; set; } = 0.15f;

    public int BreakablesBaseNextMaxExclusive { get; set; } = 5;
    public float BreakablesLuckScalar { get; set; } = 20.0f;
    public int BreakablesMapAreaDivisor { get; set; } = 40;

    public float BreakablesEdgeTopChance { get; set; } = 0.33f;
    public int BreakablesPlacementGuardMax { get; set; } = 512;
    public float BreakablesPlaceChance { get; set; } = 0.25f;
    public float BreakablesClusterChance { get; set; } = 0.35f;

    public int LightsMapAreaDivisor { get; set; } = 220;
    public int LightsTargetMin { get; set; } = 10;
    public int LightsTargetMax { get; set; } = 40;
    public int LightsAttemptsMultiplier { get; set; } = 30;
}

public sealed class DesertTombSpawningKnobs
{
    public float GuardFraction { get; set; } = 0.7f;
    public float WanderPackChance { get; set; } = 0.6f;

    public int LeashRadiusMin { get; set; } = 6;
    public int LeashRadiusMax { get; set; } = 20;

    public int PatrolPointsMinInclusive { get; set; } = 2;
    public int PatrolPointsMaxExclusive { get; set; } = 5;

    public int PatrolMaxAttempts { get; set; } = 200;
    public int PatrolAttemptsPerCandidate { get; set; } = 2;
    public int PatrolMinSpacingTiles { get; set; } = 2;
}

public sealed class DesertTombPopulationKnobs
{
    public float InitialWaveFraction { get; set; } = 0.75f;
    public int ReconcileIntervalTicks { get; set; } = 30;
    public int MaxSpawnsPerTick { get; set; } = 6;
    public int MinDistanceToPlayerTiles { get; set; } = 5;
    public bool AllowRespawnAfterInitialFill { get; set; } = false;
}

public sealed class DesertTombLootLuckKnobs
{
    public float UnluckyDelta { get; set; } = -0.001f;
    public float LuckyDelta { get; set; } = 0.001f;
    public float SuperLuckyDelta { get; set; } = 0.01f;

    public float SuperUnluckyMax { get; set; } = -0.07f;
    public float UnluckyMax { get; set; } = -0.02f;
    public float NeutralMax { get; set; } = 0.02f;
    public float LuckyMax { get; set; } = 0.07f;
}

public sealed class DesertTombBreakablesKnobs
{
    public float DropRateMultiplierMax { get; set; } = 10.0f;
}

public sealed class SpawningChanceModifiersKnobs
{
    public float LargeMagic { get; set; } = -0.13f;
    public float LargeWild { get; set; } = -0.05f;
    public float Magic { get; set; } = -0.10f;
}

public sealed class WeightingKnobs
{
    public int HorizonDays { get; set; } = 7;
    public float DeluxeProbability { get; set; } = 0.1f;

    public float K { get; set; } = 1000.0f;
    public float Alpha { get; set; } = 0.5f;

    public float CapHigh { get; set; } = 8.0f;
    public float CapLow { get; set; } = 20.0f;

    public float UnlockedFactor { get; set; } = 0.85f;
    public float HiddenNoAnchorPenalty { get; set; } = 0.5f;
}

public sealed class MonstersKnobs
{
    public PharaohKnobs Pharaoh { get; set; } = new();
    public SandiKnobs Sandi { get; set; } = new();
    public CrawlerKnobs Crawler { get; set; } = new();
    public HurricaneKnobs Hurricane { get; set; } = new();
    public MatangoKnobs Matango { get; set; } = new();
}

public sealed class PharaohKnobs
{
    public int VisionTiles { get; set; } = 8;
    public int CastRangeTiles { get; set; } = 8;
    public int RewrapRangeTiles { get; set; } = 6;

    public int BaseCastCooldownMs { get; set; } = 2600;
    public int EnragedCastCooldownMs { get; set; } = 1700;
    public int CastWindupMs { get; set; } = 800;
    public int RecoverMs { get; set; } = 450;

    public int MicroUnstuckAfterTicks { get; set; } = 15;
    public int PathfindUnstuckAfterTicks { get; set; } = 35;

    public int SandStepAfterTicks { get; set; } = 65;
    public int SandStepCooldownMs { get; set; } = 6500;

    public int LeashRadiusTiles { get; set; } = 12;
    public int LeashTicksBeforeReturn { get; set; } = 180;

    public float RewrapChance { get; set; } = 0.45f;
    public int EnrageCooldownCapMs { get; set; } = 800;

    public int ReviveTimerMax { get; set; } = 600;
    public int HealEnraged { get; set; } = 180;
    public int HealNormal { get; set; } = 80;
}

public sealed class SandiKnobs
{
    public float DiveChanceOnHit { get; set; } = 0.18f;
    public int DiveDurationMs { get; set; } = 2200;
    public int DiveCooldownMs { get; set; } = 4200;
}

public sealed class CrawlerKnobs
{
    public int CurlTriggerDistanceTiles { get; set; } = 3;
    public int CurlDurationMs { get; set; } = 1800;
    public int CurlCooldownMs { get; set; } = 3500;
}

public sealed class HurricaneKnobs
{
    public int AlignTiles { get; set; } = 10;
    public int SuckDistanceTiles { get; set; } = 2;

    public int SwirlSegmentMs { get; set; } = 600;
    public int ChargeMs { get; set; } = 900;
    public int SuckMs { get; set; } = 1600;
}

public sealed class MatangoKnobs
{
    public int VisionTiles { get; set; } = 8;
    public int CastWindupMs { get; set; } = 450;
    public int RecoverMs { get; set; } = 250;
    public int ShotCooldownMs { get; set; } = 2200;
}
#endif
