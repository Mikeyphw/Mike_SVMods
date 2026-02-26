using System;
using System.Collections.Generic;

namespace LetMeHatch.Core.ClonePipeline;

/// <summary>
/// Minimal animal produce snapshot for clone planning (Core does not depend on Stardew types).
/// </summary>
public sealed record FarmAnimalProduceLists(
    IReadOnlyList<string> ProduceItemIds,
    IReadOnlyList<string> DeluxeProduceItemIds
)
{
    public static readonly FarmAnimalProduceLists Empty = new(Array.Empty<string>(), Array.Empty<string>());
}
