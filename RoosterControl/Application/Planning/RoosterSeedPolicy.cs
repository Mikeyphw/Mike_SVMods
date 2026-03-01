using System;
using RoosterControl.Enums;
using StardewModdingAPI;
using StardewValley;

namespace RoosterControl.Application.Planning;

/// <summary>
/// Centralizes seed inputs and reproducibility identifiers for deterministic produce/hatching behavior.
/// </summary>
internal static class RoosterSeedPolicy
{
    public static (ulong FarmId, int Day, string LocationName) GetWorldSeedInputs()
    {
        if (!Context.IsWorldReady)
            return (0UL, 0, "not-ready");

        return (
            Game1.uniqueIDForThisGame,
            Game1.Date.TotalDays,
            Game1.getFarm()?.NameOrUniqueName ?? "Farm"
        );
    }

    public static RoosterSeedKey.Context BuildContext(
        RoosterType roosterType,
        string farmAnimalName,
        string buildingType,
        int? dayOverride = null,
        string? locationNameOverride = null,
        int? seedOverride = null
    )
    {
        var (farmId, day, locationName) = GetWorldSeedInputs();

        if (seedOverride is not null)
            farmId = unchecked((ulong)seedOverride.Value);

        if (dayOverride is not null)
            day = dayOverride.Value;

        if (!string.IsNullOrWhiteSpace(locationNameOverride))
            locationName = locationNameOverride!.Trim();

        return new RoosterSeedKey.Context(
            FarmId: farmId,
            Day: day,
            LocationName: locationName,
            BuildingType: (buildingType ?? string.Empty).Trim(),
            FarmAnimalName: (farmAnimalName ?? string.Empty).Trim(),
            RoosterType: roosterType
        );
    }

    public static string ComputeReproHash(string tag, in RoosterSeedKey.Context ctx)
    {
        tag ??= string.Empty;

        return StableHash.Hex(StableHash.Fnv1a64(
            $"rc|repro|{tag}|{ctx.FarmId}|{ctx.Day}|{ctx.LocationName}|{ctx.BuildingType}|{ctx.FarmAnimalName}|{ctx.RoosterType}"
        ));
    }
}
