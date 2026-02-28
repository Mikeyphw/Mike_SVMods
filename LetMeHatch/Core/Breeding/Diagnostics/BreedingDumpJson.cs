using System.Collections.Generic;
using System.Text.Json;

namespace LetMeHatch.Core.Breeding.Diagnostics;

/// <summary>
/// Centralizes JSON serialization for debug dumps to keep output stable.
/// </summary>
public static class BreedingDumpJson
{
    private static readonly JsonSerializerOptions OptionsIndented = new() { WriteIndented = true };
    private static readonly JsonSerializerOptions OptionsCompact = new() { WriteIndented = false };

    public static string SerializePlans(IEnumerable<BreedingPlan> plans, bool indented = true)
    {
        var dto = BreedingDumpDtos.FromPlans(plans);
        return JsonSerializer.Serialize(dto, indented ? OptionsIndented : OptionsCompact);
    }

    public static string SerializeCoopIndex(CoopBreedingIndex index, bool indented = true)
    {
        var dto = BreedingDumpDtos.FromCoopIndex(index);
        return JsonSerializer.Serialize(dto, indented ? OptionsIndented : OptionsCompact);
    }
}
