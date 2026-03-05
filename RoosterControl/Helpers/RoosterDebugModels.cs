using System.Collections.Generic;

namespace RoosterControl.Helpers;

#if RCTRL_DEV
internal sealed record DebugProduceDump(
    string RoosterType,
    string FarmAnimalName,
    ulong FarmId,
    int Day,
    string LocationName,
    string BuildingType,
    int? SeedOverride,
    string ReproHash,
    int ProduceSeed,
    int DeluxeSeed,
    DebugProduceSlotDump ChosenProduce,
    DebugProduceSlotDump ChosenDeluxe,
    List<DebugProduceSlotDump>? ProduceCandidates,
    List<DebugProduceSlotDump>? DeluxeCandidates,
    List<DebugWeightAnomaly>? WeightAnomalies,
    DebugProduceCacheStats? CacheStats
);

internal sealed record DebugProduceCacheStats(
    int ResolveCacheEntries,
    int ResolveCacheCapacity,
    long ResolveCacheHits,
    long ResolveCacheMisses,
    long ResolveCacheEvictions,
    int TraceCacheEntries,
    int TraceCacheCapacity,
    long TraceCacheHits,
    long TraceCacheMisses,
    long TraceCacheEvictions
);

internal sealed record DebugWeightAnomaly(
    string Channel,
    int SlotOrdinal,
    string Slot,
    int RawWeight,
    int SanitizedWeight,
    string Reason
);

internal sealed record DebugProduceSlotDump(
    int SlotOrdinal,
    string Slot,
    string ChoiceKey,
    string ResolvedId,
    int Weight,
    int BaseSeed,
    IReadOnlyList<DebugResolutionStep>? TraceSteps,
    bool TraceCacheHit,
    bool CycleDetected,
    int RawWeight
);

internal sealed record DebugResolutionStep(
    int Depth,
    string GroupKey,
    int PickSeed,
    int Index,
    string Picked
);
#endif
