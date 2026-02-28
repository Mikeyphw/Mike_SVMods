using System;
using RoosterControl.Enums;

namespace RoosterControl.Application.Planning;

/// <summary>
/// Central seed-derivation helpers for deterministic rooster produce/hatching behavior.
/// </summary>
internal static class RoosterSeedKey
{
    public readonly record struct Context(
        ulong FarmId,
        int Day,
        string LocationName,
        string BuildingType,
        string FarmAnimalName,
        RoosterType RoosterType
    );

    public static int ForChosenProduce(in Context ctx, ProduceTypeEntries produceType)
        => FoldToSeed(StableHash.Fnv1a64(
            $"rc|chosen-produce|{ctx.FarmId}|{ctx.Day}|{ctx.LocationName}|{ctx.BuildingType}|{ctx.FarmAnimalName}|{ctx.RoosterType}|{produceType}"
        ));

    public static int ForChoiceResolution(in Context ctx, string channel, int slotOrdinal)
    {
        channel ??= string.Empty;
        return FoldToSeed(StableHash.Fnv1a64(
            $"rc|resolve-choice|{ctx.FarmId}|{ctx.Day}|{ctx.LocationName}|{ctx.BuildingType}|{ctx.FarmAnimalName}|{ctx.RoosterType}|{channel}|{slotOrdinal}"
        ));
    }

    public static int ForRandomGroupPick(int baseSeed, string groupKey, int depth)
    {
        groupKey ??= string.Empty;
        return FoldToSeed(StableHash.Fnv1a64($"rc|group-pick|{baseSeed}|{depth}|{groupKey}"));
    }

    private static int FoldToSeed(ulong hash)
        => unchecked((int)(hash ^ (hash >> 32)));
}
